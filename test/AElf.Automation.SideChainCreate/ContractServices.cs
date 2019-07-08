using AElf.Automation.Common.Contracts;
using AElf.Automation.Common.Helpers;
using AElf.Types;

namespace AElf.Automation.SideChainCreate
{
    public class ContractServices
    {
        public readonly IApiHelper ApiHelper;
        public readonly string Url;
        public GenesisContract GenesisService { get; set; }
        public TokenContract TokenService { get; set; }
        public ConsensusContract ConsensusService { get; set; }
        public CrossChainContract CrossChainService { get; set; }
        public ParliamentAuthContract ParliamentService { get; set; }
        

        public string CallAddress { get; set; }
        public Address CallAccount { get; set; }

        public ContractServices(string url, string callAddress)
        {
            var keyStorePath = CommonHelper.GetCurrentDataDir();
            Url = url;
            ApiHelper = new WebApiHelper(url,keyStorePath);
            CallAddress = callAddress;
            CallAccount = Address.Parse(callAddress);

            //connect chain
            ConnectionChain();

            //get services
            GetContractServices();
        }

        public void GetContractServices()
        {
            GenesisService = GenesisContract.GetGenesisContract(ApiHelper, CallAddress);

            //TokenService contract
            var tokenAddress = GenesisService.GetContractAddressByName(NameProvider.TokenName);
            TokenService = new TokenContract(ApiHelper, CallAddress, tokenAddress.GetFormatted());

            //Consensus contract
            var consensusAddress = GenesisService.GetContractAddressByName(NameProvider.ConsensusName);
            ConsensusService = new ConsensusContract(ApiHelper, CallAddress, consensusAddress.GetFormatted());

            //CrossChain contract
            var crossChainAddress = GenesisService.GetContractAddressByName(NameProvider.CrossChainName);
            CrossChainService = new CrossChainContract(ApiHelper, CallAddress, crossChainAddress.GetFormatted());
            
            //ParliamentAuth contract
            var parliamentAuthAddress = GenesisService.GetContractAddressByName(NameProvider.ParliamentName);
            ParliamentService =
                new ParliamentAuthContract(ApiHelper, CallAddress, parliamentAuthAddress.GetFormatted());
        }

        private void ConnectionChain()
        {
            var ci = new CommandInfo(ApiMethods.GetChainInformation);
            ApiHelper.GetChainInformation(ci);
        }
    }
}