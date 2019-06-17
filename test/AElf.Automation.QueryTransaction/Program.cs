using System;
using System.Collections.Generic;
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
        public string Endpoint { get; set; } = "http://192.168.197.13:8100";
        
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
            
            Logger.WriteInfo("Select execution type:");
            "1. RunQueryTransaction".WriteSuccessLine();
            "2. RunNodeStatusCheck".WriteSuccessLine();
            var runType = Console.ReadLine();
            var check = int.TryParse(runType, out var selection);

            if (!check)
            {
                Logger.WriteError("Wrong selection input.");
                return;
            }

            switch (selection)
            {
                case 1:
                    RunQueryTransaction();
                    break;
                case 2:
                    RunNodeStatusCheck();
                    break;
            }
        }

        private void RunNodeStatusCheck()
        {
            var urlCollection = new List<string>
            {
                "http://192.168.197.13:8100",
                "http://192.168.197.28:8100",
                "http://192.168.197.33:8100",
                "http://192.168.197.13:8200",
                "http://192.168.197.28:8200",
                "http://192.168.197.33:8200",
                "http://192.168.197.13:8300",
                "http://192.168.197.28:8300",
                "http://192.168.197.33:8300"
            };
            var status = new NodesStatus(urlCollection);
            AsyncHelper.RunSync(()=>status.CheckAllNodes());
        }

        private void RunQueryTransaction()
        {
            var query = new TransactionQuery(Endpoint);
            query.ExecuteMultipleTasks(1);
            Logger.WriteInfo("Complete transaction query result.");
        }
    }
}