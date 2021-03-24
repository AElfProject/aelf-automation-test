using System;
using System.Collections.Generic;
using System.Threading;
using AElf.Client;
using AElf.Client.Dto;
using AElf.Types;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using log4net;
using Volo.Abp.Threading;

namespace AElf.Automation.RpcPerformance
{
    public class NodeStatusMonitor
    {
        private static readonly ILog Logger = Log4NetHelper.GetLogger();

        public NodeStatusMonitor(INodeManager nodeManager)
        {
            NodeManager = nodeManager;
       }

        private INodeManager NodeManager { get; }
        private long BlockHeight { get; set; } = 1;

        public void CheckTransactionsStatus(IList<string> transactionIds, int checkTimes = -1,
            INodeManager nodeManager = null)
        {
            if (nodeManager == null) nodeManager = NodeManager;
            if (checkTimes == -1)
                checkTimes = RpcConfig.ReadInformation.Timeout * 10;
            if (checkTimes == 0)
                throw new TimeoutException("Transaction status check is timeout.");
            checkTimes--;
            var listCount = transactionIds.Count;
            var length = transactionIds.Count;
            for (var i = length - 1; i >= 0; i--)
            {
                var i1 = i;
                TransactionResultDto transactionResult;
                try
                {
                    transactionResult = AsyncHelper.RunSync(() =>
                        nodeManager.ApiClient.GetTransactionResultAsync(transactionIds[i1]));
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    Thread.Sleep(2000);
                    Logger.Info($"Try again {transactionIds[i1]}");
                    transactionResult = AsyncHelper.RunSync(() =>
                        nodeManager.ApiClient.GetTransactionResultAsync(transactionIds[i1]));
                }

                var resultStatus = transactionResult.Status.ConvertTransactionResultStatus();
                switch (resultStatus)
                {
                    case TransactionResultStatus.Mined:
                        Logger.Info(
                            $"Transaction: {transactionIds[i]}, Method: {transactionResult.Transaction.MethodName}, Status: {resultStatus}",
                            true);
                        transactionIds.Remove(transactionIds[i]);
                        break;
                    case TransactionResultStatus.Pending:
                    case TransactionResultStatus.PendingValidation:
                        Console.Write(
                            $"\rTransaction: {transactionIds[i]}, Status: {resultStatus}{SpinInfo(checkTimes)}");
                        Thread.Sleep(500);
                        break;
                    case TransactionResultStatus.NodeValidationFailed:
                        Logger.Error($"Transaction: {transactionIds[i]}, Status: {resultStatus}", true);
                        Logger.Error($"Error message: {transactionResult.Error}", true);
                        transactionIds.Remove(transactionIds[i]);
                        break;
                    case TransactionResultStatus.Failed:
                        Logger.Error(
                            $"Transaction: {transactionIds[i]}, Status: {resultStatus}",
                            true);
                        Logger.Error($"Error message: {transactionResult.Error}", true);
                        transactionIds.Remove(transactionIds[i]);
                        break;
                }
            }

            if (transactionIds.Count > 0 && transactionIds.Count != 1)
            {
                if (listCount == transactionIds.Count && checkTimes == 0)
                    throw new TimeoutException("Transaction status always keep pending or not existed.");
                CheckTransactionsStatus(transactionIds, checkTimes, nodeManager);
            }

            if (transactionIds.Count == 1)
            {
                Console.Write($"\rLast one: {transactionIds[0]}");
                var transactionResult =
                    AsyncHelper.RunSync(() => nodeManager.ApiClient.GetTransactionResultAsync(transactionIds[0]));
                var txResult = transactionResult.Status.ConvertTransactionResultStatus();
                switch (txResult)
                {
                    case TransactionResultStatus.Pending:
                        CheckTransactionsStatus(transactionIds, checkTimes, nodeManager);
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

            Thread.Sleep(200);
        }

        public void CheckNodeHeightStatus(bool enable = true)
        {
            if (!enable) return;

            var checkTimes = 0;
            while (true)
            {
                long currentHeight = 0;
                try
                {
                    currentHeight = AsyncHelper.RunSync(NodeManager.ApiClient.GetBlockHeightAsync);
                }
                catch (AElfClientException e)
                {
                    Logger.Error("GetBlockHeightAsync error ...");
                    Logger.Error(e);
                }

                if (BlockHeight != currentHeight && currentHeight != 0)
                {
                    BlockHeight = currentHeight;
                    return;
                }

                checkTimes++;
                Thread.Sleep(100);
                if (checkTimes % 10 == 0)
                    Console.Write(
                        $"\rCurrent block height {currentHeight}, not changed in {checkTimes / 10: 000} seconds.");

                if (checkTimes != 3000) continue;

                Console.Write("\r\n");
                throw new TimeoutException("Node block exception, block height not changed 5 minutes later.");
            }
        }

        private TransactionPoolStatusOutput GetTransactionPoolTxCount()
        {
            try
            {
                var transactionPoolStatusOutput =
                    AsyncHelper.RunSync(NodeManager.ApiClient.GetTransactionPoolStatusAsync);

                return transactionPoolStatusOutput;
            }
            catch (AElfClientException e)
            {
                Logger.Error("GetTransactionPoolStatusAsync error ...");
                Logger.Error(e);
                return new TransactionPoolStatusOutput();
            }
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