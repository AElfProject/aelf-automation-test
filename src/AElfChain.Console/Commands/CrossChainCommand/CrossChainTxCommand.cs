using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Acs0;
using AElf;
using AElf.Contracts.MultiToken;
using AElf.Types;
using AElfChain.Common;
using AElfChain.Common.Contracts;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using Google.Protobuf;
using Sharprompt;
using Shouldly;
using Volo.Abp.Threading;

namespace AElfChain.Console.Commands
{
    public class CrossChainTxCommand : BaseCommand
    {
        private NodesInfo _mainNodes;
        private NodesInfo _sideNodes;
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
            var mainConfig = Prompt.Select("Select MainChain config", configFiles);
            _mainNodes = NodeInfoHelper.ReadConfigInfo(Path.Combine(configPath, mainConfig));
            var firstBp = _mainNodes.Nodes.First();
            MainChain = new ChainService(firstBp);

            configFiles = Directory.GetFiles(configPath, "*-side*.json")
                .Select(o => o.Split("/").Last()).ToList();
            var sideConfig = Prompt.Select("Select SideChain config", configFiles);
            _sideNodes = NodeInfoHelper.ReadConfigInfo(Path.Combine(configPath, sideConfig));
            var sideBp = _sideNodes.Nodes.First();
            SideChain = new ChainService(sideBp);

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
            var transactionResult = await SideChain.GenesisStub.ValidateSystemContractAddress.SendAsync(
                new ValidateSystemContractAddressInput
                {
                    Address = AddressHelper.Base58StringToAddress(SideChain.TokenService.ContractAddress),
                    SystemContractHashName = Hash.FromString("AElf.ContractNames.Token")
                });
            transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var validateBlockNumber = transactionResult.TransactionResult.BlockNumber;
            var transactionId = transactionResult.TransactionResult.TransactionId.ToHex();
            var indexHeight = await MainChain.CheckSideChainBlockIndex(validateBlockNumber, SideChain);
            await SideChain.CheckParentChainBlockIndex(indexHeight);

            var merklePath = await SideChain.GetMerklePath(validateBlockNumber, transactionId);
            if (merklePath == null)
                throw new Exception("Can't get the merkle path.");
            var crossChainMerkleProofContext =
                SideChain.CrossChainService.GetCrossChainMerkleProofContext(validateBlockNumber);
            var registerInput = new RegisterCrossChainTokenContractAddressInput
            {
                FromChainId = SideChain.ChainId,
                ParentChainHeight = crossChainMerkleProofContext.BoundParentChainHeight,
                TokenContractAddress =
                    AddressHelper.Base58StringToAddress(SideChain.TokenService.ContractAddress),
                TransactionBytes = ByteString.CopyFrom(transactionResult.Transaction.ToByteArray()),
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
            var transactionResult = await MainChain.GenesisStub.ValidateSystemContractAddress.SendAsync(
                new ValidateSystemContractAddressInput
                {
                    Address = AddressHelper.Base58StringToAddress(MainChain.TokenService.ContractAddress),
                    SystemContractHashName = Hash.FromString("AElf.ContractNames.Token")
                });
            transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var validateBlockNumber = transactionResult.TransactionResult.BlockNumber;
            var transactionId = transactionResult.TransactionResult.TransactionId.ToHex();
            await SideChain.CheckParentChainBlockIndex(validateBlockNumber);

            //register main chain token address
            var merklePath = await MainChain.GetMerklePath(validateBlockNumber, transactionId);
            if (merklePath == null)
                throw new Exception("Can't get the merkle path.");
            var registerInput = new RegisterCrossChainTokenContractAddressInput
            {
                FromChainId = MainChain.ChainId,
                ParentChainHeight = validateBlockNumber,
                TokenContractAddress =
                    MainChain.TokenService.ContractAddress.ConvertAddress(),
                TransactionBytes = ByteString.CopyFrom(transactionResult.Transaction.ToByteArray()),
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
            //verify token exist on main and side chain
            var mainToken = MainChain.TokenService.GetTokenInfo(symbol);
            if (mainToken.Equals(new TokenInfo()))
            {
                Logger.Error($"Main chain without '{input}' token.");
                return;
            }

            var sideToken = SideChain.TokenService.GetTokenInfo(symbol);
            if (!sideToken.Equals(new TokenInfo()))
            {
                Logger.Info($"Side chain already with '{input}' token.");
                return;
            }

            //validate token on main chain
            var transactionResult = await MainChain.TokenStub.ValidateTokenInfoExists.SendAsync(
                new ValidateTokenInfoExistsInput
                {
                    Decimals = mainToken.Decimals,
                    Issuer = mainToken.Issuer,
                    IsBurnable = mainToken.IsBurnable,
                    IssueChainId = mainToken.IssueChainId,
                    Symbol = mainToken.Symbol,
                    TokenName = mainToken.TokenName,
                    TotalSupply = mainToken.TotalSupply
                });
            transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined,
                $"Validate MainChain token '{symbol}' failed");
            var checkBlockNumber = transactionResult.TransactionResult.BlockNumber;
            var transactionId = transactionResult.TransactionResult.TransactionId.ToHex();
            await SideChain.CheckParentChainBlockIndex(checkBlockNumber);
            //create token on side chains
            var merklePath = await MainChain.GetMerklePath(checkBlockNumber, transactionId);
            if (merklePath == null)
                throw new Exception("Can't get the merkle path.");
            var createInput = new CrossChainCreateTokenInput
            {
                FromChainId = MainChain.ChainId,
                MerklePath = merklePath,
                TransactionBytes = ByteString.CopyFrom(transactionResult.Transaction.ToByteArray()),
                ParentChainHeight = checkBlockNumber
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
            //main chain transfer
            var crossChainTransferInput = new CrossChainTransferInput
            {
                Symbol = symbol,
                IssueChainId = MainChain.ChainId,
                Amount = amount,
                Memo = "main->side transfer",
                To = to.ConvertAddress(),
                ToChainId = SideChain.ChainId,
            };
            var tokenStub = MainChain.GenesisService.GetTokenStub(from);
            var transactionResult = await tokenStub.CrossChainTransfer.SendAsync(crossChainTransferInput);
            transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var transferBlockNumber = transactionResult.TransactionResult.BlockNumber;
            var transactionId = transactionResult.TransactionResult.TransactionId.ToHex();
            Logger.Info(
                $"MainChain transaction block: {transferBlockNumber}, txId:{transactionId} to side chain {SideChain.ChainId}");
            await SideChain.CheckParentChainBlockIndex(transferBlockNumber);
            //side chain receiver
            var merklePath = await MainChain.GetMerklePath(transferBlockNumber, transactionId);
            var crossChainReceiveToken = new CrossChainReceiveTokenInput
            {
                FromChainId = MainChain.ChainId,
                ParentChainHeight = transferBlockNumber,
                MerklePath = merklePath,
                TransferTransactionBytes = ByteString.CopyFrom(transactionResult.Transaction.ToByteArray())
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
            //side chain transfers
            var crossChainTransferInput = new CrossChainTransferInput
            {
                Symbol = symbol,
                IssueChainId = SideChain.ChainId,
                Amount = amount,
                Memo = "side->main transfer",
                To = to.ConvertAddress(),
                ToChainId = MainChain.ChainId
            };
            var tokenStub = SideChain.GenesisService.GetTokenStub(from);
            var transactionResult = await tokenStub.CrossChainTransfer.SendAsync(crossChainTransferInput);
            transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var transferBlockNumber = transactionResult.TransactionResult.BlockNumber;
            var transactionId = transactionResult.TransactionResult.TransactionId.ToHex();
            Logger.Info(
                $"SideChain transaction block: {transferBlockNumber}, txId:{transactionId} to main chain {MainChain.ChainId}");
            var indexHeight = await MainChain.CheckSideChainBlockIndex(transferBlockNumber, SideChain);
            await SideChain.CheckParentChainBlockIndex(indexHeight);

            //main chain receiver
            var merklePath = await SideChain.GetMerklePath(transferBlockNumber, transactionId);
            var crossChainReceiveToken = new CrossChainReceiveTokenInput
            {
                FromChainId = SideChain.ChainId,
                MerklePath = merklePath
            };
            // verify side chain transaction
            var crossChainMerkleProofContext =
                SideChain.CrossChainService.GetCrossChainMerkleProofContext(transferBlockNumber);
            crossChainReceiveToken.MerklePath.MerklePathNodes.AddRange(crossChainMerkleProofContext
                .MerklePathFromParentChain.MerklePathNodes);
            crossChainReceiveToken.ParentChainHeight = crossChainMerkleProofContext.BoundParentChainHeight;
            crossChainReceiveToken.TransferTransactionBytes =
                ByteString.CopyFrom(transactionResult.Transaction.ToByteArray());

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
                "Register[Main-Side]",
                "Register[Side-Main]",
                "CreateToken[Side]",
                "Transfer[Main-Side]",
                "Transfer[Side-Main]",
                "Exit"
            };
        }
    }
}