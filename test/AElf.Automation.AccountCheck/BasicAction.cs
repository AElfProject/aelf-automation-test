using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using log4net;

namespace AElf.Automation.AccountCheck
{
    public class BasicAction
    {
        public ConcurrentBag<string> AccountList;
        public ConcurrentBag<string> FromAccountList;
        public ConcurrentBag<string> ToAccountList;
        public INodeManager NodeManager;
        public AuthorityManager AuthorityManager;
        public string InitAccount;
        public string Password;

        public List<ContractInfo> ContractInfos;
        public int UserCount;
        public int ContractCount;
        public int CheckTimes;
        public long TransferAmount;
        public string CheckType;
        public bool IsNeedDeploy;
        
        public void GetService()
        {
            var config = ConfigInfo.ReadInformation;
            var url = config.ServiceUrl;
            InitAccount = config.InitAccount;
            Password = config.Password;
            UserCount = config.UserCount;
            CheckType = config.CheckType;
            CheckTimes = config.Times;
            NodeManager = new NodeManager(url);
            AuthorityManager = new AuthorityManager(NodeManager, InitAccount, false);
            GetConfig();
            AccountList = new ConcurrentBag<string>();
        }

        private void GetConfig()
        {
            var transferInfo = ConfigInfo.ReadInformation.TransferInfo;
            ContractInfos = ConfigInfo.ReadInformation.ContractInfos;

            ContractCount = transferInfo.ContractCount;
            TransferAmount = transferInfo.TransferAmount;
            IsNeedDeploy = transferInfo.IsNeedDeploy;
        }

        public void GetTestAccounts()
        {
            if (AccountList.Count.Equals(UserCount))
                return;
            var count = UserCount;
            var miners = AuthorityManager.GetCurrentMiners();
            var accounts = NodeManager.ListAccounts();
            var testUsers = accounts.FindAll(o => !miners.Contains(o) && !o.Equals(InitAccount));
            if (testUsers.Count >= count)
            {
                foreach (var acc in testUsers.Take(count))
                    AccountList.Add(acc);
            }
            else
            {
                foreach (var acc in testUsers) AccountList.Add(acc);

                var generateCount = count - testUsers.Count;
                for (var i = 0; i < generateCount; i++)
                {
                    var account = NodeManager.NewAccount();
                    AccountList.Add(account);
                }
            }

            FromAccountList = AccountList;

            var list = new ConcurrentBag<string>();
            for (var i = 0; i < count; i++)
            {
                var account = NodeManager.NewFakeAccount();
                list.Add(account);
            }

            ToAccountList = list;
        }

        public void UnlockAllAccounts()
        {
            foreach (var t in FromAccountList)
            {
                var result = NodeManager.UnlockAccount(t);
                if (!result)
                    throw new Exception($"Account unlock {t} failed.");
            }
        }

        private static readonly ILog Logger = Log4NetHelper.GetLogger();
    }
}