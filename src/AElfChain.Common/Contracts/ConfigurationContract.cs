using AElfChain.Common.Managers;

namespace AElfChain.Common.Contracts
{
    public enum ConfigurationMethod
    {
        SetBlockTransactionLimit,
        GetBlockTransactionLimit,
        GetOwnerAddress,
        SetRequiredAcsInContracts,
        GetRequiredAcsInContracts
        ChangeConfigurationController,
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