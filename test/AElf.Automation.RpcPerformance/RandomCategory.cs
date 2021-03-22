using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AElf.Client.Dto;
using AElf.Client.Service;
using AElf.Contracts.MultiToken;
using AElf.Types;
using AElfChain.Common;
using AElfChain.Common.Contracts;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using Google.Protobuf;
using log4net;
using Shouldly;
using Volo.Abp.Threading;

namespace AElf.Automation.RpcPerformance
{
    public class RandomCategory : IPerformanceCategory
    {
        public RandomCategory(int threadCount,
            int exeTimes,
            string baseUrl,
            string keyStorePath = "",
            bool limitTransaction = true)
        {
            if (keyStorePath == "")
                keyStorePath = CommonHelper.GetCurrentDataDir();

            AccountList = new List<AccountInfo>();
            ToAccountList = new List<AccountInfo>();
            FromAccountList = new List<AccountInfo>();
            ContractList = new List<ContractInfo>();
            MainContractList = new List<ContractInfo>();
            GenerateTransactionQueue = new ConcurrentQueue<string>();
            TxIdList = new List<string>();

            ThreadCount = threadCount;
            ExeTimes = exeTimes;
            KeyStorePath = keyStorePath;
            BaseUrl = baseUrl.Contains("http://") ? baseUrl : $"http://{baseUrl}";
            LimitTransaction = limitTransaction;
        }

        public void InitExecCommand(int userCount)
        {
            Logger.Info("Host Url: {0}", BaseUrl);
            Logger.Info("Key Store Path: {0}", Path.Combine(KeyStorePath, "keys"));
            NodeManager = new NodeManager(BaseUrl, KeyStorePath);
            AuthorityManager = new AuthorityManager(NodeManager);
            //Connect
            AsyncHelper.RunSync(ApiClient.GetChainStatusAsync);
            //New
            GetTestAccounts(userCount);
            //Unlock Account
            UnlockAllAccounts(ThreadCount);
            Task.Run(() => UnlockAllAccounts(userCount));
            //Init other services
            Summary = new ExecutionSummary(NodeManager);
            Monitor = new NodeStatusMonitor(NodeManager);
        }

        public void DeployContracts()
        {
           var address = AuthorityManager.DeployContract(AccountList.First().Account, "AElf.Contracts.MultiToken");
           TokenAddress = address.ToBase58();
           TokenMonitor = new TesterTokenMonitor(NodeManager,TokenAddress);
        }

        public void InitializeMainContracts()
        {
            var bps = NodeInfoHelper.Config.Nodes;
            //create all token
            for (var i = 0; i < ThreadCount; i++)
            {
                var contract = new ContractInfo(AccountList[i].Account, TokenAddress);
                var account = contract.Owner;
                var contractPath = contract.ContractAddress;
                var symbol = TokenMonitor.GenerateNotExistTokenSymbol();
                contract.Symbol = symbol;

                ContractList.Add(contract);

                var token = new TokenContract(NodeManager, account, contractPath);
                var balance = token.GetUserBalance(account);
                if (balance < 10000_00000000)
                    token.SetAccount(bps.First().Account);
                var transactionId = token.ExecuteMethodWithTxId(TokenMethod.Create, new CreateInput
                {
                    Symbol = symbol,
                    TokenName = $"elf token {symbol}",
                    TotalSupply = 10_0000_0000_00000000L,
                    Decimals = 8,
                    Issuer = account.ConvertAddress(),
                    IsBurnable = true
                });
                TxIdList.Add(transactionId);
            }

            Monitor.CheckTransactionsStatus(TxIdList);

            //issue all token
            var amount = 10_0000_0000_00000000L / FromAccountList.Count;
            foreach (var contract in ContractList)
            {
                var account = contract.Owner;
                var contractPath = contract.ContractAddress;
                var symbol = contract.Symbol;
                var token = new TokenContract(NodeManager, account, contractPath);
                foreach (var user in FromAccountList)
                {
                    var transactionId = token.ExecuteMethodWithTxId(TokenMethod.Issue, new IssueInput
                    {
                        Amount = amount,
                        Memo = $"Issue balance - {Guid.NewGuid()}",
                        Symbol = symbol,
                        To = user.Account.ConvertAddress()
                    });
                    TxIdList.Add(transactionId);
                }
            }

            Monitor.CheckTransactionsStatus(TxIdList);

            //check user token randomly
            foreach (var contract in ContractList)
            {
                var contractPath = contract.ContractAddress;
                var symbol = contract.Symbol; 
                var token = new TokenContract(NodeManager, AccountList.First().Account, contractPath);
                foreach (var user in FromAccountList)
                {
                    var rd = CommonHelper.GenerateRandomNumber(1, 6);
                    if (rd != 5) continue;
                    //verify token
                    var balance = token.GetUserBalance(user.Account, symbol);
                    if (balance == amount)
                        Logger.Info($"Issue token {symbol} to '{user.Account}' with amount {amount} success.");
                    else if (balance == 0)
                        Logger.Warn($"User '{user.Account}' without any {symbol} token.");
                    else
                        Logger.Error($"User {user.Account} {symbol} token balance not correct.");
                }
            }
        }

        public void PrintContractInfo()
        {
            Logger.Info("Execution account and contract address information:");
            var count = 0;
            foreach (var item in ContractList)
            {
                count++;
                Logger.Info("{0:00}. Account: {1}, Contract:{2}", count,
                    item.Owner,
                    item.ContractAddress);
            }
        }
        
        public void ExecuteContinuousRoundsTransactionsTask(bool useTxs = false)
        {
            //add transaction performance check process
            var testers = AccountList.Select(o => o.Account).ToList();
            var cts = new CancellationTokenSource();
            var token = cts.Token;
            var taskList = new List<Task>
            {
                Task.Run(() => Summary.ContinuousCheckTransactionPerformance(token), token),
                Task.Run(() => GeneratedTransaction(useTxs, cts, token), token),
            };

            Task.WaitAll(taskList.ToArray<Task>());
        }

        private void GetTestAccounts(int count)
        {
            var authority = new AuthorityManager(NodeManager);
            var miners = authority.GetCurrentMiners();
            var accounts = NodeManager.ListAccounts();
            var testUsers = accounts.FindAll(o => !miners.Contains(o));
            if (testUsers.Count >= count)
            {
                foreach (var acc in testUsers.Take(count)) AccountList.Add(new AccountInfo(acc));
            }
            else
            {
                foreach (var acc in testUsers) AccountList.Add(new AccountInfo(acc));

                var generateCount = count - testUsers.Count;
                for (var i = 0; i < generateCount; i++)
                {
                    var account = NodeManager.NewAccount();
                    AccountList.Add(new AccountInfo(account));
                }
            }

            FromAccountList = AccountList.GetRange(0, count / 2);
            ToAccountList = AccountList.GetRange(count / 2 , count / 2);
        }

        private void GeneratedTransaction(bool useTxs, CancellationTokenSource cts, CancellationToken token)
        {
            Logger.Info("Begin generate multi requests.");
            var enableRandom = RpcConfig.ReadInformation.EnableRandomTransaction;
            try
            {
                for (var r = 1; r > 0; r++) //continuous running
                {
                    var stopwatch = Stopwatch.StartNew();
                    //set random tx sending each round
                    var exeTimes = GetRandomTransactionTimes(enableRandom, ExeTimes);
                    try
                    {
                        Logger.Info("Execution transaction request round: {0}", r);
                        if (useTxs)
                        {
                            //multi task for SendTransactions query
                            var txsTasks = new List<Dictionary<string, List<string>>>();
                            for (var i = 0; i < ThreadCount; i++)
                            {
                                var j = i;
                                txsTasks.Add(Task.Run(() => ExecuteBatchTransactionTask(j, exeTimes), token).Result);
                            }
                        }
                        else
                        {
                            //multi task for SendTransaction query
                            for (var i = 0; i < ThreadCount; i++)
                            {
                                var j = i;
                                GenerateRawTransactionQueue(j, exeTimes);
                                Logger.Info(
                                    $"Begin execute group {j + 1} transactions with {ThreadCount} threads.");
                                var txTasks = new List<Task>();
                                for (var k = 0; k < ThreadCount; k++)
                                    txTasks.Add(Task.Run(() => ExecuteAloneTransactionTask(j), token));

                                Task.WaitAll(txTasks.ToArray<Task>());
                            }
                        }
                    }
                    catch (AggregateException exception)
                    {
                        Logger.Error($"Request to {NodeManager.GetApiUrl()} got exception, {exception}");
                    }
                    catch (Exception e)
                    {
                        var message = "Execute continuous transaction got exception." +
                                      $"\r\nMessage: {e.Message}" +
                                      $"\r\nStackTrace: {e.StackTrace}";
                        Logger.Error(message);
                    }

                    stopwatch.Stop();
                    TransactionSentPerSecond(ThreadCount * exeTimes, stopwatch.ElapsedMilliseconds);

                    Monitor.CheckNodeHeightStatus(); //random mode, don't check node height
                }
            }
            catch (Exception e)
            {
                Logger.Error(e.Message);
                Logger.Error("Cancel all tasks due to transaction execution exception.");
                cts.Cancel(); //cancel all tasks
            }
        }

        private void UnlockAllAccounts(int count)
        {
            /*
            Parallel.For(0, count, i =>
            {
                var result = FromNoeNodeManager.UnlockAccount(AccountList[i].Account);
                if (!result)
                    throw new Exception($"Account unlock {AccountList[i].Account} failed.");
            });
            */
            for (var i = 0; i < count; i++)
            {
                var result = NodeManager.UnlockAccount(AccountList[i].Account);
                if (!result)
                    throw new Exception($"Account unlock {AccountList[i].Account} failed.");
            }
        }

        private Dictionary<string, List<string>> ExecuteBatchTransactionTask(int threadNo, int times)
        {
            var account = ContractList[threadNo].Owner;
            var contractPath = ContractList[threadNo].ContractAddress;
            var symbol = ContractList[threadNo].Symbol;
            var token = new TokenContract(NodeManager, account, ContractList[threadNo].ContractAddress);
            var transactionsWhitRpc = new Dictionary<string, List<string>>();

            var result = Monitor.CheckTransactionPoolStatus(LimitTransaction);
            if (!result)
            {
                Logger.Warn("Transaction pool transactions over limited, canceled this round execution.");
                return transactionsWhitRpc;
            }

            var rawTransactionList = new List<string>();
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            for (var i = 0; i < times; i++)
            {
                var (from, to) = GetTransferPair(token, symbol, i);

                //Execute Transfer
                var transferInput = new TransferInput
                {
                    Symbol = ContractList[threadNo].Symbol,
                    To = to.ConvertAddress(),
                    Amount = ((i + 1) % 4 + 1) * 1000,
                    Memo = $"transfer test - {Guid.NewGuid()}"
                };
                var requestInfo =
                    NodeManager.GenerateRawTransaction(from, contractPath, TokenMethod.Transfer.ToString(),
                        transferInput);
                rawTransactionList.Add(requestInfo);
            }

            stopwatch.Stop();
            var createTxsTime = stopwatch.ElapsedMilliseconds;

            //Send batch transaction requests
            stopwatch.Restart();
            var rawTransactions = string.Join(",", rawTransactionList);
            var transactions = NodeManager.SendTransactions(rawTransactions);
            if (transactions.Equals(new List<string>()))
            {
                stopwatch.Stop();
                return transactionsWhitRpc;
            }
            var rpc = NodeManager.ApiClient.BaseUrl;
            Logger.Info(transactions);
            transactionsWhitRpc[rpc] = transactions;
            stopwatch.Stop();
            var requestTxsTime = stopwatch.ElapsedMilliseconds;
            Logger.Info(
                $"Thread {threadNo}-{symbol} request transactions: {times}, create time: {createTxsTime}ms, request time: {requestTxsTime}ms.");
            Thread.Sleep(1000);
            return transactionsWhitRpc;
        }

        private void ExecuteAloneTransactionTask(int group)
        {
            while (true)
            {
                if (!GenerateTransactionQueue.TryDequeue(out var rawTransaction))
                    break;

                var transactionId = NodeManager.SendTransaction(rawTransaction);
                Logger.Info("Group={0}, TaskLeft={1}, TxId: {2}", group + 1,
                    GenerateTransactionQueue.Count, transactionId);
                Thread.Sleep(10);
            }
        }

        private (string, string) GetTransferPair(TokenContract token, string symbol, int times,
            bool balanceCheck = false)
        {
            string from, to;
            while (true)
            {
                var fromId = times - FromAccountList.Count >= 0
                    ? (times / FromAccountList.Count > 1
                        ? times - FromAccountList.Count * (times / FromAccountList.Count)
                        : times - FromAccountList.Count)
                    : times;
                if (balanceCheck)
                {
                    var balance = token.GetUserBalance(AccountList[fromId].Account, symbol);
                    if (balance < 1000_00000000) continue;
                }

                from = FromAccountList[fromId].Account;
                break;
            }

            while (true)
            {
                var toId = times - ToAccountList.Count >= 0
                    ? (times / ToAccountList.Count > 1
                        ? times - ToAccountList.Count * (times / ToAccountList.Count)
                        : times - ToAccountList.Count)
                    : times;
//                if (AccountList[toId].Account == from) continue;
                to = ToAccountList[toId].Account;
                break;
            }

            return (from, to);
        }

        private int GetRandomTransactionTimes(bool isRandom, int max)
        {
            if (!isRandom) return max;

            var rand = new Random(Guid.NewGuid().GetHashCode());
            return rand.Next(1, max + 1);
        }

        private void GenerateRawTransactionQueue(int threadNo, int times)
        {
            var account = ContractList[threadNo].Owner;
            var contractPath = ContractList[threadNo].ContractAddress;
            var symbol = ContractList[threadNo].Symbol;
            var token = new TokenContract(NodeManager, account, contractPath);

            var result = Monitor.CheckTransactionPoolStatus(LimitTransaction);
            if (!result)
            {
                Logger.Warn("Transaction pool transactions over limited, canceled this round execution.");
                return;
            }

            for (var i = 0; i < times; i++)
            {
                var (from, to) = GetTransferPair(token, symbol, i);

                //Execute Transfer
                var transferInput = new TransferInput
                {
                    Symbol = ContractList[threadNo].Symbol,
                    To = to.ConvertAddress(),
                    Amount = ((i + 1) % 4 + 1) * 1000,
                    Memo = $"transfer test - {Guid.NewGuid()}"
                };
                var requestInfo = NodeManager.GenerateRawTransaction(from, contractPath,
                    TokenMethod.Transfer.ToString(), transferInput);
                GenerateTransactionQueue.Enqueue(requestInfo);
            }
        }

        private void TransactionSentPerSecond(int transactionCount, long milliseconds)
        {
            var tx = (float) transactionCount;
            var time = (float) milliseconds;

            var result = tx * 1000 / time;

            Logger.Info(
                $"Summary analyze: Total request {transactionCount} transactions in {time / 1000:0.000} seconds, average {result:0.00} txs/second.");
        }

        #region Public Property

        public INodeManager NodeManager { get; private set; }
        public AElfClient ApiClient => NodeManager.ApiClient;
        public AuthorityManager AuthorityManager { get; set; }
        private ExecutionSummary Summary { get; set; }
        private NodeStatusMonitor Monitor { get; set; }
        private TesterTokenMonitor TokenMonitor { get; set; }
        public string BaseUrl { get; }
        private string TokenAddress { get; set; }
        private List<AccountInfo> AccountList { get; }
        private List<AccountInfo> ToAccountList { get; set; }
        private List<AccountInfo> FromAccountList { get; set; }

        private string KeyStorePath { get; }
        private List<ContractInfo> MainContractList { get; }

        private List<ContractInfo> ContractList { get; }
        private List<string> TxIdList { get; }
        public int ThreadCount { get; }
        public int ExeTimes { get; }
        public bool LimitTransaction { get; }
        private ConcurrentQueue<string> GenerateTransactionQueue { get; }
        private static readonly ILog Logger = Log4NetHelper.GetLogger();

        #endregion
    }
}