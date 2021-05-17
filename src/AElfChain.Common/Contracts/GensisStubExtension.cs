using AElf.Contracts.Consensus.AEDPoS;
using AElf.Contracts.Genesis;
using AElf.Contracts.MultiToken;


namespace AElfChain.Common.Contracts
{
    public static class GensisStubExtension
    {
        public static BasicContractZeroImplContainer.BasicContractZeroImplStub GetGenesisImplStub(this GenesisContract genesis,
            string caller = "")
        {
            if (caller == "")
                caller = genesis.CallAddress;
            
            return genesis.GetTestStub<BasicContractZeroImplContainer.BasicContractZeroImplStub>(caller);
        }
        
        public static AEDPoSContractContainer.AEDPoSContractStub GetConsensusStub(this GenesisContract genesis,
            string caller = "")
        {
            if (caller == "")
                caller = genesis.CallAddress;

            var consensus = genesis.GetContractAddressByName(NameProvider.Consensus);

            var contract = new ConsensusContract(genesis.NodeManager, caller, consensus.ToBase58());

            return contract.GetTestStub<AEDPoSContractContainer.AEDPoSContractStub>(caller);
        }

        public static AEDPoSContractImplContainer.AEDPoSContractImplStub GetConsensusImplStub(
            this GenesisContract genesis, string caller = "")
        {
            if (caller == "")
                caller = genesis.CallAddress;

            var consensus = genesis.GetContractAddressByName(NameProvider.Consensus);

            var contract = new ConsensusContract(genesis.NodeManager, caller, consensus.ToBase58());

            return contract.GetTestStub<AEDPoSContractImplContainer.AEDPoSContractImplStub>(caller);
        }
        
        public static TokenContractContainer.TokenContractStub GetTokenStub(this GenesisContract genesis, string token,
            string caller = "")
        {
            if (caller == "")
                caller = genesis.CallAddress;
            
            var contract = new TokenContract(genesis.NodeManager, caller, token);

            return contract.GetTestStub<TokenContractContainer.TokenContractStub>(caller);
        }
    }
}