using AElf.Automation.Common.Helpers;

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
        GetCandidateHistory,
        GetVictories,
        GetTermSnapshot,
        GetCandidates,
        GetMinersCount,
        GetVotesInformationWithRecords,
        GetVotesInformationWithAllRecords
    }
    public class ElectionContract : BaseContract<ElectionMethod>
    {
        public ElectionContract(IApiHelper ch, string callAddress, string electionAddress) :
            base(ch, electionAddress)
        {
            CallAddress = callAddress;
            UnlockAccount(CallAddress);
        }

        public ElectionContract(IApiHelper ch, string callAddress)
            :base(ch, "AElf.Contracts.Election", callAddress)
        {
        }
    }
}