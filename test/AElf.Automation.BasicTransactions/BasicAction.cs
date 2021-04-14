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
            var miners = AuthorityManager.GetCurrentMiners();
            var accounts = NodeManager.ListAccounts();
            var testUsers = accounts.FindAll(o => !miners.Contains(o) && !o.Equals(InitAccount));
            TestAccount = testUsers.Count == 0 ? NodeManager.NewAccount() : testUsers.First();
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
        public long TransferAmount;
    }
}