using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AElf.Automation.Common.Helpers;
using AElf.Automation.Common.WebApi;
using AElf.Automation.Common.WebApi.Dto;
using log4net;
using Volo.Abp.Threading;

namespace AElf.Automation.RpcPerformance
{
    public class ExecutionSummary
    {
        private readonly IApiService ApiService;
        private long _blockHeight;
        private Dictionary<long, BlockDto> _blockMap;
        private static readonly ILog Logger = Log4NetHelper.GetLogger();

        private const int Phase = 120;

        public int MaxTransactionLimit { get; private set; } = ConfigInfoHelper.Config.SentTxLimit;

        /// <summary>
        /// 统计出块信息
        /// </summary>
        /// <param name="apiHelper"></param>
        /// <param name="fromStart">是否从高度为1开始检测</param>
        public ExecutionSummary(IApiHelper apiHelper, bool fromStart = false)
        {
            ApiService = apiHelper.ApiService;
            _blockMap = new Dictionary<long, BlockDto>();
            _blockHeight = fromStart ? 1 : GetBlockHeight();
        }

        public void ContinuousCheckTransactionPerformance()
        {
            var checkTimes = 0;
            while (true)
            {
                if (checkTimes == 60)
                    break;

                var height = GetBlockHeight();
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
                    var block = GetBlockByHeight(j);
                    _blockMap.Add(j, block);
                    MaxTransactionLimit = MaxTransactionLimit > block.Body.TransactionsCount
                        ? MaxTransactionLimit
                        : block.Body.TransactionsCount;
                    NodeStatusMonitor.MaxLimit = MaxTransactionLimit;
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
            var timePerTx = totalTransactions / GetTotalBlockSeconds(startBlock, endBlockDto);
            _blockMap = new Dictionary<long, BlockDto>();
            Logger.Info($"Summary Information: {Phase} blocks from height " +
                        $"{startBlock.Header.Height}~{endBlockDto.Header.Height} executed " +
                        $"{totalTransactions} transactions. Average per block are {averageTx} txs during " +
                        $"{startBlock.Header.Time:hh:mm:ss}~{endBlockDto.Header.Time:hh:mm:ss}. " +
                        $"Average each block generated in {timePerBlock} milliseconds. " +
                        $"{timePerTx} txs executed per second.");
        }

        private long GetBlockHeight()
        {
            return AsyncHelper.RunSync(ApiService.GetBlockHeight);
        }

        private BlockDto GetBlockByHeight(long height)
        {
            return AsyncHelper.RunSync(() => ApiService.GetBlockByHeight(height));
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

            return hours * 60 * 60 + minutes * 60 + seconds;
        }
    }
}