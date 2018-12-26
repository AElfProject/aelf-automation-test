using AElf.Automation.Common.Extensions;
using AElf.Automation.Common.Helpers;
using Newtonsoft.Json.Linq;

namespace AElf.Automation.Common.Contracts
{
    public enum TokenMethod
    {
        //Action
        Initialize,
        SetFeePoolAddress,
        ClaimTransactionFees,
        Transfer,
        TransferFrom,
        Approve,
        UnApprove,
        Burn,

        //View
        Symbol,
        TokenName,
        TotalSupply,
        Decimals,
        BalanceOf,
        Allowance,
        ChargedFees,
        FeePoolAddress
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
            UnlockAccount(Account);
        }

        public CommandInfo CallContractMethod(TokenMethod method, params string[] paramsArray)
        {
            return ExecuteContractMethodWithResult(method.ToString(), paramsArray);
        }

        public void CallContractWithoutResult(TokenMethod method, params string[] paramsArray)
        {
            ExecuteContractMethod(method.ToString(), paramsArray);
        }

        public JObject CallReadOnlyMethod(TokenMethod method, params string[] paramsArray)
        {
            return CallContractViewMethod(method.ToString(), paramsArray);
        }
    }
}
