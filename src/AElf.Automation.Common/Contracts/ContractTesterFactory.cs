using System;
using System.Threading;
using System.Threading.Tasks;
using AElf.Automation.Common.Helpers;
using AElf.Cryptography.ECDSA;
using AElf.CSharp.Core;
using AElf.Types;
using Volo.Abp.DependencyInjection;

namespace AElf.Automation.Common.Contracts
{
    public interface IContractTesterFactory
    {
        T Create<T>(Address contractAddress, string account, string password = "123", bool notimeout = true)
            where T : ContractStubBase, new();
    }

    public class ContractTesterFactory : IContractTesterFactory
    {
        private readonly string _baseUrl;
        private readonly string _keyPath;

        public ContractTesterFactory(string baseUrl, string keyPath = "")
        {
            _baseUrl = baseUrl;
            _keyPath = keyPath == "" ? CommonHelper.GetCurrentDataDir() : keyPath;
        }

        public T Create<T>(Address contractAddress, string account, string password = "123", bool notimeout = true)
            where T : ContractStubBase, new()
        {
            var factory = new MethodStubFactory(_baseUrl, _keyPath)
            {
                SenderAddress = account,
                Sender = AddressHelper.Base58StringToAddress(account),
                ContractAddress = contractAddress
            };
            var timeout = notimeout ? "notimeout" : "";
            factory.ApiHelper.UnlockAccount(new CommandInfo(ApiMethods.AccountUnlock)
            {
                Parameter = $"{account} {password} {timeout}"
            });

            return new T() {__factory = factory};
        }
    }
}