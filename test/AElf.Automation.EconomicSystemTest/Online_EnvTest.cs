using System;
using System.IO;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AElf.Automation.EconomicSystemTest
{
    [TestClass]
    public class Online_EnvTest
    {
        protected readonly ILogHelper _logger = LogHelper.GetLogger();
        protected Behaviors Behaviors;
        protected static string RpcUrl { get; } = "http://3.94.106.10:8000";
        protected string InitAccount { get; } = "1DBGP5qXt5r6QAu2iufv4eXodWHYqJVwmz4qNHwtNyjuCoDEm";
        protected INodeManager NodeManager { get; set; }
        public string Bp0 { get; set; } = "28Y8JA1i2cN6oHvdv7EraXJr9a1gY6D1PpJXw9QtRMRwKcBQMK";
        public string Full1 { get; set; } = "2a6MGBRVLPsy6pu4SVMWdQqHS5wvmkZv8oas9srGWHJk7GSJPV";
        public string Full2 { get; set; } = "2cv45MBBUHjZqHva2JMfrGWiByyScNbEBjgwKoudWQzp6vX8QX";

        [TestInitialize]
        public void TestInitialize()
        {
            //Init Logger
            var logName = "ElectionTest_" + DateTime.Now.ToString("MMddHHmmss") + ".log";
            var dir = Path.Combine(CommonHelper.AppRoot, "logs", logName);
            _logger.InitLogHelper(dir);

            #region Get services

            NodeManager = new NodeManager(RpcUrl);
            var contractServices = new ContractManager(NodeManager, InitAccount);
            Behaviors = new Behaviors(contractServices);

            #endregion
        }

        [TestMethod]
        public void AttendElectionAndVote()
        {
            //Transfer
            Behaviors.TransferToken(Bp0, Full1, 10_0000);
            Behaviors.TransferToken(Bp0, Full2, 10_0000);
            Behaviors.TransferToken(Bp0, InitAccount, 100_0000);

            //Election
            Behaviors.AnnouncementElection(Full1);
            Behaviors.AnnouncementElection(Full2);

            var candidates = Behaviors.GetCandidates();
            _logger.Info($"Candidate count: {candidates.Value.Count}");
            foreach (var candidate in candidates.Value) _logger.Info($"Candidate: {candidate.ToByteArray().ToHex()}");

            //Vote
            Behaviors.UserVote(InitAccount, Full1, 120, 50_0000);
            Behaviors.UserVote(InitAccount, Full2, 120, 50_0000);
        }

        [TestMethod]
        public void QuitElection()
        {
            Behaviors.QuitElection("7BSmhiLtVqHSUVGuYdYbsfaZUGpkL2ingvCmVPx66UR5L5Lbs");
            Behaviors.QuitElection("2jaRj5K8c1wWCBZav74t6nrB3TyA68JTmyinkpLHSF4Nxd9tU8");
        }

        [TestMethod]
        public void GetAllCandidatesVoteInfo()
        {
            var candidates = Behaviors.GetCandidates();
            foreach (var candidate in candidates.Value)
            {
                var publicKey = candidate.ToByteArray().ToHex();
                var voteInfo = Behaviors.GetCandidateVote(publicKey);
                _logger.Info(voteInfo.ToString());
            }
        }

        [TestMethod]
        public void GetCurrentMiners()
        {
            var minerList = Behaviors.GetCurrentMiners();
            _logger.Info("Current miners:");
            foreach (var pubkey in minerList.Pubkeys) _logger.Info(pubkey.ToHex());
        }

        [TestMethod]
        public void GetCandidates()
        {
            var candidates = Behaviors.GetCandidates();
            _logger.Info("Current candidates:");
            foreach (var pubkey in candidates.Value) _logger.Info(pubkey.ToHex());
        }
    }
}