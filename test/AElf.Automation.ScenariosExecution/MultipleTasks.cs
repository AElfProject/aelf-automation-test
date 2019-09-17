using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Automation.Common.Helpers;
using AElf.Automation.ScenariosExecution.Scenarios;
using FluentScheduler;
using log4net;

namespace AElf.Automation.ScenariosExecution
{
    public class MultipleTasks
    {
        private List<Task> _taskCollection { get; set; }

        private static readonly ILog Logger = Log4NetHelper.GetLogger();
        
        public MultipleTasks()
        {
            _taskCollection = new List<Task>();
        }

        public void RunScenariosByTasks()
        {
            var scenarios = ConfigInfoHelper.Config.TestCases.FindAll(o => o.Enable).ToList();
            
            var tokenScenario = new TokenScenario();
            tokenScenario.PrepareAccountBalance();
            var nodeScenario = new NodeScenario();
            
            //scenario tasks
            Logger.Info("Start initialize all tasks job.");
            foreach (var scenario in scenarios)
            {
                switch (scenario.CaseName)
                {
                    case "TokenScenario":
                        _taskCollection.Add(RunContinueJobWithInterval(tokenScenario.TokenScenarioJob, scenario.TimeInterval));
                        break;
                    case "ResourceScenario":
                        var resourceScenario = new ResourceScenario();
                        _taskCollection.Add(RunContinueJobWithInterval(resourceScenario.RunResourceScenarioJob, scenario.TimeInterval));
                        break;
                    case "UserScenario":
                        var userScenario = new UserScenario();
                        _taskCollection.Add(RunContinueJobWithInterval(userScenario.RunUserScenarioJob, scenario.TimeInterval));
                        break;
                    case "NodeScenario":
                        _taskCollection.Add(RunContinueJobWithInterval(nodeScenario.RunNodeScenarioJob, scenario.TimeInterval));
                        break;
                    case "ContractScenario":
                        var contractScenario = new ContractScenario();
                        _taskCollection.Add(RunContinueJobWithInterval(contractScenario.RunContractScenarioJob, scenario.TimeInterval));
                        break;
                    case "ExceptionScenario":
                        var exceptionScenario = new ExceptionScenario();
                        _taskCollection.Add(RunContinueJobWithInterval(exceptionScenario.RunExceptionScenarioJob, scenario.TimeInterval));
                        break;
                    case "PerformanceScenario":
                        var performanceScenario = new PerformanceScenario();
                        _taskCollection.Add(RunContinueJobWithInterval(performanceScenario.RunPerformanceScenarioJob, scenario.TimeInterval));
                        break;
                }
            }
            
            //node status monitor
            _taskCollection.Add(Task.Run((Action) nodeScenario.CheckNodeTransactionAction));
            _taskCollection.Add(Task.Run((Action) nodeScenario.CheckNodeStatusAction));

            Task.WaitAll(_taskCollection.ToArray());
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
                    case "PerformanceScenario":
                        var performanceScenario = new PerformanceScenario();
                        RegisterAction(registry, scenario, performanceScenario.RunPerformanceScenarioJob);
                        break;
                }
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
                {
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
            }

            return Task.Run((Action) NewAction);
        }
        
        private static void RegisterAction(Registry registry, TestCase scenario, Action action)
        {
            Logger.Info($"Register {scenario.CaseName} with time interval: {scenario.TimeInterval} seconds.");
            registry.Schedule((Action) action.Invoke).WithName(scenario.CaseName)
                .ToRunEvery(scenario.TimeInterval).Seconds();
        }
    }
}