using AElf.Automation.Common.Extensions;
using AElf.Automation.Common.Helpers;

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
        public ResourceContract(CliHelper ch, string account)
            :base(ch, "AElf.Contracts.Resource", account)
        {
        }

        public ResourceContract(CliHelper ch, string account, string contractAbi) :
            base(ch, contractAbi)
        {
            Account = account;
        }

        public CommandInfo CallContractMethod(ResourceMethod method, params string[] paramArray)
        {
            return ExecuteContractMethodWithResult(method.ToString(), paramArray);
        }

        public void CallContractWithoutResult(ResourceMethod method, params string[] paramsArray)
        {
            ExecuteContractMethod(method.ToString(), paramsArray);
        }

    }
}
