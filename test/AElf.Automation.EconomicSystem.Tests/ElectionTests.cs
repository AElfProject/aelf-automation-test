using System;
using System.Collections.Generic;
using System.IO;
using AElf.Automation.Common.Contracts;
using AElf.Automation.Common.OptionManagers;
using AElf.Automation.Common.Helpers;
using AElf.Contracts.MultiToken.Messages;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AElf.Automation.EconomicSystem.Tests
{
    public class ElectionTests
    {
        protected readonly ILogHelper _logger = LogHelper.GetLogHelper();
        protected static string RpcUrl { get; } = "http://192.168.199.205:8100";
        protected Behaviors Behaviors;
        //protected RpcApiHelper CH { get; set; }   
        protected IApiHelper CH { get; set; } 
        protected string InitAccount { get; } = "MEvVWBEQ6BTTCMCM2eoU4kVmaNGTapNxxqBtQqFVELHBBUNbc";
        protected List<string> BpNodeAddress { get; set; }
        protected List<string> FullNodeAddress { get; set; }
        protected List<string> UserList { get; set; }
        protected List<string> NodesPublicKeys { get; set; }
        protected List<CandidateInfo> CandidateInfos { get; set; }
        protected Dictionary<Behaviors.ProfitType, Hash> ProfitItemsIds { get; set; }

        protected class CandidateInfo
        {
            public string Name { get; set; }
            public string Account { get; set; }
            public string PublicKey { get; set; }
        }

        protected void Initialize()
        {
            #region Get services
            CH = new WebApiHelper(RpcUrl, AccountManager.GetDefaultDataDir());
            var contractServices = new ContractServices(CH,InitAccount);
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
            string logName = "ElectionTest_" + DateTime.Now.ToString("MMddHHmmss") + ".log";
            string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", logName);
            _logger.InitLogHelper(dir);

            //Get FullNode Info
            FullNodeAddress = new List<string>();
            FullNodeAddress.Add("27TQjMxBx2Ep9MqnTupxXoz9ByoAMgZJvBjn4eJNGKzvEMxNsE"); //13-20
            FullNodeAddress.Add("j524cYcfkxFCSoSV5Lh9BavWw2vu3PD9YrUFrJcQ2Hwqf9kWb"); //28-20
            FullNodeAddress.Add("2nyWx5YoBZeVPASHPGdiTjwPdU2XKmY1pq48c2JQT1cAFgJFoY"); //33-20
            FullNodeAddress.Add("rmXn6GFX1P82KthaovPoFU2QLP9n2YS6qMZNCuHHv7CjGxuzh"); //13-30
            FullNodeAddress.Add("2G9ygXZixbjUvbAP8TE4o5cZ9oCLG3n3iY899tN6dEmdbb7oaA"); //28-30
            FullNodeAddress.Add("2kYRs5eHFaskGQ18sARc5BKnguy3NRJs5WTnaVmWtFoMBzCLxo"); //33-30

            //Get BpNode Info
            BpNodeAddress = new List<string>();
            BpNodeAddress.Add("2876Vk2deM5ZnaXr1Ns9eySMSjpuvd53XatHTc37JXeW6HjiPs"); //13-10
            BpNodeAddress.Add("jdJz9LkXkePfTHmHfYPaksnUueymUpHVRSsA7mRWCvkZpA2gL"); //28-10
            BpNodeAddress.Add("3PezHjVGutQefW54XjRNvXGN8SfNqz4v2pzuQZ7as5HwaVDeT"); //33-10

            //Get candidate infos
            NodesPublicKeys = new List<string>();
            CandidateInfos = new List<CandidateInfo>();
            for (var i = 0; i < BpNodeAddress.Count; i++)
            {
                var name = $"Bp-{i+1}";
                var account = BpNodeAddress[i];
                var pubKey = CH.GetPublicKeyFromAddress(account);
                NodesPublicKeys.Add(pubKey);
                _logger.WriteInfo($"{account}: {pubKey}");
                CandidateInfos.Add(new CandidateInfo(){Name = name, Account = account, PublicKey = pubKey});
            }
            for (var i = 0; i < FullNodeAddress.Count; i++)
            {
                var name = $"Full-{i+1}";
                var account = FullNodeAddress[i];
                var pubKey = CH.GetPublicKeyFromAddress(account);
                NodesPublicKeys.Add(pubKey);
                _logger.WriteInfo($"{account}: {pubKey}");
                CandidateInfos.Add(new CandidateInfo(){Name = name, Account = account, PublicKey = pubKey});
            }
            
            //Transfer candidate 200_000
            var balance = Behaviors.GetBalance(FullNodeAddress[0]);
            if (balance.Balance == 0)
            {
                foreach (var account in FullNodeAddress)
                {
                    Behaviors.TokenService.ExecuteMethodWithTxId(TokenMethod.Transfer, new TransferInput
                    {
                        Symbol = "ELF",
                        Amount = 200_000L,
                        To = Address.Parse(account),
                        Memo = "Transfer token for announcement."
                    });
                }

                Behaviors.TokenService.CheckTransactionResultList();
            }
            
            //Generate 50 accounts to vote
            PrepareUserAccountAndBalance(10);
            
            #endregion
        }

        protected void TestCleanUp()
        {
            /*
            if (UserList.Count == 0) return;
            _logger.WriteInfo("Delete all account files created.");
            foreach (var item in UserList)
            {
                var file = Path.Combine(AccountManager.GetDefaultDataDir(), $"{item}.ak");
                File.Delete(file);
            }
            */
        }
        
        private void PrepareUserAccountAndBalance(int userAccount)
        {
            //Account preparation
            UserList = new List<string>
            {
                "yzXWLxTJ84p13FqyUzjh8maNafookpAQNUh5x3YzMVGkQ9oPm",
                "yzuQMgwfsCB3tjJsE9nrvxqrP83RbioVHnHuf3B97cqWgz569",
                "z11jRfppmoDShUregbcPA6zXpNPqW6wmzbs4Bpf15L7AWxXAZ",
                "z2LvgQxUoiFBnjGCvxAA6Q4EaUhR4REzR7m5Ji3akiVhbtVS1",
                "z4ZCyrr5yXWuY1WgU4eczY3ebkdChB2He8f1dqFo2Y8CALAf3",
                "z4j62HffC7WReuvZVTNExn1nST2R4YD4JkFLhv8sqQYhfm7Tg",
                "z7EHxChMWreVdKMDfn7dSTYN7VBhQ1CC73Xjmb59Z37aKmLPK",
                "zDR4wfhvqD6XyqKThZfEYzE5BTFZn75qbeRJtdRUDKMp7aAWe",
                "zE8HtCKT76YqNdHcKqyK4HKrMhaShaFNJYp2SXyZZpSfwBfMj",
                "zFkbQKXovPeKXhHd1JuJJT7bKoJ9zw71rFQCmst3DqxUSA6dz"
            };

            //分配资金给普通用户
            var balance = Behaviors.GetBalance(UserList[0]);
            if (balance.Balance >= 500)
                return;
            
            Behaviors.TokenService.SetAccount(BpNodeAddress[0]);
            foreach (var acc in UserList)
            {
                Behaviors.TokenService.ExecuteMethodWithTxId(TokenMethod.Transfer, new TransferInput
                {
                    Amount = 1000,
                    Memo = "transfer for balance test",
                    Symbol = "ELF",
                    To = Address.Parse(acc)
                });
            }
            Behaviors.TokenService.CheckTransactionResultList();

            foreach (var userAcc in UserList)
            {
                var callResult = Behaviors.TokenService.CallViewMethod<GetBalanceOutput>(TokenMethod.GetBalance, new GetBalanceInput
                {
                    Symbol = "ELF",
                    Owner = Address.Parse(userAcc)
                });
                Console.WriteLine($"User-{userAcc} balance: " + callResult.Balance);
            }

            _logger.WriteInfo("All accounts prepared and unlocked.");
        }
    }
}