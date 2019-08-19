namespace AElf.Automation.SideChain.Verification
{
    public class CrossChainTransactionInfo
    {
        public string TxId { get; set; }
        public long BlockHeight { get; set; }
        public string RawTx { get; set; }
        public string FromAccount { get; set; }
        public string ReceiveAccount { get; set; }

        public CrossChainTransactionInfo(long blockHeight, string txId, string rawTx, string fromAccount,
            string receiveAccount)
        {
            TxId = txId;
            BlockHeight = blockHeight;
            RawTx = rawTx;
            FromAccount = fromAccount;
            ReceiveAccount = receiveAccount;
        }

        public CrossChainTransactionInfo( string txId,string receiveAccount)
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
        
        public CrossChainTransactionInfo( string txId,string rawTx, string fromAccount,string receiveAccount)
        {
            TxId = txId;
            RawTx = rawTx;
            FromAccount = fromAccount;
            ReceiveAccount = receiveAccount;
        }
    }
}