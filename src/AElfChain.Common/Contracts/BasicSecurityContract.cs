using AElfChain.Common.Managers;

namespace AElfChain.Common.Contracts
{
    public enum SecurityMethod
    {
        TestBytesState
    }
    
    public class BasicSecurityContract : BaseContract<SecurityMethod>
    {
        public BasicSecurityContract(INodeManager nodeManager, string callAddress, string contractAddress)
            : base(nodeManager, contractAddress)
        {
            SetAccount(callAddress);
        }

        public BasicSecurityContract(INodeManager nodeManager, string callAddress)
            : base(nodeManager, ContractFileName, callAddress)
        {
        }
        
        public static string ContractFileName => "AElf.Contracts.TestContract.BasicSecurity";

    }
    
    
}