using AElf.Automation.Common.Helpers;
using AElf.Automation.Common.WebApi.Dto;

namespace AElf.Automation.ContractsTesting
{
    public class NodeStatus
    {
        private readonly IApiHelper _apiHelper;

        public NodeStatus(IApiHelper apiHelper)
        {
            _apiHelper = apiHelper;
        }

        public long GetBlockHeight()
        {
            var command = new CommandInfo(ApiMethods.GetBlockHeight);
            _apiHelper.GetBlockHeight(command);

            var height = (long) command.InfoMsg;

            return height;
        }

        public int GetTransactionPoolStatus()
        {
            var command = new CommandInfo(ApiMethods.GetTransactionPoolStatus);
            _apiHelper.GetTransactionPoolStatus(command);
            var transactionPoolStatus = command.InfoMsg as GetTransactionPoolStatusOutput;

            return transactionPoolStatus?.Queued ?? 0;
        }

        public BlockDto GetBlockInfo(long height)
        {
            var command = new CommandInfo(ApiMethods.GetBlockByHeight)
            {
                Parameter = $"{height} false"
            };
            _apiHelper.GetBlockByHeight(command);

            return command.InfoMsg as BlockDto;
        }

        public ChainStatusDto GetChainInformation()
        {
            var command = new CommandInfo(ApiMethods.GetChainInformation);
            _apiHelper.GetChainInformation(command);

            return command.InfoMsg as ChainStatusDto;
        }
    }
}