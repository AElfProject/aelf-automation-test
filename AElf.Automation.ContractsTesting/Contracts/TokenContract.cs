using System;
using System.Collections.Generic;
using System.Text;
using AElf.Automation.Common.Extensions;
using AElf.Automation.Common.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AElf.Automation.ContractsTesting.Contracts
{
    public enum TokenMethod
    {
        Initialize,
        Transfer,
        TransferFrom,
        Approve,
        UnApprove,
        BalanceOf,
        Allowance,
        Symbol,
        TokenName,
        TotalSupply,
        Decimals
    }
    public class TokenContract : BaseContract
    {
        public TokenContract(CliHelper ch, string account) :
            base(ch, "AElf.Contracts.Token", account)
        {
        }

        public TokenContract(CliHelper ch, string account, string contractAbi):
            base(ch, contractAbi)
        {
            Account = account;
        }

        public CommandInfo CallContractMethod(TokenMethod method, params string[] paramsArray)
        {
            return ExecuteContractMethodWithResult(method.ToString(), paramsArray);
        }
    }
}
