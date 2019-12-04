using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElfChain.Common.Helpers;
using AElf.Automation.ScenariosExecution.Scenarios;
using FluentScheduler;
using log4net;

namespace AElf.Automation.ScenariosExecution
{
    public class MultipleTasks
    {
        private static readonly ILog Logger = Log4NetHelper.GetLogger();

        public MultipleTasks()
        {
            TaskCollection = new List<Task>();
        }

        private List<Task> TaskCollection { get; }

        public void RunScenariosByTasks()
        {
            var scenarios = ConfigInfoHelper.Config.TestCases.FindAll(o => o.Enable).ToList();

            var tokenScenario = new TokenScenario();
            tokenScenario.PrepareAccountBalance();
            var nodeScenario = new NodeScenario();

            //scenario tasks
            Logger.Info("Start initialize all tasks job.");
            foreach (var scenario in scenarios)
                switch (scenario.CaseName)
                {
                    case "TokenScenario":
                        TaskCollection.Add(RunContinueJobWithInterval(tokenScenario.TokenScenarioJob,
                            scenario.TimeInterval));
                        break;
                    case "ResourceScenario":
                        var resourceScenario = new ResourceScenario();
                        TaskCollection.Add(RunContinueJobWithInterval(resourceScenario.RunResourceScenarioJob,
                            scenario.TimeInterval));
                        break;
                    case "UserScenario":
                        var userScenario = new UserScenario();
                        TaskCollection.Add(RunContinueJobWithInterval(userScenario.RunUserScenarioJob,
                            scenario.TimeInterval));
                        break;
                    case "NodeScenario":
                        TaskCollection.Add(RunContinueJobWithInterval(nodeScenario.RunNodeScenarioJob,
                            scenario.TimeInterval));
                        break;
                    case "ContractScenario":
                        var contractScenario = new ContractScenario();
                        TaskCollection.Add(RunContinueJobWithInterval(contractScenario.RunContractScenarioJob,
                            scenario.TimeInterval));
                        break;
                    case "ExceptionScenario":
                        var exceptionScenario = new ExceptionScenario();
                        TaskCollection.Add(RunContinueJobWithInterval(exceptionScenario.RunExceptionScenarioJob,
                            scenario.TimeInterval));
                        break;
                    case "PerformanceScenario":
                        var performanceScenario = new PerformanceScenario();
                        TaskCollection.Add(RunContinueJobWithInterval(performanceScenario.RunPerformanceScenarioJob,
                            scenario.TimeInterval));
                        break;
                    case "DeleteValueScenario":
                        var deleteValueScenario = new DeleteValueScenario();
                        TaskCollection.Add(RunContinueJobWithInterval(deleteValueScenario.RunDeleteValueScenarioJob,
                            scenario.TimeInterval));
                        break;
                }

            //node status monitor
            TaskCollection.Add(Task.Run(nodeScenario.CheckNodeTransactionAction));
            TaskCollection.Add(Task.Run(nodeScenario.CheckNodeStatusAction));

            Task.WaitAll(TaskCollection.ToArray());
        }

        public void RunScenariosByScheduler()
        {
            var scenarios = ConfigInfoHelper.Config.TestCases.FindAll(o => o.Enable).ToList();

            var tokenScenario = new TokenScenario();
            tokenScenario.PrepareAccountBalance();
            var nodeScenario = new NodeScenario();

            JobManager.UseUtcTime();
            var registry = new Registry();

            //scenario tasks
            foreach (var scenario in scenarios)
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
                    case "PerformanceScenario":
                        var performanceScenario = new PerformanceScenario();
                        RegisterAction(registry, scenario, performanceScenario.RunPerformanceScenarioJob);
                        break;
                    case "DeleteValueScenario":
                        var deleteValueScenario = new DeleteValueScenario();
                        RegisterAction(registry, scenario, deleteValueScenario.RunDeleteValueScenarioJob);
                        break;
                }

            JobManager.Initialize(registry);
            JobManager.JobException += info =>
                Logger.Error($"Error job: {info.Name}, Error message: {info.Exception.Message}");

            //node status monitor
            nodeScenario.CheckNodeTransactionAction();
            nodeScenario.CheckNodeStatusAction();
        }

        private static Task RunContinueJobWithInterval(Action action, int seconds)
        {
            void NewAction()
            {
                while (true)
                    try
                    {
                        action.Invoke();
                        Task.Delay(1000 * seconds);
                    }
                    catch (Exception e)
                    {
                        Logger.Error(e);
                        break;
                    }
            }

            return Task.Run(NewAction);
        }

        private static void RegisterAction(Registry registry, TestCase scenario, Action action)
        {
            Logger.Info($"Register {scenario.CaseName} with time interval: {scenario.TimeInterval} seconds.");
            registry.Schedule(action.Invoke).WithName(scenario.CaseName)
                .ToRunEvery(scenario.TimeInterval).Seconds();
        }
    }
}