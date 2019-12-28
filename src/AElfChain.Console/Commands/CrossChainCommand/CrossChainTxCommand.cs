using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AElf;
using AElf.Contracts.MultiToken;
using AElf.Types;
using AElfChain.Common;
using AElfChain.Common.Contracts;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Sharprompt;
using Shouldly;
using Volo.Abp.Threading;

namespace AElfChain.Console.Commands
{
    public class CrossChainTxCommand : BaseCommand
    {
        private NodesInfo _mainNodes;
        private NodesInfo _sideNodes;

        private NodeManager _mainManager;
        private NodeManager _sideManager;

        public ChainService MainChain { get; set; }
        public ChainService SideChain { get; set; }

        public CrossChainTxCommand(INodeManager nodeManager, ContractServices contractServices)
            : base(nodeManager, contractServices)
        {
            Logger = Log4NetHelper.GetLogger();
        }

        public override void RunCommand()
        {
            var configPath = CommonHelper.MapPath("config");
            var configFiles = Directory.GetFiles(configPath, "*-main*.json")
                .Select(o => o.Split("/").Last()).ToList();
            var mainConfig = Prompt.Select("Select env config", configFiles);
            _mainNodes = NodeInfoHelper.ReadConfigInfo(mainConfig);
            var firstBp = _mainNodes.Nodes.First();
            _mainManager = new NodeManager(firstBp.Endpoint);
            MainChain = new ChainService(firstBp);

            configFiles = Directory.GetFiles(configPath, "*-side*.json")
                .Select(o => o.Split("/").Last()).ToList();
            var sideConfig = Prompt.Select("Select env config", configFiles);
            _sideNodes = NodeInfoHelper.ReadConfigInfo(sideConfig);
            var sideBp = _sideNodes.Nodes.First();
            _sideManager = new NodeManager(sideBp.Endpoint);
            SideChain = new ChainService(sideBp);

            var mainTokenInfo = MainChain.TokenStub.GetNativeTokenInfo.CallAsync(new Empty()).Result;
            Logger.Info(JsonFormatter.ToDiagnosticString(mainTokenInfo), Format.Json);

            var sideTokenInfo = SideChain.TokenStub.GetNativeTokenInfo.CallAsync(new Empty()).Result;
            Logger.Info(JsonFormatter.ToDiagnosticString(sideTokenInfo), Format.Json);

            var quitCommand = false;
            while (!quitCommand)
            {
                var command = Prompt.Select("Select cross chain tx command", GetSubCommands());
                switch (command)
                {
                    case "Register[Main-Side]":
                        AsyncHelper.RunSync(MainChainRegisterSideChain);
                        break;
                    case "Register[Side-Main]":
                        AsyncHelper.RunSync(SideChainRegisterMainChain);
                        break;
                    case "CreateToken[Side]":
                        AsyncHelper.RunSync(SideChainCreateToken);
                        break;
                    case "Transfer[Main-Side]":
                        AsyncHelper.RunSync(TransferMain2Side);
                        break;
                    case "Transfer[Side-Main]":
                        AsyncHelper.RunSync(TransferSide2Main);
                        break;
                    case "Exit":
                        quitCommand = true;
                        break;
                    default:
                        Logger.Error("Not supported method.");
                        var subCommands = GetSubCommands();
                        string.Join("\r\n", subCommands).WriteSuccessLine();
                        break;
                }
            }
        }

        public override CommandInfo GetCommandInfo()
        {
            return new CommandInfo
            {
                Name = "cross-chain-tx",
                Description = "Cross chain transactions"
            };
        }

        public override string[] InputParameters()
        {
            throw new NotImplementedException();
        }

        private async Task MainChainRegisterSideChain()
        {
            var rawTx = SideChain.ValidateTokenAddress();
            var txId = _sideManager.SendTransaction(rawTx);
            var txResult = _sideManager.CheckTransactionResult(txId);
            if (txResult.Status.ConvertTransactionResultStatus() != TransactionResultStatus.Mined)
            {
                Logger.Error($"Validate SideChain {SideChain.ChainId} token contract failed");
                return;
            }

            await MainChain.CheckSideChainBlockIndex(txResult.BlockNumber, SideChain);
            var merklePath = await MainChain.GetMerklePath(txResult.BlockNumber, txId);
            if (merklePath == null)
                throw new Exception("Can't get the merkle path.");
            var crossChainMerkleProofContext =
                SideChain.CrossChainService.GetCrossChainMerkleProofContext(txResult.BlockNumber);
            var registerInput = new RegisterCrossChainTokenContractAddressInput
            {
                FromChainId = SideChain.ChainId,
                ParentChainHeight = crossChainMerkleProofContext.BoundParentChainHeight,
                TokenContractAddress =
                    AddressHelper.Base58StringToAddress(SideChain.TokenService.ContractAddress),
                TransactionBytes = ByteString.CopyFrom(ByteArrayHelper.HexStringToByteArray(rawTx)),
                MerklePath = merklePath
            };
            registerInput.MerklePath.MerklePathNodes.AddRange(crossChainMerkleProofContext.MerklePathFromParentChain
                .MerklePathNodes);
            MainChain.Authority.ExecuteTransactionWithAuthority(MainChain.TokenService.ContractAddress,
                nameof(TokenMethod.RegisterCrossChainTokenContractAddress), registerInput, MainChain.CallAddress);
            Logger.Info(
                $"Main chain register chain {SideChain.ChainId} token address {SideChain.TokenService.ContractAddress}");
        }

        private async Task SideChainRegisterMainChain()
        {
            var rawTx = MainChain.ValidateTokenAddress();
            var txId = _mainManager.SendTransaction(rawTx);
            var txResult = _mainManager.CheckTransactionResult(txId);
            if (txResult.Status.ConvertTransactionResultStatus() != TransactionResultStatus.Mined)
            {
                Logger.Error($"Validate MainChain {MainChain.ChainId} token contract failed");
                return;
            }
            await SideChain.CheckParentChainBlockIndex(txResult.BlockNumber);
            
            //register main chain token address
            var merklePath = await MainChain.GetMerklePath(txResult.BlockNumber, txId);
            if (merklePath == null)
                throw new Exception("Can't get the merkle path.");
            var registerInput = new RegisterCrossChainTokenContractAddressInput
            {
                FromChainId = MainChain.ChainId,
                ParentChainHeight = txResult.BlockNumber,
                TokenContractAddress =
                    MainChain.TokenService.ContractAddress.ConvertAddress(),
                TransactionBytes = ByteString.CopyFrom(ByteArrayHelper.HexStringToByteArray(rawTx)),
                MerklePath = merklePath
            };

            SideChain.Authority.ExecuteTransactionWithAuthority(SideChain.TokenService.ContractAddress,
                nameof(TokenMethod.RegisterCrossChainTokenContractAddress), registerInput, SideChain.CallAddress);
            Logger.Info(
                $"Chain {SideChain.ChainId} register Main chain token address {MainChain.TokenService.ContractAddress}");
            
        }

        private async Task SideChainCreateToken()
        {
            var input = Prompt.Input<string>("Input token Symbol:");
            var symbol = input.ToUpper();
            var mainToken = MainChain.TokenService.GetTokenInfo(symbol);
            if (mainToken.Equals(new TokenInfo()))
            {
                Logger.Error($"Main chain without '{input}' token.");
                return;
            }

            var sideToken = SideChain.TokenService.GetTokenInfo(symbol);
            if (sideToken.Equals(new TokenInfo()))
            {
                Logger.Info($"Side chain already with '{input}' token.");
                return;
            }
            
            //validate token
            var rawTx = MainChain.ValidateTokenSymbol(symbol);
            var txId = _mainManager.SendTransaction(rawTx);
            var txResult = _mainManager.CheckTransactionResult(txId);
            if (txResult.Status.ConvertTransactionResultStatus() != TransactionResultStatus.Mined)
            {
                Logger.Error($"Validate MainChain token '{symbol}' failed");
                return;
            }

            await SideChain.CheckParentChainBlockIndex(txResult.BlockNumber);
            var merklePath = await MainChain.GetMerklePath(txResult.BlockNumber, txResult.TransactionId);
            if (merklePath == null)
                throw new Exception("Can't get the merkle path.");
            var createInput = new CrossChainCreateTokenInput
            {
                FromChainId = MainChain.ChainId,
                MerklePath = merklePath,
                TransactionBytes = ByteString.CopyFrom(ByteArrayHelper.HexStringToByteArray(rawTx))
            };

            var createResult =
                SideChain.TokenService.ExecuteMethodWithResult(TokenMethod.CrossChainCreateToken,
                    createInput);
            createResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var tokenInfo = SideChain.TokenService.GetTokenInfo(symbol);
            tokenInfo.ShouldNotBe(new TokenInfo());
            Logger.Info($"Side chain token '{symbol}' created success.");
        }

        private async Task TransferMain2Side()
        {
            var from = "ZCP9k7YPHgeMM1XF94BjayULQ6hm3E5QFrsXxuPfUtJFz6sGP";
            var to = "2ERBTcqx8CzgMP7fvQS4DnKQX1AM98CSwAGFyRCQvn9Bvs4Qt1";
            var symbol = "ELF";
            var amount = 1000L;
            "Parameter: [From] [To] [Symbol] [Amount]".WriteSuccessLine();
            $"eg: {from} {to} {symbol} {amount}".WriteSuccessLine();
            var input = CommandOption.InputParameters(4);
            from = input[0];
            to = input[1];
            symbol = input[2];
            amount = long.Parse(input[3]);
            var crossChainTransferInput = new CrossChainTransferInput
            {
                Symbol = symbol,
                IssueChainId = MainChain.ChainId,
                Amount = amount,
                Memo = "cross chain transfer",
                To = to.ConvertAddress(),
                ToChainId = SideChain.ChainId,
            };
            // execute cross chain transfer
            var rawTx = _mainManager.GenerateRawTransaction(from,
                MainChain.TokenService.ContractAddress, nameof(TokenMethod.CrossChainTransfer),
                crossChainTransferInput);
            var txId = _mainManager.SendTransaction(rawTx);
            var txResult = _mainManager.CheckTransactionResult(txId);
            // get transaction info            
            var status = txResult.Status.ConvertTransactionResultStatus();
            status.ShouldBe(TransactionResultStatus.Mined);
            Logger.Info(
                $"Cross chain Transaction block: {txResult.BlockNumber}, rawTx: {rawTx}, txId:{txId} to chain {SideChain.ChainId}");

            await SideChain.CheckParentChainBlockIndex(txResult.BlockNumber);

            var merklePath = await MainChain.GetMerklePath(txResult.BlockNumber, txId);
            var crossChainReceiveToken = new CrossChainReceiveTokenInput
            {
                FromChainId = MainChain.ChainId,
                ParentChainHeight = txResult.BlockNumber,
                MerklePath = merklePath,
                TransferTransactionBytes = ByteString.CopyFrom(ByteArrayHelper.HexStringToByteArray(rawTx))
            };
            var beforeBalance = SideChain.TokenService.GetUserBalance(to, symbol);
            var result = SideChain.TokenService.CrossChainReceiveToken(from, crossChainReceiveToken);
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var afterBalance = SideChain.TokenService.GetUserBalance(to, symbol);
            Logger.Info($"{to} {symbol} balance: {beforeBalance} => {afterBalance}");
        }

        private async Task TransferSide2Main()
        {
            var from = "ZCP9k7YPHgeMM1XF94BjayULQ6hm3E5QFrsXxuPfUtJFz6sGP";
            var to = "2ERBTcqx8CzgMP7fvQS4DnKQX1AM98CSwAGFyRCQvn9Bvs4Qt1";
            var symbol = "ELF";
            var amount = 1000L;
            "Parameter: [From] [To] [Symbol] [Amount]".WriteSuccessLine();
            $"eg: {from} {to} {symbol} {amount}".WriteSuccessLine();
            var input = CommandOption.InputParameters(4);
            from = input[0];
            to = input[1];
            symbol = input[2];
            amount = long.Parse(input[3]);
            var crossChainTransferInput = new CrossChainTransferInput
            {
                Symbol = symbol,
                IssueChainId = SideChain.ChainId,
                Amount = amount,
                Memo = "cross chain transfer",
                To = to.ConvertAddress(),
                ToChainId = MainChain.ChainId
            };

            var sideChainTokenContracts = SideChain.TokenService.ContractAddress;

            // execute cross chain transfer
            var rawTx = _sideManager.GenerateRawTransaction(from,
                sideChainTokenContracts, nameof(TokenMethod.CrossChainTransfer),
                crossChainTransferInput);
            var txId = _sideManager.SendTransaction(rawTx);
            var txResult = _sideManager.CheckTransactionResult(txId);
            // get transaction info            
            var status = txResult.Status.ConvertTransactionResultStatus();
            status.ShouldBe(TransactionResultStatus.Mined);
            Logger.Info(
                $"Cross chain Transaction block: {txResult.BlockNumber}, rawTx: {rawTx}, txId:{txId} to chain {MainChain.ChainId}");

            await SideChain.CheckParentChainBlockIndex(txResult.BlockNumber);
            var merklePath = await SideChain.GetMerklePath(txResult.BlockNumber, txId);
            var crossChainReceiveToken = new CrossChainReceiveTokenInput
            {
                FromChainId = SideChain.ChainId,
                MerklePath = merklePath
            };
            // verify side chain transaction
            var crossChainMerkleProofContext = SideChain.CrossChainService.GetCrossChainMerkleProofContext(txResult.BlockNumber);
            crossChainReceiveToken.MerklePath.MerklePathNodes.AddRange(crossChainMerkleProofContext
                .MerklePathFromParentChain.MerklePathNodes);
            crossChainReceiveToken.ParentChainHeight = crossChainMerkleProofContext.BoundParentChainHeight;
            crossChainReceiveToken.TransferTransactionBytes =
                ByteString.CopyFrom(ByteArrayHelper.HexStringToByteArray(rawTx));
            var beforeBalance = MainChain.TokenService.GetUserBalance(to, symbol);
            var result = MainChain.TokenService.CrossChainReceiveToken(from, crossChainReceiveToken);
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var afterBalance = MainChain.TokenService.GetUserBalance(to, symbol);
            Logger.Info($"{to} {symbol} balance: {beforeBalance} => {afterBalance}");
        }

        private IEnumerable<string> GetSubCommands()
        {
            return new List<string>
            {
                "Register",
                "Transfer[Main-Side]",
                "Transfer[Side-Main]",
                "Validation",
                "Exit"
            };
        }
    }
}