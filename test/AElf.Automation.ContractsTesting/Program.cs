using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AElfChain.Common;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using log4net;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Volo.Abp.Threading;

namespace AElf.Automation.ContractsTesting
{
    internal class Program
    {
        #region Private Properties

        private static readonly ILog Logger = Log4NetHelper.GetLogger();

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
            Log4NetHelper.LogInit("ContractTest");
            var nm = new NodeManager(Endpoint);
            var api = nm.ApiService;
            
            //analyze size fee
            var feeProvider = new TransactionFeeProvider();
            feeProvider.CalculateTxFee();
            Console.ReadLine();
            
            //generate random number
            var randGen = new RandomGenerate(nm, BpAccount);
            AsyncHelper.RunSync(() => randGen.GenerateAndCheckRandomNumbers(1000));
            Console.ReadLine();

            //code remark test
            var codeRemark = new CodeRemarkTest(nm);
            codeRemark.ExecuteContractMethodTest();
            Console.ReadLine();

            //proto file serialize
            var serialize = new ProtoFileTest(nm);
            Console.ReadLine();

            //check configuration
            var nodeStatus = new NodeStatus(nm);
            nodeStatus.CheckConfigurationInfo();
            Console.ReadLine();

            //deploy contract
            var endpoints = new[]
            {
                "192.168.197.43:8100",
                "192.168.197.15:8100",
                "192.168.197.52:8100",
                "192.168.197.43:8200",
                "192.168.197.15:8200",
                "192.168.197.52:8200"
            };
            var rd = new Random(Guid.NewGuid().GetHashCode());
            var count = 0;
            while (count++ < 10000)
                try
                {
                    var randUrl = endpoints[rd.Next(endpoints.Length)];
                    Logger.Info($"Send request to url: {randUrl}");
                    var contractExecution = new ContractExecution(randUrl);
                    contractExecution.DeployTestContract();
                    AsyncHelper.RunSync(contractExecution.ExecuteBasicContractMethods);
                    AsyncHelper.RunSync(contractExecution.UpdateContract);
                    AsyncHelper.RunSync(contractExecution.ExecuteUpdateContractMethods);
                    Thread.Sleep(3000);
                }
                catch (Exception e)
                {
                    Logger.Error(e.Message);
                }

            //configuration set
            var configTransaction = new ConfigurationTransaction(Endpoint);
            configTransaction.GetTransactionLimit();
            configTransaction.SetTransactionLimit(50);
            configTransaction.GetTransactionLimit();
            Console.ReadLine();
            
            //check transaction fee
            var transactionFee = new AnalyzeTransactionFee();
            transactionFee.QueryBlocksInfo(277325, 284525); //298840
            transactionFee.QueryTransactionsInfo();
            transactionFee.CalculateTotalFee();
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
                Task.Run(() => NodesState.NodeStateCheck("bp3", "http://192.168.197.33:8100"))
                //Task.Run(() => NodesState.NodeStateCheck("bp1", "http://119.254.209.177:8000")),
                //Task.Run(() => NodesState.NodeStateCheck("bp2", "http://54.154.97.61:8000")),
                //Task.Run(() => NodesState.NodeStateCheck("bp3", "http://34.220.37.238:8000")),
                //Task.Run(() => NodesState.NodeStateCheck("full1", "http://103.61.37.19:8000")),
                //Task.Run(() => NodesState.NodeStateCheck("full2", "http://54.169.140.30:8000"))
            };
            Task.WaitAll(tasks.ToArray());

            #endregion

            #region Block verify testing

            var height = AsyncHelper.RunSync(api.GetBlockHeightAsync);
            for (var i = 1; i <= height; i++)
            {
                var i1 = i;
                var blockInfo = AsyncHelper.RunSync(() => nm.ApiService.GetBlockByHeightAsync(i1));
                Logger.Info("Height={0}, Block Hash={1}, TxCount={2}",
                    i,
                    blockInfo?.BlockHash,
                    blockInfo?.Body.TransactionsCount);
            }

            #endregion
        }

        #region Parameter Option

        [Option("-ba|--bp.accoount", Description = "Bp account info")]
        public string BpAccount { get; set; } = "28Y8JA1i2cN6oHvdv7EraXJr9a1gY6D1PpJXw9QtRMRwKcBQMK";

        [Option("-bp|--bp.password", Description = "Bp account password info")]
        public string BpPassword { get; set; } = NodeOption.DefaultPassword;

        [Option("-e|--endpoint", Description = "Node service endpoint info")]
        public string Endpoint { get; set; } = "http://192.168.199.205:8000";

        #endregion
    }
}