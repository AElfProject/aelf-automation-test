using AElf.Automation.Common.Contracts;
using AElf.Automation.Common.Helpers;
using AElf.Types;

namespace AElf.Automation.Contracts.ScenarioTest
{
    public class ContractServices
    {
        public readonly IApiHelper ApiHelper;
        public GenesisContract GenesisService { get; set; }
        public TokenContract TokenService { get; set; }
        public TokenConverterContract TokenConverterService { get; set; }
        public VoteContract VoteService { get; set; }
        public ProfitContract ProfitService { get; set; }
        public ElectionContract ElectionService { get; set; }
        public ConsensusContract ConsensusService { get; set; }
        public AssociationAuthContract AssociationAuthService { get; set; }
        public ParliamentAuthContract ParliamentService { get; set; }

        public string CallAddress { get; set; }
        public Address CallAccount => AddressHelper.Base58StringToAddress(CallAddress);

        public ContractServices(IApiHelper apiHelper, string callAddress, string type)
        {
            ApiHelper = apiHelper;
            CallAddress = callAddress;

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
            }
        }

        public void GetAllContractServices()
        {
            GenesisService = GenesisContract.GetGenesisContract(ApiHelper, CallAddress);

            //TokenService contract
            TokenService = GenesisService.GetTokenContract();

            //TokenConverter contract
            //TokenConverterService = GenesisService.GetTokenConverterContract();

            //Consensus contract
            ConsensusService = GenesisService.GetConsensusContract();

            //Parliament contract
            ParliamentService = GenesisService.GetParliamentAuthContract();

            //Association contract
            //AssociationAuthService = GenesisService.GetAssociationAuthContract();
        }
    }
}