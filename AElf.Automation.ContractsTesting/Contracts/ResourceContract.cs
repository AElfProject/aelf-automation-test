using AElf.Automation.Common.Extensions;
using AElf.Automation.Common.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AElf.Automation.ContractsTesting.Contracts
{
    public enum ResourceMethod
    {
        Initialize,
        GetResourceBalance,
        AdjustResourceCap,
        GetElfokenAddress,
        BuyResource,
        SellResource
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

    }
}
