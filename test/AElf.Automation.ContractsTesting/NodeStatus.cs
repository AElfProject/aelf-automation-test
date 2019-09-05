using AElf.Automation.Common.Helpers;
using AElf.Automation.Common.Managers;
using AElfChain.SDK;
using AElfChain.SDK.Models;
using Volo.Abp.Threading;
using ApiMethods = AElf.Automation.Common.Managers.ApiMethods;

namespace AElf.Automation.ContractsTesting
{
    public class NodeStatus
    {
        private readonly INodeManager _nodeManager;
        private IApiService _apiService => _nodeManager.ApiService;

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
    }
}