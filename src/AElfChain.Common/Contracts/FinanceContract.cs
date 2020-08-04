using AElfChain.Common.Managers;

namespace AElfChain.Common.Contracts
{
    public enum FinanceMethod
    {
        //Action
        Initialize,
        SupportMarket,
        
        //View
        GetAllMarkets
    }

    public class FinanceContract : BaseContract<FinanceMethod>
    {
        public FinanceContract(INodeManager nodeManager, string callAddress) :
            base(nodeManager, "AElf.Contracts.FinanceContract", callAddress)
        {
        }

        public FinanceContract(INodeManager nodeManager, string callAddress, string contractAddress) :
            base(nodeManager, contractAddress)
        {
            SetAccount(callAddress);
        }
    }
}