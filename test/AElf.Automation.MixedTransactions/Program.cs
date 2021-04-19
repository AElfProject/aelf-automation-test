using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElfChain.Common.Contracts;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
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
            var wrapper = new WrapperTransferCategory();
            transfer.GetTestAccounts();
            
            check.ToAccountList = transfer.ToAccountList;
            wrapper.ToAccountList = transfer.ToAccountList;
            check.FromAccountList = transfer.FromAccountList;
            wrapper.FromAccountList = transfer.FromAccountList;

            _fromAccountInfos = transfer.FromAccountList;
            _toAccountInfos = transfer.ToAccountList;

            // Deploy contract for test 
            _tokenContractList = transfer.DeployTokenContractWithAuthority();
            _wrapperContractList = wrapper.DeployWrapperContractWithAuthority(out var tokenContract);

            // Create or Get token
            if (transfer.NeedCreateToken)
            {
                _tokenInfoList = transfer.CreateAndIssueTokenForToken(_tokenContractList);
                _wrapperInfoList = wrapper.CreateAndIssueTokenForWrapper(_wrapperContractList,tokenContract);
            }
            else
            {
                _tokenInfoList = transfer.GetTokenList(_tokenContractList);
                _wrapperInfoList = wrapper.GetTokenList(_wrapperContractList);
            }

            //Transfer prepare 

            transfer.PrepareTokenTransfer(_tokenInfoList);
            wrapper.PrepareWrapperTransfer(_wrapperInfoList,tokenContract);

            check.CheckFromBalance(_fromAccountInfos, _tokenInfoList, out long d1);
            check.CheckToBalance(_toAccountInfos, _tokenInfoList, out long d2);
            check.CheckWrapperFromBalance(_fromAccountInfos, _wrapperInfoList,tokenContract, out long d3);
            check.CheckWrapperVirtualBalance(_wrapperInfoList, tokenContract, out long d4);

            //Transfer Task
            var cts = new CancellationTokenSource();
            var token = cts.Token;
            var taskList = new List<Task>
            {
                Task.Run(() => transfer.ContinueTransfer(_tokenInfoList, cts, token), token),
                Task.Run(() => wrapper.ContinueTransfer(_wrapperInfoList, cts, token), token),
                Task.Run(() => wrapper.ContinueContractTransfer(_wrapperInfoList, cts, token), token),
                Task.Run(() => check.ContinueCheckBlock(cts,token), token),
                Task.Run(() => transfer.CheckAccountAmount(_tokenInfoList,cts,token), token),
                Task.Run(() => wrapper.CheckAccountAmount(_wrapperInfoList,tokenContract,cts,token), token),

                Task.Run(() =>
                {
                    while (true)
                    {
                        check.CheckFromBalance(_fromAccountInfos, _tokenInfoList, out long duration1);
                        check.CheckToBalance(_toAccountInfos, _tokenInfoList, out long duration2);
                        check.CheckWrapperVirtualBalance(_wrapperInfoList,tokenContract, out long duration3);
                        check.CheckWrapperBalance(_toAccountInfos, _wrapperInfoList,tokenContract, out long duration4);
                        
                        var all = duration1 + duration2 + duration3 + duration4;
                        var requests = (_fromAccountInfos.Count * (_tokenContractList.Count + _wrapperInfoList.Count)) + (_toAccountInfos.Count * (_tokenInfoList.Count + _wrapperInfoList.Count));
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
        private static List<TransferWrapperContract> _wrapperContractList;

        private static Dictionary<TokenContract, string> _tokenInfoList;
        private static Dictionary<TransferWrapperContract, string> _wrapperInfoList;

        private static List<string> _fromAccountInfos;
        private static Dictionary<int,List<string>> _toAccountInfos;
    }
}