using AElf.Cryptography.ECDSA;
using AElf.CSharp.Core;
using AElf.Types;
using AElfChain.Common.Managers;

namespace AElfChain.Common.Contracts
{
    public interface IContractTesterFactory
    {
        T Create<T>(Address contractAddress, string account, string password = "", bool notimeout = true)
            where T : ContractStubBase, new();

        T Create<T>(Address contractAddress, ECKeyPair keyPair)
            where T : ContractStubBase, new();
    }

    public class ContractTesterFactory : IContractTesterFactory
    {
        private readonly INodeManager _nodeManager;

        public ContractTesterFactory(INodeManager nodeManager)
        {
            _nodeManager = nodeManager;
        }

        public T Create<T>(Address contractAddress, string account, string password = "", bool notimeout = true)
            where T : ContractStubBase, new()
        {
            if (password == "")
                password = NodeOption.DefaultPassword;

            var factory = new MethodStubFactory(_nodeManager)
            {
                SenderAddress = account,
                Contract = contractAddress
            };

            _nodeManager.UnlockAccount(account, password);

            return new T {__factory = factory};
        }

        public T Create<T>(Address contractAddress, ECKeyPair keyPair)
            where T : ContractStubBase, new()
        {
            var factory = new MethodStubFactory(_nodeManager)
            {
                SenderAddress = Address.FromPublicKey(keyPair.PublicKey).ToBase58(),
                Contract = contractAddress
            };

            return new T {__factory = factory};
        }
    }
}