using System;
using AElf.Automation.Common.Helpers;

namespace AElf.Automation.CheckTxStatus
{
    class Program
    {
        static void Main(string[] args)
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