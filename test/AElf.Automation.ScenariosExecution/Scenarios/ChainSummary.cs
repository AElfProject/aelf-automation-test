using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AElf.Client.Dto;
using AElf.Client.Service;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Helpers;
using log4net;
using Volo.Abp.Threading;

namespace AElf.Automation.ScenariosExecution.Scenarios
{
    public class ChainSummary
    {
        private const int Phase = 120;
        private static readonly ILog Logger = Log4NetHelper.GetLogger();
        private readonly AElfClient _apiService;
        private long _blockHeight;
        private Dictionary<long, BlockDto> _blockMap;

        public ChainSummary(string baseUrl)
        {
            _apiService = AElfClientExtension.GetClient(baseUrl);
            _blockMap = new Dictionary<long, BlockDto>();
            _blockHeight = GetBlockHeight();
        }

        public void ContinuousCheckChainStatus()
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
                    Thread.Sleep(5000);
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
            _blockMap = new Dictionary<long, BlockDto>();
            Logger.Info($"Summary Information: {Phase} blocks from height " +
                        $"{startBlock.Header.Height}~{endBlockDto.Header.Height} executed " +
                        $"{totalTransactions} transactions, average per block is {averageTx} tx during " +
                        $"{startBlock.Header.Time:hh:mm:ss}~{endBlockDto.Header.Time:hh:mm:ss}. Block generated per {timePerBlock} milliseconds.");
        }

        private long GetBlockHeight()
        {
            return AsyncHelper.RunSync(_apiService.GetBlockHeightAsync);
        }

        private BlockDto GetBlockByHeight(long height)
        {
            return AsyncHelper.RunSync(() => _apiService.GetBlockByHeightAsync(height));
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
    }
}