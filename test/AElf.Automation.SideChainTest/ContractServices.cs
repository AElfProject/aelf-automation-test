using AElf.Contracts.MultiToken;
using AElf.Types;
using AElfChain.Common.Contracts;
using AElfChain.Common.Managers;

namespace AElf.Automation.SideChainTests
{
    public class ContractServices
    {
        public readonly int ChainId;
        public readonly INodeManager NodeManager;

        public TokenContractContainer.TokenContractStub TokenContractStub;
        public TokenContractImplContainer.TokenContractImplStub TokenImplContractStub;

        public ContractServices(string url, string callAddress, string password)
        {
            NodeManager = new NodeManager(url);
            ChainId = ChainHelper.ConvertBase58ToChainId(NodeManager.GetChainId());
            CallAddress = callAddress;
            CallAccount = AddressHelper.Base58StringToAddress(callAddress);

            NodeManager.UnlockAccount(CallAddress, password);
            GetContractServices();
            var tester = new ContractTesterFactory(NodeManager);
            TokenContractStub =
                tester.Create<TokenContractContainer.TokenContractStub>(TokenService.Contract,
                    TokenService.CallAddress);
            TokenImplContractStub = tester.Create<TokenContractImplContainer.TokenContractImplStub>(
                TokenService.Contract,
                TokenService.CallAddress);
        }

        public GenesisContract GenesisService { get; set; }
        public TokenContract TokenService { get; set; }
        public ConsensusContract ConsensusService { get; set; }
        public CrossChainContract CrossChainService { get; set; }
        public ParliamentAuthContract ParliamentService { get; set; }
        public AssociationAuthContract AssociationService { get; set; }

        public string CallAddress { get; set; }
        public Address CallAccount { get; set; }

        public void GetContractServices()
        {
            GenesisService = GenesisContract.GetGenesisContract(NodeManager, CallAddress);

            //Token contract
            var tokenAddress = GenesisService.GetContractAddressByName(NameProvider.Token);
            TokenService = new TokenContract(NodeManager, CallAddress, tokenAddress.GetFormatted());

            //Consensus contract
            var consensusAddress = GenesisService.GetContractAddressByName(NameProvider.Consensus);
            ConsensusService = new ConsensusContract(NodeManager, CallAddress, consensusAddress.GetFormatted());

            //CrossChain contract
            var crossChainAddress = GenesisService.GetContractAddressByName(NameProvider.CrossChain);
            CrossChainService = new CrossChainContract(NodeManager, CallAddress, crossChainAddress.GetFormatted());

            //ParliamentAuth contract
            var parliamentAuthAddress = GenesisService.GetContractAddressByName(NameProvider.ParliamentAuth);
            ParliamentService =
                new ParliamentAuthContract(NodeManager, CallAddress, parliamentAuthAddress.GetFormatted());

            AssociationService = GenesisService.GetAssociationAuthContract();
        }
    }
}