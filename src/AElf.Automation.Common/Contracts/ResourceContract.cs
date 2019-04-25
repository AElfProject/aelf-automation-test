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
    public class ResourceContract : BaseContract
    {
        public ResourceContract(RpcApiHelper ch, string callAddress)
            :base(ch, "AElf.Contracts.Resource", callAddress)
        {
        }

        public ResourceContract(RpcApiHelper ch, string callAddress, string contractAddress) :
            base(ch, contractAddress)
        {
            CallAddress = callAddress;
            UnlockAccount(CallAddress);
        }

        public CommandInfo CallContractMethod(ResourceMethod method, IMessage inputParameter)
        {
            return ExecuteMethodWithResult(method.ToString(), inputParameter);
        }

        public void CallContractWithoutResult(ResourceMethod method, IMessage inputParameter)
        {
            ExecuteMethodWithTxId(method.ToString(), inputParameter);
        }

        public JObject CallReadOnlyMethod(ResourceMethod method, IMessage inputParameter)
        {
            return CallViewMethod(method.ToString(), inputParameter);
        }

    }
}
