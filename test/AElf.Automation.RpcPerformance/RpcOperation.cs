using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AElf.Automation.Common.Contracts;
using AElf.Automation.Common.Helpers;
using AElf.Contracts.MultiToken.Messages;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AElf.Automation.RpcPerformance
{
    public class AccountInfo
    {
        public string Account { get; set; }
        public int Increment { get; set; }

        public AccountInfo(string account)
        {
            Account = account;
            Increment = 0;
        }
    }

    public class Contract
    {
        public string ContractPath { get; set; }
        public string Symbol { get; set; }
        public int AccountId { get; set; }

        public Contract(int accId, string contractPath)
        {
            AccountId = accId;
            ContractPath = contractPath;
        }
        
        public Contract(int accId, string contractPath, string symbol)
        {
            AccountId = accId;
            ContractPath = contractPath;
            Symbol = symbol;
        }
    }

    public class RpcOperation
    {
        #region Public Property

        public RpcApiHelper ApiHelper { get; set; }
        public string RpcUrl { get; set; }
        public List<AccountInfo> AccountList { get; set; }
        public string KeyStorePath { get; set; }
        public string TokenAbi { get; set; }
        public int BlockHeight { get; set; }
        public List<Contract> ContractList { get; set; }
        public List<string> TxIdList { get; set; }
        public int ThreadCount { get; set; }
        public int ExeTimes { get; set; }
        public ConcurrentQueue<string> ContractRpcList { get; set; }
        public readonly ILogHelper Logger = LogHelper.GetLogHelper();

        public TokenContract TokenService { get; set; }
        #endregion

        public RpcOperation(int threadCount,
            int exeTimes,
            string rpcUrl = "http://127.0.0.1:8000/chain",
            string keyStorePath = "")
        {
            if (keyStorePath == "")
                keyStorePath = GetDefaultDataDir();

            AccountList = new List<AccountInfo>();
            ContractList = new List<Contract>();
            ContractRpcList = new ConcurrentQueue<string>();
            TxIdList = new List<string>();
            ThreadCount = threadCount;
            BlockHeight = 0;
            ExeTimes = exeTimes;
            KeyStorePath = keyStorePath;
            RpcUrl = rpcUrl.Contains("chain")? rpcUrl : $"{rpcUrl}/chain";
        }

        public void InitExecRpcCommand()
        {
            Logger.WriteInfo("Rpc Url: {0}", RpcUrl);
            Logger.WriteInfo("Key Store Path: {0}", Path.Combine(KeyStorePath, "keys"));
            Logger.WriteInfo("Prepare new and unlock accounts.");
            ApiHelper = new RpcApiHelper(RpcUrl, KeyStorePath);

            //Connect Chain
            var ci = new CommandInfo(ApiMethods.GetChainInformation);
            ApiHelper.ExecuteCommand(ci);
            Assert.IsTrue(ci.Result, "Connect chain got exception.");

            //New
            NewAccounts(200);

            //Unlock Account
            UnlockAllAccounts(ThreadCount);
        }
        public void DeployContract()
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

                ci.GetJsonInfo();
                var txId = ci.JsonInfo["TransactionId"].ToString();
                Assert.AreNotEqual(string.Empty, txId);
                info.TxId = txId;
                info.Result = false;
                contractList.Add(info);
            }

            var count = 0;
            var checkTimes = 30;

            while (checkTimes>0)
            {
                checkTimes--;
                Thread.Sleep(2000);
                foreach (dynamic item in contractList)
                {
                    if (item.Result != false) continue;
                    
                    var ci = new CommandInfo(ApiMethods.GetTransactionResult) {Parameter = item.TxId};
                    ApiHelper.ExecuteCommand(ci);
                    Assert.IsTrue(ci.Result);
                    ci.GetJsonInfo();
                    var deployResult = ci.JsonInfo["result"]["Status"].ToString();

                    if (deployResult == "Mined")
                    {
                        count++;
                        item.Result = true;
                        var contractPath= ci.JsonInfo["result"]["ReadableReturnValue"].ToString().Replace("\"","");
                        ContractList.Add(new Contract(item.Id, contractPath));
                        AccountList[item.Id].Increment = 1;
                    }else if (deployResult == "Failed")
                    {
                        Logger.WriteError("Transaction failed.");
                        Logger.WriteError(ci.JsonInfo.ToString());
                    }

                    Thread.Sleep(10);
                }

                if (count == contractList.Count)
                    return;
            }
            Assert.IsFalse(true, "Deployed contract not executed successfully.");
        }
        public void InitializeContract()
        {
            //create all token
            foreach (var contract in ContractList)
            {
                var account = AccountList[contract.AccountId].Account;
                var contractPath = contract.ContractPath;

                var symbol = $"ELF{RandomString(4, false)}";
                contract.Symbol = symbol;
                var ci = new CommandInfo(ApiMethods.BroadcastTransaction, account, contractPath, "Create")
                {
                    ParameterInput = new CreateInput
                    {
                        Symbol = symbol,
                        TokenName = $"elf token {GetRandomIncrementId()}",
                        TotalSupply = long.MaxValue,
                        Decimals = 2,
                        Issuer = Address.Parse(account),
                        IsBurnable = true
                    }
                };
                ApiHelper.ExecuteCommand(ci);
                Assert.IsTrue(ci.Result);
                ci.GetJsonInfo();

                var transactionId = ci.JsonInfo["TransactionId"].ToString();
                Assert.AreNotEqual(string.Empty, transactionId);
                TxIdList.Add(transactionId);
            }

            CheckResultStatus(TxIdList);
            
            //issue all token
            foreach (var contract in ContractList)
            {
                var account = AccountList[contract.AccountId].Account;
                var contractPath = contract.ContractPath;
                var symbol = contract.Symbol;

                var ci = new CommandInfo(ApiMethods.BroadcastTransaction, account, contractPath, "Issue")
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
                ci.GetJsonInfo();
                
                var transactionId = ci.JsonInfo["TransactionId"].ToString();
                Assert.AreNotEqual(string.Empty, transactionId);
                TxIdList.Add(transactionId);
            }
            
            CheckResultStatus(TxIdList);
        }
        public void ExecuteContracts()
        {
            Logger.WriteInfo("Start contract execution at: {0}", DateTime.Now.ToString(CultureInfo.InvariantCulture));
            Stopwatch exec = new Stopwatch();
            exec.Start();
            var contractTasks = new List<Task>();
            for (var i = 0; i < ThreadCount; i++)
            {
                var j = i;
                contractTasks.Add(Task.Run(() => DoContractCategory(j, ExeTimes)));
            }

            Task.WaitAll(contractTasks.ToArray<Task>());
            exec.Stop();
            Logger.WriteInfo("End contract execution at: {0}", DateTime.Now.ToString(CultureInfo.InvariantCulture));
            Logger.WriteInfo("Execution time: {0}", exec.ElapsedMilliseconds);
            GetExecutedAccount();
        }
        public void ExecuteContractsRpc()
        {
            Logger.WriteInfo("Start all generate rpc request at: {0}", DateTime.Now.ToString(CultureInfo.InvariantCulture));
            var exec = new Stopwatch();
            exec.Start();
            var contractTasks = new List<Task>();
            for (int i = 0; i < ThreadCount; i++)
            {
                var j = i;
                contractTasks.Add(Task.Run(() =>
                {
                    try
                    {
                        GenerateContractList(j, ExeTimes);
                    }
                    catch (Exception e)
                    {
                        Logger.WriteInfo($"Execute batch transaction got exception, message details are: {e.Message}");
                    }
                }));
            }

            Task.WaitAll(contractTasks.ToArray<Task>());
            exec.Stop();
            Logger.WriteInfo("All rpc requests completed at: {0}", DateTime.Now.ToString(CultureInfo.InvariantCulture));
            Logger.WriteInfo("Execution time: {0}", exec.ElapsedMilliseconds);
        }
        public void ExecuteMultiRpcTask(bool useTxs = false)
        {
            Logger.WriteInfo("Begin generate multi rpc requests.");
            for (var r = 1; r > 0; r++) //continuous running
            {
                Logger.WriteInfo("Execution transaction rpc request round: {0}", r);
                if (useTxs)
                {
                    //multi task for BroadcastTransactions query
                    var txsTasks = new List<Task>();
                    for (var i = 0; i < ThreadCount; i++)
                    {
                        var j = i;
                        txsTasks.Add(Task.Run(() => GenerateContractList(j, ExeTimes)));
                    }

                    Task.WaitAll(txsTasks.ToArray<Task>());
                }
                else
                {
                    //multi task for BroadcastTransaction query
                    for (var i = 0; i < ThreadCount; i++)
                    {
                        var j = i;
                        //Generate Rpc contracts
                        GenerateRpcList(r, j, ExeTimes);
                        //Send Rpc contracts request
                        Logger.WriteInfo("Begin execute group {0} transactions with 4 threads.", j+1);
                        var txTasks = new List<Task>();
                        for (var k = 0; k < ThreadCount; k++)
                        {
                            txTasks.Add(Task.Run(() => ExecuteOneRpcTask(j)));
                        }

                        Task.WaitAll(txTasks.ToArray<Task>());
                    }
                }

                Thread.Sleep(1000);
                CheckNodeStatus(); //check node whether is normal
            }
        }
        public void DeleteAccounts()
        {
            foreach (var item in AccountList)
            {
                var file = Path.Combine(KeyStorePath, $"{item.Account}.ak");
                File.Delete(file);
            }
        }
        public void PrintContractInfo()
        {
            Logger.WriteInfo("Execution account and contract address information:");
            var count = 0;
            foreach (var item in ContractList)
            {
                count++;
                Logger.WriteInfo("{0:00}. Account: {1}, ContractPath:{2}", count, AccountList[item.AccountId].Account,
                    item.ContractPath);
            }
        }

        #region Private Method
        
        //Without conflict group category
        private void DoContractCategory(int threadNo, int times)
        {
            var account = AccountList[ContractList[threadNo].AccountId].Account;
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
                AccountList[countNo].Increment++;

                //Execute Transfer
                var ci = new CommandInfo(ApiMethods.BroadcastTransaction, account, abiPath, "Transfer")
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
                    ci.GetJsonInfo();
                    txIdList.Add(ci.JsonInfo["TransactionId"].ToString());
                    passCount++;
                }

                Thread.Sleep(10);
                //Get Balance Info
                ci = new CommandInfo(ApiMethods.BroadcastTransaction, account, abiPath, "GetBalance")
                {
                    ParameterInput = new GetBalanceInput
                    {
                        Symbol = ContractList[threadNo].Symbol, Owner = Address.Parse(account)
                    }
                };
                ApiHelper.ExecuteCommand(ci);

                if (ci.Result)
                {
                    Assert.IsTrue(ci.Result);
                    ci.GetJsonInfo();
                    Logger.WriteInfo(ci.InfoMsg[0].ToString());
                    passCount++;
                }

                Thread.Sleep(10);
            }

            Logger.WriteInfo("Total contract sent: {0}, passed number: {1}", 2 * times, passCount);
            txIdList.Reverse();
            CheckResultStatus(txIdList);
            Logger.WriteInfo("{0} Transfer from Address {1}", set.Count, account);
        }

        private void GenerateContractList(int threadNo, int times)
        {
            var account = AccountList[ContractList[threadNo].AccountId].Account;
            var abiPath = ContractList[threadNo].ContractPath;

            var set = new HashSet<int>();

            var rpcRequest = new List<string>();
            for (var i = 0; i < times; i++)
            {
                var rd = new Random(DateTime.Now.Millisecond);
                var randNumber = rd.Next(ThreadCount, AccountList.Count);
                var countNo = randNumber;
                set.Add(countNo);
                var account1 = AccountList[countNo].Account;
                AccountList[countNo].Increment++;

                //Execute Transfer
                var ci = new CommandInfo(ApiMethods.BroadcastTransaction, account, abiPath, "Transfer")
                {
                    ParameterInput = new TransferInput
                    {
                        Symbol = ContractList[threadNo].Symbol,
                        To = Address.Parse(account1),
                        Amount = (i + 1) % 4 + 1,
                        Memo = $"transfer test - {Guid.NewGuid()}"
                    }
                };
                var requestInfo = ApiHelper.RpcGenerateTransactionRawTx(ci);
                rpcRequest.Add(requestInfo);

                //Get Balance Info
                ci = new CommandInfo(ApiMethods.BroadcastTransaction, account, abiPath, "GetBalance")
                {
                    ParameterInput = new GetBalanceInput
                    {
                        Symbol = ContractList[threadNo].Symbol, Owner = Address.Parse(account)
                    }
                };
                requestInfo = ApiHelper.RpcGenerateTransactionRawTx(ci);
                rpcRequest.Add(requestInfo);
            }

            Logger.WriteInfo(
                "Thread [{0}] contracts rpc list from account :{1} and contract abi: {2} generated completed.",
                threadNo, account, abiPath);
            //Send RPC Requests
            var ci1 = new CommandInfo(ApiMethods.BroadcastTransactions);
            foreach (var rpc in rpcRequest)
            {
                ci1.Parameter += "," + rpc;
            }

            ci1.Parameter = ci1.Parameter.Substring(1);
            ApiHelper.ExecuteCommand(ci1);
            Assert.IsTrue(ci1.Result);
            var result = ci1.InfoMsg[0].ToString().Replace("[", "").Replace("]", "").Split(",");
            Logger.WriteInfo("Batch request count: {0}, Pass count: {1} at {2}", rpcRequest.Count, result?.Length,
                DateTime.Now.ToString("HH:mm:ss.fff"));
            Logger.WriteInfo("Thread [{0}] completed executed {1} times contracts work at {2}.", threadNo, times,
                DateTime.Now.ToString(CultureInfo.InvariantCulture));
            Logger.WriteInfo("{0} Transfer from Address {1}", set.Count, account);
            Thread.Sleep(100);
        }

        private void GenerateRpcList(int round, int threadNo, int times)
        {
            var account = AccountList[ContractList[threadNo].AccountId].Account;
            var abiPath = ContractList[threadNo].ContractPath;

            var set = new HashSet<int>();
            for (var i = 0; i < times; i++)
            {
                var rd = new Random(DateTime.Now.Millisecond);
                var randNumber = rd.Next(ThreadCount, AccountList.Count);
                var countNo = randNumber;
                set.Add(countNo);
                var account1 = AccountList[countNo].Account;
                AccountList[countNo].Increment++;

                //Execute Transfer
                var ci = new CommandInfo(ApiMethods.BroadcastTransaction, account, abiPath, "Transfer")
                {
                    ParameterInput = new TransferInput
                    {
                        Symbol = ContractList[threadNo].Symbol,
                        To = Address.Parse(account1),
                        Amount = (i + 1) % 4 + 1,
                        Memo = $"transfer test - {Guid.NewGuid()}"
                    }
                };
                string requestInfo = ApiHelper.RpcGenerateTransactionRawTx(ci);
                ContractRpcList.Enqueue(requestInfo);

                //Get Balance Info
                ci = new CommandInfo(ApiMethods.BroadcastTransaction, account, abiPath, "GetBalance")
                {
                    ParameterInput = new GetBalanceInput
                    {
                        Symbol = ContractList[threadNo].Symbol, Owner = Address.Parse(account)
                    }
                };
                requestInfo = ApiHelper.RpcGenerateTransactionRawTx(ci);
                ContractRpcList.Enqueue(requestInfo);
            }
        }

        private void ExecuteOneRpcTask(int group)
        {
            while (true)
            {
                if (!ContractRpcList.TryDequeue(out var rpcMsg))
                    break;
                Logger.WriteInfo("Transaction group: {0}, execution left: {1}", group+1, ContractRpcList.Count);
                var ci = new CommandInfo(ApiMethods.BroadcastTransaction) {Parameter = rpcMsg};
                ApiHelper.ExecuteCommand(ci);
                Thread.Sleep(100);
            }
        }
        
        private void CheckNodeStatus()
        {
            var checkTimes = 0;
            while(true)
            {
                checkTimes++;
                var ci = new CommandInfo(ApiMethods.GetBlockHeight);
                ApiHelper.ExecuteCommand(ci);
                ci.GetJsonInfo();
                var result = ci.JsonInfo;
                var countStr = result["result"].ToString();
                var currentHeight = int.Parse(countStr);

                if (BlockHeight != currentHeight)
                {
                    BlockHeight = currentHeight;
                    Logger.WriteInfo("Current block height: {0}", BlockHeight);
                    return;
                }

                Thread.Sleep(4000);
                Logger.WriteWarn("Block height not changed round: {0}", checkTimes);
                if(checkTimes == 150)
                    Assert.IsTrue(false, "Node block exception, block height not changed 10 minutes.");
            }
        }
        
        private void CheckResultStatus(IList<string> idList, int checkTimes = 60)
        {
            if(checkTimes<0)
                Assert.IsTrue(false, "Transaction status check is over time.");
            checkTimes--;
            var listCount = idList.Count;
            Thread.Sleep(2000);
            var length = idList.Count;
            for (var i = length - 1; i >= 0; i--)
            {
                var ci = new CommandInfo(ApiMethods.GetTransactionResult) {Parameter = idList[i]};
                ApiHelper.ExecuteCommand(ci);

                if (ci.Result)
                {
                    ci.GetJsonInfo();
                    var deployResult = ci.JsonInfo["result"]["Status"].ToString();
                    if (deployResult == "Mined")
                        idList.Remove(idList[i]);
                }

                Thread.Sleep(50);
            }

            if (idList.Count > 0 && idList.Count != 1)
            {
                Logger.WriteInfo("***************** {0} ******************", idList.Count);
                if (listCount == idList.Count && checkTimes == 0)
                    Assert.IsTrue(false, "Transaction not executed successfully.");
                CheckResultStatus(idList, checkTimes);
            }

            if (idList.Count == 1)
            {
                Logger.WriteInfo("Last one: {0}", idList[0]);
                var ci = new CommandInfo(ApiMethods.GetTransactionResult) {Parameter = idList[0]};
                ApiHelper.ExecuteCommand(ci);

                if (ci.Result)
                {
                    ci.GetJsonInfo();
                    var deployResult = ci.JsonInfo["result"]["Status"].ToString();
                    if (deployResult != "Mined")
                    {
                        Thread.Sleep(50);
                        CheckResultStatus(idList, checkTimes);
                    }
                }
            }

            Thread.Sleep(50);
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
            for (var i = 0; i < count; i++)
            {
                var ci = new CommandInfo(ApiMethods.AccountNew) {Parameter = "123"};
                ci = ApiHelper.ExecuteCommand(ci);
                Assert.IsTrue(ci.Result);
                AccountList.Add(new AccountInfo(ci.InfoMsg?[0].ToString().Replace("Account address:", "").Trim()));
            }
        }

        private void GetExecutedAccount()
        {
            var accounts = AccountList.FindAll(x => x.Increment != 0);
            var count = 0;
            foreach (var item in accounts)
            {
                count++;
                Logger.WriteInfo("{0:000} Account: {1}, Execution times: {2}", count, item.Account, item.Increment);
            }
        }

        private string GetDefaultDataDir()
        {
            try
            {
                var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "aelf");
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);

                var keyPath = Path.Combine(path, "keys");
                if (!Directory.Exists(keyPath))
                    Directory.CreateDirectory(keyPath);

                return path;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private string GetRandomIncrementId()
        {
            var random = new Random(DateTime.Now.Millisecond);
            return random.Next().ToString();
        }

        private string RandomString(int size, bool lowerCase)
        {
            var random = new Random(DateTime.Now.Millisecond);
            var builder = new StringBuilder(size);
            var startChar = lowerCase ? 97 : 65;//65 = A / 97 = a
            for (var i = 0; i < size; i++)
                builder.Append((char)(26 * random.NextDouble() + startChar));
            return builder.ToString();
        }
        #endregion
    }
}