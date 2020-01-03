using AElf.Types;
using AElfChain.Common.Contracts;
using AElfChain.Common.Managers;

namespace AElf.Automation.ProposalTest
{
    public class ContractServices
    {
        public readonly INodeManager NodeManager;

        public ContractServices(string url, string callAddress, string keyStore, string password)
        {
            NodeManager = new NodeManager(url, keyStore);
            CallAddress = callAddress;
            CallAccount = AddressHelper.Base58StringToAddress(callAddress);
            NodeManager.UnlockAccount(CallAddress, password);

            //get all contract services
            GetContractServices();
        }

        public GenesisContract GenesisService { get; set; }
        public TokenContract TokenService { get; set; }
        public ConsensusContract ConsensusService { get; set; }
        public AssociationAuthContract AssociationService { get; set; }
        public ParliamentAuthContract ParliamentService { get; set; }
        public ReferendumAuthContract ReferendumService { get; set; }

        public string CallAddress { get; set; }
        public Address CallAccount { get; set; }

        public void GetContractServices()
        {
            GenesisService = GenesisContract.GetGenesisContract(NodeManager, CallAddress);

            //Token contract
            TokenService = GenesisService.GetTokenContract();

            //ParliamentAuth contract
            ParliamentService = GenesisService.GetParliamentAuthContract();

            //Consensus contract
            ConsensusService = GenesisService.GetConsensusContract();

            //Referendum contract
            ReferendumService = GenesisService.GetReferendumAuthContract();

            //Association contract
            AssociationService = GenesisService.GetAssociationAuthContract();
        }
    }
}