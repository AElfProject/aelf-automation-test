using System.Collections.Generic;
using System.Diagnostics;
using AElfChain.Common.Contracts;
using AElfChain.Common.Helpers;
using log4net;

namespace AElf.Automation.AccountCheck
{
    public class CheckAction : BasicAction
    {
        public void CheckBalanceOnly(List<string> accounts, List<ContractInfo> contractInfos,out long duration)
        {
            duration = 0;
            foreach (var contractInfo in contractInfos)
            {
                long contractDuration = 0;
                var contract = new TokenContract(NodeManager,InitAccount, contractInfo.ContractAddress);
                var symbol = contractInfo.TokenSymbol;
                
                Logger.Info("Start check ...");
                
                foreach (var account in accounts) 
                {
                    var stopwatch = new Stopwatch();
                    stopwatch.Start();
                    contract.GetUserBalance(account, symbol);
                    stopwatch.Stop();
                    var checkTime = stopwatch.ElapsedMilliseconds;
                    contractDuration += checkTime;
                }

                Logger.Info(
                    $"{contractInfo.ContractAddress} check {accounts.Count} user balance time: {contractDuration}ms.");
                duration += contractDuration;
            }
        }

        public Dictionary<string, List<AccountInfo>> CheckBalance(List<string> accounts, Dictionary<TokenContract,string> tokenInfos,out long duration)
        {
            var accountTokenInfo = new Dictionary<string, List<AccountInfo>>();
            duration = 0;
            foreach (var (key, value) in tokenInfos)
            {
                var accountInfo = new List<AccountInfo>();   
                long contractDuration = 0;
                Logger.Info("Start check ...");
                foreach (var account in accounts) 
                {
                    var stopwatch = new Stopwatch();
                    stopwatch.Start();
                   var balance = key.GetUserBalance(account, value);
                   stopwatch.Stop();
                   accountInfo.Add(new AccountInfo(account,balance));
                   var checkTime = stopwatch.ElapsedMilliseconds;
                   contractDuration += checkTime;
                }
                accountTokenInfo.Add(value,accountInfo);
                Logger.Info(
                    $"{key.ContractAddress} check {accounts.Count} user balance time: {contractDuration}ms.");
                duration += contractDuration;
            }

            return accountTokenInfo;
        }
        private static readonly ILog Logger = Log4NetHelper.GetLogger();
    }
}