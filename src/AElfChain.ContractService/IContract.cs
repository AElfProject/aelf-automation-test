using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.CSharp.Core;
using AElf.Types;
using AElfChain.SDK.Models;
using Google.Protobuf;

namespace AElfChain.ContractService
{
    public interface IContract
    {
        void SetContractExecutor(string account);
        
        Task<string> SendTransactionWithIdAsync(string method, IMessage input);
        Task<List<string>> SendBatchTransactionsWithIdAsync(List<string> rawInfos);
        Task<TransactionResultDto> SendTransactionWithResultAsync(string method, IMessage input);
        TResult CallTransactionAsync<TResult>(string method, IMessage input) where TResult : IMessage<TResult>;
        
        TStub GetTestStub<TStub>(Address contract, string caller) where TStub : ContractStubBase;
        
        TContract GetContractService<TContract>(Address contract, string caller) where TContract : IContract;
    }
}