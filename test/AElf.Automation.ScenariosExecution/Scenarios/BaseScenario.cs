using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AElf.Types;
using AElfChain.Common;
using AElfChain.Common.Helpers;
using AElfChain.SDK.Models;
using log4net;
using Volo.Abp.Threading;

namespace AElf.Automation.ScenariosExecution.Scenarios
{
    public class BaseScenario
    {
        protected static readonly ILog Logger = Log4NetHelper.GetLogger();
        protected List<string> AllTesters { get; set; }
        protected List<Node> AllNodes { get; set; }
        protected static string NativeToken { get; set; }
        protected static ContractServices Services { get; set; }

        protected void ExecuteContinuousTasks(IEnumerable<Action> actions, bool interrupted = true,
            int sleepSeconds = 0)
        {
            while (true)
                try
                {
                    if (actions == null)
                        throw new ArgumentException("Action methods is null.");
                    ExecuteStandaloneTask(actions, sleepSeconds);
                }
                catch (Exception e)
                {
                    Logger.Error($"ExecuteContinuousTasks got exception: {e.Message}", e);
                    if (interrupted)
                        break;
                }
        }

        protected void ExecuteStandaloneTask(IEnumerable<Action> actions, int sleepSeconds = 0,
            bool interrupted = false)
        {
            foreach (var action in actions)
            {
                try
                {
                    action.Invoke();
                }
                catch (Exception e)
                {
                    Logger.Error($"Execute action {action.Method.Name} got exception: {e.Message}");
                    if (interrupted)
                        break;
                }
            }

            if (sleepSeconds != 0)
                Thread.Sleep(1000 * sleepSeconds);
        }

        public void UpdateEndpointAction()
        {
            var randomNumber = CommonHelper.GenerateRandomNumber(1, 10);

            if (randomNumber == 5)
            {
                Console.WriteLine();
                Services.UpdateRandomEndpoint();
            }
        }

        public void CheckNodeTransactionAction()
        {
            var chain = new ChainSummary(Services.NodeManager.GetApiUrl());
            chain.ContinuousCheckChainStatus();
        }

        public void CheckNodeStatusAction()
        {
            while (true)
            {
                CheckNodeHeightStatus();
                Thread.Sleep(30 * 1000);
            }
        }

        private void CheckNodeHeightStatus()
        {
            long height = 1;
            var checkTimes = 0;
            while (true)
            {
                if (checkTimes == 120)
                    break;

                var newHeight = AsyncHelper.RunSync(Services.NodeManager.ApiService.GetBlockHeightAsync);
                if (newHeight == height)
                {
                    checkTimes++;
                    if (checkTimes % 10 == 0)
                        Logger.Warn($"Node height not changed {checkTimes / 2} seconds.");
                    Thread.Sleep(500);
                }
                else
                {
                    height = newHeight;
                    checkTimes = 0;
                }
            }

            throw new Exception("Node height not changed 1 minutes later.");
        }

        protected void InitializeScenario()
        {
            var testerCount = ConfigInfoHelper.Config.UserCount;
            var envCheck = EnvCheck.GetDefaultEnvCheck();
            AllTesters = envCheck.GenerateOrGetTestUsers(testerCount);
            if (Services == null)
            {
                var oldEnv = OldEnvCheck.GetDefaultEnvCheck();
                Services = oldEnv.GetContractServices();
            }

            var configInfo = NodeInfoHelper.Config;
            AllNodes = configInfo.Nodes;
            NativeToken = NodeOption.NativeTokenSymbol;
        }

        protected static int GenerateRandomNumber(int min, int max)
        {
            var random = new Random(Guid.NewGuid().GetHashCode());
            return random.Next(min, max + 1);
        }
    }
}