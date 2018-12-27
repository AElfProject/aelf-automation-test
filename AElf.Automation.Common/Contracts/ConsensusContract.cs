using System;
using System.Collections.Generic;
using System.Text;
using AElf.Automation.Common.Extensions;
using AElf.Automation.Common.Helpers;
using Newtonsoft.Json.Linq;

namespace AElf.Automation.Common.Contracts
{
    public enum ConsensusMethod
    {
        GetRoundInfo,
        GetCurrentTermNumber,
        IsCandidate,
        GetCandidatesListToFriendlyString,
        GetCandidateHistoryInfoToFriendlyString,
        GetCurrentMinersToFriendlyString,
        GetTicketsInfoToFriendlyString,
        GetCurrentElectionInfoToFriendlyString,
        GetBlockchainAge,
        GetCurrentVictoriesToFriendlyString,
        GetTermSnapshotToFriendlyString,
        GetTermNumberByRoundNumber,
        QueryAliasesInUseToFriendlyString,
        QueryCurrentDividendsForVoters,

        AnnounceElection,
        QuitElection,
        Vote,
        GetAllDividends,
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

        public CommandInfo CallContractMethod(ConsensusMethod method, params string[] paramsArray)
        {
            return ExecuteContractMethodWithResult(method.ToString(), paramsArray);
        }

        public void CallContractWithoutResult(ConsensusMethod method, params string[] paramsArray)
        {
            ExecuteContractMethod(method.ToString(), paramsArray);
        }

        public JObject CallReadOnlyMethod(ConsensusMethod method, params string[] paramsArray)
        {
            return CallContractViewMethod(method.ToString(), paramsArray);
        }
    }
}
