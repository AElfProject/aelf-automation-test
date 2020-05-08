using AElf.Contracts.MultiToken;
using AElf.Types;
using AElfChain.Common.Contracts;
using AElfChain.Common.DtoExtension;
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
            CallAccount = callAddress.ConvertAddress();

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
        public ParliamentContract ParliamentService { get; set; }
        public AssociationContract AssociationService { get; set; }

        public string CallAddress { get; set; }
        public Address CallAccount { get; set; }

        public void GetContractServices()
        {
            GenesisService = GenesisContract.GetGenesisContract(NodeManager, CallAddress);

            //Token contract
            var tokenAddress = GenesisService.GetContractAddressByName(NameProvider.Token);
            TokenService = new TokenContract(NodeManager, CallAddress, tokenAddress.ToBase58());

            //Consensus contract
            var consensusAddress = GenesisService.GetContractAddressByName(NameProvider.Consensus);
            ConsensusService = new ConsensusContract(NodeManager, CallAddress, consensusAddress.ToBase58());

            //CrossChain contract
            var crossChainAddress = GenesisService.GetContractAddressByName(NameProvider.CrossChain);
            CrossChainService = new CrossChainContract(NodeManager, CallAddress, crossChainAddress.ToBase58());

            //Parliament contract
            var parliamentAuthAddress = GenesisService.GetContractAddressByName(NameProvider.ParliamentAuth);
            ParliamentService =
                new ParliamentContract(NodeManager, CallAddress, parliamentAuthAddress.ToBase58());

            AssociationService = GenesisService.GetAssociationAuthContract();
        }
    }
}