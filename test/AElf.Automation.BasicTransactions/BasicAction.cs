using System;
using System.Collections.Generic;
using System.Linq;
using AElf.Contracts.MultiToken;
using AElf.Types;
using AElfChain.Common.Contracts;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;

namespace AElf.Automation.BasicTransaction
{
    public class BasicAction
    {
        protected void GetService()
        {
            if (NodeManager != null)
                return;

            var config = ConfigInfo.ReadInformation;
            var url = config.Url;
            InitAccount = config.InitAccount;
            Password = config.Password;
            TransferAmount = config.TransferAmount;
            Times = config.Times;
            TokenAddress = config.TokenAddress;
            WrapperAddress = config.WrapperAddress;
            
            NodeManager = new NodeManager(url);
            AuthorityManager = new AuthorityManager(NodeManager, InitAccount);
            GetTestAccounts();
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
        
        private void GetTestAccounts()
        {
            TestAccountList = new List<string>();
            var miners = AuthorityManager.GetCurrentMiners();
            var accounts = NodeManager.ListAccounts();
            var testUsers = accounts.FindAll(o => !miners.Contains(o) && !o.Equals(InitAccount));
            TestAccount = testUsers.Count == 0 ? NodeManager.NewAccount() : testUsers.First();
            if (testUsers.Count >= Times)
                TestAccountList = testUsers.Take(Times).ToList();
            else
            {
                var generateCount = Times - testUsers.Count;
                for (var i = 0; i < generateCount; i++)
                {
                    var account = NodeManager.NewAccount();
                    TestAccountList.Add(account);
                }
            }
        }

        protected Address GetFromVirtualAccounts(TransferWrapperContract contract)
        {
            var virtualAccount = HashHelper.ComputeFrom(InitAccount.ConvertAddress());
                var address = NodeManager.GetVirtualAddress(virtualAccount, contract.Contract);
                return address;
        }

        public INodeManager NodeManager;
        public AuthorityManager AuthorityManager;
        public string InitAccount;
        public string Password;
        public string TestAccount;
        public List<string> TestAccountList;
        public long TransferAmount;
        public int Times;

        public string TokenAddress;
        public string WrapperAddress;
    }
}