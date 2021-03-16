using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using AElf.Contracts.MultiToken;
using AElfChain.Common;
using AElfChain.Common.Contracts;
using AElfChain.Common.Helpers;
using log4net;

namespace AElf.Automation.AccountCheck
{
    public class CheckAction : BasicAction
    {
        public CheckAction()
        {
            GetService();
            SystemToken = ContractManager.Token;
        }

        public void CheckBalanceOnly(List<string> accounts, List<ContractInfo> contractInfos,out long duration)
        {
            duration = 0;
            foreach (var contractInfo in contractInfos)
            {
                var contract = new TokenContract(NodeManager,InitAccount, contractInfo.ContractAddress);
                var symbol = contractInfo.TokenSymbol;
                var stopwatch = new Stopwatch();
                Logger.Info("Start check ...");
                stopwatch.Start();
                foreach (var account in accounts) 
                {
                    contract.GetUserBalance(account, symbol);
                }
                stopwatch.Stop();
                var checkTime = stopwatch.ElapsedMilliseconds;
                Logger.Info(
                    $"{contractInfo.ContractAddress} check {accounts.Count} user balance time: {checkTime}ms.");
                duration += checkTime;
            }
        }

        public Dictionary<string, List<AccountInfo>> CheckBalance(List<string> accounts, Dictionary<TokenContract,string> tokenInfos,out long duration)
        {
            var accountTokenInfo = new Dictionary<string, List<AccountInfo>>();
            duration = 0;
            foreach (var (key, value) in tokenInfos)
            {
                var accountInfo = new List<AccountInfo>();                
                var stopwatch = new Stopwatch();
                Logger.Info("Start check ...");
                stopwatch.Start();
                foreach (var account in accounts) 
                {
                   var balance = key.GetUserBalance(account, value);
                   accountInfo.Add(new AccountInfo(account,balance));
                }
                stopwatch.Stop();
                var checkTime = stopwatch.ElapsedMilliseconds;
                accountTokenInfo.Add(value,accountInfo);
                Logger.Info(
                    $"{key.ContractAddress} check {accounts.Count} user balance time: {checkTime}ms.");
                duration += checkTime;
            }

            return accountTokenInfo;
        }

        private TokenContract SystemToken { get; }
        private static readonly ILog Logger = Log4NetHelper.GetLogger();
    }
}