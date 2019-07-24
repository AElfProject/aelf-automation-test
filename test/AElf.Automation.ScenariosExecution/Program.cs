using System;
using System.IO;
using System.Linq;
using AElf.Automation.Common.Helpers;
using AElf.Automation.ScenariosExecution.Scenarios;
using FluentScheduler;

namespace AElf.Automation.ScenariosExecution
{
    class Program
    {
        #region Private Properties

        private static readonly ILogHelper Logger = LogHelper.GetLogHelper();

        #endregion

        static void Main(string[] args)
        {
            #region Basic Preparation

            //Init Logger
            var logName = "ScenarioTest_" + DateTime.Now.ToString("MMddHHmmss") + ".log";
            var dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", logName);
            Logger.InitLogHelper(dir);

            #endregion

            var enableCases = ConfigInfoHelper.Config.TestCases.FindAll(o => o.Enable).Select(o => o.CaseName).ToList();

            var token = new TokenScenario();
            token.PrepareAccountBalance();
            var node = new NodeScenario();

            JobManager.UseUtcTime();
            var registry = new Registry();
            //scenario tasks
            if (enableCases.Contains("TokenScenario"))
                registry.Schedule(() => token.TokenScenarioJob()).WithName("TokenScenario")
                    .ToRunEvery(5).Seconds();

            if (enableCases.Contains("ResourceScenario"))
            {
                var resource = new ResourceScenario();
                registry.Schedule(() => resource.ResourceScenarioJob()).WithName("ResourceScenario")
                    .ToRunEvery(3).Seconds();
            }

            if (enableCases.Contains("UserScenario"))
            {
                var user = new UserScenario();
                registry.Schedule(() => user.UserScenarioJob()).WithName("UserScenario")
                    .ToRunEvery(10).Seconds();
            }

            if (enableCases.Contains("NodeScenario"))
                registry.Schedule(() => node.NodeScenarioJob()).WithName("NodeScenario")
                    .ToRunEvery(1).Minutes();

            if (enableCases.Contains("ContractScenario"))
            {
                var contract = new ContractScenario();
                registry.Schedule(() => contract.RunContractScenarioJob()).WithName("ContractScenario")
                    .ToRunEvery(3).Minutes();
            }

            JobManager.Initialize(registry);
            JobManager.JobException += info =>
                Logger.Error($"Error job: {info.Name}, Error message: {info.Exception.Message}");

            //node status monitor
            node.CheckNodeTransactionAction();
            node.CheckNodeStatusAction();

            Console.ReadLine();
        }
    }
}