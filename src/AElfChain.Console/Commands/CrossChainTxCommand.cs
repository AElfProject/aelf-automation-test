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

        public CrossChainTxCommand(INodeManager nodeManager, ContractManager contractManager)
            : base(nodeManager, contractManager)
        {
            Logger = Log4NetHelper.GetLogger();
        }

        public ContractManager MainContract { get; set; }
        public ContractManager SideContract { get; set; }

        public override void RunCommand()
        {
            var configPath = CommonHelper.MapPath("config");
            var configFiles = Directory.GetFiles(configPath, "*-main*.json")
                .Select(o => o.Split("/").Last()).ToList();
            var mainConfig = Prompt.Select("Select MainContract config", configFiles);
            _mainNodes = NodeInfoHelper.ReadConfigInfo(Path.Combine(configPath, mainConfig));
            var firstBp = _mainNodes.Nodes.First();
            MainContract = new ContractManager(new NodeManager(firstBp.Endpoint), firstBp.Account);

            configFiles = Directory.GetFiles(configPath, "*-side*.json")
                .Select(o => o.Split("/").Last()).ToList();
            var sideConfig = Prompt.Select("Select SideContract config", configFiles);
            _sideNodes = NodeInfoHelper.ReadConfigInfo(Path.Combine(configPath, sideConfig));
            var sideBp = _sideNodes.Nodes.First();
            SideContract = new ContractManager(new NodeManager(sideBp.Endpoint), sideBp.Account);

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
                    case "CreateTokens[Side]":
                        AsyncHelper.RunSync(SideChainCreateTokens);
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
            var transactionResult = await SideContract.GenesisStub.ValidateSystemContractAddress.SendAsync(
                new ValidateSystemContractAddressInput
                {
                    Address = AddressHelper.Base58StringToAddress(SideContract.Token.ContractAddress),
                    SystemContractHashName = Hash.FromString("AElf.ContractNames.Token")
                });
            transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var validateBlockNumber = transactionResult.TransactionResult.BlockNumber;
            var transactionId = transactionResult.TransactionResult.TransactionId.ToHex();
            var indexHeight = await MainContract.CheckSideChainBlockIndex(validateBlockNumber, SideContract.ChainId);
            await SideContract.CheckParentChainBlockIndex(indexHeight);

            var merklePath = await SideContract.GetMerklePath(validateBlockNumber, transactionId);
            if (merklePath == null)
                throw new Exception("Can't get the merkle path.");
            var crossChainMerkleProofContext =
                SideContract.CrossChain.GetCrossChainMerkleProofContext(validateBlockNumber);
            var registerInput = new RegisterCrossChainTokenContractAddressInput
            {
                FromChainId = SideContract.ChainId,
                ParentChainHeight = crossChainMerkleProofContext.BoundParentChainHeight,
                TokenContractAddress =
                    AddressHelper.Base58StringToAddress(SideContract.Token.ContractAddress),
                TransactionBytes = ByteString.CopyFrom(transactionResult.Transaction.ToByteArray()),
                MerklePath = merklePath
            };
            registerInput.MerklePath.MerklePathNodes.AddRange(crossChainMerkleProofContext.MerklePathFromParentChain
                .MerklePathNodes);
            MainContract.Authority.ExecuteTransactionWithAuthority(MainContract.Token.ContractAddress,
                nameof(TokenMethod.RegisterCrossChainTokenContractAddress), registerInput, MainContract.CallAddress);
            Logger.Info(
                $"Main chain register chain {SideContract.ChainId} token address {SideContract.Token.ContractAddress}");
        }

        private async Task SideChainRegisterMainChain()
        {
            var transactionResult = await MainContract.GenesisStub.ValidateSystemContractAddress.SendAsync(
                new ValidateSystemContractAddressInput
                {
                    Address = AddressHelper.Base58StringToAddress(MainContract.Token.ContractAddress),
                    SystemContractHashName = Hash.FromString("AElf.ContractNames.Token")
                });
            transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var validateBlockNumber = transactionResult.TransactionResult.BlockNumber;
            var transactionId = transactionResult.TransactionResult.TransactionId.ToHex();
            await SideContract.CheckParentChainBlockIndex(validateBlockNumber);

            //register main chain token address
            var merklePath = await MainContract.GetMerklePath(validateBlockNumber, transactionId);
            if (merklePath == null)
                throw new Exception("Can't get the merkle path.");
            var registerInput = new RegisterCrossChainTokenContractAddressInput
            {
                FromChainId = MainContract.ChainId,
                ParentChainHeight = validateBlockNumber,
                TokenContractAddress =
                    MainContract.Token.ContractAddress.ConvertAddress(),
                TransactionBytes = ByteString.CopyFrom(transactionResult.Transaction.ToByteArray()),
                MerklePath = merklePath
            };

            SideContract.Authority.ExecuteTransactionWithAuthority(SideContract.Token.ContractAddress,
                nameof(TokenMethod.RegisterCrossChainTokenContractAddress), registerInput, SideContract.CallAddress);
            Logger.Info(
                $"Chain {SideContract.ChainId} register Main chain token address {MainContract.Token.ContractAddress}");
        }

        private async Task SideChainCreateToken()
        {
            var input = Prompt.Input<string>("Input token Symbol");
            var symbol = input.ToUpper();
            //verify token exist on main and side chain
            var mainToken = MainContract.Token.GetTokenInfo(symbol);
            if (mainToken.Equals(new TokenInfo()))
            {
                Logger.Error($"Main chain without '{input}' token.");
                return;
            }

            var sideToken = SideContract.Token.GetTokenInfo(symbol);
            if (!sideToken.Equals(new TokenInfo()))
            {
                Logger.Info($"Side chain already with '{input}' token.");
                return;
            }

            //validate token on main chain
            var transactionResult = await MainContract.TokenImplStub.ValidateTokenInfoExists.SendAsync(
                new ValidateTokenInfoExistsInput
                {
                    Decimals = mainToken.Decimals,
                    Issuer = mainToken.Issuer,
                    IsBurnable = mainToken.IsBurnable,
                    IsProfitable = mainToken.IsProfitable,
                    IssueChainId = mainToken.IssueChainId,
                    Symbol = mainToken.Symbol,
                    TokenName = mainToken.TokenName,
                    TotalSupply = mainToken.TotalSupply
                });
            transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined,
                $"Validate MainContract token '{symbol}' failed");
            var checkBlockNumber = transactionResult.TransactionResult.BlockNumber;
            var transactionId = transactionResult.TransactionResult.TransactionId.ToHex();
            await SideContract.CheckParentChainBlockIndex(checkBlockNumber);

            //create token on side chain
            var merklePath = await MainContract.GetMerklePath(checkBlockNumber, transactionId);
            if (merklePath == null)
                throw new Exception("Can't get the merkle path.");
            var createInput = new CrossChainCreateTokenInput
            {
                FromChainId = MainContract.ChainId,
                MerklePath = merklePath,
                TransactionBytes = ByteString.CopyFrom(transactionResult.Transaction.ToByteArray()),
                ParentChainHeight = checkBlockNumber
            };
            var createResult =
                SideContract.Token.ExecuteMethodWithResult(TokenMethod.CrossChainCreateToken,
                    createInput);
            createResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var tokenInfo = SideContract.Token.GetTokenInfo(symbol);
            tokenInfo.ShouldNotBe(new TokenInfo());
            Logger.Info($"Side chain token '{symbol}' created success.");
        }

        private async Task SideChainCreateTokens()
        {
            var input = Prompt.Input<string>("Input token Symbols");
            var symbols = input.ToUpper().Trim().Split(" ");
            foreach (var symbol in symbols)
            {
                //verify token exist on main and side chain
                var mainToken = MainContract.Token.GetTokenInfo(symbol);
                if (mainToken.Equals(new TokenInfo()))
                {
                    Logger.Error($"Main chain without '{symbol}' token.");
                    continue;
                }

                var sideToken = SideContract.Token.GetTokenInfo(symbol);
                if (!sideToken.Equals(new TokenInfo()))
                {
                    Logger.Info($"Side chain already with '{symbol}' token.");
                    continue;
                }

                //validate token on main chain
                var transactionResult = await MainContract.TokenImplStub.ValidateTokenInfoExists.SendAsync(
                    new ValidateTokenInfoExistsInput
                    {
                        Decimals = mainToken.Decimals,
                        Issuer = mainToken.Issuer,
                        IsBurnable = mainToken.IsBurnable,
                        IsProfitable = mainToken.IsProfitable,
                        IssueChainId = mainToken.IssueChainId,
                        Symbol = mainToken.Symbol,
                        TokenName = mainToken.TokenName,
                        TotalSupply = mainToken.TotalSupply
                    });
                transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined,
                    $"Validate MainContract token '{symbol}' failed");
                var checkBlockNumber = transactionResult.TransactionResult.BlockNumber;
                var transactionId = transactionResult.TransactionResult.TransactionId.ToHex();
                await SideContract.CheckParentChainBlockIndex(checkBlockNumber);

                //create token on side chain
                var merklePath = await MainContract.GetMerklePath(checkBlockNumber, transactionId);
                if (merklePath == null)
                    throw new Exception("Can't get the merkle path.");
                var createInput = new CrossChainCreateTokenInput
                {
                    FromChainId = MainContract.ChainId,
                    MerklePath = merklePath,
                    TransactionBytes = ByteString.CopyFrom(transactionResult.Transaction.ToByteArray()),
                    ParentChainHeight = checkBlockNumber
                };
                var createResult =
                    SideContract.Token.ExecuteMethodWithResult(TokenMethod.CrossChainCreateToken,
                        createInput);
                createResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
                var tokenInfo = SideContract.Token.GetTokenInfo(symbol);
                tokenInfo.ShouldNotBe(new TokenInfo());
                Logger.Info($"Side chain token '{symbol}' created success.");
            }
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
                IssueChainId = MainContract.ChainId,
                Amount = amount,
                Memo = "main->side transfer",
                To = to.ConvertAddress(),
                ToChainId = SideContract.ChainId
            };
            var tokenStub = MainContract.Genesis.GetTokenStub(from);
            var transactionResult = await tokenStub.CrossChainTransfer.SendAsync(crossChainTransferInput);
            transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var transferBlockNumber = transactionResult.TransactionResult.BlockNumber;
            var transactionId = transactionResult.TransactionResult.TransactionId.ToHex();
            Logger.Info(
                $"MainContract transaction block: {transferBlockNumber}, txId:{transactionId} to side chain {SideContract.ChainId}");
            await SideContract.CheckParentChainBlockIndex(transferBlockNumber);
            //side chain receiver
            var merklePath = await MainContract.GetMerklePath(transferBlockNumber, transactionId);
            var crossChainReceiveToken = new CrossChainReceiveTokenInput
            {
                FromChainId = MainContract.ChainId,
                ParentChainHeight = transferBlockNumber,
                MerklePath = merklePath,
                TransferTransactionBytes = ByteString.CopyFrom(transactionResult.Transaction.ToByteArray())
            };
            var beforeBalance = SideContract.Token.GetUserBalance(to, symbol);
            var result = SideContract.Token.CrossChainReceiveToken(from, crossChainReceiveToken);
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var afterBalance = SideContract.Token.GetUserBalance(to, symbol);
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
                IssueChainId = SideContract.ChainId,
                Amount = amount,
                Memo = "side->main transfer",
                To = to.ConvertAddress(),
                ToChainId = MainContract.ChainId
            };
            var tokenStub = SideContract.Genesis.GetTokenStub(from);
            var transactionResult = await tokenStub.CrossChainTransfer.SendAsync(crossChainTransferInput);
            transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var transferBlockNumber = transactionResult.TransactionResult.BlockNumber;
            var transactionId = transactionResult.TransactionResult.TransactionId.ToHex();
            Logger.Info(
                $"SideContract transaction block: {transferBlockNumber}, txId:{transactionId} to main chain {MainContract.ChainId}");
            var indexHeight = await MainContract.CheckSideChainBlockIndex(transferBlockNumber, SideContract.ChainId);
            await SideContract.CheckParentChainBlockIndex(indexHeight);

            //main chain receiver
            var merklePath = await SideContract.GetMerklePath(transferBlockNumber, transactionId);
            var crossChainReceiveToken = new CrossChainReceiveTokenInput
            {
                FromChainId = SideContract.ChainId,
                MerklePath = merklePath
            };
            // verify side chain transaction
            var crossChainMerkleProofContext =
                SideContract.CrossChain.GetCrossChainMerkleProofContext(transferBlockNumber);
            crossChainReceiveToken.MerklePath.MerklePathNodes.AddRange(crossChainMerkleProofContext
                .MerklePathFromParentChain.MerklePathNodes);
            crossChainReceiveToken.ParentChainHeight = crossChainMerkleProofContext.BoundParentChainHeight;
            crossChainReceiveToken.TransferTransactionBytes =
                ByteString.CopyFrom(transactionResult.Transaction.ToByteArray());

            var beforeBalance = MainContract.Token.GetUserBalance(to, symbol);
            var result = MainContract.Token.CrossChainReceiveToken(from, crossChainReceiveToken);
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var afterBalance = MainContract.Token.GetUserBalance(to, symbol);
            Logger.Info($"{to} {symbol} balance: {beforeBalance} => {afterBalance}");
        }

        private IEnumerable<string> GetSubCommands()
        {
            return new List<string>
            {
                "Register[Main-Side]",
                "Register[Side-Main]",
                "CreateToken[Side]",
                "CreateTokens[Side]",
                "Transfer[Main-Side]",
                "Transfer[Side-Main]",
                "Exit"
            };
        }
    }
}