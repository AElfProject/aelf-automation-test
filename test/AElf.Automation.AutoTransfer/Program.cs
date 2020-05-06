using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using McMaster.Extensions.CommandLineUtils;
using Volo.Abp.Threading;

namespace AElf.Automation.AutoTransfer
{
    class Program
    {
        static int Main(string[] args)
        {
            return CommandLineApplication.Execute<Program>(args);
        }

        private Config _config;
        private NodeManager _nodeManager;
        private Random _random = new Random(DateTime.Now.Millisecond);

        private void OnExecute(CommandLineApplication app)
        {
            _config = ConfigHelper<Config>.GetConfigInfo("appsettings.json");
            var baseUrl = _config.ServiceUrl;
            var sendAccount = _config.SendAccount;
            var sendAccountPassword = _config.AccountPassword;
            //Init Logger
            var fileName = $"AutoTransfer_{DateTime.Now.Hour:00}";
            Log4NetHelper.LogInit(fileName);
            // var Logger = Log4NetHelper.GetLogger();
            _nodeManager = new NodeManager(baseUrl, CommonHelper.GetCurrentDataDir());
            // Connect
            AsyncHelper.RunSync(_nodeManager.ApiClient.GetChainStatusAsync);
            // Load all account
            var accounts = _nodeManager.ListAccounts().Select(acc =>
            {
                return acc == sendAccount
                    ? new AccountInfo(acc, sendAccountPassword)
                    : new AccountInfo(acc, string.Empty);
            }).ToList();
            UnlockAccounts(accounts);
            CheckAccountBalance(accounts);
            
            var count = 0;
            while (++count > 0)
            {
                Console.WriteLine("----Begin----");
                Console.WriteLine($"Loop batch send count: {count}{Environment.NewLine}");
                var accountTasks = new List<Task>();
                accounts.ForEach(acc =>
                {
                    accountTasks.Add(Task.Run(() =>
                    {
                        SendTransactionRandomly(accounts);
                    }));
                });
                Task.WaitAll(accountTasks.ToArray());
                Console.WriteLine($"----End----{Environment.NewLine}");
                Thread.Sleep(_random.Next(500, 2000));
            }
        }

        private void UnlockAccounts(List<AccountInfo> accounts)
        {
            accounts.ForEach(acc => { _nodeManager.UnlockAccount(acc.Account, acc.Password); });
        }

        private void CheckAccountBalance(List<AccountInfo> accounts)
        {
            var token = new ContractManager(_nodeManager, _config.SendAccount).Token;
            accounts.ForEach(acc =>
            {
                if (acc.Account != _config.SendAccount)
                {
                    var contractManager = new ContractManager(_nodeManager, acc.Account);
                    var balance = token.GetUserBalance(acc.Account);
                    if (balance < 1000000L * 100000000L)
                    {
                        var rawTransaction = _nodeManager.GenerateRawTransaction(_config.SendAccount,
                            token.ContractAddress,
                            "Transfer",
                            new TransferInput
                            {
                                To = acc.Account.ConvertAddress(),
                                Amount = 1000000L * 100000000L - balance,
                                Symbol = "ELF",
                                Memo = $"{nameof(CheckAccountBalance)} - {Guid.NewGuid()}"
                            });
                        _nodeManager.SendTransaction(rawTransaction);
                    }
                }
            });
        }

        private void SendTransactionRandomly(List<AccountInfo> accountInfos)
        {
            var token = new ContractManager(_nodeManager, _config.SendAccount).Token;
            var seed = _random.Next(0, 4);
            var sendAccount = accountInfos[seed];
            var receiveAccount = accountInfos.Where(acc => acc != sendAccount).ToList();
            Console.WriteLine($"SendAccount: {sendAccount.Account}{Environment.NewLine}");
            receiveAccount.ForEach(acc =>
            {
                var amount = 100000000L * _random.Next(1, 100); 
                var rawTransaction = _nodeManager.GenerateRawTransaction(sendAccount.Account,
                    token.ContractAddress,
                    "Transfer",
                    new TransferInput
                    {
                        To = acc.Account.ConvertAddress(),
                        Amount = amount,
                        Symbol = "ELF",
                        Memo = $"transfer test - {Guid.NewGuid()}"
                    });
                _nodeManager.SendTransaction(rawTransaction);
                Console.WriteLine($"Receive account: {acc.Account}{Environment.NewLine}" +
                                  $"Amount: {amount}");

            });
        }
    }

    public class Config
    {
        public string ServiceUrl { get; set; }
        public string SendAccount { get; set; }
        public string AccountPassword { get; set; }
    }

    public class AccountInfo
    {
        public AccountInfo(string account, string accountPassword)
        {
            Account = account;
            Password = accountPassword;
        }

        public string Account { get; }
        public string Password { get; }
    }
}