using AElf.Automation.Common.Managers;

namespace AElf.Automation.Common.Contracts
{
    public enum ConfigurationMethod
    {
        SetBlockTransactionLimit,
        GetBlockTransactionLimit,
        ChangeOwnerAddress,
        GetOwnerAddress
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