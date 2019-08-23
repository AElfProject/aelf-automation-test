using System.Collections;
using System.Threading.Tasks;
using AElf.CSharp.Core;
using AElf.Types;

namespace AElfChain.ContractService
{
    public interface ISystemContract
    {
        Hash GetContractHashName(SystemContracts contract);
        Task<Address> GetSystemContractAddress(SystemContracts contract);
        TStub GetTestStub<TStub>(Address contract, string caller) where TStub : ContractStubBase;
    }
}