using AElf.CSharp.Core;
using AElf.Types;
using AElfChain.AccountService;
using Microsoft.Extensions.Logging;

namespace AElfChain.ContractService
{
    public interface IContractFactory
    {
        T Create<T>(Address contract, AccountInfo accountInfo)
            where T : ContractStubBase, new();
    }
    public class ContractFactory : IContractFactory
    {
        public ILogger Logger { get; set; }
        private readonly IMethodStubFactory _methodStubFactory;

        public ContractFactory(IMethodStubFactory methodStubFactory, ILoggerFactory loggerFactory)
        {
            Logger = loggerFactory.CreateLogger<ContractFactory>();
            
            _methodStubFactory = methodStubFactory;
        }
        
        public T Create<T>(Address contract, AccountInfo accountInfo) where T : ContractStubBase, new()
        {
            if (!(_methodStubFactory is MethodStubFactory stub)) return null;
                
            var factory = stub.GetMethodStubFactory(contract, accountInfo);

            return new T() {__factory = factory};
        }
    }
}