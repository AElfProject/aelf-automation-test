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
        public FeeReceiverContract(IApiHelper apiHelper, string callAddress, string feeReceiverAddress) :
            base(apiHelper, feeReceiverAddress)
        {
            CallAddress = callAddress;
            UnlockAccount(CallAddress);
        }

        public FeeReceiverContract(IApiHelper apiHelper, string callAddress)
            :base(apiHelper, "AElf.Contracts.Resource.FeeReceiver", callAddress)
        {
        }
    }
}