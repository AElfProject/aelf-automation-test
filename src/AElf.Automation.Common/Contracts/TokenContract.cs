using AElf.Automation.Common.Helpers;
using Google.Protobuf;
using Newtonsoft.Json.Linq;

namespace AElf.Automation.Common.Contracts
{
    public enum TokenMethod
    {
        //Action
        Create,
        InitializeTokenContract,
        CreateNativeToken,
        Issue,
        IssueNativeToken,
        Transfer,
        CrossChainTransfer,
        CrossChainReceiveToken,
        Lock,
        Unlock,
        TransferFrom,
        Approve,
        UnApprove,
        Burn,
        ChargeTransactionFees,
        ClaimTransactionFees,
        SetFeePoolAddress,

        //View
        GetTokenInfo,
        GetBalance,
        GetAllowance,
        IsInWhiteList
    }
    public class TokenContract : BaseContract<TokenMethod>
    {
        public TokenContract(IApiHelper ch, string callAddress) :
            base(ch, "AElf.Contracts.MultiToken", callAddress)
        {
        }

        public TokenContract(IApiHelper ch, string callAddress, string contractAddress):
            base(ch, contractAddress)
        {
            CallAddress = callAddress;
            UnlockAccount(CallAddress);
        }
    }
}
