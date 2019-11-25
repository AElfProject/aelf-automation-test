using AElfChain.Common.Contracts;
using AElfChain.Common.Managers;
using AElf.Types;

namespace AElf.Automation.Contracts.ScenarioTest
{
    public class ContractServices
    {
        public readonly INodeManager NodeManager;

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

        public string CallAddress { get; set; }
        public Address CallAccount => AddressHelper.Base58StringToAddress(CallAddress);
        public ContractServices(INodeManager nodeManager, string callAddress, string type)
        {
            NodeManager = nodeManager;
            CallAddress = callAddress;
            NodeManager.UnlockAccount(callAddress);

            //get all contract services
            GetAllContractServices();

            if (type.Equals("Main"))
            {
                //ProfitService contract
                ProfitService = GenesisService.GetProfitContract();

                //VoteService contract
                VoteService = GenesisService.GetVoteContract();

                //ElectionService contract
                ElectionService = GenesisService.GetElectionContract();
                
                //TokenConverter contract
                //TokenConverterService = GenesisService.GetTokenConverterContract();
            }
        }

        public void GetAllContractServices()
        {
            GenesisService = GenesisContract.GetGenesisContract(NodeManager, CallAddress);

            //TokenService contract
            TokenService = GenesisService.GetTokenContract();

            //Consensus contract
            ConsensusService = GenesisService.GetConsensusContract();

            //Parliament contract
            ParliamentService = GenesisService.GetParliamentAuthContract();

            //AssociationAuth contract
            AssociationAuthService = GenesisService.GetAssociationAuthContract();

            //Referendum contract
            ReferendumAuthService = GenesisService.GetReferendumAuthContract();
        }
    }
}