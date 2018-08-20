using System;

namespace AElf.Automation.CliTesting.Wallet.Exceptions
{
    public class ContractLoadedException : Exception
    {
        public ContractLoadedException() : base("Contract loading failed")
        {
            
        }
    }
}