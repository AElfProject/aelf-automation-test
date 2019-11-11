using AElfChain.Common.Helpers;

namespace AElf.Automation.CheckTxStatus
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            #region Basic Preparation

            //Init Logger
            Log4NetHelper.LogInit("CheckTransaction_");

            #endregion

            var transactionCheck = new TransactionCheck();
            transactionCheck.CheckTxStatus();
        }
    }
}