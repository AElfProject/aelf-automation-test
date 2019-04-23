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
        public ConsensusContract(CliHelper ch, string account, string consensusAbi) :
            base(ch, consensusAbi)
        {
            Account = account;
            UnlockAccount(Account);
        }

        public ConsensusContract(CliHelper ch, string account)
            :base(ch, "AElf.Contracts.Consensus", account)
        {
        }

        public CommandInfo CallContractMethod(ConsensusMethod method, IMessage inputParameter)
        {
            return ExecuteContractMethodWithResult(method.ToString(), inputParameter);
        }

        public void CallContractWithoutResult(ConsensusMethod method, IMessage inputParameter)
        {
            ExecuteContractMethod(method.ToString(), inputParameter);
        }

        public JObject CallReadOnlyMethod(ConsensusMethod method, IMessage inputParameter)
        {
            return CallContractViewMethod(method.ToString(), inputParameter);
        }
    }
}
