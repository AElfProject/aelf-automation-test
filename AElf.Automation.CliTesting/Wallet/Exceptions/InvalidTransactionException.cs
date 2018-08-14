using System;

namespace AElf.Automation.CliTesting.Wallet.Exceptions
{
    public class InvalidTransactionException : Exception
    {
        public InvalidTransactionException() : base("Invalid transaction data.")
        {
            
        }
    }
}