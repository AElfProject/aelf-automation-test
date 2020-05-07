namespace AElf.Automation.AutoTransfer
{
    public class AccountInfo
    {
        public AccountInfo(string account, string accountPassword)
        {
            Account = account;
            Password = accountPassword;
        }

        public string Account { get; }
        public string Password { get; }
    }
}