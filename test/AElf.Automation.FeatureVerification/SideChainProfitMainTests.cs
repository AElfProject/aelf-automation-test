using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Acs1;
using Acs10;
using AElf.Contracts.Election;
using AElf.Contracts.MultiToken;
using AElf.Contracts.TokenConverter;
using AElf.Contracts.Treasury;
using AElf.CSharp.Core.Extension;
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
using Volo.Abp.Threading;

namespace AElf.Automation.Contracts.ScenarioTest
{
    [TestClass]
    public class SideChainProfitMainTests
    {
        public SideChainProfitMainTests()
        {
            Log4NetHelper.LogInit("SideChainProfitMain");
            Logger = Log4NetHelper.GetLogger();

            NodeInfoHelper.SetConfig("nodes-env205-main");
            var node = NodeInfoHelper.Config.Nodes.First();

            NodeManager = new NodeManager(node.Endpoint);
            MainManager = new ContractManager(NodeManager, node.Account);
            MainManager.Profit.GetTreasurySchemes(MainManager.Treasury.ContractAddress);
            if (MainManager.Token.GetTokenInfo(Symbol).Equals(new TokenInfo()))
                AsyncHelper.RunSync(()=>CreateAndIssueAllNewSymbol(Symbol,true));
            if (MainManager.Token.GetTokenInfo(Symbol1).Equals(new TokenInfo()))
                AsyncHelper.RunSync(()=>CreateAndIssueAllNewSymbol(Symbol1,true));
            if (MainManager.Token.GetTokenInfo(Symbol2).Equals(new TokenInfo()))
                AsyncHelper.RunSync(()=>CreateAndIssueAllNewSymbol(Symbol2,false));
        }

        private ILog Logger { get; }

        public INodeManager NodeManager { get; set; }
        public ContractManager MainManager { get; set; }
        private string Symbol { get; } = "TEST";
        private string Symbol1 { get; } = "NOPROFIT";
        private string Symbol2 { get; } = "NOWHITE";
        
        [TestMethod]
        public async Task PrepareMainToken_Test()
        {
            var nodes = NodeInfoHelper.Config.Nodes.Select(o => o.Account);
            foreach (var nodeUser in nodes)
            {
                if (nodeUser == MainManager.CallAddress) continue;

                var transactionResult = await MainManager.TokenStub.Transfer.SendAsync(new TransferInput
                {
                    To = nodeUser.ConvertAddress(),
                    Amount = 200000_00000000L,
                    Symbol = "ELF",
                    Memo = "prepare test token"
                });
                transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            }
        }

        [TestMethod]
        [DataRow(5000_00000000L)]
        public async Task BuyResource_ForTxFee(long amount)
        {
            var symbols = new[] {"CPU", "RAM"};
            foreach (var symbol in symbols)
            {
                var beforeBalance = MainManager.Token.GetUserBalance(MainManager.CallAddress, symbol);
                var buyResult = await MainManager.TokenconverterStub.Buy.SendAsync(new BuyInput
                {
                    Symbol = symbol,
                    Amount = amount
                });
                buyResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
                var afterBalance = MainManager.Token.GetUserBalance(MainManager.CallAddress, symbol);
                afterBalance.ShouldBe(beforeBalance + amount);
            }
        }

        [TestMethod]
        public async Task NodeAttendElection_Test()
        {
            var nodes = NodeInfoHelper.Config.Nodes.Select(o => o.Account).ToList();
            var bps = MainManager.Authority.GetCurrentMiners();
            var nonBps = nodes.Where(o => !bps.Contains(o)).ToList();
            foreach (var node in nonBps)
            {
                var electionStub = MainManager.Genesis.GetElectionStub(node);
                var announcementResult = await electionStub.AnnounceElection.SendAsync(new Empty());
                announcementResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            }
        }

        [TestMethod]
        public async Task InitialMiners_VoteTest()
        {
            const long VOTE = 20000_00000000;
            var candidates = await MainManager.ElectionStub.GetCandidates.CallAsync(new Empty());
            var candidatePubkeys = candidates.Value.Select(o => o.ToHex()).ToList();
            var beforeShare = MainManager.Token.GetUserBalance(MainManager.CallAddress, "SHARE");
            var electionStub = MainManager.Genesis.GetElectionStub();
            var voteResult = await electionStub.Vote.SendAsync(new VoteMinerInput
            {
                CandidatePubkey = candidatePubkeys[0],
                Amount = VOTE,
                EndTimestamp = KernelHelper.GetUtcNow().AddDays(120)
            });
            voteResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var afterShare = MainManager.Token.GetUserBalance(MainManager.CallAddress, "SHARE");
            afterShare.ShouldBe(beforeShare + VOTE);
        }

        [TestMethod]
        public async Task Donate()
        {
            var bps = NodeInfoHelper.Config.Nodes.Select(o => o.Account).Take(4);
            var account = bps.First();
            var authority = new AuthorityManager(NodeManager);
            var treasuryStub = MainManager.Genesis.GetTreasuryStub(account);

            var treasuryBalance = await treasuryStub.GetUndistributedDividends.CallAsync(new Empty());
            Logger.Info(JsonConvert.SerializeObject(treasuryBalance));
            
            var symbolList = new List<string>(){Symbol,Symbol1,Symbol2};
            foreach (var symbol in symbolList)
            {
                MainManager.Token.ApproveToken(account, MainManager.Treasury.ContractAddress, 1000_00000000, symbol);
                var result = await treasuryStub.Donate.SendAsync(new DonateInput
                {
                    Symbol = symbol,
                    Amount = 1000_00000000
                });
                result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            }

            treasuryBalance = await treasuryStub.GetUndistributedDividends.CallAsync(new Empty());
            Logger.Info(JsonConvert.SerializeObject(treasuryBalance));
        }

        [TestMethod]
        public async Task GetUndistributedDividends()
        {
            var bps = NodeInfoHelper.Config.Nodes.Select(o => o.Account).Take(4);
            var account = bps.First();
            var treasuryStub = MainManager.Genesis.GetTreasuryStub(account);
            var treasuryBalance = await treasuryStub.GetUndistributedDividends.CallAsync(new Empty());
            Logger.Info(JsonConvert.SerializeObject(treasuryBalance));
        }

        [TestMethod]
        public async Task SetSymbolList()
        {
            var account = NodeInfoHelper.Config.Nodes.Select(o => o.Account).First();
            var authority = new AuthorityManager(NodeManager,account);
            var bps = authority.GetCurrentMiners();
            
            var treasuryStub = MainManager.Genesis.GetTreasuryStub(account);
            var symbolList = await treasuryStub.GetSymbolList.CallAsync(new Empty());
            Logger.Info(JsonConvert.SerializeObject(symbolList));

            var controller = await treasuryStub.GetTreasuryController.CallAsync(new Empty());
            var addSymbol = authority.ExecuteTransactionWithAuthority(MainManager.Treasury.ContractAddress,
                nameof(TreasuryContractContainer.TreasuryContractStub.SetSymbolList),
                new SymbolList {Value = {"ELF", Symbol}},
                bps.First(), controller.OwnerAddress);
            addSymbol.Status.ShouldBe(TransactionResultStatus.Mined);

            symbolList = await treasuryStub.GetSymbolList.CallAsync(new Empty());
            Logger.Info(JsonConvert.SerializeObject(symbolList));
        }
        
        [TestMethod]
        public async Task CheckSymbol()
        {
            var account = NodeInfoHelper.Config.Nodes.Select(o => o.Account).First();
            var treasuryStub = MainManager.Genesis.GetTreasuryStub(account);
            var symbolList = await treasuryStub.GetSymbolList.CallAsync(new Empty());
            Logger.Info(JsonConvert.SerializeObject(symbolList));
        }

        [TestMethod]
        public async Task ChangeFeeSymbol()
        {
            var fee = await MainManager.TokenImplStub.GetMethodFee.CallAsync(new StringValue {Value = "Approve"});
            Logger.Info(JsonConvert.SerializeObject(fee));
            var feeController = await MainManager.TokenImplStub.GetMethodFeeController.CallAsync(new Empty());
            var authority = new AuthorityManager(NodeManager);
            var input = new MethodFees
            {
                MethodName = nameof(TokenMethod.Approve),
                Fees =
                {
                    new MethodFee
                    {
                        BasicFee = 1000000000,
                        Symbol = Symbol
                    },
                    new MethodFee
                    {
                        BasicFee = 1000000000,
                        Symbol = "ELF"
                    }
                }
            };
            var addFeeSymbol = authority.ExecuteTransactionWithAuthority(MainManager.Token.ContractAddress,
                nameof(TokenContractImplContainer.TokenContractImplStub.SetMethodFee), input, MainManager.CallAddress,
                feeController.OwnerAddress);
            addFeeSymbol.Status.ShouldBe(TransactionResultStatus.Mined);

            fee = await MainManager.TokenImplStub.GetMethodFee.CallAsync(new StringValue {Value = "Approve"});
            Logger.Info(JsonConvert.SerializeObject(fee));
        }

        [TestMethod]
        public async Task CheckFeeSymbol()
        {
            var fee = await MainManager.TokenImplStub.GetMethodFee.CallAsync(new StringValue {Value = "Transfer"});
            Logger.Info(JsonConvert.SerializeObject(fee));
        }

        [TestMethod]
        public void GetBalance()
        {
            var authority = new AuthorityManager(NodeManager);
            var miners = authority.GetCurrentMiners();
            var symbolList = new List<string>() {Symbol, Symbol1, Symbol2};
            foreach (var miner in miners)
            foreach (var symbol in symbolList)
            {
                var balance = MainManager.Token.GetUserBalance(miner, symbol);
                Logger.Info($"{miner} {symbol} balance is {balance}");
            }
        }

        [TestMethod]
        public async Task Summary_Test()
        {
            await PrepareMainToken_Test();
            await BuyResource_ForTxFee(5000_00000000L);
            await NodeAttendElection_Test();
            await InitialMiners_VoteTest();
        }

        public async Task CreateAndIssueAllNewSymbol(string symbol, bool isAbleWhite)
        {
            var input = new CreateInput
            {
                TokenName = "TEST token",
                Symbol = symbol,
                Decimals = 10,
                TotalSupply = 100000000_0000000000,
                Issuer = MainManager.CallAccount,
                IsBurnable = true
            };
            if (isAbleWhite)
            {
                input.LockWhiteList.Add(MainManager.Treasury.Contract);
                input.LockWhiteList.Add(MainManager.TokenConverter.Contract);
            }
            var result = await MainManager.TokenStub.Create.SendAsync(input);

            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var issue = MainManager.Token.IssueBalance(MainManager.CallAddress, MainManager.CallAddress,
                100000000_0000000000, symbol);
            issue.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
        }
    }
}