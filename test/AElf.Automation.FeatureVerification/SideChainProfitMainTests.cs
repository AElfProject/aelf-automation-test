using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.Election;
using AElf.Contracts.MultiToken;
using AElf.Contracts.TokenConverter;
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
using Shouldly;

namespace AElf.Automation.Contracts.ScenarioTest
{
    [TestClass]
    public class SideChainProfitMainTests
    {
        public SideChainProfitMainTests()
        {
            Log4NetHelper.LogInit("SideChainProfitMain");
            Logger = Log4NetHelper.GetLogger();

            NodeInfoHelper.SetConfig("nodes-env2-main");
            var node = NodeInfoHelper.Config.Nodes.First();

            NodeManager = new NodeManager(node.Endpoint);
            MainManager = new ContractManager(NodeManager, node.Account);
            MainManager.Profit.GetTreasurySchemes(MainManager.Treasury.ContractAddress);
        }

        private ILog Logger { get; }

        public INodeManager NodeManager { get; set; }
        public ContractManager MainManager { get; set; }

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
            const long VOTE = 200;
            var bps = NodeInfoHelper.Config.Nodes.Select(o => o.Account).Take(4);
            var candidates = await MainManager.ElectionStub.GetCandidates.CallAsync(new Empty());
            var candidatePubkeys = candidates.Value.Select(o => o.ToHex()).ToList();
            foreach (var bp in bps)
            {
                var beforeShare = MainManager.Token.GetUserBalance(bp, "SHARE");
                var electionStub = MainManager.Genesis.GetElectionStub(bp);
                var voteResult = await electionStub.Vote.SendAsync(new VoteMinerInput
                {
                    CandidatePubkey = candidatePubkeys[0],
                    Amount = VOTE,
                    EndTimestamp = KernelHelper.GetUtcNow().AddDays(120)
                });
                voteResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
                var afterShare = MainManager.Token.GetUserBalance(bp, "SHARE");
                afterShare.ShouldBe(beforeShare + VOTE);
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
    }
}