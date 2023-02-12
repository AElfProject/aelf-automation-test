﻿using System;
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
using TokenContract = AElfChain.Common.Contracts.TokenContract;

namespace AElf.Automation.RpcPerformance
{
    public class ExecutionCategory : IPerformanceCategory
    {
        public ExecutionCategory(int threadCount,
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
            AsyncHelper.RunSync(ApiClient.GetChainStatusAsync);
            //New
            GetTestAccounts(userCount);
            //Unlock Account
            UnlockAllAccounts(ThreadCount);

            //Init other services
            Summary = new ExecutionSummary(NodeManager);
            Monitor = new NodeStatusMonitor(NodeManager);
            TokenMonitor = new TesterTokenMonitor(NodeManager);

            //Transfer token for approve
            var bps = NodeInfoHelper.Config.Nodes.Select(o => o.Account);
            TokenMonitor.TransferTokenForTest(bps.ToList());

            //Set select limit transaction
            var setAccount = bps.First();
            var transactionExecuteLimit = new TransactionExecuteLimit(NodeManager, setAccount);
            if (transactionExecuteLimit.WhetherEnableTransactionLimit())
                transactionExecuteLimit.SetExecutionSelectTransactionLimit();

            //Transfer token for transaction fee
            TokenMonitor.TransferTokenForTest(AccountList.Take(ThreadCount).Select(o => o.Account).ToList());
        }

        public void DeployContracts()
        {
            var contractList = new List<object>();
            for (var i = 0; i < ThreadCount; i++)
            {
                dynamic info = new ExpandoObject();
                info.Id = i;
                info.Account = AccountList[i].Account;

                var txId = NodeManager.DeployContract(AccountList[i].Account, "AElf.Contracts.MultiToken");
                info.TxId = txId;
                info.Result = false;
                contractList.Add(info);
            }

            var count = 0;
            var checkTimes = RpcConfig.ReadInformation.Timeout;

            while (checkTimes > 0)
            {
                checkTimes--;
                Thread.Sleep(1000);
                foreach (dynamic item in contractList)
                {
                    if (item.Result != false) continue;
                    string txId = item.TxId;
                    var transactionResult = AsyncHelper.RunSync(() => ApiClient.GetTransactionResultAsync(txId));
                    var status = transactionResult.Status.ConvertTransactionResultStatus();
                    switch (status)
                    {
                        case TransactionResultStatus.Mined:
                        {
                            count++;
                            item.Result = true;
                            var byteString =
                                ByteString.FromBase64(transactionResult.Logs
                                    .First(l => l.Name.Contains(nameof(ContractDeployed))).NonIndexed);
                            var contractPath = ContractDeployed.Parser.ParseFrom(byteString).Address.ToBase58();
                            ContractList.Add(new ContractInfo(AccountList[item.Id].Account, contractPath));
                            break;
                        }
                        case TransactionResultStatus.Failed:
                            var message =
                                $"Transaction {item.TxId} execution status: {status}." +
                                $"\r\nDetail Message: {JsonConvert.SerializeObject(transactionResult, Formatting.Indented)}";
                            Logger.Error(message);
                            break;
                        case TransactionResultStatus.Pending:
                        case TransactionResultStatus.NotExisted:
                            Logger.Warn($"Transaction {item.TxId} execution status: {status}.");
                            continue;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                    Thread.Sleep(10);
                }

                if (count == contractList.Count)
                    return;
            }

            throw new Exception("Deployed contract not executed successfully.");
        }

        public void DeployContractsWithAuthority()
        {
            ContractList = new List<ContractInfo>();
            var bps = NodeInfoHelper.Config.Nodes;

            var account = AccountList[0].Account;
            var authority = new AuthorityManager(NodeManager, account);
            var miners = authority.GetCurrentMiners();
            if (miners.Count >= ThreadCount)
                for (var i = 0; i < ThreadCount; i++)
                {
                    var currentMiners = authority.GetCurrentMiners();
                    var balance = TokenMonitor.SystemToken.GetUserBalance(currentMiners[i]);
                    if (balance < 1000_00000000)
                        TokenMonitor.SystemToken.TransferBalance(bps.First().Account, currentMiners[i], 10000_00000000,
                            "ELF");
                    Logger.Info($"{miners[i]} deploy contract:");
                    var contractAddress =
                        authority.DeployContractWithAuthority(currentMiners[i], "AElf.Contracts.MultiToken");
                    if (contractAddress.Equals(null))
                        i -= 1;
                    else
                        ContractList.Add(new ContractInfo(currentMiners[i], contractAddress.ToBase58()));
                }
            else
                for (var i = 0; i < ThreadCount;)
                {
                    var currentMiners = authority.GetCurrentMiners();
                    foreach (var miner in currentMiners)
                    {
                        var contractAddress =
                            authority.DeployContractWithAuthority(miner, "AElf.Contracts.MultiToken");
                        if (!contractAddress.Equals(null))
                        {
                            ContractList.Add(new ContractInfo(miner, contractAddress.ToBase58()));
                            i++;
                            if (i == ThreadCount) break;
                        }
                    }

                    Thread.Sleep(60000);
                }
        }

        public void SideChainDeployContractsWithCreator()
        {
            var account = AccountList[0].Account;
            var authority = new AuthorityManager(NodeManager, account);
            var creator = NodeInfoHelper.Config.Nodes.First().Account;
            for (var i = 0; i < ThreadCount; i++)
            {
                var contractAddress = authority.DeployContractWithAuthority(creator, "AElf.Contracts.MultiToken");
                ContractList.Add(new ContractInfo(creator, contractAddress.ToBase58()));
                Thread.Sleep(60000);
            }
        }

        public void SideChainDeployContractsWithAuthority()
        {
            var creator = NodeInfoHelper.Config.Nodes.First().Account;
            for (var i = 0; i < ThreadCount; i++)
            {
                var account = AccountList[i].Account;
                var balance = TokenMonitor.SystemToken.GetUserBalance(account);
                if (balance < 1000_00000000)
                    TokenMonitor.SystemToken.TransferBalance(creator, account, 1000_00000000);
                var authority = new AuthorityManager(NodeManager, account);
                var contractAddress = authority.DeployContractWithAuthority(account, "AElf.Contracts.MultiToken");
                ContractList.Add(new ContractInfo(account, contractAddress.ToBase58()));
            }
        }


        public void InitializeMainContracts()
        {
            var chainStatus = AsyncHelper.RunSync(NodeManager.ApiClient.GetChainStatusAsync);
            var genesis = GenesisContract.GetGenesisContract(NodeManager);
            var systemToken = genesis.GetTokenContract();
            var bps = NodeInfoHelper.Config.Nodes.Select(o => o.Account).ToList();
            //create all token
            foreach (var contract in ContractList)
            {
                var account = contract.Owner;
                var contractPath = contract.ContractAddress;
                var symbol = TesterTokenMonitor.GenerateNotExistTokenSymbol(NodeManager);
                contract.Symbol = symbol;

                Logger.Info($"{contractPath} create test token: ");
                var token = new TokenContract(NodeManager, account, contractPath);
                //create fake ELF token, just for transaction fee
                var primaryToken = NodeManager.GetPrimaryTokenSymbol();
                var balance = systemToken.GetUserBalance(account);
                if (balance < 20000_00000000)
                    systemToken.TransferBalance(bps.First(), account, 20000_00000000);
                token.ExecuteMethodWithResult(TokenMethod.Create, new CreateInput
                {
                    Symbol = primaryToken,
                    TokenName = $"fake {primaryToken} for tx fee",
                    TotalSupply = 10_0000_0000_00000000L,
                    Decimals = 8,
                    Issuer = account.ConvertAddress(),
                    IsBurnable = true
                });

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

        public void InitializeSideChainToken()
        {
            InitializeMainContracts();
        }

        public void ExecuteOneRoundTransactionTask()
        {
            Logger.Info("Start transaction execution at: {0}",
                DateTime.Now.ToString(CultureInfo.InvariantCulture));
            var exec = new Stopwatch();
            exec.Start();
            var contractTasks = new List<Task>();
            for (var i = 0; i < ContractList.Count; i++)
            {
                var j = i;
                contractTasks.Add(Task.Run(() => ExecuteTransactionTask(j, ExeTimes)));
            }

            Task.WaitAll(contractTasks.ToArray<Task>());
            UpdateRandomEndpoint(); //update sent transaction to random endpoint

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

        public void ExecuteContinuousRoundsTransactionsTask(bool useTxs = false)
        {
            var randomTransactionOption = RpcConfig.ReadInformation.RandomEndpointOption;
            //add transaction performance check process
            var testers = AccountList.Take(ThreadCount).Select(o => o.Account).ToList();
            if (NodeManager.IsMainChain())
            {
                var authority = new AuthorityManager(NodeManager, testers.First());
                var miners = authority.GetCurrentMiners();
                testers = miners;
            }

            var cts = new CancellationTokenSource();
            var token = cts.Token;
            var taskList = new List<Task>
            {
                Task.Run(() => Summary.ContinuousCheckTransactionPerformance(token), token),
                Task.Run(() => TokenMonitor.ExecuteTokenCheckTask(testers, token), token),
                Task.Run(() =>
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
                                    var txsTasks = new List<Task>();
                                    for (var i = 0; i < ThreadCount; i++)
                                    {
                                        var j = i;
                                        txsTasks.Add(Task.Run(() => ExecuteBatchTransactionTask(j, exeTimes), token));
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
                                            txTasks.Add(Task.Run(() => ExecuteAloneTransactionTask(j), token));

                                        Task.WaitAll(txTasks.ToArray<Task>());
                                    }
                                }
                            }
                            catch (AggregateException exception)
                            {
                                Logger.Error(
                                    $"Request to {NodeManager.GetApiUrl()} got exception, {exception.Message}");
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

                            Monitor.CheckNodeHeightStatus(!randomTransactionOption
                                .EnableRandom); //random mode, don't check node height
                            UpdateRandomEndpoint(); //update sent transaction to random endpoint
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
        public AElfClient ApiClient => NodeManager.ApiClient;
        private ExecutionSummary Summary { get; set; }
        private NodeStatusMonitor Monitor { get; set; }
        private TesterTokenMonitor TokenMonitor { get; set; }
        public string BaseUrl { get; }
        private List<AccountInfo> AccountList { get; }
        private List<AccountInfo> ToAccountList { get; set; }
        private List<AccountInfo> FromAccountList { get; set; }
        private string KeyStorePath { get; }
        private List<ContractInfo> ContractList { get; set; }
        private List<string> TxIdList { get; }
        public int ThreadCount { get; set; }
        public int ExeTimes { get; }
        public bool LimitTransaction { get; }
        private ConcurrentQueue<string> GenerateTransactionQueue { get; }
        private static readonly ILog Logger = Log4NetHelper.GetLogger();

        #endregion

        #region Private Method

        private void ExecuteTransactionTask(int threadNo, int times)
        {
            var account = ContractList[threadNo].Owner;
            var abiPath = ContractList[threadNo].ContractAddress;

            var set = new HashSet<int>();
            var txIdList = new List<string>();
            var passCount = 0;
            for (var i = 0; i < times; i++)
            {
                var rd = new Random(DateTime.Now.Millisecond);
                var randNumber = rd.Next(ThreadCount, AccountList.Count);
                var countNo = randNumber;
                if (AccountList[countNo].Account.Equals(account))
                    countNo = countNo + 1 > AccountList.Count - 1 ? countNo - 1 : countNo + 1;

                set.Add(countNo);
                var toAccount = AccountList[countNo].Account;

                //Execute Transfer
                var transferInput = new TransferInput
                {
                    Symbol = ContractList[threadNo].Symbol,
                    Amount = ((i + 1) % 4 + 1) * 10000,
                    Memo = $"transfer test - {Guid.NewGuid()}",
                    To = toAccount.ConvertAddress()
                };
                var transactionId = NodeManager.SendTransaction(account, abiPath, "Transfer", transferInput);
                txIdList.Add(transactionId);
                passCount++;

                Thread.Sleep(10);
            }

            Logger.Info("Total contract sent: {0}, passed number: {1}", 2 * times, passCount);
            txIdList.Reverse();
            Monitor.CheckTransactionsStatus(txIdList);
            Logger.Info("{0} Transfer from Address {1}", set.Count, account);
        }

        private void ExecuteBatchTransactionTask(int threadNo, int times)
        {
            var account = ContractList[threadNo].Owner;
            var contractPath = ContractList[threadNo].ContractAddress;
            var token = new TokenContract(NodeManager,account,contractPath);
            var result = Monitor.CheckTransactionPoolStatus(LimitTransaction);
            if (!result)
            {
                Logger.Warn("Transaction pool transactions over limited, canceled this round execution.");
                return;
            }

            var rawTransactionList = new List<string>();
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            for (var i = 0; i < times; i++)
            {
//                var rd = new Random(DateTime.Now.Millisecond);
//                var countNo = rd.Next(0, AccountList.Count);
//                var toAccount = AccountList[countNo].Account;
//                while (toAccount == account)
//                {
//                    countNo = rd.Next(0, AccountList.Count);
//                    toAccount = AccountList[countNo].Account;
//                }
                var (from, to) = GetTransferPair(token, ContractList[threadNo].Symbol, i);

                //Execute Transfer
                var transferInput = new TransferInput
                {
                    Symbol = ContractList[threadNo].Symbol,
                    To = to.ConvertAddress(),
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
                $"Thread {threadNo}-{ContractList[threadNo].Symbol} request transactions: {times}, create time: {createTxsTime}ms, request time: {requestTxsTime}ms.");
            Thread.Sleep(10);
        }

        private void GenerateRawTransactionQueue(int threadNo, int times)
        {
            var account = ContractList[threadNo].Owner;
            var contractPath = ContractList[threadNo].ContractAddress;

            var result = Monitor.CheckTransactionPoolStatus(LimitTransaction);
            if (!result)
            {
                Logger.Warn("Transaction pool transactions over limited, canceled this round execution.");
                return;
            }

            for (var i = 0; i < times; i++)
            {
                var rd = new Random(DateTime.Now.Millisecond);
                var countNo = rd.Next(ThreadCount, AccountList.Count);
                var toAccount = AccountList[countNo].Account;

                //Execute Transfer
                var transferInput = new TransferInput
                {
                    Symbol = ContractList[threadNo].Symbol,
                    To = toAccount.ConvertAddress(),
                    Amount = ((i + 1) % 4 + 1) * 10000,
                    Memo = $"transfer test - {Guid.NewGuid()}"
                };
                var requestInfo = NodeManager.GenerateRawTransaction(account, contractPath,
                    TokenMethod.Transfer.ToString(), transferInput);
                GenerateTransactionQueue.Enqueue(requestInfo);
            }
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
            ToAccountList = AccountList.GetRange(count / 2 - 1, count / 2);
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
                to = ToAccountList[toId].Account;
                break;
            }

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

        private int GetRandomTransactionTimes(bool isRandom, int max)
        {
            if (!isRandom) return max;

            var rand = new Random(Guid.NewGuid().GetHashCode());
            return rand.Next(1, max + 1);
        }

        private void UpdateRandomEndpoint()
        {
            var randomTransactionOption = RpcConfig.ReadInformation.RandomEndpointOption;
            var maxLimit = RpcConfig.ReadInformation.SentTxLimit;
            if (!randomTransactionOption.EnableRandom) return;
            var exceptionTimes = 8;
            while (true)
            {
                var serviceUrl = randomTransactionOption.GetRandomEndpoint();
                if (serviceUrl == NodeManager.GetApiUrl())
                    continue;
                NodeManager.UpdateApiUrl(serviceUrl);
                try
                {
                    var transactionPoolCount =
                        AsyncHelper.RunSync(NodeManager.ApiClient.GetTransactionPoolStatusAsync).Validated;
                    if (transactionPoolCount > maxLimit)
                    {
                        Thread.Sleep(50);
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

                    Logger.Error($"Query transaction pool status got exception : {ex.Message}");
                    Thread.Sleep(1000);
                    exceptionTimes--;
                }
            }
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
                var primaryToken = NodeManager.GetPrimaryTokenSymbol();

                var primaryTokenInfo = token.GetTokenInfo(primaryToken);
                var tokenInfo = token.GetTokenInfo(symbol);

                if (primaryTokenInfo.Equals(new TokenInfo()))
                {
                    Logger.Info($"{primaryToken} is not existed. Create again");
                    var txResult = token.ExecuteMethodWithResult(TokenMethod.Create, new CreateInput
                    {
                        Symbol = primaryToken,
                        TokenName = $"fake token {primaryToken}",
                        TotalSupply = 10_0000_0000_00000000L,
                        Decimals = 8,
                        Issuer = account.ConvertAddress(),
                        IsBurnable = true
                    });
                    if (!txResult.Status.ConvertTransactionResultStatus().Equals(TransactionResultStatus.Mined))
                    {
                        Logger.Info($"Create {primaryToken} failed, remove {contractPath}");
                        removed.Add(contract);
                        continue;
                    }
                }

                if (tokenInfo.Equals(new TokenInfo()))
                {
                    Logger.Info($"{symbol} is not existed. Create again");
                    var txResult = token.ExecuteMethodWithResult(TokenMethod.Create, new CreateInput
                    {
                        Symbol = symbol,
                        TokenName = $"elf token {symbol}",
                        TotalSupply = 10_0000_0000_00000000L,
                        Decimals = 2,
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