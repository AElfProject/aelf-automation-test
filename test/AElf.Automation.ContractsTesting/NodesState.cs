using System.Threading;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using Newtonsoft.Json;
using Volo.Abp.Threading;

namespace AElf.Automation.ContractsTesting
{
    public class NodesState
    {
        private static readonly ILogHelper Log = LogHelper.GetLogger();

        public static void NodeStateCheck(string name, string rpcUrl)
        {
            var nodeManager = new NodeManager(rpcUrl);
            var nodeStatus = new NodeStatus(nodeManager);
            long height = 1;
            while (true)
            {
                //string message;
                var currentHeight = nodeStatus.GetBlockHeight();
                //var txPoolCount = nodeStatus.GetTransactionPoolStatus();
                if (currentHeight == height)
                {
                    //message = $"Node: {name}, TxPool Count: {txPoolCount}";
                    //Log.Info(message);
                    Thread.Sleep(250);
                }
                else
                {
                    height = currentHeight;
                    //message = $"Node: {name}, Height: {currentHeight}, TxPool Count: {txPoolCount}";
                    //Log.Info(message);
                    var chainStatus = AsyncHelper.RunSync(nodeManager.ApiClient.GetChainStatusAsync);
                    Log.Info($"Chain Status: {JsonConvert.SerializeObject(chainStatus, Formatting.Indented)}");
                    var blockInfo = nodeStatus.GetBlockInfo(height);
                    var blockMessage =
                        $"Node: {name}, Height: {blockInfo.Header.Height}, BlockHash: {blockInfo.BlockHash}, Transaction Count: {blockInfo.Body.TransactionsCount}";
                    Log.Info(blockMessage);
                    Thread.Sleep(500);
                }
            }
        }

        public static void GetAllBlockTimes(string name, string url)
        {
            var nodeManager = new NodeManager(url);
            var nodeStatus = new NodeStatus(nodeManager);

            var currentHeight = nodeStatus.GetBlockHeight();
            for (var i = 1; i <= currentHeight; i++)
            {
                var height = i;
                var blockInfo = nodeStatus.GetBlockInfo(height);
                Log.Info(
                    $"Node: {name}, Height={blockInfo.Header.Height}, TxCount={blockInfo.Body.TransactionsCount}, Time={blockInfo.Header.Time:hh:mm:ss.fff}");
            }
        }
    }
}