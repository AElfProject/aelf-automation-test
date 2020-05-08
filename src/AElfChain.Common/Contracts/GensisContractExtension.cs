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

        public static ParliamentContract GetParliamentContract(
            this GenesisContract genesis, string caller = "")
        {
            if (caller == "")
                caller = genesis.CallAddress;

            var parliamentAuth = genesis.GetContractAddressByName(NameProvider.ParliamentAuth);

            return new ParliamentContract(genesis.NodeManager, caller, parliamentAuth.ToBase58());
        }

        public static ProfitContract GetProfitContract(this GenesisContract genesis, string caller = "")
        {
            if (caller == "")
                caller = genesis.CallAddress;

            var profit = genesis.GetContractAddressByName(NameProvider.Profit);

            return new ProfitContract(genesis.NodeManager, caller, profit.ToBase58());
        }

        public static TokenContract GetTokenContract(this GenesisContract genesis, string caller = "")
        {
            if (caller == "")
                caller = genesis.CallAddress;

            var token = genesis.GetContractAddressByName(NameProvider.Token);

            return new TokenContract(genesis.NodeManager, caller, token.ToBase58());
        }

        public static TokenHolderContract GetTokenHolderContract(this GenesisContract genesis, string caller = "")
        {
            if (caller == "")
                caller = genesis.CallAddress;

            var tokenHolder = genesis.GetContractAddressByName(NameProvider.TokenHolder);

            return new TokenHolderContract(genesis.NodeManager, caller, tokenHolder.ToBase58());
        }

        public static TokenConverterContract GetTokenConverterContract(
            this GenesisContract genesis, string caller = "")
        {
            if (caller == "")
                caller = genesis.CallAddress;

            var tokenConverter = genesis.GetContractAddressByName(NameProvider.TokenConverter);

            return new TokenConverterContract(genesis.NodeManager, caller, tokenConverter.ToBase58());
        }

        public static TreasuryContract GetTreasuryContract(this GenesisContract genesis, string caller = "")
        {
            if (caller == "")
                caller = genesis.CallAddress;

            var treasury = genesis.GetContractAddressByName(NameProvider.Treasury);

            return new TreasuryContract(genesis.NodeManager, caller, treasury.ToBase58());
        }

        public static VoteContract GetVoteContract(this GenesisContract genesis, string caller = "")
        {
            if (caller == "")
                caller = genesis.CallAddress;

            var vote = genesis.GetContractAddressByName(NameProvider.Vote);

            return new VoteContract(genesis.NodeManager, caller, vote.ToBase58());
        }

        public static ElectionContract GetElectionContract(this GenesisContract genesis, string caller = "")
        {
            if (caller == "")
                caller = genesis.CallAddress;

            var election = genesis.GetContractAddressByName(NameProvider.Election);

            return new ElectionContract(genesis.NodeManager, caller, election.ToBase58());
        }

        public static CrossChainContract GetCrossChainContract(this GenesisContract genesis, string caller = "")
        {
            if (caller == "")
                caller = genesis.CallAddress;

            var cross = genesis.GetContractAddressByName(NameProvider.CrossChain);

            return new CrossChainContract(genesis.NodeManager, caller, cross.ToBase58());
        }

        public static AssociationContract GetAssociationAuthContract(
            this GenesisContract genesis, string caller = "")
        {
            if (caller == "")
                caller = genesis.CallAddress;

            var association = genesis.GetContractAddressByName(NameProvider.AssociationAuth);

            return new AssociationContract(genesis.NodeManager, caller, association.ToBase58());
        }

        public static ReferendumContract GetReferendumAuthContract(
            this GenesisContract genesis, string caller = "")
        {
            if (caller == "")
                caller = genesis.CallAddress;

            var referendumAuth = genesis.GetContractAddressByName(NameProvider.ReferendumAuth);

            return new ReferendumContract(genesis.NodeManager, caller, referendumAuth.ToBase58());
        }

        public static ConfigurationContract GetConfigurationContract(this GenesisContract genesis, string caller = "")
        {
            if (caller == "")
                caller = genesis.CallAddress;

            var configuration = genesis.GetContractAddressByName(NameProvider.Configuration);

            return new ConfigurationContract(genesis.NodeManager, caller, configuration.ToBase58());
        }
    }
}