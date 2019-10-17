using AElfChain.Common.Managers;

namespace AElfChain.Common.Contracts
{
    public enum PluginAcs8Method
    {
        CpuConsumingMethod,
        StoConsumingMethod,
        NetConsumingMethod,
        FewConsumingMethod
    }

    public class ExecutionPluginForAcs8Contract : BaseContract<PluginAcs8Method>
    {
        public ExecutionPluginForAcs8Contract(INodeManager nodeManager, string callAddress, string contractAddress)
            : base(nodeManager, contractAddress)
        {
            SetAccount(callAddress);
        }

        public ExecutionPluginForAcs8Contract(INodeManager nodeManager, string callAddress)
            : base(nodeManager, ContractFileName, callAddress)
        {
        }

        public static string ContractFileName => "AElf.Kernel.SmartContract.ExecutionPluginForAcs8.Tests.TestContract";
    }
}