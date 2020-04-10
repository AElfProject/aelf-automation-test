using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Contracts.MultiToken;
using AElf.Contracts.TokenHolder;
using AElf.Types;
using AElfChain.Common;
using AElfChain.Common.Contracts;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using log4net;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Shouldly;

namespace AElf.Automation.Contracts.ScenarioTest
{
    [TestClass]
    public class SideChainProfitSideTests
    {
        public SideChainProfitSideTests()
        {
            Log4NetHelper.LogInit("SideChainProfitSide");
            Logger = Log4NetHelper.GetLogger();

            NodeInfoHelper.SetConfig("nodes-env2-side1");
            var node = NodeInfoHelper.Config.Nodes.First();

            NodeManager = new NodeManager(node.Endpoint);
            SideManager = new ContractManager(NodeManager, node.Account);
        }

        private ILog Logger { get; }

        public INodeManager NodeManager { get; set; }
        public ContractManager SideManager { get; set; }

        [TestMethod]
        public void Prepare_TestToken()
        {
            const long BALANCE = 5000_00000000L;
            var symbols = new[] {"STA", "ELF", "RAM", "CPU"};
            var secondBp = NodeInfoHelper.Config.Nodes[1].Account;
            foreach (var symbol in symbols)
                SideManager.Token.TransferBalance(SideManager.CallAddress, secondBp, BALANCE, symbol);

            foreach (var symbol in symbols)
            {
                var balance = SideManager.Token.GetUserBalance(secondBp, symbol);
                Logger.Info($"{secondBp} {symbol}={balance}");
            }
        }

        [TestMethod]
        public async Task Register_Mortgage_Test()
        {
            var beforeBalance = SideManager.Token.GetUserBalance(SideManager.CallAddress, "SHARE");
            var approveResult = await SideManager.TokenImplStub.Approve.SendAsync(new ApproveInput
            {
                Spender = SideManager.TokenHolder.Contract,
                Amount = 100,
                Symbol = "SHARE"
            });
            approveResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var registerResult =
                await SideManager.TokenHolderStub.RegisterForProfits.SendAsync(new RegisterForProfitsInput
                {
                    SchemeManager = SideManager.Consensus.Contract,
                    Amount = 100
                });
            registerResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined,
                registerResult.TransactionResult.Error);

            var afterBalance = SideManager.Token.GetUserBalance(SideManager.CallAddress, "SHARE");
            afterBalance.ShouldBe(beforeBalance - 100);
        }

        [TestMethod]
        [DataRow("SCPU")]
        [DataRow("SRAM")]
        public async Task CreateNewToken_And_Contribute_Test(string symbol)
        {
            const long supply = 10_00000000_00000000L;

            //create
            var createResult = await SideManager.TokenImplStub.Create.SendAsync(new CreateInput
            {
                Decimals = 8,
                IsBurnable = true,
                IsProfitable = true,
                IssueChainId = ChainHelper.ConvertBase58ToChainId(NodeManager.GetChainId()),
                Issuer = SideManager.CallAccount,
                Symbol = symbol,
                TokenName = $"Test Token {symbol}",
                TotalSupply = supply
            });
            createResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            //issue
            var bpUsers = SideManager.Authority.GetCurrentMiners();
            foreach (var bp in bpUsers)
            {
                if (bp == SideManager.CallAddress) continue;
                var issueResult = await SideManager.TokenImplStub.Issue.SendAsync(new IssueInput
                {
                    To = bp.ConvertAddress(),
                    Amount = 5000000_00000000L,
                    Symbol = symbol,
                    Memo = "issue token"
                });
                issueResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

                //query result
                var balance = SideManager.Token.GetUserBalance(bp, symbol);
                Logger.Info($"Account: {bp}, {symbol}={balance}");

                //distribute
                await Contribute_SideChainDividendsPool_Test(symbol, 1000000_00000000L);
            }
        }

        [TestMethod]
        [DataRow("SCPU", 400_00000000L)]
        [DataRow("SRAM", 400_00000000L)]
        public async Task Contribute_SideChainDividendsPool_Test(string symbol, long amount)
        {
            var secondBp = NodeInfoHelper.Config.Nodes[1].Account;
            var consensusStub = SideManager.Genesis.GetConsensusImplStub(secondBp);
            var tokenStub = SideManager.Genesis.GetTokenImplStub(secondBp);
            var approveResult = await tokenStub.Approve.SendAsync(new ApproveInput
            {
                Spender = SideManager.Consensus.Contract,
                Amount = amount,
                Symbol = symbol
            });
            approveResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var contributeResult =
                await consensusStub.ContributeToSideChainDividendsPool.SendAsync(
                    new ContributeToSideChainDividendsPoolInput
                    {
                        Symbol = symbol,
                        Amount = amount
                    });
            contributeResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        }

        [TestMethod]
        public void Query_User_ProfitSymbol_Test()
        {
            var symbols = new[] {"SCPU", "SRAM"};
            foreach (var symbol in symbols)
            {
                var balance = SideManager.Token.GetUserBalance(SideManager.CallAddress, symbol);
                Logger.Info($"Bp balance info: {symbol}={balance}");
            }
        }

        [TestMethod]
        [DataRow("CPU")]
        public async Task ClaimProfit_Test(string symbol)
        {
            var profitMap = await SideManager.TokenHolderStub.GetProfitsMap.CallAsync(new ClaimProfitsInput
            {
                SchemeManager = SideManager.Consensus.Contract,
                Beneficiary = SideManager.CallAccount
            });
            Logger.Info(JsonConvert.SerializeObject(profitMap));

            var beforeBalance = SideManager.Token.GetUserBalance(SideManager.CallAddress, symbol);
            var claimResult = await SideManager.TokenHolderStub.ClaimProfits.SendAsync(new ClaimProfitsInput
            {
                SchemeManager = SideManager.Consensus.Contract,
                Beneficiary = SideManager.CallAccount
            });
            claimResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var afterBalance = SideManager.Token.GetUserBalance(SideManager.CallAddress, symbol);
            Logger.Info($"Profit balance: {afterBalance - beforeBalance} {symbol}");
        }

        [TestMethod]
        public async Task Summary_Case()
        {
            Prepare_TestToken();
            await Register_Mortgage_Test();
            await Contribute_SideChainDividendsPool_Test("ELF", 500_00000000L);
            await Contribute_SideChainDividendsPool_Test("RAM", 500_00000000L);
            await Contribute_SideChainDividendsPool_Test("CPU", 500_00000000L);
        }

        [TestMethod]
        public async Task QueryProfitMap_Test()
        {
            var profitMap = await SideManager.TokenHolderStub.GetProfitsMap.CallAsync(new ClaimProfitsInput
            {
                SchemeManager = SideManager.Consensus.Contract,
                Beneficiary = SideManager.CallAccount
            });
            foreach (var (key, value) in profitMap.Value) Logger.Info($"Profit info {key} = {value}");
        }

        [TestMethod]
        public async Task GetScheme_Test()
        {
            var scheme = await SideManager.TokenHolderStub.GetScheme.CallAsync(SideManager.Consensus.Contract);
            Logger.Info(scheme.SchemeId.ToHex());

            var schemeInfo = await SideManager.ProfitStub.GetScheme.CallAsync(scheme.SchemeId);
        }
    }
}