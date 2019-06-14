using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.Automation.Common.Contracts;
using AElf.Automation.Common.Helpers;
using AElf.Automation.Common.WebApi.Dto;
using AElf.Contracts.MultiToken.Messages;
using AElf.Types;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AElf.Automation.RpcPerformance
{
    public class ReadOnlyCategory : IPerformanceCategory
    {
        #region Public Property

        public IApiHelper ApiHelper { get; private set; }
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
        private readonly ILogHelper _logger = LogHelper.GetLogHelper();

        #endregion

        public ReadOnlyCategory(int threadCount,
            int exeTimes,
            string baseUrl = "http://127.0.0.1:8000",
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
            BaseUrl = baseUrl;
            LimitTransaction = limitTransaction;
        }

        public void InitExecCommand(int userCount = 200)
        {
            _logger.WriteInfo("Host Url: {0}", BaseUrl);
            _logger.WriteInfo("Key Store Path: {0}", Path.Combine(KeyStorePath, "keys"));

            ApiHelper = new WebApiHelper(BaseUrl, KeyStorePath);

            //Connect Chain
            var ci = new CommandInfo(ApiMethods.GetChainInformation);
            ApiHelper.ExecuteCommand(ci);
            Assert.IsTrue(ci.Result, "Connect chain got exception.");

            //New
            NewAccounts(userCount);

            //Unlock Account
            UnlockAllAccounts(userCount);

            //Init other services
            Summary = new ExecutionSummary(ApiHelper);
            Monitor = new NodeStatusMonitor(ApiHelper);
        }

        public void DeployContracts()
        {
        }

        public void InitializeContracts()
        {
            var callAddress = AccountList[0].Account;
            var genesisService = GenesisContract.GetGenesisContract(ApiHelper, AccountList[0].Account);
            
            //TokenService contract
            var tokenAddress = genesisService.GetContractAddressByName(NameProvider.TokenName);
            Token = new TokenContract(ApiHelper, callAddress, tokenAddress.GetFormatted());
            ContractList = new List<ContractInfo>();
            for (var i = 0; i < ThreadCount; i++)
            {
                var contract = new ContractInfo(AccountList[i].Account, Token.ContractAddress);
                ContractList.Add(contract);
            }
        }

        public void PrintContractInfo()
        {
            _logger.WriteInfo("Execution account and contract address information:");
            var count = 0;
            foreach (var item in ContractList)
            {
                count++;
                _logger.WriteInfo("{0:00}. Account: {1}, ContractAddress:{2}", count,
                    item.Owner,
                    item.ContractPath);
            }
        }

        public void ExecuteOneRoundTransactionTask()
        {
            _logger.WriteInfo("Start transaction execution at: {0}",
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
            _logger.WriteInfo("End transaction execution at: {0}, execution time span is {1}",
                DateTime.Now.ToString(CultureInfo.InvariantCulture), exec.ElapsedMilliseconds);
        }

        public void ExecuteOneRoundTransactionsTask()
        {
            _logger.WriteInfo("Start generate all requests at: {0}",
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
                        _logger.WriteInfo($"Execute batch transaction got exception, message details are: {e.Message}");
                    }
                }));
            }

            Task.WaitAll(contractTasks.ToArray<Task>());
            exec.Stop();
            _logger.WriteInfo("All requests execution completed at: {0}, execution time span: {1}",
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
                    _logger.WriteInfo("Begin generate multi requests.");
                    try
                    {
                        var stopwatch = new Stopwatch();
                        stopwatch.Start();
                        for (var r = 1; r > 0; r++) //continuous running
                        {
                            _logger.WriteInfo("Execution transaction request round: {0}", r);
                            if (useTxs)
                            {
                                //multi task for BroadcastTransactions query
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
                                //multi task for BroadcastTransaction query
                                for (var i = 0; i < ThreadCount; i++)
                                {
                                    var j = i;
                                    //Generate transaction requests
                                    GenerateRawTransactionQueue(j, ExeTimes);
                                    //Send  transaction requests
                                    _logger.WriteInfo(
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
                        _logger.WriteError(message);
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
                var ci = new CommandInfo(ApiMethods.BroadcastTransaction, AccountList[threadNo].Account, Token.ContractAddress, TokenMethod.GetBalance.ToString())
                {
                    ParameterInput = new GetBalanceInput
                    {
                        Symbol = "ELF",
                        Owner = Address.Parse(account1)
                    }
                };
                ApiHelper.ExecuteCommand(ci);

                if (ci.Result)
                {
                    var transactionResult = ci.InfoMsg as BroadcastTransactionOutput;
                    txIdList.Add(transactionResult?.TransactionId);
                    passCount++;
                }

                Thread.Sleep(10);
            }

            _logger.WriteInfo("Total contract sent: {0}, passed number: {1}", 2 * times, passCount);
            txIdList.Reverse();
            Monitor.CheckTransactionsStatus(txIdList);
        }

        private void ExecuteBatchTransactionTask(int threadNo, int times)
        {
            Monitor.CheckTransactionPoolStatus(LimitTransaction); //check transaction pool first

            var rawTransactions = new List<string>();
            for (var i = 0; i < times; i++)
            {
                var rd = new Random(DateTime.Now.Millisecond);
                var randNumber = rd.Next(ThreadCount, AccountList.Count);
                var countNo = randNumber;
                var account1 = AccountList[countNo].Account;

                //Execute Transfer
                var ci = new CommandInfo(ApiMethods.BroadcastTransaction, AccountList[threadNo].Account, Token.ContractAddress, TokenMethod.GetBalance.ToString())
                {
                    ParameterInput = new GetBalanceInput
                    {
                        Symbol = "ELF",
                        Owner = Address.Parse(account1),
                    }
                };
                var requestInfo = ApiHelper.GenerateTransactionRawTx(ci);
                rawTransactions.Add(requestInfo);
            }

            //Send batch transaction requests
            var commandInfo = new CommandInfo(ApiMethods.BroadcastTransactions)
            {
                Parameter = string.Join(",", rawTransactions)
            };
            ApiHelper.ExecuteCommand(commandInfo);
            Assert.IsTrue(commandInfo.Result);
            var transactions = (string[]) commandInfo.InfoMsg;
            _logger.WriteInfo("Batch request userCount: {0}, passed transaction userCount: {1}", rawTransactions.Count,
                transactions.Length);
            _logger.WriteInfo("Thread [{0}] completed executed {1} times contracts work.", threadNo, times);
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
                var ci = new CommandInfo(ApiMethods.BroadcastTransaction, AccountList[threadNo].Account, Token.ContractAddress, TokenMethod.GetBalance.ToString())
                {
                    ParameterInput = new GetBalanceInput
                    {
                        Symbol = "ELF",
                        Owner = Address.Parse(account1)
                    }
                };
                var requestInfo = ApiHelper.GenerateTransactionRawTx(ci);
                GenerateTransactionQueue.Enqueue(requestInfo);
            }
        }

        private void ExecuteAloneTransactionTask(int group)
        {
            while (true)
            {
                if (!GenerateTransactionQueue.TryDequeue(out var rpcMsg))
                    break;
                _logger.WriteInfo("Transaction group: {0}, execution left: {1}", group + 1,
                    GenerateTransactionQueue.Count);
                var ci = new CommandInfo(ApiMethods.BroadcastTransaction) {Parameter = rpcMsg};
                ApiHelper.ExecuteCommand(ci);
                Thread.Sleep(100);
            }
        }

        private void UnlockAllAccounts(int count)
        {
            for (var i = 0; i < count; i++)
            {
                var ci = new CommandInfo(ApiMethods.AccountUnlock)
                {
                    Parameter = $"{AccountList[i].Account} 123 notimeout"
                };
                ci = ApiHelper.ExecuteCommand(ci);
                Assert.IsTrue(ci.Result);
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
                    var ci = new CommandInfo(ApiMethods.AccountNew) {Parameter = "123"};
                    ci = ApiHelper.ExecuteCommand(ci);
                    Assert.IsTrue(ci.Result);
                    AccountList.Add(new AccountInfo(ci.InfoMsg.ToString()));
                }
            }
        }

        private List<string> GetExistAccounts()
        {
            var ci = ApiHelper.ListAccounts();
            var accounts = ci.InfoMsg as List<string>;

            return accounts;
        }

        private void TransactionSentPerSecond(int transactionCount, long milliseconds)
        {
            var tx = (float) transactionCount;
            var time = (float) milliseconds;

            var result = tx * 1000 / time;

            _logger.WriteInfo(
                $"Summary analyze: Total request {transactionCount} transactions in {time / 1000:0.000} seconds, average {result:0.00} txs/second.");
        }

        #endregion
    }
}