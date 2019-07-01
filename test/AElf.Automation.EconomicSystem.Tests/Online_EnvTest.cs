using System;
using System.IO;
using AElf.Automation.Common.Contracts;
using AElf.Automation.Common.Helpers;
using AElf.Automation.Common.OptionManagers;
using AElf.Contracts.MultiToken.Messages;
using AElf.Types;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AElf.Automation.EconomicSystem.Tests
{
    [TestClass]
    public class Online_EnvTest
    {
        protected readonly ILogHelper _logger = LogHelper.GetLogHelper();
        protected static string RpcUrl { get; } = "http://3.1.220.141:8000";
        protected Behaviors Behaviors;
        protected string InitAccount { get; } = "2876Vk2deM5ZnaXr1Ns9eySMSjpuvd53XatHTc37JXeW6HjiPs";
        protected IApiHelper CH { get; set; }
        public string Bp0 { get; set; } = "28Y8JA1i2cN6oHvdv7EraXJr9a1gY6D1PpJXw9QtRMRwKcBQMK";
        public string Full1 { get; set; } = "2a6MGBRVLPsy6pu4SVMWdQqHS5wvmkZv8oas9srGWHJk7GSJPV";
        public string Full2 { get; set; } = "2cv45MBBUHjZqHva2JMfrGWiByyScNbEBjgwKoudWQzp6vX8QX";
        
        [TestInitialize]
        public void TestInitialize()
        {
            //Init Logger
            string logName = "ElectionTest_" + DateTime.Now.ToString("MMddHHmmss") + ".log";
            string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", logName);
            _logger.InitLogHelper(dir);
            
            #region Get services
            CH = new WebApiHelper(RpcUrl, AccountManager.GetDefaultDataDir());
            var contractServices = new ContractServices(CH, InitAccount);
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
            _logger.WriteInfo($"Candidate count: {candidates.Value.Count}");
            foreach (var candidate in candidates.Value)
            {
                _logger.WriteInfo($"Candidate: {candidate.ToByteArray().ToHex()}");
            }
            
            //Vote
            Behaviors.UserVote(InitAccount, Full1, 120, 50_0000);
            Behaviors.UserVote(InitAccount, Full2, 120, 50_0000);
        }

        [TestMethod]
        public void GetAllCandidatesVoteInfo()
        {
            var candidates = Behaviors.GetCandidates();
            foreach (var candidate in candidates.Value)
            {
                var publicKey = candidate.ToByteArray().ToHex();
                var voteInfo = Behaviors.GetCandidateVote(publicKey);
                _logger.WriteInfo(voteInfo.ToString());
            }
        }
    }
}