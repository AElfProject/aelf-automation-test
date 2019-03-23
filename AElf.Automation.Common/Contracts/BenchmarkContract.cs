using AElf.Automation.Common.Helpers;
using Google.Protobuf;
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

        public CommandInfo CallContractMethod(BenchmarkMethod method, IMessage inputParameter)
        {
            return ExecuteContractMethodWithResult(method.ToString(), inputParameter);
        }

        public void CallContractWithoutResult(BenchmarkMethod method, IMessage inputParameter)
        {
            ExecuteContractMethod(method.ToString(), inputParameter);
        }

        public JObject CallReadOnlyMethod(BenchmarkMethod method, IMessage inputParameter)
        {
            return CallContractViewMethod(method.ToString(), inputParameter);
        }
    }
}
