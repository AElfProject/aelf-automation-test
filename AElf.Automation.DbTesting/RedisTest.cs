using AElf.Automation.Common.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using AElf.Automation.Common.Extensions;
using AElf.Automation.RpcTesting;
using AElf.Kernel;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using NLog;
using Hash = AElf.Automation.Common.Protobuf.Hash;

namespace AElf.Automation.DbTesting
{
    [TestClass]
    public class RedisTest
    {
        public ILogHelper Logger = LogHelper.GetLogHelper();

        [TestInitialize]
        public void InitTestLog()
        {
            string logName = "RedisTest_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".log";
            string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", logName);
            Logger.InitLogHelper(dir);
        }

        [TestMethod]
        public void CompareTheSame1()
        {
            var rh1 = new RedisHelper("192.168.197.34");
            var rh2 = new RedisHelper("192.168.197.29");
            var list1 = rh1.GetAllKeys();
            var list2 = rh2.GetAllKeys();

            var same = RedisHelper.GetIntersection(list1, list2);
            var diff1 = RedisHelper.GetExceptList(list1, list2);
            var diff2 = RedisHelper.GetExceptList(list2, list1);
            Console.WriteLine($"list1 count: {list1.Count}");
            Console.WriteLine($"list2 count: {list2.Count}");
            Console.WriteLine($"Same count: {same.Count}");

            Console.WriteLine($"Diff count list1: {diff1.Count}");
            Console.WriteLine("Different Keys in List1");
            foreach (var item in diff1)
            {
                Console.WriteLine(item);
            }

            Console.WriteLine($"Diff count list2: {diff2.Count}");
            Console.WriteLine("Different Keys in List2");
            foreach (var item in diff2)
            {
                Console.WriteLine(item);
            }

            Assert.IsTrue(diff1.Count <2, "192.168.197.34 diff");
            Assert.IsTrue(diff2.Count <2, "192.168.197.20 diff");
        }

        [TestMethod]
        public void CompareTheSame2()
        {
            var rh1 = new RedisHelper("192.168.197.34");
            var rh2 = new RedisHelper("192.168.197.13");
            var list1 = rh1.GetAllKeys();
            var list2 = rh2.GetAllKeys();

            var same = RedisHelper.GetIntersection(list1, list2);
            var diff1 = RedisHelper.GetExceptList(list1, list2);
            var diff2 = RedisHelper.GetExceptList(list2, list1);
            Console.WriteLine($"list1 count: {list1.Count}");
            Console.WriteLine($"list2 count: {list2.Count}");
            Console.WriteLine($"Same count: {same.Count}");

            Console.WriteLine($"Diff count list1: {diff1.Count}");
            Console.WriteLine("Different Keys in List1");
            foreach (var item in diff1)
            {
                Console.WriteLine(item);
            }

            Console.WriteLine($"Diff count list2: {diff2.Count}");
            Logger.WriteInfo("Different Keys in List2");
            foreach (var item in diff2)
            {
                Logger.WriteInfo(item);
            }

            Assert.IsTrue(diff1.Count < 2, "192.168.197.34 diff");
            Assert.IsTrue(diff2.Count < 2, "192.168.197.13 diff");
        }

        [TestMethod]
        public void CompareTheSame3()
        {
            var rh1 = new RedisHelper("192.168.197.29");
            var rh2 = new RedisHelper("192.168.197.13");
            var list1 = rh1.GetAllKeys();
            var list2 = rh2.GetAllKeys();

            var same = RedisHelper.GetIntersection(list1, list2);
            var diff1 = RedisHelper.GetExceptList(list1, list2);
            var diff2 = RedisHelper.GetExceptList(list2, list1);
            Logger.WriteInfo($"list1 count: {list1.Count}");
            Logger.WriteInfo($"list2 count: {list2.Count}");
            Logger.WriteInfo($"Same count: {same.Count}");

            Logger.WriteInfo($"Diff count list1: {diff1.Count}");
            Logger.WriteInfo("Different Keys in List1");
            foreach (var item in diff1)
            {
                Logger.WriteInfo(item);
            }

            Logger.WriteInfo($"Diff count list2: {diff2.Count}");
            Logger.WriteInfo("Different Keys in List2");
            foreach (var item in diff2)
            {
                Logger.WriteInfo(item);
            }

            Assert.IsTrue(diff1.Count < 2, "192.168.197.20 diff");
            Assert.IsTrue(diff2.Count < 2, "192.168.197.13 diff");
        }

        [TestMethod]
        public void CompareDbValue()
        {
            var rh1 = new RedisHelper("192.168.197.34");
            var rh2 = new RedisHelper("192.168.197.29");
            var list1 = rh1.GetAllKeys();
            var list2 = rh2.GetAllKeys();

            var same = RedisHelper.GetIntersection(list1, list2);
            int sameCount = 0;
            int diffCount = 0;
            foreach(var item in same)
            {
                var byteInfo1 = rh1.GetT<byte[]>(item);
                var byteInfo2 = rh2.GetT<byte[]>(item);
                string hex1 = BitConverter.ToString(byteInfo1, 0).Replace("-", string.Empty).ToLower();
                string hex2 = BitConverter.ToString(byteInfo2, 0).Replace("-", string.Empty).ToLower();
                if (hex1 != hex2)
                {
                    diffCount++;
                    Logger.WriteInfo($"key: {item}");
                    Logger.WriteInfo($"value1: {hex1}");
                    Logger.WriteInfo($"value2: {hex2}");
                    Logger.WriteInfo(String.Empty);
                }
                else
                    sameCount++;
            }
            Logger.WriteInfo($"Same:{sameCount}, Diff:{diffCount}");
        }

        [DataTestMethod]
        [DataRow("192.168.199.221", "0x0a20cd56c83bee574ab9ed2e8cd85028ba1c730323e7819f7507c132a084c2a4517a120b5472616e73616374696f6e")]
        public void QueryRedisKeyInfo(string redishost, string key)
        {
            var rh = new RedisHelper(redishost);
            var ktm = new KeyTypeManager(rh);
            var keyInfo = ktm.GetKeyInfo(key);
            var hash = new AElf.Kernel.Hash(keyInfo.KeyObject.Value);
            Logger.WriteInfo($"ConvertValue={hash.ToHex()}, ObjectValue={keyInfo.ValueInfo}");
        }

        [DataTestMethod]
        [DataRow("192.168.199.221", "http://192.168.199.221:8000/chain")]
        public void ScanDBInformation(string redishost, string rpcUrl)
        {
            //Rpc Info analyze
            RpcAPI ra = new RpcAPI(rpcUrl);
            List<BlockInfo> BlockCollection = new List<BlockInfo>();
            int height = ra.GetCurrentHeight();

            var rh = new RedisHelper(redishost);
            var ktm = new KeyTypeManager(rh);
            //Get Block info
            for (int i = 1; i < height; i++)
            {
                var jsonInfo = ra.GetBlockInfo(i);
                var block = new BlockInfo(i, jsonInfo);
                BlockCollection.Add(block);
            }

            //Redis Info analyze
            ktm.GetAllKeyInfoCollection();
            ktm.PrintSummaryInfo(false);

            Logger.WriteInfo("Begin print block info by height");
            Logger.WriteInfo("-------------------------------------------------------------------------------------------------------------");
            //Analyze keys in collection
            foreach (var block in BlockCollection)
            {
                Logger.WriteInfo($"Block Height: {block.Height}, TxCount:{block.Transactions.Count}");
                //Analyze Blockhash
                    var keyinfoList = ktm.HashList["Hash"]
                    .FindAll(o=>o.Checked==false)
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
                                .FindAll(o=>o.Checked==false)
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
                    .FindAll(o=>o.Checked==false)
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
                    .FindAll(o=>o.Checked==false)
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
                        .FindAll(o=>o.Checked==false)
                        .FirstOrDefault(o => o.ValueInfo.ToString().Contains(checkStr));
                    if (transactionInfo != null)
                    {
                        transactionInfo.Checked = true;
                        Logger.WriteInfo(transactionInfo.ToString());
                    }

                    //Transaction Result
                    var transactionResult = ktm.HashList["TransactionResult"]
                        .FindAll(o=>o.Checked==false)
                        .FirstOrDefault(o => o.ValueInfo.ToString().Contains(transaction.Trim()));

                    if (transactionResult != null)
                    {
                        transactionResult.Checked = true;
                        Logger.WriteInfo(transactionResult.ToString());
                    }
                }
                Logger.WriteInfo("-------------------------------------------------------------------------------------------------------------");
            }

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
