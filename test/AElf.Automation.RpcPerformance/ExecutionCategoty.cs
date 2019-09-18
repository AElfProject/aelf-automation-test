using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
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
using AElf.Types;
using AElfChain.SDK;
using AElfChain.SDK.Models;
using log4net;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Volo.Abp.Threading;
using ApiMethods = AElf.Automation.Common.Managers.ApiMethods;

namespace AElf.Automation.RpcPerformance
{
    public class ExecutionCategory : IPerformanceCategory
    {
        #region Public Property

        public INodeManager NodeManager { get; private set; }
        public IApiService ApiService => NodeManager.ApiService;        
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
        private static readonly ILog Logger = Log4NetHelper.GetLogger();

        #endregion

        public ExecutionCategory(int threadCount,
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
            //Prepare basic token for test - Disable now
            TransferTokenFromBp(true);
            //Set select limit transaction
            var setAccount = GetSetConfigurationLimitAccount();
            var transactionExecuteLimit = new TransactionExecuteLimit(NodeManager, setAccount);
            if (transactionExecuteLimit.WhetherEnableTransactionLimit())
                transactionExecuteLimit.SetExecutionSelectTransactionLimit();

            //Init other services
            Summary = new ExecutionSummary(NodeManager);
            Monitor = new NodeStatusMonitor(NodeManager);
        }

        public void DeployContracts()
        {
            var contractList = new List<object>();
            for (var i = 0; i < ThreadCount; i++)
            {
                dynamic info = new ExpandoObject();
                info.Id = i;
                info.Account = AccountList[i].Account;

                var ci = new CommandInfo(ApiMethods.DeploySmartContract)
                {
                    Parameter = $"AElf.Contracts.MultiToken {AccountList[i].Account}"
                };
                NodeManager.ExecuteCommand(ci);
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
                    string txId = item.TxId;
                    var transactionResult = AsyncHelper.RunSync(()=>ApiService.GetTransactionResultAsync(txId));
                    var status = transactionResult.Status.ConvertTransactionResultStatus();
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
                            Logger.Error(message);
                            break;
                        case TransactionResultStatus.Pending:
                            Logger.Warn($"Transaction {item.TxId} execution status: {transactionResult.Status}.");
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

        public void DeployContractsWithAuthority()
        {
            for (var i = 0; i < ThreadCount; i++)
            {
                var account = AccountList[i].Account;
                var authority = new AuthorityManager(NodeManager, account);
                var contractAddress = authority.DeployContractWithAuthority(account, "AElf.Contracts.MultiToken.dll");
                ContractList.Add(new ContractInfo(account, contractAddress.GetFormatted()));
            }
        }

        public void SideChainDeployContractsWithAuthority()
        {
            for (var i = 0; i < ThreadCount; i++)
            {
                var account = AccountList[0].Account;
                var authority = new AuthorityManager(NodeManager, account);
                var miners = authority.GetCurrentMiners();
                if (i > miners.Count)
                    return;
                var contractAddress = authority.DeployContractWithAuthority(miners[i], "AElf.Contracts.MultiToken.dll");
                ContractList.Add(new ContractInfo(account, contractAddress.GetFormatted()));
            }
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
                
                var token = new TokenContract(NodeManager, account, contractPath);
                var transactionId = token.ExecuteMethodWithTxId(TokenMethod.Create, new CreateInput
                {
                    Symbol = symbol,
                    TokenName = $"elf token {symbol}",
                    TotalSupply = long.MaxValue,
                    Decimals = 2,
                    Issuer = AddressHelper.Base58StringToAddress(account),
                    IsBurnable = true
                });
                TxIdList.Add(transactionId);
            }
            Monitor.CheckTransactionsStatus(TxIdList);

            //issue all token
            foreach (var contract in ContractList)
            {
                var account = contract.Owner;
                var contractPath = contract.ContractPath;
                var symbol = contract.Symbol;
                
                var token = new TokenContract(NodeManager, account, contractPath);
                var transactionId = token.ExecuteMethodWithTxId(TokenMethod.Issue, new IssueInput()
                {
                    Amount = long.MaxValue,
                    Memo = $"Issue all balance to owner - {Guid.NewGuid()}",
                    Symbol = symbol,
                    To = AddressHelper.Base58StringToAddress(account)
                });
                TxIdList.Add(transactionId);
            }
            Monitor.CheckTransactionsStatus(TxIdList);

            //prepare conflict environment
            var conflict = ConfigInfoHelper.Config.Conflict;
            if (!conflict)
                InitializeTransactionGroup();
        }

        private void InitializeTransactionGroup()
        {
            var nodeManager = NodeManager;
            var users = AccountList.Skip(ThreadCount).ToList();
            var contracts = ContractList;

            Group = new TransactionGroup(nodeManager, users, contracts);
            Group.InitializeAllUsersToken();
            Task.Run(() => Group.GenerateAllContractTransactions());
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
            var randomTransactionOption = ConfigInfoHelper.Config.RandomEndpointOption;
            //add transaction performance check process
            var taskList = new List<Task>
            {
                Task.Run(() => { Summary.ContinuousCheckTransactionPerformance(); }),
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
                                    txsTasks.Add(conflict
                                        ? Task.Run(() => ExecuteBatchTransactionTask(j, exeTimes))
                                        : Task.Run(() => ExecuteNonConflictBatchTransactionTask(j, exeTimes)));
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
                            Logger.Error($"Request to {NodeManager.GetApiUrl()} got exception, {exception.Message}");
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
                })
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
                var toAccount = AccountList[countNo].Account;

                //Execute Transfer
                var ci = new CommandInfo(ApiMethods.SendTransaction, account, abiPath, "Transfer")
                {
                    ParameterInput = new TransferInput
                    {
                        Symbol = ContractList[threadNo].Symbol,
                        Amount = (i + 1) % 4 + 1,
                        Memo = $"transfer test - {Guid.NewGuid()}",
                        To = AddressHelper.Base58StringToAddress(toAccount)
                    }
                };
                NodeManager.ExecuteCommand(ci);

                if (ci.Result)
                {
                    var transactionResult = ci.InfoMsg as SendTransactionOutput;
                    txIdList.Add(transactionResult?.TransactionId);
                    passCount++;
                }

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
            var contractPath = ContractList[threadNo].ContractPath;

            Monitor.CheckTransactionPoolStatus(LimitTransaction); //check transaction pool first

            var rawTransactions = new List<string>();
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            for (var i = 0; i < times; i++)
            {
                var rd = new Random(DateTime.Now.Millisecond);
                var countNo = rd.Next(ThreadCount, AccountList.Count);
                var toAccount = AccountList[countNo].Account;

                //Execute Transfer
                var ci = new CommandInfo(ApiMethods.SendTransaction, account, contractPath, "Transfer")
                {
                    ParameterInput = new TransferInput
                    {
                        Symbol = ContractList[threadNo].Symbol,
                        To = AddressHelper.Base58StringToAddress(toAccount),
                        Amount = (i + 1) % 4 + 1,
                        Memo = $"transfer test - {Guid.NewGuid()}"
                    }
                };
                var requestInfo = NodeManager.GenerateTransactionRawTx(ci);
                rawTransactions.Add(requestInfo);
            }
            stopwatch.Stop();
            var createTxsTime = stopwatch.ElapsedMilliseconds;

            //Send batch transaction requests
            stopwatch.Restart();
            var commandInfo = new CommandInfo(ApiMethods.SendTransactions)
            {
                Parameter = string.Join(",", rawTransactions)
            };
            NodeManager.ExecuteCommand(commandInfo);
            stopwatch.Stop();
            var requestTxsTime = stopwatch.ElapsedMilliseconds;
            
            Assert.IsTrue(commandInfo.Result);
            Logger.Info($"Thread {threadNo} request transactions: {times}, create time: {createTxsTime}ms, request time: {requestTxsTime}ms.");
            Thread.Sleep(10);
        }

        private void ExecuteNonConflictBatchTransactionTask(int threadNo, int times)
        {
            Monitor.CheckTransactionPoolStatus(LimitTransaction); //check transaction pool first
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            var rawTransactions = Group.GetRawTransactions();
            stopwatch.Stop();
            var generateRawInfoTime = stopwatch.ElapsedMilliseconds;
            
            //Send batch transaction requests
            stopwatch.Restart();
            var commandInfo = new CommandInfo(ApiMethods.SendTransactions)
            {
                Parameter = string.Join(",", rawTransactions)
            };
            NodeManager.ExecuteCommand(commandInfo);
            stopwatch.Stop();
            var requestTime = stopwatch.ElapsedMilliseconds;
            
            Assert.IsTrue(commandInfo.Result);
            Logger.Info($"Thread {threadNo} send transactions: {times}, generate time: {generateRawInfoTime}ms, execute time: {requestTime}ms");
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
                var countNo = rd.Next(ThreadCount, AccountList.Count);
                var toAccount = AccountList[countNo].Account;

                //Execute Transfer
                var ci = new CommandInfo(ApiMethods.SendTransaction, account, contractPath, "Transfer")
                {
                    ParameterInput = new TransferInput
                    {
                        Symbol = ContractList[threadNo].Symbol,
                        To = AddressHelper.Base58StringToAddress(toAccount),
                        Amount = (i + 1) % 4 + 1,
                        Memo = $"transfer test - {Guid.NewGuid()}"
                    }
                };
                var requestInfo = NodeManager.GenerateTransactionRawTx(ci);
                GenerateTransactionQueue.Enqueue(requestInfo);
            }
        }

        private void ExecuteAloneTransactionTask(int group)
        {
            while (true)
            {
                if (!GenerateTransactionQueue.TryDequeue(out var rpcMsg))
                    break;

                var ci = new CommandInfo(ApiMethods.SendTransaction) {Parameter = rpcMsg};
                NodeManager.BroadcastWithRawTx(ci);
                var transactionOutput = ci.InfoMsg as SendTransactionOutput;
                Logger.Info("Group={0}, TaskLeft={1}, TxId: {2}", group + 1,
                    GenerateTransactionQueue.Count, transactionOutput?.TransactionId);
                Thread.Sleep(10);
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
            var randomTransactionOption = ConfigInfoHelper.Config.RandomEndpointOption;
            var maxLimit = ConfigInfoHelper.Config.SentTxLimit;
            if (!randomTransactionOption.EnableRandom) return;
            while (true)
            {
                var serviceUrl = randomTransactionOption.GetRandomEndpoint();
                if (serviceUrl == NodeManager.GetApiUrl())
                    continue;
                NodeManager.UpdateApiUrl(serviceUrl);
                try
                {
                    var transactionPoolCount =
                        AsyncHelper.RunSync(NodeManager.ApiService.GetTransactionPoolStatusAsync).Queued;
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
                    Logger.Error($"Query transaction pool status got exception : {ex.Message}");
                }
            }
        }
        
        private void TransferTokenFromBp(bool enable)
        {
            if (!enable) return;
            try
            {
                var genesis = GenesisContract.GetGenesisContract(NodeManager);
                var bpNode = NodeInfoHelper.Config.Nodes.First();
                var token = genesis.GetTokenContract();
                var chainType = ConfigInfoHelper.Config.ChainTypeInfo;
                var symbol = chainType.IsMainChain ? "ELF" : chainType.TokenSymbol;
                
                for (var i = 0; i < ThreadCount; i++)
                {
                    token.TransferBalance(bpNode.Account, AccountList[i].Account, 10_0000_0000, symbol);
                }
            }
            catch (Exception e)
            {
                Logger.Error("Prepare basic token got exception.");
                Logger.Error(e);
            }
        }

        private string GetSetConfigurationLimitAccount()
        {
            var nodeConfig = NodeInfoHelper.Config;
            return nodeConfig.IsMainChain ? AccountList[0].Account : nodeConfig.Nodes.First().Account;
        }

        #endregion
    }
}