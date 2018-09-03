using AElf.Automation.Common.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.Automation.Common.Helpers;


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
        public string AbiPath { get; set; }
        public int AccountId { get; set; }
        public long Amount { get; set; }

        public Contract(int accId, string abiPath)
        {
            AccountId = accId;
            AbiPath = abiPath;
        }
    }

    public class RpcAPI
    {
        #region Public Proerty
        public CliHelper CH { get; set; }
        public string RpcUrl { get; set; }
        public List<AccountInfo> AccountList { get; set; }
        public string KeyStorePath { get; set; }
        public List<Contract> ContractList { get; set; }
        public List<string> TxIdList { get; set; }
        public int ThreadCount { get; set; }
        public int ExeTimes { get; set; }
        public ConcurrentQueue<string> ContractRpcList { get; set; }
        public ILogHelper Logger = LogHelper.GetLogHelper();
        #endregion

        public RpcAPI(int threadCount, 
            int exeTimes, 
            string rpcUrl= "http://192.168.197.34:8000", 
            string keyStorePath= "")
        {
            if (keyStorePath == "")
                keyStorePath = GetDefaultDataDir();

            AccountList = new List<AccountInfo>();
            ContractList = new List<Contract>();
            ContractRpcList = new ConcurrentQueue<string>();
            TxIdList = new List<string>();
            ThreadCount = threadCount;
            ExeTimes = exeTimes;
            RpcUrl = rpcUrl;
            KeyStorePath = Path.Combine(keyStorePath, "keys");
            Logger.WriteInfo("Rpc Url: {0}", RpcUrl);
            Logger.WriteInfo("Key Store Path: {0}", KeyStorePath);
        }

        public void PrepareEnv()
        {
            Logger.WriteInfo("Preare new and unlock accounts.");
            CH = new CliHelper(RpcUrl);
            //New
            NewAccounts(1000);
            //Unlock Account
            UnlockAllAccounts(ThreadCount);
        }

        public void InitExecRpcCommand()
        {
            //Connect Chain
            var ci = new CommandInfo("connect_chain");
            CH.ExecuteCommand(ci);
            Assert.IsTrue(ci.Result);

            //Load Contract Abi
            ci = new CommandInfo("load_contract_abi");
            CH.RpcLoadContractAbi(ci);
            Assert.IsTrue(ci.Result);
        }

        public void DeployContract()
        {
            List<object> contractList = new List<object>();
            for (int i = 0; i < ThreadCount; i++)
            {
                dynamic info = new System.Dynamic.ExpandoObject();
                info.Id = i;
                info.Account = AccountList[i].Account;
                
                var ci = new CommandInfo("deploy_contract");
                ci.Parameter = $"AElf.Benchmark.TestContract 0 {AccountList[i].Account}";
                CH.ExecuteCommand(ci);
                Assert.IsTrue(ci.Result);
                
                ci.GetJsonInfo();
                string genesisContract = ci.JsonInfo["txId"].ToString();
                Assert.AreNotEqual(string.Empty, genesisContract);
                info.TxId = genesisContract;
                info.Result = false;
                contractList.Add(info);
            }
            int count = 0;
            while(true)
            {
                Thread.Sleep(2000);
                foreach(dynamic item in contractList)
                {
                    if(item.Result == false)
                    {
                        var ci = new CommandInfo("get_tx_result");
                        ci.Parameter = item.TxId;
                        CH.ExecuteCommand(ci);
                        Assert.IsTrue(ci.Result);
                        ci.GetJsonInfo();
                        ci.JsonInfo = ci.JsonInfo;
                        string deployResult = ci.JsonInfo["result"]["result"]["tx_status"].ToString();

                        if (deployResult == "Mined")
                        {
                            count++;
                            item.Result = true;
                            string abiPath = ci.JsonInfo["result"]["result"]["return"].ToString();
                            ContractList.Add(new Contract(item.Id, abiPath));
                            AccountList[item.Id].Increment = 1;
                        }
                        Thread.Sleep(10);
                    }
                }
                if (count == contractList.Count)
                    break;
            }
        }

        public void InitializeContract()
        {
            for (int i = 0; i < ContractList.Count; i++)
            {
                string account = AccountList[ContractList[i].AccountId].Account;
                string abiPath = ContractList[i].AbiPath;

                //Get Increment
                var ci = new CommandInfo("get_increment");
                ci.Parameter = account;
                CH.ExecuteCommand(ci);
                Assert.IsTrue(ci.Result);
                ci.GetJsonInfo();
                string increNo = ci.JsonInfo["result"]["result"]["increment"].ToString();
                
                //Load Contract abi
                ci = new CommandInfo("load_contract_abi");
                ci.Parameter = abiPath;
                CH.ExecuteCommand(ci);
                Assert.IsTrue(ci.Result);
                
                //Execute contract method
                string parameterinfo = "{\"from\":\"" + account +
                                      "\",\"to\":\"" + abiPath +
                                      "\",\"method\":\"InitBalance\",\"incr\":\"" +
                                      increNo + "\",\"params\":[\"" + account + "\"]}";
                ci = new CommandInfo("broadcast_tx");
                ci.Parameter = parameterinfo;
                CH.ExecuteCommand(ci);
                Assert.IsTrue(ci.Result);
                ci.GetJsonInfo();
                
                string genesisContract = ci.JsonInfo["txId"].ToString();
                Assert.AreNotEqual(string.Empty, genesisContract);
                TxIdList.Add(genesisContract);
            }
            CheckResultStatus(TxIdList);
        }

        public void LoadAllContractAbi()
        {
            foreach(var item in ContractList)
            {
                string abiPath = item.AbiPath;
                var ci = new CommandInfo("load_contract_abi");
                ci.Parameter = abiPath;
                CH.ExecuteCommand(ci);
                Assert.IsTrue(ci.Result);
            }
        }

        public void ExecuteContracts()
        {
            Logger.WriteInfo("Start contract execution at: {0}", DateTime.Now.ToString());
            Stopwatch exec = new Stopwatch();
            exec.Start();
            List<Task> contractTasks = new List<Task>();
            for (int i=0; i< ThreadCount; i++)
            {
                var j = i;
                contractTasks.Add(Task.Run(() => DoContractCategory(j, ExeTimes)));
            }
            
            Task.WaitAll(contractTasks.ToArray<Task>());
            exec.Stop();
            Logger.WriteInfo("End contract execution at: {0}", DateTime.Now.ToString());
            Logger.WriteInfo("Execution time: {0}", exec.ElapsedMilliseconds);
            GetExecutedAccount();
        }

        public void ExecuteContractsRpc()
        {
            Logger.WriteInfo("Start all generate rpc request at: {0}", DateTime.Now.ToString());
            Stopwatch exec = new Stopwatch();
            exec.Start();
            List<Task> contractTasks = new List<Task>();
            for (int i = 0; i < ThreadCount; i++)
            {
                var j = i;
                contractTasks.Add(Task.Run(() => GenerateContractList(j, ExeTimes)));
            }

            Task.WaitAll(contractTasks.ToArray<Task>());
            exec.Stop();
            Logger.WriteInfo("All rpc requests completed at: {0}", DateTime.Now.ToString());
            Logger.WriteInfo("Execution time: {0}", exec.ElapsedMilliseconds);
        }
        
        //Without conflict group category
        public void DoContractCategory(int threadNo, int times)
        {
            string account = AccountList[ContractList[threadNo].AccountId].Account;
            string abiPath = ContractList[threadNo].AbiPath;

            //Get Increment info
            var ci = new CommandInfo("get_increment");
            ci.Parameter = account;
            CH.ExecuteCommand(ci);
            Assert.IsTrue(ci.Result);
            ci.GetJsonInfo();
            string increNo = ci.JsonInfo["result"]["result"]["increment"].ToString();

            int number = Int32.Parse(increNo);

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
                string parameterinfo = "{\"from\":\"" + account +
                              "\",\"to\":\"" + abiPath +
                              "\",\"method\":\"Transfer\",\"incr\":\"" +
                              number.ToString() + "\",\"params\":[\"" + account + "\",\"" + account1 + "\",\"1\"]}";
                ci = new CommandInfo("broadcast_tx");
                ci.Parameter = parameterinfo;
                CH.ExecuteCommand(ci);
                
                if(ci.Result)
                {
                    ci.GetJsonInfo();
                    txIdList.Add(ci.JsonInfo["txId"].ToString());
                    number++;
                    passCount++;
                }
                Thread.Sleep(20);
                //Get Balance Info
                parameterinfo = "{\"from\":\"" + account +
                                       "\",\"to\":\"" + abiPath +
                                       "\",\"method\":\"GetBalance\",\"incr\":\"" +
                                       number.ToString() + "\",\"params\":[\"" + account + "\"]}";
                ci = new CommandInfo("broadcast_tx");
                ci.Parameter = parameterinfo;
                CH.ExecuteCommand(ci);
                
                if(ci.Result)
                {
                    Assert.IsTrue(ci.Result);
                    ci.GetJsonInfo();
                    txIdList.Add(ci.JsonInfo["txId"].ToString());
                    number++;
                    passCount++;
                }
                Thread.Sleep(20);
            }
            Logger.WriteInfo("Total contract sent: {0}, passed number: {1}", 2*times, passCount);
            txIdList.Reverse();
            CheckResultStatus(txIdList);
            Logger.WriteInfo("{0} Transfer from Address {1}", set.Count, account);
        }

        public void GenerateContractList(int threadNo, int times)
        {
            string account = AccountList[ContractList[threadNo].AccountId].Account;
            string abiPath = ContractList[threadNo].AbiPath;

            //Get Increment info
            var ci = new CommandInfo("get_increment");
            ci.Parameter = account;
            CH.ExecuteCommand(ci);
            Assert.IsTrue(ci.Result);
            ci.GetJsonInfo();
            string increNo = ci.JsonInfo["result"]["result"]["increment"].ToString();
            int number = Int32.Parse(increNo);

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
                string parameterinfo = "{\"from\":\"" + account +
                              "\",\"to\":\"" + abiPath +
                              "\",\"method\":\"Transfer\",\"incr\":\"" +
                              number.ToString() + "\",\"params\":[\"" + account + "\",\"" + account1 + "\",\"1\"]}";
                ci = new CommandInfo("broadcast_tx");
                ci.Parameter = parameterinfo;
                string requestInfo = CH.RpcGenerateTransactionRawTx(ci);
                rpcRequest.Add(requestInfo);
                number++;

                //Get Balance Info
                parameterinfo = "{\"from\":\"" + account +
                                       "\",\"to\":\"" + abiPath +
                                       "\",\"method\":\"GetBalance\",\"incr\":\"" +
                                       number.ToString() + "\",\"params\":[\"" + account + "\"]}";
                ci = new CommandInfo("broadcast_tx");
                ci.Parameter = parameterinfo;
                requestInfo = CH.RpcGenerateTransactionRawTx(ci);
                rpcRequest.Add(requestInfo);
                number++;
            }
            Logger.WriteInfo("Thread [{0}] contracts rpc list from account :{1} and contract abi: {2} generated completed.",threadNo, account, abiPath);
            //Send RPC Requests
            ci = new CommandInfo("broadcast_txs");
            foreach(var rpc in rpcRequest)
            {
                ci.Parameter += "," + rpc;
            }
            ci.Parameter = ci.Parameter.Substring(1);
            CH.ExecuteCommand(ci);
            Assert.IsTrue(ci.Result);
            var result = ci.InfoMsg[0].Replace("[", "").Replace("]", "").Split(",");
            Logger.WriteInfo("Batch request count: {0}, Pass count: {1} at {2}", rpcRequest.Count, result.Length, DateTime.Now.ToString("HH:mm:ss.fff"));
            Logger.WriteInfo("Thread [{0}] completeed executed {1} times contracts work at {2}.", threadNo, times, DateTime.Now.ToString());
            Logger.WriteInfo("{0} Transfer from Address {1}", set.Count, account);
        }

        public void GenerateRpcList(int threadNo, int times)
        {
            string account = AccountList[ContractList[threadNo].AccountId].Account;
            string abiPath = ContractList[threadNo].AbiPath;

            //Get Increment info
            var ci = new CommandInfo("get_increment");
            ci.Parameter = account;
            CH.ExecuteCommand(ci);
            Assert.IsTrue(ci.Result);
            ci.GetJsonInfo();
            string increNo = ci.JsonInfo["result"]["result"]["increment"].ToString();
            int number = Int32.Parse(increNo);

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
                string parameterinfo = "{\"from\":\"" + account +
                              "\",\"to\":\"" + abiPath +
                              "\",\"method\":\"Transfer\",\"incr\":\"" +
                              number.ToString() + "\",\"params\":[\"" + account + "\",\"" + account1 + "\",\"1\"]}";
                ci = new CommandInfo("broadcast_tx");
                ci.Parameter = parameterinfo;
                string requestInfo = CH.RpcGenerateTransactionRawTx(ci);
                ContractRpcList.Enqueue(requestInfo);
                number++;

                //Get Balance Info
                parameterinfo = "{\"from\":\"" + account +
                                       "\",\"to\":\"" + abiPath +
                                       "\",\"method\":\"GetBalance\",\"incr\":\"" +
                                       number.ToString() + "\",\"params\":[\"" + account + "\"]}";
                ci = new CommandInfo("broadcast_tx");
                ci.Parameter = parameterinfo;
                requestInfo = CH.RpcGenerateTransactionRawTx(ci);
                ContractRpcList.Enqueue(requestInfo);
                number++;
            }
        }
        
        public void ExecuteOneRpcTask()
        {
            string rpcMsg = string.Empty;
            var request = new RpcRequestManager(RpcUrl);
            while (true)
            {
                if (!ContractRpcList.TryDequeue(out rpcMsg))
                    break;
                Logger.WriteInfo("Contracts execution left: {0}", ContractRpcList.Count);
                var ci = new CommandInfo("broadcast_tx");
                ci.Parameter = rpcMsg;
                CH.ExecuteCommand(ci);
                if (!ci.Result)
                    ContractRpcList.Enqueue(rpcMsg);
                Thread.Sleep(20);
            }
        }

        public void ExecuteMultiTask(int threadCount =4)
        {
            Logger.WriteInfo("Begin generate multi rpc requests.");
            List<Task> genRpcTasks = new List<Task>();
            for(int i=0; i<ThreadCount; i++)
            {
                var j = i;
                genRpcTasks.Add(Task.Run(()=>GenerateRpcList(j, ExeTimes)));
            }
            Task.WaitAll(genRpcTasks.ToArray<Task>());

            Logger.WriteInfo("Begin execute multi rpc contracts.");
            List<Task> contractTasks = new List<Task>();
            for (int i = 0; i < threadCount; i++)
            {
                var j = i;
                contractTasks.Add(Task.Run(() => ExecuteOneRpcTask()));
            }

            Task.WaitAll(contractTasks.ToArray<Task>());
        }

        public void DeleteAccounts()
        {
            foreach (var item in AccountList)
            {
                string file = Path.Combine(KeyStorePath, $"{item.Account}.ak");
                File.Delete(file);
            }
        }

        #region Private Method
        private void CheckResultStatus(List<string> idList)
        {
            Thread.Sleep(4000);
            int length = idList.Count;
            for(int i= length-1; i>=0; i--)
            {
                var ci = new CommandInfo("get_tx_result");
                ci.Parameter = idList[i];
                CH.ExecuteCommand(ci);

                if(ci.Result)
                {
                    ci.GetJsonInfo();
                    string deployResult = ci.JsonInfo["result"]["result"]["tx_status"].ToString();
                    if (deployResult == "Mined")
                        idList.Remove(idList[i]);
                }
                Thread.Sleep(50);
            }
            if (idList.Count > 0 && idList.Count != 1)
            {
                Logger.WriteInfo("***************** {0} ******************", idList.Count);
                CheckResultStatus(idList);
            }
            if(idList.Count == 1)
            {
                Logger.WriteInfo("Last one: {0}", idList[0]);
                var ci = new CommandInfo("get_tx_result");
                ci.Parameter = idList[0];
                CH.ExecuteCommand(ci);
                
                if (ci.Result)
                {
                    ci.GetJsonInfo();
                    string deployResult = ci.JsonInfo["result"]["result"]["tx_status"].ToString();
                    if (deployResult != "Mined")
                    {
                        Thread.Sleep(50);
                        CheckResultStatus(idList);
                    }
                }
            }

            Thread.Sleep(50);
        }

        private void UnlockAllAccounts(int count)
        {
            for(int i=0; i<count; i++)
            {
                var ci = new CommandInfo("account unlock", "account");
                ci.Parameter = String.Format("{0} {1} {2}", AccountList[i].Account, "123", "notimeout");
                ci = CH.ExecuteCommand(ci);
                Assert.IsTrue(ci.Result);
            }
        }

        private void NewAccounts(int count)
        {
            for (int i = 0; i < count; i++)
            {
                var ci = new CommandInfo("account new", "account");
                ci.Parameter = "123";
                ci = CH.ExecuteCommand(ci);
                Assert.IsTrue(ci.Result);
                AccountList.Add(new AccountInfo(ci.InfoMsg?[0].Replace("Account address:","").Trim()));
            }
        }

        private void GetExecutedAccount()
        {
            var accounts = AccountList.FindAll(x => x.Increment != 0);
            int count = 0;
            foreach(var item in accounts)
            {
                count++;
                Logger.WriteInfo("{0:000} Account: {1}, Execution times: {2}", count, item.Account, item.Increment);
            }
        }
        
        private string GetDefaultDataDir()
        {
            try
            {
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "aelf");
            }
            catch (Exception e)
            {
                return null;
            }
        }

        public void PrintContractInfo()
        {
            Logger.WriteInfo("Execution account and contract abi information:");
            int count = 0;
            foreach (var item in ContractList)
            {
                count++;
                Logger.WriteInfo("{0:00}. Account: {1}, AbiPath:{2}",count, AccountList[item.AccountId].Account, item.AbiPath);
            }
        }
        #endregion
    }
}
