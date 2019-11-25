using AElfChain.Common.Contracts;
using AElfChain.Common.Managers;
using AElf.Types;

namespace AElf.Automation.SideChain.Verification
{
    public class ContractServices
    {
        public readonly int ChainId;
        public readonly string PrimaryTokenSymbol;
        public readonly INodeManager NodeManager;
        public GenesisContract GenesisService { get; set; }
        public TokenContract TokenService { get; set; }
        public ConsensusContract ConsensusService { get; set; }
        public CrossChainContract CrossChainService { get; set; }
        public ParliamentAuthContract ParliamentService { get; set; }

        public string CallAddress { get;}
        public Address CallAccount { get;}
        

        public ContractServices(string url, string callAddress, string keyStore, string password)
        {
            NodeManager = new NodeManager(url, keyStore);
            CallAddress = callAddress;
            CallAccount = AddressHelper.Base58StringToAddress(callAddress);
            NodeManager.UnlockAccount(CallAddress, password);
            GetContractServices();
            var chainInfo = GetChainInfo();
            ChainId = ChainHelper.ConvertBase58ToChainId(chainInfo.ChainId);
            PrimaryTokenSymbol = chainInfo.PrimaryTokenSymbol;
        }
        
        public void GetContractServices()
        {
            GenesisService = GenesisContract.GetGenesisContract(NodeManager, CallAddress);

            //TokenService contract
            var tokenAddress = GenesisService.GetContractAddressByName(NameProvider.Token);
            TokenService = new TokenContract(NodeManager, CallAddress, tokenAddress.GetFormatted());

            //CrossChain contract
            var crossChainAddress = GenesisService.GetContractAddressByName(NameProvider.CrossChain);
            CrossChainService = new CrossChainContract(NodeManager, CallAddress, crossChainAddress.GetFormatted());

            //ParliamentAuth contract
            var parliamentAuthAddress = GenesisService.GetContractAddressByName(NameProvider.ParliamentAuth);
            ParliamentService =
                new ParliamentAuthContract(NodeManager, CallAddress, parliamentAuthAddress.GetFormatted());

            //Consensus contract
            var consensusAddress = GenesisService.GetContractAddressByName(NameProvider.Consensus);
            ConsensusService = new ConsensusContract(NodeManager, CallAddress, consensusAddress.GetFormatted());
        }
        
        private ChainInfo GetChainInfo()
        {
            var chainId = NodeManager.GetChainId();
            var primaryTokenSymbol = TokenService.GetPrimaryTokenSymbol();
            var chainInfo = new ChainInfo(chainId,primaryTokenSymbol);
            return chainInfo;
        }
    }
}