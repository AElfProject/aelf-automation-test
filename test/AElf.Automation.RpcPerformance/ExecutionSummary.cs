using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AElf.Client.Dto;
using AElf.Client.Service;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using log4net;
using Volo.Abp.Threading;

namespace AElf.Automation.RpcPerformance
{
    public class ExecutionSummary
    {
        private const int Phase = 8;
        private static readonly ILog Logger = Log4NetHelper.GetLogger();
        private readonly AElfClient _apiService;
        private long _blockHeight;
        private Dictionary<long, BlockDto> _blockMap;

        /// <summary>
        ///     analyze generate blocks summary info
        /// </summary>
        /// <param name="nodeManager"></param>
        /// <param name="fromStart">whether check from height 1</param>
        public ExecutionSummary(INodeManager nodeManager, bool fromStart = false)
        {
            _apiService = nodeManager.ApiClient;
            _blockMap = new Dictionary<long, BlockDto>();
            _blockHeight = fromStart ? 1 : AsyncHelper.RunSync(_apiService.GetBlockHeightAsync);
        }

        public void ContinuousCheckTransactionPerformance(CancellationToken ct)
        {
            var checkTimes = 0;
            while (true)
            {
                if (ct.IsCancellationRequested)
                {
                    Logger.Warn("ContinuousCheckTransactionPerformance task was been cancelled.");
                    break;
                }

                if (checkTimes == 60)
                    break;
                var height = AsyncHelper.RunSync(_apiService.GetBlockHeightAsync);
                if (height == _blockHeight)
                {
                    checkTimes++;
                    Thread.Sleep(4000);
                    continue;
                }

                checkTimes = 0;
                if (height < _blockHeight)
                    continue;
                for (var i = _blockHeight; i < height; i++)
                {
                    var j = i;
                    var block = AsyncHelper.RunSync(() => _apiService.GetBlockByHeightAsync(j));
                    _blockMap.Add(j, block);
                    if (!_blockMap.Keys.Count.Equals(Phase)) continue;
                    SummaryBlockTransactionInPhase(_blockMap.Values.First(), _blockMap.Values.Last());
                }

                _blockHeight = height;
                Thread.Sleep(100);
            }
        }

        private void SummaryBlockTransactionInPhase(BlockDto startBlock, BlockDto endBlockDto)
        {
            var totalTransactions = _blockMap.Values.Sum(o => o.Body.TransactionsCount);
            var averageTx = totalTransactions / Phase;
            var timePerBlock = GetPerBlockTimeSpan(startBlock, endBlockDto);
            var timePerTx = totalTransactions * 1000 / GetTotalBlockSeconds(startBlock, endBlockDto);
            _blockMap = new Dictionary<long, BlockDto>();
            Logger.Info($"Summary Information: {Phase} blocks from height " +
                        $"{startBlock.Header.Height}~{endBlockDto.Header.Height} executed " +
                        $"{totalTransactions} transactions. Average per block are {averageTx} txs during " +
                        $"{startBlock.Header.Time:hh:mm:ss}~{endBlockDto.Header.Time:hh:mm:ss}. " +
                        $"Average each block generated in {timePerBlock} milliseconds. " +
                        $"{timePerTx} txs executed per second.");
        }

        private static int GetPerBlockTimeSpan(BlockDto startBlock, BlockDto endBlockDto)
        {
            var timeSpan = new TimeSpan(endBlockDto.Header.Time.Ticks - startBlock.Header.Time.Ticks);
            var hours = timeSpan.Hours;
            var minutes = timeSpan.Minutes;
            var seconds = timeSpan.Seconds;
            var milliseconds = timeSpan.Milliseconds;

            return (hours * 60 * 60 * 1000 + minutes * 60 * 1000 + seconds * 1000 + milliseconds) / Phase;
        }

        private static int GetTotalBlockSeconds(BlockDto startBlock, BlockDto endBlockDto)
        {
            var timeSpan = new TimeSpan(endBlockDto.Header.Time.Ticks - startBlock.Header.Time.Ticks);
            var hours = timeSpan.Hours;
            var minutes = timeSpan.Minutes;
            var seconds = timeSpan.Seconds;
            var mileSeconds = timeSpan.Milliseconds;

            return 1000 * (hours * 60 * 60 + minutes * 60 + seconds) + mileSeconds;
        }
    }
}