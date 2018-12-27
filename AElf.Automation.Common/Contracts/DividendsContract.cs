using System;
using System.Collections.Generic;
using System.Text;
using AElf.Automation.Common.Extensions;
using AElf.Automation.Common.Helpers;

namespace AElf.Automation.Common.Contracts
{
    public enum DicidendsMethod
    {
        GetTermDividends,
        GetTermTotalWeights,
        GetLatestRequestDividendsTermNumber
    }
    public class DividendsContract :BaseContract
    {
        public DividendsContract(CliHelper ch, string account, string dividendsAbi)
            :base(ch, dividendsAbi)
        {
            Account = account;
            UnlockAccount(Account);
        }

        public DividendsContract(CliHelper ch, string account)
            : base(ch, "AElf.Contracts.Dividends", account)
        {
        }

        public CommandInfo CallContractMethod(DicidendsMethod method, params string[] paramsArray)
        {
            return ExecuteContractMethodWithResult(method.ToString(), paramsArray);
        }

        public void CallContractWithoutResult(DicidendsMethod method, params string[] paramsArray)
        {
            ExecuteContractMethod(method.ToString(), paramsArray);
        }
    }
}
