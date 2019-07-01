using System.Linq;
using System.Threading.Tasks;
using AElf.Automation.Common.Helpers;
using AElf.Automation.Common.WebApi;
using Newtonsoft.Json;
using Volo.Abp.Threading;

namespace AElf.Automation.QueryTransaction
{
    public class StressQuery
    {
        private static readonly ILogHelper Logger = LogHelper.GetLogHelper();
        private readonly WebApiService _apiService;

        public StressQuery(string url)
        {
            _apiService = new WebApiService(url);
        }

        public void RunStressTest(int times)
        {
            for (var i = 0; i < times; i++)
            {
                /*
                var tasks = new List<Task>
                {
                    Task.Run(GetChainStatus),
                    Task.Run(GetCurrentRoundInformationAsync),
                    Task.Run(GetTaskQueueStatusAsync),
                    Task.Run(GetTransactionPoolStatus),
                    Task.Run(GetBlockStateAsync),
                };

                Task.WaitAll(tasks.ToArray());
                */
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
            var chainStatus = await _apiService.GetChainStatus();
            Logger.WriteInfo($"ChainStatus: {chainStatus}");
            
            //query contract descriptor
            var contract = chainStatus.GenesisContractAddress;
            var descriptor = await _apiService.GetContractFileDescriptorSet(contract);
            Logger.WriteInfo($"GetContractFileDescriptorSet: {descriptor}");
        }
        
        private async Task GetCurrentRoundInformationAsync()
        {
            var roundInfo = await _apiService.GetCurrentRoundInformationAsync();
            Logger.WriteInfo(JsonConvert.SerializeObject(roundInfo));
        }

        private async Task GetTaskQueueStatusAsync()
        {
            var queueCollection = await _apiService.GetTaskQueueStatus();
            Logger.WriteInfo($"TaskQueue: {queueCollection}");
        }

        private async Task GetTransactionPoolStatus()
        {
            var queueInfo = await _apiService.GetTransactionPoolStatus();
            Logger.WriteInfo($"Queue info: {queueInfo.Queued}");
        }

        private async Task GetBlockStateAsync()
        {
            //block height
            var height = await _apiService.GetBlockHeight();
            Logger.WriteInfo($"Block height: {height}");
            
            //block by height
            var block = await _apiService.GetBlockByHeight(height, true);
            Logger.WriteInfo($"BlockHash: {block.BlockHash}");
            
            //block by hash
            var block1 = await _apiService.GetBlock(block.BlockHash, true);
            Logger.WriteInfo($"Block info: {JsonConvert.SerializeObject(block1)}");
            
            //query transaction
            var transactionId = block.Body.Transactions.First();
            var transaction = await _apiService.GetTransactionResult(transactionId);
            Logger.WriteInfo($"Transaction: {JsonConvert.SerializeObject(transaction)}");
            
            //query transactions
            var transactions = await _apiService.GetTransactionResults(block.BlockHash);
            Logger.WriteInfo($"Transactions: {transactions}");
            
            //get blockState
            var blockState = await _apiService.GetBlockState(block.BlockHash);
            Logger.WriteInfo($"BlockState: {blockState}");
        }
    }
}