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
            var token = tx.DeployTokenContract();
            var wrapper = tx.DeployWrapperContract(token);
            var symbol = tx.CreateAndIssueTokenForWrapper(token.Contract);
            Logger.Info($"Symbol: {symbol}");

            Console.Write("Press <Enter> to test basic account transfer... ");
            if (Console.ReadKey().Key == ConsoleKey.Enter)
            {
                Logger.Info(
                    $"Start basic account transfer, from account: {tx.InitAccount}, to account: {tx.TestAccount}");
                tx.TransferFromAccount(token, symbol);
            }

            Console.Write("Press <Enter> to test contract transfer... ");
            if (Console.ReadKey().Key == ConsoleKey.Enter)
            {
                Logger.Info(
                    $"Start contract transfer, from account: {wrapper.ContractAddress}, to account: {tx.TestAccount}");
                tx.TransferFromContract(token, wrapper, symbol);
            }

            Console.Write("Press <Enter> to test random contract transfer... ");
            if (Console.ReadKey().Key == ConsoleKey.Enter)
            {
                Logger.Info($"Start random contract transfer: ");
                var otherToken = tx.DeployTokenContract();
                var otherSymbol = tx.CreateAndIssueTokenForWrapper(otherToken.Contract);
                tx.TransferFromAccount(otherToken, otherSymbol);
            }

            Console.Write("Press <Enter> to test virtual account... ");
            if (Console.ReadKey().Key != ConsoleKey.Enter) return;
            Logger.Info("Start virtual account transfer: ");
            tx.TransferFromVirtual(token, wrapper, symbol);
        }

        private static readonly ILog Logger = Log4NetHelper.GetLogger();
    }
}