using AElf.Automation.Common.Contracts;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Contracts.Election;
using AElf.Contracts.MultiToken.Messages;
using AElf.Contracts.Profit;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using TermSnapshot = AElf.Contracts.Election.TermSnapshot;


namespace AElf.Automation.EconomicSystem.Tests
{
    public partial class Behaviors
    {
        #region Election View Methods

        public PublicKeysList GetVictories()
        {
            var result = ElectionService.CallViewMethod<PublicKeysList>(ElectionMethod.GetVictories,
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
                        Value = ApiHelper.GetPublicKeyFromAddress(account)
                    });
            return result;
        }

        public PublicKeysList GetCandidates()
        {
            var result =
                ElectionService.CallViewMethod<PublicKeysList>(ElectionMethod.GetCandidates,
                    new Empty());

            return result;
        }

        public ElectorVote GetVotesInformation(string voteAccount)
        {
            var result =
                ElectionService.CallViewMethod<ElectorVote>(ElectionMethod.GetVotesInformation, new StringInput
                {
                    Value = ApiHelper.GetPublicKeyFromAddress(voteAccount)
                });

            return result;
        }

        public ElectorVote GetVotesInformationWithRecords(string voteAccount)
        {
            var result = ElectionService.CallViewMethod<ElectorVote>(ElectionMethod.GetVotesInformationWithRecords,
                new StringInput
                {
                    Value = ApiHelper.GetPublicKeyFromAddress(voteAccount)
                });
            return result;
        }

        public ElectorVote GetElectorVoteWithAllRecords(string voteAccount)
        {
            var result = ElectionService.CallViewMethod<ElectorVote>(ElectionMethod.GetElectorVoteWithAllRecords,
                new StringInput
                {
                    Value = ApiHelper.GetPublicKeyFromAddress(voteAccount)
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
        public CreatedProfitItems GetCreatedProfitItems()
        {
            var result = ProfitService.CallViewMethod<CreatedProfitItems>(ProfitMethod.GetCreatedProfitItems,
                new GetCreatedProfitItemsInput
                {
                    Creator = ContractServices.GenesisService.GetContractAddressByName(NameProvider.ElectionName)
                });
            return result;
        }

        public ProfitItem GetProfitItem(string hex)
        {
            var result = ProfitService.CallViewMethod<ProfitItem>(ProfitMethod.GetProfitItem,
                new Hash()
                {
                    Value = Hash.LoadHex(hex).Value
                });
            return result;
        }

        public Address GetProfitItemVirtualAddress(Hash profitId, long period)
        {
            var result = ProfitService.CallViewMethod<Address>(ProfitMethod.GetProfitItemVirtualAddress,
                new GetProfitItemVirtualAddressInput
                {
                    ProfitId = profitId,
                    Period = period
                });

            return result;
        }

        public Address GetTreasuryAddress(Hash profitId, long period = 0)
        {
            return ProfitService.CallViewMethod<Address>(ProfitMethod.GetProfitItemVirtualAddress,
                new GetProfitItemVirtualAddressInput
                {
                    ProfitId = profitId,
                    Period = period
                });
        }

        public ProfitDetails GetProfitDetails(string voteAddress, Hash profitId)
        {
            var result =
                ProfitService.CallViewMethod<ProfitDetails>(ProfitMethod.GetProfitDetails,
                    new GetProfitDetailsInput
                    {
                        Receiver = Address.Parse(voteAddress),
                        ProfitId = profitId
                    });
            return result;
        }

        public ReleasedProfitsInformation GetReleasedProfitsInformation(Hash profitId, long period)
        {
            var result = ProfitService.CallViewMethod<ReleasedProfitsInformation>(
                ProfitMethod.GetReleasedProfitsInformation,
                new GetReleasedProfitsInformationInput
                {
                    ProfitId = profitId,
                    Period = period
                });
            return result;
        }

        #endregion

        #region TokenService Method

        public GetBalanceOutput GetBalance(string account, string symbol = "ELF")
        {
            var balance = TokenService.CallViewMethod<GetBalanceOutput>(TokenMethod.GetBalance, new GetBalanceInput
            {
                Owner = Address.Parse(account),
                Symbol = symbol
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