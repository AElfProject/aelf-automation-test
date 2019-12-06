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
}