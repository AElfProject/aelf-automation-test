using AElf.Automation.Common.Helpers;
using Google.Protobuf;
using Newtonsoft.Json.Linq;

namespace AElf.Automation.Common.Contracts
{
    public enum ResourceMethod
    {
        //View
        GetElfTokenAddress,
        GetFeeAddress,
        GetResourceControllerAddress,
        GetConverter,
        GetUserBalance,
        GetExchangeBalance,
        GetElfBalance,

        //Action
        Initialize,
        IssueResource,
        BuyResource,
        SellResource,
        LockResource,
        WithdrawResource
    }
    public class ResourceContract : BaseContract<ResourceMethod>
    {
        public ResourceContract(IApiHelper ch, string callAddress)
            :base(ch, "AElf.Contracts.Resource", callAddress)
        {
        }

        public ResourceContract(IApiHelper ch, string callAddress, string contractAddress) :
            base(ch, contractAddress)
        {
            CallAddress = callAddress;
            UnlockAccount(CallAddress);
        }
    }
}
