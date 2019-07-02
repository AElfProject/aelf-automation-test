using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AElf.Automation.Common.Helpers;
using AElf.Automation.Common.WebApi;
using Volo.Abp.Threading;

namespace AElf.Automation.QueryTransaction
{
    public class TransactionQuery
    {
        private static readonly ILogHelper Logger = LogHelper.GetLogHelper();
        private readonly ConcurrentQueue<string> _transactionQueue = new ConcurrentQueue<string>();
        private readonly WebApiService _apiService;
        private long _blockHeight = 1;
        private bool _completeQuery;

        public TransactionQuery(string url)
        {
            _apiService = new WebApiService(url);
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

        private async Task QueryBlockWithHeight()
        {
            while (true)
            {
                var height = await _apiService.GetBlockHeight();
                Logger.WriteInfo($"Current height:{height}");
                if (_blockHeight == height)
                {
                    _completeQuery = true;
                    return;
                }

                for (var i = _blockHeight; i <= height; i++)
                {
                    var block = await _apiService.GetBlockByHeight(i, true);
                    Logger.WriteInfo(
                        $"Block height: {i}, Block hash: {block.BlockHash}, TxCount: {block.Body.TransactionsCount}");
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
                    Thread.Sleep(50);
                    continue;
                }

                var transaction = await _apiService.GetTransactionResult(txId);
                Logger.WriteInfo($"Transaction: {txId},{transaction.Status}");
            }
        }
    }
}