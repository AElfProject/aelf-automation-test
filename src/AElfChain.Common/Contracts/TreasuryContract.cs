using Acs10;
using AElf.Contracts.TestContract.BasicSecurity;
using AElf.Contracts.Treasury;
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
        GetDividendPoolWeightProportion,
        GetDividends
    }

    public class TreasuryContract : BaseContract<TreasuryMethod>
    {
        public TreasuryContract(INodeManager nodeManager, string callAddress, string contractAddress) :
            base(nodeManager, contractAddress)
        {
            SetAccount(callAddress);
        }
        
        
        public Dividends GetCurrentTreasuryBalance()
        {
            var result = CallViewMethod<Dividends>(TreasuryMethod.GetUndistributedDividends,new Empty());
            return result;
        }
        
        public Dividends GetDividends(long height)
        {
            var result = CallViewMethod<Dividends>(TreasuryMethod.GetDividends,new Int64Value{Value = height});
            return result;
        }
        
        public MinerRewardWeightProportion GetMinerRewardWeightProportion()
        {
            var result = CallViewMethod<MinerRewardWeightProportion>(TreasuryMethod.GetMinerRewardWeightProportion,new Empty());
            return result;
        }
        
        public DividendPoolWeightProportion GetDividendPoolWeightProportion()
        {
            var result = CallViewMethod<DividendPoolWeightProportion>(TreasuryMethod.GetDividendPoolWeightProportion,new Empty());
            return result;
        }
    }
}