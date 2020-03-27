using System;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Contracts.Election;
using AElf.Contracts.MultiToken;
using AElf.Types;
using AElfChain.Common.Contracts;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using Google.Protobuf.WellKnownTypes;
using log4net;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Shouldly;

namespace AElf.Automation.EconomicSystemTest
{
    [TestClass]
    public class ElectionChangeTests
    {
        public string BpUser = "28Y8JA1i2cN6oHvdv7EraXJr9a1gY6D1PpJXw9QtRMRwKcBQMK";

        public string FullUser1 = "2ZYyxEH6j8zAyJjef6Spa99Jx2zf5GbFktyAQEBPWLCvuSAn8D";

        public string FullUser2 = "eFU9Quc8BsztYpEHKzbNtUpu9hGKgwGD2tyL13MqtFkbnAoCZ";

        public string FullUserPubKey1 =
            "04b6c07711bc30cdf98c9f081e70591f98f2ba7ff971e5a146d47009a754dacceb46813f92bc82c700971aa93945f726a96864a2aa36da4030f097f806b5abeca4";

        public string FullUserPubKey2 =
            "0433414d92934ef08a1b31ce2117dbb8be484657ec5e2fbd27058b3260587325e5f467778f22957af79af783306729fbaff0cd7f75eff61e5ce198531986d2af23";

        public string Tester = "2sYnepXzmsyxkoDZfAkJxDa8SpC5kgW5jiBNneNpVTb97anhSt";

        public string TesterPubkey =
            "04b93b99d44808ffce06e36fbcdd21f007e7b60999c954ebd7e3a9f7b52daf9fa2bd7110e9c8850c6c3ef0c4f6dfd431f9ed9254cc11f3b97cad6efaea283ef950";

        public ElectionChangeTests()
        {
            //Init Logger
            Log4NetHelper.LogInit("ElectionChangeTest");
            Logger = Log4NetHelper.GetLogger();

            const string endpoint = "http://192.168.197.40:8000";
            NodeManager = new NodeManager(endpoint);

            var genesis = NodeManager.GetGenesisContract();
            Token = genesis.GetTokenContract();
            TokenContractStub = genesis.GetTokenStub();
            Election = genesis.GetElectionContract();
            ElectionContractStub = genesis.GetElectionStub();
        }

        public ILog Logger { get; set; }
        public INodeManager NodeManager { get; set; }
        public TokenContract Token { get; set; }
        public TokenContractContainer.TokenContractStub TokenContractStub { get; set; }
        public ElectionContract Election { get; set; }
        public ElectionContractContainer.ElectionContractStub ElectionContractStub { get; set; }

        public AEDPoSContractContainer.AEDPoSContractStub AEDPoSContractStub { get; set; }

        [TestMethod]
        public void PrepareTokenForUser()
        {
            Token.TransferBalance(BpUser, FullUser1, 200000_00000000);
            Token.TransferBalance(BpUser, FullUser2, 200000_00000000);
            Token.TransferBalance(BpUser, Tester, 100000_00000000);

            Logger.Info($"FullUser1 balance: {Token.GetUserBalance(FullUser1)}");
            Logger.Info($"FullUser2 balance: {Token.GetUserBalance(FullUser2)}");
            Logger.Info($"Tester balance: {Token.GetUserBalance(Tester)}");
        }

        [TestMethod]
        public async Task AttendElection()
        {
            var genesis = NodeManager.GetGenesisContract();

            ElectionContractStub = genesis.GetElectionStub(FullUser1);
            await ElectionContractStub.AnnounceElection.SendAsync(new Empty());

            ElectionContractStub = genesis.GetElectionStub(FullUser2);
            await ElectionContractStub.AnnounceElection.SendAsync(new Empty());

            var candidates = await ElectionContractStub.GetCandidates.CallAsync(new Empty());
            Logger.Info(JsonConvert.SerializeObject(candidates, Formatting.Indented));
        }

        [TestMethod]
        [DataRow(500)]
        public async Task UserVote(long amount)
        {
            var genesis = NodeManager.GetGenesisContract();
            ElectionContractStub = genesis.GetElectionStub(Tester);
            await ElectionContractStub.Vote.SendAsync(new VoteMinerInput
            {
                Amount = amount,
                CandidatePubkey = FullUserPubKey1,
                EndTimestamp = DateTime.UtcNow.Add(TimeSpan.FromSeconds(600)).ToTimestamp()
            });

            var result = await ElectionContractStub.GetCandidateVoteWithRecords.CallAsync(new StringValue
            {
                Value = FullUserPubKey1
            });
            Logger.Info(JsonConvert.SerializeObject(result, Formatting.Indented));
        }

        [TestMethod]
        [DataRow("dd3493b88d87654ddcb3a16a1f2fa40a4d26fdb59e05276b2e225a51f97999a1")]
        public async Task ChangeUserVote(string hash)
        {
            var genesis = NodeManager.GetGenesisContract();
            ElectionContractStub = genesis.GetElectionStub(Tester);
            var voteId = HashHelper.HexStringToHash(hash);
            var transactionResult = await ElectionContractStub.ChangeVotingOption.SendAsync(new ChangeVotingOptionInput
            {
                CandidatePubkey = FullUserPubKey2,
                VoteId = voteId
            });
            transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var voteResult = await ElectionContractStub.GetCandidateVoteWithRecords.CallAsync(new StringValue
            {
                Value = FullUserPubKey2
            });
            voteResult.ObtainedActiveVotingRecords.Select(o => o.VoteId).ShouldContain(voteId);
        }

        [TestMethod]
        [DataRow("4c756bbc64046e8961965ec6479ae2417483aa778851ffd2cbf591f91c0e58aa")]
        public async Task WithdrawToken(string hash)
        {
            var genesis = NodeManager.GetGenesisContract();
            ElectionContractStub = genesis.GetElectionStub(Tester);
            var before = Token.GetUserBalance(Tester);
            var result = await ElectionContractStub.Withdraw.SendAsync(
                HashHelper.HexStringToHash(hash));
            var after = Token.GetUserBalance(Tester);
            Logger.Info($"Balance change: {before} => {after}");
        }

        [TestMethod]
        public async Task GetNextElectCountDown()
        {
            var result = await AEDPoSContractStub.GetNextElectCountDown.CallAsync(new Empty());
            Logger.Info(JsonConvert.SerializeObject(result, Formatting.Indented));
        }

        [TestMethod]
        public void GetUserPublicKey()
        {
            var publicKey = NodeManager.GetAccountPublicKey(Tester);
            Logger.Info($"PubKey: {publicKey}");
        }

        [TestMethod]
        public async Task TestChangeVoteInfo()
        {
            PrepareTokenForUser();
            await AttendElection();
            await UserVote(200);
            await UserVote(300);
        }
    }
}