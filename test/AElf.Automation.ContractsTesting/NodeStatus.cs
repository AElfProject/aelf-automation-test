using System.Threading;
using AElf.Automation.Common.Contracts;
using AElf.Automation.Common.Helpers;
using AElf.Automation.Common.Managers;
using AElfChain.SDK;
using AElfChain.SDK.Models;
using Google.Protobuf.WellKnownTypes;
using log4net;
using Volo.Abp.Threading;
using ApiMethods = AElf.Automation.Common.Managers.ApiMethods;

namespace AElf.Automation.ContractsTesting
{
    public class NodeStatus
    {
        private readonly INodeManager _nodeManager;
        private IApiService _apiService => _nodeManager.ApiService;

        public ILog Logger = Log4NetHelper.GetLogger();

        public NodeStatus(INodeManager nodeManager)
        {
            _nodeManager = nodeManager;
        }

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
            return AsyncHelper.RunSync(()=>_apiService.GetBlockByHeightAsync(height));
        }

        public ChainStatusDto GetChainInformation()
        {
            var command = new CommandInfo(ApiMethods.GetChainInformation);
            _nodeManager.GetChainInformation(command);

            return command.InfoMsg as ChainStatusDto;
        }

        public void CheckConfigurationInfo()
        {
            var account = _nodeManager.AccountManager.GetRandomAccount();
            var genesis = GenesisContract.GetGenesisContract(_nodeManager, account);
            var configurationStub = genesis.GetConfigurationStub();
            var limit = AsyncHelper.RunSync(()=>configurationStub.GetBlockTransactionLimit.CallAsync(new Empty()));
            Logger.Info($"Current transaction limit number is: {limit.Value}");

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