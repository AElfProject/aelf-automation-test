using AElf.Automation.Common.Contracts;
using AElf.Automation.Common.Helpers;
using AElf.Types;

namespace AElf.Automation.SideChain.VerificationTest
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
        
        public ContractServices(string url, string callAddress,string type)
        {
            Url = url;
            ApiHelper = new WebApiHelper(url);
            CallAddress = callAddress;
            UnlockInitAccount(callAddress);
            CallAccount = Address.Parse(callAddress);
            
            //connect chain
            ConnectionChain();
            
            //get services
            GetContractServices();

            if (type.Equals("Main"))
            {
                //ParliamentAuth contract
                var parliamentAuthAddress = GenesisService.GetContractAddressByName(NameProvider.ParliamentName);
                ParliamentService = new ParliamentAuthContract(ApiHelper, CallAddress, parliamentAuthAddress.GetFormatted());
            }
        }
        
        public void GetContractServices()
        {
            GenesisService = GenesisContract.GetGenesisContract(ApiHelper, CallAddress);
            
            //TokenService contract
            var tokenAddress = GenesisService.GetContractAddressByName(NameProvider.TokenName);
            TokenService = new TokenContract(ApiHelper, CallAddress, tokenAddress.GetFormatted());
            
            //CrossChain contract
            var crossChainAddress = GenesisService.GetContractAddressByName(NameProvider.CrossChainName);
            CrossChainService = new CrossChainContract(ApiHelper, CallAddress, crossChainAddress.GetFormatted());
        }
        
        private void ConnectionChain()
        {
            var ci = new CommandInfo(ApiMethods.GetChainInformation);
            ApiHelper.GetChainInformation(ci);
        }
        
        private void UnlockInitAccount(string InitAccount)
        {
            var ci = new CommandInfo(ApiMethods.AccountUnlock)
            {
                Parameter = $"{InitAccount} 123 notimeout"
            };
            ApiHelper.ExecuteCommand(ci);
        }
    }
}