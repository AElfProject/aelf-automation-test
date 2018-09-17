using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AElf.Automation.Common.Extensions;
using AElf.Automation.Common.Helpers;
using AElf.Automation.RpcTesting;

namespace AElf.Automation.RedisTesting
{
    class Program
    {
        static void Main(string[] args)
        {
            var rt = new RedisTest();
            rt.InitTestLog();
            rt.ScanDbInformation("192.168.199.221", "http://192.168.199.221:8000/chain");
        }
    }

    public class RedisTest
    {
        public ILogHelper Logger = LogHelper.GetLogHelper();

        public void InitTestLog()
        {
            string logName = "RedisTest_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".log";
            string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", logName);
            Logger.InitLogHelper(dir);
        }

        public void QueryRedisKeyInfo(string redishost, string key)
        {
            var rh = new RedisHelper(redishost);
            var ktm = new KeyTypeManager(rh);
            var keyInfo = ktm.GetKeyInfo(key);
            var hash = new AElf.Kernel.Hash(keyInfo.KeyObject.Value);
            Logger.WriteInfo($"ConvertValue={hash.ToHex()}, ObjectValue={keyInfo.ValueInfo}");
        }

        public void ScanDbInformation(string redishost, string rpcUrl)
        {
            //Rpc Info analyze
            RpcAPI ra = new RpcAPI(rpcUrl);
            ConcurrentQueue<BlockInfo> blockCollection = new ConcurrentQueue<BlockInfo>();
            int height = ra.GetCurrentHeight();

            var rh = new RedisHelper(redishost);
            var ktm = new KeyTypeManager(rh);

            List<Task> blockTasks = new List<Task>();
            //Redis Info analyze
            blockTasks.Add(Task.Run(() =>
            {
                ktm.GetAllKeyInfoCollection();
                ktm.PrintSummaryInfo(false);
            }));
            //Get Block info
            Logger.WriteInfo("Get current block height information, current total height: {0}.", height);
            int reqHeight = 0;
            object obj = new object();
            for (int i = 0; i < 8; i++)
            {
                blockTasks.Add(Task.Run(() =>
                {
                    while (true)
                    {
                        try
                        {
                            int threadHeight = 0;
                            lock (obj)
                            {
                                reqHeight++;
                                threadHeight = reqHeight;
                            }
                            if (threadHeight >= height)
                                break;
                            var jsonInfo = ra.GetBlockInfo(threadHeight);
                            var block = new BlockInfo(threadHeight, jsonInfo);
                            blockCollection.Enqueue(block);
                            Thread.Sleep(50);
                        }
                        catch (Exception e)
                        {
                            Logger.WriteError("Get block info got exception: {0}", e.Message);
                        }
                    }
                }));
            }
            Task.WaitAll(blockTasks.ToArray<Task>());

            //Analyze keys in collection
            Logger.WriteInfo("Begin analyze block information by multi tasks.");
            List<Task> contractTasks = new List<Task>();

            Logger.WriteInfo("Begin print block info by height");
            Logger.WriteInfo("-------------------------------------------------------------------------------------------------------------");
            for (int i = 0; i < 8; i++)
            {
                contractTasks.Add(Task.Run(() =>
                {
                    while (true)
                    {
                        StringBuilder sb = new StringBuilder();
                        BlockInfo block = null;
                        try
                        {
                            if (!blockCollection.TryDequeue(out block))
                                break;
                            sb.AppendLine($"Block Height: {block.Height}, TxCount:{block.Transactions.Count}");
                            List<Task> analyzeTasks = new List<Task>();
                            //Analyze Block Hash
                            analyzeTasks.Add(Task.Run(() =>
                            {
                                var keyinfoList = ktm.HashList["Hash"]
                                    .FindAll(o => o.ValueInfo.ToString().Contains(block.BlockHash));
                                if (keyinfoList?.Count != 0)
                                {
                                    foreach (var keyinfo in keyinfoList)
                                    {
                                        keyinfo.Checked = true;
                                        sb.AppendLine(keyinfo.ToString());
                                        if (keyinfo.HashString == "Chain")
                                        {
                                            var hash = new AElf.Kernel.Hash(keyinfo.KeyObject.Value);
                                            string hashValue = hash.ToHex();
                                            var changeInfo = ktm.HashList["Hash"]
                                                .FirstOrDefault(o => o.ValueInfo.ToString().Contains(hashValue));
                                            if (changeInfo != null)
                                            {
                                                changeInfo.Checked = true;
                                                sb.AppendLine(changeInfo.ToString());
                                            }
                                        }
                                    }
                                }
                            }));

                            //Analyze BlockBody
                            analyzeTasks.Add(Task.Run(() =>
                            {
                                var blockBody = ktm.HashList["BlockBody"]
                                    .FirstOrDefault(o => o.ValueInfo.ToString().Contains(block.BlockHash));
                                if (blockBody != null)
                                {
                                    blockBody.Checked = true;
                                    sb.AppendLine(blockBody.ToString());
                                }
                            }));

                            //Analyze PreviousBlockHash
                            analyzeTasks.Add(Task.Run(() =>
                            {
                                var blockHeader = ktm.HashList["BlockHeader"]
                                    .FirstOrDefault(o => o.ValueInfo.ToString().Contains(block.PreviousBlockHash));
                                if (blockHeader != null)
                                {
                                    blockHeader.Checked = true;
                                    sb.AppendLine(blockHeader.ToString());
                                }
                            }));

                            //Analyze Transactions
                            analyzeTasks.Add(Task.Run(() =>
                            {
                                foreach (var transaction in block.Transactions)
                                {
                                    //Transaction
                                    var txResult = ra.GetTxResult(transaction.Trim());
                                    string incrementId = txResult["result"]["result"]["tx_info"]["IncrementId"].ToString();
                                    string checkStr = $"\"IncrementId\": \"{incrementId}\"";
                                    var transactionInfo = ktm.HashList["Transaction"]
                                        .FirstOrDefault(o => o.ValueInfo.ToString().Contains(checkStr));
                                    if (transactionInfo != null)
                                    {
                                        transactionInfo.Checked = true;
                                        sb.AppendLine(transactionInfo.ToString());
                                    }

                                    //Transaction Result
                                    var transactionResult = ktm.HashList["TransactionResult"]
                                        .FirstOrDefault(o => o.ValueInfo.ToString().Contains(transaction.Trim()));

                                    if (transactionResult != null)
                                    {
                                        transactionResult.Checked = true;
                                        sb.AppendLine(transactionResult.ToString());
                                    }
                                }
                            }));
                            Task.WaitAll(analyzeTasks.ToArray<Task>());
                        }
                        catch (Exception e)
                        {
                            Logger.WriteError("Analyze block height={0} info get exception: {1}", block?.Height, e.Message);
                        }
                        finally
                        {
                            sb.AppendLine(
                                "-------------------------------------------------------------------------------------------------------------");
                            Logger.WriteInfo(sb.ToString());
                        }
                    }
                }));
            }

            Task.WaitAll(contractTasks.ToArray<Task>());

            //Print Unchecked key item info
            Logger.WriteInfo(string.Empty);
            Logger.WriteInfo("Print unchecked key info");
            foreach (var item in ktm.HashList.Keys)
            {
                var listCollection = ktm.HashList[item].FindAll(o => o.Checked == false);
                Logger.WriteInfo($"Category:{item}, Unchecked count:{listCollection.Count}");
                foreach (var keyinfo in listCollection)
                {
                    Logger.WriteInfo(keyinfo.ToString());
                }
                Logger.WriteInfo(string.Empty);
            }

            //Summary info
            Logger.WriteInfo("Summary basic type info");
            foreach (var item in ktm.HashList.Keys)
            {
                int total = ktm.HashList[item].Count;
                int checkCount = ktm.HashList[item].FindAll(o => o.Checked == true).Count;
                int uncheckCount = total - checkCount;
                Logger.WriteInfo($"Category:{item}, Total:{total}, Checked:{checkCount}, Unchecked:{uncheckCount}");
            }

            //Summary hash info
            Logger.WriteInfo(string.Empty);
            Logger.WriteInfo("Summary hash type info");
            ktm.ConvertHashType();
            foreach (var item in ktm.ProtoHashList.Keys)
            {
                int total = ktm.ProtoHashList[item].Count;
                int checkCount = ktm.ProtoHashList[item].FindAll(o => o.Checked == true).Count;
                int uncheckCount = total - checkCount;
                Logger.WriteInfo($"Category:{item}, Total:{total}, Checked:{checkCount}, Unchecked:{uncheckCount}");
            }
            Logger.WriteInfo("Complete redis db content analyze.");
        }
    }
}