using AElf.Automation.CliTesting;
using AElf.Automation.CliTesting.AutoTest;
using AElf.Automation.Common.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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
        public AElfCliProgram Instance { get; set; }
        public string RpcUrl { get; set; }
        public List<CommandRequest> RequestList { get; set; }
        public List<AccountInfo> AccountList { get; set; }
        public string KeyStorePath { get; set; }
        public List<Contract> ContractList { get; set; }
        public List<string> TxIdList { get; set; }
        public int ThreadCount { get; set; }
        public int ExeTimes { get; set; }
        public ConcurrentQueue<string> ContractRpcList { get; set; }
        #endregion

        public RpcAPI(int threadCount, 
            int exeTimes, 
            string rpcUrl= "http://192.168.197.34:8000", 
            string keyStorePath= "")
        {
            if (keyStorePath == "")
                keyStorePath = GetDefaultDataDir();

            RequestList = new List<CommandRequest>();
            AccountList = new List<AccountInfo>();
            ContractList = new List<Contract>();
            ContractRpcList = new ConcurrentQueue<string>();
            TxIdList = new List<string>();
            ThreadCount = threadCount;
            ExeTimes = exeTimes;
            RpcUrl = rpcUrl;
            KeyStorePath = Path.Combine(keyStorePath, "keys");
            Console.WriteLine("Rpc Url: {0}", RpcUrl);
            Console.WriteLine("Key Store Path: {0}", KeyStorePath);
        }

        public void PrepareEnv()
        {
            Console.WriteLine();
            InitCli.InitCliCommand(RpcUrl);
            Instance = InitCli.CliInstance;

            //Account Perpare
            //Delete
            DeleteAccounts();
            //New
            NewAccounts(1000);
            //Unlock Account
            UnlockAllAccounts(ThreadCount);
        }

        public void InitExecRpcCommand()
        {
            //Connect Chain
            CommandRequest connReq = new CommandRequest("connect_chain", "connect_chain");
            connReq.Result = Instance.ExecuteCommandWithPerformance(connReq.Command, out connReq.InfoMessage, out connReq.ErrorMessage, out connReq.TimeInfo);
            RequestList.Add(connReq);
            Assert.IsTrue(connReq.Result);

            //Load Contract Abi
            CommandRequest loadReq = new CommandRequest("load_contract_abi", "load_contract_abi");
            loadReq.Result = Instance.ExecuteCommandWithPerformance(loadReq.Command, out loadReq.InfoMessage, out loadReq.ErrorMessage, out loadReq.TimeInfo);
            RequestList.Add(loadReq);
            Assert.IsTrue(loadReq.Result);
        }

        public void DeployContract()
        {
            List<object> contractList = new List<object>();
            for (int i = 0; i < ThreadCount; i++)
            {
                dynamic info = new System.Dynamic.ExpandoObject();
                info.Id = i;
                info.Account = AccountList[i].Account;
                CommandRequest contractReq = new CommandRequest("deploy_contract", $"deploy_contract AElf.Benchmark.TestContract 0 {AccountList[i].Account}");
                contractReq.Result = Instance.ExecuteCommandWithPerformance(contractReq.Command, out contractReq.InfoMessage, out contractReq.ErrorMessage, out contractReq.TimeInfo);
                RequestList.Add(contractReq);
                Assert.IsTrue(contractReq.Result);

                contractReq.GetJsonInfo();
                string genesisContract = contractReq.JsonInfo["txId"].ToString();
                Assert.AreNotEqual(string.Empty, genesisContract);
                info.TxId = genesisContract;
                info.Result = false;
                contractList.Add(info);
            }
            int count = 0;
            while(true)
            {
                foreach(dynamic item in contractList)
                {
                    if(item.Result == false)
                    {
                        CommandRequest txReq = new CommandRequest("get_tx_result", $"get_tx_result {item.TxId}");
                        txReq.Result = Instance.ExecuteCommandWithPerformance(txReq.Command, out txReq.InfoMessage, out txReq.ErrorMessage, out txReq.TimeInfo);
                        RequestList.Add(txReq);

                        Assert.IsTrue(txReq.Result);
                        txReq.GetJsonInfo();
                        string deployResult = txReq.JsonInfo["tx_status"].ToString();
                        if (deployResult == "Mined")
                        {
                            count++;
                            item.Result = true;
                            string abiPath = txReq.JsonInfo["return"].ToString();
                            ContractList.Add(new Contract(item.Id, abiPath));
                            AccountList[item.Id].Increment = 1;
                        }
                        Thread.Sleep(10);
                    }
                }
                if (count == contractList.Count)
                    break;
                else
                    Thread.Sleep(1000);
            }
        }

        public void InitializeContract()
        {
            List<string> txIdList = new List<string>();
            for (int i = 0; i < ContractList.Count; i++)
            {
                string account = AccountList[ContractList[i].AccountId].Account;
                string abiPath = ContractList[i].AbiPath;
                //Get Increment
                CommandRequest accountReq = new CommandRequest("get_increment", $"get_increment {account}");
                accountReq.Result = Instance.ExecuteCommandWithPerformance(accountReq.Command, out accountReq.InfoMessage, out accountReq.ErrorMessage, out accountReq.TimeInfo);
                RequestList.Add(accountReq);

                Assert.IsTrue(accountReq.Result);
                string increNo = accountReq.InfoMessage;

                //Load Contract abi
                CommandRequest loadReq = new CommandRequest("load_contract_abi", $"load_contract_abi {abiPath}");
                loadReq.Result = Instance.ExecuteCommandWithPerformance(loadReq.Command, out loadReq.InfoMessage, out loadReq.ErrorMessage, out loadReq.TimeInfo);
                RequestList.Add(loadReq);

                Assert.IsTrue(loadReq.Result);

                //Execute contract method
                //string parameterinfo = "{\"from\":\"" + account +
                //                       "\",\"to\":\"" + abiPath +
                //                       "\",\"method\":\"Initialize\",\"incr\":\"" +
                //                       increNo + "\",\"params\":[\"PF" + i.ToString("00") + "\",\"PerformanceToken" + i.ToString("00") + "\",\"20000000\",\"2\"]}";
                string parameterinfo = "{\"from\":\"" + account +
                                      "\",\"to\":\"" + abiPath +
                                      "\",\"method\":\"InitBalance\",\"incr\":\"" +
                                      increNo + "\",\"params\":[\"" + account + "\"]}";
                CommandRequest exeReq = new CommandRequest("broadcast_tx", $"broadcast_tx {parameterinfo}");
                exeReq.Result = Instance.ExecuteCommandWithPerformance(exeReq.Command, out exeReq.InfoMessage, out exeReq.ErrorMessage, out exeReq.TimeInfo);
                RequestList.Add(exeReq);
                Assert.IsTrue(exeReq.Result);

                exeReq.GetJsonInfo();
                string genesisContract = exeReq.JsonInfo["txId"].ToString();
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
                //Load Contract abi
                CommandRequest loadReq = new CommandRequest("load_contract_abi", $"load_contract_abi {abiPath}");
                loadReq.Result = Instance.ExecuteCommandWithPerformance(loadReq.Command, out loadReq.InfoMessage, out loadReq.ErrorMessage, out loadReq.TimeInfo);
                RequestList.Add(loadReq);
                Assert.IsTrue(loadReq.Result);
            }
        }

        public void ExecuteContracts()
        {
            Console.WriteLine("Start contract execution at: {0}", DateTime.Now.ToString());
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
            Console.WriteLine("End contract execution at: {0}", DateTime.Now.ToString());
            Console.WriteLine("Execution time: {0}", exec.ElapsedMilliseconds);
            GetExecutedAccount();
        }

        public void ExecuteContractsRpc()
        {
            Console.WriteLine("Start all generate rpc request at: {0}", DateTime.Now.ToString());
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
            Console.WriteLine("All rpc requests completed at: {0}", DateTime.Now.ToString());
            Console.WriteLine("Execution time: {0}", exec.ElapsedMilliseconds);
        }
        
        //Without conflict group category
        public void DoContractCategory(int threadNo, int times)
        {
            string account = AccountList[ContractList[threadNo].AccountId].Account;
            string abiPath = ContractList[threadNo].AbiPath;

            //Get Increment info
            CommandRequest accountReq = new CommandRequest("get_increment", $"get_increment {account}");
            accountReq.Result = Instance.ExecuteCommandWithPerformance(accountReq.Command, out accountReq.InfoMessage, out accountReq.ErrorMessage, out accountReq.TimeInfo);
            RequestList.Add(accountReq);
            Assert.IsTrue(accountReq.Result);
            string increNo = accountReq.InfoMessage;
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
                //string parameterinfo = "{\"from\":\"" + account +
                //                       "\",\"to\":\"" + abiPath +
                //                       "\",\"method\":\"Transfer\",\"incr\":\"" +
                //                       number.ToString() + "\",\"params\":[\"" + account1 + "\",\"2\"]}";
                string parameterinfo = "{\"from\":\"" + account +
                              "\",\"to\":\"" + abiPath +
                              "\",\"method\":\"Transfer\",\"incr\":\"" +
                              number.ToString() + "\",\"params\":[\"" + account + "\",\"" + account1 + "\",\"1\"]}";
                CommandRequest exeReq = new CommandRequest("broadcast_tx", $"broadcast_tx {parameterinfo}");
                exeReq.Result = Instance.ExecuteCommandWithPerformance(exeReq.Command, out exeReq.InfoMessage, out exeReq.ErrorMessage, out exeReq.TimeInfo);
                RequestList.Add(exeReq);
                if(exeReq.Result)
                {
                    exeReq.GetJsonInfo();
                    txIdList.Add(exeReq.JsonInfo["txId"].ToString());
                    number++;
                    passCount++;
                }
                Thread.Sleep(20);
                //Get Balance Info
                parameterinfo = "{\"from\":\"" + account +
                                       "\",\"to\":\"" + abiPath +
                                       "\",\"method\":\"GetBalance\",\"incr\":\"" +
                                       number.ToString() + "\",\"params\":[\"" + account + "\"]}";
                CommandRequest queryReq = new CommandRequest("broadcast_tx", $"broadcast_tx {parameterinfo}");
                queryReq.Result = Instance.ExecuteCommandWithPerformance(queryReq.Command, out queryReq.InfoMessage, out queryReq.ErrorMessage, out queryReq.TimeInfo);
                RequestList.Add(queryReq);
                if(queryReq.Result)
                {
                    Assert.IsTrue(queryReq.Result);
                    queryReq.GetJsonInfo();
                    txIdList.Add(queryReq.JsonInfo["txId"].ToString());
                    number++;
                    passCount++;
                }
                Thread.Sleep(20);
            }
            Console.WriteLine("Total contract sent: {0}, passed number: {1}", 2*times, passCount);
            //txIdList.Reverse();
            //CheckResultStatus(txIdList);
            //Console.WriteLine("All contracts from account :{0} and contract abi: {1} executed completed.", account, abiPath);
            Console.WriteLine("{0} Transfer from Address {1}", set.Count, account);
        }

        public void GenerateContractList(int threadNo, int times)
        {
            string account = AccountList[ContractList[threadNo].AccountId].Account;
            string abiPath = ContractList[threadNo].AbiPath;

            //Get Increment info
            CommandRequest accountReq = new CommandRequest("get_increment", $"get_increment {account}");
            accountReq.Result = Instance.ExecuteCommandWithPerformance(accountReq.Command, out accountReq.InfoMessage, out accountReq.ErrorMessage, out accountReq.TimeInfo);
            RequestList.Add(accountReq);
            Assert.IsTrue(accountReq.Result);
            string increNo = accountReq.InfoMessage;
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
                CommandRequest exeReq = new CommandRequest("broadcast_tx", $"broadcast_tx {parameterinfo}");
                string requestInfo = Instance.GetRpcRequestInformation(exeReq.Command);
                rpcRequest.Add(requestInfo);
                number++;

                //Get Balance Info
                parameterinfo = "{\"from\":\"" + account +
                                       "\",\"to\":\"" + abiPath +
                                       "\",\"method\":\"GetBalance\",\"incr\":\"" +
                                       number.ToString() + "\",\"params\":[\"" + account + "\"]}";
                CommandRequest queryReq = new CommandRequest("broadcast_tx", $"broadcast_tx {parameterinfo}");
                requestInfo = Instance.GetRpcRequestInformation(queryReq.Command);
                rpcRequest.Add(requestInfo);
                number++;
            }
            Console.WriteLine("Thread [{0}] contracts rpc list from account :{1} and contract abi: {2} generated completed.",threadNo, account, abiPath);
            //Send RPC Request
            string returnCode = string.Empty;
            var request = new RpcRequest(RpcUrl);
            Console.WriteLine("Start send thread {0} rpc request at {1}", threadNo, DateTime.Now.ToString());
            //for(int i=0; i<(rpcRequest.Count/100); i++)
            //{
            //    string response = request.PostRequest("broadcast_txs", rpcRequest.GetRange(i*100, 100), out returnCode);
            //    Thread.Sleep(20);
            //}
            string response = request.PostRequest("broadcast_txs", rpcRequest, out returnCode);
            var result = JsonConvert.DeserializeObject<JObject>(response);
            Console.WriteLine("Batch request count: {0}, Pass count: {0} at {1}", rpcRequest.Count, result["result"]["pass_count"], DateTime.Now.ToString("HH:mm:ss.fff"));
            //Add summary info
            for(int i=0; i<Int32.Parse(result["result"]["pass_count"].ToString()); i++)
            {
                var cr = new CommandRequest("broadcast_tx", "broadcast_tx");
                cr.Result = true;
                RequestList.Add(cr);
            }
            Console.WriteLine("Thread [{0}] completeed executed {1} times contracts work at {2}.", threadNo, times, DateTime.Now.ToString());
            Console.WriteLine("{0} Transfer from Address {1}", set.Count, account);
        }

        public void GenerateRpcList(int threadNo, int times)
        {
            string account = AccountList[ContractList[threadNo].AccountId].Account;
            string abiPath = ContractList[threadNo].AbiPath;

            //Get Increment info
            CommandRequest accountReq = new CommandRequest("get_increment", $"get_increment {account}");
            accountReq.Result = Instance.ExecuteCommandWithPerformance(accountReq.Command, out accountReq.InfoMessage, out accountReq.ErrorMessage, out accountReq.TimeInfo);
            RequestList.Add(accountReq);
            Assert.IsTrue(accountReq.Result);
            string increNo = accountReq.InfoMessage;
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
                CommandRequest exeReq = new CommandRequest("broadcast_tx", $"broadcast_tx {parameterinfo}");
                string requestInfo = Instance.GetRpcRequestInformation(exeReq.Command);
                ContractRpcList.Enqueue(requestInfo);
                number++;

                //Get Balance Info
                parameterinfo = "{\"from\":\"" + account +
                                       "\",\"to\":\"" + abiPath +
                                       "\",\"method\":\"GetBalance\",\"incr\":\"" +
                                       number.ToString() + "\",\"params\":[\"" + account + "\"]}";
                CommandRequest queryReq = new CommandRequest("broadcast_tx", $"broadcast_tx {parameterinfo}");
                requestInfo = Instance.GetRpcRequestInformation(queryReq.Command);
                ContractRpcList.Enqueue(requestInfo);
                number++;
            }
        }
        
        public void ExecuteOneRpcTask()
        {
            string rpcMsg = string.Empty;
            while (true)
            {
                if (!ContractRpcList.TryDequeue(out rpcMsg))
                    break;
                Console.WriteLine("ContractRpcList:{0}", ContractRpcList.Count);
                string returnCode = string.Empty;
                var request = new RpcRequest(RpcUrl);
                string parameter = "{\"rawtx\":\"" + rpcMsg + "\"}";
                string response = request.PostRequest("broadcast_tx", parameter, out returnCode);
                Thread.Sleep(50);
            }
        }

        public void ExecuteMultiTask(int threadCount =4)
        {
            Console.WriteLine("Begin generate multi rpc requests.");
            List<Task> genRpcTasks = new List<Task>();
            for(int i=0; i<ThreadCount; i++)
            {
                var j = i;
                genRpcTasks.Add(Task.Run(()=>GenerateRpcList(j, ExeTimes)));
            }
            Task.WaitAll(genRpcTasks.ToArray<Task>());

            Console.WriteLine("Begin execute multi rpc contracts.");
            List<Task> contractTasks = new List<Task>();
            for (int i = 0; i < threadCount; i++)
            {
                var j = i;
                contractTasks.Add(Task.Run(() => ExecuteOneRpcTask()));
            }

            Task.WaitAll(contractTasks.ToArray<Task>());
        }
        
        #region Private Method
        private void CheckResultStatus(List<string> idList)
        {
            int length = idList.Count;
            for(int i= length-1; i>=0; i--)
            {
                CommandRequest txReq = new CommandRequest("get_tx_result", $"get_tx_result {idList[i]}");
                txReq.Result = Instance.ExecuteCommandWithPerformance(txReq.Command, out txReq.InfoMessage, out txReq.ErrorMessage, out txReq.TimeInfo);
                RequestList.Add(txReq);

                if(txReq.Result)
                {
                    txReq.GetJsonInfo();
                    string deployResult = txReq.JsonInfo["tx_status"].ToString();
                    if (deployResult == "Mined")
                        idList.Remove(idList[i]);
                }
                Thread.Sleep(10);
            }
            if (idList.Count > 0 && idList.Count != 1)
            {
                Console.WriteLine("***************** {0} ******************", idList.Count);
                CheckResultStatus(idList);
            }
            if(idList.Count == 1)
            {
                Console.WriteLine("Last one: {0}", idList[0]);
                CommandRequest txReq = new CommandRequest("get_tx_result", $"get_tx_result {idList[0]}");
                txReq.Result = Instance.ExecuteCommandWithPerformance(txReq.Command, out txReq.InfoMessage, out txReq.ErrorMessage, out txReq.TimeInfo);
                RequestList.Add(txReq);

                if (txReq.Result)
                {
                    txReq.GetJsonInfo();
                    string deployResult = txReq.JsonInfo["tx_status"].ToString();
                    Assert.IsTrue(txReq.Result);
                    txReq.GetJsonInfo();
                    deployResult = txReq.JsonInfo["tx_status"].ToString();
                }
            }

            Thread.Sleep(10);
        }

        private void UnlockAllAccounts(int count)
        {
            GetAccountList();
            for(int i=0; i<count; i++)
            {
                CommandRequest cmdReq = new CommandRequest($"account unlock {AccountList[i].Account} 123 notimeout");
                cmdReq.Result = Instance.ExecuteCommand(cmdReq.Command, out cmdReq.InfoMessage, out cmdReq.ErrorMessage);

                Assert.IsTrue(cmdReq.Result);
            }
        }

        private void DeleteAccounts()
        {
            foreach (var item in Directory.GetFiles(KeyStorePath, "*.ak"))
            {
                File.Delete(item);
            }
        }

        private void NewAccounts(int count)
        {
            //List<string> accounts = new List<string>();
            for (int i = 0; i < count; i++)
            {
                CommandRequest cmdReq = new CommandRequest("account new 123");
                cmdReq.Result = Instance.ExecuteCommand(cmdReq.Command, out cmdReq.InfoMessage, out cmdReq.ErrorMessage);

                Assert.IsTrue(cmdReq.Result);
            }
        }

        private void GetAccountList()
        {
            var fileList = Directory.GetFiles(KeyStorePath, "*.ak");
            foreach (var item in fileList)
            {
                string[] fileInfo;
                if (item.Contains("/"))
                    fileInfo = item.Split("/");
                else
                    fileInfo = item.Split("\\");
                string account = fileInfo[fileInfo.Length - 1].Replace(".ak", "");
                AccountList.Add(new AccountInfo(account));
            }
        }

        private void GetExecutedAccount()
        {
            var accounts = AccountList.FindAll(x => x.Increment != 0);
            int count = 0;
            foreach(var item in accounts)
            {
                count++;
                Console.WriteLine("{0:000} Account: {1}, Execution times: {2}", count, item.Account, item.Increment);
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
        #endregion
    }
}
