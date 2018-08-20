using System;

namespace AElf.Automation.CliTesting.Wallet.Exceptions
{
    public class AccountLockedException : Exception
    {
        public string Account { get; private set; }
        
        public AccountLockedException(string account) : base("The following account is locked: " + account)
        {
            Account = account;
        }
    }
}