using AElf.Automation.Common.Contracts;
using AElf.Automation.Common.Helpers;
using AElf.Types;
using AElfChain.SDK;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ApiMethods = AElf.Automation.Common.Helpers.ApiMethods;

namespace AElf.Automation.SideChain.Verification
{
    public class ContractServices
    {
        public readonly IApiHelper ApiHelper;
        public readonly int ChainId;
        public GenesisContract GenesisService { get; set; }
        public TokenContract TokenService { get; set; }
        public ConsensusContract ConsensusService { get; set; }
        public CrossChainContract CrossChainService { get; set; }
        public ParliamentAuthContract ParliamentService { get; set; }

        public string CallAddress { get; set; }
        public Address CallAccount { get; set; }

        public ContractServices(string url, string callAddress, string keyStore,string password, int chainId)
        {
            ChainId = chainId;
            ApiHelper = new WebApiHelper(url,keyStore);
            ApiHelper.ApiService.SetFailReTryTimes(20);
            CallAddress = callAddress;
            CallAccount = AddressHelper.Base58StringToAddress(callAddress);
            UnlockAccounts(ApiHelper,CallAddress,password);
            
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

            //CrossChain contract
            var crossChainAddress = GenesisService.GetContractAddressByName(NameProvider.CrossChainName);
            CrossChainService = new CrossChainContract(ApiHelper, CallAddress, crossChainAddress.GetFormatted());
            
            //ParliamentAuth contract
            var parliamentAuthAddress = GenesisService.GetContractAddressByName(NameProvider.ParliamentName);
            ParliamentService =
                new ParliamentAuthContract(ApiHelper, CallAddress, parliamentAuthAddress.GetFormatted());
            
            //Consensus contract
            var consensusAddress = GenesisService.GetContractAddressByName(NameProvider.ConsensusName);
            ConsensusService = new ConsensusContract(ApiHelper,CallAddress,consensusAddress.GetFormatted());
        }

        private void ConnectionChain()
        {
            var ci = new CommandInfo(ApiMethods.GetChainInformation);
            ApiHelper.GetChainInformation(ci);
        }

        private void UnlockAccounts(IApiHelper apiHelper,string account,string password)
        {
            ApiHelper.ListAccounts();
            var ci = new CommandInfo(ApiMethods.AccountUnlock)
            {
                Parameter = $"{account} {password} notimeout"
            };
            ci = apiHelper.ExecuteCommand(ci);
            Assert.IsTrue(ci.Result);
        }
    }
}