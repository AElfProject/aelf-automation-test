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
        public ConfigurationContract(IApiHelper apiHelper, string callAddress, string contractAddress) : 
            base(apiHelper, contractAddress)
        {
            CallAddress = callAddress;
            UnlockAccount(CallAddress);
        }
    }
}