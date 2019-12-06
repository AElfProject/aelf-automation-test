using AElfChain.Common.Managers;
using AElf.Contracts.Election;
using Google.Protobuf.WellKnownTypes;

namespace AElfChain.Common.Contracts
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
                    new StringValue
                    {
                        Value = NodeManager.GetAccountPublicKey(account)
                    });
            return result;
        }

        public long GetCandidateVoteCount(string candidatePublicKey)
        {
            var candidateVote = CallViewMethod<CandidateVote>(ElectionMethod.GetCandidateVote, new StringValue
            {
                Value = candidatePublicKey
            });

            return candidateVote.AllObtainedVotedVotesAmount;
        }
    }
}