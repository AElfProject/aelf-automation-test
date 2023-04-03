using AElfChain.Common.Managers;

namespace AElfChain.Common.Contracts
{
    public enum ConfigurationMethod
    {
        SetConfiguration,
        GetConfiguration,
        GetOwnerAddress,
        ChangeConfigurationController,
        GetConfigurationController,
        ChangeMethodFeeController,
        GetMethodFeeController
    }

    public enum ConfigurationNameProvider
    {
        BlockTransactionLimit,
        RequiredAcsInContracts,
        StateSizeLimit,
        ExecutionObserverThreshold,
        UserContractMethodFee
    }

    public class ConfigurationContract : BaseContract<ConfigurationMethod>
    {
        public ConfigurationContract(INodeManager nodeManager, string callAddress, string contractAddress)
            : base(nodeManager, contractAddress)
        {
            SetAccount(callAddress);
        }
    }
}