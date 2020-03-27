using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Contracts.Election;
using AElf.Contracts.MultiToken;
using AElfChain.Common.Contracts;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using Google.Protobuf.WellKnownTypes;
using log4net;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace AElf.Automation.EconomicSystemTest
{
    [TestClass]
    public class Election_EvilTests
    {
        public string BpUser = "28Y8JA1i2cN6oHvdv7EraXJr9a1gY6D1PpJXw9QtRMRwKcBQMK";

        //Get FullNode Info
        public List<string> FullNodeAddress = new List<string>
        {
            //"2ZYyxEH6j8zAyJjef6Spa99Jx2zf5GbFktyAQEBPWLCvuSAn8D",
            //"eFU9Quc8BsztYpEHKzbNtUpu9hGKgwGD2tyL13MqtFkbnAoCZ",
            //"2V2UjHQGH8WT4TWnzebxnzo9uVboo67ZFbLjzJNTLrervAxnws",
            "EKRtNn3WGvFSTDewFH81S7TisUzs9wPyP4gCwTww32waYWtLB",
            "2LA8PSHTw4uub71jmS52WjydrMez4fGvDmBriWuDmNpZquwkNx",
            "2RCLmZQ2291xDwSbDEJR6nLhFJcMkyfrVTq1i1YxWC4SdY49a6"
        };

        public string Tester = "2sYnepXzmsyxkoDZfAkJxDa8SpC5kgW5jiBNneNpVTb97anhSt";

        public string TesterPubkey =
            "04b93b99d44808ffce06e36fbcdd21f007e7b60999c954ebd7e3a9f7b52daf9fa2bd7110e9c8850c6c3ef0c4f6dfd431f9ed9254cc11f3b97cad6efaea283ef950";

        public Election_EvilTests()
        {
            //Init Logger
            Log4NetHelper.LogInit("ElectionChangeTest");
            Logger = Log4NetHelper.GetLogger();

            const string endpoint = "http://192.168.197.40:8000";
            NodeManager = new NodeManager(endpoint);

            var genesis = NodeManager.GetGenesisContract(BpUser);
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
        public void PrepareFullNodeToken()
        {
            foreach (var fullUser in FullNodeAddress)
            {
                var token = Token.GetUserBalance(fullUser);
                if (token > 100000_00000000)
                    continue;
                Token.TransferBalance(BpUser, fullUser, 150000_00000000);
            }

            Token.TransferBalance(BpUser, Tester, 50000_00000000);
        }

        [TestMethod]
        public async Task FullNodeJoinElections()
        {
            var pubkeyList = await ElectionContractStub.GetCandidates.CallAsync(new Empty());
            var candidates = pubkeyList.Value.Select(o => o.ToHex()).ToList();
            foreach (var fullUser in FullNodeAddress)
            {
                Election.SetAccount(fullUser);
                var pubKey = NodeManager.GetAccountPublicKey(fullUser);
                if (candidates.Contains(pubKey))
                    continue;

                Election.ExecuteMethodWithResult(ElectionMethod.AnnounceElection, new Empty());
            }

            pubkeyList = await ElectionContractStub.GetCandidates.CallAsync(new Empty());
            Logger.Info($"Candidates count: {pubkeyList.Value.Count}");
        }

        [TestMethod]
        public void UserVote()
        {
            Election.SetAccount(Tester);
            var beforeToken = Token.GetUserBalance(Tester);
            var initialBalance = 1000;
            var lockTime = 90;
            foreach (var fullUser in FullNodeAddress)
            {
                var pubKey = NodeManager.GetAccountPublicKey(fullUser);
                Election.ExecuteMethodWithResult(ElectionMethod.Vote, new VoteMinerInput
                {
                    Amount = initialBalance,
                    CandidatePubkey = pubKey,
                    EndTimestamp = DateTime.UtcNow.Add(TimeSpan.FromDays(lockTime)).ToTimestamp()
                });
                initialBalance -= 100;
            }

            var afterToken = Token.GetUserBalance(Tester);
            Logger.Info($"User token info: before={beforeToken}, after={afterToken}");
        }

        [TestMethod]
        public void AddressConvert()
        {
            var address = Tester.ConvertAddress();
            var info = address.ToString();
            var jsonInfo = JsonConvert.SerializeObject(address);
        }

        [TestMethod]
        public void GetFullNodePubkey()
        {
            foreach (var fullUser in FullNodeAddress)
            {
                var pubKey = NodeManager.GetAccountPublicKey(fullUser);
                Logger.Info($"Account: {fullUser}");
                Logger.Info($"PublicKey: {pubKey}");
            }
        }
    }
}