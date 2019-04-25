using AElf.Automation.Common.Helpers;

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
        public TokenConverterContract(RpcApiHelper ch, string callAddress) :
            base(ch, "AElf.Contracts.TokenConverter", callAddress)
        {
        }

        public TokenConverterContract(RpcApiHelper ch, string callAddress, string contractAddress):
            base(ch, contractAddress)
        {
            CallAddress = callAddress;
            UnlockAccount(CallAddress);
        }
    }
}