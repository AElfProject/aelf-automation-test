namespace AElf.Automation.SideChain.Verification.Verify
{
    public class CrossChainTransactionVerifyResult
    {
        public CrossChainTransactionVerifyResult(string result, int chainId, string txId)
        {
            Result = result;
            ChainId = chainId;
            TxId = txId;
        }

        public string Result { get; }
        public int ChainId { get; }
        public string TxId { get; }
    }
}