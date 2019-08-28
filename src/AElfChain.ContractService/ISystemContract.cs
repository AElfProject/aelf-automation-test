using System.Collections;
using System.Threading.Tasks;
using AElf.CSharp.Core;
using AElf.Types;
using AElfChain.AccountService;

namespace AElfChain.ContractService
{
    public interface ISystemContract
    {
        Hash GetContractHashName(SystemContracts contract);
        Task<Address> GetSystemContractAddressAsync(SystemContracts contract);
        TStub GetTestStub<TStub>(Address contract, AccountInfo accountInfo) where TStub : ContractStubBase, new();
    }
}