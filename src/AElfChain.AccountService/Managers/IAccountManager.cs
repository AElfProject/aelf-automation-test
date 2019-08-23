using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Types;

namespace AElfChain.AccountService
{
    public interface IAccountManager
    {
        Task<List<string>> ListAccount();
        Task<bool> AccountIsExist(string account);
        Task<AccountInfo> NewAccountAsync(string password = AccountOption.DefaultPassword);
        Task<bool> UnlockAccountAsync(string account, string password = AccountOption.DefaultPassword, bool notimeout = true);
        Task<AccountInfo> GetAccountInfoAsync(string account, string password = AccountOption.DefaultPassword);
        Task<AccountInfo> GetRandomAccountInfoAsync();
        Task<Transaction> SignTransactionAsync(Transaction transaction);
    }
}