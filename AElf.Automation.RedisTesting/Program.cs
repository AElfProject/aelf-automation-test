using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
            rt.ScanDBInformation("192.168.199.221", "http://192.168.199.221:8000/chain");
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

        public void ScanDBInformation(string redishost, string rpcUrl)
        {
            //Rpc Info analyze
            RpcAPI ra = new RpcAPI(rpcUrl);
            ConcurrentQueue<BlockInfo> BlockCollection = new ConcurrentQueue<BlockInfo>();
            int height = ra.GetCurrentHeight();

            var rh = new RedisHelper(redishost);
            var ktm = new KeyTypeManager(rh);
            //Get Block info
            Logger.WriteInfo("Get current block height information.");
            for (int i = 1; i < height; i++)
            {
                var jsonInfo = ra.GetBlockInfo(i);
                var block = new BlockInfo(i, jsonInfo);
                BlockCollection.Enqueue(block);
                Thread.Sleep(50);
            }

            //Redis Info analyze
            ktm.GetAllKeyInfoCollection();
            ktm.PrintSummaryInfo(false);

            Logger.WriteInfo("Begin print block info by height");
            Logger.WriteInfo("-------------------------------------------------------------------------------------------------------------");
            //Analyze keys in collection
            Logger.WriteInfo("Begin analyze block information by multi tasks..");
            List<Task> contractTasks = new List<Task>();
            for (int i = 0; i < 8; i++)
            {
                var j = i;
                contractTasks.Add(Task.Run(() =>
                {
                    BlockInfo block;
                    while (true)
                    {
                        if (!BlockCollection.TryDequeue(out block))
                            break;

                        Logger.WriteInfo($"Block Height: {block.Height}, TxCount:{block.Transactions.Count}");
                        //Analyze Blockhash
                        var keyinfoList = ktm.HashList["Hash"]
                            .FindAll(o => o.ValueInfo.ToString().Contains(block.BlockHash));
                        if (keyinfoList != null && keyinfoList?.Count !=0)
                        {
                            foreach (var keyinfo in keyinfoList)
                            {
                                keyinfo.Checked = true;
                                Logger.WriteInfo(keyinfo.ToString());
                                if (keyinfo.HashString == "Chain")
                                {
                                    var hash = new AElf.Kernel.Hash(keyinfo.KeyObject.Value);
                                    string hashValue = hash.ToHex();
                                    var changeInfo = ktm.HashList["Hash"]
                                        .FirstOrDefault(o => o.ValueInfo.ToString().Contains(hashValue));
                                    if (changeInfo != null)
                                    {
                                        changeInfo.Checked = true;
                                        Logger.WriteInfo(changeInfo.ToString());
                                    }
                                }
                            }
                        }

                        var blockBody = ktm.HashList["BlockBody"]
                            .FirstOrDefault(o => o.ValueInfo.ToString().Contains(block.BlockHash));
                        if (blockBody != null)
                        {
                            blockBody.Checked = true;
                            Logger.WriteInfo(blockBody.ToString());
                        }

                        //Analyze PreviousBlockHash
                        //Analyze MerkleTreeRootOfTransactions
                        //Analyze MerkleTreeRootOfWorldState
                        var blockHeader = ktm.HashList["BlockHeader"]
                            .FirstOrDefault(o => o.ValueInfo.ToString().Contains(block.PreviousBlockHash));
                        if (blockHeader != null)
                        {
                            blockHeader.Checked = true;
                            Logger.WriteInfo(blockHeader.ToString());
                        }

                        //Analyze Transactions
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
                                Logger.WriteInfo(transactionInfo.ToString());
                            }

                            //Transaction Result
                            var transactionResult = ktm.HashList["TransactionResult"]
                                .FirstOrDefault(o => o.ValueInfo.ToString().Contains(transaction.Trim()));

                            if (transactionResult != null)
                            {
                                transactionResult.Checked = true;
                                Logger.WriteInfo(transactionResult.ToString());
                            }
                        }
                        Logger.WriteInfo("-------------------------------------------------------------------------------------------------------------");
                    }
                }));
            }

            Task.WaitAll(contractTasks.ToArray<Task>());

            //Print Unchecked key item info
            Logger.WriteInfo(string.Empty);
            Logger.WriteInfo("Print unchecked key info");
            foreach (var item in ktm.HashList.Keys)
            {
                Logger.WriteInfo($"Category:{item}, Unchecked count:{ktm.HashList[item].FindAll(o=>o.Checked==false).Count}");
                foreach (var keyinfo in ktm.HashList[item].FindAll(o=>o.Checked==false))
                {
                    Logger.WriteInfo(keyinfo.ToString());
                }
                Logger.WriteInfo(string.Empty);
            }

            //Summary info
            Logger.WriteInfo(string.Empty);
            Logger.WriteInfo("Summary basic type info");
            foreach (var item in ktm.HashList.Keys)
            {
                Logger.WriteInfo($"Category:{item}, Total:{ktm.HashList[item].Count}, Checked:{ktm.HashList[item].FindAll(o=>o.Checked==true).Count}, Unchecked:{ktm.HashList[item].FindAll(o=>o.Checked==false).Count}");
            }

            //Summary hash info
            Logger.WriteInfo(string.Empty);
            Logger.WriteInfo("Summary hash type info");
            ktm.ConvertHashType();
            foreach (var item in ktm.ProtoHashList.Keys)
            {
                Logger.WriteInfo($"Category:{item}, Total:{ktm.ProtoHashList[item].Count}, Checked:{ktm.ProtoHashList[item].FindAll(o=>o.Checked==true).Count}, Unchecked:{ktm.ProtoHashList[item].FindAll(o=>o.Checked==false).Count}");
            }
        }
    }
}