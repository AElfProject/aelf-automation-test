using AElf.Contracts.Consensus.AEDPoS;
using AElf.Contracts.Election;
using AElf.Contracts.MultiToken;
using AElf.Contracts.Profit;
using AElfChain.Common;
using AElfChain.Common.Contracts;
using Google.Protobuf.WellKnownTypes;
using PubkeyList = AElf.Contracts.Election.PubkeyList;

namespace AElf.Automation.EconomicSystemTest
{
    public partial class Behaviors
    {
        #region Profit View Method

        // return the hash of Profit Items(Treasury,MinierReward,BackupSubsidy,CitizaWelfare,BasicReward,VotesWeight,ReElectionReward)
        public CreatedSchemeIds GetCreatedProfitItems()
        {
            var result = ProfitService.CallViewMethod<CreatedSchemeIds>(ProfitMethod.GetManagingSchemeIds,
                new GetManagingSchemeIdsInput
                {
                    Manager = ContractManager.Genesis.GetContractAddressByName(NameProvider.Treasury)
                });
            return result;
        }

        #endregion

        #region Token Method

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

        #region Election View Methods

        public PubkeyList GetVictories()
        {
            var result = ElectionService.CallViewMethod<PubkeyList>(ElectionMethod.GetVictories,
                new Empty());

            return result;
        }

        public int GetMinersCount()
        {
            return ElectionService.CallViewMethod<Int32Value>(ElectionMethod.GetMinersCount,
                new Empty()).Value;
        }

        public CandidateInformation GetCandidateInformation(string account)
        {
            var result =
                ElectionService.CallViewMethod<CandidateInformation>(ElectionMethod.GetCandidateInformation,
                    new StringValue
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
                ElectionService.CallViewMethod<ElectorVote>(ElectionMethod.GetVotesInformation, new StringValue
                {
                    Value = NodeManager.GetAccountPublicKey(voteAccount)
                });

            return result;
        }

        public ElectorVote GetVotesInformationWithRecords(string voteAccount)
        {
            var result = ElectionService.CallViewMethod<ElectorVote>(ElectionMethod.GetVotesInformationWithRecords,
                new StringValue
                {
                    Value = NodeManager.GetAccountPublicKey(voteAccount)
                });
            return result;
        }

        public ElectorVote GetElectorVoteWithAllRecords(string voteAccount)
        {
            var result = ElectionService.CallViewMethod<ElectorVote>(ElectionMethod.GetElectorVoteWithAllRecords,
                new StringValue
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

        #region Vote Method

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