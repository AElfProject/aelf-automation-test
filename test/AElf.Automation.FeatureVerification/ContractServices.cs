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
                //Vote contract
                VoteService = GenesisService.GetVoteContract();

                //Election contract
                ElectionService = GenesisService.GetElectionContract();

                //TokenConverter contract
                TokenConverterService = GenesisService.GetTokenConverterContract();

                TreasuryContract = GenesisService.GetTreasuryContract();
            }
        }

        public GenesisContract GenesisService { get; set; }
        public TokenContract TokenService { get; set; }
        public TokenConverterContract TokenConverterService { get; set; }
        public VoteContract VoteService { get; set; }
        public ProfitContract ProfitService { get; set; }
        public ElectionContract ElectionService { get; set; }
        public ConsensusContract ConsensusService { get; set; }
        public AssociationContract AssociationService { get; set; }
        public ReferendumContract ReferendumService { get; set; }
        public ParliamentContract ParliamentService { get; set; }
        public ConfigurationContract ConfigurationService { get; set; }
        public CrossChainContract CrossChainService { get; set; }
        public TreasuryContract TreasuryContract { get; set; }
        public TokenHolderContract TokenHolderContract { get; set; }


        public string CallAddress { get; set; }

        public void GetAllContractServices()
        {
            GenesisService = GenesisContract.GetGenesisContract(NodeManager, CallAddress);

            //Token contract
            TokenService = GenesisService.GetTokenContract();

            //Consensus contract
            ConsensusService = GenesisService.GetConsensusContract();

            //Parliament contract
            ParliamentService = GenesisService.GetParliamentContract();

            //AssociationAuth contract
            AssociationService = GenesisService.GetAssociationAuthContract();

            //Referendum contract
            ReferendumService = GenesisService.GetReferendumAuthContract();

            ConfigurationService = GenesisService.GetConfigurationContract();
            CrossChainService = GenesisService.GetCrossChainContract();
            TokenHolderContract = GenesisService.GetTokenHolderContract();
            //Profit contract
            ProfitService = GenesisService.GetProfitContract();
        }
    }
}