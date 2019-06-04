using System.Collections.Generic;
using System.Threading;
using AElf.Automation.Common.Helpers;
using AElf.Automation.Common.WebApi.Dto;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Volo.Abp.Threading;

namespace AElf.Automation.RpcPerformance
{
    public class NodeStatusMonitor
    {
        private readonly ILogHelper _logger = LogHelper.GetLogHelper();
        private IApiHelper ApiHelper { get; }
        
        private long BlockHeight { get; set; } = 1;
        
        public static int MaxLimit { get; set; }
        
        public NodeStatusMonitor(IApiHelper apiHelper)
        {
            ApiHelper = apiHelper;
            MaxLimit = ConfigInfoHelper.Config.TransactionLimit;;
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
                Thread.Sleep(100);
                if (_checkCount % 10 == 0)
                    _logger.WriteWarn(
                        $"TxHub transaction userCount is {txCount}, test limit number is: {MaxLimit}");
            }
        }

        public void CheckTransactionsStatus(IList<string> transactionIds, int checkTimes = 60)
        {
            if (checkTimes < 0)
                Assert.IsTrue(false, "Transaction status check is timeout.");
            checkTimes--;
            var listCount = transactionIds.Count;
            var length = transactionIds.Count;
            for (var i = length - 1; i >= 0; i--)
            {
                var ci = new CommandInfo(ApiMethods.GetTransactionResult) {Parameter = transactionIds[i]};
                ApiHelper.GetTxResult(ci);

                if (ci.Result)
                {
                    var transactionResult = ci.InfoMsg as TransactionResultDto;
                    var deployResult = transactionResult?.Status;
                    if (deployResult == "Mined")
                    {
                        _logger.WriteInfo($"Transaction: {transactionIds[i]}, Status: Mined");
                        transactionIds.Remove(transactionIds[i]);
                    }
                }

                Thread.Sleep(15);
            }

            if (transactionIds.Count > 0 && transactionIds.Count != 1)
            {
                if (listCount == transactionIds.Count && checkTimes == 0)
                    Assert.IsTrue(false, "Transaction not executed successfully.");
                CheckTransactionsStatus(transactionIds, checkTimes);
            }

            if (transactionIds.Count == 1)
            {
                _logger.WriteInfo("Last one: {0}", transactionIds[0]);
                var ci = new CommandInfo(ApiMethods.GetTransactionResult) {Parameter = transactionIds[0]};
                ApiHelper.ExecuteCommand(ci);

                if (ci.Result)
                {
                    var transactionResult = ci.InfoMsg as TransactionResultDto;
                    var txResult = transactionResult?.Status;
                    if (txResult != "Mined")
                    {
                        Thread.Sleep(50);
                        CheckTransactionsStatus(transactionIds, checkTimes);
                    }
                }
            }

            Thread.Sleep(50);
        }
        
        public void CheckNodeHeightStatus()
        {
            var checkTimes = 0;
            while (true)
            {
                var ci = new CommandInfo(ApiMethods.GetBlockHeight);
                ApiHelper.GetBlockHeight(ci);
                var currentHeight = (long) ci.InfoMsg;

                if (BlockHeight != currentHeight)
                {
                    BlockHeight = currentHeight;
                    return;
                }

                checkTimes++;
                Thread.Sleep(100);
                if (checkTimes % 100 == 0)
                    _logger.WriteWarn(
                        $"Current block height {currentHeight}, not changed in {checkTimes / 10} seconds.");

                if (checkTimes == 3000)
                    Assert.IsTrue(false, "Node block exception, block height not changed 5 minutes later.");
            }
        }
        private int GetTransactionPoolTxCount()
        {
            var transactionPoolStatusOutput =
                AsyncHelper.RunSync(() => ApiHelper.ApiService.GetTransactionPoolStatus());

            return transactionPoolStatusOutput.Queued;
        }
    }
}