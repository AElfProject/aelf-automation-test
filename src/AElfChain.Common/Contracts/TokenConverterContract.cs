using AElfChain.Common.Managers;

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