using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.Client.Service;
using AElfChain.Common;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using Volo.Abp.Threading;

namespace AElf.Automation.ScenariosExecution.Scenarios
{
    public class MonitorMassRequestScenario : BaseScenario
    {
        public MonitorMassRequestScenario()
        {
            InitializeScenario();
            NodeManager = Services.NodeManager;
            ChainHeight = AsyncHelper.RunSync(ApiClient.GetBlockHeightAsync);
        }

        public INodeManager NodeManager { get; set; }
        public AElfClient ApiClient => NodeManager.ApiClient;

        public long ChainHeight { get; set; }

        public void RunMassRequestScenarioJob(int threadNumber)
        {
            var stopwatch = Stopwatch.StartNew();
            var pendingSource = new CancellationTokenSource(300 * 1000);
            ChainHeight = AsyncHelper.RunSync(ApiClient.GetBlockHeightAsync);
            var tasks = new List<Task>();
            for (var i = 0; i < threadNumber; i++)
            {
                tasks.Add(Task.Run(() =>
                {
                    while (!pendingSource.IsCancellationRequested)
                        ExecuteStandaloneTask(new Action[]
                        {
                            QueryChainStatus,
                            QueryBlockInfo,
                            QueryContractInfo,
                            QueryUserTokenBalance
                        });
                }));
                Thread.Sleep(100);
            }

            Task.WaitAll(tasks.ToArray());

            stopwatch.Stop();
            Logger.Info($"Mass requests continuous time: {stopwatch.ElapsedMilliseconds}ms");

            UpdateEndpointAction();
        }

        private void QueryBlockInfo()
        {
            var randomHeight = CommonHelper.GenerateRandomNumber(1, (int) ChainHeight);
            var blockInfo = AsyncHelper.RunSync(() => ApiClient.GetBlockByHeightAsync(randomHeight, true));
            var blockHash = blockInfo.BlockHash;
            var transactionIds = blockInfo.Body.Transactions;
            $"QueryBlockInfo: BlockHash={blockHash}, Transactions={blockInfo.Body.TransactionsCount}"
                .WriteSuccessLine();
            //request transactions
            AsyncHelper.RunSync(() => ApiClient.GetTransactionResultsAsync(blockHash));
            //request transaction
            Parallel.ForEach(transactionIds,
                txId =>
                {
                    var transaction = AsyncHelper.RunSync(() => ApiClient.GetTransactionResultAsync(txId));
                    $"QueryBlockInfo: Transaction={transaction.TransactionId}, Status={transaction.Status}"
                        .WriteSuccessLine();
                });
        }

        private void QueryContractInfo()
        {
            var contracts = Services.GenesisService.GetAllSystemContracts().Values;
            Parallel.ForEach(contracts,
                contract =>
                {
                    $"QueryContractInfo: Contract={contract.GetFormatted()}".WriteSuccessLine();
                    AsyncHelper.RunSync(() => ApiClient.GetContractFileDescriptorSetAsync(contract.GetFormatted()));
                });
        }

        private void QueryChainStatus()
        {
            var chainStatus = AsyncHelper.RunSync(ApiClient.GetChainStatusAsync);
            $"QueryChainStatus: ChainHeight={chainStatus.BestChainHeight}, BlockHash={chainStatus.BestChainHash}"
                .WriteSuccessLine();
        }

        private void QueryUserTokenBalance()
        {
            var nodes = NodeInfoHelper.Config.Nodes.Select(o => o.Account);
            Parallel.ForEach(nodes, node =>
            {
                var symbols = new List<string> {"ELF", "CPU", "RAM", "NET", "DISK"};
                var tokenInfo = "QueryUserTokenBalance:";
                foreach (var symbol in symbols)
                {
                    var balance = Services.TokenService.GetUserBalance(node, symbol);
                    tokenInfo += $" {symbol}={balance}";
                }

                tokenInfo.WriteSuccessLine();
            });
        }
    }
}