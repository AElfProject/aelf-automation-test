using System;
using System.Collections.Generic;
using System.IO;
using AElf.Automation.Common.Helpers;
using AElf.Automation.Common.OptionManagers;
using AElf.Types;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AElf.Automation.EconomicSystem.Tests
{
    [TestClass]
    public class QueryTests
    {
        private readonly ILogHelper _logger = LogHelper.GetLogHelper();
        private static string RpcUrl { get; } = "http://34.212.171.27:8000";
        private Behaviors Behaviors;
        private string InitAccount { get; } = "MEvVWBEQ6BTTCMCM2eoU4kVmaNGTapNxxqBtQqFVELHBBUNbc";
        private IApiHelper CH { get; set; }
        private Dictionary<Behaviors.ProfitType, Hash> ProfitItemsIds { get; set; }

        [TestInitialize]
        public void TestInitialize()
        {
            #region Get services

            CH = new WebApiHelper(RpcUrl, AccountManager.GetDefaultDataDir());
            var contractServices = new ContractServices(CH, InitAccount);
            Behaviors = new Behaviors(contractServices);

            var result = Behaviors.GetCreatedProfitItems();
            ProfitItemsIds = new Dictionary<Behaviors.ProfitType, Hash>
            {
                {Behaviors.ProfitType.Treasury, result.ProfitIds[0]},
                {Behaviors.ProfitType.MinerReward, result.ProfitIds[1]},
                {Behaviors.ProfitType.BackSubsidy, result.ProfitIds[2]},
                {Behaviors.ProfitType.CitizenWelfare, result.ProfitIds[3]},
                {Behaviors.ProfitType.BasicMinerReward, result.ProfitIds[4]},
                {Behaviors.ProfitType.VotesWeightReward, result.ProfitIds[5]},
                {Behaviors.ProfitType.ReElectionReward, result.ProfitIds[6]},
            };

            #endregion

            #region Basic Preparation

            //Init Logger
            string logName = "QueryTest_" + DateTime.Now.ToString("MMddHHmmss") + ".log";
            string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", logName);
            _logger.InitLogHelper(dir);

            #endregion
        }

        [TestMethod]
        public void GetCurrentMiners()
        {
            var miners = Behaviors.GetCurrentMiners();
            foreach (var publicKey in miners.PublicKeys)
            {
                _logger.WriteInfo($"Miner PublicKey: {publicKey.ToByteArray().ToHex()}");
            }
        }

        [TestMethod]
        public void GetAccountAndPublicKey()
        {
            var keyList = new List<string>
            {
                "28Y8JA1i2cN6oHvdv7EraXJr9a1gY6D1PpJXw9QtRMRwKcBQMK",
                "2oSMWm1tjRqVdfmrdL8dgrRvhWu1FP8wcZidjS6wPbuoVtxhEz",
                "WRy3ADLZ4bEQTn86ENi5GXi5J1YyHp9e99pPso84v2NJkfn5k",
                "2frDVeV6VxUozNqcFbgoxruyqCRAuSyXyfCaov6bYWc7Gkxkh2",
                "2ZYyxEH6j8zAyJjef6Spa99Jx2zf5GbFktyAQEBPWLCvuSAn8D",
                "eFU9Quc8BsztYpEHKzbNtUpu9hGKgwGD2tyL13MqtFkbnAoCZ",
                "2V2UjHQGH8WT4TWnzebxnzo9uVboo67ZFbLjzJNTLrervAxnws",
                "EKRtNn3WGvFSTDewFH81S7TisUzs9wPyP4gCwTww32waYWtLB",
                "2LA8PSHTw4uub71jmS52WjydrMez4fGvDmBriWuDmNpZquwkNx",
                "7BSmhiLtVqHSUVGuYdYbsfaZUGpkL2ingvCmVPx66UR5L5Lbs",
                "2RCLmZQ2291xDwSbDEJR6nLhFJcMkyfrVTq1i1YxWC4SdY49a6",
                "YF8o6ytMB7n5VF9d1RDioDXqyQ9EQjkFK3AwLPCH2b9LxdTEq",
                "h6CRCFAhyozJPwdFRd7i8A5zVAqy171AVty3uMQUQp1MB9AKa",
                "28qLVdGMokanMAp9GwfEqiWnzzNifh8LS9as6mzJFX1gQBB823",
                "2Dyh4ASm6z7CaJ1J1WyvMPe2sJx5TMBW8CMTKeVoTMJ3ugQi3P",
                "2G4L1S7KPfRscRP6zmd7AdVwtptVD3vR8YoF1ZHgPotDNbZnNY",
                "2jaRj5K8c1wWCBZav74t6nrB3TyA68JTmyinkpLHSF4Nxd9tU8",
                "2REajHMeW2DMrTdQWn89RQ26KQPRg91coCEtPP42EC9Cj7sZ61"
            };

            var count = 1;
            foreach (var key in keyList)
            {
                var publicKey = CH.GetPublicKeyFromAddress(key);
                _logger.WriteInfo($"{count:00} {key} {publicKey}");
                count++;
            }
        }
        
        [TestMethod]
        
    }
}