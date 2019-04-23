using AElf.Automation.Common.Helpers;
using Google.Protobuf;
using Newtonsoft.Json.Linq;

namespace AElf.Automation.Common.Contracts
{
    public enum TokenMethod
    {
        //Action
        Create,
        Issue,
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
        GetBalance,
        Allowance,
        ChargedFees,
        FeePoolAddress
    }
    public class TokenContract : BaseContract
    {
        public TokenContract(CliHelper ch, string address) :
            base(ch, "AElf.Contracts.MultiToken", address)
        {
        }

        public TokenContract(CliHelper ch, string address, string contractAbi):
            base(ch, contractAbi)
        {
            Address = address;
            UnlockAccount(Address);
        }

        public CommandInfo CallContractMethod(TokenMethod method, IMessage inputParameter)
        {
            return ExecuteContractMethodWithResult(method.ToString(), inputParameter);
        }

        public void CallContractWithoutResult(TokenMethod method, IMessage inputParameter)
        {
            ExecuteContractMethod(method.ToString(), inputParameter);
        }

        public JObject CallReadOnlyMethod(TokenMethod method, IMessage inputParameter)
        {
            return CallContractViewMethod(method.ToString(), inputParameter);
        }
    }
}
