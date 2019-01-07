using AElf.Automation.Common.Extensions;
using AElf.Automation.Common.Helpers;
using Newtonsoft.Json.Linq;

namespace AElf.Automation.Common.Contracts
{
    public enum BenchmarkMethod
    {
        InitBalance,
        Transfer,
        GetBalance
    }
    
    public class BenchmarkContract : BaseContract
    {
        public BenchmarkContract(CliHelper ch, string account):
            base(ch, "AElf.Benchmark.TestContrat", account)
        {
        }

        public BenchmarkContract(CliHelper ch, string account, string contractAbi) :
            base(ch, "AElf.Benchmark.TestContrat", contractAbi)
        {
            Account = account;
            UnlockAccount(Account);
        }

        public CommandInfo CallContractMethod(BenchmarkMethod method, params string[] paramArray)
        {
            return ExecuteContractMethodWithResult(method.ToString(), paramArray);
        }

        public void CallContractWithoutResult(BenchmarkMethod method, params string[] paramsArray)
        {
            ExecuteContractMethod(method.ToString(), paramsArray);
        }

        public JObject CallReadOnlyMethod(BenchmarkMethod method, params string[] paramsArray)
        {
            return CallContractViewMethod(method.ToString(), paramsArray);
        }
    }
}
