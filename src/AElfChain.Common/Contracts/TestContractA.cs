using AElfChain.Common.Managers;

namespace AElfChain.Common.Contracts
{
    public enum AMethod
    {
        InitializeA,
        Transfer
    }
    
    public class TestContractA : BaseContract<AMethod>
    {
        public TestContractA(INodeManager nodeManager, string callAddress, string contractAddress)
            : base(nodeManager, contractAddress)
        {
            SetAccount(callAddress);
        }

        public TestContractA(INodeManager nodeManager, string callAddress)
            : base(nodeManager, ContractFileName, callAddress)
        {
        }

        public static string ContractFileName => "AElf.Contracts.TestContract.BasicFunction";
    }
}