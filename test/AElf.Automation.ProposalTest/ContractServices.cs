using AElf.Automation.Common.Contracts;
using AElf.Automation.Common.Helpers;
using AElf.Types;
using Google.Protobuf;
using log4net.Repository.Hierarchy;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AElf.Automation.ProposalTest
{
    public class ContractServices
    {
        public readonly IApiHelper ApiHelper;
        public GenesisContract GenesisService { get; set; }
        public TokenContract TokenService { get; set; }
        public ConsensusContract ConsensusService { get; set; }
        public AssociationAuthContract AssociationService { get; set; }
        public ParliamentAuthContract ParliamentService { get; set; }
        public ReferendumAuthContract ReferendumService { get; set; }

        public string CallAddress { get; set; }
        public Address CallAccount { get; set; }

        public ContractServices(string url, string callAddress, string keyStore, string password)
        {
            ApiHelper = new WebApiHelper(url, keyStore);
            CallAddress = callAddress;
            CallAccount = AddressHelper.Base58StringToAddress(callAddress);
            UnlockAccounts(ApiHelper, CallAddress, password);

            //connect chain
            ConnectionChain();

            //get all contract services
            GetContractServices();
        }

        public void GetContractServices()
        {
            GenesisService = GenesisContract.GetGenesisContract(ApiHelper, CallAddress);

            //TokenService contract
            var tokenAddress = GenesisService.GetContractAddressByName(NameProvider.TokenName);
            TokenService = new TokenContract(ApiHelper, CallAddress, tokenAddress.GetFormatted());

            //ParliamentAuth contract
            var parliamentAuthAddress = GenesisService.GetContractAddressByName(NameProvider.ParliamentName);
            ParliamentService =
                new ParliamentAuthContract(ApiHelper, CallAddress, parliamentAuthAddress.GetFormatted());

            //Consensus contract
            var consensusAddress = GenesisService.GetContractAddressByName(NameProvider.ConsensusName);
            ConsensusService = new ConsensusContract(ApiHelper, CallAddress, consensusAddress.GetFormatted());

            GetOrDeployAssociationContract();
            GetOrDeployReferendumContract();
        }

        private void ConnectionChain()
        {
            var ci = new CommandInfo(ApiMethods.GetChainInformation);
            ApiHelper.GetChainInformation(ci);
        }

        private void UnlockAccounts(IApiHelper apiHelper, string account, string password)
        {
            ApiHelper.ListAccounts();
            var ci = new CommandInfo(ApiMethods.AccountUnlock)
            {
                Parameter = $"{account} {password} notimeout"
            };
            ci = apiHelper.ExecuteCommand(ci);
            Assert.IsTrue(ci.Result);
        }

        private void GetOrDeployAssociationContract()
        {
            var associationAuthAddress = GenesisService.GetContractAddressByName(NameProvider.AssociationName).Value;
            AssociationService = associationAuthAddress == ByteString.Empty
                ? new AssociationAuthContract(ApiHelper, CallAddress)
                : new AssociationAuthContract(ApiHelper, CallAddress, associationAuthAddress.ToBase64());
        }

        private void GetOrDeployReferendumContract()
        {
            var referendumAuthAddress = GenesisService.GetContractAddressByName(NameProvider.ReferendumName).Value;
            ReferendumService = referendumAuthAddress == ByteString.Empty
                ? new ReferendumAuthContract(ApiHelper, CallAddress)
                : new ReferendumAuthContract(ApiHelper, CallAddress, referendumAuthAddress.ToBase64());
            ReferendumService.InitializeReferendum();
        }
    }
}