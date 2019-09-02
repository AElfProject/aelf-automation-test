using System;
using AElf.Automation.Common.Helpers;
using AElf.Contracts.MultiToken;
using AElfChain.SDK.Models;

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
        RegisterCrossChainTokenContractAddress,
        CrossChainCreateToken,

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
            Logger = Log4NetHelper.GetLogger();
        }

        public TokenContract(IApiHelper apiHelper, string callAddress, string contractAddress) :
            base(apiHelper, contractAddress)
        {
            CallAddress = callAddress;
            UnlockAccount(CallAddress);
            Logger = Log4NetHelper.GetLogger();
        }

        public TransactionResultDto TransferBalance(string from, string to, long amount, string symbol = "ELF")
        {
            var tester = GetNewTester(from);
            var result = tester.ExecuteMethodWithResult(TokenMethod.Transfer, new TransferInput
            {
                Symbol = symbol,
                To = AddressHelper.Base58StringToAddress(to),
                Amount = amount,
                Memo = $"transfer amount {amount} - {Guid.NewGuid().ToString()}"
            });

            return result;
        }
        public long GetUserBalance(string account, string symbol = "ELF")
        {
            return CallViewMethod<GetBalanceOutput>(TokenMethod.GetBalance, new GetBalanceInput
            {
                Owner = AddressHelper.Base58StringToAddress(account),
                Symbol = symbol
            }).Balance;
        }

        public TokenContractContainer.TokenContractStub GetTokenContractTester()
        {
            var stub = new ContractTesterFactory(ApiHelper);
            var tokenStub =
                stub.Create<TokenContractContainer.TokenContractStub>(AddressHelper.Base58StringToAddress(ContractAddress), CallAddress);
            return tokenStub;
        }
    }
}