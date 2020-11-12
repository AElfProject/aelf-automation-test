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

        public void InitExecCommand(int userCount = 20)
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
            Task.Run(() => UnlockAllAccounts(userCount));
            //Init other services
            Summary = new ExecutionSummary(NodeManager);
            Monitor = new NodeStatusMonitor(NodeManager);
            TokenMonitor = new TesterTokenMonitor(NodeManager);
            SystemTokenAddress = TokenMonitor.SystemToken.ContractAddress;

            var chainId = NodeManager.GetChainId();
            CrossTransferToInitAccount();
            //Transfer token for approve
            var bps = NodeInfoHelper.Config.Nodes.Select(o => o.Account);
            var enumerable = bps as string[] ?? bps.ToArray();
            TokenMonitor.TransferTokenForTest(enumerable.ToList());

            //Set select limit transaction
            var setAccount = enumerable.First();
            var transactionExecuteLimit = new TransactionExecuteLimit(NodeManager, setAccount);
            if (transactionExecuteLimit.WhetherEnableTransactionLimit())
                transactionExecuteLimit.SetExecutionSelectTransactionLimit();

            //Transfer token for transaction fee
            TokenMonitor.TransferTokenForTest(AccountList.Select(o => o.Account).ToList());
        }

        public void DeployContractsWithAuthority()
        {
        }

        public void SideChainDeployContractsWithAuthority()
        {
        }

        public void SideChainDeployContractsWithCreator()
        {
        }

        public void DeployContracts()
        {
            throw new NotImplementedException();
        }

        public void InitializeMainContracts()
        {
            var primaryToken = NodeManager.GetPrimaryTokenSymbol();
            var bps = NodeInfoHelper.Config.Nodes;
            //create all token
            for (var i = 0; i < ThreadCount; i++)
            {
                var contract = new ContractInfo(AccountList[i].Account, SystemTokenAddress);
                var account = contract.Owner;
                var contractPath = contract.ContractAddress;
                var symbol = TesterTokenMonitor.GenerateNotExistTokenSymbol(NodeManager);
                contract.Symbol = symbol;
                if (i == 0) //add default ELF transfer group
                {
                    contract.Symbol = primaryToken;
                    ContractList.Add(contract);
                    continue;
                }

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
            var amount = 10_0000_0000_00000000L / AccountList.Count;
            foreach (var contract in ContractList)
            {
                var account = contract.Owner;
                var contractPath = contract.ContractAddress;
                var symbol = contract.Symbol;
                if (symbol == primaryToken) continue;
                var token = new TokenContract(NodeManager, account, contractPath);
                foreach (var user in AccountList)
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
                if (symbol == primaryToken) continue;
                var token = new TokenContract(NodeManager, AccountList.First().Account, contractPath);
                foreach (var user in AccountList)
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

        public void InitializeSideChainToken()
        {
            var mainUrl = RpcConfig.ReadInformation.ChainTypeOption.MainChainUrl;
            MainNodeManager = new NodeManager(mainUrl);
            var mainChainId = ChainHelper.ConvertBase58ToChainId(MainNodeManager.GetChainId());
            var bps = NodeInfoHelper.Config.Nodes;
            var initAccount = bps.First().Account;
            var mainGenesis = MainNodeManager.GetGenesisContract();
            var mainToken = mainGenesis.GetTokenContract();
            //create all token on main chain
            for (var i = 0; i < ThreadCount; i++)
            {
                var contract = new ContractInfo(initAccount, mainToken.ContractAddress);
                var contractPath = contract.ContractAddress;
                var symbol = TesterTokenMonitor.GenerateNotExistTokenSymbol(MainNodeManager);
                contract.Symbol = symbol;
                MainContractList.Add(contract);
                var balance = mainToken.GetUserBalance(initAccount);
                mainToken.SetAccount(initAccount);
                var transactionId = mainToken.ExecuteMethodWithTxId(TokenMethod.Create, new CreateInput
                {
                    Symbol = symbol,
                    TokenName = $"elf token {symbol}",
                    TotalSupply = 10_0000_0000_00000000L,
                    Decimals = 8,
                    Issuer = initAccount.ConvertAddress(),
                    IsBurnable = true
                });
                TxIdList.Add(transactionId);
            }

            Monitor.CheckTransactionsStatus(TxIdList, -1, MainNodeManager);

            // issue all token to init account
            var amount = 10_0000_0000_00000000L;
            foreach (var contract in MainContractList)
            {
                var account = contract.Owner;
                var contractPath = contract.ContractAddress;
                var symbol = contract.Symbol;
                var transactionId = mainToken.ExecuteMethodWithTxId(TokenMethod.Issue, new IssueInput
                {
                    Amount = amount,
                    Memo = $"Issue balance - {Guid.NewGuid()}",
                    Symbol = symbol,
                    To = initAccount.ConvertAddress()
                });
                TxIdList.Add(transactionId);
            }

            Monitor.CheckTransactionsStatus(TxIdList, -1, MainNodeManager);

            //create token on main chain
            var crossChainManager = new CrossChainManager(MainNodeManager, NodeManager, initAccount);
            var txInfos = new Dictionary<string, TransactionResultDto>();
            foreach (var contract in MainContractList)
            {
                var result = crossChainManager.ValidateTokenSymbol(contract.Symbol, out string raw);
                var txId = MainNodeManager.SendTransaction(raw);
                result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
                txInfos.Add(raw, result);
            }

            // create input 
            var crossChainCreateTokenInputList = new List<CrossChainCreateTokenInput>();
            foreach (var txInfo in txInfos)
            {
                var merklePath = crossChainManager.GetMerklePath(txInfo.Value.BlockNumber,
                    txInfo.Value.TransactionId, out var root);

                var crossChainCreateToken = new CrossChainCreateTokenInput
                {
                    FromChainId = mainChainId,
                    MerklePath = merklePath,
                    TransactionBytes = ByteStringHelper.FromHexString(txInfo.Key),
                    ParentChainHeight = txInfo.Value.BlockNumber
                };
                crossChainCreateTokenInputList.Add(crossChainCreateToken);
            }

            //check last transaction index 
            var last = txInfos.Last();
            crossChainManager.CheckSideChainIndexMainChain(last.Value.BlockNumber);

            //cross create token
            var sideContract = new ContractInfo(initAccount, SystemTokenAddress);
            var sideContractPath = sideContract.ContractAddress;
            var sideToken = new TokenContract(NodeManager, initAccount, sideContractPath);
            foreach (var crossChainCreateTokenInput in crossChainCreateTokenInputList)
            {
                var result =
                    sideToken.ExecuteMethodWithResult(TokenMethod.CrossChainCreateToken, crossChainCreateTokenInput);
                result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            }

            //check token on side chain 
            foreach (var contractInfo in MainContractList)
            {
                var tokenInfo = sideToken.GetTokenInfo(contractInfo.Symbol);
                tokenInfo.ShouldNotBe(new TokenInfo());
            }

            //cross chain transfer 
            var transferTxInfos = new Dictionary<string, TransactionResultDto>();
            foreach (var contract in MainContractList)
            {
                var result = crossChainManager.CrossChainTransfer(contract.Symbol, amount, initAccount, initAccount,
                    out string raw);
                var txId = MainNodeManager.SendTransaction(raw);
                result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
                transferTxInfos.Add(raw, result);
            }

            // create input 
            var crossChainReceiveTokenInputList = new List<CrossChainReceiveTokenInput>();
            foreach (var txInfo in transferTxInfos)
            {
                var merklePath = crossChainManager.GetMerklePath(txInfo.Value.BlockNumber,
                    txInfo.Value.TransactionId, out var root);

                var crossChainCreateToken = new CrossChainReceiveTokenInput
                {
                    MerklePath = merklePath,
                    FromChainId = mainChainId,
                    ParentChainHeight = txInfo.Value.BlockNumber,
                    TransferTransactionBytes = ByteStringHelper.FromHexString(txInfo.Key)
                };
                crossChainReceiveTokenInputList.Add(crossChainCreateToken);
            }

            //check last transaction index 
            last = transferTxInfos.Last();
            crossChainManager.CheckSideChainIndexMainChain(last.Value.BlockNumber);

            //side chain receive 
            foreach (var crossChainReceiveTokenInput in crossChainReceiveTokenInputList)
            {
                var result =
                    sideToken.ExecuteMethodWithResult(TokenMethod.CrossChainReceiveToken, crossChainReceiveTokenInput);
                result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            }

            //transfer token to other accounts 
            foreach (var main in MainContractList)
            {
                var contract = new ContractInfo(initAccount, SystemTokenAddress);
                var symbol = main.Symbol;
                contract.Symbol = symbol;
                ContractList.Add(contract);
            }

            var transferAmount = 10_0000_0000_00000000L / AccountList.Count;
            foreach (var contract in ContractList)
            {
                var symbol = contract.Symbol;
                foreach (var user in AccountList)
                {
                    if (user.Account.Equals(initAccount)) continue;
                    var transactionId = sideToken.ExecuteMethodWithTxId(TokenMethod.Transfer, new TransferInput
                    {
                        Amount = transferAmount,
                        Memo = $"transfer balance - {Guid.NewGuid()}",
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
                foreach (var user in AccountList)
                {
                    var rd = CommonHelper.GenerateRandomNumber(1, 6);
                    if (rd != 5) continue;
                    //verify token
                    var balance = token.GetUserBalance(user.Account, symbol);
                    if (balance == transferAmount)
                        Logger.Info($"Issue token {symbol} to '{user.Account}' with amount {transferAmount} success.");
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

        public void ExecuteContinuousRoundsTransactionsTask(bool useTxs = false)
        {
            //add transaction performance check process
            var nodeTransactionOption = RpcConfig.ReadInformation.NodeTransactionOption;
            var max = nodeTransactionOption.MaxTransactionSelect;
            var min = nodeTransactionOption.MinTransactionSelect;
            var testers = AccountList.Select(o => o.Account).ToList();
            var bps = NodeInfoHelper.Config.Nodes.Select(o => o.Account);
            var enumerable = bps as string[] ?? bps.ToArray();
            var setAccount = enumerable.First();
            var cts = new CancellationTokenSource();
            var token = cts.Token;
            var taskList = new List<Task>
            {
                Task.Run(() => Summary.ContinuousCheckTransactionPerformance(token), token),
                Task.Run(() => TokenMonitor.ExecuteTokenCheckTask(testers, token), token),
                Task.Run(() => GeneratedTransaction(useTxs, cts, token), token),
                Task.Run(() =>
                {
                    var transactionExecuteLimit = new TransactionExecuteLimit(NodeManager,setAccount );
                    if (transactionExecuteLimit.WhetherUpdateLimit())
                    {
                        var index = 0;
                        for (int i = 1; i > 0; i++)
                        {
                            if (index > max - min)
                                index = 0;
                            Thread.Sleep(300000);
                            transactionExecuteLimit.UpdateExecutionSelectTransactionLimit(index);
                            index++;
                        }
                    }
                },token),
                Task.Run(() =>
                {
                    for (int i = 1; i > 0; i++)
                    {
                        var transactionsList = new List<Dictionary<string, List<string>>>();
                        while (transactionsList.Count == 0)
                        {
                            Thread.Sleep(1000);
                            transactionsList = GetTransactions();
                        }

                        Logger.Info($"Check transaction result {i}:");
                        var txsTasks = transactionsList
                            .Select(transactions => Task.Run(() => CheckTransaction(transactions), token)).ToList();
                        Task.WaitAll(txsTasks.ToArray<Task>());
                    }
                }, token)
            };

            Task.WaitAll(taskList.ToArray<Task>());
        }

        private void GetTestAccounts(int count)
        {
            var accounts = NodeManager.ListAccounts();
            if (accounts.Count >= count)
            {
                foreach (var acc in accounts.Take(count)) AccountList.Add(new AccountInfo(acc));
            }
            else
            {
                foreach (var acc in accounts) AccountList.Add(new AccountInfo(acc));

                var generateCount = count - accounts.Count;
                for (var i = 0; i < generateCount; i++)
                {
                    var account = NodeManager.NewAccount();
                    AccountList.Add(new AccountInfo(account));
                }
            }
        }

        private void GeneratedTransaction(bool useTxs, CancellationTokenSource cts, CancellationToken token)
        {
            Logger.Info("Begin generate multi requests.");
            var enableRandom = RpcConfig.ReadInformation.EnableRandomTransaction;
            var randomTransactionOption = RpcConfig.ReadInformation.RandomEndpointOption;
            QueueTransaction = new Queue<List<Dictionary<string, List<string>>>>();
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

                            QueueTransaction.Enqueue(txsTasks);
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
        }

        private List<Dictionary<string, List<string>>> GetTransactions()
        {
            if (QueueTransaction.Count >= 1000)
                QueueTransaction.Clear();
            if (QueueTransaction.Count == 0)
                return new List<Dictionary<string, List<string>>>();
            var transactionLists = QueueTransaction.Dequeue();
            return transactionLists;
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
                var checkTimes = randomTransactionOption.EndpointList.Count;
                while (!NodeManager.UpdateApiUrl(serviceUrl) && checkTimes > 0)
                {
                    var errorUrlIndex = randomTransactionOption.EndpointList.IndexOf(serviceUrl.Replace("http://", ""));
                    var nextIndex = errorUrlIndex == randomTransactionOption.EndpointList.Count - 1
                        ? 0
                        : errorUrlIndex + 1;
                    serviceUrl = randomTransactionOption.GetRandomEndpoint(nextIndex);
                    checkTimes--;
                }

                try
                {
                    var transactionPoolCount =
                        AsyncHelper.RunSync(NodeManager.ApiClient.GetTransactionPoolStatusAsync).Validated;
                    if (transactionPoolCount > maxLimit)
                    {
                        Thread.Sleep(5000);
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

        private void CrossTransferToInitAccount()
        {
            var bps = NodeInfoHelper.Config.Nodes;
            var initAccount = bps.First().Account;
            var isSideChain = RpcConfig.ReadInformation.ChainTypeOption.IsSideChain;
            if (!isSideChain) return;
            var mainUrl = RpcConfig.ReadInformation.ChainTypeOption.MainChainUrl;
            MainNodeManager = new NodeManager(mainUrl);
            var mainChainId = ChainHelper.ConvertBase58ToChainId(MainNodeManager.GetChainId());
            var primaryToken = MainNodeManager.GetPrimaryTokenSymbol();
            var crossChainManager = new CrossChainManager(MainNodeManager, NodeManager, initAccount);
            if (crossChainManager.CheckPrivilegePreserved()) return;

            var token = new TokenContract(NodeManager, initAccount, SystemTokenAddress);
            var initBalance = token.GetUserBalance(initAccount, primaryToken);
            if (initBalance > 8000_0000_00000000) return;
            Logger.Info($"{initAccount} balance is {initBalance}, need cross transfer first");

            //cross chain transfer 
            var amount = 10000_0000_00000000;
            var result =
                crossChainManager.CrossChainTransfer(primaryToken, amount, initAccount, initAccount, out string raw);
            var txId = MainNodeManager.SendTransaction(raw);
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            // create input 
            var merklePath = crossChainManager.GetMerklePath(result.BlockNumber,
                txId, out var root);

            var crossChainCreateToken = new CrossChainReceiveTokenInput
            {
                MerklePath = merklePath,
                FromChainId = mainChainId,
                ParentChainHeight = result.BlockNumber,
                TransferTransactionBytes = ByteString.CopyFrom(ByteArrayHelper.HexStringToByteArray(raw))
            };

            //check last transaction index 
            crossChainManager.CheckSideChainIndexMainChain(result.BlockNumber);

            //side chain receive 

            var receiveResult =
                token.ExecuteMethodWithResult(TokenMethod.CrossChainReceiveToken, crossChainCreateToken);
            receiveResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            initBalance = token.GetUserBalance(initAccount, primaryToken);
            Logger.Info($"{initAccount} balance is {initBalance}");
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
                    To = to.ConvertAddress()
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
                var (from, to) = GetTransferPair(token, symbol);

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

        private void CheckTransaction(Dictionary<string, List<string>> transactionsWithRpc)
        {
            if (transactionsWithRpc.Count < 1) return;
            var rpc = transactionsWithRpc.Keys.First();
            if (!NodeManager.ApiClient.BaseUrl.Equals(rpc))
                NodeManager.UpdateApiUrl(rpc);
            Logger.Info($"Check transaction, the first transaction: {transactionsWithRpc[rpc].First()}");
            var txIds = new List<string>();
            foreach (var txId in transactionsWithRpc[rpc])
            {
                var transactionResult = AsyncHelper.RunSync(() => ApiClient.GetTransactionResultAsync(txId));
                var status = transactionResult.Status.ConvertTransactionResultStatus();
                if (status.Equals(TransactionResultStatus.NotExisted))
                    txIds.Add(txId);
            }

            transactionsWithRpc[rpc] = txIds;
            if (transactionsWithRpc[rpc].Count < 1) return;
            foreach (var transaction in transactionsWithRpc[rpc])
            {
                Logger.Warn($"NotExisted transaction : {transaction}");
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
                if (AccountList[toId].Account == from) continue;
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

            var result = Monitor.CheckTransactionPoolStatus(LimitTransaction);
            if (!result)
            {
                Logger.Warn("Transaction pool transactions over limited, canceled this round execution.");
                return;
            }

            for (var i = 0; i < times; i++)
            {
                var (from, to) = GetTransferPair(token, symbol);

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
        public INodeManager MainNodeManager { get; private set; }
        public CrossChainManager CrossChainManager { get; set; }

        public AElfClient ApiClient => NodeManager.ApiClient;
        private ExecutionSummary Summary { get; set; }
        private NodeStatusMonitor Monitor { get; set; }
        private TesterTokenMonitor TokenMonitor { get; set; }
        public string BaseUrl { get; }
        private string SystemTokenAddress { get; set; }
        private List<AccountInfo> AccountList { get; }
        private string KeyStorePath { get; }
        private List<ContractInfo> MainContractList { get; }

        private List<ContractInfo> ContractList { get; }
        private List<string> TxIdList { get; }

        private new Queue<List<Dictionary<string, List<string>>>> QueueTransaction { get; set; }

        public int ThreadCount { get; }
        public int ExeTimes { get; }
        public bool LimitTransaction { get; }
        private ConcurrentQueue<string> GenerateTransactionQueue { get; }
        private static readonly ILog Logger = Log4NetHelper.GetLogger();
        private static readonly Random RandomGen = new Random(DateTime.Now.Millisecond);

        #endregion
    }
}