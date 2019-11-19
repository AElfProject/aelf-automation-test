namespace AElf.Automation.RpcPerformance
{
    public class AccountInfo
    {
        public AccountInfo(string account)
        {
            Account = account;
            Balance = 0;
        }

        public string Account { get; }
        public int Balance { get; set; }
    }
}