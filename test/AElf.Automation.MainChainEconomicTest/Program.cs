using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using log4net;

namespace AElf.Automation.MainChainEconomicTest
{
    class Program
    {
        static void Main(string[] args)
        {
            Log4NetHelper.LogInit("CheckReward");
            TaskCollection = new List<Task>();
            var caller = "28Y8JA1i2cN6oHvdv7EraXJr9a1gY6D1PpJXw9QtRMRwKcBQMK";
            var endpoint = "192.168.197.21:8000";
            var nm = new NodeManager(endpoint);
            var mining = new MiningRewards(nm,caller);
            TaskCollection.Add(RunContinueJobWithInterval(() => 
            {
                mining.GetCurrentRoundMinedBlockBonus();
                Thread.Sleep(10000);
            },5));   
            TaskCollection.Add(RunContinueJobWithInterval(() =>
            {
                mining.CheckMinerProfit();

                Thread.Sleep(60000);
            },10));
            Task.WaitAll(TaskCollection.ToArray());
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
        
        private static readonly ILog Logger = Log4NetHelper.GetLogger();
        private static List<Task> TaskCollection { get; set; }
    }
}