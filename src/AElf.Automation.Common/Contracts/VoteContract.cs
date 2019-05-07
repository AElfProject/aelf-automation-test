using AElf.Automation.Common.Helpers;

namespace AElf.Automation.Common.Contracts
{
    public enum VoteMethod
    {
        //action
        InitialVoteContract,
        Register,
        Vote,
        Withdraw,
        UpdateEpochNumber,
        AddOption,
        RemoveOption,
        
        //view
        GetVotingResult,
        GetVotingHistories,
        GetVotingRecord,
        GetVotingEvent,
        GetVotingHistory
    }
    public class VoteContract : BaseContract<VoteMethod>
    {
        public VoteContract(IApiHelper apiHelper, string callAddress) :
            base(apiHelper, "AElf.Contracts.Vote", callAddress)
        {
        }

        public VoteContract(IApiHelper apiHelper, string callAddress, string contractAddress):
            base(apiHelper, contractAddress)
        {
            CallAddress = callAddress;
            UnlockAccount(CallAddress);
        }
    }
}