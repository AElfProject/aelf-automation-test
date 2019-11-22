namespace AElf.Automation.SideChain.Verification
{
    public class CrossChainTransactionInfo
    {
        public CrossChainTransactionInfo(long blockHeight, string txId, string rawTx, string fromAccount,
            string receiveAccount)
        {
            TxId = txId;
            BlockHeight = blockHeight;
            RawTx = rawTx;
            FromAccount = fromAccount;
            ReceiveAccount = receiveAccount;
        }

        public CrossChainTransactionInfo(string txId, string receiveAccount)
        {
            TxId = txId;
            ReceiveAccount = receiveAccount;
        }

        public CrossChainTransactionInfo(long blockHeight, string txId, string rawTx)
        {
            TxId = txId;
            BlockHeight = blockHeight;
            RawTx = rawTx;
        }

        public CrossChainTransactionInfo(string txId, string rawTx, string fromAccount, string receiveAccount)
        {
            TxId = txId;
            RawTx = rawTx;
            FromAccount = fromAccount;
            ReceiveAccount = receiveAccount;
        }

        public string TxId { get; }
        public long BlockHeight { get; }
        public string RawTx { get; }
        public string FromAccount { get; }
        public string ReceiveAccount { get; }
    }
}