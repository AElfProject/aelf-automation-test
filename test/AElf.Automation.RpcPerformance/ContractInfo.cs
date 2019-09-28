namespace AElf.Automation.RpcPerformance
{
    public class ContractInfo
    {
        public string ContractPath { get; }
        public string Symbol { get; set; }
        public string Owner { get; private set; }

        public ContractInfo(string owner, string contractPath)
        {
            Owner = owner;
            ContractPath = contractPath;
        }
    }
}