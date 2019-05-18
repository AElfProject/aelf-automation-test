using System;
using System.Collections.Generic;
using System.IO;
using AElf.Automation.Common.Helpers;
using AElf.Automation.ScenariosExecution.Scenarios;

namespace AElf.Automation.ScenariosExecution
{
    class Program
    {
        #region Private Properties

        private static readonly ILogHelper Logger = LogHelper.GetLogHelper();

        private static ConfigInfo ConfigInfo { get; set; }
        private static List<string> Users { get; set; }

        private static TokenExecutor Executor { get; set; }

        #endregion

        static void Main(string[] args)
        {
            #region Basic Preparation
            //Init Logger
            var logName = "ScenarioTest_" + DateTime.Now.ToString("MMddHHmmss") + ".log";
            var dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", logName);
            Logger.InitLogHelper(dir);
            
            #endregion

            var token = new TokenScenario();
            token.PrepareAccountBalance();
            token.ExecuteContinuousTasks(new Action[]
            {
                token.TransferAction,
                token.ApproveTransferAction,
                token.GetChainTransactionAction
            });
            
            Console.ReadLine();
        }
    }
}