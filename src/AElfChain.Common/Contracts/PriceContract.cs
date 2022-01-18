using AElf.Contracts.Price;
using AElf.Types;
using AElfChain.Common.Managers;
using Google.Protobuf.WellKnownTypes;

namespace AElfChain.Common.Contracts
{
    public enum PriceMethod
    {
        Initialize,
        QuerySwapTokenPrice,
        QueryExchangeTokenPrice,
        RecordSwapTokenPrice,
        RecordExchangeTokenPrice,
        UpdateSwapTokenTraceInfo,
        UpdateAuthorizedSwapTokenPriceQueryUsers,
        ChangeOracle,
        ChangeTracePathLimit,
        
        GetSwapTokenPriceInfo,
        GetBatchSwapTokenPriceInfo,
        GetExchangeTokenPriceInfo,
        GetBatchExchangeTokenPriceInfo,
        CheckQueryIdIfExisted,
        GetAuthorizedSwapTokenPriceQueryUsers,
        GetTracePathLimit
    }

    public class PriceContract : BaseContract<PriceMethod>
    {
        public PriceContract(INodeManager nodeManager, string callAddress) : base(nodeManager,
            "AElf.Contracts.Price", callAddress)
        {
        }

        public PriceContract(INodeManager nodeManager, string contractAddress, string callAddress) : base(nodeManager,
            contractAddress)
        {
            SetAccount(callAddress);
        }

        public AuthorizedSwapTokenPriceQueryUsers GetAuthorizedSwapTokenPriceQueryUsers()
        {
            return CallViewMethod<AuthorizedSwapTokenPriceQueryUsers>(PriceMethod.GetAuthorizedSwapTokenPriceQueryUsers,
                new Empty());
        }
        
        public TracePathLimit GetTracePathLimit()
        {
            return CallViewMethod<TracePathLimit>(PriceMethod.GetTracePathLimit,
                new Empty());
        }

        public IsQueryIdExisted CheckQueryIdIfExisted(Hash queryId)
        {
            return CallViewMethod<IsQueryIdExisted>(PriceMethod.CheckQueryIdIfExisted, queryId);
        }

        public Price GetSwapTokenPriceInfo(string targetSymbol, string symbol)
        {
            var input = new GetSwapTokenPriceInfoInput
            {
                TargetTokenSymbol = targetSymbol,
                TokenSymbol = symbol
            };
            return CallViewMethod<Price>(PriceMethod.GetSwapTokenPriceInfo, input);
        }

        public BatchPrices GetBatchSwapTokenPriceInfo(string targetSymbol, string symbol)
        {
            var input = new GetBatchSwapTokenPriceInfoInput
            {
                TokenPriceQueryList =
                {
                    new GetSwapTokenPriceInfoInput
                    {
                        TargetTokenSymbol = targetSymbol,
                        TokenSymbol = symbol
                    }
                }
            };
            return CallViewMethod<BatchPrices>(PriceMethod.GetBatchSwapTokenPriceInfo, input);
        }

        public Price GetExchangeTokenPriceInfo(string targetSymbol, string symbol, Address address)
        {
            var input = new GetExchangeTokenPriceInfoInput
            {
                TargetTokenSymbol = targetSymbol,
                TokenSymbol = symbol,
                Organization = address
            };
            return CallViewMethod<Price>(PriceMethod.GetExchangeTokenPriceInfo, input);
        }
    }
}