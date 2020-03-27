using System;
using AElfChain.Common;
using AElfChain.Common.Helpers;
using log4net;
using McMaster.Extensions.CommandLineUtils;

namespace AElf.Automation.ScenariosExecution
{
    internal class Program
    {
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
            EnvPreparation.PrepareTestAccounts();

            var multipleTasks = new MultipleTasks();
            multipleTasks.RunScenariosByTasks();

            Console.ReadLine();
        }

        #region Private Properties

        private static ILog Logger { get; set; }

        [Option("-c|--config", Description = "Config file about all nodes settings")]
        private static string ConfigFile { get; set; }

        #endregion
    }
}