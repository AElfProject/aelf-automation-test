using AElf.Contracts.Election;
using AElf.Types;
using AElfChain.Common.Managers;
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
        ChangeVotingOption,

        //view
        GetCalculateVoteWeight,
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
        GetNextElectCountDown,
        GetElectorVoteWithRecords,
        GetElectorVote,
        GetVoteWeightSetting,
        GetVoteWeightProportion,
        GetDataCenterRankingList,
        GetMinerElectionVotingItemId
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

        public Hash GetMinerElectionVotingItemId()
        {
            var minerElectionVotingItemId = CallViewMethod<Hash>(ElectionMethod.GetMinerElectionVotingItemId, new Empty());

            return minerElectionVotingItemId;
        }
    }
}