using System;
using AElf.Cryptography.ECDSA;
using AElf.CSharp.Core;
using AElf.Types;
using Volo.Abp.DependencyInjection;

namespace AElf.Automation.Common.Contracts
{
    public interface IContractTesterFactory
    {
        T Create<T>(Address contractAddress, ECKeyPair senderKey) where T : ContractStubBase, new();
    }

    public class ContractTesterFactory
    {
        private readonly string BaseUrl;

        public ContractTesterFactory(string baseUrl)
        {
            BaseUrl = baseUrl;
        }

        public  T Create<T>(Address contractAddress, ECKeyPair senderKey) where T : ContractStubBase, new()
        {
            return new T()
            {
                __factory = new MethodStubFactory(BaseUrl)
                {
                    ContractAddress = contractAddress,
                    KeyPair = senderKey
                }
            };
        }
    }
}