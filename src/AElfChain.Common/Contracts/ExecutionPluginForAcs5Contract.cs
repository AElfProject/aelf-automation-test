using AElfChain.Common.Managers;

namespace AElfChain.Common.Contracts
{
    public enum PluginAcs5Method
    {
        SetMethodCallingThreshold,
        GetMethodCallingThreshold,
        DummyMethod
    }
    
    public class ExecutionPluginForAcs5Contract : BaseContract<PluginAcs5Method>
    {
        public ExecutionPluginForAcs5Contract(INodeManager nodeManager, string callAddress, string contractAddress)
            : base(nodeManager, contractAddress)
        {
            SetAccount(callAddress);
        }

        public ExecutionPluginForAcs5Contract(INodeManager nodeManager, string callAddress)
            : base(nodeManager, ContractFileName, callAddress)
        {
        }

        public static string ContractFileName => "AElf.Kernel.SmartContract.ExecutionPluginForAcs5.Tests.TestContract";
    }
}