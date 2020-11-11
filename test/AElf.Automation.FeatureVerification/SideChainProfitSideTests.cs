using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Acs10;
using AElf.Contracts.MultiToken;
using AElf.Contracts.Profit;
using AElf.Contracts.TokenHolder;
using AElf.Types;
using AElfChain.Common;
using AElfChain.Common.Contracts;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using Google.Protobuf.WellKnownTypes;
using log4net;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Shouldly;
using ClaimProfitsInput = AElf.Contracts.TokenHolder.ClaimProfitsInput;
using ReceivedProfitsMap = AElf.Contracts.TokenHolder.ReceivedProfitsMap;

namespace AElf.Automation.Contracts.ScenarioTest
{
    [TestClass]
    public class SideChainProfitSideTests
    {
        public SideChainProfitSideTests()
        {
            Log4NetHelper.LogInit("SideChainProfitSide");
            Logger = Log4NetHelper.GetLogger();

            NodeInfoHelper.SetConfig("nodes-env2-side2");
            var node = NodeInfoHelper.Config.Nodes.First();

            NodeManager = new NodeManager(node.Endpoint);
            SideManager = new ContractManager(NodeManager, node.Account);
            AuthorityManager = new AuthorityManager(NodeManager);
        }

        private ILog Logger { get; }

        public INodeManager NodeManager { get; set; }
        public ContractManager SideManager { get; set; }
        public AuthorityManager AuthorityManager { get; set; }

        private readonly List<string> ResourceSymbol = new List<string>
            {"CPU", "NET", "DISK", "RAM", "READ", "WRITE", "STORAGE", "TRAFFIC"};


        [TestMethod]
        public void Prepare_TestToken()
        {
            const long BALANCE = 1000L;
            var symbols = new[] {"SHARE"};
            var bps = NodeInfoHelper.Config.Nodes;
            foreach (var symbol in symbols)
            foreach (var bp in bps)
            {
                if (bp.Account.Equals(SideManager.CallAddress)) continue;
                SideManager.Token.TransferBalance(SideManager.CallAddress, bp.Account, BALANCE, symbol);
            }

            foreach (var symbol in symbols)
            foreach (var bp in bps)
            {
                var balance = SideManager.Token.GetUserBalance(bp.Account, symbol);
                Logger.Info($"{bp.Account} {symbol}={balance}");
            }
        }

        [TestMethod]
        public async Task RegisterManyAccount()
        {
            var bps = NodeInfoHelper.Config.Nodes.Take(4);
            var i = 1;
           foreach (var bp in bps)
            {
                if(bp.Account.Equals(SideManager.CallAddress)) continue;
                var amount = 100 * i;
                await Register_Mortgage_Test(bp.Account, amount);
                i++;
            }
        }

        [TestMethod]
        public async Task Register_Mortgage_Test(string account, long amount)
        {
            var beforeBalance = SideManager.Token.GetUserBalance(account, "SHARE");
            var stbBalance = SideManager.Token.GetUserBalance(account, "STB");
            if (beforeBalance < amount)
            {
                SideManager.Token.TransferBalance(SideManager.CallAddress, account, amount, "SHARE");
                beforeBalance = SideManager.Token.GetUserBalance(account, "SHARE");
            }

            if (stbBalance < 100_00000000)
                SideManager.Token.TransferBalance(SideManager.CallAddress, account, 1000_00000000, "STB");
            var stub = SideManager.Genesis.GetTokenImplStub(account);
            var approveResult = await stub.Approve.SendAsync(new ApproveInput
            {
                Spender = SideManager.TokenHolder.Contract,
                Amount = amount,
                Symbol = "SHARE"
            });
            approveResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var holder = SideManager.Genesis.GetTokenHolderStub(account);
            var registerResult =
                await holder.RegisterForProfits.SendAsync(new RegisterForProfitsInput
                {
                    SchemeManager = SideManager.Consensus.Contract,
                    Amount = amount
                });
            registerResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined,
                registerResult.TransactionResult.Error);

            var afterBalance = SideManager.Token.GetUserBalance(account, "SHARE");
            afterBalance.ShouldBe(beforeBalance - amount);
        }

        [TestMethod]
        public async Task SetFeeReceiver()
        {
            var nodes = NodeInfoHelper.Config.Nodes;
            var symbol = SideManager.Token.GetPrimaryTokenSymbol();
            foreach (var node in nodes)
            {
                var balance = SideManager.Token.GetUserBalance(node.Account, symbol);
                if (balance > 1000_00000000) continue;
                SideManager.Token.TransferBalance(nodes.First().Account, node.Account, 10000_00000000, symbol);
            }

            var getReceiver = await SideManager.TokenImplStub.GetFeeReceiver.CallAsync(new Empty());
            if (!getReceiver.Equals(new Address()))
            {
                Logger.Info($"FeeReceiver: {getReceiver}");
                return;
            }
            var associationOrganization =
                AuthorityManager.CreateAssociationOrganization(nodes.TakeLast(5).Select(a => a.Account).ToList());
            var setReceiver = await SideManager.TokenImplStub.SetFeeReceiver.SendAsync(associationOrganization);
            setReceiver.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            getReceiver = await SideManager.TokenImplStub.GetFeeReceiver.CallAsync(new Empty());
            getReceiver.ShouldBe(associationOrganization);
            Logger.Info($"FeeReceiver: {getReceiver}");
        }

        [TestMethod]
        public async Task GetReceiver()
        {
            var getReceiver = await SideManager.TokenImplStub.GetFeeReceiver.CallAsync(new Empty());
            Logger.Info($"{getReceiver}");
        }

        [TestMethod]
        public void GetBalance()
        {
            var receiver = "a1vLKESfgbqfnAX2Ej6b1kfuFkduB6AkN1Q5XnnhhusDUM6Tf";
            var symbol = SideManager.Token.GetPrimaryTokenSymbol();
            var balance = SideManager.Token.GetUserBalance(receiver, symbol);
            var elfBalance = SideManager.Token.GetUserBalance(receiver);
            Logger.Info($"{balance} {elfBalance}");
            var bps = NodeInfoHelper.Config.Nodes;
            var symbols = new[]
                {"SHARE", "TEST", "CPU", "NET", "DISK", "RAM", "READ", "WRITE", "STORAGE", "TRAFFIC", "ELF"};
            foreach (var s in symbols)
            foreach (var bp in bps)
            {
                var accountBalance = SideManager.Token.GetUserBalance(bp.Account, s);
                Logger.Info($"{bp.Account} {s} balance is {accountBalance}");
            }
        }

        [TestMethod]
        public async Task Withdraw()
        {
            var account = "eFU9Quc8BsztYpEHKzbNtUpu9hGKgwGD2tyL13MqtFkbnAoCZ";
            var beforeBalance = SideManager.Token.GetUserBalance(account, "SHARE");
            var stub = SideManager.Genesis.GetTokenHolderStub(account);
            var result = await stub.Withdraw.SendAsync(SideManager.Consensus.Contract);
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var afterBalance = SideManager.Token.GetUserBalance(account, "SHARE");
            afterBalance.ShouldBe(beforeBalance + 100);
        }

        [TestMethod]
        [DataRow("ELF", 100_00000000L)]
        public async Task Donate_SideChainDividendsPool_Test(string symbol, long amount)
        {
            var init = NodeInfoHelper.Config.Nodes[0].Account;
            var consensusStub = SideManager.Genesis.GetConsensusImplStub(init);
            var tokenStub = SideManager.Genesis.GetTokenImplStub(init);
            var approveResult = await tokenStub.Approve.SendAsync(new ApproveInput
            {
                Spender = SideManager.Consensus.Contract,
                Amount = amount,
                Symbol = symbol
            });
            approveResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var balance = await tokenStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = init.ConvertAddress(),
                Symbol = symbol
            });
            Logger.Info($"Account: {init}, {symbol}={balance}");

            var allowance = await tokenStub.GetAllowance.CallAsync(new GetAllowanceInput
            {
                Owner = init.ConvertAddress(),
                Spender = SideManager.Consensus.Contract,
                Symbol = symbol
            });
            Logger.Info($"Account: {init}, {symbol}={allowance}");

            var contributeResult =
                await consensusStub.Donate.SendAsync(
                    new DonateInput
                    {
                        Symbol = symbol,
                        Amount = amount
                    });
            contributeResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var check = await consensusStub.GetSymbolList.CallAsync(new Empty());
            var unAmount = await consensusStub.GetUndistributedDividends.CallAsync(new Empty());
            Logger.Info($"Symbol list : {check}\n amount:{unAmount}");
        }

        [TestMethod]
        public async Task Query_User_ProfitSymbol_Test()
        {
            var symbols = new[] {"READ", "WRITE", "STORAGE", "TRAFFIC", "ELF", "STB", "CPU", "RAM","TEST"};
            var bps = NodeInfoHelper.Config.Nodes;
            foreach (var bp in bps)
            foreach (var symbol in symbols)
            {
                var balance = SideManager.Token.GetUserBalance(bp.Account, symbol);
                Logger.Info($"{bp.Account} balance info: {symbol}={balance}");
            }

            var consensusStub = SideManager.Genesis.GetConsensusImplStub(bps.First().Account);
            var check = await consensusStub.GetSymbolList.CallAsync(new Empty());
            var amount = await consensusStub.GetUndistributedDividends.CallAsync(new Empty());
            Logger.Info($"Symbol list : {check}\n amount:{amount}");
        }

        [TestMethod]
        public async Task ClaimProfit_Test()
        {
            var bps = NodeInfoHelper.Config.Nodes;
            var symbols = new[] {"CPU","RAM","READ"};
            foreach (var bp in bps)
            {
                var holder = SideManager.Genesis.GetTokenHolderStub(bp.Account);
                var profitMap = await holder.GetProfitsMap.CallAsync(new ClaimProfitsInput
                {
                    SchemeManager = SideManager.Consensus.Contract,
                    Beneficiary = bp.Account.ConvertAddress()
                });
                Logger.Info($"{bp.Account}:{JsonConvert.SerializeObject(profitMap)}");

                if (profitMap.Equals(new ReceivedProfitsMap())) continue;
                
                var balanceList = new Dictionary<string,long>();
                foreach (var symbol in symbols)
                {
                    var beforeBalance = SideManager.Token.GetUserBalance(bp.Account, symbol);
                    balanceList.Add(symbol,beforeBalance);
                }

                var claimResult = await holder.ClaimProfits.SendAsync(new ClaimProfitsInput
                {
                    SchemeManager = SideManager.Consensus.Contract,
                    Beneficiary = bp.Account.ConvertAddress()
                });
                claimResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
                
                foreach (var symbol in symbols)
                {
                    var afterBalance = SideManager.Token.GetUserBalance(bp.Account, symbol);
                    var beforeBalance = balanceList.Where(b => b.Key.Equals(symbol)).ToList().First().Value;
                    Logger.Info($"Profit balance: {afterBalance - beforeBalance} {symbol}");
                }
            }
        }

        [TestMethod]
        public async Task CheckProfit()
        {
            var bps = NodeInfoHelper.Config.Nodes;
            foreach (var bp in bps)
            {
                var holder = SideManager.Genesis.GetTokenHolderStub(bp.Account);
                var profitMap = await holder.GetProfitsMap.CallAsync(new ClaimProfitsInput
                {
                    SchemeManager = SideManager.Consensus.Contract,
                    Beneficiary = bp.Account.ConvertAddress()
                });
                Logger.Info($"{bp.Account}:{JsonConvert.SerializeObject(profitMap)}");
            }
        }

        [TestMethod]
        public async Task Summary_Case()
        {
            Prepare_TestToken();
            await RegisterManyAccount();
            await Donate_SideChainDividendsPool_Test("ELF", 500_00000000L);
            await Donate_SideChainDividendsPool_Test("RAM", 500_00000000L);
            await Donate_SideChainDividendsPool_Test("CPU", 500_00000000L);
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
            Logger.Info(schemeInfo);

            var profit = await SideManager.ProfitStub.GetDistributedProfitsInfo.CallAsync(new SchemePeriod
            {
                Period = schemeInfo.CurrentPeriod,
                SchemeId = scheme.SchemeId
            });
            Logger.Info(profit);

            foreach (var symbol in ResourceSymbol)
            {
                var balance = SideManager.Token.GetUserBalance(SideManager.Consensus.ContractAddress, symbol);
                Logger.Info($"{SideManager.Consensus.ContractAddress}: {symbol}: {balance}");
            }

            var init = NodeInfoHelper.Config.Nodes[0].Account;
            var consensusStub = SideManager.Genesis.GetConsensusImplStub(init);
            var check = await consensusStub.GetSymbolList.CallAsync(new Empty());
            var unAmount = await consensusStub.GetUndistributedDividends.CallAsync(new Empty());
            Logger.Info($"Symbol list : {check}\n amount:{unAmount}");
        }
    }
}