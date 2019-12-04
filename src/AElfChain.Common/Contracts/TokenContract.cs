using System;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using AElf.Contracts.MultiToken;
using AElfChain.Common.Utils;
using AElfChain.SDK.Models;
using Google.Protobuf.WellKnownTypes;

namespace AElfChain.Common.Contracts
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
        GetPrimaryTokenSymbol,
        IsInWhiteList,
        GetNativeTokenInfo,
        GetCrossChainTransferTokenContractAddress,
        GetMethodFee
    }

    public class TokenContract : BaseContract<TokenMethod>
    {
        public TokenContract(INodeManager nodeManager, string callAddress) :
            base(nodeManager, "AElf.Contracts.MultiToken", callAddress)
        {
            Logger = Log4NetHelper.GetLogger();
        }

        public TokenContract(INodeManager nodeManager, string callAddress, string contractAddress) :
            base(nodeManager, contractAddress)
        {
            SetAccount(callAddress);
            Logger = Log4NetHelper.GetLogger();
        }

        public TransactionResultDto TransferBalance(string from, string to, long amount, string symbol = "")
        {
            var tester = GetNewTester(from);
            var result = tester.ExecuteMethodWithResult(TokenMethod.Transfer, new TransferInput
            {
                Symbol = NodeOption.GetTokenSymbol(symbol),
                To = to.ConvertAddress(),
                Amount = amount,
                Memo = $"transfer amount {amount} - {Guid.NewGuid().ToString()}"
            });

            return result;
        }

        public TransactionResultDto IssueBalance(string from, string to, long amount, string symbol = "")
        {
            var tester = GetNewTester(from);
            tester.SetAccount(from);
            var result = tester.ExecuteMethodWithResult(TokenMethod.Issue, new IssueInput
            {
                Symbol = symbol,
                To = to.ConvertAddress(),
                Amount = amount,
                Memo = $"Issue amount {Guid.NewGuid()}"
            });

            return result;
        }


        public long GetUserBalance(string account, string symbol = "")
        {
            return CallViewMethod<GetBalanceOutput>(TokenMethod.GetBalance, new GetBalanceInput
            {
                Owner = account.ConvertAddress(),
                Symbol = NodeOption.GetTokenSymbol(symbol)
            }).Balance;
        }

        public long GetAllowance(string from, string to, string symbol = "")
        {
            return CallViewMethod<GetAllowanceOutput>(TokenMethod.GetAllowance,
                new GetAllowanceInput
                {
                    Owner = from.ConvertAddress(),
                    Spender = to.ConvertAddress(),
                    Symbol = NodeOption.GetTokenSymbol(symbol)
                }).Allowance;
        }

        public string GetPrimaryTokenSymbol()
        {
            return CallViewMethod<StringValue>(TokenMethod.GetPrimaryTokenSymbol, new Empty()).Value;
        }

        public string GetNativeTokenSymbol()
        {
            return CallViewMethod<TokenInfo>(TokenMethod.GetNativeTokenInfo, new Empty()).Symbol;
        }

        public TokenInfo GetTokenInfo(string symbol)
        {
            return CallViewMethod<TokenInfo>(TokenMethod.GetTokenInfo, new GetTokenInfoInput
            {
                Symbol = symbol
            });
        }
    }
}