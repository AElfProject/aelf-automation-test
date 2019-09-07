using AElf.Automation.Common.Contracts;
using AElf.Automation.Common.Helpers;
using AElf.Automation.Common.Managers;
using AElf.Types;
using Google.Protobuf;
using log4net.Repository.Hierarchy;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AElf.Automation.ProposalTest
{
    public class ContractServices
    {
        public readonly INodeManager NodeManager;
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
            NodeManager = new NodeManager(url,keyStore);
            CallAddress = callAddress;
            CallAccount = AddressHelper.Base58StringToAddress(callAddress);
            NodeManager.UnlockAccount(CallAddress, password);

            //connect chain
            ConnectionChain();

            //get all contract services
            GetContractServices();
        }

        public void GetContractServices()
        {
            GenesisService = GenesisContract.GetGenesisContract(NodeManager, CallAddress);

            //TokenService contract
            TokenService = GenesisService.GetTokenContract();

            //ParliamentAuth contract
            ParliamentService = GenesisService.GetParliamentAuthContract();
            
            //Consensus contract
            ConsensusService = GenesisService.GetConsensusContract();

            GetOrDeployAssociationContract();
            GetOrDeployReferendumContract();
        }

        private void ConnectionChain()
        {
            var ci = new CommandInfo(ApiMethods.GetChainInformation);
            NodeManager.GetChainInformation(ci);
        }
        
        private void GetOrDeployAssociationContract()
        {
            var associationAuthAddress = GenesisService.GetContractAddressByName(NameProvider.AssociationName).Value;
            AssociationService = associationAuthAddress == ByteString.Empty
                ? new AssociationAuthContract(NodeManager, CallAddress)
                : new AssociationAuthContract(NodeManager, CallAddress, associationAuthAddress.ToBase64());
        }

        private void GetOrDeployReferendumContract()
        {
            var referendumAuthAddress = GenesisService.GetContractAddressByName(NameProvider.ReferendumName).Value;
            ReferendumService = referendumAuthAddress == ByteString.Empty
                ? new ReferendumAuthContract(NodeManager, CallAddress)
                : new ReferendumAuthContract(NodeManager, CallAddress, referendumAuthAddress.ToBase64());
            ReferendumService.InitializeReferendum();
        }
    }
}