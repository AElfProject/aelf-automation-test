using System;
using AElfChain.Common.Helpers;
using log4net;

namespace AElf.Automation.ScenariosExecution
{
    internal class Program
    {
        #region Private Properties

        private static readonly ILog Logger = Log4NetHelper.GetLogger();

        #endregion

        private static void Main(string[] args)
        {
            Log4NetHelper.LogInit($"ScenarioTest_Hour_{DateTime.Now.Hour:00}");

            var multipleTasks = new MultipleTasks();
            multipleTasks.RunScenariosByTasks();

            Console.ReadLine();
        }
    }
}