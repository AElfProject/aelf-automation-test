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
using AElf.Automation.Common.OptionManagers.Authority;
using AElf.Automation.Common.WebApi.Dto;
using AElf.Contracts.MultiToken.Messages;
using AElf.Types;
using log4net;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Volo.Abp.Threading;

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
        private TokenContract SystemToken { get; set; }
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

            ApiHelper = new WebApiHelper(BaseUrl, KeyStorePath);

            //Connect Chain
            var ci = new CommandInfo(ApiMethods.GetChainInformation);
            ApiHelper.ExecuteCommand(ci);
            Assert.IsTrue(ci.Result, "Connect chain got exception.");

            //New
            NewAccounts(userCount);

            //Unlock Account
            UnlockAllAccounts(userCount);
            
            //Prepare basic token for test - Disable now
            TransferTokenForTest(false);

            //Set select limit transaction
            var transactionExecuteLimit = new TransactionExecuteLimit(BaseUrl, AccountList[0].Account);
            if (transactionExecuteLimit.WhetherEnableTransactionLimit())
                transactionExecuteLimit.SetExecutionSelectTransactionLimit();

            //Init other services
            Summary = new ExecutionSummary(ApiHelper);
            Monitor = new NodeStatusMonitor(ApiHelper);
        }

        private void TransferTokenForTest(bool enable)
        {
            if (!enable) return;
            try
            {
                SystemToken = GetSystemToken(AccountList.First().Account);
                var bpNode = NodeInfoHelper.Config.Nodes.First();
                for (var i = 0; i < ThreadCount; i++)
                {
                    SystemToken.TransferBalance(bpNode.Account, AccountList[i].Account, 9000_000_000);
                }
            }
            catch (Exception e)
            {
                Logger.Error("Prepare basic ELF token got exception.");
                Logger.Error(e);
            }
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
                var account = AccountList[0].Account;
                var authority = new AuthorityManager(BaseUrl, account);
                var miners = authority.GetCurrentMiners();
                if(i > miners.Count)
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
                var ci = new CommandInfo(ApiMethods.SendTransaction, account, contractPath, "Create")
                {
                    ParameterInput = new CreateInput
                    {
                        Symbol = symbol,
                        TokenName = $"elf token {symbol}",
                        TotalSupply = long.MaxValue,
                        Decimals = 2,
                        Issuer = AddressHelper.Base58StringToAddress(account),
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
                        To = AddressHelper.Base58StringToAddress(account)
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

        private void InitializeTransactionGroup()
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
                            Logger.Error($"Request to {ApiHelper.GetApiUrl()} got exception, {exception.Message}");
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
                Logger.Info("{0:00}. Account: {1}, ContractAddress:{2}", count,
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
                ApiHelper.ExecuteCommand(ci);

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
            Logger.Info("Batch request transactions: {0}, passed transaction count: {1}", rawTransactions.Count,
                transactions.Length);
            Logger.Info("Thread [{0}] completed executed {1} times contracts work.", threadNo, times);
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
            Logger.Info(" Batch request transactions: {0}, passed transaction count: {1}", rawTransactions.Count,
                transactions.Length);
            Logger.Info("Thread [{0}] completed executed {1} times contracts work.", threadNo, times);
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
                Logger.Info("Transaction group: {0}, execution left: {1}", group + 1,
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
                if (serviceUrl == ApiHelper.GetApiUrl())
                    continue;
                ApiHelper.UpdateApiUrl(serviceUrl);
                try
                {
                    var transactionPoolCount =
                        AsyncHelper.RunSync(() => ApiHelper.ApiService.GetTransactionPoolStatus()).Queued;
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

        private TokenContract GetSystemToken(string account)
        {
            var genesis = GenesisContract.GetGenesisContract(ApiHelper, account);
            var tokenAddress = genesis.GetContractAddressByName(NameProvider.TokenName);
            
            return new TokenContract(ApiHelper, account, tokenAddress.GetFormatted());
        }

        #endregion
    }
}