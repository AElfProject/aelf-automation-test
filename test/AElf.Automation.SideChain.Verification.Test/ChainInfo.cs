namespace AElf.Automation.SideChain.Verification
{
    public class ChainInfo
    {
        public ChainInfo(string chainId, string primaryTokenSymbol)
        {
            ChainId = chainId;
            PrimaryTokenSymbol = primaryTokenSymbol;
        }

        public string ChainId { get; }
        public string PrimaryTokenSymbol { get; }
    }
}