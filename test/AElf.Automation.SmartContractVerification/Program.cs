using System;
using AElf.Contracts.Resource;

namespace AElf.Automation.SmartContractVerification
{
    class Program
    {
        static void Main(string[] args)
        {
            //Resource Contract
            var rc = new ResourceContract();
            rc.Initialize();

            Console.WriteLine("Hello World!");
        }
    }
}