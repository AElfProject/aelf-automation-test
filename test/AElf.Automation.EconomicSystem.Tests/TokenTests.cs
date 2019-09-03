using System.Collections.Generic;
using AElf.Automation.Common.Helpers;
using AElf.Automation.Common.OptionManagers;
using log4net;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AElf.Automation.EconomicSystem.Tests
{
    public class TokenTests
    {
        protected readonly ILog _logger = Log4NetHelper.GetLogger();
        protected static string RpcUrl { get; } = "http://192.168.197.70:8000";

        protected Behaviors Behaviors;

        //protected RpcApiHelper CH { get; set; }   
        protected IApiHelper CH { get; set; }
        protected string InitAccount { get; } = "2RCLmZQ2291xDwSbDEJR6nLhFJcMkyfrVTq1i1YxWC4SdY49a6";
        protected List<string> BpNodeAddress { get; set; }
        protected List<string> UserList { get; set; }
        protected List<string> IssuerList { get; set; }
        protected List<TokenInfo> TokenInfos { get; set; }


        protected class TokenInfo
        {
            public string Symbol { get; set; }
            public string TokenName { get; set; }
            public string Issuer { get; set; }

            public TokenInfo(string symbol, string tokenName, string issuer)
            {
                Symbol = symbol;
                TokenName = tokenName;
                Issuer = issuer;
            }
        }


        protected void Initialize()
        {
            #region Get services

            CH = new WebApiHelper(RpcUrl, CommonHelper.GetDefaultDataDir());
            var contractServices = new ContractServices(CH, InitAccount);
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
                var ci = new CommandInfo(ApiMethods.AccountNew) {Parameter = Account.DefaultPassword};
                ci = Behaviors.ApiHelper.ExecuteCommand(ci);
                Assert.IsTrue(ci.Result);
                var account = ci.InfoMsg.ToString();
                UserList.Add(account);
            }

            for (var i = 0; i < 2; i++)
            {
                var ci = new CommandInfo(ApiMethods.AccountNew) {Parameter = Account.DefaultPassword};
                ci = Behaviors.ApiHelper.ExecuteCommand(ci);
                Assert.IsTrue(ci.Result);
                var account = ci.InfoMsg.ToString();
                IssuerList.Add(account);
            }


            for (var i = 0; i < accountNumber - 2; i++)
            {
                var ci = new CommandInfo(ApiMethods.AccountUnlock)
                {
                    Parameter = $"{UserList[i]} 123 notimeout"
                };
                ci = Behaviors.ApiHelper.ExecuteCommand(ci);
                Assert.IsTrue(ci.Result);
            }

            for (var i = 0; i < 2; i++)
            {
                var ci = new CommandInfo(ApiMethods.AccountUnlock)
                {
                    Parameter = $"{IssuerList[i]} 123 notimeout"
                };
                ci = Behaviors.ApiHelper.ExecuteCommand(ci);
                Assert.IsTrue(ci.Result);
            }
        }
    }
}