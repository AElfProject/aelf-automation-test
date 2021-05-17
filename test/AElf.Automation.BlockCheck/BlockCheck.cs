using System.Diagnostics;
using System.Threading.Tasks;
using AElf.Client.Service;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using log4net;
using Volo.Abp.Threading;

namespace AElf.Automation.BlockCheck
{
    public class BlockCheck
    {
        public BlockCheck()
        {
            GetService();
        }

        public long GetBlockInfo()
        {
            var currentBlockHeight = AsyncHelper.RunSync(() => _aElfClient.GetBlockHeightAsync());
            if (currentBlockHeight < StartBlock)
            {
                StartBlock = 1;
                VerifyBlockCount = currentBlockHeight;
            }
            else if (currentBlockHeight < StartBlock + VerifyBlockCount)
            {
                VerifyBlockCount = currentBlockHeight - StartBlock;
            }

            Logger.Info($"Check block info start: {StartBlock}, verify count: {VerifyBlockCount}");
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            Parallel.For(1, VerifyBlockCount + 1, item =>
            {
                var blockInfo = AsyncHelper.RunSync(() => _aElfClient.GetBlockByHeightAsync(item + StartBlock, IncludeTransaction));
                Logger.Info(
                    $"block height: {blockInfo.Header.Height}, block hash:{blockInfo.BlockHash}");
            });
            stopwatch.Stop();
            var checkTime = stopwatch.ElapsedMilliseconds;
            var req = (double) VerifyBlockCount / checkTime * 1000;
            Logger.Info($"Check {VerifyBlockCount} block info use {checkTime}ms, req: {req}/s");
            return checkTime;
        }

        public long GetOneBlockInfoTimes()
        {
            var currentBlockHeight = AsyncHelper.RunSync(() => _aElfClient.GetBlockHeightAsync());

            Logger.Info($"Check block {currentBlockHeight} info {VerifyTimes} times.");

            var blockInfo = AsyncHelper.RunSync(() =>
                _aElfClient.GetBlockByHeightAsync(currentBlockHeight, IncludeTransaction));
            Logger.Info("\n" +
                        $"Block height: {blockInfo.Header.Height}\n" +
                        $"Block hash: {blockInfo.BlockHash}\n" +
                        $"Block bloom: {blockInfo.Header.Bloom}\n" +
                        $"Block signer: {blockInfo.Header.SignerPubkey}\n" +
                        $"Block previousBlockHash: {blockInfo.Header.PreviousBlockHash}\n" +
                        $"Block time: {blockInfo.Header.Time}\n" +
                        $"Block chainId: {blockInfo.Header.ChainId}\n" +
                        $"Block merkleTreeRootOfTransactions: {blockInfo.Header.MerkleTreeRootOfTransactions}\n" +
                        $"Block merkleTreeRootOfTransactionState: {blockInfo.Header.MerkleTreeRootOfTransactionState}\n" +
                        $"Block merkleTreeRootOfWorldState: {blockInfo.Header.MerkleTreeRootOfWorldState}\n" +
                        $"Block extra: {blockInfo.Header.Extra}");
            Logger.Info($"Block include transaction: {blockInfo.Body.TransactionsCount}");
            Logger.Info(blockInfo.Body.Transactions);

            var stopwatch = new Stopwatch();
            stopwatch.Start();
            Parallel.For(1, VerifyBlockCount + 1, item =>
            {
                var blockInfo = AsyncHelper.RunSync(() =>
                    _aElfClient.GetBlockByHeightAsync(currentBlockHeight, IncludeTransaction));
                Logger.Info(
                    $"block height: {blockInfo.Header.Height}, block hash:{blockInfo.BlockHash}");
            });
            stopwatch.Stop();
            var checkTime = stopwatch.ElapsedMilliseconds;
            var req = (double) VerifyBlockCount / checkTime * 1000;
            
            Logger.Info($"Check {VerifyBlockCount} block info use {checkTime}ms, req: {req}/s");
            return checkTime;
            
            // for (var i = 0; i < VerifyTimes; i++)
            // {
            //     Logger.Info(i);
            //     var stopwatch = new Stopwatch();
            //     stopwatch.Start();
            //     AsyncHelper.RunSync(() => _aElfClient.GetBlockByHeightAsync(currentBlockHeight, IncludeTransaction));
            //     stopwatch.Stop();
            //     var checkTime = stopwatch.ElapsedMilliseconds;
            //     all += checkTime;
            // }
            //
            // var req = (double) VerifyTimes / all * 1000;
            //
            // Logger.Info($"Check {VerifyTimes} block info use {all}ms, req: {req}/s");
            // return all;
        }

        private void GetService()
        {
            var config = ConfigInfo.ReadInformation;
            var url = config.Url;
            _nodeManager = new NodeManager(url);
            _aElfClient = _nodeManager.ApiClient;
            StartBlock = config.StartBlock;
            VerifyBlockCount = config.VerifyBlockCount;
            IncludeTransaction = config.IncludeTransaction;
            VerifyTimes = config.VerifyTimes;
        }

        private INodeManager _nodeManager;
        private AElfClient _aElfClient;
        public readonly ILog Logger = Log4NetHelper.GetLogger();
        public long StartBlock;
        public long VerifyBlockCount;
        public long VerifyTimes;
        public bool IncludeTransaction;
    }
}