﻿using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AElfChain.Common.Contracts;
using AElfChain.Common.Helpers;
using log4net;
using Shouldly;

namespace AElf.Automation.AccountCheck
{
    static class Program
    {
        static void Main(string[] args)
        {
            Log4NetHelper.LogInit("AccountCheck");

            var transfer = new TransferAction();
            var check = new CheckAction();
            transfer.GetService();
            check.GetService();
            transfer.GetTestAccounts();
            check.ToAccountList = transfer.ToAccountList;
            check.FromAccountList = transfer.FromAccountList;
            check.AccountList = transfer.AccountList;

            var times = check.CheckTimes;
            var amount = check.TransferAmount;

            if (transfer.IsNeedDeploy)
            {
                _contractList = transfer.DeployContractWithAuthority();
                _tokenInfoList = transfer.CreateAndIssueToken(_contractList);
            }
            else
            {
                foreach (var contract in transfer.ContractInfos)
                {
                    _contractList = new List<TokenContract>();
                    _tokenInfoList = new Dictionary<TokenContract, string>();
                    var token = new TokenContract(transfer.NodeManager, transfer.InitAccount,
                        contract.ContractAddress);
                    _contractList.Add(token);
                    _tokenInfoList.Add(token, contract.TokenSymbol);
                }
            }

            if (check.CheckType == "account")
            {
                transfer.PrepareTransfer(_tokenInfoList);
                // original balance 
                _fromAccountInfos = check.CheckBalance(check.FromAccountList, _tokenInfoList, out long d1);
                _toAccountInfos = check.CheckBalance(check.ToAccountList, _tokenInfoList, out long d2);

                for (var i = 0; i < 10; i++)
                {
                    check.CheckBalance(check.FromAccountList, _tokenInfoList, out long fromDuration);
                    check.CheckBalance(check.ToAccountList, _tokenInfoList, out long toDuration);
                }
                transfer.Transfer(_tokenInfoList);

                long all = 0;
                while (times > 0)
                {
                    Logger.Info($"{times}");

                    //after transfer balance

                    var @from = check.CheckBalance(check.FromAccountList, _tokenInfoList, out long fromDuration);
                    Thread.Sleep(1000);
                    var to = check.CheckBalance(check.ToAccountList, _tokenInfoList, out long toDuration);
                    all = all + fromDuration + toDuration;

                    Logger.Info("Check from account balance:");
                    foreach (var (symbol, list) in _fromAccountInfos)
                    {
                        var after = from.First(a => a.Key.Equals(symbol));
                        foreach (var account in list)
                        {
                            var accountInfo = after.Value.First(a => a.Account.Equals(account.Account));
                            Logger.Info($"{account.Account}: {account.Balance}");
                            account.Balance.ShouldBe(accountInfo.Balance + amount);
                        }
                    }
                    
                    Logger.Info("Check to account balance:");
                    foreach (var (symbol, list) in _toAccountInfos)
                    {
                        var after = to.First(a => a.Key.Equals(symbol));
                        foreach (var account in list)
                        {
                            var accountInfo = after.Value.First(a => a.Account.Equals(account.Account));
                            Logger.Info($"{account.Account}: {account.Balance}");
                            account.Balance.ShouldBe(accountInfo.Balance - amount);
                        }
                    }

                    _fromAccountInfos = from;
                    _toAccountInfos = to;
                    times--;
                    Thread.Sleep(1000);
                }

                var req = (double) (check.CheckTimes * (check.FromAccountList.Count + check.ToAccountList.Count) *
                                    _tokenInfoList.Count) / all * 1000;

                Logger.Info($"all:{all}ms, 1s request {req}");
            }
            else
            {
                transfer.PrepareTransfer(_tokenInfoList);
                var list = transfer.Transfer(_tokenInfoList);
                long all = 0;

                for (var i = 0; i < 10; i++)
                    check.CheckTx(list,out long duration);

                while (times > 0)
                {
                    Logger.Info($"{times}");
                    check.CheckTx(list,out long duration);
                    Thread.Sleep(1000);
                    all += duration;
                    times--;
                    Thread.Sleep(1000);
                }
                var req = (double) (check.CheckTimes * list.Count) / all * 1000;
                Logger.Info($"all {all}ms, 1s request {req} ");
            }
        }

        private static readonly ILog Logger = Log4NetHelper.GetLogger();
        private static List<TokenContract> _contractList;
        private static Dictionary<TokenContract, string> _tokenInfoList;
        private static Dictionary<string, ConcurrentBag<AccountInfo>> _fromAccountInfos;
        private static Dictionary<string, ConcurrentBag<AccountInfo>> _toAccountInfos;
    }
}