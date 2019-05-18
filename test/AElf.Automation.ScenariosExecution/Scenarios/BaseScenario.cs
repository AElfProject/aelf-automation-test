using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.Automation.Common.Helpers;
using AElf.Automation.Common.WebApi;

namespace AElf.Automation.ScenariosExecution.Scenarios
{
    public class BaseScenario
    {
        protected readonly ILogHelper Logger = LogHelper.GetLogHelper();
        
        public List<string> TestUsers { get; set; }
        
        public List<Node> BpNodes { get; set; }
        public List<Node> FullNodes { get; set; }
        public ContractServices Services { get; set; }
        
        public void ExecuteContinuousTasks(IEnumerable<Action> actions, bool interrupted = true)
        {
            for (var i = 1; i >0; i++)
            {
                try
                {
                    ExecuteTasks(actions);
                }
                catch (Exception e)
                {
                    Logger.WriteError($"ExecuteContinuousTasks got exception: {e.Message}");
                    if(interrupted)
                        break;
                }
            }
        }
        
        public void ExecuteTasks(IEnumerable<Action> actions, int sleepSeconds = 0)
        {
            Task.WaitAll(actions.Select(action => Task.Run(() => action.Invoke())).ToArray());
            if(sleepSeconds != 0)
                Thread.Sleep(1000*sleepSeconds);
        }

        public void GetChainTransactionAction()
        {
            var chain = new ChainSummary(BpNodes.Last().ServiceUrl);
            chain.ContinuousCheckChainStatus();
        }

        protected void InitializeScenario()
        {
            var envCheck = new EnvCheck();
            envCheck.CheckInitialEnvironment();
            TestUsers = envCheck.GenerateOrGetTestUsers();
            Services = envCheck.GetContractServices();

            var configInfo = ConfigInfoHelper.Config;
            BpNodes = configInfo.BpNodes;
            FullNodes = configInfo.FullNodes;
        }
    }
}