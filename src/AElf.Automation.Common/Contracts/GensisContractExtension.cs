namespace AElf.Automation.Common.Contracts
{
    public static class GensisContractExtension
    {
        public static ConsensusContract GetConsensusContract(this GenesisContract genesis)
        {
            var consensus = genesis.GetContractAddressByName(NameProvider.ConsensusName);

            return new ConsensusContract(genesis.NodeManager, genesis.CallAddress, consensus.GetFormatted());
        }

        public static FeeReceiverContract GetFeeReceiverContract(
            this GenesisContract genesis)
        {
            var feeReceiver = genesis.GetContractAddressByName(NameProvider.FeeReceiverName);

            return new FeeReceiverContract(genesis.NodeManager, genesis.CallAddress, feeReceiver.GetFormatted());
        }

        public static ParliamentAuthContract GetParliamentAuthContract(
            this GenesisContract genesis)
        {
            var parliamentAuth = genesis.GetContractAddressByName(NameProvider.ParliamentName);

            return new ParliamentAuthContract(genesis.NodeManager, genesis.CallAddress, parliamentAuth.GetFormatted());
        }

        public static ProfitContract GetProfitContract(this GenesisContract genesis)
        {
            var profit = genesis.GetContractAddressByName(NameProvider.ProfitName);

            return new ProfitContract(genesis.NodeManager, genesis.CallAddress, profit.GetFormatted());
        }

        public static TokenContract GetTokenContract(this GenesisContract genesis)
        {
            var token = genesis.GetContractAddressByName(NameProvider.TokenName);

            return new TokenContract(genesis.NodeManager, genesis.CallAddress, token.GetFormatted());
        }

        public static TokenConverterContract GetTokenConverterContract(
            this GenesisContract genesis)
        {
            var tokenConverter = genesis.GetContractAddressByName(NameProvider.TokenConverterName);

            return new TokenConverterContract(genesis.NodeManager, genesis.CallAddress, tokenConverter.GetFormatted());
        }

        public static TreasuryContract GetTreasuryContract(this GenesisContract genesis)
        {
            var treasury = genesis.GetContractAddressByName(NameProvider.TreasuryName);

            return new TreasuryContract(genesis.NodeManager, genesis.CallAddress, treasury.GetFormatted());
        }

        public static VoteContract GetVoteContract(this GenesisContract genesis)
        {
            var vote = genesis.GetContractAddressByName(NameProvider.VoteName);

            return new VoteContract(genesis.NodeManager, genesis.CallAddress, vote.GetFormatted());
        }

        public static ElectionContract GetElectionContract(this GenesisContract genesis)
        {
            var election = genesis.GetContractAddressByName(NameProvider.ElectionName);

            return new ElectionContract(genesis.NodeManager, genesis.CallAddress, election.GetFormatted());
        }

        public static CrossChainContract GetCrossChainContract(this GenesisContract genesis)
        {
            var cross = genesis.GetContractAddressByName(NameProvider.CrossChainName);

            return new CrossChainContract(genesis.NodeManager, genesis.CallAddress, cross.GetFormatted());
        }

        public static AssociationAuthContract GetAssociationAuthContract(
            this GenesisContract genesis)
        {
            var association = genesis.GetContractAddressByName(NameProvider.AssociationName);

            return new AssociationAuthContract(genesis.NodeManager, genesis.CallAddress, association.GetFormatted());
        }

        public static ConfigurationContract GetConfigurationContract(this GenesisContract genesis)
        {
            var configuration = genesis.GetContractAddressByName(NameProvider.Configuration);

            return new ConfigurationContract(genesis.NodeManager, genesis.CallAddress, configuration.GetFormatted());
        }
    }
}