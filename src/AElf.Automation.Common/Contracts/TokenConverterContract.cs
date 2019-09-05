using AElf.Automation.Common.Helpers;
using AElf.Automation.Common.Managers;
using AElf.Contracts.TokenConverter;

namespace AElf.Automation.Common.Contracts
{
    public enum TokenConverterMethod
    {
        //action
        Initialize,
        SetConnector,
        Buy,
        Sell,
        SetFeeRate,
        SetManagerAddress,

        //view
        GetTokenContractAddress,
        GetFeeReceiverAddress,
        GetFeeRate,
        GetManagerAddress,
        GetBaseTokenSymbol,
        GetConnector
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
    }
}