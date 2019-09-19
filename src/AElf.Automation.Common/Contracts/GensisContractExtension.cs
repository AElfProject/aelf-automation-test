namespace AElf.Automation.Common.Contracts
{
    public static class GensisContractExtension
    {
        public static ConsensusContract GetConsensusContract(this GenesisContract genesis, string caller = "")
        {
            if (caller == "")
                caller = genesis.CallAddress;
            
            var consensus = genesis.GetContractAddressByName(NameProvider.ConsensusName);

            return new ConsensusContract(genesis.NodeManager, caller, consensus.GetFormatted());
        }

        public static FeeReceiverContract GetFeeReceiverContract(
            this GenesisContract genesis, string caller = "")
        {
            if (caller == "")
                caller = genesis.CallAddress;
            
            var feeReceiver = genesis.GetContractAddressByName(NameProvider.FeeReceiverName);

            return new FeeReceiverContract(genesis.NodeManager, caller, feeReceiver.GetFormatted());
        }

        public static ParliamentAuthContract GetParliamentAuthContract(
            this GenesisContract genesis, string caller = "")
        {
            if (caller == "")
                caller = genesis.CallAddress;
            
            var parliamentAuth = genesis.GetContractAddressByName(NameProvider.ParliamentName);

            return new ParliamentAuthContract(genesis.NodeManager, caller, parliamentAuth.GetFormatted());
        }

        public static ProfitContract GetProfitContract(this GenesisContract genesis, string caller = "")
        {
            if (caller == "")
                caller = genesis.CallAddress;
            
            var profit = genesis.GetContractAddressByName(NameProvider.ProfitName);

            return new ProfitContract(genesis.NodeManager, caller, profit.GetFormatted());
        }

        public static TokenContract GetTokenContract(this GenesisContract genesis, string caller = "")
        {
            if (caller == "")
                caller = genesis.CallAddress;
            
            var token = genesis.GetContractAddressByName(NameProvider.TokenName);

            return new TokenContract(genesis.NodeManager, caller, token.GetFormatted());
        }

        public static TokenConverterContract GetTokenConverterContract(
            this GenesisContract genesis, string caller = "")
        {
            if (caller == "")
                caller = genesis.CallAddress;
            
            var tokenConverter = genesis.GetContractAddressByName(NameProvider.TokenConverterName);

            return new TokenConverterContract(genesis.NodeManager, caller, tokenConverter.GetFormatted());
        }

        public static TreasuryContract GetTreasuryContract(this GenesisContract genesis, string caller = "")
        {
            if (caller == "")
                caller = genesis.CallAddress;
            
            var treasury = genesis.GetContractAddressByName(NameProvider.TreasuryName);

            return new TreasuryContract(genesis.NodeManager, caller, treasury.GetFormatted());
        }

        public static VoteContract GetVoteContract(this GenesisContract genesis, string caller = "")
        {
            if (caller == "")
                caller = genesis.CallAddress;
            
            var vote = genesis.GetContractAddressByName(NameProvider.VoteName);

            return new VoteContract(genesis.NodeManager, caller, vote.GetFormatted());
        }

        public static ElectionContract GetElectionContract(this GenesisContract genesis, string caller = "")
        {
            if (caller == "")
                caller = genesis.CallAddress;
            
            var election = genesis.GetContractAddressByName(NameProvider.ElectionName);

            return new ElectionContract(genesis.NodeManager, caller, election.GetFormatted());
        }

        public static CrossChainContract GetCrossChainContract(this GenesisContract genesis, string caller = "")
        {
            if (caller == "")
                caller = genesis.CallAddress;
            
            var cross = genesis.GetContractAddressByName(NameProvider.CrossChainName);

            return new CrossChainContract(genesis.NodeManager, caller, cross.GetFormatted());
        }

        public static AssociationAuthContract GetAssociationAuthContract(
            this GenesisContract genesis, string caller = "")
        {
            if (caller == "")
                caller = genesis.CallAddress;
            
            var association = genesis.GetContractAddressByName(NameProvider.AssociationName);

            return new AssociationAuthContract(genesis.NodeManager, caller, association.GetFormatted());
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