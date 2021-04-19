using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using AElfChain.Common.Contracts;
using AElfChain.Common.Helpers;
using log4net;

namespace AElf.Automation.AccountCheck
{
    public class CheckAction : BasicAction
    {
        public void CheckBalanceOnly(ConcurrentBag<string> accounts, List<ContractInfo> contractInfos,out long duration)
        {
            duration = 0;
            foreach (var contractInfo in contractInfos)
            {
                var contract = new TokenContract(NodeManager,InitAccount, contractInfo.ContractAddress);
                var symbol = contractInfo.TokenSymbol;
                
                Logger.Info("Start check ...");
                var stopwatch = new Stopwatch();
                stopwatch.Start();

                Parallel.ForEach(accounts, item =>
                {
                    contract.GetUserBalance(item, symbol);
                });
                stopwatch.Stop();
                var checkTime = stopwatch.ElapsedMilliseconds;

                Logger.Info(
                    $"{contractInfo.ContractAddress} check {accounts.Count} user balance time: {checkTime}ms.");
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
        private static readonly ILog Logger = Log4NetHelper.GetLogger();
    }
}