using AElf.Automation.Common.Helpers;
using AElf.Cryptography.ECDSA;
using AElf.CSharp.Core;
using AElf.Types;

namespace AElf.Automation.Common.Contracts
{
    public interface IContractTesterFactory
    {
        T Create<T>(Address contractAddress, string account, string password = "123", bool notimeout = true)
            where T : ContractStubBase, new();
        T Create<T>(Address contractAddress, ECKeyPair keyPair)
            where T : ContractStubBase, new();
    }

    public class ContractTesterFactory : IContractTesterFactory
    {
        private readonly IApiHelper _apiHelper;

        public ContractTesterFactory(IApiHelper apiHelper)
        {
            _apiHelper = apiHelper;
        }

        public T Create<T>(Address contractAddress, string account, string password = "123", bool notimeout = true)
            where T : ContractStubBase, new()
        {
            var factory = new MethodStubFactory(_apiHelper)
            {
                SenderAddress = account,
                Contract = contractAddress
            };
            var timeout = notimeout ? "notimeout" : "";
            _apiHelper.UnlockAccount(new CommandInfo(ApiMethods.AccountUnlock)
            {
                Parameter = $"{account} {password} {timeout}"
            });

            return new T() {__factory = factory};
        }

        public T Create<T>(Address contractAddress, ECKeyPair keyPair) 
            where T : ContractStubBase, new()
        {
            var factory = new MethodStubFactory(_apiHelper)
            {
                SenderAddress = Address.FromPublicKey(keyPair.PublicKey).GetFormatted(),
                Contract = contractAddress
            };

            return new T() {__factory = factory};
        }
    }
}