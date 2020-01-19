using AElfChain.Common.Managers;

namespace AElfChain.Common.Contracts
{
    public enum ConfigurationMethod
    {
        SetBlockTransactionLimit,
        GetBlockTransactionLimit,
        ChangeOwnerAddress,
        GetOwnerAddress,
        SetRequiredAcsInContracts,
        GetRequiredAcsInContracts
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