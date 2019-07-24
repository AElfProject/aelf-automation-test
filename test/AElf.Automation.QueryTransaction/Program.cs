using System;
using System.Collections.Generic;
using System.IO;
using AElf.Automation.Common.Helpers;
using log4net;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Volo.Abp.Threading;

namespace AElf.Automation.QueryTransaction
{
    class Program
    {
        private static readonly ILog Logger = Log4NetHelper.GetLogger();

        [Option("-e|--endpoint", Description = "Node service endpoint info")]
        public string Endpoint { get; set; } = "http://192.168.197.35:8000";

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
            Log4NetHelper.LogInit("TransactionQuery");
            Logger.Info("Select execution type:");
            "1. RunQueryTransaction".WriteSuccessLine();
            "2. RunNodeStatusCheck".WriteSuccessLine();
            "3. RunStressTest".WriteSuccessLine();
            var runType = Console.ReadLine();
            var check = int.TryParse(runType, out var selection);

            if (!check)
            {
                Logger.Error("Wrong selection input.");
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
                case 3:
                    RunStressTest();
                    break;
            }

            Logger.Info("Complete testing.");
            Console.ReadLine();
        }

        private void RunNodeStatusCheck()
        {
            var urlCollection = new List<string>
            {
                "http://192.168.197.13:8000",
                "http://192.168.197.28:8000",
                "http://192.168.197.33:8000"
                /*
                "http://34.221.114.160:8000",
                "http://34.222.242.234:8000",
                "http://3.1.220.141:8000",
                "http://18.202.227.136:8000",
                "http://54.234.13.11:8000",
                "http://54.252.210.175:8000",
                "http://54.238.196.57:8000",
                "http://35.183.236.26:8000",
                "http://54.233.160.136:8000",
                "http://13.57.57.68:8000",
                "http://52.66.193.22:8000",
                "http://54.180.120.54:8000",
                "http://35.159.19.62:8000",
                "http://52.56.201.142:8000",
                "http://35.180.189.97:8000",
                "http://13.48.78.131:8000",
                "http://18.191.36.156:8000",
                "http://8.208.23.245:8000",
                "http://47.254.233.45:8000"
                */
            };
            var status = new NodesStatus(urlCollection);
            AsyncHelper.RunSync(() => status.CheckAllNodes());
        }

        private void RunQueryTransaction()
        {
            var query = new TransactionQuery(Endpoint);
            query.ExecuteMultipleTasks(1);
            Logger.Info("Complete transaction query result.");
        }

        private void RunStressTest()
        {
            var stress = new StressQuery(Endpoint);
            stress.RunStressTest(300);
        }
    }
}