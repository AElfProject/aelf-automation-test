using System;
using System.Threading.Tasks;
using AElf.Automation.Common.Contracts;
using AElf.Automation.Common.Helpers;
using AElf.Automation.Common.Managers;
using AElf.Contracts.Election;
using AElf.Contracts.MultiToken;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using log4net;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace AElf.Automation.EconomicSystem.Tests
{
    [TestClass]
    public class ElectionChangeTests
    {
        public ILog Logger { get; set; } 
        public INodeManager NodeManager { get; set; }
        public TokenContract Token { get; set; }
        public TokenContractContainer.TokenContractStub TokenContractStub { get; set; }
        public ElectionContract Election { get; set; }
        public ElectionContractContainer.ElectionContractStub ElectionContractStub { get; set; }

        public string Tester = "2sYnepXzmsyxkoDZfAkJxDa8SpC5kgW5jiBNneNpVTb97anhSt";

        public string FullUser1 = "2ZYyxEH6j8zAyJjef6Spa99Jx2zf5GbFktyAQEBPWLCvuSAn8D";
        public string FullUserPubKey1 = "04b6c07711bc30cdf98c9f081e70591f98f2ba7ff971e5a146d47009a754dacceb46813f92bc82c700971aa93945f726a96864a2aa36da4030f097f806b5abeca4";

        public string FullUser2 = "eFU9Quc8BsztYpEHKzbNtUpu9hGKgwGD2tyL13MqtFkbnAoCZ";
        public string FullUserPubKey2 = "0433414d92934ef08a1b31ce2117dbb8be484657ec5e2fbd27058b3260587325e5f467778f22957af79af783306729fbaff0cd7f75eff61e5ce198531986d2af23";
        
        public string BpUser = "28Y8JA1i2cN6oHvdv7EraXJr9a1gY6D1PpJXw9QtRMRwKcBQMK";
        
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
        [DataRow(200)]
        public async Task UserVote(long amount)
        {
            var genesis = NodeManager.GetGenesisContract();
            ElectionContractStub = genesis.GetElectionStub(Tester);
            await ElectionContractStub.Vote.SendAsync(new VoteMinerInput
            {
                Amount = amount,
                CandidatePubkey = FullUserPubKey1,
                EndTimestamp = DateTime.UtcNow.Add(TimeSpan.FromSeconds(180)).ToTimestamp()
            });

            var result = await ElectionContractStub.GetCandidateVoteWithRecords.CallAsync(new StringInput
            {
                Value = FullUserPubKey1
            });
            Logger.Info(JsonConvert.SerializeObject(result, Formatting.Indented));
        }

        [TestMethod]
        [DataRow("e3a84ab3e433f4a43a51f98927c82e8ff2d10987ddf7e3703d15bca39a3b2c97")]
        public async Task ChangeUserVote(string hash)
        {
            var genesis = NodeManager.GetGenesisContract();
            ElectionContractStub = genesis.GetElectionStub(Tester);
            await ElectionContractStub.ChangeVotingOption.SendAsync(new ChangeVotingOptionInput
            {
                CandidatePubkey = FullUserPubKey2,
                VoteId = HashHelper.HexStringToHash(hash)
            });
        }

        [TestMethod]
        [DataRow("e3a84ab3e433f4a43a51f98927c82e8ff2d10987ddf7e3703d15bca39a3b2c97")]
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
            var result = await ElectionContractStub.GetNextElectCountDown.CallAsync(new Empty());
            Logger.Info(JsonConvert.SerializeObject(result, Formatting.Indented));
        }
        
        [TestMethod]
        public async Task GetUSerPublicKey()
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