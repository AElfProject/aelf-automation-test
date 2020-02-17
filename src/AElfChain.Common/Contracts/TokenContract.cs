using System;
using AElf.Client.Dto;
using AElf.Contracts.MultiToken;
using AElf.Types;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
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
        UpdateCoefficientFromContract,
        UpdateCoefficientFromSender,
        UpdateLinerAlgorithm,
        UpdatePowerAlgorithm,
        ChangeFeePieceKey,
        ValidateTokenInfoExists,
        AdvanceResourceToken,
        UpdateRental,
        UpdateRentedResourceToken,

        //View
        GetTokenInfo,
        GetBalance,
        GetAllowance,
        GetPrimaryTokenSymbol,
        IsInWhiteList,
        GetNativeTokenInfo,
        GetCrossChainTransferTokenContractAddress,
        GetMethodFee,
        GetOwningRental,
        GetLockedAmount
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
                Memo = $"T-{Guid.NewGuid().ToString()}"
            });

            return result;
        }

        public TransactionResultDto IssueBalance(string from, string to, long amount, string symbol = "")
        {
            var tester = GetNewTester(from);
            tester.SetAccount(from);
            var result = tester.ExecuteMethodWithResult(TokenMethod.Issue, new IssueInput
            {
                Symbol = NodeOption.GetTokenSymbol(symbol),
                To = to.ConvertAddress(),
                Amount = amount,
                Memo = $"I-{Guid.NewGuid()}"
            });

            return result;
        }

        public TransactionResultDto ApproveToken(string from, string to, long amount, string symbol = "")
        {
            SetAccount(from);
            var result = ExecuteMethodWithResult(TokenMethod.Approve, new ApproveInput
            {
                Symbol = NodeOption.GetTokenSymbol(symbol),
                Amount = amount,
                Spender = to.ConvertAddress()
            });

            return result;
        }

        public TransactionResultDto CrossChainReceiveToken(string from, CrossChainReceiveTokenInput input)
        {
            var tester = GetNewTester(from);
            return tester.ExecuteMethodWithResult(TokenMethod.CrossChainReceiveToken, input);
        }

        public long GetUserBalance(string account, string symbol = "")
        {
            return CallViewMethod<GetBalanceOutput>(TokenMethod.GetBalance, new GetBalanceInput
            {
                Owner = account.ConvertAddress(),
                Symbol = NodeOption.GetTokenSymbol(symbol)
            }).Balance;
        }

        public long GetLockedAmount(string account, Hash lockId, string symbol = "")
        {
            return CallViewMethod<GetLockedAmountOutput>(TokenMethod.GetLockedAmount, new GetLockedAmountInput
            {
                Address = account.ConvertAddress(),
                LockId = lockId,
                Symbol = NodeOption.GetTokenSymbol(symbol)
            }).Amount;
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

        public OwningRental GetOwningRental()
        {
            return CallViewMethod<OwningRental>(TokenMethod.GetOwningRental, new Empty());
        }
    }
}