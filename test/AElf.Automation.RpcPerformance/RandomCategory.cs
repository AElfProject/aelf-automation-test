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
            int transactionGroup,
            int duration,
            string keyStorePath = "")
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
            TransactionGroup = transactionGroup;
            Duration = duration;
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
            var contract = RpcConfig.ReadInformation.ContractAddress;
            if (contract != "")
            {
                TokenAddress = contract;
            }
            else
            {
                Logger.Info("Start deploy test token contract: ");
                var address = AuthorityManager.DeployContract(AccountList.First().Account, "AElf.Contracts.MultiToken");
                TokenAddress = address.ToBase58();
            }

            TokenMonitor = new TesterTokenMonitor(NodeManager, TokenAddress);
            Logger.Info($"Test token address : {TokenAddress}");
        }

        public void InitializeMainContracts()
        {
            //create all token
            var tokenList = RpcConfig.ReadInformation.TokenList;
            if (tokenList.Count != 0)
            {
                var count = tokenList.Count != ThreadCount ? tokenList.Count : ThreadCount;
                for (var i = 0; i < count; i++)
                {
                    var contract = new ContractInfo(AccountList[i].Account, TokenAddress);
                    var symbol = tokenList[i];
                    var checkSymbol = TokenMonitor.CheckSymbol(symbol);
                    var token = new TokenContract(NodeManager, AccountList[i].Account, TokenAddress);

                    if (!checkSymbol)
                    {
                        token.SetAccount(contract.Owner);
                        var transactionId = token.ExecuteMethodWithResult(TokenMethod.Create, new CreateInput
                        {
                            Symbol = symbol,
                            TokenName = $"elf token {symbol}",
                            TotalSupply = 10_0000_0000_00000000L,
                            Decimals = 8,
                            Issuer = contract.Owner.ConvertAddress(),
                            IsBurnable = true
                        });
                    }
                    contract.Symbol = symbol;
                    ContractList.Add(contract);
                }

                if (ContractList.Count != 0)
                {
                    foreach (var contract in ContractList)
                    {
                        var account = contract.Owner;
                        var contractPath = contract.ContractAddress;
                        var symbol = contract.Symbol;
                        var token = new TokenContract(NodeManager, account, contractPath);
                        var tokenInfo = token.GetTokenInfo(symbol);
                        var issueAmount = tokenInfo.TotalSupply / FromAccountList.Count;
                        foreach (var txRes in from @from in FromAccountList
                            let balance = token.GetUserBalance(@from.Account, symbol)
                            where balance == 0
                            select token.IssueBalance(account, @from.Account, issueAmount, symbol))
                        {
                            txRes.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
                        }
                    }
                }
            }

            var already = ContractList.Count;
            if (already >= ThreadCount)
                return;
            for (var i = 0; i < ThreadCount - already; i++)
            {
                var contract = new ContractInfo(AccountList[i].Account, TokenAddress);
                var account = contract.Owner;
                var contractPath = contract.ContractAddress;
                var symbol = TokenMonitor.GenerateNotExistTokenSymbol();
                contract.Symbol = symbol;

                ContractList.Add(contract);

                var token = new TokenContract(NodeManager, account, contractPath);

                token.SetAccount(account);
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
                var info = token.GetTokenInfo(symbol);
                if (info.TotalSupply == info.Issued)
                    continue;
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

            var fromCount = TransactionGroup;

            FromAccountList = AccountList.GetRange(0, fromCount);
            ToAccountList =
                AccountList.GetRange(fromCount, count - fromCount);
        }

        private void GeneratedTransaction(bool useTxs, CancellationTokenSource cts, CancellationToken token)
        {
            Logger.Info("Begin generate multi requests.");
            var enableRandom = RpcConfig.ReadInformation.EnableRandomTransaction;
            try
            {
                for (var r = 1; r > 0; r++) //continuous running
                {
                    //set random tx sending each round
                    var exeTimes = GetRandomTransactionTimes(enableRandom, ExeTimes);
                    var stopwatch = new Stopwatch();
                    stopwatch.Start();

                    try
                    {
                        Logger.Info("Execution transaction request round: {0}", r);
                        if (useTxs)
                        {
                            //multi task for SendTransactions query
                            var txsTasks = new List<Task>();

                            for (var i = 0; i < ThreadCount; i++)
                            {
                                var j = i;
                                txsTasks.Add(Task.Run(() => ExecuteBatchTransactionTask(j, exeTimes), token));
//                                Task.Run(() => ExecuteBatchTransactionTask(j, exeTimes), token);
                            }

                            Task.WaitAll(txsTasks.ToArray<Task>());
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
                    var createTxsTime = stopwatch.ElapsedMilliseconds;
                    TransactionSentPerSecond(ThreadCount * exeTimes, createTxsTime);

                    Monitor.CheckNodeHeightStatus(); //random mode, don't check node height
                    Thread.Sleep(Duration);
                }
            }
            catch (Exception e)
            {
                Logger.Error(e.Message);
                Logger.Error("Cancel all tasks due to transaction execution exception.");
                cts.Cancel(); //cancel all tasks
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

        private void ExecuteBatchTransactionTask(int threadNo, int times)
        {
            var account = ContractList[threadNo].Owner;
            var contractPath = ContractList[threadNo].ContractAddress;
            var symbol = ContractList[threadNo].Symbol;

            var rawTransactionList = new List<string>();
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            for (var i = 0; i < times; i++)
            {
                var (from, to) = GetTransferPair(i);
                //Execute Transfer
                var transferInput = new TransferInput
                {
                    Symbol = symbol,
                    To = to.ConvertAddress(),
                    Amount = ((i + 1) % 4 + 1) * 1000,
                    Memo = $"transfer test - {Guid.NewGuid()}"
                };
                var requestInfo =
                    NodeManager.GenerateRawTransaction(from, contractPath,
                        TokenMethod.Transfer.ToString(),
                        transferInput);
                rawTransactionList.Add(requestInfo);
            }

            stopwatch.Stop();
            var createTxsTime = stopwatch.ElapsedMilliseconds;

            //Send batch transaction requests
            stopwatch.Restart();
            var rawTransactions = string.Join(",", rawTransactionList);
            var transactions = NodeManager.SendTransactions(rawTransactions);
            stopwatch.Stop();

            var requestTxsTime = stopwatch.ElapsedMilliseconds;
            Logger.Info(
                $"Thread {threadNo}-{symbol} request transactions: {times}, create time: {createTxsTime}ms, request time: {requestTxsTime}ms.");
        }

        private (string, string) GetTransferPair(int times)
        {
            var fromId = times - FromAccountList.Count >= 0
                ? (times / FromAccountList.Count > 1
                    ? times - FromAccountList.Count * (times / FromAccountList.Count)
                    : times - FromAccountList.Count)
                : times;

            var from = FromAccountList[fromId].Account;

            var toId = times - ToAccountList.Count >= 0
                ? (times / ToAccountList.Count > 1
                    ? times - ToAccountList.Count * (times / ToAccountList.Count)
                    : times - ToAccountList.Count)
                : times;
            var to = ToAccountList[toId].Account;

            return (from, to);
        }

        private int GetRandomTransactionTimes(bool isRandom, int max)
        {
            if (!isRandom) return max;

            var rand = new Random(Guid.NewGuid().GetHashCode());
            return rand.Next(1, max + 1);
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
        private int TransactionGroup { get; set; }
        private List<AccountInfo> AccountList { get; }
        private List<AccountInfo> ToAccountList { get; set; }
        private List<AccountInfo> FromAccountList { get; set; }

        private string KeyStorePath { get; }
        private List<ContractInfo> MainContractList { get; }

        private List<ContractInfo> ContractList { get; }
        private List<string> TxIdList { get; }
        public int ThreadCount { get; set; }
        public int ExeTimes { get; }
        public int Duration { get; }

        private ConcurrentQueue<string> GenerateTransactionQueue { get; }
        private static readonly ILog Logger = Log4NetHelper.GetLogger();

        #endregion
    }
}