using System;
using System.Collections.Generic;
using System.IO;
using AElf.Automation.Common.Contracts;
using AElf.Automation.Common.Extensions;
using AElf.Automation.Common.Helpers;
using AElf.Contracts.MultiToken.Messages;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AElf.Automation.EconomicSystem.Tests
{
    [TestClass]
    public class ElectionTests
    {
        public readonly ILogHelper _logger = LogHelper.GetLogHelper();
        
        public static string RpcUrl { get; } = "http://192.168.197.13:8100/chain";
        public readonly NodeBehaviors NodeBehaviors;
        public readonly UserBehaviors UserBehaviors;
        public readonly QueryBehaviors QueryBehaviors;
        public RpcApiHelper CH { get; set; }        
        public string InitAccount { get; } = "2876Vk2deM5ZnaXr1Ns9eySMSjpuvd53XatHTc37JXeW6HjiPs";
        
        public static List<string> BpNodeAddress { get; set; }
        public static List<string> FullNodeAddress { get; set; }
        
        public List<string> UserList { get; set; }
        public List<string> NodesPublicKeys { get; set; }
        public List<string> CandidatePublicKeys { get; set; }
        public List<string> CurrentMinersKeys { get; set; }
        public List<CandidateInfo> CandidateInfos { get; set; }
        
        public class CandidateInfo
        {
            public string Name { get; set; }
            public string Account { get; set; }
            public string PublicKey { get; set; }
        }
        
        public ElectionTests()
        {
            CH = new RpcApiHelper(RpcUrl, AccountManager.GetDefaultDataDir());
            var contractServices = new ContractServices(CH,InitAccount);
            NodeBehaviors = new NodeBehaviors(contractServices);
            UserBehaviors = new UserBehaviors(contractServices); 
            QueryBehaviors = new QueryBehaviors(contractServices);
        }
        
        [ClassInitialize]
        public void Initialize()
        {
            #region Basic Preparation

            //Init Logger
            string logName = "ElectionTest_" + DateTime.Now.ToString("MMddHHmmss") + ".log";
            string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", logName);
            _logger.InitLogHelper(dir);

            //Get FullNode Info
            FullNodeAddress = new List<string>();
            FullNodeAddress.Add("27TQjMxBx2Ep9MqnTupxXoz9ByoAMgZJvBjn4eJNGKzvEMxNsE");
            FullNodeAddress.Add("rmXn6GFX1P82KthaovPoFU2QLP9n2YS6qMZNCuHHv7CjGxuzh");
            FullNodeAddress.Add("j524cYcfkxFCSoSV5Lh9BavWw2vu3PD9YrUFrJcQ2Hwqf9kWb");
            FullNodeAddress.Add("2G9ygXZixbjUvbAP8TE4o5cZ9oCLG3n3iY899tN6dEmdbb7oaA");
            FullNodeAddress.Add("2nyWx5YoBZeVPASHPGdiTjwPdU2XKmY1pq48c2JQT1cAFgJFoY");
            FullNodeAddress.Add("2kYRs5eHFaskGQ18sARc5BKnguy3NRJs5WTnaVmWtFoMBzCLxo");

            //Get BpNode Info
            BpNodeAddress = new List<string>();
            BpNodeAddress.Add("2876Vk2deM5ZnaXr1Ns9eySMSjpuvd53XatHTc37JXeW6HjiPs");
            BpNodeAddress.Add("jdJz9LkXkePfTHmHfYPaksnUueymUpHVRSsA7mRWCvkZpA2gL");
            BpNodeAddress.Add("3PezHjVGutQefW54XjRNvXGN8SfNqz4v2pzuQZ7as5HwaVDeT");

            //Get candidate infos
            NodesPublicKeys = new List<string>();
            CandidateInfos = new List<CandidateInfo>();
            for (int i = 0; i < BpNodeAddress.Count; i++)
            {
                string name = $"Bp-{i+1}";
                string account = BpNodeAddress[i];
                string pubKey = CH.GetPublicKeyFromAddress(account);
                NodesPublicKeys.Add(pubKey);
                _logger.WriteInfo($"{account}: {pubKey}");
                CandidateInfos.Add(new CandidateInfo(){Name = name, Account = account, PublicKey = pubKey});
            }
            for (int i = 0; i < FullNodeAddress.Count; i++)
            {
                string name = $"Full-{i+1}";
                string account = FullNodeAddress[i];
                string pubKey = CH.GetPublicKeyFromAddress(account);
                NodesPublicKeys.Add(pubKey);
                _logger.WriteInfo($"{account}: {pubKey}");
                CandidateInfos.Add(new CandidateInfo(){Name = name, Account = account, PublicKey = pubKey});
            }
            
            //Transfer candidate 200_000
            for (int i = 0; i < FullNodeAddress.Count; i++)
            {
                UserBehaviors.TokenService.ExecuteMethodWithTxId(TokenMethod.Transfer, new TransferInput
                {
                    Symbol = "ELF",
                    Amount = 200_000L,
                    To = Address.Parse(FullNodeAddress[i]),
                    Memo = "Transfer token for announcement."
                });
            }
            UserBehaviors.TokenService.CheckTransactionResultList();
            
            //Generate 50 accounts to vote
            PrepareUserAccountAndBalance(50);
           
            #endregion
        }

        private void PrepareUserAccountAndBalance(int userAccount)
        {
            //Account preparation
            UserList = new List<string>();
            var ci = new CommandInfo(ApiMethods.AccountNew);
            for (int i = 0; i < userAccount; i++)
            {
                ci.Parameter = "123";
                ci = CH.NewAccount(ci);
                if (ci.Result)
                    UserList.Add(ci.InfoMsg?[0].Replace("Account address:", "").Trim());

                //unlock
                var uc = new CommandInfo("AccountUnlock", "account");
                uc.Parameter = $"{UserList[i]} 123 notimeout";
                CH.UnlockAccount(uc);
            }

            //分配资金给普通用户
            UserBehaviors.TokenService.SetAccount(BpNodeAddress[0]);
            foreach (var acc in UserList)
            {
                UserBehaviors.TokenService.ExecuteMethodWithTxId(TokenMethod.Transfer, new TransferInput
                {
                    Amount = 1000,
                    Memo = "transfer for balance test",
                    Symbol = "ELF",
                    To = Address.Parse(acc) 
                });
            }
            UserBehaviors.TokenService.CheckTransactionResultList();

            foreach (var userAcc in UserList)
            {
                var callResult = UserBehaviors.TokenService.CallViewMethod<GetBalanceOutput>(TokenMethod.GetBalance, new GetBalanceInput
                {
                    Symbol = "ELF",
                    Owner = Address.Parse(userAcc)
                });
                Console.WriteLine($"User-{userAcc} balance: " + callResult.Balance);
            }

            _logger.WriteInfo("All accounts created and unlocked.");
        }
        
        [ClassCleanup]
        public void TestCleanUp()
        {
            if (UserList.Count == 0) return;
            _logger.WriteInfo("Delete all account files created.");
            foreach (var item in UserList)
            {
                string file = Path.Combine(AccountManager.GetDefaultDataDir(), $"{item}.ak");
                File.Delete(file);
            }
        }
    }
}