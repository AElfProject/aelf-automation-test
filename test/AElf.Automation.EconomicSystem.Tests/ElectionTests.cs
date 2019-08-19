using System;
using System.Collections.Generic;
using System.IO;
using AElf.Automation.Common.Contracts;
using AElf.Automation.Common.OptionManagers;
using AElf.Automation.Common.Helpers;
using AElf.Contracts.MultiToken;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AElf.Automation.EconomicSystem.Tests
{
    public class ElectionTests
    {
        protected readonly ILogHelper _logger = LogHelper.GetLogger();
        protected static string RpcUrl { get; } = "http://3.1.220.141:8000";

        protected Behaviors Behaviors;

        //protected RpcApiHelper CH { get; set; }   
        protected IApiHelper CH { get; set; }
        protected string InitAccount { get; } = "2876Vk2deM5ZnaXr1Ns9eySMSjpuvd53XatHTc37JXeW6HjiPs";
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

            CH = new WebApiHelper(RpcUrl, CommonHelper.GetCurrentDataDir());
            var contractServices = new ContractServices(CH, InitAccount);
            Behaviors = new Behaviors(contractServices);

            var schemeIds = Behaviors.GetCreatedProfitItems().SchemeIds;
            ProfitItemsIds = new Dictionary<Behaviors.ProfitType, Hash>
            {
                {Behaviors.ProfitType.Treasury, schemeIds[0]},
                {Behaviors.ProfitType.MinerReward, schemeIds[1]},
                {Behaviors.ProfitType.BackSubsidy, schemeIds[2]},
                {Behaviors.ProfitType.CitizenWelfare, schemeIds[3]},
                {Behaviors.ProfitType.BasicMinerReward, schemeIds[4]},
                {Behaviors.ProfitType.VotesWeightReward, schemeIds[5]},
                {Behaviors.ProfitType.ReElectionReward, schemeIds[6]},
            };

            #endregion

            #region Basic Preparation

            //Init Logger
            string logName = "ElectionTest_" + DateTime.Now.ToString("MMddHHmmss") + ".log";
            string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", logName);
            _logger.InitLogHelper(dir);

            //Get FullNode Info
            FullNodeAddress = new List<string>();
            FullNodeAddress.Add("Ckk45Me2h1mG92VttavsW2FToqh8iq9MkLcytAQhGwastYKdB");
            FullNodeAddress.Add("JLF7Cox7yMkw9zJDgUTmH1dJehndszZW2UTRqkwCsE9evq5Uq");
            FullNodeAddress.Add("2sfQXA7WtHL2cpmDRw6uUEdMYhosf3gYBZQCD564yjW9ZhNFy6");
            FullNodeAddress.Add("2im4Yo9tz81fpTo64y1PdFbvdFTVCqmHusHjJknGGuhK7mjdHF");

            //Get BpNode Info
            BpNodeAddress = new List<string>();
            BpNodeAddress.Add("2876Vk2deM5ZnaXr1Ns9eySMSjpuvd53XatHTc37JXeW6HjiPs");
            BpNodeAddress.Add("jdJz9LkXkePfTHmHfYPaksnUueymUpHVRSsA7mRWCvkZpA2gL");
            BpNodeAddress.Add("3PezHjVGutQefW54XjRNvXGN8SfNqz4v2pzuQZ7as5HwaVDeT");

            //Get candidate infos
            NodesPublicKeys = new List<string>();
            CandidateInfos = new List<CandidateInfo>();
            for (var i = 0; i < BpNodeAddress.Count; i++)
            {
                var name = $"Bp-{i + 1}";
                var account = BpNodeAddress[i];
                var pubKey = CH.GetPublicKeyFromAddress(account);
                NodesPublicKeys.Add(pubKey);
                _logger.Info($"{account}: {pubKey}");
                CandidateInfos.Add(new CandidateInfo() {Name = name, Account = account, PublicKey = pubKey});
            }

            for (var i = 0; i < FullNodeAddress.Count; i++)
            {
                var name = $"Full-{i + 1}";
                var account = FullNodeAddress[i];
                var pubKey = CH.GetPublicKeyFromAddress(account);
                NodesPublicKeys.Add(pubKey);
                _logger.Info($"{account}: {pubKey}");
                CandidateInfos.Add(new CandidateInfo() {Name = name, Account = account, PublicKey = pubKey});
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
                        To = AddressHelper.Base58StringToAddress(account),
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
            _logger.Info("Delete all account files created.");
            foreach (var item in UserList)
            {
                var file = Path.Combine(CommonHelper.GetCurrentDataDir(), $"{item}.ak");
                File.Delete(file);
            }
            */
        }

        private void PrepareUserAccountAndBalance(int userAccount)
        {
            //Account preparation
            UserList = new List<string>
            {
                "2KTYvsWxcnjQPNnD1zWFCm83aLvmRGAQ8bvLnLFUV7XrrnYWNv"
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
                    To = AddressHelper.Base58StringToAddress(acc)
                });
            }

            Behaviors.TokenService.CheckTransactionResultList();

            foreach (var userAcc in UserList)
            {
                var callResult = Behaviors.TokenService.CallViewMethod<GetBalanceOutput>(TokenMethod.GetBalance,
                    new GetBalanceInput
                    {
                        Symbol = "ELF",
                        Owner = AddressHelper.Base58StringToAddress(userAcc)
                    });
                Console.WriteLine($"User-{userAcc} balance: " + callResult.Balance);
            }

            _logger.Info("All accounts prepared and unlocked.");
        }
    }
}