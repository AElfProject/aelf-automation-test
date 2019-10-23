using AElf.Automation.Common.Managers;
using AElf.Contracts.Election;

namespace AElf.Automation.Common.Contracts
{
    public enum ElectionMethod
    {
        //action
        InitialElectionContract,
        AnnounceElection,
        QuitElection,
        Vote,
        Withdraw,
        UpdateTermNumber,

        //view
        GetElectionResult,
        GetVotesInformation,
        GetCandidateInformation,
        GetCandidates,
        GetCandidateVote,
        GetVotedCandidates,
        GetCandidateVoteWithRecords,
        GetCandidateVoteWithAllRecords,
        GetVictories,
        GetTermSnapshot,
        GetMinersCount,
        GetVotesInformationWithRecords,
        GetElectorVoteWithAllRecords,
        GetNextElectCountDown
    }

    public class ElectionContract : BaseContract<ElectionMethod>
    {
        public ElectionContract(INodeManager nodeManager, string callAddress, string electionAddress)
            : base(nodeManager, electionAddress)
        {
            SetAccount(callAddress);
        }

        public ElectionContract(INodeManager nodeManager, string callAddress)
            : base(nodeManager, ContractFileName, callAddress)
        {
        }

        public static string ContractFileName => "AElf.Contracts.Election";

        public CandidateInformation GetCandidateInformation(string account)
        {
            var result =
                CallViewMethod<CandidateInformation>(ElectionMethod.GetCandidateInformation,
                    new StringInput
                    {
                        Value = NodeManager.GetAccountPublicKey(account)
                    });
            return result;
        }
    }
}