using System;
using System.Collections.Generic;
using System.Threading;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using AElf.Types;
using AElfChain.SDK.Models;
using log4net;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Volo.Abp.Threading;

namespace AElf.Automation.RpcPerformance
{
    public class NodeStatusMonitor
    {
        private static readonly ILog Logger = Log4NetHelper.GetLogger();
        private INodeManager NodeManager { get; }
        private long BlockHeight { get; set; } = 1;
        public static int MaxLimit { private get; set; }

        public NodeStatusMonitor(INodeManager nodeManager)
        {
            NodeManager = nodeManager;
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
                    AsyncHelper.RunSync(() => NodeManager.ApiService.GetTransactionResultAsync(transactionIds[i1]));
                var resultStatus = transactionResult.Status.ConvertTransactionResultStatus();
                switch (resultStatus)
                {
                    case TransactionResultStatus.Mined:
                        Logger.Info($"Transaction: {transactionIds[i]}, Status: {resultStatus}", true);
                        transactionIds.Remove(transactionIds[i]);
                        break;
                    case TransactionResultStatus.Pending:
                    case TransactionResultStatus.Unexecutable:
                        Console.Write($"\rTransaction: {transactionIds[i]}, Status: {resultStatus}{SpinInfo(checkTimes)}");
                        break;
                    case TransactionResultStatus.Failed:
                        Logger.Error($"Transaction: {transactionIds[i]}, Status: {resultStatus}", true);
                        Logger.Error($"Error message: {transactionResult.Error}", true);
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
                Console.Write($"\rLast one: {transactionIds[0]}");
                var transactionResult =
                    AsyncHelper.RunSync(() => NodeManager.ApiService.GetTransactionResultAsync(transactionIds[0]));
                var txResult = transactionResult.Status.ConvertTransactionResultStatus();
                switch (txResult)
                {
                    case TransactionResultStatus.Pending:
                    case TransactionResultStatus.Unexecutable:
                        CheckTransactionsStatus(transactionIds, checkTimes);
                        Thread.Sleep(500);
                        break;
                    case TransactionResultStatus.Mined:
                        Logger.Info($"Transaction: {transactionIds[0]}, Status: {txResult}", true);
                        transactionIds.RemoveAt(0);
                        return;
                    default:
                        Logger.Error($"Transaction: {transactionIds[0]}, Status: {txResult}", true);
                        Logger.Error($"Error message: {transactionResult.Error}", true);
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
                var currentHeight = AsyncHelper.RunSync(NodeManager.ApiService.GetBlockHeightAsync);
                if (BlockHeight != currentHeight)
                {
                    BlockHeight = currentHeight;
                    return;
                }

                checkTimes++;
                Thread.Sleep(100);
                if (checkTimes % 10 == 0)
                    Console.Write(
                        $"\rCurrent block height {currentHeight}, not changed in {checkTimes / 10 : 000} seconds.");

                if (checkTimes != 3000) continue;
                
                Console.Write("\r\n");
                throw new TimeoutException("Node block exception, block height not changed 5 minutes later.");
            }
        }

        private int GetTransactionPoolTxCount()
        {
            var transactionPoolStatusOutput =
                AsyncHelper.RunSync(NodeManager.ApiService.GetTransactionPoolStatusAsync);

            return transactionPoolStatusOutput.Queued;
        }

        private static string SpinInfo(int number)
        {
            switch (number % 4)
            {
                case 0: return "/";
                case 1: return "-";
                case 2: return "\\";
                case 3: return "-";
                default:
                    return ".";
            }
        }
    }
}