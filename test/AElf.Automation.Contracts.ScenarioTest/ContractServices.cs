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
        public Address CallAccount { get; set; }

        public ContractServices(IApiHelper apiHelper, string callAddress, string type)
        {
            ApiHelper = apiHelper;
            CallAddress = callAddress;
            CallAccount = AddressHelper.Base58StringToAddress(callAddress);

            //connect chain
            ConnectionChain();

            //get all contract services
            GetAllContractServices();

            if (type.Equals("Main"))
            {
                //ProfitService contract
                var profitAddress = GenesisService.GetContractAddressByName(NameProvider.ProfitName);
                ProfitService = new ProfitContract(ApiHelper, CallAddress, profitAddress.GetFormatted());

                //VoteService contract
                var voteAddress = GenesisService.GetContractAddressByName(NameProvider.VoteName);
                VoteService = new VoteContract(ApiHelper, CallAddress, voteAddress.GetFormatted());

                //ElectionService contract
                var electionAddress = GenesisService.GetContractAddressByName(NameProvider.ElectionName);
                ElectionService = new ElectionContract(ApiHelper, CallAddress, electionAddress.GetFormatted());
            }
        }

        public void GetAllContractServices()
        {
            GenesisService = GenesisContract.GetGenesisContract(ApiHelper, CallAddress);

            //TokenService contract
            var tokenAddress = GenesisService.GetContractAddressByName(NameProvider.TokenName);
            TokenService = new TokenContract(ApiHelper, CallAddress, tokenAddress.GetFormatted());

            //TokenConverter contract
            //var converterAddress = GenesisService.GetContractAddressByName(NameProvider.TokenConverterName);
            //TokenConverterService = new TokenConverterContract(ApiHelper, CallAddress, converterAddress.GetFormatted());

            //Consensus contract
            var consensusAddress = GenesisService.GetContractAddressByName(NameProvider.ConsensusName);
            ConsensusService = new ConsensusContract(ApiHelper, CallAddress, consensusAddress.GetFormatted());

            //Parliament contract
            var parliamentAddress = GenesisService.GetContractAddressByName(NameProvider.ParliamentName);
            ParliamentService = new ParliamentAuthContract(ApiHelper, CallAddress, parliamentAddress.GetFormatted());

            //Association contract
//            var associationAuthAddress = GenesisService.GetContractAddressByName(NameProvider.AssciationName);
            var associationAuthAddress = "Acv7j84Ghi19JesSBQ8d56XenwCrJ5VBPvrS4mthtbuBjYtXR";
            AssociationAuthService = new AssociationAuthContract(ApiHelper, CallAddress, associationAuthAddress);
        }

        private void ConnectionChain()
        {
            var ci = new CommandInfo(ApiMethods.GetChainInformation);
            ApiHelper.GetChainInformation(ci);
        }
    }
}