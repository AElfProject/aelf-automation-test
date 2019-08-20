using System.Threading.Tasks;
using AElf.Types;
using Google.Protobuf;

namespace AElfChain.AccountService
{
    public interface ITransactionManager
    {
        Transaction CreateTransaction(string from, string to, string methodName, ByteString input);
        Task<Transaction> AddBlockReference(Transaction transaction);
        Task<Transaction> SignTransaction(Transaction transaction, string password = "123");
        string ConvertTransactionToRawInfo(Transaction transaction);
    }
}