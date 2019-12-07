using System;
using AElf;
using AElf.Types;

namespace AElfChain.Common.Utils
{
    public static class TransactionUtil
    {
        public static string CalculateTxId(string rawTx)
        {
            var byteArray = ByteArrayHelper.HexStringToByteArray(rawTx);
            var transaction = Transaction.Parser.ParseFrom(byteArray);
            return transaction.GetHash().ToHex();
        }
    }
    
//    public static class TransactionResultStatusExtension
//    {
//        public static TransactionResultStatus ConvertTransactionResultStatus(this string status)
//        {
//            return (TransactionResultStatus) Enum.Parse(typeof(TransactionResultStatus), status, true);
//        }
//    }
}