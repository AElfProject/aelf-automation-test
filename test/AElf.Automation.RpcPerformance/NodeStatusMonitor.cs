using System.Collections.Generic;
using System.Threading;
using AElf.Automation.Common.Helpers;
using AElfChain.SDK.Models;
using AElf.Types;
using log4net;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Volo.Abp.Threading;

namespace AElf.Automation.RpcPerformance
{
    public class NodeStatusMonitor
    {
        private static readonly ILog Logger = Log4NetHelper.GetLogger();
        private IApiHelper ApiHelper { get; }
        private long BlockHeight { get; set; } = 1;
        public static int MaxLimit { get; set; }

        public NodeStatusMonitor(IApiHelper apiHelper)
        {
            ApiHelper = apiHelper;
            MaxLimit = ConfigInfoHelper.Config.SentTxLimit;
        }

        private static int _checkCount;
        private readonly object _checkObj = new object();

        public void CheckTransactionPoolStatus(bool enable)
        {
            if (!enable) return;
            while (true)
            {
                var txCount = GetTransactionPoolTxCount();
                if (txCount < MaxLimit)
                {
                    lock (_checkObj)
                    {
                        _checkCount = 0;
                    }

                    break;
                }

                lock (_checkObj)
                {
                    _checkCount++;
                }

                Thread.Sleep(200);
                if (_checkCount % 10 == 0)
                    $"TxHub transaction count:{txCount}, transaction limit number: {MaxLimit}".WriteWarningLine();
            }
        }

        public void CheckTransactionsStatus(IList<string> transactionIds, int checkTimes = -1)
        {
            if (checkTimes == -1)
                checkTimes = ConfigInfoHelper.Config.Timeout * 10;
            if (checkTimes == 0)
                Assert.IsTrue(false, "Transaction status check is timeout.");
            checkTimes--;
            var listCount = transactionIds.Count;
            var length = transactionIds.Count;
            for (var i = length - 1; i >= 0; i--)
            {
                var i1 = i;
                var transactionResult =
                    AsyncHelper.RunSync(() => ApiHelper.ApiService.GetTransactionResultAsync(transactionIds[i1]));
                var resultStatus = transactionResult.Status.ConvertTransactionResultStatus();
                switch (resultStatus)
                {
                    case TransactionResultStatus.Mined:
                        Logger.Info($"Transaction: {transactionIds[i]}, Status: {resultStatus}");
                        transactionIds.Remove(transactionIds[i]);
                        break;
                    case TransactionResultStatus.Pending:
                        $"Transaction: {transactionIds[i]}, Status: {resultStatus}".WriteWarningLine();
                        break;
                    case TransactionResultStatus.Failed:
                    case TransactionResultStatus.Unexecutable:
                        Logger.Error($"Transaction: {transactionIds[i]}, Status: {resultStatus}");
                        Logger.Error($"Error message: {transactionResult.Error}");
                        transactionIds.Remove(transactionIds[i]);
                        break;
                }
            }

            if (transactionIds.Count > 0 && transactionIds.Count != 1)
            {
                if (listCount == transactionIds.Count && checkTimes == 0)
                    Assert.IsTrue(false, "Transaction status always keep pending or not existed.");
                CheckTransactionsStatus(transactionIds, checkTimes);
            }

            if (transactionIds.Count == 1)
            {
                Logger.Info("Last one: {0}", transactionIds[0]);
                var transactionResult =
                    AsyncHelper.RunSync(() => ApiHelper.ApiService.GetTransactionResultAsync(transactionIds[0]));
                var txResult = transactionResult.Status.ConvertTransactionResultStatus();
                switch (txResult)
                {
                    case TransactionResultStatus.Pending:
                        CheckTransactionsStatus(transactionIds, checkTimes);
                        Thread.Sleep(500);
                        break;
                    case TransactionResultStatus.Mined:
                        Logger.Info($"Transaction: {transactionIds[0]}, Status: {txResult}");
                        transactionIds.RemoveAt(0);
                        return;
                    default:
                        Logger.Error($"Transaction: {transactionIds[0]}, Status: {txResult}");
                        Logger.Error($"Error message: {transactionResult.Error}");
                        break;
                }
            }

            Thread.Sleep(100);
        }

        public void CheckNodeHeightStatus(bool enable = true)
        {
            if (!enable) return;

            var checkTimes = 0;
            while (true)
            {
                var currentHeight = AsyncHelper.RunSync(ApiHelper.ApiService.GetBlockHeightAsync);
                if (BlockHeight != currentHeight)
                {
                    BlockHeight = currentHeight;
                    return;
                }

                checkTimes++;
                Thread.Sleep(100);
                if (checkTimes % 100 == 0)
                    Logger.Warn(
                        $"Current block height {currentHeight}, not changed in {checkTimes / 10} seconds.");

                if (checkTimes == 3000)
                    Assert.IsTrue(false, "Node block exception, block height not changed 5 minutes later.");
            }
        }

        private int GetTransactionPoolTxCount()
        {
            var transactionPoolStatusOutput =
                AsyncHelper.RunSync(ApiHelper.ApiService.GetTransactionPoolStatusAsync);

            return transactionPoolStatusOutput.Queued;
        }
    }
}