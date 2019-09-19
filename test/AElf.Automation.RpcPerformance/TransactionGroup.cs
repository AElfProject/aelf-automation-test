using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.Automation.Common.Contracts;
using AElf.Automation.Common.Helpers;
using AElf.Automation.Common.Managers;
using AElf.Contracts.MultiToken;
using AElfChain.SDK;
using log4net;
using Volo.Abp.Threading;

namespace AElf.Automation.RpcPerformance
{
    public class TransactionGroup
    {
        private List<AccountInfo> TestUsers { get; }
        private List<ContractInfo> Contracts { get; }

        private INodeManager NodeManager { get; }

        private IApiService ApiService => NodeManager.ApiService;
        private ConcurrentQueue<List<string>> TransactionsQueue { get; }

        private static readonly ILog Logger = Log4NetHelper.GetLogger();

        private NodeStatusMonitor NodeMonitor { get; }

        public TransactionGroup(INodeManager nodeManager, List<AccountInfo> users, List<ContractInfo> contracts)
        {
            TransactionsQueue = new ConcurrentQueue<List<string>>();
            TestUsers = users;
            Contracts = contracts;
            NodeManager = nodeManager;
            NodeMonitor = new NodeStatusMonitor(nodeManager);
        }

        public void InitializeAllUsersToken()
        {
            var tasks = new List<Task>();
            foreach (var contract in Contracts)
            {
                tasks.Add(Task.Run(() =>
                {
                    var count = 0;
                    var rawTransactions = new List<string>();
                    foreach (var user in TestUsers)
                    {
                        NodeMonitor.CheckTransactionPoolStatus(true);
                        count++;
                        var rawTx = NodeManager.GenerateRawTransaction(contract.Owner, contract.ContractPath,
                            TokenMethod.Transfer.ToString(),
                            new TransferInput
                            {
                                Symbol = contract.Symbol,
                                To = AddressHelper.Base58StringToAddress(user.Account),
                                Amount = 10000,
                                Memo = $"transfer test - {Guid.NewGuid()}"
                            });
                        rawTransactions.Add(rawTx);
                        user.Balance = 10000;

                        if (count % 50 != 0) continue;
                        var rawTxs = string.Join(",", rawTransactions);
                        var transactions = AsyncHelper.RunSync(() => ApiService.SendTransactionsAsync(rawTxs));
                        NodeMonitor.CheckTransactionsStatus(transactions.ToList());
                        Logger.Info("Batch request count: {0}, passed transaction count: {1}",
                            rawTransactions.Count,
                            transactions.Length);
                        rawTransactions = new List<string>();
                    }
                }));
            }

            Task.WaitAll(tasks.ToArray());
        }

        public List<string> GetRawTransactions()
        {
            List<string> rawTransactions;
            while (!TransactionsQueue.TryDequeue(out rawTransactions))
            {
                Thread.Sleep(50);
            }

            return rawTransactions;
        }

        public void GenerateAllContractTransactions()
        {
            while (true)
            {
                if (TransactionsQueue.Count > 5)
                {
                    Thread.Sleep(100);
                    continue;
                }

                var tasks = new List<Task>();
                foreach (var contract in Contracts)
                {
                    tasks.Add(Task.Run(() => GenerateRawTransactions(contract.ContractPath, contract.Symbol)));
                }

                Task.WaitAll(tasks.ToArray());
            }
        }

        private void GenerateRawTransactions(string contractAddress, string symbol)
        {
            var round = TestUsers.Count / 50;
            for (var i = 0; i < round; i += 2)
            {
                var rawTransactions = new List<string>();
                for (var j = 0; j < 50; j++)
                {
                    AccountInfo from, to;
                    var amount = j % 10 + 1;
                    if (TestUsers[j + i * 50].Balance < 100)
                    {
                        to = TestUsers[j + i * 50];
                        from = TestUsers[j + (i + 1) * 50];
                    }
                    else
                    {
                        from = TestUsers[j + i * 50];
                        to = TestUsers[j + (i + 1) * 50];
                    }

                    from.Balance -= amount;
                    to.Balance += amount;
                    var input = new TransferInput
                    {
                        Symbol = symbol,
                        To = AddressHelper.Base58StringToAddress(to.Account),
                        Amount = (i + 1) % 4 + 1,
                        Memo = $"transfer test - {Guid.NewGuid()}"
                    };
                    var rawTx = NodeManager.GenerateRawTransaction(from.Account, contractAddress, "Transfer", input);
                    rawTransactions.Add(rawTx);
                }

                TransactionsQueue.Enqueue(rawTransactions);
            }
        }
    }
}