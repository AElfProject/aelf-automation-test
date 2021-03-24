using System;
using System.Threading;
using AElfChain.Common;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using log4net;
using McMaster.Extensions.CommandLineUtils;
using Shouldly;

namespace AElf.Automation.RpcPerformance
{
    internal class Program
    {
        private static ILog Logger { get; set; }

        public static void Main()
        {
            if (ConfigFile != null) NodeInfoHelper.SetConfig(ConfigFile);

            //Init Logger
            var fileName = $"GC_{GroupCount}_TC_{TransactionCount}_Hour_{DateTime.Now.Hour:00}";
            Log4NetHelper.LogInit(fileName);
            Logger = Log4NetHelper.GetLogger();

            var transactionType = RpcConfig.ReadInformation.RandomSenderTransaction;
            var performance = transactionType
                ? (IPerformanceCategory) new RandomCategory(GroupCount, TransactionCount, RpcUrl,TransactionGroup)
                : new ExecutionCategory(GroupCount, TransactionCount, RpcUrl,TransactionGroup);

            //Execute transaction command
            try
            {
                performance.InitExecCommand(UserCount);
                performance.DeployContracts();
                performance.InitializeMainContracts();
                ExecuteTransactionPerformanceTask(performance);
            }
            catch (TimeoutException e)
            {
                Logger.Error(e.Message);
            }
            catch (ShouldAssertException e)
            {
                Logger.Error(e.Message);
            }
            catch (Exception e)
            {
                var message = $"Message: {e.Message}\r\nSource: {e.Source}\r\nStackTrace: {e.StackTrace}";
                Logger.Error(message);
            }

            //Result summary
            Logger.Info("Complete performance testing.");
        }

        private static void ExecuteTransactionPerformanceTask(IPerformanceCategory performance)
        {
            Logger.Info("Run with continuous txs mode: ContinuousTxs.");
            performance.ExecuteContinuousRoundsTransactionsTask(true);
            performance.PrintContractInfo();
        }

        #region Parameter Option

        private static string ConfigFile { get; set; }
        private static int GroupCount { get; } = RpcConfig.ReadInformation.GroupCount;
        private static int TransactionCount { get; } = RpcConfig.ReadInformation.TransactionCount;
        private static int TransactionGroup { get; } = RpcConfig.ReadInformation.TransactionGroup;

        private static int UserCount { get; } = RpcConfig.ReadInformation.UserCount;
        private static string RpcUrl { get; } = RpcConfig.ReadInformation.ServiceUrl;

        #endregion
    }
}