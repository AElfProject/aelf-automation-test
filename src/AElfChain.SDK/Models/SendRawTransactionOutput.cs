namespace AElfChain.SDK.Models
{
    public class SendRawTransactionOutput
    {
        public string TransactionId { get; set; }

        public TransactionDto Transaction { get; set; }
    }
}