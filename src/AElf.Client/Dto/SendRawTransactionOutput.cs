namespace AElf.Client.Dto
{
    public class SendRawTransactionOutput
    {
        public string TransactionId { get; set; }

        public TransactionDto Transaction { get; set; }
    }
}