using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.Automation.Common.Contracts;
using AElf.Automation.Common.Helpers;
using AElf.Contracts.MultiToken.Messages;
using AElf.Types;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AElf.Automation.RpcPerformance
{
    public class TransactionGroup
    {
        private List<AccountInfo> TestUsers { get; }
        private List<ContractInfo> Contracts { get; }

        private IApiHelper ApiHelper { get; }
        private ConcurrentQueue<List<string>> TransactionsQueue { get; }

        private readonly ILogHelper _logger = LogHelper.GetLogHelper();

        private NodeStatusMonitor NodeMonitor { get; }

        public TransactionGroup(IApiHelper apiHelper, List<AccountInfo> users, List<ContractInfo> contracts)
        {
            TransactionsQueue = new ConcurrentQueue<List<string>>();
            TestUsers = users;
            Contracts = contracts;
            ApiHelper = apiHelper;
            NodeMonitor = new NodeStatusMonitor(apiHelper);
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
                        var rawTx = ApiHelper.GenerateTransactionRawTx(contract.Owner, contract.ContractPath,
                            TokenMethod.Transfer.ToString(),
                            new TransferInput
                            {
                                Symbol = contract.Symbol,
                                To = Address.Parse(user.Account),
                                Amount = 10000,
                                Memo = $"transfer test - {Guid.NewGuid()}"
                            });
                        rawTransactions.Add(rawTx);
                        user.Balance = 10000;

                        if (count % 50 != 0) continue;
                        var ci = new CommandInfo(ApiMethods.SendTransactions)
                        {
                            Parameter = string.Join(",", rawTransactions)
                        };
                        ApiHelper.ExecuteCommand(ci);
                        Assert.IsTrue(ci.Result);
                        var transactions = (string[]) ci.InfoMsg;
                        NodeMonitor.CheckTransactionsStatus(transactions.ToList());
                        _logger.WriteInfo("Batch request count: {0}, passed transaction count: {1}",
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
                    var bt = new CommandInfo(ApiMethods.SendTransaction, from.Account, contractAddress, "Transfer")
                    {
                        ParameterInput = new TransferInput
                        {
                            Symbol = symbol,
                            To = Address.Parse(to.Account),
                            Amount = (i + 1) % 4 + 1,
                            Memo = $"transfer test - {Guid.NewGuid()}"
                        }
                    };
                    var rawTx = ApiHelper.GenerateTransactionRawTx(bt);
                    rawTransactions.Add(rawTx);
                }

                TransactionsQueue.Enqueue(rawTransactions);
            }
        }
    }
}