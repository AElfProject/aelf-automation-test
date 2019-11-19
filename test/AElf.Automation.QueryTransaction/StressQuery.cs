using System;
using System.Linq;
using System.Threading.Tasks;
using AElfChain.Common.Helpers;
using AElfChain.SDK;
using log4net;
using Newtonsoft.Json;
using Volo.Abp.Threading;

namespace AElf.Automation.QueryTransaction
{
    public class StressQuery
    {
        private static readonly ILog Logger = Log4NetHelper.GetLogger();
        private readonly IApiService _apiService;

        public StressQuery(string url)
        {
            _apiService = AElfChainClient.GetClient(url);
        }

        public void RunStressTest(int times)
        {
            for (var i = 0; i < times; i++)
            {
                AsyncHelper.RunSync(GetChainStatus);
                AsyncHelper.RunSync(GetCurrentRoundInformationAsync);
                AsyncHelper.RunSync(GetTaskQueueStatusAsync);
                AsyncHelper.RunSync(GetTransactionPoolStatus);
                AsyncHelper.RunSync(GetBlockStateAsync);
            }
        }

        private async Task GetChainStatus()
        {
            //chain status
            var chainStatus = await _apiService.GetChainStatusAsync();
            Logger.Info($"ChainStatus: {chainStatus}");

            //query contract descriptor
            var contract = chainStatus.GenesisContractAddress;
            var descriptor = await _apiService.GetContractFileDescriptorSetAsync(contract);
            Logger.Info($"GetContractFileDescriptorSet: {descriptor}");
        }

        private async Task GetCurrentRoundInformationAsync()
        {
            var roundInfo = await _apiService.GetCurrentRoundInformationAsync();
            Logger.Info(JsonConvert.SerializeObject(roundInfo));
        }

        private async Task GetTaskQueueStatusAsync()
        {
            var queueCollection = await _apiService.GetTaskQueueStatusAsync();
            Logger.Info($"TaskQueue: {queueCollection}");
        }

        private async Task GetTransactionPoolStatus()
        {
            var queueInfo = await _apiService.GetTransactionPoolStatusAsync();
            Logger.Info($"Queue info: {queueInfo}");
        }

        private async Task GetBlockStateAsync()
        {
            //block height
            var height = await _apiService.GetBlockHeightAsync();
            Logger.Info($"Block height: {height}");

            //block by height
            var block = await _apiService.GetBlockByHeightAsync(height, true);
            Logger.Info($"BlockHash: {block.BlockHash}");

            //block by hash
            var block1 = await _apiService.GetBlockAsync(block.BlockHash, true);
            Logger.Info($"Block info: {JsonConvert.SerializeObject(block1)}");

            //query transaction
            var transactionId = block.Body.Transactions.First();
            try
            {
                var transaction = await _apiService.GetTransactionResultAsync(transactionId.Replace('a', 'x'));
                Logger.Info($"Transaction: {JsonConvert.SerializeObject(transaction)}");
            }
            catch (Exception e)
            {
                Logger.Error(e.Message);
            }

            //query transactions
            var transactions = await _apiService.GetTransactionResultsAsync(block.BlockHash);
            Logger.Info($"Transactions: {transactions}");

            //get blockState
            var blockState = await _apiService.GetBlockStateAsync(block.BlockHash);
            Logger.Info($"BlockState: {blockState}");
        }
    }
}