using System;
using System.Collections.Generic;
using System.Text;
using AElf.Automation.Common.Extensions;
using AElf.Automation.Common.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AElf.Automation.ContractsTesting.Contracts
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
        }

        public CommandInfo CallContractMethod(BenchmarkMethod method, params string[] paramArray)
        {
            return ExecuteContractMethodWithResult(method.ToString(), paramArray);
        }
    }
}
