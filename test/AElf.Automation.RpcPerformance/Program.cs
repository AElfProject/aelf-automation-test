using System;
using System.IO;
using AElf.Automation.Common.Helpers;
using McMaster.Extensions.CommandLineUtils;

namespace AElf.Automation.RpcPerformance
{
    [Command(Name = "Transaction Client", Description = "Monitor contract transaction testing client.")]
    [HelpOption("-?")]
    class Program
    {
        #region Parameter Option

        [Option("-tc|--thread.count", Description =
            "Thread count to execute transactions. Default value is 4")]
        public int ThreadCount { get; } = 4;

        [Option("-tg|--transaction.group", Description =
            "Transaction count to execute of each round or one round. Default value is 10.")]
        public int TransactionGroup { get; } = 10;

        [Option("-ru|--rpc.url", Description = "Rpc service url of node. It's required parameter.")]
        public string RpcUrl { get; }

        [Option("-em|--execute.mode", Description =
            "Transaction execution mode include: \n0. Not set \n1. Normal mode \n2. Continuous Tx mode \n3. Batch mode \n4. Continuous Txs mode")]
        public int ExecuteMode { get; } = 0;

        #endregion

        static readonly ILogHelper Logger = LogHelper.GetLogHelper();

        public static int Main(string[] args)
        {
            if (args.Length != 3) return CommandLineApplication.Execute<Program>(args);
            
            var tc = args[0];
            var tg = args[1];
            var ru = args[2];
            args = new[] {"-tc", tc, "-tg", tg, "-ru", ru, "-em", "0"};

            return CommandLineApplication.Execute<Program>(args);
        }

        private void OnExecute(CommandLineApplication app)
        {
            if (ThreadCount == 0 || TransactionGroup == 0 || RpcUrl == null)
            {
                app.ShowHelp();
                return;
            }

            var performance = new ExecutionCategory(ThreadCount, TransactionGroup, RpcUrl);
            //Init Logger
            var logName = "RpcTh_" + performance.ThreadCount + "_Tx_" + performance.ExeTimes +"_"+ DateTime.Now.ToString("MMddHHmmss") + ".log";
            var dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", logName);
            Logger.InitLogHelper(dir);

            //Execute transaction command
            try
            {
                performance.InitExecCommand();
                performance.DeployContracts();
                performance.InitializeContracts();

                ExecuteRpcTask(performance, ExecuteMode);
            }
            catch (Exception e)
            {
                Logger.WriteError("Message: " + e.Message);
                Logger.WriteError("Source: " + e.Source);
                Logger.WriteError("StackTrace: " + e.StackTrace);
            }
            finally
            {
                //Delete accounts
                performance.DeleteAccounts();
            }

            //Result summary
            var set = new CategoryInfoSet(performance.ApiHelper.CommandList);
            set.GetCategoryBasicInfo();
            set.GetCategorySummaryInfo();
            var xmlFile = set.SaveTestResultXml(performance.ThreadCount, performance.ExeTimes);
            Logger.WriteInfo("Log file: {0}", dir);
            Logger.WriteInfo("Xml file: {0}", xmlFile);
            Logger.WriteInfo("Complete performance testing.");
        }

        private static void ExecuteRpcTask(ExecutionCategory performance, int execMode = 0)
        {
            if (execMode == 0)
            {
                Logger.WriteInfo("Select execution type:");
                Console.WriteLine("1. Normal mode");
                Console.WriteLine("2. Continue Tx mode");
                Console.WriteLine("3. Batch mode");
                Console.WriteLine("4. Continue Txs mode");
                Console.Write("Input selection: ");

                var runType = Console.ReadLine();
                var check = Int32.TryParse(runType, out execMode);
                if (!check)
                {
                    Logger.WriteInfo("Wrong input, please input again.");
                    ExecuteRpcTask(performance);
                }
            }

            var tm = (TestMode) execMode;
            switch (tm)
            {
                case TestMode.CommonTx:
                    Logger.WriteInfo($"Run with tx mode: {tm.ToString()}.");
                    performance.ExecuteOneRoundTransactionTask();
                    break;
                case TestMode.ContinuousTx:
                    Logger.WriteInfo($"Run with continuous tx mode: {tm.ToString()}.");
                    performance.ExecuteContinuousRoundsTransactionsTask();
                    break;
                case TestMode.BatchTxs:
                    Logger.WriteInfo($"Run with txs mode: {tm.ToString()}.");
                    performance.ExecuteOneRoundTransactionsTask();
                    break;
                case TestMode.ContinuousTxs:
                    Logger.WriteInfo($"Run with continuous txs mode: {tm.ToString()}.");
                    performance.ExecuteContinuousRoundsTransactionsTask(true);
                    break;
                case TestMode.NotSet:
                    break;
                default:
                    Logger.WriteInfo("Wrong input, please input again.");
                    ExecuteRpcTask(performance);
                    break;
            }

            performance.PrintContractInfo();
        }
    }
}