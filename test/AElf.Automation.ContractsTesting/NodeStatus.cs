using System;
using System.Security.Cryptography;
using AElf.Automation.Common.Helpers;
using AElf.Automation.Common.WebApi.Dto;

namespace AElf.Automation.ContractsTesting
{
    public class NodeStatus
    {
        private readonly IApiHelper _apiHelper;
        private static readonly ILogHelper _log = LogHelper.GetLogHelper();

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
    }
}