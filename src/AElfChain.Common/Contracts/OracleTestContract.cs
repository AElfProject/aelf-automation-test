using AElf.Client.Dto;
using AElf.Contracts.TestsOracle;
using AElf.Types;
using AElfChain.Common.Contracts;
using AElfChain.Common.Managers;
using Google.Protobuf.WellKnownTypes;

namespace AElfChain.Common
{
    public enum OracleTestMethod
    {
        RecordTokenPrice,
        GetOracleTokenSymbol
    }

    public class OracleTestContract : BaseContract<OracleTestMethod>
    {
        public OracleTestContract(INodeManager nodeManager, string callAddress) : base(nodeManager,
            "AElf.Contracts.TestOracle", callAddress)
        {
        }

        public OracleTestContract(INodeManager nodeManager, string contractAddress, string callAddress) : base(
            nodeManager,
            contractAddress)
        {
            SetAccount(callAddress);
        }

        public TransactionResultDto RecordTokenPrice(Address _priceContract, string queryId, string targetTokenSymbol,
            string tokenSymbol, string price, Timestamp timestamp, Address oracleNodes)
        {
            var result = ExecuteMethodWithResult(OracleTestMethod.RecordTokenPrice, new TokenPriceInfo
            {
                CallBackAddress = _priceContract,
                CallBackMethodName = "RecordSwapTokenPrice",
                QueryId = Hash.LoadFromHex(queryId),
                TokenPrice = new TokenPrice
                {
                    Timestamp = timestamp,
                    TargetTokenSymbol = targetTokenSymbol,
                    TokenSymbol = tokenSymbol,
                    Price = price
                },
                OracleNodes = {oracleNodes}
            });
            return result;
        }
        
        public TransactionResultDto RecordTokenPriceExchange(Address _priceContract, string queryId, string targetTokenSymbol,
            string tokenSymbol, string price, Timestamp timestamp, Address oracleNodes)
        {
            var result = ExecuteMethodWithResult(OracleTestMethod.RecordTokenPrice, new TokenPriceInfo
            {
                CallBackAddress = _priceContract,
                CallBackMethodName = "RecordExchangeTokenPrice",
                QueryId = Hash.LoadFromHex(queryId),
                TokenPrice = new TokenPrice
                {
                    Timestamp = timestamp,
                    TargetTokenSymbol = targetTokenSymbol,
                    TokenSymbol = tokenSymbol,
                    Price = price
                },
                OracleNodes = {oracleNodes}
            });
            return result;
        }
    }
}