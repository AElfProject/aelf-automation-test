using AElf.Types;
using AElfChain.Common.Managers;
using Google.Protobuf.WellKnownTypes;

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
        SetVoteWeightInterest,

        //View
        GetTreasurySchemeId,
        GetUndistributedDividends,
        GetMinerRewardWeightProportion,
        GetDividendPoolWeightProportion
    }

    public class TreasuryContract : BaseContract<TreasuryMethod>
    {
        public TreasuryContract(INodeManager nodeManager, string callAddress, string contractAddress) :
            base(nodeManager, contractAddress)
        {
            SetAccount(callAddress);
        }
        
        
        public long GetCurrentTreasuryBalance()
        {
            var result = CallViewMethod<SInt64Value>(TreasuryMethod.GetUndistributedDividends,new Empty());
            return result.Value;
        }
    }
}