using AElf.Automation.Common.Helpers;
using AElf.Contracts.Configuration;
using AElf.Contracts.ParliamentAuth;

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
        public ConfigurationContract(IApiHelper ch, string callAddress, string contractAddress) : 
            base(ch, contractAddress)
        {
            CallAddress = callAddress;
            UnlockAccount(CallAddress);
        }
    }
}