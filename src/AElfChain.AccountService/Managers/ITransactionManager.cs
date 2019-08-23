using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Types;
using AElfChain.SDK.Models;
using Google.Protobuf;

namespace AElfChain.AccountService
{
    public interface ITransactionManager
    {
        Task<Transaction> CreateTransaction(string from, string to, string method, ByteString input);
        Task<string> SendTransactionWithIdAsync(Transaction transaction);
        Task<List<string>> SendBatchTransactionWithIdAsync(List<Transaction> transaction);
        Task<TransactionResultDto> QueryTransactionAsync(string transactionId);
        string ConvertTransactionToRawInfo(Transaction transaction);
    }
}