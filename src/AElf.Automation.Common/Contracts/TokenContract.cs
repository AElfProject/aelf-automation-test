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
        public TokenContract(RpcApiHelper ch, string callAddress) :
            base(ch, "AElf.Contracts.MultiToken", callAddress)
        {
        }

        public TokenContract(RpcApiHelper ch, string callAddress, string contractAddress):
            base(ch, contractAddress)
        {
            CallAddress = callAddress;
            UnlockAccount(CallAddress);
        }

        public CommandInfo CallMethodWithResult(TokenMethod method, IMessage inputParameter)
        {
            return ExecuteMethodWithResult(method.ToString(), inputParameter);
        }

        public void CallWithoutResult(TokenMethod method, IMessage inputParameter)
        {
            ExecuteMethodWithTxId(method.ToString(), inputParameter);
        }

        public JObject CallViewMethod(TokenMethod method, IMessage inputParameter)
        {
            return CallViewMethod(method.ToString(), inputParameter);
        }
        
        public T CallViewMethod<T>(TokenMethod method, IMessage inputParameter) where T : IMessage<T>, new()
        {
            return CallViewMethod<T>(method.ToString(), inputParameter);
        }
    }
}
