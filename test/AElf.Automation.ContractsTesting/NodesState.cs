using System.Threading;
using AElf.Automation.Common.Helpers;
using Newtonsoft.Json;

namespace AElf.Automation.ContractsTesting
{
    public class NodesState
    {
        private static readonly ILog Log = LogHelper.GetLogHelper();

        public static void NodeStateCheck(string name, string rpcUrl)
        {
            var apiHelper = new WebApiHelper(rpcUrl);
            var nodeStatus = new NodeStatus(apiHelper);
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
                    var chainStatus = nodeStatus.GetChainInformation();
                    Log.Info($"Chain Status: {JsonConvert.SerializeObject(chainStatus)}");
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
            var apiHelper = new WebApiHelper(url);
            var nodeStatus = new NodeStatus(apiHelper);

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