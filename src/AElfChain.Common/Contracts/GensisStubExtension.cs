using AElf.Contracts.AssociationAuth;
using AElf.Contracts.Configuration;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Contracts.CrossChain;
using AElf.Contracts.Election;
using AElf.Contracts.MultiToken;
using AElf.Contracts.ParliamentAuth;
using AElf.Contracts.Profit;
using AElf.Contracts.ReferendumAuth;
using AElf.Contracts.TokenConverter;
using AElf.Contracts.Treasury;
using AElf.Contracts.Vote;

namespace AElfChain.Common.Contracts
{
    public static class GensisStubExtension
    {
        public static AEDPoSContractContainer.AEDPoSContractStub GetConsensusStub(this GenesisContract genesis,
            string caller = "")
        {
            if (caller == "")
                caller = genesis.CallAddress;

            var consensus = genesis.GetContractAddressByName(NameProvider.Consensus);

            var contract = new ConsensusContract(genesis.NodeManager, caller, consensus.GetFormatted());

            return contract.GetTestStub<AEDPoSContractContainer.AEDPoSContractStub>(caller);
        }

        public static AEDPoSContractImplContainer.AEDPoSContractImplStub GetConsensusImplStub(
            this GenesisContract genesis, string caller = "")
        {
            if (caller == "")
                caller = genesis.CallAddress;

            var consensus = genesis.GetContractAddressByName(NameProvider.Consensus);

            var contract = new ConsensusContract(genesis.NodeManager, caller, consensus.GetFormatted());

            return contract.GetTestStub<AEDPoSContractImplContainer.AEDPoSContractImplStub>(caller);
        }

        public static ParliamentAuthContractContainer.ParliamentAuthContractStub GetParliamentAuthStub(
            this GenesisContract genesis, string caller = "")
        {
            if (caller == "")
                caller = genesis.CallAddress;

            var parliamentAuth = genesis.GetContractAddressByName(NameProvider.ParliamentAuth);

            var contract =
                new ParliamentAuthContract(genesis.NodeManager, caller, parliamentAuth.GetFormatted());

            return contract
                .GetTestStub<ParliamentAuthContractContainer.ParliamentAuthContractStub>(caller);
        }

        public static ProfitContractContainer.ProfitContractStub GetProfitStub(this GenesisContract genesis,
            string caller = "")
        {
            if (caller == "")
                caller = genesis.CallAddress;

            var profit = genesis.GetContractAddressByName(NameProvider.Profit);

            var contract = new ProfitContract(genesis.NodeManager, caller, profit.GetFormatted());

            return contract.GetTestStub<ProfitContractContainer.ProfitContractStub>(caller);
        }

        public static TokenContractContainer.TokenContractStub GetTokenStub(this GenesisContract genesis,
            string caller = "")
        {
            if (caller == "")
                caller = genesis.CallAddress;

            var token = genesis.GetContractAddressByName(NameProvider.Token);

            var contract = new TokenContract(genesis.NodeManager, caller, token.GetFormatted());

            return contract.GetTestStub<TokenContractContainer.TokenContractStub>(caller);
        }

        public static TokenConverterContractContainer.TokenConverterContractStub GetTokenConverterStub(
            this GenesisContract genesis, string caller = "")
        {
            if (caller == "")
                caller = genesis.CallAddress;

            var tokenConverter = genesis.GetContractAddressByName(NameProvider.TokenConverter);

            var contract =
                new TokenConverterContract(genesis.NodeManager, caller, tokenConverter.GetFormatted());

            return contract
                .GetTestStub<TokenConverterContractContainer.TokenConverterContractStub>(caller);
        }

        public static TreasuryContractContainer.TreasuryContractStub GetTreasuryStub(this GenesisContract genesis,
            string caller = "")
        {
            if (caller == "")
                caller = genesis.CallAddress;

            var treasury = genesis.GetContractAddressByName(NameProvider.Treasury);

            var contract = new TreasuryContract(genesis.NodeManager, caller, treasury.GetFormatted());

            return contract.GetTestStub<TreasuryContractContainer.TreasuryContractStub>(caller);
        }

        public static VoteContractContainer.VoteContractStub GetVoteStub(this GenesisContract genesis,
            string caller = "")
        {
            if (caller == "")
                caller = genesis.CallAddress;

            var vote = genesis.GetContractAddressByName(NameProvider.Vote);

            var contract = new VoteContract(genesis.NodeManager, caller, vote.GetFormatted());

            return contract.GetTestStub<VoteContractContainer.VoteContractStub>(caller);
        }

        public static ElectionContractContainer.ElectionContractStub GetElectionStub(this GenesisContract genesis,
            string caller = "")
        {
            if (caller == "")
                caller = genesis.CallAddress;

            var election = genesis.GetContractAddressByName(NameProvider.Election);

            var contract = new ElectionContract(genesis.NodeManager, caller, election.GetFormatted());

            return contract.GetTestStub<ElectionContractContainer.ElectionContractStub>(caller);
        }

        public static CrossChainContractContainer.CrossChainContractStub GetCrossChainStub(this GenesisContract genesis,
            string caller = "")
        {
            if (caller == "")
                caller = genesis.CallAddress;

            var cross = genesis.GetContractAddressByName(NameProvider.CrossChain);

            var contract = new CrossChainContract(genesis.NodeManager, caller, cross.GetFormatted());

            return contract.GetTestStub<CrossChainContractContainer.CrossChainContractStub>(caller);
        }

        public static AssociationAuthContractContainer.AssociationAuthContractStub GetAssociationAuthStub(
            this GenesisContract genesis, string caller = "")
        {
            if (caller == "")
                caller = genesis.CallAddress;

            var association = genesis.GetContractAddressByName(NameProvider.AssociationAuth);

            var contract =
                new AssociationAuthContract(genesis.NodeManager, caller, association.GetFormatted());

            return contract.GetTestStub<AssociationAuthContractContainer.AssociationAuthContractStub>(caller);
        }

        public static ReferendumAuthContractContainer.ReferendumAuthContractStub GetReferendumAuthStub(
            this GenesisContract genesis, string caller = "")
        {
            if (caller == "")
                caller = genesis.CallAddress;

            var referendumAuth = genesis.GetContractAddressByName(NameProvider.ReferendumAuth);

            var contract =
                new ReferendumAuthContract(genesis.NodeManager, caller, referendumAuth.GetFormatted());

            return contract
                .GetTestStub<ReferendumAuthContractContainer.ReferendumAuthContractStub>(caller);
        }

        public static ConfigurationContainer.ConfigurationStub GetConfigurationStub(this GenesisContract genesis,
            string caller = "")
        {
            if (caller == "")
                caller = genesis.CallAddress;

            var configuration = genesis.GetContractAddressByName(NameProvider.Configuration);

            var contract =
                new ConfigurationContract(genesis.NodeManager, caller, configuration.GetFormatted());

            return contract.GetTestStub<ConfigurationContainer.ConfigurationStub>(caller);
        }
    }
}