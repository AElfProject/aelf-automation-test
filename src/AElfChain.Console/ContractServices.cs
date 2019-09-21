using AElf.Automation.Common.Contracts;
using AElf.Automation.Common.Managers;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Contracts.MultiToken;
using AElf.Contracts.TokenConverter;

namespace AElfChain.Console
{
    public class ContractServices
    {
        public GenesisContract Genesis;
        public AuthorityManager Authority;

        //contract
        public TokenContract Token => Genesis.GetTokenContract();

        public TokenConverterContract TokenConverter => Genesis.GetTokenConverterContract();

        public ConsensusContract Consensus => Genesis.GetConsensusContract();

        //contract stub
        public TokenContractContainer.TokenContractStub TokenStub => Genesis.GetTokenStub();

        public TokenConverterContractContainer.TokenConverterContractStub TokenConverterStub =>
            Genesis.GetTokenConverterStub();

        public AEDPoSContractContainer.AEDPoSContractStub ConsensusStub => Genesis.GetConsensusStub();
        
        public ContractServices(INodeManager nodeManager, string caller = "")
        {
            Genesis = nodeManager.GetGenesisContract(caller);
            Authority = new AuthorityManager(nodeManager, Genesis.CallAddress);
        }
    }
}