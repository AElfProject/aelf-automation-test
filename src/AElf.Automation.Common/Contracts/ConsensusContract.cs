using AElf.Automation.Common.Helpers;

namespace AElf.Automation.Common.Contracts
{
    public enum ConsensusMethod
    {
        GetRoundInfo,
        GetCurrentTermNumber,
        IsCandidate,
        GetVotesCount,
        GetTicketsCount,
        GetCandidatesList,
        GetCandidateHistoryInformation,
        GetCurrentMiners,
        GetCurrentRoundInformation,
        GetTicketsInfo,
        GetPageableElectionInfo,
        GetBlockchainAge,
        GetCurrentVictories,
        GetTermSnapshot,
        GetTermNumberByRoundNumber,
        QueryAliasesInUse,
        QueryCurrentDividends,
        QueryCurrentDividendsForVoters,
        QueryMinedBlockCountInCurrentTerm,
        GetAvailableDividends,

        AnnounceElection,
        QuitElection,
        Vote,
        ReceiveAllDividends,
        WithdrawAll,
        InitialBalance
    }
    public class ConsensusContract : BaseContract<ConsensusMethod>
    {
        public ConsensusContract(IApiHelper apiHelper, string callAddress, string consensusAddress) :
            base(apiHelper, consensusAddress)
        {
            CallAddress = callAddress;
            UnlockAccount(CallAddress);
        }

        public ConsensusContract(IApiHelper apiHelper, string callAddress)
            :base(apiHelper, "AElf.Contracts.Consensus", callAddress)
        {
        }
    }
}
