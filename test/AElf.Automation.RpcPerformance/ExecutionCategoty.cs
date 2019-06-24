using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.Automation.Common.Helpers;
using AElf.Automation.Common.WebApi.Dto;
using AElf.Contracts.MultiToken.Messages;
using AElf.Types;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace AElf.Automation.RpcPerformance
{
    public class ExecutionCategory : IPerformanceCategory
    {
        #region Public Property

        public IApiHelper ApiHelper { get; private set; }
        private ExecutionSummary Summary { get; set; }
        private TransactionGroup Group { get; set; }
        private NodeStatusMonitor Monitor { get; set; }
        public string BaseUrl { get; }
        private List<AccountInfo> AccountList { get; }
        private string KeyStorePath { get; }
        private List<ContractInfo> ContractList { get; }
        private List<string> TxIdList { get; }
        public int ThreadCount { get; }
        public int ExeTimes { get; }
        public bool LimitTransaction { get; }
        private ConcurrentQueue<string> GenerateTransactionQueue { get; }
        private readonly ILogHelper _logger = LogHelper.GetLogHelper();

        #endregion

        public ExecutionCategory(int threadCount,
            int exeTimes,
            string baseUrl = "http://127.0.0.1:8000",
            string keyStorePath = "",
            bool limitTransaction = true)
        {
            if (keyStorePath == "")
                keyStorePath = CommonHelper.GetCurrentDataDir();

            AccountList = new List<AccountInfo>();
            ContractList = new List<ContractInfo>();
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
            var contractList = new List<object>();
            for (var i = 0; i < ThreadCount; i++)
            {
                dynamic info = new System.Dynamic.ExpandoObject();
                info.Id = i;
                info.Account = AccountList[i].Account;

                var ci = new CommandInfo(ApiMethods.DeploySmartContract)
                {
                    Parameter = $"AElf.Contracts.MultiToken {AccountList[i].Account}"
                };
                ApiHelper.ExecuteCommand(ci);
                Assert.IsTrue(ci.Result);
                var transactionResult = ci.InfoMsg as SendTransactionOutput;
                var txId = transactionResult?.TransactionId;
                Assert.IsFalse(string.IsNullOrEmpty(txId), "Transaction Id is null or empty");
                info.TxId = txId;
                info.Result = false;
                contractList.Add(info);
            }

            var count = 0;
            var checkTimes = ConfigInfoHelper.Config.Timeout;

            while (checkTimes > 0)
            {
                checkTimes--;
                Thread.Sleep(1000);
                foreach (dynamic item in contractList)
                {
                    if (item.Result != false) continue;

                    var ci = new CommandInfo(ApiMethods.GetTransactionResult) {Parameter = item.TxId};
                    ApiHelper.GetTransactionResult(ci);
                    Assert.IsTrue(ci.Result);
                    if (!(ci.InfoMsg is TransactionResultDto transactionResult)) continue;
                    var status =
                        (TransactionResultStatus) Enum.Parse(typeof(TransactionResultStatus),
                            transactionResult.Status);
                    switch (status)
                    {
                        case TransactionResultStatus.Mined:
                        {
                            count++;
                            item.Result = true;
                            var contractPath = transactionResult.ReadableReturnValue.Replace("\"", "");
                            ContractList.Add(new ContractInfo(AccountList[item.Id].Account, contractPath));
                            break;
                        }
                        case TransactionResultStatus.Failed:
                        case TransactionResultStatus.NotExisted:
                        case TransactionResultStatus.Unexecutable:
                            var message =
                                $"Transaction {item.TxId} execution status: {transactionResult.Status}." +
                                $"\r\nDetail Message: {JsonConvert.SerializeObject(transactionResult)}";
                            _logger.WriteError(message);
                            break;
                        case TransactionResultStatus.Pending:
                            _logger.WriteInfo($"Transaction {item.TxId} execution status: {transactionResult.Status}.");
                            continue;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                    Thread.Sleep(10);
                }

                if (count == contractList.Count)
                    return;
            }

            Assert.IsFalse(true, "Deployed contract not executed successfully.");
        }

        public void InitializeContracts()
        {
            //create all token
            foreach (var contract in ContractList)
            {
                var account = contract.Owner;
                var contractPath = contract.ContractPath;

                var symbol = $"ELF{CommonHelper.RandomString(4, false)}";
                contract.Symbol = symbol;
                var ci = new CommandInfo(ApiMethods.SendTransaction, account, contractPath, "Create")
                {
                    ParameterInput = new CreateInput
                    {
                        Symbol = symbol,
                        TokenName = $"elf token {symbol}",
                        TotalSupply = long.MaxValue,
                        Decimals = 2,
                        Issuer = Address.Parse(account),
                        IsBurnable = true
                    }
                };
                ApiHelper.ExecuteCommand(ci);
                Assert.IsTrue(ci.Result);
                var transactionResult = ci.InfoMsg as SendTransactionOutput;
                var transactionId = transactionResult?.TransactionId;
                Assert.IsFalse(string.IsNullOrEmpty(transactionId), "Transaction Id is null or empty");
                TxIdList.Add(transactionId);
            }

            Monitor.CheckTransactionsStatus(TxIdList);

            //issue all token
            foreach (var contract in ContractList)
            {
                var account = contract.Owner;
                var contractPath = contract.ContractPath;
                var symbol = contract.Symbol;

                var ci = new CommandInfo(ApiMethods.SendTransaction, account, contractPath, "Issue")
                {
                    ParameterInput = new IssueInput()
                    {
                        Amount = long.MaxValue,
                        Memo = $"Issue all balance to owner - {Guid.NewGuid()}",
                        Symbol = symbol,
                        To = Address.Parse(account)
                    }
                };
                ApiHelper.ExecuteCommand(ci);
                Assert.IsTrue(ci.Result);
                var transactionResult = ci.InfoMsg as SendTransactionOutput;
                var transactionId = transactionResult?.TransactionId;
                Assert.IsFalse(string.IsNullOrEmpty(transactionId), "Transaction Id is null or empty");
                TxIdList.Add(transactionId);
            }

            Monitor.CheckTransactionsStatus(TxIdList);

            //prepare conflict environment
            var conflict = ConfigInfoHelper.Config.Conflict;
            if (!conflict)
                InitializeTransactionGroup();
        }

        public void InitializeTransactionGroup()
        {
            var apiHelper = ApiHelper;
            var users = AccountList.Skip(ThreadCount).ToList();
            var contracts = ContractList;

            Group = new TransactionGroup(apiHelper, users, contracts);
            Group.InitializeAllUsersToken();
            Task.Run(() => Group.GenerateAllContractTransactions());
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
                                //multi task for SendTransactions query
                                var txsTasks = new List<Task>();
                                for (var i = 0; i < ThreadCount; i++)
                                {
                                    var j = i;
                                    txsTasks.Add(conflict
                                        ? Task.Run(() => ExecuteBatchTransactionTask(j, ExeTimes))
                                        : Task.Run(() => ExecuteNonConflictBatchTransactionTask(j, ExeTimes)));
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

        #region Private Method

        //Without conflict group category
        private void ExecuteTransactionTask(int threadNo, int times)
        {
            var account = ContractList[threadNo].Owner;
            var abiPath = ContractList[threadNo].ContractPath;

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
                var ci = new CommandInfo(ApiMethods.SendTransaction, account, abiPath, "Transfer")
                {
                    ParameterInput = new TransferInput
                    {
                        Symbol = ContractList[threadNo].Symbol,
                        Amount = (i + 1) % 4 + 1,
                        Memo = $"transfer test - {Guid.NewGuid()}",
                        To = Address.Parse(account1)
                    }
                };
                ApiHelper.ExecuteCommand(ci);

                if (ci.Result)
                {
                    var transactionResult = ci.InfoMsg as SendTransactionOutput;
                    txIdList.Add(transactionResult?.TransactionId);
                    passCount++;
                }

                Thread.Sleep(10);
            }

            _logger.WriteInfo("Total contract sent: {0}, passed number: {1}", 2 * times, passCount);
            txIdList.Reverse();
            Monitor.CheckTransactionsStatus(txIdList);
            _logger.WriteInfo("{0} Transfer from Address {1}", set.Count, account);
        }

        private void ExecuteBatchTransactionTask(int threadNo, int times)
        {
            var account = ContractList[threadNo].Owner;
            var contractPath = ContractList[threadNo].ContractPath;

            Monitor.CheckTransactionPoolStatus(LimitTransaction); //check transaction pool first

            var rawTransactions = new List<string>();
            for (var i = 0; i < times; i++)
            {
                var rd = new Random(DateTime.Now.Millisecond);
                var randNumber = rd.Next(ThreadCount, AccountList.Count);
                var countNo = randNumber;
                var account1 = AccountList[countNo].Account;

                //Execute Transfer
                var ci = new CommandInfo(ApiMethods.SendTransaction, account, contractPath, "Transfer")
                {
                    ParameterInput = new TransferInput
                    {
                        Symbol = ContractList[threadNo].Symbol,
                        To = Address.Parse(account1),
                        Amount = (i + 1) % 4 + 1,
                        Memo = $"transfer test - {Guid.NewGuid()}"
                    }
                };
                var requestInfo = ApiHelper.GenerateTransactionRawTx(ci);
                rawTransactions.Add(requestInfo);
            }

            //Send batch transaction requests
            var commandInfo = new CommandInfo(ApiMethods.SendTransactions)
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

        private void ExecuteNonConflictBatchTransactionTask(int threadNo, int times)
        {
            Monitor.CheckTransactionPoolStatus(LimitTransaction); //check transaction pool first
            var rawTransactions = Group.GetRawTransactions();

            //Send batch transaction requests
            var commandInfo = new CommandInfo(ApiMethods.SendTransactions)
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
            var account = ContractList[threadNo].Owner;
            var contractPath = ContractList[threadNo].ContractPath;

            Monitor.CheckTransactionPoolStatus(LimitTransaction);

            for (var i = 0; i < times; i++)
            {
                var rd = new Random(DateTime.Now.Millisecond);
                var randNumber = rd.Next(ThreadCount, AccountList.Count);
                var countNo = randNumber;
                var account1 = AccountList[countNo].Account;

                //Execute Transfer
                var ci = new CommandInfo(ApiMethods.SendTransaction, account, contractPath, "Transfer")
                {
                    ParameterInput = new TransferInput
                    {
                        Symbol = ContractList[threadNo].Symbol,
                        To = Address.Parse(account1),
                        Amount = (i + 1) % 4 + 1,
                        Memo = $"transfer test - {Guid.NewGuid()}"
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
                var ci = new CommandInfo(ApiMethods.SendTransaction) {Parameter = rpcMsg};
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
            if (accounts.Count >= count)
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

                var generateCount = count - accounts.Count;
                for (var i = 0; i < generateCount; i++)
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