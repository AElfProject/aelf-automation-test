using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using AElf.Automation.Common.OptionManagers;
using AElf.Automation.Common.Helpers;
using AElf.Automation.Common.WebApi.Dto;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Volo.Abp.Threading;

namespace AElf.Automation.ContractsTesting
{
    class Program
    {
        #region Private Properties

        private static readonly ILog Logger = Log.GetLogHelper();

        #endregion

        #region Parameter Option

        [Option("-ba|--bp.accoount", Description = "Bp account info")]
        public string BpAccount { get; set; } = "ELF_3SMq6XUt2ogboq3fTXwKF6bs3zt9f3EBqsMfDpVzvaX4U4K";

        [Option("-bp|--bp.password", Description = "Bp account password info")]
        public string BpPassword { get; set; } = "123";

        [Option("-e|--endpoint", Description = "Node service endpoint info")]
        public string Endpoint { get; set; } = "http://192.168.197.13:8100";

        #endregion

        public static int Main(string[] args)
        {
            try
            {
                return CommandLineApplication.Execute<Program>(args);
            }
            catch (AssertFailedException ex)
            {
                Logger.Error($"Execute failed: {ex.Message}");
            }

            return 0;
        }

        private void OnExecute()
        {
            #region Basic Preparation

            //Init Logger
            var logName = "ContractTest_" + DateTime.Now.ToString("MMddHHmmss") + ".log";
            var dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", logName);
            Logger.InitLogHelper(dir);

            var ch = new WebApiHelper(Endpoint, CommonHelper.GetCurrentDataDir());

            //deploy contract
            var contractExecution = new ContractExecution(Endpoint);
            contractExecution.DeployTestContract();
            AsyncHelper.RunSync(contractExecution.ExecuteBasicContractMethods);
            AsyncHelper.RunSync(contractExecution.UpdateContract);
            AsyncHelper.RunSync(contractExecution.ExecuteUpdateContractMethods);

            //configuration set
            var configTransaction = new ConfigurationTransaction("http://192.168.197.13:8100");
            configTransaction.GetTransactionLimit();
            configTransaction.SetTransactionLimit(50);
            configTransaction.GetTransactionLimit();
            Console.ReadLine();

            #endregion

            #region Node status check

            NodesState.GetAllBlockTimes("bp1", "http://192.168.197.13:8100");
            Console.ReadLine();
            NodesState.GetAllBlockTimes("bp2", "http://192.168.197.29:8100");
            Console.ReadLine();
            NodesState.GetAllBlockTimes("bp3", "http://192.168.197.33:8100");
            Console.ReadLine();

            var tasks = new List<Task>
            {
                Task.Run(() => NodesState.NodeStateCheck("bp1", "http://192.168.197.13:8100")),
                Task.Run(() => NodesState.NodeStateCheck("bp2", "http://192.168.197.28:8100")),
                Task.Run(() => NodesState.NodeStateCheck("bp3", "http://192.168.197.33:8100")),
                //Task.Run(() => NodesState.NodeStateCheck("bp1", "http://119.254.209.177:8000")),
                //Task.Run(() => NodesState.NodeStateCheck("bp2", "http://54.154.97.61:8000")),
                //Task.Run(() => NodesState.NodeStateCheck("bp3", "http://34.220.37.238:8000")),
                //Task.Run(() => NodesState.NodeStateCheck("full1", "http://103.61.37.19:8000")),
                //Task.Run(() => NodesState.NodeStateCheck("full2", "http://54.169.140.30:8000"))
            };
            Task.WaitAll(tasks.ToArray());

            #endregion

            #region Block verify testing

            var heightCi = new CommandInfo(ApiMethods.GetBlockHeight);
            ch.GetBlockHeight(heightCi);
            var height = (long) heightCi.InfoMsg;
            for (var i = 1; i <= height; i++)
            {
                var blockCi = new CommandInfo(ApiMethods.GetBlockByHeight)
                {
                    Parameter = $"{i} false"
                };
                ch.GetBlockByHeight(blockCi);
                var blockInfo = blockCi.InfoMsg as BlockDto;
                Logger.Info("Height={0}, Block Hash={1}, TxCount={2}",
                    i,
                    blockInfo?.BlockHash,
                    blockInfo?.Body.TransactionsCount);
            }

            #endregion
        }
    }
}