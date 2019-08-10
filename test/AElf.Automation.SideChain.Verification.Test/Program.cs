using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using AElf.Automation.Common.Helpers;
using McMaster.Extensions.CommandLineUtils;

namespace AElf.Automation.SideChain.Verification.Test
{
    [Command(Name = "Transaction Client", Description = "Monitor contract transaction testing client.")]
    [HelpOption("-?")]
    class Program
    {
        #region Parameter Option

        [Option("-ruM|--mainRpc.url", Description = "Rpc service url of node. It's required parameter.")]
        public string MainUrl { get; }

        [Option("-ruS|--sideRpc.url", Description = "Rpc service url of node. It's required parameter.")]
        public string SideUrl { get; }

        [Option("-ac|--chain.account", Description = "Main Chain account, It's required parameter.")]
        public static string InitAccount { get; }

        [Option("-em|--execute.mode", Description =
            "Transaction execution mode include: \n0. Not set \n1. Verify main chain transaction \n2. Verify side chain transaction \n3. Cross Chain Transfer ")]
        public int ExecuteMode { get; } = 0;

        public int ThreadCount { get; } = 1;
        public int TransactionGroup { get; } = 10;

        #endregion


        public static List<string> SideUrls { get; set; }
        private static readonly ILog Logger = Log.GetLogHelper();

        public static int Main(string[] args)
        {
            if (args.Length != 3) return CommandLineApplication.Execute<Program>(args);

            var ruM = args[0];
            var ruS = args[1];
            var ac = args[2];
            args = new[] {"-ruM", ruM, "-ruS", ruS, "-ac", ac, "-em", "0"};

            return CommandLineApplication.Execute<Program>(args);
        }

        private void OnExecute(CommandLineApplication app)
        {
            if (MainUrl == null)
            {
                app.ShowHelp();
                return;
            }

            var sides = SideUrl.Split(",");
            SideUrls = new List<string>(sides);

            var operationSet = new OperationSet(ThreadCount, TransactionGroup, InitAccount, SideUrls, MainUrl);

            //Init Logger
            var logName = "CrossCHainTh_" + operationSet.ThreadCount + "_Tx_" + operationSet.ExeTimes + "_" +
                          DateTime.Now.ToString("MMddHHmmss") + ".log";
            var dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", logName);
            Logger.InitLogHelper(dir);

            //Execute transaction command
            try
            {
                operationSet.InitMainExecCommand();

                ExecuteOperation(operationSet, ExecuteMode);
            }
            catch (Exception e)
            {
                Logger.Error("Message: " + e.Message);
                Logger.Error("Source: " + e.Source);
                Logger.Error("StackTrace: " + e.StackTrace);
            }
            finally
            {
                //Delete accounts
                operationSet.DeleteAccounts();
            }

            //Result summary
            var set = new CategoryInfoSet(operationSet.MainChain.ApiHelper.CommandList);
            set.GetCategoryBasicInfo();
            set.GetCategorySummaryInfo();
            var xmlFile = set.SaveTestResultXml(operationSet.ThreadCount, operationSet.ExeTimes);
            Logger.Info("Log file: {0}", dir);
            Logger.Info("Xml file: {0}", xmlFile);
            Logger.Info("Complete performance testing.");
        }

        private static void ExecuteOperation(OperationSet operationSet, int execMode = 0)
        {
            if (execMode == 0)
            {
                Logger.Info("Select execution type:");
                Console.WriteLine("1. Verify main chain transaction");
                Console.WriteLine("2. Verify side chain transaction");
                Console.WriteLine("3. Cross Chain Transfer");
                Console.Write("Input selection:");

                var runType = Console.ReadLine();
                var check = int.TryParse(runType, out execMode);
                if (!check)
                {
                    Logger.Info("Wrong input, please input again.");
                    ExecuteOperation(operationSet);
                }
            }

            var tm = (TestMode) execMode;
            switch (tm)
            {
                case TestMode.VerifyMainTx:
                    Logger.Info($"Run with verify main chain transaction: {tm.ToString()}.");
                    Console.Write("Input the block number: ");
                    var blockNumber = Console.ReadLine();
                    operationSet.MainChainTransactionVerifyOnSideChains(blockNumber);
                    break;
                case TestMode.VerifySideTx:
                    Logger.Info($"Run with verify side chain transaction: {tm.ToString()}.");
                    Console.Write("Input the side number: ");
                    var sideChainNum = Console.ReadLine();
                    var num = int.Parse(sideChainNum);
                    if (num > SideUrls.Count + 1)
                    {
                        Logger.Info("Wrong input, please input again.");
                        ExecuteOperation(operationSet);
                    }

                    operationSet.SideChainTransactionVerifyOnMainChain(num - 1);
                    break;
                case TestMode.CrossChainTransfer:
                    Logger.Info($"Run with cross chain transfer: {tm.ToString()}.");
                    operationSet.CrossChainTransferToInitAccount();
                    operationSet.MultiCrossChainTransfer();
                    break;
                case TestMode.NotSet:
                    break;
                default:
                    Logger.Info("Wrong input, please input again.");
                    ExecuteOperation(operationSet);
                    break;
            }
        }
    }
}