using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using AElfChain.Common.Contracts;
using AElfChain.Common.Helpers;
using log4net;
using Volo.Abp.Threading;

namespace AElf.Automation.AccountCheck
{
    public class CheckAction : BasicAction
    {
        public void CheckBalanceOnly(ConcurrentBag<string> accounts, Dictionary<TokenContract,string> tokenInfos,out long duration)
        {
            duration = 0;
            foreach (var (key,value) in tokenInfos)
            {
                var contract = new TokenContract(NodeManager,InitAccount, key.ContractAddress);
                var symbol = value;
                
                Logger.Info("Start check ...");
                var stopwatch = new Stopwatch();
                stopwatch.Start();

                Parallel.ForEach(accounts, item =>
                {
                    var balance = contract.GetUserBalance(item, symbol);
                    Logger.Info($"{item},{balance}");
                });
                stopwatch.Stop();
                var checkTime = stopwatch.ElapsedMilliseconds;

                Logger.Info(
                    $"{key.ContractAddress} check {accounts.Count} user balance time: {checkTime}ms.");
                duration += checkTime;
            }
        }

        public Dictionary<string, ConcurrentBag<AccountInfo>> CheckBalance(ConcurrentBag<string> accounts, Dictionary<TokenContract,string> tokenInfos,out long duration)
        {
            var accountTokenInfo = new Dictionary<string, ConcurrentBag<AccountInfo>>();
            duration = 0;
            foreach (var (key, value) in tokenInfos)
            {
                var accountInfo = new ConcurrentBag<AccountInfo>();   
                Logger.Info("Start check ...");
                var stopwatch = new Stopwatch();
                stopwatch.Start();

                Parallel.ForEach(accounts, item =>
                {
                    var balance = key.GetUserBalance(item, value);
                        accountInfo.Add(new AccountInfo(item,balance));
                });
                
                stopwatch.Stop();
                var checkTime = stopwatch.ElapsedMilliseconds;
                accountTokenInfo.Add(value,accountInfo);
                Logger.Info(
                    $"{key.ContractAddress} check {accounts.Count} user balance time: {checkTime}ms.");
                duration += checkTime;
            }

            return accountTokenInfo;
        }

        public void CheckTx(List<string> txList, out long checkTime)
        {
            Logger.Info("Start check tx...");
            var txInfo = new ConcurrentDictionary<string,string>();

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            Parallel.ForEach(txList, item =>
            {
                var txResult = AsyncHelper.RunSync(() =>NodeManager.ApiClient.GetTransactionResultAsync(item));
                Logger.Info($"{item}:{txResult.Status}");
            });
                
            stopwatch.Stop();
            checkTime = stopwatch.ElapsedMilliseconds;
           
            Logger.Info(
                $"check {txList.Count} time: {checkTime}ms.");
        }
        
        private static readonly ILog Logger = Log4NetHelper.GetLogger();
    }
}