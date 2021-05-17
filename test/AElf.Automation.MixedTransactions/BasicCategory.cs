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
            var config = ConfigInfo.ReadInformation;
            var url = config.ServiceUrl;
            InitAccount = config.InitAccount;
            Password = config.Password;
            TransactionGroup = config.TransactionGroup;
            VerifyCount = config.VerifyCount;
            TransactionCount = config.TransactionCount;
            NeedCreateToken = config.NeedCreateToken;

            NodeManager = new NodeManager(url);
            AuthorityManager = new AuthorityManager(NodeManager, InitAccount, false);

            GetConfig();
            AccountList = new List<string>();
            ToAccountList = new List<string>();
            FromAccountList = new List<string>(); 
        }

        private void GetConfig()
        {
            ContractInfos = ConfigInfo.ReadInformation.ContractInfos;
        }

        public void GetTestAccounts()
        {
            var authority = new AuthorityManager(NodeManager);
            var miners = authority.GetCurrentMiners();
            var accounts = NodeManager.ListAccounts();
            var testUsers = accounts.FindAll(o => !miners.Contains(o));
            if (testUsers.Count >= TransactionGroup)
            {
                foreach (var acc in testUsers.Take(TransactionGroup)) FromAccountList.Add(acc);
                foreach (var acc in testUsers.Take(TransactionGroup)) AccountList.Add(acc);
            }
            else
            {
                foreach (var acc in testUsers) FromAccountList.Add(acc);

                var generateCount = TransactionGroup - testUsers.Count;
                for (var i = 0; i < generateCount; i++)
                {
                    var account = NodeManager.NewAccount();
                    FromAccountList.Add(account);
                }
            }

            var list = new List<string>();
            for (var i = 0; i < TransactionGroup; i++)
            {
                var account = NodeManager.NewFakeAccount();
                    list.Add(account);
            }
            ToAccountList = list;
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
            var from = FromAccountList[times];
            var to = ToAccountList[times];
            return (from, to);
        }

        public List<string> AccountList;
        public List<string> FromAccountList;
        public List<string>ToAccountList;
        public INodeManager NodeManager;
        public AuthorityManager AuthorityManager;
        public string InitAccount;
        public string Password;

        public List<ContractInfo> ContractInfos;
        public int TransactionGroup;
        public long VerifyCount;
        public long TransactionCount;
        public bool NeedCreateToken;
    }
}