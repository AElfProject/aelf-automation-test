using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AElf.Standards.ACS0;
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
using Newtonsoft.Json;
using Volo.Abp.Threading;

namespace AElf.Automation.RpcPerformance
{
    public class ExecutionCategory : IPerformanceCategory
    {
        public ExecutionCategory(int threadCount,
            int exeTimes,
            string baseUrl,
            int transactionGroup,
            int duration,
            string keyStorePath = "")
        {
            if (keyStorePath == "")
                keyStorePath = CommonHelper.GetCurrentDataDir();

            AccountList = new List<AccountInfo>();
            ToAccountList = new List<string>();
            FromAccountList = new List<AccountInfo>();
            ContractList = new List<ContractInfo>();
            GenerateTransactionQueue = new ConcurrentQueue<string>();
            TxIdList = new List<string>();

            ThreadCount = threadCount;
            ExeTimes = exeTimes;
            KeyStorePath = keyStorePath;
            BaseUrl = baseUrl.Contains("http://") ? baseUrl : $"http://{baseUrl}";
            TransactionGroup = transactionGroup;
            Duration = duration;
        }

        public void InitExecCommand()
        {
            Logger.Info("Host Url: {0}", BaseUrl);
            Logger.Info("Key Store Path: {0}", Path.Combine(KeyStorePath, "keys"));
            NodeManager = new NodeManager(BaseUrl, KeyStorePath);
            AuthorityManager = new AuthorityManager(NodeManager);

            //Connect
            AsyncHelper.RunSync(ApiClient.GetChainStatusAsync);
            //New
            GetTestAccounts();
            //Unlock Account
            UnlockAllAccounts();

            //Init other services
            Summary = new ExecutionSummary(NodeManager);
            Monitor = new NodeStatusMonitor(NodeManager);
        }

        public void DeployContracts()
        {
            for (var i = 0; i < ThreadCount; i++)
            {
                var address = AuthorityManager.DeployContract
                    (AccountList.First().Account, "AElf.Contracts.MultiToken");
                ContractList.Add(new ContractInfo(AccountList.First().Account, address.ToBase58()));
            }
        }

        public void InitializeMainContracts()
        {
            //create all token
            foreach (var contract in ContractList)
            {
                var tokenMonitor = new TesterTokenMonitor(NodeManager, contract.ContractAddress);
                var account = contract.Owner;
                var contractPath = contract.ContractAddress;
                var symbol = tokenMonitor.GenerateNotExistTokenSymbol();
                contract.Symbol = symbol;

                Logger.Info($"{contractPath} create test token: ");
                var token = new TokenContract(NodeManager, account, contractPath);
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
            CheckTokenSymbol(ContractList);

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
        }

        public void ExecuteContinuousRoundsTransactionsTask(bool useTxs = false)
        {
            //add transaction performance check process
            var testers = AccountList.Take(ThreadCount).Select(o => o.Account).ToList();
            var cts = new CancellationTokenSource();
            var token = cts.Token;
            var taskList = new List<Task>
            {
                Task.Run(() => Summary.ContinuousCheckTransactionPerformance(token), token),
                Task.Run(() =>
                {
                    Logger.Info("Begin generate multi requests.");
                    try
                    {
                        for (var r = 1; r > 0; r++) //continuous running
                        {
                            var stopwatch = Stopwatch.StartNew();
                            //set random tx sending each round
                            try
                            {
                                Logger.Info("Execution transaction request round: {0}", r);
                                if (useTxs)
                                {
                                    //multi task for SendTransactions query
                                    for (var i = 0; i < ThreadCount; i++)
                                    {
                                        var j = i;
                                        Task.Run(() => ExecuteBatchTransactionTask(j, TransactionGroup), token);
                                    }
                                }
                            }
                            catch (AggregateException exception)
                            {
                                Logger.Error(
                                    $"Request to {NodeManager.GetApiUrl()} got exception, {exception}");
                            }
                            catch (Exception e)
                            {
                                var message = "Execute continuous transaction got exception." +
                                              $"\r\nMessage: {e.Message}" +
                                              $"\r\nStackTrace: {e.StackTrace}";
                                Logger.Error(message);
                            }

                            stopwatch.Stop();
                            TransactionSentPerSecond(ExeTimes, stopwatch.ElapsedMilliseconds);

                            Monitor.CheckNodeHeightStatus(); //random mode, don't check node height
                        }
                    }
                    catch (Exception e)
                    {
                        Logger.Error(e.Message);
                        Logger.Error("Cancel all tasks due to transaction execution exception.");
                        cts.Cancel(); //cancel all tasks
                    }
                }, token)
            };
            Task.WaitAll(taskList.ToArray<Task>());
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

        #region Public Property

        public INodeManager NodeManager { get; private set; }
        public AuthorityManager AuthorityManager { get; set; }
        public AElfClient ApiClient => NodeManager.ApiClient;
        private ExecutionSummary Summary { get; set; }
        private NodeStatusMonitor Monitor { get; set; }
        private TesterTokenMonitor TokenMonitor { get; set; }
        public string BaseUrl { get; }
        private List<AccountInfo> AccountList { get; }
        private List<string> ToAccountList { get; set; }
        private List<AccountInfo> FromAccountList { get; set; }
        private string KeyStorePath { get; }
        private List<ContractInfo> ContractList { get; set; }
        private List<string> TxIdList { get; }
        public int ThreadCount { get; set; }
        private int TransactionGroup { get; set; }
        public int ExeTimes { get; }
        public int Duration { get; }
        private ConcurrentQueue<string> GenerateTransactionQueue { get; }
        private static readonly ILog Logger = Log4NetHelper.GetLogger();

        #endregion

        #region Private Method

        private void ExecuteBatchTransactionTask(int threadNo, int times)
        {
            var account = ContractList[threadNo].Owner;
            var symbol = ContractList[threadNo].Symbol;
            var contractPath = ContractList[threadNo].ContractAddress;
            var token = new TokenContract(NodeManager, account, contractPath);

            var rawTransactionList = new ConcurrentBag<string>();
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var count = ExeTimes / times;
                //Execute Transfer
                Parallel.For(1, times + 1, item =>
                {
                    var (from, to) = GetTransferPair(item - 1);
                    for (var i = 0; i < count; i++)
                    { 
                        var transferInput = new TransferInput
                        {
                            Symbol = symbol,
                            To = to.ConvertAddress(),
                            Amount = 1,
                            Memo = $"T - {Guid.NewGuid()}"
                        };
                        var requestInfo =
                            NodeManager.GenerateRawTransaction(@from, contractPath,
                                TokenMethod.Transfer.ToString(),
                                transferInput);
                        rawTransactionList.Add(requestInfo);
                    }
                });
           

            stopwatch.Stop();
            var createTxsTime = stopwatch.ElapsedMilliseconds;

            var rawTransactions = string.Join(",", rawTransactionList);

            stopwatch.Restart();
            var list = NodeManager.SendTransactions(rawTransactions);
            stopwatch.Stop();
            if(list == new List<string>())
                Logger.Error("**** Transaction Generated Error");

            var requestTxsTime = stopwatch.ElapsedMilliseconds;
            Logger.Info(
                $"Thread {threadNo}-{ContractList[threadNo].Symbol} request transactions: " +
                $"{ExeTimes}, create time: {createTxsTime}ms, request time: {requestTxsTime}ms.");
        }

        private void UnlockAllAccounts()
        {
            /*
            Parallel.For(0, count, i =>
            {
                var result = FromNoeNodeManager.UnlockAccount(AccountList[i].Account);
                if (!result)
                    throw new Exception($"Account unlock {AccountList[i].Account} failed.");
            });
            */
            foreach (var t in from t in AccountList
                let result = NodeManager.UnlockAccount(t.Account)
                where !result
                select t)
            {
                throw new Exception($"Account unlock {t.Account} failed.");
            }
        }

        private void GetTestAccounts()
        {
            var authority = new AuthorityManager(NodeManager);
            var miners = authority.GetCurrentMiners();
            var accounts = NodeManager.ListAccounts();
            var testUsers = accounts.FindAll(o => !miners.Contains(o));
            if (testUsers.Count >= TransactionGroup)
            {
                foreach (var acc in testUsers.Take(TransactionGroup)) FromAccountList.Add(new AccountInfo(acc));
                foreach (var acc in testUsers.Take(TransactionGroup)) AccountList.Add(new AccountInfo(acc));
            }
            else
            {
                foreach (var acc in testUsers) FromAccountList.Add(new AccountInfo(acc));

                var generateCount = TransactionGroup - testUsers.Count;
                for (var i = 0; i < generateCount; i++)
                {
                    var account = NodeManager.NewAccount();
                    FromAccountList.Add(new AccountInfo(account));
                    AccountList.Add(new AccountInfo(account));
                }
            }

            var list = new List<string>();

            for (var i = 0; i < TransactionGroup; i++)
            {
                var account = NodeManager.NewFakeAccount();
                    list.Add(account);
            }
            ToAccountList = list;
        }

        private (string, string) GetTransferPair(int times)
        {
            var from = FromAccountList[times].Account;
            var to = ToAccountList[times];
            return (from, to);
        }


        private void TransactionSentPerSecond(int transactionCount, long milliseconds)
        {
            var tx = (float) transactionCount;
            var time = (float) milliseconds;

            var result = tx * 1000 / time;

            Logger.Info(
                $"Summary analyze: Total request {transactionCount} transactions in {time / 1000:0.000} seconds, average {result:0.00} txs/second.");
        }

        private void CheckTokenSymbol(List<ContractInfo> contractInfos)
        {
            List<ContractInfo> removed = new List<ContractInfo>();
            foreach (var contract in contractInfos)
            {
                var account = contract.Owner;
                var contractPath = contract.ContractAddress;
                var symbol = contract.Symbol;

                var token = new TokenContract(NodeManager, account, contractPath);
                var tokenInfo = token.GetTokenInfo(symbol);

                if (tokenInfo.Equals(new TokenInfo()))
                {
                    Logger.Info($"{symbol} is not existed. Create again");
                    var txResult = token.ExecuteMethodWithResult(TokenMethod.Create, new CreateInput
                    {
                        Symbol = symbol,
                        TokenName = $"elf token {symbol}",
                        TotalSupply = 10_0000_0000_00000000L,
                        Decimals = 8,
                        Issuer = account.ConvertAddress(),
                        IsBurnable = true
                    });
                    if (!txResult.Status.ConvertTransactionResultStatus().Equals(TransactionResultStatus.Mined))
                    {
                        Logger.Info($"Create {symbol} failed, remove {contractPath}");
                        removed.Add(contract);
                    }
                }
            }

            if (removed.Count <= 0) return;
            foreach (var r in removed)
                contractInfos.Remove(r);
        }

        #endregion
    }
}