using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElfChain.Common.Contracts;
using AElfChain.Common.Helpers;
using log4net;

namespace AElf.Automation.MixedTransactions
{
    internal static class Program
    {
        public static void Main()
        {
            Log4NetHelper.LogInit("MixedTransaction");

            var transfer = new TransferCategory();
            var check = new CheckCategory();
            transfer.GetTestAccounts();

            check.ToAccountList = transfer.ToAccountList;
            check.FromAccountList = transfer.FromAccountList;

            _fromAccountInfos = transfer.FromAccountList;
            _toAccountInfos = transfer.ToAccountList;

            // Deploy contract for test 
            _tokenContractList = transfer.DeployTokenContractWithAuthority();
            // Create or Get token
            _tokenInfoList = transfer.NeedCreateToken
                ? transfer.CreateAndIssueTokenForToken(_tokenContractList)
                : transfer.GetTokenList(_tokenContractList);

            //Transfer prepare 

            transfer.PrepareTokenTransfer(_tokenInfoList);

            check.CheckFromBalance(_fromAccountInfos, _tokenInfoList, out long d1);
            check.CheckToBalance(_toAccountInfos, _tokenInfoList, out long d2);
            //Transfer Task
            var cts = new CancellationTokenSource();
            var token = cts.Token;
            var taskList = new List<Task>
            {
                Task.Run(() => transfer.ContinueTransfer(_tokenInfoList, cts, token), token),
                // Task.Run(() => wrapper.ContinueContractTransfer(_wrapperInfoList, cts, token), token),
                Task.Run(() => check.ContinueCheckBlock(cts, token), token),
                Task.Run(() => check.ContinueCheckTx(cts, token), token),
                Task.Run(() => transfer.CheckAccountAmount(_tokenInfoList, cts, token), token),

                Task.Run(() =>
                {
                    while (true)
                    {
                        check.CheckFromBalance(_fromAccountInfos, _tokenInfoList, out long duration1);
                        check.CheckToBalance(_toAccountInfos, _tokenInfoList, out long duration2);
                        var all = duration1 + duration2 ;
                        var requests = (_fromAccountInfos.Count * _tokenContractList.Count) +
                                       (_toAccountInfos.Count * _tokenInfoList.Count );
                        var req = (double) requests / all * 1000;
                        Logger.Info($"Check balance 1s request {req}");

                        Thread.Sleep(60000);
                    }
                }, token)
            };

            Task.WaitAll(taskList.ToArray<Task>());
        }

        private static readonly ILog Logger = Log4NetHelper.GetLogger();
        private static List<TokenContract> _tokenContractList;
        private static Dictionary<TokenContract, string> _tokenInfoList;
        private static List<string> _fromAccountInfos;
        private static List<string> _toAccountInfos;
    }
}