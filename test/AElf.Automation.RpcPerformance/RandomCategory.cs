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
using AElf.Automation.Common;
using AElf.Automation.Common.Contracts;
using AElf.Automation.Common.Helpers;
using AElf.Automation.Common.Managers;
using AElf.Automation.Common.Utils;
using AElf.Contracts.MultiToken;
using AElfChain.SDK;
using log4net;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Org.BouncyCastle.Asn1.TeleTrust;
using Volo.Abp.Threading;

namespace AElf.Automation.RpcPerformance
{
    public class RandomCategory : IPerformanceCategory
    {
        #region Public Property
        public INodeManager NodeManager { get; private set; }
        public IApiService ApiService => NodeManager.ApiService;
        private ExecutionSummary Summary { get; set; }
        private TransactionGroup Group { get; set; }
        private NodeStatusMonitor Monitor { get; set; }
        private TesterTokenMonitor TokenMonitor { get; set; }
        public string BaseUrl { get; }
        private string SystemTokenAddress { get; set; }
        private List<AccountInfo> AccountList { get; }
        private string KeyStorePath { get; }
        private List<ContractInfo> ContractList { get; }
        private List<string> TxIdList { get; }
        public int ThreadCount { get; }
        public int ExeTimes { get; }
        public bool LimitTransaction { get; }
        private ConcurrentQueue<string> GenerateTransactionQueue { get; }
        private static readonly ILog Logger = Log4NetHelper.GetLogger();
        private static Random RandomGen = new Random(DateTime.Now.Millisecond);

        #endregion

        public RandomCategory(int threadCount,
            int exeTimes,
            string baseUrl,
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
            BaseUrl = baseUrl.Contains("http://") ? baseUrl : $"http://{baseUrl}";
            LimitTransaction = limitTransaction;
        }

        public void InitExecCommand(int userCount = 200)
        {
            Logger.Info("Host Url: {0}", BaseUrl);
            Logger.Info("Key Store Path: {0}", Path.Combine(KeyStorePath, "keys"));
            NodeManager = new NodeManager(BaseUrl, KeyStorePath);

            //Connect
            AsyncHelper.RunSync(ApiService.GetChainStatusAsync);
            //New
            GetTestAccounts(userCount);
            //Unlock Account
            UnlockAllAccounts(ThreadCount);
            Task.Run(() => UnlockAllAccounts(userCount));
            Logger.Info("Start other testing");
            //Init other services
            Summary = new ExecutionSummary(NodeManager);
            Monitor = new NodeStatusMonitor(NodeManager);
            TokenMonitor = new TesterTokenMonitor(NodeManager);
            SystemTokenAddress = TokenMonitor.SystemToken.ContractAddress;

            if (NodeInfoHelper.Config.ChainTypeInfo.IsMainChain)
                //TransferTokenFromBp();
                TokenMonitor.TransferTokenForTest(AccountList.Select(o => o.Account).ToList());
            else
                //Prepare token for side chain 
                TokenMonitor.IssueTokenForTest(AccountList.Select(o => o.Account).ToList());

            //Set select limit transaction
            var setAccount = GetSetConfigurationLimitAccount();
            var transactionExecuteLimit = new TransactionExecuteLimit(NodeManager, setAccount);
            if (transactionExecuteLimit.WhetherEnableTransactionLimit())
                transactionExecuteLimit.SetExecutionSelectTransactionLimit();
        }

        public void DeployContractsWithAuthority()
        {
        }

        public void SideChainDeployContractsWithAuthority()
        {
        }

        public void DeployContracts()
        {
            throw new System.NotImplementedException();
        }

        public void InitializeContracts()
        {
            //create all token
            for(var i=0; i<ThreadCount; i++)
            {
                var contract = new ContractInfo(AccountList[i].Account, SystemTokenAddress); 
                var account = contract.Owner;
                var contractPath = contract.ContractAddress;
                var symbol = $"{CommonHelper.RandomString(8, false)}";
                contract.Symbol = symbol;
                ContractList.Add(contract);

                var token = new TokenContract(NodeManager, account, contractPath);
                var transactionId = token.ExecuteMethodWithTxId(TokenMethod.Create, new CreateInput
                {
                    Symbol = symbol,
                    TokenName = $"elf token {symbol}",
                    TotalSupply = long.MaxValue,
                    Decimals = 8,
                    Issuer = AddressHelper.Base58StringToAddress(account),
                    IsBurnable = true
                });
                TxIdList.Add(transactionId);
            }

            Monitor.CheckTransactionsStatus(TxIdList);

            //issue all token
            var amount = long.MaxValue / AccountList.Count;
            foreach (var contract in ContractList)
            {
                var account = contract.Owner;
                var contractPath = contract.ContractAddress;
                var symbol = contract.Symbol;

                var token = new TokenContract(NodeManager, account, contractPath);
                foreach (var user in AccountList)
                {
                    var transactionId = token.ExecuteMethodWithTxId(TokenMethod.Issue, new IssueInput()
                    {
                        Amount = amount,
                        Memo = $"Issue balance - {Guid.NewGuid()}",
                        Symbol = symbol,
                        To = AddressHelper.Base58StringToAddress(user.Account)
                    });
                    TxIdList.Add(transactionId);
                }
            }

            Monitor.CheckTransactionsStatus(TxIdList);
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

        public void ExecuteContinuousRoundsTransactionsTask(bool useTxs = false, bool conflict = true)
        {
            var randomTransactionOption = ConfigInfoHelper.Config.RandomEndpointOption;
            //add transaction performance check process
            var testers = AccountList.Select(o => o.Account).ToList();
            var taskList = new List<Task>
            {
                Task.Run(() => Summary.ContinuousCheckTransactionPerformance()),
                Task.Run(() => TokenMonitor.ExecuteTokenCheckTask(testers)),
                Task.Run(() =>
                {
                    Logger.Info("Begin generate multi requests.");

                    var enableRandom = ConfigInfoHelper.Config.EnableRandomTransaction;
                    var stopwatch = new Stopwatch();
                    stopwatch.Start();
                    for (var r = 1; r > 0; r++) //continuous running
                    {
                        //set random tx sending each round
                        var exeTimes = GetRandomTransactionTimes(enableRandom, ExeTimes);
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
                                    txsTasks.Add(Task.Run(() => ExecuteBatchTransactionTask(j, exeTimes)));
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
                                    GenerateRawTransactionQueue(j, exeTimes);
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

                        Monitor.CheckNodeHeightStatus(!randomTransactionOption
                            .EnableRandom); //random mode, don't check node height

                        stopwatch.Stop();
                        TransactionSentPerSecond(ThreadCount * exeTimes, stopwatch.ElapsedMilliseconds);

                        UpdateRandomEndpoint(); //update sent transaction to random endpoint

                        stopwatch = new Stopwatch();
                        stopwatch.Start();
                    }

                    throw new Exception("Send transaction stopped due to some unknown reason.");
                })
            };

            Task.WaitAll(taskList.ToArray<Task>());
        }

        private void GetTestAccounts(int count)
        {
            var accounts = NodeManager.ListAccounts();
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
                    var account = NodeManager.NewAccount();
                    AccountList.Add(new AccountInfo(account));
                }
            }
        }

        private void UnlockAllAccounts(int count)
        {
            /*
            for (var i = 0; i < count; i++)
            {
                var result = NodeManager.UnlockAccount(AccountList[i].Account);
                Assert.IsTrue(result);
            }
            */

            Parallel.For(0, count, i =>
            {
                var result = NodeManager.UnlockAccount(AccountList[i].Account);
                Assert.IsTrue(result);
            });
        }

        private void UpdateRandomEndpoint()
        {
            var randomTransactionOption = ConfigInfoHelper.Config.RandomEndpointOption;
            var maxLimit = ConfigInfoHelper.Config.SentTxLimit;
            if (!randomTransactionOption.EnableRandom) return;
            var exceptionTimes = 120;
            while (true)
            {
                var serviceUrl = randomTransactionOption.GetRandomEndpoint();
                if (serviceUrl == NodeManager.GetApiUrl())
                    continue;
                NodeManager.UpdateApiUrl(serviceUrl);
                try
                {
                    var transactionPoolCount =
                        AsyncHelper.RunSync(NodeManager.ApiService.GetTransactionPoolStatusAsync).Validated;
                    if (transactionPoolCount > maxLimit)
                    {
                        Thread.Sleep(1000);
                        Logger.Warn(
                            $"TxHub current transaction count:{transactionPoolCount}, current test limit number: {maxLimit}");
                        continue;
                    }

                    break;
                }
                catch (Exception ex)
                {
                    if (exceptionTimes == 0)
                        throw new HttpRequestException(ex.Message);

                    Logger.Error($"Query transaction pool status got exception : {ex}");
                    Thread.Sleep(1000);
                    exceptionTimes--;
                }
            }
        }

        private void TransferTokenForSideChain()
        {
            try
            {
                var nodeConfig = NodeInfoHelper.Config;
                var account = nodeConfig.Nodes.First().Account;
                var sideChainTokenSymbol = nodeConfig.ChainTypeInfo.TokenSymbol;
                var genesis = GenesisContract.GetGenesisContract(NodeManager, account);
                var systemToken = genesis.GetTokenContract();

                for (var i = 0; i < ThreadCount; i++)
                {
                    systemToken.IssueBalance(account, AccountList[i].Account, 10000_00000000, sideChainTokenSymbol);
                }

                var nodes = nodeConfig.Nodes;

                foreach (var node in nodes)
                {
                    if (node.Account == account) continue;
                    systemToken.IssueBalance(account, node.Account, 10000_00000000, sideChainTokenSymbol);
                }
            }
            catch (Exception e)
            {
                Logger.Error("Issue side chain token got exception.");
                Logger.Error(e);
            }
        }

        private string GetSetConfigurationLimitAccount()
        {
            var nodeConfig = NodeInfoHelper.Config;
            return NodeOption.IsMainChain ? AccountList[0].Account : nodeConfig.Nodes.First().Account;
        }

        private void ExecuteTransactionTask(int threadNo, int times)
        {
            var acc = ContractList[threadNo].Owner;
            var contractPath = ContractList[threadNo].ContractAddress;
            var symbol = ContractList[threadNo].Symbol;
            var token = new TokenContract(NodeManager, acc, contractPath);
            var txIdList = new List<string>();
            var passCount = 0;
            for (var i = 0; i < times; i++)
            {
                var (from, to) = GetTransferPair(token, symbol);

                //Execute Transfer
                var transferInput = new TransferInput
                {
                    Symbol = ContractList[threadNo].Symbol,
                    Amount = ((i + 1) % 4 + 1) * 10000,
                    Memo = $"transfer test - {Guid.NewGuid()}",
                    To = AddressHelper.Base58StringToAddress(to)
                };
                var transactionId = NodeManager.SendTransaction(from, contractPath, "Transfer", transferInput);
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
            var account = ContractList[threadNo].Owner;
            var contractPath = ContractList[threadNo].ContractAddress;
            var symbol = ContractList[threadNo].Symbol;
            var token = new TokenContract(NodeManager, account, ContractList[threadNo].ContractAddress);

            Monitor.CheckTransactionPoolStatus(LimitTransaction); //check transaction pool first

            var rawTransactionList = new List<string>();
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            for (var i = 0; i < times; i++)
            {
                var (from, to) = GetTransferPair(token, symbol);

                //Execute Transfer
                var transferInput = new TransferInput
                {
                    Symbol = ContractList[threadNo].Symbol,
                    To = AddressHelper.Base58StringToAddress(to),
                    Amount = ((i + 1) % 4 + 1) * 10000,
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
            Logger.Info(transactions);
            stopwatch.Stop();
            var requestTxsTime = stopwatch.ElapsedMilliseconds;
            Logger.Info(
                $"Thread {threadNo} request transactions: {times}, create time: {createTxsTime}ms, request time: {requestTxsTime}ms.");
            Thread.Sleep(10);
        }

        private void ExecuteAloneTransactionTask(int group)
        {
            while (true)
            {
                if (!GenerateTransactionQueue.TryDequeue(out var rawTransaction))
                    break;

                var transactionOutput = AsyncHelper.RunSync(() => ApiService.SendTransactionAsync(rawTransaction));
                Logger.Info("Group={0}, TaskLeft={1}, TxId: {2}", group + 1,
                    GenerateTransactionQueue.Count, transactionOutput.TransactionId);
                Thread.Sleep(10);
            }
        }

        private (string, string) GetTransferPair(TokenContract token, string symbol, bool balanceCheck = false)
        {
            string from, to;
            while (true)
            {
                var fromId = RandomGen.Next(0, AccountList.Count);
                if (balanceCheck)
                {
                    var balance = token.GetUserBalance(AccountList[fromId].Account, symbol);
                    if (balance < 1000_00000000) continue;
                }
                from = AccountList[fromId].Account;
                break;
            }

            while (true)
            {
                var toId = RandomGen.Next(0, AccountList.Count);
                if(AccountList[toId].Account == from) continue;
                to = AccountList[toId].Account;
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

            Monitor.CheckTransactionPoolStatus(LimitTransaction);

            for (var i = 0; i < times; i++)
            {
                var (from, to) = GetTransferPair(token, symbol);

                //Execute Transfer
                var transferInput = new TransferInput
                {
                    Symbol = ContractList[threadNo].Symbol,
                    To = AddressHelper.Base58StringToAddress(to),
                    Amount = ((i + 1) % 4 + 1) * 10000,
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
    }
}