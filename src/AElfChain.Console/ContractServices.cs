using System.Collections.Generic;
using System.Linq;
using AElfChain.Common.Contracts;
using AElfChain.Common.Managers;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Contracts.MultiToken;
using AElf.Contracts.ParliamentAuth;
using AElf.Contracts.TokenConverter;
using AElf.Types;
using AElfChain.Common.Managers;

namespace AElfChain.Console
{
    public class ContractServices
    {
        public INodeManager NodeManager;
        public GenesisContract Genesis;

        public AuthorityManager Authority => GetAuthority();

        //contract
        public Dictionary<string, string> SystemContracts => GetSystemContracts();
        public TokenContract Token => Genesis.GetTokenContract();

        public TokenConverterContract TokenConverter => Genesis.GetTokenConverterContract();

        public ConsensusContract Consensus => Genesis.GetConsensusContract();

        //contract stub
        public TokenContractContainer.TokenContractStub TokenStub => Genesis.GetTokenStub();

        public TokenConverterContractContainer.TokenConverterContractStub TokenConverterStub =>
            Genesis.GetTokenConverterStub();

        public AEDPoSContractContainer.AEDPoSContractStub ConsensusStub => Genesis.GetConsensusStub();

        public ParliamentAuthContract ParliamentAuth => Genesis.GetParliamentAuthContract();

        public ParliamentAuthContractContainer.ParliamentAuthContractStub ParliamentAuthStub =>
            Genesis.GetParliamentAuthStub();
        
        public ContractServices(INodeManager nodeManager, string caller = "")
        {
            NodeManager = nodeManager;
            Genesis = nodeManager.GetGenesisContract(caller);
        }

        public string GetContractAddress(string name)
        {
            if (SystemContracts.ContainsKey(name))
                return SystemContracts[name];
            
            return null;
        }

        private AuthorityManager GetAuthority()
        {
            if(_authorityManager == null)
                _authorityManager = new AuthorityManager(NodeManager, Genesis.CallAddress);

            return _authorityManager;
        }

        private AuthorityManager _authorityManager;

        private Dictionary<string, string> GetSystemContracts()
        {
            if (_systemContracts == null)
            {
                var contracts = Genesis.GetAllSystemContracts();
                _systemContracts = new Dictionary<string, string>();
                foreach (var key in contracts.Keys)
                {
                    if(contracts[key].Equals(new Address())) continue;
                    _systemContracts.Add(key.ToString(), contracts[key].GetFormatted());
                }
            }

            return _systemContracts;
        }

        private Dictionary<string, string> _systemContracts;
    }
}