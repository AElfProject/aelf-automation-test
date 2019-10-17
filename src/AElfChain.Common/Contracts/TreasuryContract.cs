using AElfChain.Common.Managers;

namespace AElfChain.Common.Contracts
{
    public enum TreasuryMethod
    {
        //Action
        InitialTreasuryContract,
        InitialMiningRewardProfitItem,
        ReleaseMiningReward,
        Release,
        Donate,
        DonateAll,

        //View
        GetTreasurySchemeId
    }

    public class TreasuryContract : BaseContract<TreasuryMethod>
    {
        public TreasuryContract(INodeManager nodeManager, string callAddress, string contractAddress) :
            base(nodeManager, contractAddress)
        {
            SetAccount(callAddress);
        }
    }
}