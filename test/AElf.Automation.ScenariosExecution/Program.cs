using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using AElf.Automation.Common.Helpers;
using AElf.Automation.ScenariosExecution.Scenarios;

namespace AElf.Automation.ScenariosExecution
{
    class Program
    {
        #region Private Properties

        private static readonly ILogHelper Logger = LogHelper.GetLogHelper();

        private static ConfigInfo ConfigInfo { get; set; }
        private static List<string> Users { get; set; }

        private static TokenExecutor Executor { get; set; }

        #endregion

        static void Main(string[] args)
        {
            #region Basic Preparation
            //Init Logger
            var logName = "ScenarioTest_" + DateTime.Now.ToString("MMddHHmmss") + ".log";
            var dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", logName);
            Logger.InitLogHelper(dir);
            
            #endregion

            var token = new TokenScenario();
            token.PrepareAccountBalance();
            
            var contract = new ContractScenario();
            
            var resource = new ResourceScenario();
            
            var node = new NodeScenario();
            
            var user = new UserScenario();

            var tasks = new List<Task>()
            {
                //scenario task
                Task.Run(() => token.RunTokenScenario()),
                Task.Run(() => resource.RunResourceScenario()),
                Task.Run(() => contract.RunContractScenario()),
                Task.Run(() => node.RunNodeScenario()),
                Task.Run(() => user.RunUserScenario()),
                
                //node task
                Task.Run(()=>node.CheckNodeStatusAction()),
                Task.Run(()=>node.CheckNodeTransactionAction())
            };
            Task.WaitAll(tasks.ToArray());
        
            Console.ReadLine();
        }
    }
}