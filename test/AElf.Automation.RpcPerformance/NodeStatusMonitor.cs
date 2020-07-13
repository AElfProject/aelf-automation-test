using System;
using System.Collections.Generic;
using System.Threading;
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
        public static int MaxQueueLimit = 5012;

        public NodeStatusMonitor(INodeManager nodeManager)
        {
            NodeManager = nodeManager;
            LimitValidateLimit = RpcConfig.ReadInformation.SentTxLimit;
            IsUpdate = RpcConfig.ReadInformation.UpdateTxLimit;
        }

        private INodeManager NodeManager { get; }
        private long BlockHeight { get; set; } = 1;
        public static int MaxValidateLimit { private get; set; } = 5;
        public static int LimitValidateLimit { private get; set; }
        public static bool IsUpdate { get; set; }
        public static int times { get; set; } = 1;

        public bool CheckTransactionPoolStatus(bool enable)
        {
            if (!enable) return true;
            var checkTimes = 0;
            while (true)
            {
                if (checkTimes >= 150) return false; //over check time and cancel current round execution            
                var poolStatus = GetTransactionPoolTxCount();
                if (IsUpdate && times == 1000)
                {
                    var old = MaxValidateLimit;
                    var update = MaxValidateLimit * 3;
                    MaxValidateLimit = update >= LimitValidateLimit ? LimitValidateLimit : update;
                    Logger.Info($"Update tx limit {old} to {MaxValidateLimit}");
                    times = 1;
                }
                times++;
                if (poolStatus.Validated < MaxValidateLimit && poolStatus.Queued < MaxQueueLimit)
                    return true;

                checkTimes++;
                if (checkTimes % 10 == 0)
                    $"TxHub transaction count: QueuedCount={poolStatus.Queued} ValidatedCount={poolStatus.Validated}. Transaction limit: {MaxValidateLimit}"
                        .WriteWarningLine();
                Thread.Sleep(200);
            }
        }

        public void CheckTransactionsStatus(IList<string> transactionIds, int checkTimes = -1,INodeManager nodeManager = null)
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
                var transactionResult = AsyncHelper.RunSync(() => nodeManager.ApiClient.GetTransactionResultAsync(transactionIds[i1]));
                var resultStatus = transactionResult.Status.ConvertTransactionResultStatus();
                switch (resultStatus)
                {
                    case TransactionResultStatus.Mined:
                        Logger.Info(
                            $"Transaction: {transactionIds[i]}, Method: {transactionResult.Transaction.MethodName}, Status: {resultStatus}-[{transactionResult.GetTransactionFeeInfo()}]",
                            true);
                        transactionIds.Remove(transactionIds[i]);
                        break;
                    case TransactionResultStatus.Pending:
                    case TransactionResultStatus.PendingValidation:
                    case TransactionResultStatus.Unexecutable:
                        Console.Write(
                            $"\rTransaction: {transactionIds[i]}, Status: {resultStatus}{SpinInfo(checkTimes)}");
                        Thread.Sleep(500);
                        break;
                    case TransactionResultStatus.NodeValidationFailed:
                        Logger.Error($"Transaction: {transactionIds[i]}, Status: {resultStatus}",true);
                        Logger.Error($"Error message: {transactionResult.Error}", true);
                        transactionIds.Remove(transactionIds[i]);
                        break;
                    case TransactionResultStatus.Failed:
                        Logger.Error(
                            $"Transaction: {transactionIds[i]}, Status: {resultStatus}-[{transactionResult.GetTransactionFeeInfo()}]",
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
                CheckTransactionsStatus(transactionIds, checkTimes,nodeManager);
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
                    case TransactionResultStatus.Unexecutable:
                        CheckTransactionsStatus(transactionIds, checkTimes,nodeManager);
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
                var currentHeight = AsyncHelper.RunSync(NodeManager.ApiClient.GetBlockHeightAsync);
                if (BlockHeight != currentHeight)
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
            var transactionPoolStatusOutput =
                AsyncHelper.RunSync(NodeManager.ApiClient.GetTransactionPoolStatusAsync);

            return transactionPoolStatusOutput;
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