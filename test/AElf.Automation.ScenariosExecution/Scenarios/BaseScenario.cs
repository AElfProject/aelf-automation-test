using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.Automation.Common.Contracts;
using AElf.Automation.Common.Helpers;
using AElf.Automation.Common.WebApi.Dto;
using AElf.Contracts.Consensus.AEDPoS;
using Google.Protobuf.WellKnownTypes;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AElf.Automation.ScenariosExecution.Scenarios
{
    public class BaseScenario
    {
        protected static readonly ILogHelper Logger = LogHelper.GetLogHelper();

        protected List<string> AllTesters { get; set; }

        protected List<Node> BpNodes { get; set; }
        protected List<Node> FullNodes { get; set; }
        
        protected List<Node> CurrentBpNodes { get; set; }
        protected ContractServices Services { get; set; }

        protected void ExecuteContinuousTasks(IEnumerable<Action> actions, bool interrupted = true, int sleepSeconds = 0)
        {
            while (true)
            {
                try
                {
                    if (actions == null)
                        throw new ArgumentException("Action methods is null.");
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

        protected void ExecuteStandaloneTask(IEnumerable<Action> actions, int sleepSeconds = 0)
        {
            try
            {
                foreach (var action in actions)
                {
                    action.Invoke();
                }

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
                if(checkTimes == 120)
                    break;
                
                var newHeight = Services.ApiHelper.ApiService.GetBlockHeight().Result;
                if (newHeight == height)
                {
                    checkTimes++;
                    if(checkTimes % 10 == 0)
                        Logger.WriteWarn($"Node height not changed {checkTimes/2} seconds.");
                    Thread.Sleep(500);
                }
                else
                {
                    height = newHeight;
                    checkTimes = 0;
                }
            }
            Assert.IsTrue(false,$"Node height not changed 1 minutes later.");
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
            GetCurrentBpNodes();
            Services = envCheck.GetContractServices(CurrentBpNodes.First().ServiceUrl);
            Logger.WriteInfo($"All request would be sent from: {CurrentBpNodes.First().Name}");
        }

        protected static int GenerateRandomNumber(int min, int max)
        {
            var random = new Random(DateTime.UtcNow.Millisecond);
            return random.Next(min, max);
        }
        
        protected void GetCurrentBpNodes()
        {
            CurrentBpNodes = new List<Node>();
            var consensus = Services.ConsensusService;
            var miners = consensus.CallViewMethod<MinerList>(ConsensusMethod.GetCurrentMinerList, new Empty());
            var minersPublicKeys = miners.PublicKeys.Select(o => o.ToByteArray().ToHex()).ToList();
            foreach (var bp in BpNodes)
            {
                if(minersPublicKeys.Contains(bp.PublicKey))
                    CurrentBpNodes.Add(bp);
            }

            foreach (var full in FullNodes)
            {
                if(minersPublicKeys.Contains(full.PublicKey))
                    CurrentBpNodes.Add(full);
            }
            Logger.WriteInfo($"Current miners are: [{string.Join(",", CurrentBpNodes.Select(o=>o.Name))}]");
        }
    }
}