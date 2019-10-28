using AElfChain.SDK.Models;

namespace AElf.Automation.CheckTxStatus
{
    public class TransactionInfo
    {
        public readonly TransactionDto TransactionDto;
        public readonly string Status;
        public readonly string From;
        public string To;
        public string MethodName;
        public long RefBlockNumber;
        public string RefBlockPrefix;
        public string Params;
        
        
        public TransactionInfo(TransactionDto tx, string status)
        {
            Status = status;
            TransactionDto = tx;
            From = tx.From;
            To = tx.To;
            MethodName = tx.MethodName;
            RefBlockNumber = tx.RefBlockNumber;
            RefBlockPrefix = tx.RefBlockPrefix;
            Params = tx.Params;
        }
    }
}