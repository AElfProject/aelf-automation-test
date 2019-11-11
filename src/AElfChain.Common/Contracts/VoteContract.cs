using AElfChain.Common.Managers;

namespace AElfChain.Common.Contracts
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
        GetVotingItem,
        GetVotingHistory
    }

    public class VoteContract : BaseContract<VoteMethod>
    {
        public VoteContract(INodeManager nodeManager, string callAddress) :
            base(nodeManager, "AElf.Contracts.Vote", callAddress)
        {
        }

        public VoteContract(INodeManager nodeManager, string callAddress, string contractAddress) :
            base(nodeManager, contractAddress)
        {
            SetAccount(callAddress);
        }
    }
}