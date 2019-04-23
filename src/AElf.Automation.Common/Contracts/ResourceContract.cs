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
        public ResourceContract(CliHelper ch, string address)
            :base(ch, "AElf.Contracts.Resource", address)
        {
        }

        public ResourceContract(CliHelper ch, string address, string contractAbi) :
            base(ch, contractAbi)
        {
            Address = address;
            UnlockAccount(Address);
        }

        public CommandInfo CallContractMethod(ResourceMethod method, IMessage inputParameter)
        {
            return ExecuteContractMethodWithResult(method.ToString(), inputParameter);
        }

        public void CallContractWithoutResult(ResourceMethod method, IMessage inputParameter)
        {
            ExecuteContractMethod(method.ToString(), inputParameter);
        }

        public JObject CallReadOnlyMethod(ResourceMethod method, IMessage inputParameter)
        {
            return CallContractViewMethod(method.ToString(), inputParameter);
        }

    }
}
