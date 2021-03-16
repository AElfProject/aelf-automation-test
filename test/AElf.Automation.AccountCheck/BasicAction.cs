using System.Collections.Generic;
using System.Linq;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using log4net;

namespace AElf.Automation.AccountCheck
{
    public class BasicAction
    {
        public List<string> AccountList;
        public List<string> FromAccountList;
        public List<string> ToAccountList;
        public INodeManager NodeManager;
        public AuthorityManager AuthorityManager;
        public ContractManager ContractManager;
        public string InitAccount;
        public string Password;

        public List<ContractInfo> ContractInfos;
        public int UserCount;
        public int ContractCount;
        public int CheckTimes;
        public long TransferAmount;
        public bool OnlyCheck;
        public bool IsNeedDeploy;
        public bool IsAddSystemContract;


        protected void GetService()
        {
            if (NodeManager !=null)
                return;
            
            var config = ConfigInfo.ReadInformation;
            var url = config.ServiceUrl;
            InitAccount = config.InitAccount;
            Password = config.Password;
            UserCount = config.UserCount;
            OnlyCheck = config.OnlyCheck;
            CheckTimes = config.Times;

            NodeManager = new NodeManager(url);
            AuthorityManager = new AuthorityManager(NodeManager,InitAccount,false);
            ContractManager = new ContractManager(NodeManager,InitAccount,Password);

            GetConfig();
            AccountList = new List<string>();
        }

        private void GetConfig()
        {
            var transferInfo = ConfigInfo.ReadInformation.TransferInfo;
            ContractInfos = ConfigInfo.ReadInformation.ContractInfos;
            
            ContractCount = transferInfo.ContractCount;
            TransferAmount = transferInfo.TransferAmount;
            IsNeedDeploy = transferInfo.IsNeedDeploy;
            IsAddSystemContract = transferInfo.IsAddSystemContract;
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

            FromAccountList = AccountList.GetRange(0, count / 2);
            ToAccountList = AccountList.GetRange(count / 2 , count / 2);
        }

        private static readonly ILog Logger = Log4NetHelper.GetLogger();

    }
}