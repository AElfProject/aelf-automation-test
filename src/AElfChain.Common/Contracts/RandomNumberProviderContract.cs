using AElfChain.Common.Managers;

namespace AElfChain.Common.Contracts
{
    public enum RandomNumberProviderMethod
    {

    }
    
    public class RandomNumberProviderContract : BaseContract<RandomNumberProviderMethod>
    {
        public RandomNumberProviderContract(INodeManager nodeManager, string callAddress, string contractAddress)
            : base(nodeManager, contractAddress)
        {
            SetAccount(callAddress);
        }

        public RandomNumberProviderContract(INodeManager nodeManager, string callAddress)
            : base(nodeManager, ContractFileName, callAddress)
        {
        }
        
        public static string ContractFileName => "AElf.Contracts.TestContract.RandomNumberProvider";

    }
}