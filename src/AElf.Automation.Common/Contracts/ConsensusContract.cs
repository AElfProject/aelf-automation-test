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
        GetCandidatesListToFriendlyString,
        GetCandidateHistoryInfoToFriendlyString,
        GetCurrentMinersToFriendlyString,
        GetTicketsInfoToFriendlyString,
        GetPageableElectionInfoToFriendlyString,
        GetBlockchainAge,
        GetCurrentVictoriesToFriendlyString,
        GetTermSnapshotToFriendlyString,
        GetTermNumberByRoundNumber,
        QueryAliasesInUseToFriendlyString,
        QueryCurrentDividends,
        QueryCurrentDividendsForVoters,
        QueryMinedBlockCountInCurrentTerm,

        AnnounceElection,
        QuitElection,
        Vote,
        ReceiveAllDividends,
        WithdrawAll,
        InitialBalance
    }
    public class ConsensusContract : BaseContract
    {
        public ConsensusContract(CliHelper ch, string callAddress, string consensusAddress) :
            base(ch, consensusAddress)
        {
            CallAddress = callAddress;
            UnlockAccount(CallAddress);
        }

        public ConsensusContract(CliHelper ch, string callAddress)
            :base(ch, "AElf.Contracts.Consensus", callAddress)
        {
        }

        public CommandInfo CallContractMethod(ConsensusMethod method, IMessage inputParameter)
        {
            return ExecuteMethodWithResult(method.ToString(), inputParameter);
        }

        public void CallContractWithoutResult(ConsensusMethod method, IMessage inputParameter)
        {
            ExecuteMethodWithTxId(method.ToString(), inputParameter);
        }

        public JObject CallReadOnlyMethod(ConsensusMethod method, IMessage inputParameter)
        {
            return CallViewMethod(method.ToString(), inputParameter);
        }
    }
}
