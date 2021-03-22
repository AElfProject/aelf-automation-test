namespace AElfChain.Common.Contracts
{
    public static class GensisContractExtension
    {
        public static ConsensusContract GetConsensusContract(this GenesisContract genesis, string caller = "")
        {
            if (caller == "")
                caller = genesis.CallAddress;

            var consensus = genesis.GetContractAddressByName(NameProvider.Consensus);

            return new ConsensusContract(genesis.NodeManager, caller, consensus.ToBase58());
        }
        
        public static TokenContract GetTokenContract(this GenesisContract genesis, string tokenAddress, string caller = "")
        {
            if (caller == "")
                caller = genesis.CallAddress;
            
            return new TokenContract(genesis.NodeManager, caller, tokenAddress);
        }
    }
}