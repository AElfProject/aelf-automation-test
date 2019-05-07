using System;
using AElf.Automation.Common.Helpers;

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
            _apiHelper.RpcGetBlockHeight(command);
            command.GetJsonInfo();

            return long.Parse(command.JsonInfo["result"].ToString());
        }

        public long GetTransactionPoolStatus()
        {
            var command = new CommandInfo(ApiMethods.GetTransactionPoolStatus);
            _apiHelper.RpcGetTransactionPoolStatus(command);
            command.GetJsonInfo();

            return long.Parse(command.JsonInfo["result"]["Queued"].ToString());
        }
    }
}