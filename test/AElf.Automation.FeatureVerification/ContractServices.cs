using AElfChain.Common.Contracts;
using AElfChain.Common.Managers;

namespace AElf.Automation.Contracts.ScenarioTest
{
    public class ContractServices
    {
        public readonly INodeManager NodeManager;

        public ContractServices(INodeManager nodeManager, string callAddress, string type)
        {
            NodeManager = nodeManager;
            CallAddress = callAddress;
            NodeManager.UnlockAccount(callAddress);

            //get all contract services
            GetAllContractServices();

            if (type.Equals("Main"))
            {
                //Profit contract
                ProfitService = GenesisService.GetProfitContract();

                //Vote contract
                VoteService = GenesisService.GetVoteContract();

                //Election contract
                ElectionService = GenesisService.GetElectionContract();

                //TokenConverter contract
                TokenConverterService = GenesisService.GetTokenConverterContract();
            }
        }

        public GenesisContract GenesisService { get; set; }
        public TokenContract TokenService { get; set; }
        public TokenConverterContract TokenConverterService { get; set; }
        public VoteContract VoteService { get; set; }
        public ProfitContract ProfitService { get; set; }
        public ElectionContract ElectionService { get; set; }
        public ConsensusContract ConsensusService { get; set; }
        public AssociationAuthContract AssociationAuthService { get; set; }
        public ReferendumAuthContract ReferendumAuthService { get; set; }
        public ParliamentAuthContract ParliamentService { get; set; }
        public ConfigurationContract ConfigurationService { get; set; }
        public CrossChainContract CrossChainService { get; set; }

        public string CallAddress { get; set; }

        public void GetAllContractServices()
        {
            GenesisService = GenesisContract.GetGenesisContract(NodeManager, CallAddress);

            //Token contract
            TokenService = GenesisService.GetTokenContract();

            //Consensus contract
            ConsensusService = GenesisService.GetConsensusContract();

            //ParliamentAuth contract
            ParliamentService = GenesisService.GetParliamentAuthContract();

            //AssociationAuth contract
            AssociationAuthService = GenesisService.GetAssociationAuthContract();

            //Referendum contract
            ReferendumAuthService = GenesisService.GetReferendumAuthContract();

            ConfigurationService = GenesisService.GetConfigurationContract();
            CrossChainService = GenesisService.GetCrossChainContract();
        }
    }
}