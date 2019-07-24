using AElf.Automation.Common.Helpers;
using AElf.Contracts.MultiToken.Messages;
using AElf.Types;

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

        public TokenContract(IApiHelper apiHelper, string callAddress, string contractAddress) :
            base(apiHelper, contractAddress)
        {
            CallAddress = callAddress;
            UnlockAccount(CallAddress);
        }

        public long GetUserBalance(string account, string symbol = "ELF")
        {
            return CallViewMethod<GetBalanceOutput>(TokenMethod.GetBalance, new GetBalanceInput
            {
                Owner = Address.Parse(account),
                Symbol = symbol
            }).Balance;
        }

        public TokenContractContainer.TokenContractStub GetTokenContractTester()
        {
            var stub = new ContractTesterFactory(ApiHelper.GetApiUrl());
            var tokenStub = stub.Create<TokenContractContainer.TokenContractStub>(Address.Parse(ContractAddress), CallAddress);
            return tokenStub;
        }
    }
}