using System.Threading;
using AElf.Client.Dto;
using AElf.Client.Service;
using AElf.Types;
using AElfChain.Common.Contracts;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using Google.Protobuf.WellKnownTypes;
using log4net;
using Volo.Abp.Threading;

namespace AElf.Automation.ContractsTesting
{
    public class NodeStatus
    {
        private readonly INodeManager _nodeManager;

        public ILog Logger = Log4NetHelper.GetLogger();

        public NodeStatus(INodeManager nodeManager)
        {
            _nodeManager = nodeManager;
        }

        private AElfClient _apiService => _nodeManager.ApiClient;

        public long GetBlockHeight()
        {
            return AsyncHelper.RunSync(_apiService.GetBlockHeightAsync);
        }

        public int GetTransactionPoolStatus()
        {
            var transactionPoolStatus = AsyncHelper.RunSync(_apiService.GetTransactionPoolStatusAsync);

            return transactionPoolStatus?.Queued ?? 0;
        }

        public BlockDto GetBlockInfo(long height)
        {
            return AsyncHelper.RunSync(() => _apiService.GetBlockByHeightAsync(height));
        }

        public void GetBlocksInformation(long start, long end)
        {
            //740020,742810
            for (var height = start; height <= end; height++)
            {
                var current = height;
                var block = AsyncHelper.RunSync(() => _apiService.GetBlockByHeightAsync(current));
                Logger.Info($"{height},{block.Header.Time},{block.BlockHash},{block.Body.TransactionsCount}");
            }

            "Complete analyze result.".WriteSuccessLine();
        }

        public void CheckConfigurationInfo()
        {
            var account = _nodeManager.AccountManager.GetRandomAccount();
            var genesis = GenesisContract.GetGenesisContract(_nodeManager, account);
            var configurationStub = genesis.GetConfigurationStub();
            var limit = AsyncHelper.RunSync(() => configurationStub.GetConfiguration.CallAsync(new StringValue
                {Value = nameof(ConfigurationNameProvider.BlockTransactionLimit)}));
            var limitValue = SInt32Value.Parser.ParseFrom(limit.Value).Value;
            Logger.Info($"Current transaction limit number is: {limitValue}");

            var currentHeightBefore = AsyncHelper.RunSync(_apiService.GetBlockHeightAsync);
            Thread.Sleep(4000);
            var currentHeightAfter = AsyncHelper.RunSync(_apiService.GetBlockHeightAsync);

            for (var i = currentHeightBefore; i <= currentHeightAfter; i++)
            {
                var i1 = i;
                var block = AsyncHelper.RunSync(() => _apiService.GetBlockByHeightAsync(i1, true));
                Logger.Info($"Height: {i1}, transaction count: {block.Body.TransactionsCount}");
            }
        }
    }
}