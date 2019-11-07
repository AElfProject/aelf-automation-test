namespace AElf.Automation.RpcPerformance
{
    public class ContractInfo
    {
        public string ContractAddress { get; }
        public string Symbol { get; set; }
        public string Owner { get; private set; }

        public ContractInfo(string owner, string contractAddress)
        {
            Owner = owner;
            ContractAddress = contractAddress;
        }
    }
}