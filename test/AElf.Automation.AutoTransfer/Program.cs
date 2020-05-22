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
        private ContractManager _contractManager;

        private void OnExecute(CommandLineApplication app)
        {
            _config = ConfigHelper<Config>.GetConfigInfo("appsettings.json");
            //Init Logger
            var fileName = $"AutoTransfer_{DateTime.Now.Hour:00}";
            Log4NetHelper.LogInit(fileName);
            // var Logger = Log4NetHelper.GetLogger();
            _nodeManager = new NodeManager(_config.ServiceUrl, CommonHelper.GetCurrentDataDir());
            // Connect
            AsyncHelper.RunSync(_nodeManager.ApiClient.GetChainStatusAsync);
            // Load all account
            var accounts = LoadAllUnlockedAccount();
            _contractManager = new ContractManager(_nodeManager, _config.SendAccount);
            CheckAccountBalance(accounts);

            var count = 0;
            while (++count > 0)
            {
                Console.WriteLine("----Begin----");
                Console.WriteLine($"Loop batch send count: {count}");
                Console.WriteLine($"Tasks count: {accounts.Count}{Environment.NewLine}");
                var accountTasks = new List<Task>();
                accounts.ForEach(acc =>
                {
                    accountTasks.Add(Task.Run(() =>
                    {
                        try
                        {
                            SendTransactionRandomly(accounts);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e.Message);
                        }
                    }));
                });
                Task.WaitAll(accountTasks.ToArray());
                Console.WriteLine($"----End----{Environment.NewLine}");
                Thread.Sleep(_random.Next(500, 2000));

                if (count % 100 == 0)
                {
                    CheckAccountBalance(accounts);
                }
            }
        }

        private List<AccountInfo> LoadAllUnlockedAccount()
        {
            var unlockAccounts = new List<AccountInfo>();
            var accounts = _nodeManager.ListAccounts().Select(acc =>
            {
                return acc == _config.SendAccount
                    ? new AccountInfo(acc, _config.AccountPassword)
                    : new AccountInfo(acc, string.Empty);
            }).ToList();

            if (accounts.All(acc => acc.Account != _config.SendAccount))
            {
                throw new Exception($"Config account: {_config.SendAccount} not found");
            }
            
            var index = 0;
            while (unlockAccounts.Count < _config.AccountCount)
            {
                var i = index++;

                if (i < accounts.Count && _nodeManager.UnlockAccount(accounts[i].Account, accounts[i].Password))
                {
                    unlockAccounts.Add(accounts[i]);
                    continue;
                }

                var newAccount = new AccountInfo(_nodeManager.NewAccount(), string.Empty);
                _nodeManager.UnlockAccount(newAccount.Account);
                unlockAccounts.Add(newAccount);
            }
            
            return unlockAccounts;
        }

        private void CheckAccountBalance(List<AccountInfo> accounts)
        {
            var token = _contractManager.Token;
            accounts.ForEach(acc =>
            {
                if (acc.Account != _config.SendAccount)
                {
                    var balance = token.GetUserBalance(acc.Account);
                    if (balance < 1_000_000L * 100_000_000L)
                    {
                        var rawTransaction = _nodeManager.GenerateRawTransaction(_config.SendAccount,
                            token.ContractAddress,
                            "Transfer",
                            new TransferInput
                            {
                                To = acc.Account.ConvertAddress(),
                                Amount = 1_000_000L * 100_000_000L - balance,
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
            var token = _contractManager.Token;
            var sendReceivePairs = new List<KeyValuePair<AccountInfo, AccountInfo>>();
            var sendCount = _random.Next(1, accountInfos.Count + 1);
            while (sendCount-- > 0)
            {
                var sendAccountIndex = _random.Next(0, accountInfos.Count);
                var receiveAccountIndex = _random.Next(0, accountInfos.Count);
                while (sendAccountIndex == receiveAccountIndex)
                {
                    receiveAccountIndex = _random.Next(0, accountInfos.Count);
                }
                sendReceivePairs.Add(new KeyValuePair<AccountInfo, AccountInfo>(accountInfos[sendAccountIndex], accountInfos[receiveAccountIndex]));
            }

            for (int i = 0; i < sendReceivePairs.Count; i++)
            {
                var amount = 100_000_000L * _random.Next(1, 100);
                var (sendAccount, receiveAccount) = sendReceivePairs[i];
                var rawTransaction = _nodeManager.GenerateRawTransaction(sendAccount.Account,
                    token.ContractAddress,
                    "Transfer",
                    new TransferInput
                    {
                        To = receiveAccount.Account.ConvertAddress(),
                        Amount = amount,
                        Symbol = "ELF",
                        Memo = $"transfer test - {Guid.NewGuid()}"
                    });
                _nodeManager.SendTransaction(rawTransaction);
                Console.WriteLine($"Tx {i + 1}/{sendReceivePairs.Count}{Environment.NewLine}" +
                                  $"Send account: {sendAccount.Account}{Environment.NewLine}" +
                                  $"Receive account: {receiveAccount.Account}{Environment.NewLine}" +
                                  $"Amount: {amount}");
            }
        }
    }
}