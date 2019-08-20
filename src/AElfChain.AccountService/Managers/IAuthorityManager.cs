using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Types;
using Google.Protobuf;

namespace AElfChain.AccountService
{
    public interface IAuthorityManager
    {
        Task<Address> DeployContractWithAuthority(string caller, string contractName);
        
        Task<TransactionResult> ExecuteTransactionWithAuthority(string contractAddress, string method, IMessage input,
            Address organizationAddress, IEnumerable<string> approveUsers, string callUser);
    }
}