using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.Types;
using AElfChain.Common;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using AElfChain.SDK.Models;
using log4net;

namespace AElf.Automation.ScenariosExecution.ContractActions
{
    public partial class TransactionJob
    {
        private static readonly ILog Logger = Log4NetHelper.GetLogger();
        public ConcurrentQueue<string> TransactionQueue { get; set; }
        public ConcurrentQueue<string> TransactionIdQueue { get; set; }
        public ConcurrentQueue<INodeManager> NodeManagerQueue { get; set; }
        
        public ContractServices Services { get; set; }
        public TesterJob TesterJob { get; set; }
        
        public List<string> Endpoints { get; set; }

        public static readonly int MaxManagerCount = 100;
        public static readonly int TransactionLimit = 100;

        public TransactionJob()
        {
            TransactionQueue = new ConcurrentQueue<string>();
            TransactionIdQueue = new ConcurrentQueue<string>();
            NodeManagerQueue = new ConcurrentQueue<INodeManager>();
            Endpoints = new List<string>();
        }

        public void InitializeJob()
        {
            //preparation
            var nodes = NodeInfoHelper.Config.Nodes;
            Endpoints = nodes.Select(o => o.Endpoint).ToList();
            Endpoints.ForEach(CreateNodeManager);

            //init tester
            var nodeManager = GetNodeManager();
            TesterJob = new TesterJob(nodeManager);
            TesterJob.GetTestAccounts(200);
            TesterJob.CheckTesterToken();
            
            //init contracts
            var bp = nodes.Select(o => o.Account).First();
            Services = new ContractServices(nodeManager, bp);
            NodeManagerQueue.Enqueue(nodeManager);
        }

        public void RunJob()
        {
            //execution task
            var tasks = new List<Task>
            {
                Task.Run(CheckTesterToken), 
                Task.Run(()=>AddTokenActions(1)),
                Task.Run(ExecuteTransaction), 
                Task.Run(CheckTransaction)
            };

            Task.WaitAll(tasks.ToArray());
        }

        public void CreateNodeManager(string url)
        {
            while (true)
            {
                if (NodeManagerQueue.Count <= MaxManagerCount)
                {
                    var nodeManager = new NodeManager(url);
                    NodeManagerQueue.Enqueue(nodeManager);
                    return;
                }

                Thread.Sleep(1000);
            }
        }

        public INodeManager GetNodeManager()
        {
            while (true)
            {
                if (!NodeManagerQueue.TryDequeue(out var nodeManager))
                {
                    
                    "No available node manager to use...".WriteWarningLine();
                    Thread.Sleep(1000);
                    continue;
                }

                return nodeManager;
            }
        }

        public void AddTransaction(string rawTransaction)
        {
            while (true)
            {
                if (TransactionQueue.Count > TransactionLimit)
                {
                    Thread.Sleep(500);
                    continue;
                }
                
                TransactionQueue.Enqueue(rawTransaction);
                break;
            }
        }

        protected void ExecuteTransaction()
        {
            while (true)
            {
                if (!TransactionQueue.TryDequeue(out var transaction))
                {
                    "No available transaction action need to execute, wait....".WriteWarningLine();
                    Thread.Sleep(1000);
                    continue;
                }

                var nodeManager = GetNodeManager();
                var txId = nodeManager.SendTransaction(transaction);

                TransactionIdQueue.Enqueue(txId);
                Logger.Info($"TransactionId: {txId}");
                NodeManagerQueue.Enqueue(nodeManager);
            }
        }

        protected void CheckTransaction()
        {
            while (true)
            {
                if (!TransactionIdQueue.TryDequeue(out var txId))
                {
                    "No transaction need to check result, wait...".WriteWarningLine();
                    Thread.Sleep(4000);
                    continue;
                }

                var nodeManager = GetNodeManager();
                var transactionResult = nodeManager.CheckTransactionResult(txId, 10);
                if (transactionResult.Status.ConvertTransactionResultStatus() == TransactionResultStatus.Pending)
                    TransactionIdQueue.Enqueue(txId);

                NodeManagerQueue.Enqueue(nodeManager);
            }
        }

        protected void CheckTesterToken()
        {
            while (true)
            {
                TesterJob.CheckTesterToken();
                Thread.Sleep(5 * 60 * 1000);
            }
        }
    }
}