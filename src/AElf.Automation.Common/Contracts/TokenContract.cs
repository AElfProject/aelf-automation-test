using AElf.Automation.Common.Helpers;

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
        public TokenContract(IApiHelper apiHelper, string callAddress) :
            base(apiHelper, "AElf.Contracts.MultiToken", callAddress)
        {
        }

        public TokenContract(IApiHelper apiHelper, string callAddress, string contractAddress):
            base(apiHelper, contractAddress)
        {
            CallAddress = callAddress;
            UnlockAccount(CallAddress);
        }
    }
}
