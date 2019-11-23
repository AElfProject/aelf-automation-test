namespace AElf.Automation.SideChain.Verification
{
    public class ChainInfo
    {
        public string ChainId { get; }
        public string PrimaryTokenSymbol { get; }

        public ChainInfo( string chainId, string primaryTokenSymbol)
        {
            ChainId = chainId;
            PrimaryTokenSymbol = primaryTokenSymbol;
        }
    }
}