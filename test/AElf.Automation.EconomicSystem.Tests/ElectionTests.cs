using System;
using System.Collections.Generic;
using AElf.Automation.Common.Contracts;
using AElf.Automation.Common.Helpers;
using AElf.Automation.Common.Managers;
using AElf.Contracts.MultiToken;
using AElf.Types;
using log4net;

namespace AElf.Automation.EconomicSystem.Tests
{
    public class ElectionTests
    {
        protected readonly ILog _logger = Log4NetHelper.GetLogger();
        protected static string RpcUrl { get; } = "http//:52.66.209.107:8000";

        protected Behaviors Behaviors;

        //protected RpcApiHelper CH { get; set; }   
        protected INodeManager CH { get; set; }
        protected string InitAccount { get; } = "WRy3ADLZ4bEQTn86ENi5GXi5J1YyHp9e99pPso84v2NJkfn5k";
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
            //Init Logger
            Log4NetHelper.LogInit("ElectionTest");

            #region Get services

            CH = new NodeManager(RpcUrl);
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

            //Get FullNode Info
            FullNodeAddress = new List<string>();
            FullNodeAddress.Add("2frDVeV6VxUozNqcFbgoxruyqCRAuSyXyfCaov6bYWc7Gkxkh2");
            FullNodeAddress.Add("2ZYyxEH6j8zAyJjef6Spa99Jx2zf5GbFktyAQEBPWLCvuSAn8D");
            FullNodeAddress.Add("eFU9Quc8BsztYpEHKzbNtUpu9hGKgwGD2tyL13MqtFkbnAoCZ");
            FullNodeAddress.Add("2V2UjHQGH8WT4TWnzebxnzo9uVboo67ZFbLjzJNTLrervAxnws");
            FullNodeAddress.Add("EKRtNn3WGvFSTDewFH81S7TisUzs9wPyP4gCwTww32waYWtLB");
            FullNodeAddress.Add("2LA8PSHTw4uub71jmS52WjydrMez4fGvDmBriWuDmNpZquwkNx");

            //Get BpNode Info
            BpNodeAddress = new List<string>();
            BpNodeAddress.Add("7BSmhiLtVqHSUVGuYdYbsfaZUGpkL2ingvCmVPx66UR5L5Lbs");
            BpNodeAddress.Add("28Y8JA1i2cN6oHvdv7EraXJr9a1gY6D1PpJXw9QtRMRwKcBQMK");
            BpNodeAddress.Add("2oSMWm1tjRqVdfmrdL8dgrRvhWu1FP8wcZidjS6wPbuoVtxhEz");
            BpNodeAddress.Add("WRy3ADLZ4bEQTn86ENi5GXi5J1YyHp9e99pPso84v2NJkfn5k");

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
                var file = Path.Combine(CommonHelper.GetCurrentDataDir(), $"{item}.json");
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