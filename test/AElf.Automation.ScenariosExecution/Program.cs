using System;
using System.IO;
using System.Linq;
using AElf.Automation.Common.Helpers;
using AElf.Automation.ScenariosExecution.Scenarios;
using FluentScheduler;
using log4net;

namespace AElf.Automation.ScenariosExecution
{
    class Program
    {
        #region Private Properties

        private static readonly ILog Logger = Log4NetHelper.GetLogger();

        #endregion

        static void Main(string[] args)
        {
            Log4NetHelper.LogInit("ScenarioTest");

            var scenarios = ConfigInfoHelper.Config.TestCases.FindAll(o => o.Enable).ToList();

            var tokenScenario = new TokenScenario();
            tokenScenario.PrepareAccountBalance();
            var nodeScenario = new NodeScenario();

            JobManager.UseUtcTime();
            var registry = new Registry();
            
            //scenario tasks
            foreach (var scenario in scenarios)
            {
                switch (scenario.CaseName)
                {
                    case "TokenScenario":
                        RegisterAction(registry, scenario, tokenScenario.TokenScenarioJob);
                        break;
                    case "ResourceScenario":
                        var resourceScenario = new ResourceScenario();
                        RegisterAction(registry, scenario, resourceScenario.RunResourceScenarioJob);                        
                        break;
                    case "UserScenario":
                        var userScenario = new UserScenario();
                        RegisterAction(registry, scenario, userScenario.RunUserScenarioJob);
                        break;
                    case "NodeScenario":
                        RegisterAction(registry, scenario, nodeScenario.RunNodeScenarioJob);
                        break;
                    case "ContractScenario":
                        var contractScenario = new ContractScenario();
                        RegisterAction(registry, scenario, contractScenario.RunContractScenarioJob);
                        break;
                    case "ExceptionScenario":
                        var exceptionScenario = new ExceptionScenario();
                        RegisterAction(registry, scenario, exceptionScenario.RunExceptionScenarioJob);
                        break;
                }
            }
           
            JobManager.Initialize(registry);
            JobManager.JobException += info =>
                Logger.Error($"Error job: {info.Name}, Error message: {info.Exception.Message}");

            //node status monitor
            nodeScenario.CheckNodeTransactionAction();
            nodeScenario.CheckNodeStatusAction();

            Console.ReadLine();
        }

        private static void RegisterAction(Registry registry, TestCase scenario, Action action)
        {
            Logger.Info($"Register {scenario.CaseName} with time interval: {scenario.TimeInterval} seconds.");
            registry.Schedule(() => action.Invoke()).WithName(scenario.CaseName)
                .ToRunEvery(scenario.TimeInterval).Seconds();
        }
    }
}