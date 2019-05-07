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
        public TokenConverterContract(IApiHelper apiHelper, string callAddress) :
            base(apiHelper, "AElf.Contracts.TokenConverter", callAddress)
        {
        }

        public TokenConverterContract(IApiHelper apiHelper, string callAddress, string contractAddress):
            base(apiHelper, contractAddress)
        {
            CallAddress = callAddress;
            UnlockAccount(CallAddress);
        }
    }
}