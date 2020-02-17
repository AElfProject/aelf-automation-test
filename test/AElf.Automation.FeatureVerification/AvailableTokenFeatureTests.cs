using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using AElf.Types;
using AElfChain.Common;
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
        public INodeManager NodeManager { get; set; }
        public ContractManager ContractManager { get; set; }

        public AvailableTokenFeatureTests()
        {
            Log4NetHelper.LogInit();
            NodeInfoHelper.SetConfig("nodes-env2-main");
            var firstNode = NodeInfoHelper.Config.Nodes.First();

            NodeManager = new NodeManager(firstNode.Endpoint);
            ContractManager = new ContractManager(NodeManager, firstNode.Account);
        }

        [TestMethod]
        public async Task QueryAvailableTokenInfos()
        {
            var tokenInfos = await ContractManager.TokenStub.GetSymbolsToPayTXSizeFee.CallAsync(new Empty());
            if (tokenInfos.Equals(new SymbolListToPayTXSizeFee()))
            {
                Logger.Info("GetAvailableTokenInfos: Null");
                return;
            }

            foreach (var info in tokenInfos.SymbolsToPayTxSizeFee)
            {
                Logger.Info($"Symbol: {info.TokenSymbol}, TokenWeight: {info.AddedTokenWeight}, BaseWeight: {info.BaseTokenWeight}");
            }
        }

        [TestMethod]
        public async Task SetAvailableTokenInfos()
        {
            var availableTokenInfo = new SymbolListToPayTXSizeFee()
            {
                SymbolsToPayTxSizeFee =
                {
                    new SymbolToPayTXSizeFee
                    {
                        TokenSymbol = "RAM",
                        AddedTokenWeight = 100,
                        BaseTokenWeight = 1
                    },
                    new SymbolToPayTXSizeFee
                    {
                        TokenSymbol = "CPU",
                        AddedTokenWeight = 50,
                        BaseTokenWeight = 1
                    },
                    new SymbolToPayTXSizeFee
                    {
                        TokenSymbol = "ELF",
                        AddedTokenWeight = 1,
                        BaseTokenWeight = 1
                    }
                }
            };
            
            var transactionResult = ContractManager.Authority.ExecuteTransactionWithAuthority(
                ContractManager.Token.ContractAddress, nameof(ContractManager.TokenStub.SetSymbolsToPayTXSizeFee),
                availableTokenInfo, ContractManager.CallAddress);
            transactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            await QueryAvailableTokenInfos();
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
    }
}