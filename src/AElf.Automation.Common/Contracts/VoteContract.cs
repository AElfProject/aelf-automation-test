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
        public VoteContract(RpcApiHelper ch, string callAddress) :
            base(ch, "AElf.Contracts.Vote", callAddress)
        {
        }

        public VoteContract(RpcApiHelper ch, string callAddress, string contractAddress):
            base(ch, contractAddress)
        {
            CallAddress = callAddress;
            UnlockAccount(CallAddress);
        }
    }
}