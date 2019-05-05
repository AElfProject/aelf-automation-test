using System;
using System.Collections.Generic;
using System.IO;
using AElf.Automation.Common.Extensions;
using AElf.Automation.Common.Helpers;

namespace AElf.Automation.SideChainTests
{    
    public class SideChainTestBase
    {
        public ContractTester Tester;
        public readonly ILogHelper _logger = LogHelper.GetLogHelper();
        
        public static string RpcUrl { get; } = "http://192.168.197.70:8001/chain";       
 
        public RpcApiHelper CH { get; set; }        
        public string InitAccount { get; } = "2RCLmZQ2291xDwSbDEJR6nLhFJcMkyfrVTq1i1YxWC4SdY49a6";
        public static string SideAChainAccount { get; } = "";
        public static string SideBChainAccount { get; } = "";
        
        public List<string> BpNodeAddress { get; set; }        
        public List<string> UserList { get; set; }

        protected void Initialize()
        {
            CH = new RpcApiHelper(RpcUrl, AccountManager.GetDefaultDataDir());
            var contractServices = new ContractServices(CH,InitAccount);
            Tester = new ContractTester(contractServices);
            //Init Logger
            string logName = "ElectionTest_" + DateTime.Now.ToString("MMddHHmmss") + ".log";
            string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", logName);
            _logger.InitLogHelper(dir);
            //Get BpNode Info
            BpNodeAddress = new List<string>();
            BpNodeAddress.Add("28qLVdGMokanMAp9GwfEqiWnzzNifh8LS9as6mzJFX1gQBB823"); 
            BpNodeAddress.Add("2RCLmZQ2291xDwSbDEJR6nLhFJcMkyfrVTq1i1YxWC4SdY49a6");
            BpNodeAddress.Add("YF8o6ytMB7n5VF9d1RDioDXqyQ9EQjkFK3AwLPCH2b9LxdTEq");
            BpNodeAddress.Add("h6CRCFAhyozJPwdFRd7i8A5zVAqy171AVty3uMQUQp1MB9AKa");
        }
        
        protected void TestCleanUp()
        {
            if (UserList.Count == 0) return;
            _logger.WriteInfo("Delete all account files created.");
            foreach (var item in UserList)
            {
                var file = Path.Combine(AccountManager.GetDefaultDataDir(), $"{item}.ak");
                File.Delete(file);
            }
        }

     
    }
}