using AElf.Contracts.AssociationAuth;
using AElf.Contracts.Configuration;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Contracts.CrossChain;
using AElf.Contracts.Election;
using AElf.Contracts.MultiToken;
using AElf.Contracts.ParliamentAuth;
using AElf.Contracts.Profit;
using AElf.Contracts.Resource.FeeReceiver;
using AElf.Contracts.TokenConverter;
using AElf.Contracts.Treasury;
using AElf.Contracts.Vote;

namespace AElf.Automation.Common.Contracts
{
    public static class GensisContractExtension
    {
        public static AEDPoSContractContainer.AEDPoSContractStub GetConsensusStub(this GenesisContract genesis)
        {
            var consensus = genesis.GetContractAddressByName(NameProvider.ConsensusName);
            
            var contract = new ConsensusContract(genesis.ApiHelper, consensus.GetFormatted());

            return contract.GetTestStub<AEDPoSContractContainer.AEDPoSContractStub>(genesis.CallAddress);
        }

        public static FeeReceiverContractContainer.FeeReceiverContractStub GetFeeReceiverStub(
            this GenesisContract genesis)
        {
            var feeReceiver = genesis.GetContractAddressByName(NameProvider.FeeReceiverName);
            
            var contract = new ConsensusContract(genesis.ApiHelper, feeReceiver.GetFormatted());

            return contract.GetTestStub<FeeReceiverContractContainer.FeeReceiverContractStub>(genesis.CallAddress);
        }

        public static ParliamentAuthContractContainer.ParliamentAuthContractStub GetParliamentAuthStub(
            this GenesisContract genesis)
        {
            var parliamentAuth = genesis.GetContractAddressByName(NameProvider.ParliamentName);
            
            var contract = new ConsensusContract(genesis.ApiHelper, parliamentAuth.GetFormatted());

            return contract.GetTestStub<ParliamentAuthContractContainer.ParliamentAuthContractStub>(genesis.CallAddress);
        }

        public static ProfitContractContainer.ProfitContractStub GetProfitStub(this GenesisContract genesis)
        {
            var profit = genesis.GetContractAddressByName(NameProvider.ProfitName);
            
            var contract = new ConsensusContract(genesis.ApiHelper, profit.GetFormatted());

            return contract.GetTestStub<ProfitContractContainer.ProfitContractStub>(genesis.CallAddress);
        }

        public static TokenContractContainer.TokenContractStub GetTokenStub(this GenesisContract genesis)
        {
            var token = genesis.GetContractAddressByName(NameProvider.TokenName);
            
            var contract = new ConsensusContract(genesis.ApiHelper, token.GetFormatted());

            return contract.GetTestStub<TokenContractContainer.TokenContractStub>(genesis.CallAddress);
        }

        public static TokenConverterContractContainer.TokenConverterContractStub GetTokenConverterStub(
            this GenesisContract genesis)
        {
            var tokenConverter = genesis.GetContractAddressByName(NameProvider.TokenConverterName);
            
            var contract = new ConsensusContract(genesis.ApiHelper, tokenConverter.GetFormatted());

            return contract.GetTestStub<TokenConverterContractContainer.TokenConverterContractStub>(genesis.CallAddress);
        }

        public static TreasuryContractContainer.TreasuryContractStub GetTreasuryStub(this GenesisContract genesis)
        {
            var treasury = genesis.GetContractAddressByName(NameProvider.TreasuryName);
            
            var contract = new ConsensusContract(genesis.ApiHelper, treasury.GetFormatted());

            return contract.GetTestStub<TreasuryContractContainer.TreasuryContractStub>(genesis.CallAddress);
        }

        public static VoteContractContainer.VoteContractStub GetVoteStub(this GenesisContract genesis)
        {
            var vote = genesis.GetContractAddressByName(NameProvider.VoteName);
            
            var contract = new ConsensusContract(genesis.ApiHelper, vote.GetFormatted());

            return contract.GetTestStub<VoteContractContainer.VoteContractStub>(genesis.CallAddress);
        }

        public static ElectionContractContainer.ElectionContractStub GetElectionStub(this GenesisContract genesis)
        {
            var election = genesis.GetContractAddressByName(NameProvider.ElectionName);
            
            var contract = new ConsensusContract(genesis.ApiHelper, election.GetFormatted());

            return contract.GetTestStub<ElectionContractContainer.ElectionContractStub>(genesis.CallAddress);
        }

        public static CrossChainContractContainer.CrossChainContractStub GetCrossChainStub(this GenesisContract genesis)
        {
            var cross = genesis.GetContractAddressByName(NameProvider.CrossChainName);
            
            var contract = new ConsensusContract(genesis.ApiHelper, cross.GetFormatted());

            return contract.GetTestStub<CrossChainContractContainer.CrossChainContractStub>(genesis.CallAddress);
        }

        public static AssociationAuthContractContainer.AssociationAuthContractStub GetAssociationAuthStub(
            this GenesisContract genesis)
        {
            var association = genesis.GetContractAddressByName(NameProvider.AssociationName);
            
            var contract = new ConsensusContract(genesis.ApiHelper, association.GetFormatted());

            return contract.GetTestStub<AssociationAuthContractContainer.AssociationAuthContractStub>(genesis.CallAddress);
        }

        public static ConfigurationContainer.ConfigurationStub GetConfigurationStub(this GenesisContract genesis)
        {
            var configuration = genesis.GetContractAddressByName(NameProvider.Configuration);
            
            var contract = new ConsensusContract(genesis.ApiHelper, configuration.GetFormatted());

            return contract.GetTestStub<ConfigurationContainer.ConfigurationStub>(genesis.CallAddress);
        }
    }
}