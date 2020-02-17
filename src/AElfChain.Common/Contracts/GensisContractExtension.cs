namespace AElfChain.Common.Contracts
{
    public static class GensisContractExtension
    {
        public static ConsensusContract GetConsensusContract(this GenesisContract genesis, string caller = "")
        {
            if (caller == "")
                caller = genesis.CallAddress;

            var consensus = genesis.GetContractAddressByName(NameProvider.Consensus);

            return new ConsensusContract(genesis.NodeManager, caller, consensus.GetFormatted());
        }

        public static ParliamentAuthContract GetParliamentAuthContract(
            this GenesisContract genesis, string caller = "")
        {
            if (caller == "")
                caller = genesis.CallAddress;

            var parliamentAuth = genesis.GetContractAddressByName(NameProvider.ParliamentAuth);

            return new ParliamentAuthContract(genesis.NodeManager, caller, parliamentAuth.GetFormatted());
        }

        public static ProfitContract GetProfitContract(this GenesisContract genesis, string caller = "")
        {
            if (caller == "")
                caller = genesis.CallAddress;

            var profit = genesis.GetContractAddressByName(NameProvider.Profit);

            return new ProfitContract(genesis.NodeManager, caller, profit.GetFormatted());
        }

        public static TokenContract GetTokenContract(this GenesisContract genesis, string caller = "")
        {
            if (caller == "")
                caller = genesis.CallAddress;

            var token = genesis.GetContractAddressByName(NameProvider.Token);

            return new TokenContract(genesis.NodeManager, caller, token.GetFormatted());
        }

        public static TokenHolderContract GetTokenHolderContract(this GenesisContract genesis, string caller = "")
        {
            if (caller == "")
                caller = genesis.CallAddress;

            var tokenHolder = genesis.GetContractAddressByName(NameProvider.TokenHolder);

            return new TokenHolderContract(genesis.NodeManager, caller, tokenHolder.GetFormatted());
        }

        public static TokenConverterContract GetTokenConverterContract(
            this GenesisContract genesis, string caller = "")
        {
            if (caller == "")
                caller = genesis.CallAddress;

            var tokenConverter = genesis.GetContractAddressByName(NameProvider.TokenConverter);

            return new TokenConverterContract(genesis.NodeManager, caller, tokenConverter.GetFormatted());
        }

        public static TreasuryContract GetTreasuryContract(this GenesisContract genesis, string caller = "")
        {
            if (caller == "")
                caller = genesis.CallAddress;

            var treasury = genesis.GetContractAddressByName(NameProvider.Treasury);

            return new TreasuryContract(genesis.NodeManager, caller, treasury.GetFormatted());
        }

        public static VoteContract GetVoteContract(this GenesisContract genesis, string caller = "")
        {
            if (caller == "")
                caller = genesis.CallAddress;

            var vote = genesis.GetContractAddressByName(NameProvider.Vote);

            return new VoteContract(genesis.NodeManager, caller, vote.GetFormatted());
        }

        public static ElectionContract GetElectionContract(this GenesisContract genesis, string caller = "")
        {
            if (caller == "")
                caller = genesis.CallAddress;

            var election = genesis.GetContractAddressByName(NameProvider.Election);

            return new ElectionContract(genesis.NodeManager, caller, election.GetFormatted());
        }

        public static CrossChainContract GetCrossChainContract(this GenesisContract genesis, string caller = "")
        {
            if (caller == "")
                caller = genesis.CallAddress;

            var cross = genesis.GetContractAddressByName(NameProvider.CrossChain);

            return new CrossChainContract(genesis.NodeManager, caller, cross.GetFormatted());
        }

        public static AssociationAuthContract GetAssociationAuthContract(
            this GenesisContract genesis, string caller = "")
        {
            if (caller == "")
                caller = genesis.CallAddress;

            var association = genesis.GetContractAddressByName(NameProvider.AssociationAuth);

            return new AssociationAuthContract(genesis.NodeManager, caller, association.GetFormatted());
        }

        public static ReferendumAuthContract GetReferendumAuthContract(
            this GenesisContract genesis, string caller = "")
        {
            if (caller == "")
                caller = genesis.CallAddress;

            var referendumAuth = genesis.GetContractAddressByName(NameProvider.ReferendumAuth);

            return new ReferendumAuthContract(genesis.NodeManager, caller, referendumAuth.GetFormatted());
        }

        public static ConfigurationContract GetConfigurationContract(this GenesisContract genesis, string caller = "")
        {
            if (caller == "")
                caller = genesis.CallAddress;

            var configuration = genesis.GetContractAddressByName(NameProvider.Configuration);

            return new ConfigurationContract(genesis.NodeManager, caller, configuration.GetFormatted());
        }
    }
}