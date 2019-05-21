using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.Automation.Common.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AElf.Automation.ScenariosExecution.Scenarios
{
    public class BaseScenario
    {
        protected static readonly ILogHelper Logger = LogHelper.GetLogHelper();
        
        public List<string> AllTesters { get; set; }
        
        public List<Node> BpNodes { get; set; }
        public List<Node> FullNodes { get; set; }
        public ContractServices Services { get; set; }
        
        public void ExecuteContinuousTasks(IEnumerable<Action> actions, bool interrupted = true, int sleepSeconds = 0)
        {
            while (true)
            {
                try
                {
                    ExecuteStandaloneTask(actions, sleepSeconds);
                }
                catch (Exception e)
                {
                    Logger.WriteError($"ExecuteContinuousTasks got exception: {e.Message}");
                    if(interrupted)
                        break;
                }
            }
        }
        
        public void ExecuteStandaloneTask(IEnumerable<Action> actions, int sleepSeconds = 0)
        {
            try
            {
                Task.WaitAll(actions.Select(action => Task.Run(() => action.Invoke())).ToArray());
                if(sleepSeconds != 0)
                    Thread.Sleep(1000*sleepSeconds);
            }
            catch (Exception e)
            {
                Logger.WriteError($"ExecuteStandaloneTask got exception: {e.Message}");
                throw;
            }
        }

        public void CheckNodeTransactionAction()
        {
            var chain = new ChainSummary(BpNodes.Last().ServiceUrl);
            chain.ContinuousCheckChainStatus();
        }

        public void CheckNodeStatusAction()
        {
            long height = 1;
            var checkTimes = 0;
            while (true)
            {
                if(checkTimes == 120)
                    break;
                
                Thread.Sleep(1000);
                var newHeight = Services.ApiHelper.ApiService.GetBlockHeight().Result;
                if (newHeight == height)
                {
                    checkTimes++;
                    if(checkTimes % 10 == 0)
                        Logger.WriteWarn($"Node height not changed {checkTimes} seconds.");
                }
                else
                {
                    height = newHeight;
                    checkTimes = 0;
                }
            }
            Assert.IsTrue(false,$"Node height not changed 2 minutes later.");
        }

        protected void InitializeScenario()
        {
            var envCheck = new EnvCheck();
            envCheck.CheckInitialEnvironment();
            AllTesters = envCheck.GenerateOrGetTestUsers();
            Services = envCheck.GetContractServices();

            var configInfo = ConfigInfoHelper.Config;
            BpNodes = configInfo.BpNodes;
            FullNodes = configInfo.FullNodes;
        }

        protected static int GenerateRandomNumber(int min, int max)
        {
            var random = new Random(DateTime.UtcNow.Millisecond);
            return random.Next(min, max);
        }
    }
}