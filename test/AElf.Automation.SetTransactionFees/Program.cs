using AElf.Automation.Common.Helpers;
using AElf.Automation.Common.Managers;
using log4net;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AElf.Automation.SetTransactionFees
{
    class Program
    {
        private static readonly ILog Logger = Log4NetHelper.GetLogger();
        
        [Option("-e|--endpoint", Description = "Node service endpoint info")]
        public string Endpoint { get; set; } = "http://192.168.197.43:8100";
        
        [Option("-a|--amount", Description = "Transaction method fee balance")]
        public long Amount { get; set; } = 1000_0000L;
        
        public static int Main(string[] args)
        {
            try
            {
                return CommandLineApplication.Execute<Program>(args);
            }
            catch (AssertFailedException ex)
            {
                Logger.Error($"Execute failed: {ex.Message}");
            }

            return 0;
        }
        
        private void OnExecute()
        {
            //Init Logger
            Log4NetHelper.LogInit("ContractFee");

            var nm = new NodeManager(Endpoint);
            var contractsFee = new ContractsFee(nm);
            //before
            contractsFee.QueryAllContractsMethodFee();
            //set fee
            contractsFee.SetAllContractsMethodFee(Amount);
            //after
            contractsFee.QueryAllContractsMethodFee();
            
            Logger.Info("All contract methods fee set completed.");
        }
    }
}