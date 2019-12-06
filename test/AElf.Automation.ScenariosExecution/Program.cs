using System;
using AElfChain.Common.Helpers;
using AElfChain.Common;
using McMaster.Extensions.CommandLineUtils;
using log4net;

namespace AElf.Automation.ScenariosExecution
{
    internal class Program
    {
        #region Private Properties

        private static ILog Logger { get; set; }
        
        [Option("-c|--config", Description = "Config file about bp node setting")]
        private static string ConfigFile { get; set; }

        #endregion

        public static int Main(string[] args)
        {
            return CommandLineApplication.Execute<Program>(args);
        }

        private void OnExecute(CommandLineApplication app)
        {
            if (ConfigFile != null) NodeInfoHelper.SetConfig(ConfigFile);
            Log4NetHelper.LogInit($"ScenarioTest_Hour_{DateTime.Now.Hour:00}");
            Logger = Log4NetHelper.GetLogger();
            
            //prepare account
            OldEnvCheck.PrepareTestAccounts();
            
            var multipleTasks = new MultipleTasks();
            multipleTasks.RunScenariosByTasks();

            Console.ReadLine();
        }
    }
}