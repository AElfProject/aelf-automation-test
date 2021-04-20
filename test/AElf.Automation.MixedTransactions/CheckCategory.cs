using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using AElf.Client.Service;
using AElfChain.Common.Contracts;
using AElfChain.Common.Helpers;
using log4net;
using Volo.Abp.Threading;

namespace AElf.Automation.MixedTransactions
{
    public class CheckCategory : BasicCategory
    {
        public CheckCategory()
        {
            GetService();
            _aElfClient = NodeManager.ApiClient;
        }

        public void CheckToBalance(Dictionary<int, List<string>> accounts, Dictionary<TokenContract, string> tokenInfos,
            out long duration)
        {
            duration = 0;
            foreach (var (key, value) in tokenInfos)
            {
                long contractDuration = 0;
                Logger.Info("Start Token balance check ...");
                foreach (var (k, v) in accounts)
                {
                    foreach (var acc in v)
                    {
                        var stopwatch = new Stopwatch();
                        stopwatch.Start();
                        var balance = key.GetUserBalance(acc, value);
                        stopwatch.Stop();
                        var checkTime = stopwatch.ElapsedMilliseconds;
                        contractDuration += checkTime;
                        Logger.Info($"{acc} {value} balance is {balance}");
                    }
                }

                Logger.Info(
                    $"{key.ContractAddress} check {accounts.Values.Count * accounts.Keys.Count} user balance time: {contractDuration}ms.");
                duration += contractDuration;
            }
        }

        public void CheckFromBalance(List<string> accounts, Dictionary<TokenContract, string> tokenInfos,
            out long duration)
        {
            duration = 0;
            foreach (var (key, value) in tokenInfos)
            {
                long contractDuration = 0;
                Logger.Info("Start Token balance check ...");
                foreach (var account in accounts)
                {
                    var stopwatch = new Stopwatch();
                    stopwatch.Start();
                    var balance = key.GetUserBalance(account, value);
                    stopwatch.Stop();
                    var checkTime = stopwatch.ElapsedMilliseconds;
                    contractDuration += checkTime;
                    // Logger.Info($"{account} {value} balance is {balance}");
                }

                Logger.Info(
                    $"{key.ContractAddress} check {accounts.Count} user balance time: {contractDuration}ms.");
                duration += contractDuration;
            }
        }

        public void CheckWrapperBalance(Dictionary<int, List<string>> accounts,
            Dictionary<TransferWrapperContract, string> tokenInfos, TokenContract tokenContract, out long duration)
        {
            duration = 0;
            foreach (var (key, value) in tokenInfos)
            {
                long contractDuration = 0;
                Logger.Info("Start Wrapper balance check ...");
                foreach (var (k, v) in accounts)
                {
                    foreach (var acc in v)
                    {
                        var stopwatch = new Stopwatch();
                        stopwatch.Start();
                        var balance = tokenContract.GetUserBalance(acc, value);
                        stopwatch.Stop();
                        var checkTime = stopwatch.ElapsedMilliseconds;
                        contractDuration += checkTime;
                        // Logger.Info($"{acc} {value} balance is {balance}");
                    }
                }

                Logger.Info(
                    $"{key.ContractAddress} check {accounts.Values.Count * accounts.Keys.Count} user balance time: {contractDuration}ms.");
                duration += contractDuration;
            }
        }
        
        public void CheckWrapperFromBalance(List<string> accounts,
            Dictionary<TransferWrapperContract, string> tokenInfos, TokenContract tokenContract, out long duration)
        {
            duration = 0;
            foreach (var (key, value) in tokenInfos)
            {
                long contractDuration = 0;
                Logger.Info("Start Wrapper balance check ...");
                foreach (var account in accounts) 
                {
                    var stopwatch = new Stopwatch();
                    stopwatch.Start();
                    var balance = tokenContract.GetUserBalance(account, value);
                    stopwatch.Stop();
                    var checkTime = stopwatch.ElapsedMilliseconds;
                    contractDuration += checkTime;
                    Logger.Info($"{account} {value} balance is {balance}");
                }

                Logger.Info(
                    $"{tokenContract.ContractAddress} check {accounts.Count} user balance time: {contractDuration}ms.");
                duration += contractDuration;
            }
        }

        public void CheckWrapperVirtualBalance(Dictionary<TransferWrapperContract, string> tokenInfos,
            TokenContract tokenContract, out long duration)
        {
            duration = 0;
            foreach (var (key, value) in tokenInfos)
            {
                long contractDuration = 0;
                var virtualAccount = GetFromVirtualAccounts(key);

                Logger.Info("Start check virtual balance ...");
                foreach (var account in virtualAccount)
                {
                    var stopwatch = new Stopwatch();
                    stopwatch.Start();
                    var balance = tokenContract.GetUserBalance(account, value);
                    stopwatch.Stop();
                    var checkTime = stopwatch.ElapsedMilliseconds;
                    contractDuration += checkTime;
                    Logger.Info($"{account} {value} balance is {balance}");
                }

                Logger.Info(
                    $"{tokenContract.ContractAddress} check {virtualAccount.Count} user balance time: {contractDuration}ms.");
                duration += contractDuration;
            }
        }

        public void ContinueCheckBlock(CancellationTokenSource cts, CancellationToken token)
        {
            try
            {
                var round = 1;
                while (true)
                {
                    try
                    {
                        Thread.Sleep(60000);
                        Logger.Info("Execution check request round: {0}", round);
                        var txsTasks = Task.Run(GetBlockInfo, token);
                        Task.WaitAll(txsTasks);
                        round++;
                    }
                    catch (AggregateException exception)
                    {
                        Logger.Error($"Request to {NodeManager.GetApiUrl()} got exception, {exception}");
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Error(e.Message);
                Logger.Error("Cancel all tasks due to transaction execution exception.");
                cts.Cancel(); //cancel all tasks
            }
        }

        private void GetBlockInfo()
        {
            var verifyCount = VerifyCount;
            var currentBlockHeight = AsyncHelper.RunSync(() => _aElfClient.GetBlockHeightAsync());
            if (currentBlockHeight - VerifyCount < 0)
                verifyCount = currentBlockHeight;
            var startBlock = verifyCount == currentBlockHeight ? 1 : currentBlockHeight - verifyCount;

            Logger.Info($"Check block info start: {startBlock}, verify count: {verifyCount}");
            long all = 0;
            for (var i = startBlock; i <= startBlock + verifyCount; i++)
            {
                var i1 = i;
                var stopwatch = new Stopwatch();
                stopwatch.Start();
                var blockInfo = AsyncHelper.RunSync(() => _aElfClient.GetBlockByHeightAsync(i1, true));
                stopwatch.Stop();
                var checkTime = stopwatch.ElapsedMilliseconds;

                Logger.Info(
                    $"block height: {blockInfo.Header.Height}, block hash:{blockInfo.BlockHash} time:{checkTime}ms");
                all += checkTime;
            }

            var req = (double) VerifyCount / all * 1000;
            Logger.Info($"Check {VerifyCount} block info use {all}ms, req: {req}/s");
        }

        private readonly AElfClient _aElfClient;
        private static readonly ILog Logger = Log4NetHelper.GetLogger();
    }
}