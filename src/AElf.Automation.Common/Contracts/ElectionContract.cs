using AElf.Automation.Common.Helpers;
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
        GetVictories,
        GetTermSnapshot,
        GetMinersCount,
        GetVotesInformationWithRecords,
        GetElectorVoteWithAllRecords
    }

    public class ElectionContract : BaseContract<ElectionMethod>
    {
        public ElectionContract(IApiHelper apiHelper, string callAddress, string electionAddress) :
            base(apiHelper, electionAddress)
        {
            CallAddress = callAddress;
            UnlockAccount(CallAddress);
        }

        public ElectionContract(IApiHelper apiHelper, string callAddress)
            : base(apiHelper, "AElf.Contracts.Election", callAddress)
        {
        }

        public CandidateInformation GetCandidateInformation(string account)
        {
            var result =
                CallViewMethod<CandidateInformation>(ElectionMethod.GetCandidateInformation,
                    new StringInput
                    {
                        Value = ApiHelper.GetPublicKeyFromAddress(account)
                    });
            return result;
        }
    }
}