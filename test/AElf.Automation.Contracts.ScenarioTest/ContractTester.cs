using AElfChain.Common.Contracts;
using AElfChain.Common.Managers;

namespace AElf.Automation.Contracts.ScenarioTest
{
    public class ContractTester
    {
        public readonly INodeManager NodeManager;
        public readonly ContractServices ContractServices;

        public readonly ElectionContract ElectionService;
        public readonly VoteContract VoteService;
        public readonly ProfitContract ProfitService;
        public readonly TokenContract TokenService;
        public readonly GenesisContract GenesisService;
        public readonly TokenConverterContract TokenConverterService;
        public readonly ConsensusContract ConsensusService;
        public readonly AssociationAuthContract AssociationService;
        public readonly ParliamentAuthContract ParliamentService;
        public readonly ReferendumAuthContract ReferendumService;


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