using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AElfChain.Common.Helpers;
using AElfChain.SDK;
using log4net;
using Volo.Abp.Threading;

namespace AElf.Automation.QueryTransaction
{
    public class TransactionQuery
    {
        private static readonly ILog Logger = Log4NetHelper.GetLogger();
        private readonly ConcurrentQueue<string> _transactionQueue = new ConcurrentQueue<string>();
        private readonly IApiService _apiService;
        private long _blockHeight = 1;
        private bool _completeQuery;

        public TransactionQuery(string url)
        {
            _apiService = AElfChainClient.GetClient(url);
        }

        public void ExecuteMultipleTasks(int threadCount = 1)
        {
            var tasks = new List<Task>()
            {
                Task.Run(() => AsyncHelper.RunSync(QueryBlockWithHeight)),
            };
            for (var i = 0; i < threadCount; i++)
            {
                tasks.Add(Task.Run(() => AsyncHelper.RunSync(QueryTransactionWithTxId)));
            }

            Task.WaitAll(tasks.ToArray());
        }

        public void QueryBlocksTask(long startHeight)
        {
            _blockHeight = startHeight;

            AsyncHelper.RunSync(QueryBlockWithHeight);
        }

        private async Task QueryBlockWithHeight()
        {
            while (true)
            {
                var height = await _apiService.GetBlockHeightAsync();
                Logger.Info($"Current height:{height}");
                if (_blockHeight == height)
                {
                    _completeQuery = true;
                    return;
                }

                for (var i = _blockHeight; i <= height; i++)
                {
                    var block = await _apiService.GetBlockByHeightAsync(i, true);
                    Logger.Info(
                        $"Block height: {i}, TxCount: {block.Body.TransactionsCount}");
                    block.Body.Transactions.ForEach(item => _transactionQueue.Enqueue(item));
                }

                _blockHeight = height;
            }
        }

        private async Task QueryTransactionWithTxId()
        {
            while (!_completeQuery || _transactionQueue.Count != 0)
            {
                if (!_transactionQueue.TryDequeue(out var txId))
                {
                    Thread.Sleep(10);
                    continue;
                }

                var transaction = await _apiService.GetTransactionResultAsync(txId);
                Logger.Info($"Transaction: {txId},{transaction.Status}");
            }
        }
    }
}