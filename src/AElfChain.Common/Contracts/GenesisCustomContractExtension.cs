using System.Collections.Generic;
using AElf.Types;
using AElfChain.Contract;
using Volo.Abp.Threading;

namespace AElfChain.Common.Contracts
{
    public static class GenesisCustomContractExtension
    {
        public static Dictionary<Address, List<string>> CustomContracts { get; set; }

        public static Dictionary<Address, List<string>> SystemContracts { get; set; }

        public static Dictionary<NameProvider, Address> SystemContractAddresses { get; set; }

        public static void GetAllSystemContractDic(this GenesisContract genesis)
        {
            SystemContractAddresses = genesis.GetAllSystemContracts();
            foreach (var contract in SystemContractAddresses.Values)
            {
                if (SystemContracts == null)
                    SystemContracts = new Dictionary<Address, List<string>>();
                if (SystemContracts.ContainsKey(contract)) continue;
                var contractDescriptor =
                    AsyncHelper.RunSync(() =>
                        genesis.ApiClient.GetContractFileDescriptorSetAsync(contract.GetFormatted()));
                var systemContractHandler = new CustomContractSerializer(contractDescriptor);
                var methods = systemContractHandler.GetContractMethods();
                SystemContracts.Add(contract, methods);
            }
        }

        public static List<Address> QueryCustomContractByMethodName(this GenesisContract genesis, string method)
        {
            return new List<Address>();
        }
    }
}