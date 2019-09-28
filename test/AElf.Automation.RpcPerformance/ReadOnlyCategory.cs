using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.Automation.Common;
using AElf.Automation.Common.Contracts;
using AElf.Automation.Common.Helpers;
using AElf.Automation.Common.Managers;
using AElf.Contracts.MultiToken;
using log4net;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Volo.Abp.Threading;

namespace AElf.Automation.RpcPerformance
{
    public class ReadOnlyCategory : IPerformanceCategory
    {
        #region Public Property

        public INodeManager NodeManager { get; private set; }
        private ExecutionSummary Summary { get; set; }

        private TransactionGroup Group { get; set; }

        private NodeStatusMonitor Monitor { get; set; }
        public string BaseUrl { get; }
        private List<AccountInfo> AccountList { get; }
        private string KeyStorePath { get; }
        private TokenContract Token { get; set; }
        private List<ContractInfo> ContractList { get; set; }
        private List<string> TxIdList { get; }
        public int ThreadCount { get; }
        public int ExeTimes { get; }
        public bool LimitTransaction { get; }
        private ConcurrentQueue<string> GenerateTransactionQueue { get; }
        private static readonly ILog Logger = Log4NetHelper.GetLogger();

        #endregion

        public ReadOnlyCategory(int threadCount,
            int exeTimes,
            string baseUrl,
            string keyStorePath = "",
            bool limitTransaction = true)
        {
            if (keyStorePath == "")
                keyStorePath = CommonHelper.GetCurrentDataDir();

            AccountList = new List<AccountInfo>();
            GenerateTransactionQueue = new ConcurrentQueue<string>();
            TxIdList = new List<string>();
            ThreadCount = threadCount;
            ExeTimes = exeTimes;
            KeyStorePath = keyStorePath;
            BaseUrl = baseUrl.Contains("http://") ? baseUrl : $"http://{baseUrl}";
            LimitTransaction = limitTransaction;
        }

        public void InitExecCommand(int userCount = 200)
        {
            Logger.Info("Host Url: {0}", BaseUrl);
            Logger.Info("Key Store Path: {0}", Path.Combine(KeyStorePath, "keys"));

            NodeManager = new NodeManager(BaseUrl, KeyStorePath);

            //Connect Chain
            AsyncHelper.RunSync(NodeManager.ApiService.GetChainStatusAsync);

            //New
            NewAccounts(userCount);

            //Unlock Account
            UnlockAllAccounts(userCount);

            //Init other services
            Summary = new ExecutionSummary(NodeManager);
            Monitor = new NodeStatusMonitor(NodeManager);
        }

        public void DeployContractsWithAuthority()
        {
        }

        public void SideChainDeployContractsWithAuthority()
        {
        }

        public void DeployContracts()
        {
        }

        public void InitializeContracts()
        {
            var callAddress = AccountList[0].Account;
            var genesisService = GenesisContract.GetGenesisContract(NodeManager, AccountList[0].Account);

            //TokenService contract
            var tokenAddress = genesisService.GetContractAddressByName(NameProvider.Token);
            Token = new TokenContract(NodeManager, callAddress, tokenAddress.GetFormatted());
            ContractList = new List<ContractInfo>();
            for (var i = 0; i < ThreadCount; i++)
            {
                var contract = new ContractInfo(AccountList[i].Account, Token.ContractAddress);
                ContractList.Add(contract);
            }
        }

        public void PrintContractInfo()
        {
            Logger.Info("Execution account and contract address information:");
            var count = 0;
            foreach (var item in ContractList)
            {
                count++;
                Logger.Info("{0:00}. Account: {1}, Contract: {2}", count,
                    item.Owner,
                    item.ContractPath);
            }
        }

        public void ExecuteOneRoundTransactionTask()
        {
            Logger.Info("Start transaction execution at: {0}",
                DateTime.Now.ToString(CultureInfo.InvariantCulture));
            var exec = new Stopwatch();
            exec.Start();
            var contractTasks = new List<Task>();
            for (var i = 0; i < ThreadCount; i++)
            {
                var j = i;
                contractTasks.Add(Task.Run(() => ExecuteTransactionTask(j, ExeTimes)));
            }

            Task.WaitAll(contractTasks.ToArray<Task>());

            exec.Stop();
            Logger.Info("End transaction execution at: {0}, execution time span is {1}",
                DateTime.Now.ToString(CultureInfo.InvariantCulture), exec.ElapsedMilliseconds);
        }

        public void ExecuteOneRoundTransactionsTask()
        {
            Logger.Info("Start generate all requests at: {0}",
                DateTime.Now.ToString(CultureInfo.InvariantCulture));
            var exec = new Stopwatch();
            exec.Start();
            var contractTasks = new List<Task>();
            for (var i = 0; i < ThreadCount; i++)
            {
                var j = i;
                contractTasks.Add(Task.Run(() =>
                {
                    try
                    {
                        ExecuteBatchTransactionTask(j, ExeTimes);
                    }
                    catch (Exception e)
                    {
                        Logger.Info($"Execute batch transaction got exception, message details are: {e.Message}");
                    }
                }));
            }

            Task.WaitAll(contractTasks.ToArray<Task>());
            exec.Stop();
            Logger.Info("All requests execution completed at: {0}, execution time span: {1}",
                DateTime.Now.ToString(CultureInfo.InvariantCulture), exec.ElapsedMilliseconds);
        }

        public void ExecuteContinuousRoundsTransactionsTask(bool useTxs = false, bool conflict = true)
        {
            //add transaction performance check process
            var taskList = new List<Task>
            {
                Task.Run(() => { Summary.ContinuousCheckTransactionPerformance(); }),
                Task.Run(() =>
                {
                    Logger.Info("Begin generate multi requests.");
                    try
                    {
                        var stopwatch = new Stopwatch();
                        stopwatch.Start();
                        for (var r = 1; r > 0; r++) //continuous running
                        {
                            Logger.Info("Execution transaction request round: {0}", r);
                            if (useTxs)
                            {
                                //multi task for SendTransactions query
                                var txsTasks = new List<Task>();
                                for (var i = 0; i < ThreadCount; i++)
                                {
                                    var j = i;
                                    txsTasks.Add(Task.Run(() => ExecuteBatchTransactionTask(j, ExeTimes)));
                                }

                                Task.WaitAll(txsTasks.ToArray<Task>());
                            }
                            else
                            {
                                //multi task for SendTransaction query
                                for (var i = 0; i < ThreadCount; i++)
                                {
                                    var j = i;
                                    //Generate transaction requests
                                    GenerateRawTransactionQueue(j, ExeTimes);
                                    //Send  transaction requests
                                    Logger.Info(
                                        $"Begin execute group {j + 1} transactions with {ThreadCount} threads.");
                                    var txTasks = new List<Task>();
                                    for (var k = 0; k < ThreadCount; k++)
                                    {
                                        txTasks.Add(Task.Run(() => ExecuteAloneTransactionTask(j)));
                                    }

                                    Task.WaitAll(txTasks.ToArray<Task>());
                                }
                            }

                            if (r % 3 != 0) continue;

                            Monitor.CheckNodeHeightStatus();

                            stopwatch.Stop();
                            TransactionSentPerSecond(ThreadCount * ExeTimes * 3, stopwatch.ElapsedMilliseconds);

                            stopwatch = new Stopwatch();
                            stopwatch.Start();
                        }
                    }
                    catch (Exception e)
                    {
                        var message = "Execute continuous transaction got exception." +
                                      $"\r\nMessage: {e.Message}" +
                                      $"\r\nStackTrace: {e.StackTrace}";
                        Logger.Error(message);
                    }
                })
            };

            Task.WaitAll(taskList.ToArray<Task>());
        }

        #region Private Method

        //Without conflict group category
        private void ExecuteTransactionTask(int threadNo, int times)
        {
            var set = new HashSet<int>();
            var txIdList = new List<string>();
            var passCount = 0;
            for (var i = 0; i < times; i++)
            {
                var rd = new Random(DateTime.Now.Millisecond);
                var randNumber = rd.Next(ThreadCount, AccountList.Count);
                var countNo = randNumber;
                set.Add(countNo);
                var account1 = AccountList[countNo].Account;

                //Execute Transfer
                var getBalanceInput = new GetBalanceInput
                {
                    Symbol = NodeOption.NativeTokenSymbol,
                    Owner = AddressHelper.Base58StringToAddress(account1)
                };
                var transactionId = NodeManager.SendTransaction(AccountList[threadNo].Account,
                    Token.ContractAddress, TokenMethod.GetBalance.ToString(), getBalanceInput);
                txIdList.Add(transactionId);
                passCount++;

                Thread.Sleep(10);
            }

            Logger.Info("Total contract sent: {0}, passed number: {1}", 2 * times, passCount);
            txIdList.Reverse();
            Monitor.CheckTransactionsStatus(txIdList);
        }

        private void ExecuteBatchTransactionTask(int threadNo, int times)
        {
            Monitor.CheckTransactionPoolStatus(LimitTransaction); //check transaction pool first

            var rawTransactionList = new List<string>();
            for (var i = 0; i < times; i++)
            {
                var rd = new Random(DateTime.Now.Millisecond);
                var randNumber = rd.Next(ThreadCount, AccountList.Count);
                var countNo = randNumber;
                var account1 = AccountList[countNo].Account;

                //Execute Transfer
                var getBalanceInput = new GetBalanceInput
                {
                    Symbol = NodeOption.NativeTokenSymbol,
                    Owner = AddressHelper.Base58StringToAddress(account1),
                };
                var requestInfo = NodeManager.GenerateRawTransaction(AccountList[threadNo].Account,
                    Token.ContractAddress,
                    TokenMethod.GetBalance.ToString(), getBalanceInput);
                rawTransactionList.Add(requestInfo);
            }

            //Send batch transaction requests
            var rawTransactions = string.Join(",", rawTransactionList);
            var transactions = NodeManager.SendTransactions(rawTransactions);
            Logger.Info("Batch request userCount: {0}, passed transaction userCount: {1}", rawTransactionList.Count,
                transactions.Count);
            Logger.Info("Thread [{0}] completed executed {1} times contracts work.", threadNo, times);
            Thread.Sleep(50);
        }

        private void GenerateRawTransactionQueue(int threadNo, int times)
        {
            Monitor.CheckTransactionPoolStatus(LimitTransaction);

            for (var i = 0; i < times; i++)
            {
                var rd = new Random(DateTime.Now.Millisecond);
                var randNumber = rd.Next(ThreadCount, AccountList.Count);
                var countNo = randNumber;
                var account1 = AccountList[countNo].Account;

                //Execute Transfer
                var getBalanceInput = new GetBalanceInput
                {
                    Symbol = NodeOption.NativeTokenSymbol,
                    Owner = AddressHelper.Base58StringToAddress(account1)
                };
                var requestInfo = NodeManager.GenerateRawTransaction(AccountList[threadNo].Account,
                    Token.ContractAddress, TokenMethod.GetBalance.ToString(), getBalanceInput);
                GenerateTransactionQueue.Enqueue(requestInfo);
            }
        }

        private void ExecuteAloneTransactionTask(int group)
        {
            while (true)
            {
                if (!GenerateTransactionQueue.TryDequeue(out var rawTransaction))
                    break;
                Logger.Info("Transaction group: {0}, execution left: {1}", group + 1,
                    GenerateTransactionQueue.Count);
                NodeManager.SendTransaction(rawTransaction);
                Thread.Sleep(100);
            }
        }

        private void UnlockAllAccounts(int count)
        {
            for (var i = 0; i < count; i++)
            {
                var result = NodeManager.UnlockAccount(AccountList[i].Account);
                Assert.IsTrue(result);
            }
        }

        private void NewAccounts(int count)
        {
            var accounts = GetExistAccounts();
            if (accounts.Count > count)
            {
                foreach (var acc in accounts.Take(count))
                {
                    AccountList.Add(new AccountInfo(acc));
                }
            }
            else
            {
                foreach (var acc in accounts)
                {
                    AccountList.Add(new AccountInfo(acc));
                }

                for (var i = 0; i < count - accounts.Count; i++)
                {
                    var account = NodeManager.NewAccount();
                    AccountList.Add(new AccountInfo(account));
                }
            }
        }

        private List<string> GetExistAccounts()
        {
            return NodeManager.ListAccounts();
        }

        private void TransactionSentPerSecond(int transactionCount, long milliseconds)
        {
            var tx = (float) transactionCount;
            var time = (float) milliseconds;

            var result = tx * 1000 / time;

            Logger.Info(
                $"Summary analyze: Total request {transactionCount} transactions in {time / 1000:0.000} seconds, average {result:0.00} txs/second.");
        }

        #endregion
    }
}