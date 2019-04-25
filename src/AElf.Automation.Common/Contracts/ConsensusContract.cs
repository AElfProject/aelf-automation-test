using AElf.Automation.Common.Helpers;
using Google.Protobuf;
using Newtonsoft.Json.Linq;

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
        public ConsensusContract(RpcApiHelper ch, string callAddress, string consensusAddress) :
            base(ch, consensusAddress)
        {
            CallAddress = callAddress;
            UnlockAccount(CallAddress);
        }

        public ConsensusContract(RpcApiHelper ch, string callAddress)
            :base(ch, "AElf.Contracts.Consensus", callAddress)
        {
        }
    }
}
