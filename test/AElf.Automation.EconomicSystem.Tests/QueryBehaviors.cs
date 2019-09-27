using AElf.Automation.Common;
using AElf.Automation.Common.Contracts;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Contracts.Election;
using AElf.Contracts.MultiToken;
using AElf.Contracts.Profit;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Automation.EconomicSystem.Tests
{
    public partial class Behaviors
    {
        #region Election View Methods

        public PubkeyList GetVictories()
        {
            var result = ElectionService.CallViewMethod<PubkeyList>(ElectionMethod.GetVictories,
                new Empty());

            return result;
        }

        public int GetMinersCount()
        {
            return ElectionService.CallViewMethod<SInt32Value>(ElectionMethod.GetMinersCount,
                new Empty()).Value;
        }

        public CandidateInformation GetCandidateInformation(string account)
        {
            var result =
                ElectionService.CallViewMethod<CandidateInformation>(ElectionMethod.GetCandidateInformation,
                    new StringInput
                    {
                        Value = NodeManager.GetAccountPublicKey(account)
                    });
            return result;
        }

        public PubkeyList GetCandidates()
        {
            var result =
                ElectionService.CallViewMethod<PubkeyList>(ElectionMethod.GetCandidates,
                    new Empty());

            return result;
        }

        public ElectorVote GetVotesInformation(string voteAccount)
        {
            var result =
                ElectionService.CallViewMethod<ElectorVote>(ElectionMethod.GetVotesInformation, new StringInput
                {
                    Value = NodeManager.GetAccountPublicKey(voteAccount)
                });

            return result;
        }

        public ElectorVote GetVotesInformationWithRecords(string voteAccount)
        {
            var result = ElectionService.CallViewMethod<ElectorVote>(ElectionMethod.GetVotesInformationWithRecords,
                new StringInput
                {
                    Value = NodeManager.GetAccountPublicKey(voteAccount)
                });
            return result;
        }

        public ElectorVote GetElectorVoteWithAllRecords(string voteAccount)
        {
            var result = ElectionService.CallViewMethod<ElectorVote>(ElectionMethod.GetElectorVoteWithAllRecords,
                new StringInput
                {
                    Value = NodeManager.GetAccountPublicKey(voteAccount)
                });
            return result;
        }

        public TermSnapshot GetTermSnapshot(long termNumber)
        {
            var result = ElectionService.CallViewMethod<TermSnapshot>(ElectionMethod.GetTermSnapshot,
                new GetTermSnapshotInput
                {
                    TermNumber = termNumber
                });
            return result;
        }

        #endregion

        #region VoteService Method

        #endregion

        #region ProfitService View Method

        // return the hash of ProfitService Items(Treasury,MinierReward,BackupSubsidy,CitizaWelfare,BasicReward,VotesWeight,ReElectionReward)
        public CreatedSchemeIds GetCreatedProfitItems()
        {
            var result = ProfitService.CallViewMethod<CreatedSchemeIds>(ProfitMethod.GetManagingSchemeIds,
                new GetManagingSchemeIdsInput
                {
                    Manager = ContractServices.GenesisService.GetContractAddressByName(NameProvider.TreasuryName)
                });
            return result;
        }

        #endregion

        #region TokenService Method

        public GetBalanceOutput GetBalance(string account, string symbol = "")
        {
            var balance = TokenService.CallViewMethod<GetBalanceOutput>(TokenMethod.GetBalance, new GetBalanceInput
            {
                Owner = AddressHelper.Base58StringToAddress(account),
                Symbol = NodeOption.GetTokenSymbol(symbol)
            });
            return balance;
        }

        #endregion

        #region Consensus view Method

        public MinerList GetCurrentMiners()
        {
            var miners = ConsensusService.CallViewMethod<MinerList>(ConsensusMethod.GetCurrentMinerList, new Empty());
            return miners;
        }

        public long GetCurrentTermInformation()
        {
            var round = ConsensusService.CallViewMethod<Round>(ConsensusMethod.GetCurrentRoundInformation, new Empty());

            return round.TermNumber;
        }

        #endregion
    }
}