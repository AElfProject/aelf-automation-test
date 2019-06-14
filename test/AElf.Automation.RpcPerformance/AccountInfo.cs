namespace AElf.Automation.RpcPerformance
{
    public class AccountInfo
    {
        public string Account { get; }
        public int Balance { get; set; }

        public AccountInfo(string account)
        {
            Account = account;
            Balance = 0;
        }
    }
}