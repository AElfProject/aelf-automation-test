using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AElf.Automation.Common.Helpers;
using AElf.Automation.Common.WebApi;
using AElf.Automation.Common.WebApi.Dto;

namespace AElf.Automation.RpcPerformance
{
    public class ExecutionSummary
    {
        private readonly IApiService _apiHelper;
        private long _blockHeight;
        private Dictionary<long, BlockDto> _blockMap;
        private readonly ILogHelper _logger = LogHelper.GetLogHelper();
        
        private const int Phase = 120;
        
        public ExecutionSummary(string baseUrl)
        {
            _apiHelper = new WebApiService(baseUrl);
            _blockMap = new Dictionary<long, BlockDto>();
            _blockHeight = GetBlockHeight();
        }

        public void ContinuousCheckTransactionPerformance()
        {
            while (true)
            {
                var height = GetBlockHeight();
                if(height == _blockHeight)
                    Thread.Sleep(5000);
                if(height < _blockHeight)
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
            }
        }

        private void SummaryBlockTransactionInPhase(BlockDto startBlock, BlockDto endBlockDto)
        {
            var totalTransactions = _blockMap.Values.Sum(o=>o.Body.TransactionsCount);
            var averageTx = totalTransactions / Phase;
            var timePerBlock = GetPerBlockTimeSpan(startBlock, endBlockDto);
            _blockMap = new Dictionary<long, BlockDto>();
            _logger.WriteInfo($"Summary Information: {Phase} blocks from height " +
                              $"{startBlock.Header.Height}~{endBlockDto.Header.Height} executed " +
                              $"{totalTransactions} transactions, average per block is {averageTx} tx during " +
                              $"{startBlock.Header.Time:hh:mm:ss}~{endBlockDto.Header.Time:hh:mm:ss}. Block generated per {timePerBlock} milliseconds.");
            _logger.WriteInfo("-------------------------------------------------------------------------------------------------------------------------------");
        }

        private long GetBlockHeight()
        {
            return _apiHelper.GetBlockHeight().Result;
        }

        private BlockDto GetBlockByHeight(long height)
        {
            return _apiHelper.GetBlockByHeight(height).Result;
        }

        private static int GetPerBlockTimeSpan(BlockDto startBlock, BlockDto endBlockDto)
        {
            var timeSpan = new TimeSpan(endBlockDto.Header.Time.Ticks - startBlock.Header.Time.Ticks);
            var hours = timeSpan.Hours;
            var minutes = timeSpan.Minutes;
            var seconds = timeSpan.Seconds;
            var milliseconds = timeSpan.Milliseconds;
            
            return (hours*60*60*1000 + minutes*60*1000 + seconds*1000 + milliseconds) / Phase;
        }
    }
}