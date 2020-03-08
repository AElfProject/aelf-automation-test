using AElfChain.Common.Managers;

namespace AElfChain.Common.Contracts
{
    public enum TokenSwapMethod
    {
        //Action
        AddSwapPair,
        AddSwapRound,
        SwapToken,
        ChangeSwapRatio,
        Deposit,
        
        //View
        GetSwapPair,
        GetCurrentSwapRound
    }

    public class TokenSwapContract : BaseContract<TokenSwapMethod>
    {
        public TokenSwapContract(INodeManager nodeManager, string callAddress) :
            base(nodeManager, "TokenSwapContract", callAddress)
        {
        }

        public TokenSwapContract(INodeManager nodeManager, string callAddress, string contractAddress) :
            base(nodeManager, contractAddress)
        {
            SetAccount(callAddress);
        }
    }
}