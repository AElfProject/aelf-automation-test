using System.Collections.Generic;
using System.Linq;
using AElf.Contracts.Genesis;
using AElf.Types;
using AElfChain.Common.ContractSerializer;
using Google.Protobuf.WellKnownTypes;

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
                if(SystemContracts == null)
                    SystemContracts = new Dictionary<Address, List<string>>();
                if (SystemContracts.ContainsKey(contract)) continue;
                var contractDescriptor =
                    genesis.ApiService.GetContractFileDescriptorSetAsync(contract.GetFormatted()).Result;
                var systemContractHandler = new CustomContractHandler(contractDescriptor);
                var methods = systemContractHandler.GetContractMethods();
                SystemContracts.Add(contract, methods);
            }
        }

        public static void GetAllCustomContractDic(this GenesisContract genesis)
        {
            if (SystemContractAddresses == null)
            {
                SystemContractAddresses = genesis.GetAllSystemContracts();
            }

            var addressList =
                genesis.CallViewMethod<AddressList>(GenesisMethod.GetDeployedContractAddressList, new Empty());
            foreach (var address in addressList.Value)
            {
                if (SystemContractAddresses.ContainsValue(address)) continue;
                if(CustomContracts == null)
                    CustomContracts = new Dictionary<Address, List<string>>();
                if (CustomContracts.ContainsKey(address))
                {
                    CustomContracts.Remove(address);
                }

                var contractDescriptor =
                    genesis.ApiService.GetContractFileDescriptorSetAsync(address.GetFormatted()).Result;
                var customContractHandler = new CustomContractHandler(contractDescriptor);
                var methods = customContractHandler.GetContractMethods();
                CustomContracts.TryAdd(address, methods);
            }
        }

        public static List<Address> QuerySystemContractByMethodName(this GenesisContract genesis, string method)
        {
            if (SystemContracts == null)
            {
                GetAllSystemContractDic(genesis);
            }

            return SystemContracts.Where(o => o.Value.Contains(method)).Select(o => o.Key).ToList();
        }

        public static List<Address> QueryCustomContractByMethodName(this GenesisContract genesis, string method)
        {
            GetAllCustomContractDic(genesis);

            return CustomContracts.Where(o => o.Value.Contains(method)).Select(o => o.Key).ToList();
        }
    }
}