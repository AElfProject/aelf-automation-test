using AElfChain.Common.Contracts;
using AElfChain.Common.Managers;

namespace AElf.Automation.Contracts.ScenarioTest
{
    public class ContractTester
    {
        public readonly AssociationAuthContract AssociationService;
        public readonly ConsensusContract ConsensusService;
        public readonly ContractServices ContractServices;

        public readonly ElectionContract ElectionService;
        public readonly GenesisContract GenesisService;
        public readonly INodeManager NodeManager;
        public readonly ParliamentAuthContract ParliamentService;
        public readonly ProfitContract ProfitService;
        public readonly ReferendumAuthContract ReferendumService;
        public readonly TokenConverterContract TokenConverterService;
        public readonly TokenContract TokenService;
        public readonly VoteContract VoteService;


        public ContractTester(ContractServices contractServices)
        {
            NodeManager = contractServices.NodeManager;
            ContractServices = contractServices;

            GenesisService = ContractServices.GenesisService;
            ElectionService = ContractServices.ElectionService;
            VoteService = ContractServices.VoteService;
            ProfitService = ContractServices.ProfitService;
            TokenService = ContractServices.TokenService;
            ConsensusService = ContractServices.ConsensusService;
            AssociationService = ContractServices.AssociationAuthService;
            ParliamentService = ContractServices.ParliamentService;
            ReferendumService = ContractServices.ReferendumAuthService;
        }
    }
}