using System.Collections.Generic;
using System.Diagnostics;
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
            SystemToken = ContractManager.Token;
            _aElfClient = NodeManager.ApiClient;
        }
        
        public void CheckBalance(List<string> accounts, Dictionary<TokenContract,string> tokenInfos,out long duration)
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
                    Logger.Info($"{account} {value} balance is {balance}");
                }
                Logger.Info(
                    $"{key.ContractAddress} check {accounts.Count} user balance time: {contractDuration}ms.");
                duration += contractDuration;
            }
        }
        
        public void CheckWrapperBalance(List<string> accounts, Dictionary<TransferWrapperContract,string> tokenInfos,out long duration)
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
                    var balance = SystemToken.GetUserBalance(account, value);
                    stopwatch.Stop();
                    var checkTime = stopwatch.ElapsedMilliseconds;
                    contractDuration += checkTime;
                    Logger.Info($"{account} {value} balance is {balance}");
                }
                Logger.Info(
                    $"{SystemToken.ContractAddress} check {accounts.Count} user balance time: {contractDuration}ms.");
                duration += contractDuration;
            }
        }
        
        public void CheckWrapperVirtualBalance(Dictionary<TransferWrapperContract,string> tokenInfos,out long duration)
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
                    var balance = SystemToken.GetUserBalance(account, value);
                    stopwatch.Stop();
                    var checkTime = stopwatch.ElapsedMilliseconds;
                    contractDuration += checkTime;
                    Logger.Info($"{account} {value} balance is {balance}");
                }
                Logger.Info(
                    $"{SystemToken.ContractAddress} check {virtualAccount.Count} user balance time: {contractDuration}ms.");
                duration += contractDuration;
            }
        }
        
        public void GetBlockInfo()
        {
            var currentBlockHeight = AsyncHelper.RunSync(() => _aElfClient.GetBlockHeightAsync());
            if (currentBlockHeight - VerifyCount < 0)
                VerifyCount = currentBlockHeight;
            var startBlock = VerifyCount == currentBlockHeight ? 1 : currentBlockHeight - VerifyCount;

            Logger.Info($"Check block info start: {startBlock}, verify count: {VerifyCount}");
            long all = 0;
            for (var i = startBlock; i < VerifyCount; i++)
            {
                var i1 = i;
                var stopwatch = new Stopwatch();
                stopwatch.Start();
                var blockInfo = AsyncHelper.RunSync(() =>_aElfClient.GetBlockByHeightAsync(i1,true));
                stopwatch.Stop();
                var checkTime = stopwatch.ElapsedMilliseconds;
                
                Logger.Info($"block height: {blockInfo.Header.Height}, block hash:{blockInfo.BlockHash} time:{checkTime}ms");
                all += checkTime;
            }
            
            var req = (double)VerifyCount/all * 1000;
            Logger.Info($"Check {VerifyCount} block info use {all}ms, req: {req}/s");
        }
        
        private TokenContract SystemToken { get; }
        private readonly AElfClient _aElfClient;
        private static readonly ILog Logger = Log4NetHelper.GetLogger();
    }
}