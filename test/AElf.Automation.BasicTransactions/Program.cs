using System;
using AElfChain.Common.Helpers;
using log4net;

namespace AElf.Automation.BasicTransaction
{
    class Program
    {
        static void Main()
        {
            Log4NetHelper.LogInit("BasicTransaction");

            var tx = new TransactionAction();
            var token = tx.DeployTokenContract(TestMode.UserTransfer);
            var wrapper = tx.DeployWrapperContract(token);
            var symbol = tx.CreateAndIssueTokenForWrapper(token.Contract);
            Logger.Info($"Symbol: {symbol}");

            var execMode = ConfigInfo.ReadInformation.ExecuteMode;
            var times = ConfigInfo.ReadInformation.Times;
            var count = ConfigInfo.ReadInformation.ContractCount;
            
            var tm = (TestMode) execMode;
            long all = 0;
            double req;

            switch (tm)
            {
                case TestMode.UserTransfer:
                    Logger.Info(
                        $"Start basic account transfer, from account: {tx.InitAccount}, to account: {tx.TestAccount}, times: {times}");
                    for (var i = 0; i < times; i++)
                    {
                        var duration = tx.TransferFromAccount(token, symbol);
                        all += duration;
                    }

                    req = (double) times / all * 1000;

                    Logger.Info($"User transfer {times} times use {all}ms, req: {req}/s, time: {all / times}ms");
                    break;
                case TestMode.ContractTransfer:
                    Logger.Info(
                        $"Start contract transfer, from account: {wrapper.ContractAddress}, to account: {tx.TestAccount}， times: {times}");
                    for (var i = 0; i < times; i++)
                    {
                        var duration = tx.TransferFromContract(token, wrapper, symbol);
                        all += duration;
                    }

                    req = (double) times / all * 1000;
                    Logger.Info($"Contract transfer {times} times use {all}ms, req: {req}/s, time: {all / times}ms");
                    break;
                case TestMode.RandomContractTransfer:
                    long total = 0;
                    for (var i = 0; i < count; i++)
                    {
                        all = 0;
                        var otherToken = tx.DeployTokenContract(TestMode.RandomContractTransfer);
                        var otherSymbol = tx.CreateAndIssueTokenForWrapper(otherToken.Contract);
                        Logger.Info($"Start random contract transfer, contract: {otherToken.ContractAddress}");
                        for (var j = 0; j < times; j++)
                        {
                            var duration = tx.TransferFromAccount(otherToken, otherSymbol);
                            all += duration;
                        }

                        total += all;
                        req = (double) times / all * 1000;
                        Logger.Info(
                            $"Random contract transfer {times} times use {all}ms, req: {req}/s, time: {all / times}ms");
                    }

                    Logger.Info(
                        $"Random contract transfer {times * count} times use {total}ms, " +
                        $"req: {(double) times * count / total * 1000}/s, " +
                        $"time: {total / (times * count)}ms");
                    
                    break;
                case TestMode.VirtualTransfer:
                    Logger.Info("Start virtual account transfer: ");
                    tx.TransferFromVirtual(token, wrapper, symbol);
                    break;
                case TestMode.CheckUserBalance:
                    Logger.Info("Start check user balance: ");
                    for (var i = 0; i < times; i++)
                    {
                        var duration = tx.CheckAccountBalance(token, symbol);
                        all += duration;
                    }

                    req = (double) (times * 4) / all * 1000;
                    Logger.Info(
                        $"Check balance {times * 4} times use {all}ms, req: {req}/s, time: {all / (times * 4)}ms");
                    break;
                case TestMode.CheckUserInfo:
                    all = tx.CheckAccount(token, symbol);
                    req = (double) times / all * 1000;
                    Logger.Info(
                        $"Check  {times} account use {all}ms, req: {req}/s, time: {all /times}ms");
                    break;
                case TestMode.CheckContractBalance:
                    Logger.Info("Start check contract balance: ");
                    for (var i = 0; i < times; i++)
                    {
                        var duration = tx.CheckContract(token, wrapper, symbol);
                        all += duration;
                    }

                    req = (double) (times * 4) / all * 1000;
                    Logger.Info(
                        $"Check balance {times * 4} times use {all}ms, req: {req}/s, time: {all / (times * 4)}ms");
                    break;
                case TestMode.CheckBlockInfo:
                    Logger.Info("Start check block info:");
                    all = tx.CheckBlockHeight(times);
                    req = (double) times / all * 1000;
                    Logger.Info($"Check block {times} times use {all}ms, req: {req}/s, time: {all / times}ms");
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private static readonly ILog Logger = Log4NetHelper.GetLogger();
    }
}