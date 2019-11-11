using AElfChain.Common.Contracts;
using AElfChain.Common.Managers;
using AElf.Types;
using AElfChain.Common.Managers;

namespace AElf.Automation.EconomicSystem.Tests
{
    public class ContractServices
    {
        public readonly INodeManager NodeManager;
        public GenesisContract GenesisService { get; set; }
        public TokenContract TokenService { get; set; }
        public TokenConverterContract TokenConverterService { get; set; }
        public VoteContract VoteService { get; set; }
        public TreasuryContract TreasuryService { get; set; }
        public ProfitContract ProfitService { get; set; }
        public ElectionContract ElectionService { get; set; }
        public ConsensusContract ConsensusService { get; set; }

        public string CallAddress { get; set; }
        public Address CallAccount { get; set; }

        public ContractServices(INodeManager nodeManager, string callAddress)
        {
            NodeManager = nodeManager;
            CallAddress = callAddress;
            CallAccount = AddressHelper.Base58StringToAddress(callAddress);

            //get all contract services
            GetAllContractServices();
        }

        public void GetAllContractServices()
        {
            GenesisService = GenesisContract.GetGenesisContract(NodeManager, CallAddress);

            //TokenService contract
            var tokenAddress = GenesisService.GetContractAddressByName(NameProvider.Token);
            TokenService = new TokenContract(NodeManager, CallAddress, tokenAddress.GetFormatted());

            //ProfitService contract
            var profitAddress = GenesisService.GetContractAddressByName(NameProvider.Profit);
            ProfitService = new ProfitContract(NodeManager, CallAddress, profitAddress.GetFormatted());

            //VoteService contract
            var voteAddress = GenesisService.GetContractAddressByName(NameProvider.Vote);
            VoteService = new VoteContract(NodeManager, CallAddress, voteAddress.GetFormatted());

            //ElectionService contract
            var electionAddress = GenesisService.GetContractAddressByName(NameProvider.Election);
            ElectionService = new ElectionContract(NodeManager, CallAddress, electionAddress.GetFormatted());

            //Consensus contract
            var consensusAddress = GenesisService.GetContractAddressByName(NameProvider.Consensus);
            ConsensusService = new ConsensusContract(NodeManager, CallAddress, consensusAddress.GetFormatted());
            
            //Treasury contract
            TreasuryService = GenesisService.GetTreasuryContract();
        }
    }
}