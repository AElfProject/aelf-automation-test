using System.Collections.Generic;
using System.Linq;
using AElf.Contracts.MultiToken;
using AElfChain.Common.Contracts;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;

namespace AElf.Automation.MixedTransactions
{
    public class BasicCategory
    {
        protected void GetService()
        {
            if (NodeManager != null)
                return;

            var config = ConfigInfo.ReadInformation;
            var url = config.ServiceUrl;
            InitAccount = config.InitAccount;
            Password = config.Password;
            UserCount = config.UserCount;
            VerifyCount = config.VerifyCount;
            TransactionCount = config.TransactionCount;
            NeedCreateToken = config.NeedCreateToken;

            NodeManager = new NodeManager(url);
            AuthorityManager = new AuthorityManager(NodeManager, InitAccount, false);

            GetConfig();
            AccountList = new List<string>();
            GetTestAccounts();
        }

        private void GetConfig()
        {
            ContractInfos = ConfigInfo.ReadInformation.ContractInfos;
        }

        private void GetTestAccounts()
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
            ToAccountList = AccountList.GetRange(count / 2, count / 2);
        }

        protected List<string> GetFromVirtualAccounts(TransferWrapperContract contract)
        {
            var virtualAccountList = new List<string>();
            foreach (var from in FromAccountList)
            {
                var virtualAccount = HashHelper.ComputeFrom(from.ConvertAddress());
                var address = NodeManager.GetVirtualAddress(virtualAccount, contract.Contract);
                virtualAccountList.Add(address.ToBase58());
            }

            return virtualAccountList;
        }

        protected string GenerateNotExistTokenSymbol(TokenContract token)
        {
            while (true)
            {
                var symbol = CommonHelper.RandomString(8, false);
                var tokenInfo = token.GetTokenInfo(symbol);
                if (tokenInfo.Equals(new TokenInfo()))
                    return symbol;
            }
        }

        protected (string, string) GetTransferPair(int times)
        {
            var fromId = times - FromAccountList.Count >= 0
                ? (times / FromAccountList.Count > 1
                    ? times - FromAccountList.Count * (times / FromAccountList.Count)
                    : times - FromAccountList.Count)
                : times;
            var from = FromAccountList[fromId];

            var toId = times - ToAccountList.Count >= 0
                ? (times / ToAccountList.Count > 1
                    ? times - ToAccountList.Count * (times / ToAccountList.Count)
                    : times - ToAccountList.Count)
                : times;
            var to = ToAccountList[toId];

            return (from, to);
        }

        public List<string> AccountList;
        public List<string> FromAccountList;
        public List<string> ToAccountList;
        public INodeManager NodeManager;
        public AuthorityManager AuthorityManager;
        public string InitAccount;
        public string Password;

        public List<ContractInfo> ContractInfos;
        public int UserCount;
        public long VerifyCount;
        public long TransactionCount;
        public bool NeedCreateToken;
    }
}