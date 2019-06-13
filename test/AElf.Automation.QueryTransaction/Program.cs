using System;
using System.IO;
using AElf.Automation.Common.Helpers;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Volo.Abp.Threading;

namespace AElf.Automation.QueryTransaction
{
    class Program
    {
        private static readonly ILogHelper Logger = LogHelper.GetLogHelper();
        
        [Option("-e|--endpoint", Description = "Node service endpoint info")]
        public string Endpoint { get; set; } = "http://34.212.171.27:8000";
        
        public static int Main(string[] args)
        {
            try
            {
                return CommandLineApplication.Execute<Program>(args);
            }
            catch (AssertFailedException ex)
            {
                Logger.WriteError($"Execute failed: {ex.Message}");
            }

            return 0;
        }

        private void OnExecute()
        {
            //Init Logger
            var logName = "TransactionQuery" + DateTime.Now.ToString("MMddHHmmss") + ".log";
            var dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", logName);
            Logger.InitLogHelper(dir);
            
            var query = new TransactionQuery(Endpoint);
            query.ExecuteMultipleTasks(1);
            Logger.WriteInfo("Complete transaction query result.");
        }
    }
}