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
        
        public ConfigurationContainer.ConfigurationStub GetConfigurationStubTester(
            string callAddress = null)
        {
            var caller = callAddress ?? CallAddress;
            var stub = new ContractTesterFactory(ApiHelper);
            var contractStub =
                stub.Create<ConfigurationContainer.ConfigurationStub>(
                    AddressHelper.Base58StringToAddress(ContractAddress), caller);
            return contractStub;
        }
    }
}