using System.Collections.Generic;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using log4net;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AElf.Automation.EconomicSystemTest
{
    public class TokenTests
    {
        protected readonly ILog _logger = Log4NetHelper.GetLogger();

        protected Behaviors Behaviors;
        protected static string RpcUrl { get; } = "http://192.168.197.70:8000";

        //protected RpcApiHelper NodeManager { get; set; }   
        protected INodeManager NodeManager { get; set; }
        protected string InitAccount { get; } = "2RCLmZQ2291xDwSbDEJR6nLhFJcMkyfrVTq1i1YxWC4SdY49a6";
        protected List<string> BpNodeAddress { get; set; }
        protected List<string> UserList { get; set; }
        protected List<string> IssuerList { get; set; }
        protected List<TokenInfo> TokenInfos { get; set; }


        protected void Initialize()
        {
            #region Get services

            NodeManager = new NodeManager(RpcUrl, CommonHelper.GetDefaultDataDir());
            var contractServices = new ContractManager(NodeManager, InitAccount);
            Behaviors = new Behaviors(contractServices);

            #endregion

            #region Basic Preparation

            Log4NetHelper.LogInit();

            //Get BpNode Info
            BpNodeAddress = new List<string>();
            BpNodeAddress.Add("2RCLmZQ2291xDwSbDEJR6nLhFJcMkyfrVTq1i1YxWC4SdY49a6"); //13-10

            //Generate 10 accounts to transfer
            PrepareUserAccount(7);

            #endregion
        }

        protected void TestCleanUp()
        {
//            if (UserList.Count == 0) return;
//            _logger.WriteInfo("Delete all account files created.");
//            foreach (var item in UserList)
//            {
//                var file = Path.Combine(AccountManager.GetDefaultDataDir(), $"{item}.json");
//                File.Delete(file);
//            }
        }

        protected void PrepareUserAccount(int accountNumber)
        {
            UserList = new List<string>();
            IssuerList = new List<string>();

            for (var i = 0; i < accountNumber - 2; i++)
            {
                var account = Behaviors.NodeManager.NewAccount();
                UserList.Add(account);
            }

            for (var i = 0; i < 2; i++)
            {
                var account = Behaviors.NodeManager.NewAccount();
                IssuerList.Add(account);
            }

            for (var i = 0; i < accountNumber - 2; i++)
            {
                var result = Behaviors.NodeManager.UnlockAccount(UserList[i]);
                Assert.IsTrue(result);
            }

            for (var i = 0; i < 2; i++)
            {
                var result = Behaviors.NodeManager.UnlockAccount(IssuerList[i]);
                Assert.IsTrue(result);
            }
        }


        protected class TokenInfo
        {
            public TokenInfo(string symbol, string tokenName, string issuer)
            {
                Symbol = symbol;
                TokenName = tokenName;
                Issuer = issuer;
            }

            public string Symbol { get; set; }
            public string TokenName { get; set; }
            public string Issuer { get; set; }
        }
    }
}