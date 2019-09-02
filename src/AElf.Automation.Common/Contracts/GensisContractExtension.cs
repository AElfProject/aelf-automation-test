namespace AElf.Automation.Common.Contracts
{
    public static class GensisContractExtension
    {
        public static ConsensusContract GetConsensusContract(this GenesisContract genesis)
        {
            var consensus = genesis.GetContractAddressByName(NameProvider.ConsensusName);

            return new ConsensusContract(genesis.ApiHelper, genesis.CallAddress, consensus.GetFormatted());
        }
        
        public static FeeReceiverContract GetFeeReceiverContract(
            this GenesisContract genesis)
        {
            var feeReceiver = genesis.GetContractAddressByName(NameProvider.FeeReceiverName);
            
            return new FeeReceiverContract(genesis.ApiHelper, genesis.CallAddress, feeReceiver.GetFormatted());
        }

        public static ParliamentAuthContract GetParliamentAuthContract(
            this GenesisContract genesis)
        {
            var parliamentAuth = genesis.GetContractAddressByName(NameProvider.ParliamentName);
            
            return new ParliamentAuthContract(genesis.ApiHelper, genesis.CallAddress, parliamentAuth.GetFormatted());
        }

        public static ProfitContract GetProfitContract(this GenesisContract genesis)
        {
            var profit = genesis.GetContractAddressByName(NameProvider.ProfitName);
            
            return new ProfitContract(genesis.ApiHelper, genesis.CallAddress, profit.GetFormatted());
        }

        public static TokenContract GetTokenContract(this GenesisContract genesis)
        {
            var token = genesis.GetContractAddressByName(NameProvider.TokenName);
            
            return new TokenContract(genesis.ApiHelper, genesis.CallAddress, token.GetFormatted());
        }

        public static TokenConverterContract GetTokenConverterContract(
            this GenesisContract genesis)
        {
            var tokenConverter = genesis.GetContractAddressByName(NameProvider.TokenConverterName);
            
            return new TokenConverterContract(genesis.ApiHelper, genesis.CallAddress, tokenConverter.GetFormatted());
        }

        public static TreasuryContract GetTreasuryContract(this GenesisContract genesis)
        {
            var treasury = genesis.GetContractAddressByName(NameProvider.TreasuryName);
            
            return new TreasuryContract(genesis.ApiHelper, genesis.CallAddress, treasury.GetFormatted());
        }

        public static VoteContract GetVoteContract(this GenesisContract genesis)
        {
            var vote = genesis.GetContractAddressByName(NameProvider.VoteName);
            
            return new VoteContract(genesis.ApiHelper, genesis.CallAddress, vote.GetFormatted());
        }

        public static ElectionContract GetElectionContract(this GenesisContract genesis)
        {
            var election = genesis.GetContractAddressByName(NameProvider.ElectionName);
            
            return new ElectionContract(genesis.ApiHelper, genesis.CallAddress, election.GetFormatted());
        }

        public static CrossChainContract GetCrossChainContract(this GenesisContract genesis)
        {
            var cross = genesis.GetContractAddressByName(NameProvider.CrossChainName);
            
            return new CrossChainContract(genesis.ApiHelper, genesis.CallAddress, cross.GetFormatted());
        }

        public static AssociationAuthContract GetAssociationAuthContract(
            this GenesisContract genesis)
        {
            var association = genesis.GetContractAddressByName(NameProvider.AssociationName);
            
            return new AssociationAuthContract(genesis.ApiHelper, genesis.CallAddress, association.GetFormatted());
        }

        public static ConfigurationContract GetConfigurationContract(this GenesisContract genesis)
        {
            var configuration = genesis.GetContractAddressByName(NameProvider.Configuration);
            
            return new ConfigurationContract(genesis.ApiHelper, genesis.CallAddress, configuration.GetFormatted());
        }
    }
}