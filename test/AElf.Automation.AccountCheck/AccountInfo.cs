using System.Collections.Generic;
using AElfChain.Common.Contracts;

namespace AElf.Automation.AccountCheck
{
    public class AccountInfo
    {
        public AccountInfo(string account, long balance)
        {
            Account = account;
            Balance = balance;
        }

        public string Account { get; set; }
        public long Balance { get; set; }
    }
}