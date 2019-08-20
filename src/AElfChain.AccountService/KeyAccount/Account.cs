using System.Threading;
using AElf.Cryptography.ECDSA;

namespace AElfChain.AccountService.KeyAccount
{
    public class Account
    {
        // Close account when time out 
        public Timer LockTimer { private get; set; }
        public ECKeyPair KeyPair { get; set; }
        public string AccountName { get; }

        public Account(string address)
        {
            AccountName = address;
        }

        public void Lock()
        {
            LockTimer.Dispose();
        }
    }
}