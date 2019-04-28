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
using AElf.Kernel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ProtoBuf;

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

        public RpcApiHelper CH { get; set; }
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
            CH = new RpcApiHelper(RpcUrl, KeyStorePath);

            //Connect Chain
            var ci = new CommandInfo(ApiMethods.GetChainInformation);
            CH.ExecuteCommand(ci);
            Assert.IsTrue(ci.Result, "Connect chain got exception.");

            //New
            NewAccounts(200);

            //Unlock Account
            UnlockAllAccounts(ThreadCount);
        }

        public void CheckNodeStatus()
        {
            for (int i = 0; i < 10; i++)
            {
                var ci = new CommandInfo(ApiMethods.GetBlockHeight);
                CH.ExecuteCommand(ci);
                ci.GetJsonInfo();
                var result = ci.JsonInfo;
                string countStr = result["result"].ToString();
                int currentHeight = Int32.Parse(countStr);

                if (BlockHeight != currentHeight)
                {
                    BlockHeight = currentHeight;
                    Logger.WriteInfo("Current block height: {0}", BlockHeight);
                    return;
                }

                Thread.Sleep(3000);
                Logger.WriteWarn("Block height not changed round: {0}", i);
            }
            Assert.IsTrue(false, "Node block exception, block height not increased anymore.");
        }

        public void DeployContract()
        {
            List<object> contractList = new List<object>();
            for (int i = 0; i < ThreadCount; i++)
            {
                dynamic info = new System.Dynamic.ExpandoObject();
                info.Id = i;
                info.Account = AccountList[i].Account;

                var ci = new CommandInfo(ApiMethods.DeploySmartContract);
                ci.Parameter = $"AElf.Contracts.MultiToken {AccountList[i].Account}";
                CH.ExecuteCommand(ci);
                Assert.IsTrue(ci.Result);

                ci.GetJsonInfo();
                string genesisContract = ci.JsonInfo["TransactionId"].ToString();
                Assert.AreNotEqual(string.Empty, genesisContract);
                info.TxId = genesisContract;
                info.Result = false;
                contractList.Add(info);
            }

            int count = 0;
            int checkTimes = 30;

            while (checkTimes>0)
            {
                checkTimes--;
                Thread.Sleep(2000);
                foreach (dynamic item in contractList)
                {
                    if (item.Result == false)
                    {
                        var ci = new CommandInfo(ApiMethods.GetTransactionResult);
                        ci.Parameter = item.TxId;
                        CH.ExecuteCommand(ci);
                        Assert.IsTrue(ci.Result);
                        ci.GetJsonInfo();
                        ci.JsonInfo = ci.JsonInfo;
                        string deployResult = ci.JsonInfo["result"]["Status"].ToString();

                        if (deployResult == "Mined")
                        {
                            count++;
                            item.Result = true;
                            string contractPath= ci.JsonInfo["result"]["ReadableReturnValue"].ToString().Replace("\"","");
                            ContractList.Add(new Contract(item.Id, contractPath));
                            AccountList[item.Id].Increment = 1;
                        }else if (deployResult == "Failed")
                        {
                            Logger.WriteError("Transaction failed.");
                            var transactionResultArray = Encoding.Unicode.GetBytes(ci.JsonInfo["result"].ToString());
                            MemoryStream ms = new MemoryStream(transactionResultArray);
                            var transactionResult = Serializer.Deserialize<TransactionResult>(ms);
                            ms.Dispose();
                        }

                        Thread.Sleep(10);
                    }
                }

                if (count == contractList.Count)
                    return;
            }
            Assert.IsFalse(true, "Deployed contract not executed successfully.");
        }

        public void InitializeContract()
        {
            //create all token
            for (int i = 0; i < ContractList.Count; i++)
            {
                var account = AccountList[ContractList[i].AccountId].Account;
                var contractPath = ContractList[i].ContractPath;

                var symbol = $"ELF{RandomString(4, false)}";
                ContractList[i].Symbol = symbol;
                var ci = new CommandInfo(ApiMethods.BroadcastTransaction, account, contractPath, "Create");
                ci.ParameterInput = new CreateInput
                {
                    Symbol = symbol,
                    TokenName = $"elf token {GetRandomIncrementId()}",
                    TotalSupply = long.MaxValue,
                    Decimals = 2,
                    Issuer = Address.Parse(account),
                    IsBurnable = true
                };
                CH.ExecuteCommand(ci);
                Assert.IsTrue(ci.Result);
                ci.GetJsonInfo();

                string transactionId = ci.JsonInfo["TransactionId"].ToString();
                Assert.AreNotEqual(string.Empty, transactionId);
                TxIdList.Add(transactionId);
            }

            CheckResultStatus(TxIdList);
            
            //issue all token
            for (int i = 0; i < ContractList.Count; i++)
            {
                var account = AccountList[ContractList[i].AccountId].Account;
                var contractPath = ContractList[i].ContractPath;
                var symbol = ContractList[i].Symbol;
                
                var ci = new CommandInfo(ApiMethods.BroadcastTransaction, account, contractPath, "Issue");
                ci.ParameterInput = new IssueInput()
                {
                    Amount = long.MaxValue,
                    Memo = "Issue all balance to owner.",
                    Symbol = symbol,
                    To = Address.Parse(account)
                };
                CH.ExecuteCommand(ci);
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
            List<Task> contractTasks = new List<Task>();
            for (int i = 0; i < ThreadCount; i++)
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
            Stopwatch exec = new Stopwatch();
            exec.Start();
            List<Task> contractTasks = new List<Task>();
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

        //Without conflict group category
        public void DoContractCategory(int threadNo, int times)
        {
            string account = AccountList[ContractList[threadNo].AccountId].Account;
            string abiPath = ContractList[threadNo].ContractPath;

            HashSet<int> set = new HashSet<int>();
            List<string> txIdList = new List<string>();
            int passCount = 0;
            for (int i = 0; i < times; i++)
            {
                Random rd = new Random(DateTime.Now.Millisecond);
                int randNumber = rd.Next(ThreadCount, AccountList.Count);
                int countNo = randNumber;
                set.Add(countNo);
                string account1 = AccountList[countNo].Account;
                AccountList[countNo].Increment++;

                //Execute Transfer
                var ci = new CommandInfo(ApiMethods.BroadcastTransaction, account, abiPath, "Transfer");
                ci.ParameterInput = new TransferInput
                {
                    Symbol = ContractList[threadNo].Symbol,
                    Amount = (i + 1) % 4 + 1,
                    Memo = $"transfer test - {Guid.NewGuid()}",
                    To = Address.Parse(account1)
                };
                CH.ExecuteCommand(ci);

                if (ci.Result)
                {
                    ci.GetJsonInfo();
                    txIdList.Add(ci.JsonInfo["TransactionId"].ToString());
                    passCount++;
                }

                Thread.Sleep(10);
                //Get Balance Info
                ci = new CommandInfo(ApiMethods.BroadcastTransaction, account, abiPath, "GetBalance");
                ci.ParameterInput = new GetBalanceInput
                {
                    Symbol = ContractList[threadNo].Symbol,
                    Owner = Address.Parse(account)
                };
                CH.ExecuteCommand(ci);

                if (ci.Result)
                {
                    Assert.IsTrue(ci.Result);
                    ci.GetJsonInfo();
                    Logger.WriteInfo(ci.InfoMsg[0]);
                    passCount++;
                }

                Thread.Sleep(10);
            }

            Logger.WriteInfo("Total contract sent: {0}, passed number: {1}", 2 * times, passCount);
            txIdList.Reverse();
            CheckResultStatus(txIdList);
            Logger.WriteInfo("{0} Transfer from Address {1}", set.Count, account);
        }

        public void GenerateContractList(int threadNo, int times)
        {
            string account = AccountList[ContractList[threadNo].AccountId].Account;
            string abiPath = ContractList[threadNo].ContractPath;

            HashSet<int> set = new HashSet<int>();

            List<string> rpcRequest = new List<string>();
            for (int i = 0; i < times; i++)
            {
                Random rd = new Random(DateTime.Now.Millisecond);
                int randNumber = rd.Next(ThreadCount, AccountList.Count);
                int countNo = randNumber;
                set.Add(countNo);
                string account1 = AccountList[countNo].Account;
                AccountList[countNo].Increment++;

                //Execute Transfer
                var ci = new CommandInfo(ApiMethods.BroadcastTransaction, account, abiPath, "Transfer");
                ci.ParameterInput = new TransferInput
                {
                    Symbol = ContractList[threadNo].Symbol,
                    To = Address.Parse(account1),
                    Amount = (i+1) % 4 + 1,
                    Memo = $"transfer test - {Guid.NewGuid()}"
                };
                string requestInfo = CH.RpcGenerateTransactionRawTx(ci);
                rpcRequest.Add(requestInfo);

                //Get Balance Info
                ci = new CommandInfo(ApiMethods.BroadcastTransaction, account, abiPath, "GetBalance");
                ci.ParameterInput = new GetBalanceInput
                {
                    Symbol = ContractList[threadNo].Symbol,
                    Owner = Address.Parse(account)
                };
                requestInfo = CH.RpcGenerateTransactionRawTx(ci);
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
            CH.ExecuteCommand(ci1);
            Assert.IsTrue(ci1.Result);
            var result = ci1.InfoMsg[0].Replace("[", "").Replace("]", "").Split(",");
            Logger.WriteInfo("Batch request count: {0}, Pass count: {1} at {2}", rpcRequest.Count, result?.Length,
                DateTime.Now.ToString("HH:mm:ss.fff"));
            Logger.WriteInfo("Thread [{0}] completeed executed {1} times contracts work at {2}.", threadNo, times,
                DateTime.Now.ToString(CultureInfo.InvariantCulture));
            Logger.WriteInfo("{0} Transfer from Address {1}", set.Count, account);
            Thread.Sleep(100);
        }

        public void GenerateRpcList(int round, int threadNo, int times)
        {
            string account = AccountList[ContractList[threadNo].AccountId].Account;
            string abiPath = ContractList[threadNo].ContractPath;

            HashSet<int> set = new HashSet<int>();
            for (int i = 0; i < times; i++)
            {
                Random rd = new Random(DateTime.Now.Millisecond);
                int randNumber = rd.Next(ThreadCount, AccountList.Count);
                int countNo = randNumber;
                set.Add(countNo);
                string account1 = AccountList[countNo].Account;
                AccountList[countNo].Increment++;

                //Execute Transfer
                var ci = new CommandInfo(ApiMethods.BroadcastTransaction, account, abiPath, "Transfer");
                ci.ParameterInput = new TransferInput
                {
                    Symbol = ContractList[threadNo].Symbol,
                    To = Address.Parse(account1),
                    Amount = (i + 1) % 4 + 1,
                    Memo = $"transfer test - {Guid.NewGuid()}"
                };
                string requestInfo = CH.RpcGenerateTransactionRawTx(ci);
                ContractRpcList.Enqueue(requestInfo);

                //Get Balance Info
                ci = new CommandInfo(ApiMethods.BroadcastTransaction, account, abiPath, "GetBalance");
                ci.ParameterInput = new GetBalanceInput
                {
                    Symbol = ContractList[threadNo].Symbol,
                    Owner = Address.Parse(account)
                };
                requestInfo = CH.RpcGenerateTransactionRawTx(ci);
                ContractRpcList.Enqueue(requestInfo);
            }
        }

        public void ExecuteOneRpcTask(int group)
        {
            while (true)
            {
                if (!ContractRpcList.TryDequeue(out var rpcMsg))
                    break;
                Logger.WriteInfo("Transaction group: {0}, execution left: {1}", group+1, ContractRpcList.Count);
                var ci = new CommandInfo(ApiMethods.BroadcastTransaction);
                ci.Parameter = rpcMsg;
                CH.ExecuteCommand(ci);
                Thread.Sleep(100);
            }
        }

        public void ExecuteMultiRpcTask(bool useTxs = false)
        {
            Logger.WriteInfo("Begin generate multi rpc requests.");
            for (int r = 1; r > 0; r++) //continuous running
            {
                Logger.WriteInfo("Execution transaction rpc request round: {0}", r);
                if (useTxs)
                {
                    //multi task for BroadcastTransactions query
                    var txsTasks = new List<Task>();
                    for (int i = 0; i < ThreadCount; i++)
                    {
                        var j = i;
                        txsTasks.Add(Task.Run(() => GenerateContractList(j, ExeTimes)));
                    }

                    Task.WaitAll(txsTasks.ToArray<Task>());
                }
                else
                {
                    //multi task for BroadcastTransaction query
                    for (int i = 0; i < ThreadCount; i++)
                    {
                        var j = i;
                        //Generate Rpc contracts
                        GenerateRpcList(r, j, ExeTimes);
                        //Send Rpc contracts request
                        Logger.WriteInfo("Begin execute group {0} transactions with 4 threads.", j+1);
                        List<Task> txTasks = new List<Task>();
                        for (int k = 0; k < ThreadCount; k++)
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
                string file = Path.Combine(KeyStorePath, $"{item.Account}.ak");
                File.Delete(file);
            }
        }
        
        public void PrintContractInfo()
        {
            Logger.WriteInfo("Execution account and contract address information:");
            int count = 0;
            foreach (var item in ContractList)
            {
                count++;
                Logger.WriteInfo("{0:00}. Account: {1}, ContractPath:{2}", count, AccountList[item.AccountId].Account,
                    item.ContractPath);
            }
        }

        #region Private Method

        private void CheckResultStatus(List<string> idList, int checkTimes = 60)
        {
            if(checkTimes<0)
                Assert.IsTrue(false, "Transaction status check is over time.");
            checkTimes--;
            int listCount = idList.Count;
            Thread.Sleep(2000);
            int length = idList.Count;
            for (int i = length - 1; i >= 0; i--)
            {
                var ci = new CommandInfo(ApiMethods.GetTransactionResult);
                ci.Parameter = idList[i];
                CH.ExecuteCommand(ci);

                if (ci.Result)
                {
                    ci.GetJsonInfo();
                    string deployResult = ci.JsonInfo["result"]["Status"].ToString();
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
                var ci = new CommandInfo(ApiMethods.GetTransactionResult);
                ci.Parameter = idList[0];
                CH.ExecuteCommand(ci);

                if (ci.Result)
                {
                    ci.GetJsonInfo();
                    string deployResult = ci.JsonInfo["result"]["Status"].ToString();
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
            for (int i = 0; i < count; i++)
            {
                var ci = new CommandInfo(ApiMethods.AccountUnlock);
                ci.Parameter = $"{AccountList[i].Account} 123 notimeout";
                ci = CH.ExecuteCommand(ci);
                Assert.IsTrue(ci.Result);
            }
        }

        private void NewAccounts(int count)
        {
            for (int i = 0; i < count; i++)
            {
                var ci = new CommandInfo(ApiMethods.AccountNew);
                ci.Parameter = "123";
                ci = CH.ExecuteCommand(ci);
                Assert.IsTrue(ci.Result);
                AccountList.Add(new AccountInfo(ci.InfoMsg?[0].Replace("Account address:", "").Trim()));
            }
        }

        private void GetExecutedAccount()
        {
            var accounts = AccountList.FindAll(x => x.Increment != 0);
            int count = 0;
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
                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "aelf");
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);

                string keyPath = Path.Combine(path, "keys");
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
            StringBuilder builder = new StringBuilder(size);
            int startChar = lowerCase ? 97 : 65;//65 = A / 97 = a
            for (int i = 0; i < size; i++)
                builder.Append((char)(26 * random.NextDouble() + startChar));
            return builder.ToString();
        }
        #endregion
    }
}