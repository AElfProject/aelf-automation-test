using System;
using AElf.Client.Dto;
using AElf.Contracts.TokenConverter;
using AElfChain.Common.Managers;
using Google.Protobuf.WellKnownTypes;

namespace AElfChain.Common.Contracts
{
    public enum TokenConverterMethod
    {
        //action
        Initialize,
        SetConnector,
        Buy,
        Sell,
        SetFeeRate,
        ChangeConnectorController,
        AddPairConnector,
        EnableConnector,
        UpdateConnector,

        //view
        GetTokenContractAddress,
        GetFeeReceiverAddress,
        GetFeeRate,
        GetManagerAddress,
        GetBaseTokenSymbol,
        GetConnector,
        GetDepositConnectorBalance,
        GetPairConnector
    }

    public class TokenConverterContract : BaseContract<TokenConverterMethod>
    {
        public TokenConverterContract(INodeManager nodeManager, string callAddress) :
            base(nodeManager, "AElf.Contracts.TokenConverter", callAddress)
        {
        }

        public TokenConverterContract(INodeManager nodeManager, string callAddress, string contractAddress) :
            base(nodeManager, contractAddress)
        {
            SetAccount(callAddress);
        }

        public TransactionResultDto Buy(string buyer,string symbol,long amount)
        {
            var tester = GetNewTester(buyer);
            var result = tester.ExecuteMethodWithResult(TokenConverterMethod.Buy, new BuyInput()
            {
                Symbol = symbol,
                Amount = amount
            });
            return result;
        }

        public long GetDepositConnectorBalance(string symbol)
        {
            var balance = CallViewMethod<Int64Value>(TokenConverterMethod.GetDepositConnectorBalance,
                new StringValue {Value = symbol});
            return balance.Value;
        }

        public string GetFeeRate()
        {
            return (CallViewMethod<StringValue>(TokenConverterMethod.GetFeeRate, new Empty())).Value;
        }

        public PairConnector GetPairConnector(string symbol)
        {
            return CallViewMethod<PairConnector>(TokenConverterMethod.GetPairConnector,
                new TokenSymbol {Symbol = symbol});
        }
    }
}