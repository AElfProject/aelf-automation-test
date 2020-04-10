using AElf.Client.Dto;
using AElf.Types;
using Google.Protobuf;

namespace AElfChain.Common.DtoExtension
{
    public static class TransactionDtoExtension
    {
        public static int GetTxSize(this TransactionDto transactionDto)
        {
            var transaction = new Transaction
            {
                From = transactionDto.From.ConvertAddress(),
                To = transactionDto.To.ConvertAddress(),
                Params = ByteString.FromBase64(transactionDto.Params),
                Signature = ByteString.FromBase64(transactionDto.Signature),
                RefBlockNumber = transactionDto.RefBlockNumber,
                RefBlockPrefix = ByteString.FromBase64(transactionDto.RefBlockPrefix)
            };

            return transaction.CalculateSize();
        }
    }
}