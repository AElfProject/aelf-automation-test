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
        public ConsensusContract(IApiHelper ch, string callAddress, string consensusAddress) :
            base(ch, consensusAddress)
        {
            CallAddress = callAddress;
            UnlockAccount(CallAddress);
        }

        public ConsensusContract(IApiHelper ch, string callAddress)
            :base(ch, "AElf.Contracts.Consensus", callAddress)
        {
        }
    }
}
