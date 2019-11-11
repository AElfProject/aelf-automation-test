using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Cryptography.ECDSA;

namespace AElf.Automation.Common.Managers
{
    public interface IKeyStore
    {
        Task<KeyStoreErrors> UnlockAccountAsync(string address, string password, bool withTimeout = true);

        ECKeyPair GetAccountKeyPair(string address);

        Task<ECKeyPair> CreateAccountKeyPairAsync(string password);

        Task<List<string>> GetAccountsAsync();
    }
}