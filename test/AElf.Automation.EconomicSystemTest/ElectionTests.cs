using System;
using System.Collections.Generic;
using AElf.Contracts.MultiToken;
using AElf.Contracts.Profit;
using AElf.Types;
using AElfChain.Common;
using AElfChain.Common.Contracts;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using log4net;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AElf.Automation.EconomicSystemTest
{
    public class ElectionTests
    {
        protected static readonly ILog Logger = Log4NetHelper.GetLogger();

        protected Behaviors Behaviors;
        protected static string RpcUrl { get; } = "http://192.168.197.44:8000";
        protected INodeManager NodeManager { get; set; }
        protected AuthorityManager AuthorityManager { get; set; }
        protected string InitAccount { get; } = "28Y8JA1i2cN6oHvdv7EraXJr9a1gY6D1PpJXw9QtRMRwKcBQMK";
        protected List<string> BpNodeAddress { get; set; }
        protected List<string> FullNodeAddress { get; set; }
        protected List<string> ReplaceAddress { get; set; }
        protected List<string> Voter { get; set; }
        protected static List<string> UserList { get; set; }
        public Dictionary<SchemeType, Scheme> Schemes { get; set; }
        
        protected void Initialize()
        {
            //Init Logger
            Log4NetHelper.LogInit("ElectionTest");

            #region Get services

            NodeManager = new NodeManager(RpcUrl);
            var contractServices = new ContractManager(NodeManager, InitAccount);
            Behaviors = new Behaviors(contractServices,InitAccount);
            AuthorityManager = Behaviors.AuthorityManager;
            Schemes = ProfitContract.Schemes;
            #endregion

            #region Basic Preparation

            BpNodeAddress = new List<string>();
            BpNodeAddress.Add("28Y8JA1i2cN6oHvdv7EraXJr9a1gY6D1PpJXw9QtRMRwKcBQMK");
            BpNodeAddress.Add("2oSMWm1tjRqVdfmrdL8dgrRvhWu1FP8wcZidjS6wPbuoVtxhEz");
            BpNodeAddress.Add("WRy3ADLZ4bEQTn86ENi5GXi5J1YyHp9e99pPso84v2NJkfn5k");
            BpNodeAddress.Add("2frDVeV6VxUozNqcFbgoxruyqCRAuSyXyfCaov6bYWc7Gkxkh2");
//            BpNodeAddress.Add("2ZYyxEH6j8zAyJjef6Spa99Jx2zf5GbFktyAQEBPWLCvuSAn8D");
//            BpNodeAddress.Add("2RCLmZQ2291xDwSbDEJR6nLhFJcMkyfrVTq1i1YxWC4SdY49a6");
//            BpNodeAddress.Add("YF8o6ytMB7n5VF9d1RDioDXqyQ9EQjkFK3AwLPCH2b9LxdTEq");
//            BpNodeAddress.Add("h6CRCFAhyozJPwdFRd7i8A5zVAqy171AVty3uMQUQp1MB9AKa");

            FullNodeAddress = new List<string>();
//            FullNodeAddress.Add("2frDVeV6VxUozNqcFbgoxruyqCRAuSyXyfCaov6bYWc7Gkxkh2");
            FullNodeAddress.Add("2ZYyxEH6j8zAyJjef6Spa99Jx2zf5GbFktyAQEBPWLCvuSAn8D");
            FullNodeAddress.Add("eFU9Quc8BsztYpEHKzbNtUpu9hGKgwGD2tyL13MqtFkbnAoCZ");
            FullNodeAddress.Add("2V2UjHQGH8WT4TWnzebxnzo9uVboo67ZFbLjzJNTLrervAxnws");
            FullNodeAddress.Add("EKRtNn3WGvFSTDewFH81S7TisUzs9wPyP4gCwTww32waYWtLB");
            FullNodeAddress.Add("2LA8PSHTw4uub71jmS52WjydrMez4fGvDmBriWuDmNpZquwkNx");
            FullNodeAddress.Add("2RCLmZQ2291xDwSbDEJR6nLhFJcMkyfrVTq1i1YxWC4SdY49a6");
//            FullNodeAddress.Add("2Dyh4ASm6z7CaJ1J1WyvMPe2sJx5TMBW8CMTKeVoTMJ3ugQi3P");
//            FullNodeAddress.Add("YF8o6ytMB7n5VF9d1RDioDXqyQ9EQjkFK3AwLPCH2b9LxdTEq");
//            FullNodeAddress.Add("h6CRCFAhyozJPwdFRd7i8A5zVAqy171AVty3uMQUQp1MB9AKa");
//            FullNodeAddress.Add("28qLVdGMokanMAp9GwfEqiWnzzNifh8LS9as6mzJFX1gQBB823");

            ReplaceAddress = new List<string>();
            //node-1
            ReplaceAddress.Add("2cv45MBBUHjZqHva2JMfrGWiByyScNbEBjgwKoudWQzp6vX8QX");
//            ReplaceAddress.Add("2REajHMeW2DMrTdQWn89RQ26KQPRg91coCEtPP42EC9Cj7sZ61");
//            ReplaceAddress.Add("2G4L1S7KPfRscRP6zmd7AdVwtptVD3vR8YoF1ZHgPotDNbZnNY");
            //node-2
            ReplaceAddress.Add("91sb6pXzTP3JGAnYCM4jC6wh9UqyqGCQjwYmYEfiBh6s77c2y");

            //node-3
            ReplaceAddress.Add("69a7mLtphGDbyjABDn8Q8N37TiyRdqTZSP6uVMGtToaL4S7ji");

            //node-4
            ReplaceAddress.Add("QZjDh1KLNVkgGVdN2zTaeyY8fxEME2suRgNztwD6ioA65nx6Q");

            //node-6
            ReplaceAddress.Add("iTK4EBu5soTacHSAnFknv8q7Ujad89kZrJVJoKgVbQU6GnP23");

            //node-8
            ReplaceAddress.Add("W4xEKTZcvPKXRAmdu9xEpM69ArF7gUxDh9MDgtsKnu7JfePXo");
//            ReplaceAddress.Add("7BSmhiLtVqHSUVGuYdYbsfaZUGpkL2ingvCmVPx66UR5L5Lbs");
//            ReplaceAddress.Add("2mTDfKiuKFmNc7FzK2wqLkoZtJRM633KE3Yxq2RSb51Vvbsfec");

//            FullNodeAddress.Add("2Dyh4ASm6z7CaJ1J1WyvMPe2sJx5TMBW8CMTKeVoTMJ3ugQi3P");
//            FullNodeAddress.Add("2REajHMeW2DMrTdQWn89RQ26KQPRg91coCEtPP42EC9Cj7sZ61");
//            FullNodeAddress.Add("YF8o6ytMB7n5VF9d1RDioDXqyQ9EQjkFK3AwLPCH2b9LxdTEq");
//            FullNodeAddress.Add("h6CRCFAhyozJPwdFRd7i8A5zVAqy171AVty3uMQUQp1MB9AKa");
//            FullNodeAddress.Add("28qLVdGMokanMAp9GwfEqiWnzzNifh8LS9as6mzJFX1gQBB823");
//            FullNodeAddress.Add("2G4L1S7KPfRscRP6zmd7AdVwtptVD3vR8YoF1ZHgPotDNbZnNY");
//            FullNodeAddress.Add("W4xEKTZcvPKXRAmdu9xEpM69ArF7gUxDh9MDgtsKnu7JfePXo");
//            FullNodeAddress.Add("2a6MGBRVLPsy6pu4SVMWdQqHS5wvmkZv8oas9srGWHJk7GSJPV");
//            FullNodeAddress.Add("2cv45MBBUHjZqHva2JMfrGWiByyScNbEBjgwKoudWQzp6vX8QX");
//            FullNodeAddress.Add("7BSmhiLtVqHSUVGuYdYbsfaZUGpkL2ingvCmVPx66UR5L5Lbs");
//            
//            FullNodeAddress.Add("2mTDfKiuKFmNc7FzK2wqLkoZtJRM633KE3Yxq2RSb51Vvbsfec");
//            FullNodeAddress.Add("2gfVsyYbLPehmVjZxKHZfxp9AMRUEV6KFHkZDgdU7VZf64teew");
//            FullNodeAddress.Add("2bmNbLLR94QATnweMcVwLpUAPXt21x8k4zyMxQ7fXSySsrhUNm");

            Voter = new List<string>();
            Voter.Add("2gfVsyYbLPehmVjZxKHZfxp9AMRUEV6KFHkZDgdU7VZf64teew");
            Voter.Add("2a6MGBRVLPsy6pu4SVMWdQqHS5wvmkZv8oas9srGWHJk7GSJPV");
//            Voter.Add("h6CRCFAhyozJPwdFRd7i8A5zVAqy171AVty3uMQUQp1MB9AKa");
//            Voter.Add("28qLVdGMokanMAp9GwfEqiWnzzNifh8LS9as6mzJFX1gQBB823");
//            Voter.Add("2G4L1S7KPfRscRP6zmd7AdVwtptVD3vR8YoF1ZHgPotDNbZnNY");

            Logger.Info($"{NodeManager.ApiClient.BaseUrl}");

            #endregion
        }

        protected void TestCleanUp()
        {
            /*
            if (UserList.Count == 0) return;
            _logger.Info("Delete all account files created.");
            foreach (var item in UserList)
            {
                var file = Path.Combine(CommonHelper.GetCurrentDataDir(), $"{item}.json");
                File.Delete(file);
            }
            */
        }
    }
}