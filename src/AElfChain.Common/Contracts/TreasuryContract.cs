using AElf.Standards.ACS10;
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
        SetProfitsReceiver,

        //View
        GetTreasurySchemeId,
        GetUndistributedDividends,
        GetMinerRewardWeightProportion,
        GetDividendPoolWeightProportion,
        GetDividends,
        GetProfitsReceiver,
        GetProfitsReceiverOrDefault
    }

    public class TreasuryContract : BaseContract<TreasuryMethod>
    {
        public TreasuryContract(INodeManager nodeManager, string callAddress, string contractAddress) :
            base(nodeManager, contractAddress)
        {
            SetAccount(callAddress);
        }
        
        public Address GetProfitReceiver(string publicKey)
        {
            var result = CallViewMethod<Address>(TreasuryMethod.GetProfitsReceiver,new StringValue
            {
                Value = publicKey
            });
            return result;
        }
        
        public Address GetProfitsReceiverOrDefault(string publicKey)
        {
            var result = CallViewMethod<Address>(TreasuryMethod.GetProfitsReceiverOrDefault,new StringValue
            {
                Value = publicKey
            });
            return result;
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