using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Automation.Common.Contracts;
using AElf.Automation.Common.Helpers;
using AElf.Types;
using Google.Protobuf;
using log4net;

namespace AElfChain.AccountService
{
    public class AuthorityManager : IAuthorityManager
    {
        private NodesInfo _info;
        private GenesisContract _genesis;
        private ConsensusContract _consensus;
        private ParliamentAuthContract _parliament;
        
        public static readonly ILog Logger = Log4NetHelper.GetLogger();
        
        public AuthorityManager()
        {
            
        }
        
        public Task<Address> DeployContractWithAuthority(string caller, string contractName)
        {
            throw new System.NotImplementedException();
        }

        public Task<TransactionResult> ExecuteTransactionWithAuthority(string contractAddress, string method, IMessage input, Address organizationAddress,
            IEnumerable<string> approveUsers, string callUser)
        {
            throw new System.NotImplementedException();
        }
        
        private void GetConfigNodeInfo()
        {
            var nodes = NodeInfoHelper.Config;
            nodes.CheckNodesAccount();

            _info = nodes;
        }
    }
}