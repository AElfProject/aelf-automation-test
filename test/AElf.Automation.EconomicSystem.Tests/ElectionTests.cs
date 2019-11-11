using System;
using System.Collections.Generic;
using AElfChain.Common;
using AElfChain.Common.Contracts;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using AElf.Contracts.MultiToken;
using AElf.Types;
using AElfChain.Common;
using AElfChain.Common.Managers;
using log4net;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AElf.Automation.EconomicSystem.Tests
{
    public class ElectionTests
    {
        protected static readonly ILog _logger = Log4NetHelper.GetLogger();
        protected static string RpcUrl { get; } = "http://192.168.197.40:8000";

        protected Behaviors Behaviors;

        //protected RpcApiHelper CH { get; set; }   
        protected INodeManager CH { get; set; }
        protected string InitAccount { get; } = "28Y8JA1i2cN6oHvdv7EraXJr9a1gY6D1PpJXw9QtRMRwKcBQMK";
        protected List<string> BpNodeAddress { get; set; }
        protected List<string> FullNodeAddress { get; set; }
        protected static List<string> UserList { get; set; }
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
            //Init Logger
            Log4NetHelper.LogInit("ElectionTest");

            #region Get services

            CH = new NodeManager(RpcUrl);
            var contractServices = new ContractServices(CH, InitAccount);
            Behaviors = new Behaviors(contractServices);

//            var schemeIds = Behaviors.GetCreatedProfitItems().SchemeIds;
//            ProfitItemsIds = new Dictionary<Behaviors.ProfitType, Hash>
//            {
//                {Behaviors.ProfitType.Treasury, schemeIds[0]},
//                {Behaviors.ProfitType.MinerReward, schemeIds[1]},
//                {Behaviors.ProfitType.BackSubsidy, schemeIds[2]},
//                {Behaviors.ProfitType.CitizenWelfare, schemeIds[3]},
//                {Behaviors.ProfitType.BasicMinerReward, schemeIds[4]},
//                {Behaviors.ProfitType.VotesWeightReward, schemeIds[5]},
//                {Behaviors.ProfitType.ReElectionReward, schemeIds[6]},
//            };

            #endregion

            #region Basic Preparation

            //Get FullNode Info
            FullNodeAddress = new List<string>
            {
                "2ZYyxEH6j8zAyJjef6Spa99Jx2zf5GbFktyAQEBPWLCvuSAn8D",
                "eFU9Quc8BsztYpEHKzbNtUpu9hGKgwGD2tyL13MqtFkbnAoCZ",
                "2V2UjHQGH8WT4TWnzebxnzo9uVboo67ZFbLjzJNTLrervAxnws",
                "EKRtNn3WGvFSTDewFH81S7TisUzs9wPyP4gCwTww32waYWtLB",
                "2LA8PSHTw4uub71jmS52WjydrMez4fGvDmBriWuDmNpZquwkNx",
                "2RCLmZQ2291xDwSbDEJR6nLhFJcMkyfrVTq1i1YxWC4SdY49a6",
            };

            //Get BpNode Info
            BpNodeAddress = new List<string>
            {
                "2frDVeV6VxUozNqcFbgoxruyqCRAuSyXyfCaov6bYWc7Gkxkh2",
                "28Y8JA1i2cN6oHvdv7EraXJr9a1gY6D1PpJXw9QtRMRwKcBQMK",
                "2oSMWm1tjRqVdfmrdL8dgrRvhWu1FP8wcZidjS6wPbuoVtxhEz",
                "WRy3ADLZ4bEQTn86ENi5GXi5J1YyHp9e99pPso84v2NJkfn5k"
            };

            //Get candidate infos
            NodesPublicKeys = new List<string>();
            CandidateInfos = new List<CandidateInfo>();
            for (var i = 0; i < BpNodeAddress.Count; i++)
            {
                var name = $"Bp-{i + 1}";
                var account = BpNodeAddress[i];
                var pubKey = CH.GetAccountPublicKey(account);
                NodesPublicKeys.Add(pubKey);
                _logger.Info($"{account}: {pubKey}");
                CandidateInfos.Add(new CandidateInfo() {Name = name, Account = account, PublicKey = pubKey});
            }

            for (var i = 0; i < FullNodeAddress.Count; i++)
            {
                var name = $"Full-{i + 1}";
                var account = FullNodeAddress[i];
                var pubKey = CH.GetAccountPublicKey(account);
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
                        Symbol = NodeOption.NativeTokenSymbol,
                        Amount = 200_000L,
                        To = AddressHelper.Base58StringToAddress(account),
                        Memo = "Transfer token for announcement."
                    });
                }

                Behaviors.TokenService.CheckTransactionResultList();
            }

            PrepareUserAccountAndBalance(30);

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

        protected void PrepareUserAccountAndBalance(int userAccount)
        {
            //Account preparation
            UserList = NewAccount(Behaviors, userAccount);
            UnlockAccounts(Behaviors, userAccount, UserList);

            //分配资金给普通用户
            var balance = Behaviors.GetBalance(UserList[0]);
            if (balance.Balance >= 20_0000_00000000)
                return;

            Behaviors.TokenService.SetAccount(BpNodeAddress[0]);
            foreach (var acc in UserList)
            {
                Behaviors.TokenService.ExecuteMethodWithTxId(TokenMethod.Transfer, new TransferInput
                {
                    Amount = 20_0000_00000000,
                    Memo = "transfer for balance test",
                    Symbol = NodeOption.NativeTokenSymbol,
                    To = AddressHelper.Base58StringToAddress(acc)
                });
            }

            Behaviors.TokenService.CheckTransactionResultList();

            foreach (var userAcc in UserList)
            {
                var callResult = Behaviors.TokenService.CallViewMethod<GetBalanceOutput>(TokenMethod.GetBalance,
                    new GetBalanceInput
                    {
                        Symbol = NodeOption.NativeTokenSymbol,
                        Owner = AddressHelper.Base58StringToAddress(userAcc)
                    });
                Console.WriteLine($"User-{userAcc} balance: " + callResult.Balance);
            }

            _logger.Info("All accounts prepared and unlocked.");
        }

        protected List<string> NewAccount(Behaviors services, int count)
        {
            var accountList = new List<string>();
            for (var i = 0; i < count; i++)
            {
                var account = services.NodeManager.NewAccount();
                accountList.Add(account);
            }

            return accountList;
        }

        protected void UnlockAccounts(Behaviors services, int count, List<string> accountList)
        {
            services.NodeManager.ListAccounts();
            for (var i = 0; i < count; i++)
            {
                var result = services.NodeManager.UnlockAccount(accountList[i]);
                Assert.IsTrue(result);
            }
        }
    }
}