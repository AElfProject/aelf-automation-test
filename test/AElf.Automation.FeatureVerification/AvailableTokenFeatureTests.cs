using System.Linq;
using System.Threading.Tasks;
using Acs3;
using AElf.Contracts.MultiToken;
using AElf.Contracts.Parliament;
using AElf.Contracts.TokenConverter;
using AElf.Types;
using AElfChain.Common;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using Google.Protobuf.WellKnownTypes;
using log4net;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace AElf.Automation.Contracts.ScenarioTest
{
    [TestClass]
    public class AvailableTokenFeatureTests
    {
        private static readonly ILog Logger = Log4NetHelper.GetLogger();

        public AvailableTokenFeatureTests()
        {
            Log4NetHelper.LogInit();
            NodeInfoHelper.SetConfig("nodes-env-3bp");
            var firstNode = NodeInfoHelper.Config.Nodes.First();

            NodeManager = new NodeManager(firstNode.Endpoint);
            ContractManager = new ContractManager(NodeManager, firstNode.Account);
        }

        public INodeManager NodeManager { get; set; }
        public ContractManager ContractManager { get; set; }

        [TestMethod]
        public async Task QueryAvailableTokenInfos()
        {
            var tokenInfos = await ContractManager.TokenStub.GetSymbolsToPayTxSizeFee.CallAsync(new Empty());
            if (tokenInfos.Equals(new SymbolListToPayTxSizeFee()))
            {
                Logger.Info("GetAvailableTokenInfos: Null");
                return;
            }

            foreach (var info in tokenInfos.SymbolsToPayTxSizeFee)
                Logger.Info(
                    $"Symbol: {info.TokenSymbol}, TokenWeight: {info.AddedTokenWeight}, BaseWeight: {info.BaseTokenWeight}");
        }

        [TestMethod]
        public async Task SetAvailableTokenInfos()
        {
            var availableTokenInfo = new SymbolListToPayTxSizeFee
            {
                SymbolsToPayTxSizeFee =
                {
                    new SymbolToPayTxSizeFee
                    {
                        TokenSymbol = "ELF",
                        AddedTokenWeight = 1,
                        BaseTokenWeight = 1
                    },
                    new SymbolToPayTxSizeFee
                    {
                        TokenSymbol = "CPU",
                        AddedTokenWeight = 50,
                        BaseTokenWeight = 1
                    },
                    new SymbolToPayTxSizeFee
                    {
                        TokenSymbol = "RAM",
                        AddedTokenWeight = 50,
                        BaseTokenWeight = 1
                    },
                    new SymbolToPayTxSizeFee
                    {
                        TokenSymbol = "NET",
                        AddedTokenWeight = 50,
                        BaseTokenWeight = 1
                    }
                }
            };

            var transactionResult = ContractManager.Authority.ExecuteTransactionWithAuthority(
                ContractManager.Token.ContractAddress, nameof(ContractManager.TokenStub.SetSymbolsToPayTxSizeFee),
                availableTokenInfo, ContractManager.CallAddress);
            transactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            await QueryAvailableTokenInfos();
        }

        [TestMethod]
        [DataRow("2sWEUtNMJLWFTUp3SGwM8i9aoTM2rdyMYuxQAEgD6XaDJjz9ch", "ELF", 5000_0000L)]
        [DataRow("2sWEUtNMJLWFTUp3SGwM8i9aoTM2rdyMYuxQAEgD6XaDJjz9ch", "CPU", 8_0000_0000L)]
        [DataRow("2sWEUtNMJLWFTUp3SGwM8i9aoTM2rdyMYuxQAEgD6XaDJjz9ch", "NYNYO", 50_0000_0000L)]
        public async Task PrepareTesterToken(string account, string symbol, long amount)
        {
            //bp balance
            var bpBalance = ContractManager.Token.GetUserBalance(ContractManager.CallAddress, symbol);
            if (bpBalance < 100_00000000L)
                if (symbol != "ELF")
                {
                    var buyResult = await ContractManager.TokenconverterStub.Buy.SendAsync(new BuyInput
                    {
                        Symbol = symbol,
                        Amount = 100_00000000L
                    });
                    buyResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
                }

            var balance = ContractManager.Token.GetUserBalance(account, symbol);
            if (balance >= amount) return;
            var transactionResult =
                ContractManager.Token.TransferBalance(ContractManager.CallAddress, account, amount - balance, symbol);
            transactionResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            //query user balance
            var afterBalance = ContractManager.Token.GetUserBalance(account, symbol);
            Logger.Info($"Account: {account}, {symbol} = {afterBalance}");
        }

        [TestMethod]
        public async Task CreateNewToken()
        {
            string symbol;
            while (true)
            {
                symbol = CommonHelper.RandomString(5, false);
                var tokenInfo = ContractManager.Token.GetTokenInfo(symbol);
                if (tokenInfo.Equals(new TokenInfo())) break;
            }

            //create
            var createResult = await ContractManager.TokenStub.Create.SendAsync(new CreateInput
            {
                TokenName = "Test create token",
                Symbol = symbol,
                Decimals = 8,
                IsBurnable = true,
                IsProfitable = true,
                IssueChainId = ChainHelper.ConvertBase58ToChainId(NodeManager.GetChainId()),
                Issuer = ContractManager.CallAccount,
                TotalSupply = 10_0000_0000_00000000L
            });
            createResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            //issue
            var issueResult = await ContractManager.TokenStub.Issue.SendAsync(new IssueInput
            {
                To = ContractManager.CallAccount,
                Amount = 5_0000_0000_00000000L,
                Symbol = symbol,
                Memo = "issue half tokens"
            });
            issueResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            //get balance
            var balance = ContractManager.Token.GetUserBalance(ContractManager.CallAddress, symbol);
            Logger.Info($"Account: {ContractManager.CallAddress}, Symbol: {symbol}, Balance: {balance}");
        }

        [TestMethod]
        public async Task Controller_Transfer_For_Symbol_To_Pay_Tx_Fee()
        {
            var primaryToken = ContractManager.Token.GetPrimaryTokenSymbol();

            //Without authority would be failed
            var newSymbolList = new SymbolListToPayTxSizeFee();
            newSymbolList.SymbolsToPayTxSizeFee.Add(new SymbolToPayTxSizeFee
            {
                TokenSymbol = primaryToken,
                AddedTokenWeight = 1,
                BaseTokenWeight = 1
            });
            var symbolSetRet = await ContractManager.TokenStub.SetSymbolsToPayTxSizeFee.SendAsync(newSymbolList);
            symbolSetRet.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);

            var newParliament = new CreateOrganizationInput
            {
                ProposerAuthorityRequired = false,
                ProposalReleaseThreshold = new ProposalReleaseThreshold
                {
                    MaximalAbstentionThreshold = 1,
                    MaximalRejectionThreshold = 1,
                    MinimalApprovalThreshold = 1,
                    MinimalVoteThreshold = 1
                },
                ParliamentMemberProposingAllowed = false
            };
            var parliamentCreateRet =
                await ContractManager.ParliamentAuthStub.CreateOrganization.SendAsync(newParliament);
            var newOrganization = parliamentCreateRet.Output;

            var transactionResult = ContractManager.Authority.ExecuteTransactionWithAuthority(
                ContractManager.Token.ContractAddress,
                nameof(ContractManager.TokenImplStub.SetSymbolsToPayTxSizeFee),
                newOrganization,
                ContractManager.CallAddress
            );
            transactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            transactionResult = ContractManager.Authority.ExecuteTransactionWithAuthority(
                ContractManager.Token.ContractAddress,
                nameof(TokenContractContainer.TokenContractStub.SetSymbolsToPayTxSizeFee),
                newSymbolList,
                newOrganization,
                ContractManager.Authority.GetCurrentMiners(),
                ContractManager.CallAddress
            );
            transactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            //Verify symbol list
            var symbolList = await ContractManager.TokenStub.GetSymbolsToPayTxSizeFee.CallAsync(new Empty());
            symbolList.SymbolsToPayTxSizeFee.Count.ShouldBe(1);
        }
    }
}