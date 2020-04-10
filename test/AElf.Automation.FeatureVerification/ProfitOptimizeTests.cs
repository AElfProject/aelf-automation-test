using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Acs1;
using AElf.Contracts.MultiToken;
using AElf.Contracts.Profit;
using AElf.Contracts.Treasury;
using AElf.Types;
using AElfChain.Common;
using AElfChain.Common.Contracts;
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
    public class ProfitOptimizeTests
    {
        public ProfitOptimizeTests()
        {
            Log4NetHelper.LogInit("ProfitOptimizeTests");
            Logger = Log4NetHelper.GetLogger();

            NodeInfoHelper.SetConfig("nodes-env2-side1");
            var node = NodeInfoHelper.Config.Nodes.First();

            NodeManager = new NodeManager(node.Endpoint);
            ContractManager = new ContractManager(NodeManager, node.Account);
            ContractManager.Profit.GetTreasurySchemes(ContractManager.Treasury.ContractAddress);
            Schemes = ProfitContract.Schemes;
        }

        private ILog Logger { get; }

        public INodeManager NodeManager { get; set; }
        public ContractManager ContractManager { get; set; }

        public Dictionary<SchemeType, Scheme> Schemes { get; set; }

        [TestMethod]
        public async Task SetDistributingSymbolList_Test()
        {
            var beforeList = await ContractManager.TreasuryStub.GetDistributingSymbolList.CallAsync(new Empty());
            beforeList.Value.ShouldBe(new[] {"ELF"});
            var symbolList = new SymbolList
            {
                Value = {"ELF", "CPU", "RAM"}
            };
            var transactionResult = ContractManager.Authority.ExecuteTransactionWithAuthority(
                ContractManager.Treasury.ContractAddress,
                nameof(ContractManager.TreasuryStub.SetDistributingSymbolList),
                symbolList, ContractManager.CallAddress);
            transactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            //Query distribute symbol list
            var queryResult = await ContractManager.TreasuryStub.GetDistributingSymbolList.CallAsync(new Empty());
            queryResult.Value.ShouldBe(symbolList.Value);
        }

        [TestMethod]
        [DataRow("SCPU")]
        [DataRow("SRAM")]
        public async Task Prepare_NewToken_Test(string symbol)
        {
            const long supply = 10_00000000_00000000L;

            //create
            var createResult = await ContractManager.TokenImplStub.Create.SendAsync(new CreateInput
            {
                Decimals = 8,
                IsBurnable = true,
                IsProfitable = true,
                IssueChainId = ChainHelper.ConvertBase58ToChainId(NodeManager.GetChainId()),
                Issuer = ContractManager.CallAccount,
                Symbol = symbol,
                TokenName = $"Test Token {symbol}",
                TotalSupply = supply
            });
            createResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            //issue
            var bpUsers = ContractManager.Authority.GetCurrentMiners();
            foreach (var bp in bpUsers)
            {
                var issueResult = await ContractManager.TokenImplStub.Issue.SendAsync(new IssueInput
                {
                    To = bp.ConvertAddress(),
                    Amount = 5000000_00000000L,
                    Symbol = symbol,
                    Memo = "issue token"
                });
                issueResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

                //query result
                var balance = ContractManager.Token.GetUserBalance(bp, symbol);
                Logger.Info($"Account: {bp}, {symbol}={balance}");
            }
        }

        [TestMethod]
        [DataRow("GetBalance", "CPU", 500_00000000L)]
        [DataRow("GetTokenInfo", "RAM", 600_00000000L)]
        [DataRow("GetMethodFee", "ELF", 50000000L)]
        public async Task SetTransactionFee_Test(string method, string symbol, long amount)
        {
            var transactionResult = ContractManager.Authority.ExecuteTransactionWithAuthority(
                ContractManager.Token.ContractAddress,
                nameof(ContractManager.TokenImplStub.SetMethodFee),
                new MethodFees
                {
                    MethodName = method,
                    Fees =
                    {
                        new MethodFee
                        {
                            Symbol = symbol,
                            BasicFee = amount
                        }
                    }
                },
                ContractManager.CallAddress);
            transactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            //query verification
            var transactionFee = await ContractManager.TokenImplStub.GetMethodFee.CallAsync(new StringValue
            {
                Value = method
            });
            transactionFee.Fees.First().ShouldBe(new MethodFee
            {
                Symbol = symbol,
                BasicFee = amount
            });
        }


        [TestMethod]
        public async Task SendTxForTransactionFee_CPU_Test()
        {
            var users = NodeInfoHelper.Config.Nodes.Select(o => o.Account);
            var txFee = 0L;
            foreach (var user in users)
            {
                var txResult = await ContractManager.TokenImplStub.GetBalance.SendAsync(new GetBalanceInput
                {
                    Symbol = "CPU",
                    Owner = user.ConvertAddress()
                });
                txResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
                txFee += txResult.TransactionResult.GetDefaultTransactionFee();
            }

            Logger.Info($"Total transaction fee: {txFee}");
        }

        [TestMethod]
        public async Task SendTxForTransactionFee_RAM_Test()
        {
            var symbols = new[] {"ELF", "CPU", "RAM", "DISK", "NET"};
            var txFee = 0L;
            foreach (var symbol in symbols)
            {
                var txResult = await ContractManager.TokenImplStub.GetTokenInfo.SendAsync(new GetTokenInfoInput
                {
                    Symbol = symbol
                });
                txResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
                txFee += txResult.TransactionResult.GetDefaultTransactionFee();
            }

            Logger.Info($"Total transaction fee: {txFee}");
        }

        [TestMethod]
        [DataRow("CPU")]
        public async Task BpUser_TakeProfit_Test(string symbol)
        {
            var bps = ContractManager.Authority.GetCurrentMiners();
            foreach (var bpUser in bps)
            {
                var beforeBalance = ContractManager.Token.GetUserBalance(bpUser);
                var profitDetails =
                    await ContractManager.ProfitStub.GetProfitDetails.CallAsync(new GetProfitDetailsInput
                    {
                        SchemeId = Schemes[SchemeType.MinerBasicReward].SchemeId,
                        Beneficiary = bpUser.ConvertAddress()
                    });
                if (profitDetails.Equals(new ProfitDetails())) continue;
                //take profit
                var profitAmount = await ContractManager.ProfitStub.GetProfitAmount.CallAsync(new GetProfitAmountInput
                {
                    SchemeId = Schemes[SchemeType.MinerBasicReward].SchemeId,
                    Beneficiary = bpUser.ConvertAddress()
                });
                Logger.Info($"{bpUser}: {symbol} = {profitAmount.Value}");
                if (profitAmount.Value > 0)
                {
                    var profitStub = ContractManager.Genesis.GetProfitStub(bpUser);
                    var claimResult = await profitStub.ClaimProfits.SendAsync(new ClaimProfitsInput
                    {
                        SchemeId = Schemes[SchemeType.MinerBasicReward].SchemeId,
                        Beneficiary = bpUser.ConvertAddress()
                    });
                    claimResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
                }

                //after balance
                var afterBalance = ContractManager.Token.GetUserBalance(bpUser, symbol);
                Logger.Info($"{bpUser} {symbol} before = {beforeBalance}, after = {afterBalance}");
            }
        }
    }
}