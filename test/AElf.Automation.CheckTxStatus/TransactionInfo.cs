using AElf.Client.Dto;

namespace AElf.Automation.CheckTxStatus
{
    public class TransactionInfo
    {
        public readonly string From;
        public readonly string Status;
        public readonly TransactionDto TransactionDto;
        public string MethodName;
        public string Params;
        public long RefBlockNumber;
        public string RefBlockPrefix;
        public string To;


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