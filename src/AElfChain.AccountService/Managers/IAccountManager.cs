using System.Collections.Generic;
using System.Threading.Tasks;

namespace AElfChain.AccountService
{
    public interface IAccountManager
    {
        Task<List<string>> ListAccount();
        Task<bool> AccountIsExist(string account);
        Task<AccountInfo> NewAccountAsync(string password = "123");
        Task<bool> UnlockAccountAsync(string account, string password = "123", bool notimeout = true);
        Task<AccountInfo> GetAccountInfoAsync(string account, string password = "123");

        Task<byte[]> SignAsync(AccountInfo accountInfo, byte[] data);
        Task<byte[]> SignAsync(string account, string password, byte[] data);
    }
}