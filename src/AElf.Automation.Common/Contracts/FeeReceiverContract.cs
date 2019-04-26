using AElf.Automation.Common.Helpers;

namespace AElf.Automation.Common.Contracts
{
    public enum FeeReceiverMethod
    {
        //action
        Initialize,
        Withdraw,
        WithdrawAll,
        Burn,
        
        //view
        GetElfTokenAddress,
        GetFoundationAddress,
        GetOwedToFoundation
    }
    
    public class FeeReceiverContract : BaseContract<FeeReceiverMethod>
    {
        public FeeReceiverContract(RpcApiHelper ch, string callAddress, string feeReceiverAddress) :
            base(ch, feeReceiverAddress)
        {
            CallAddress = callAddress;
            UnlockAccount(CallAddress);
        }

        public FeeReceiverContract(RpcApiHelper ch, string callAddress)
            :base(ch, "AElf.Contracts.Resource.FeeReceiver", callAddress)
        {
        }
    }
}