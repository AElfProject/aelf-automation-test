using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using AElf.Automation.Common.Extensions;
using AElf.Automation.Common.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AElf.Automation.RpcTesting
{
    [TestClass]
    public class RpcAutoTest
    {
        public ILogHelper Logger = LogHelper.GetLogHelper();

        [TestInitialize]
        public void InitTestLog()
        {
            string logName = "RpcAutoTest_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".log";
            string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", logName);
            Logger.InitLogHelper(dir);
        }

        [TestMethod]
        public void GetBlockHeight()
        {
            string url = "http://192.168.197.34:8000";
            string method = "get_block_height";
            string parameter = "{}";
            string code = string.Empty;

            var request = new RpcRequestManager(url);
            string response = request.PostRequest(method, parameter, out code);
            Console.WriteLine(response);
            Assert.AreEqual("OK", code);
            Assert.IsTrue(response.Contains("block_height"));
        }

        [TestMethod]
        public void GetBlockInfo()
        {
            string url = "http://192.168.197.34:8000";
            string method = "get_block_info";
            int height = 5;
            string parameter = "{\"block_height\":\"" + height + "\"}";
            string code = string.Empty;

            var request = new RpcRequestManager(url);
            string response = request.PostRequest(method, parameter, out code);
            Console.WriteLine(response);
            Assert.AreEqual("OK", code);
            Assert.IsTrue(response.Contains("ChainId"));
        }


        [DataTestMethod]
        [DataRow("http://192.168.199.221:8000/chain", "0x038807f8d022d5e0203ddd81e8b47a06c7510153eec5a1670428060c2ca34c9a")]
        public void GetTxResult(string rpcUrl, string txId)
        {
            var CH = new CliHelper(rpcUrl);

            string method = "get_tx_result";
            var ci = new CommandInfo(method);
            ci.Parameter = txId;
            CH.ExecuteCommand(ci);
            ci.GetJsonInfo();
        }

        [DataTestMethod]
        [DataRow("http://192.168.197.35:8000/chain")]
        public void GetAllBlocksInfo(string rpcUrl)
        {
            var CH = new CliHelper(rpcUrl);
            List<string> transactionIds = new List<string>();

            string method = "get_block_height";
            var ci = new CommandInfo(method);
            CH.ExecuteCommand(ci);
            ci.GetJsonInfo();
            var result = ci.JsonInfo;
            string countStr = result["result"]["result"]["block_height"].ToString();
            int currentHeight = Int32.Parse(countStr);

            for (int i = 1; i <= currentHeight; i++)
            {
                method = "get_block_info";
                ci = new CommandInfo(method);
                ci.Parameter = $"{i.ToString()} true";
                CH.ExecuteCommand(ci);
                ci.GetJsonInfo();
                result = ci.JsonInfo;
                string txcount = result["result"]["result"]["Body"]["TransactionsCount"].ToString();
                string[] transactions = result["result"]["result"]["Body"]["Transactions"].ToString().Replace("[\n", "").Replace("\n]", "").Replace("\"", "").Split(",");
                foreach (var tx in transactions)
                {
                    if(tx.Trim() != "")
                        transactionIds.Add(tx.Trim());
                    /*
                    method = "get_tx_result";
                    ci = new CommandInfo(method);
                    ci.Parameter = tx.Trim();
                    if (ci.Parameter == "")
                        break;
                    CH.ExecuteCommand(ci);
                    Thread.Sleep(20);
                    */
                }
                string txPoolSize = result["result"]["result"]["CurrentTransactionPoolSize"].ToString();
                Logger.WriteInfo("Height: {0},  TxCount: {1}, TxPoolSize: {2}, Time: {3}", i, txcount, txPoolSize, DateTime.Now.ToString());
                Thread.Sleep(50);
            }

            //Query tx result informtion
            Logger.WriteInfo("Begin tx result query.");
            for (int i = 0; i < 10; i++)
            {
                foreach (var tx in transactionIds)
                {
                    method = "get_tx_result";
                    ci = new CommandInfo(method);
                    ci.Parameter = tx;
                    CH.ExecuteCommand(ci);
                    Thread.Sleep(10);
                }
            }

            Logger.WriteInfo("Complete all query operation.");
        }

        [DataTestMethod]
        [DataRow("http://192.168.199.221:8000/chain")]
        public void GetInTimeBlockInfo(string rpcUrl)
        {
            int value = 0;
            for (int i = 1; i > 0; i++)
            {
                string method = "get_block_height";
                string parameter = "{}";
                string code = string.Empty;

                var request = new RpcRequestManager(rpcUrl);
                string response = request.PostRequest(method, parameter, out code);
                Console.WriteLine(response);
                Assert.AreEqual("OK", code);
                var result = JObject.Parse(response);
                string countStr = result["result"]["result"]["block_height"].ToString();
                if (value == Int32.Parse(countStr))
                    continue;

                value = Int32.Parse(countStr);
                string count = (Int32.Parse(countStr) - 1).ToString();
                method = "get_block_info";

                parameter = "{\"block_height\":\"" + count + "\",\"include_txs\":\"true\"}";
                code = string.Empty;

                request = new RpcRequestManager(rpcUrl);
                response = request.PostRequest(method, parameter, out code);
                Console.WriteLine(response);
                Assert.AreEqual("OK", code);
                result = JObject.Parse(response);
                string txcount = result["result"]["result"]["Body"]["TransactionsCount"].ToString();
                string txPoolSize = result["result"]["result"]["CurrentTransactionPoolSize"].ToString();
                System.Diagnostics.Debug.WriteLine("Height: {0},  TxCount: {1}, TxPoolSize: {2}, Time: {3}", count, txcount, txPoolSize, DateTime.Now.ToString());
                Thread.Sleep(1000);
            }
        }

        [DataTestMethod]
        [DataRow(90, 110)]
        public void GetTxCounts(int begin, int end)
        {
            List<object> blockInfos = new List<object>();
            string url = "http://192.168.199.221:8000/chain";
            var ch = new CliHelper(url);
            var ci = new CommandInfo("get_block_info");
            for(int i= begin; i<=end; i++)
            {
                dynamic blockInfo = new System.Dynamic.ExpandoObject();
                int height = i;
                ci.Parameter = height.ToString();
                ch.ExecuteCommand(ci);
                Assert.IsTrue(ci.Result);
                ci.GetJsonInfo();

                var result = ci.JsonInfo;
                string count = result["result"]["result"]["Body"]["TransactionsCount"].ToString();
                string txpoolSize = result["result"]["result"]["CurrentTransactionPoolSize"].ToString();
                blockInfo.Height = height;
                blockInfo.TxCount = count;
                blockInfo.PoolSize = txpoolSize;
                blockInfos.Add(blockInfo);
            }
            foreach(dynamic item in blockInfos)
            {
                string message = $"Height: {item.Height}, TxCount: {item.TxCount}, CurrentTransactionPoolSize: {item.PoolSize}";
                Console.WriteLine(message);
                System.Diagnostics.Debug.WriteLine(message);
            }
            Console.ReadLine();
        }

        [TestMethod]
        public void CompareBlockInfo1()
        {
            string url1 = "http://192.168.197.34:8000";
            string url2 = "http://192.168.197.13:8000";

            //Get Block Height
            string method = "get_block_height";
            string parameter = "{}";
            string code = string.Empty;

            var request = new RpcRequestManager(url1);
            string response = request.PostRequest(method, parameter, out code);
            var result = JObject.Parse(response);

            Assert.AreEqual("OK", code);
            Console.WriteLine(result["result"]["result"]);
            int height = Int32.Parse(result["result"]["result"]["block_height"].ToString());
            for(int i=0; i<height; i++)
            {
                method = "get_block_info";
                parameter = "{\"block_height\":\"" + i + "\"}";
                string code1 = string.Empty;
                string code2 = string.Empty;
                var request1= new RpcRequestManager(url1);
                var request2 = new RpcRequestManager(url2);

                string response1 = request.PostRequest(method, parameter, out code1);
                string response2 = request.PostRequest(method, parameter, out code2);
                Assert.AreEqual("OK", code1);
                Assert.AreEqual("OK", code2);
                Assert.AreEqual(response1, response2);
            }
        }

        [TestMethod]
        public void CompareBlockInfo2()
        {
            string url1 = "http://192.168.197.34:8000";
            string url2 = "http://192.168.197.29:8000";

            //Get Block Height
            string method = "get_block_height";
            string parameter = "{}";
            string code = string.Empty;

            var request = new RpcRequestManager(url1);
            string response = request.PostRequest(method, parameter, out code);
            var result = JObject.Parse(response);

            Assert.AreEqual("OK", code);
            Console.WriteLine(result["result"]["result"]);
            int height = Int32.Parse(result["result"]["result"]["block_height"].ToString());
            for (int i = 0; i < height; i++)
            {
                method = "get_block_info";
                parameter = "{\"block_height\":\"" + i + "\"}";
                string code1 = string.Empty;
                string code2 = string.Empty;
                var request1 = new RpcRequestManager(url1);
                var request2 = new RpcRequestManager(url2);

                string response1 = request.PostRequest(method, parameter, out code1);
                string response2 = request.PostRequest(method, parameter, out code2);
                Assert.AreEqual("OK", code1);
                Assert.AreEqual("OK", code2);
                Assert.AreEqual(response1, response2);
            }
        }

        [TestMethod]
        public void TestRedisKeyInfo()
        {
            var rh = new RedisHelper("192.168.197.13");
            var list = rh.GetAllKeys();
            Console.WriteLine("Total key counts: {0}", list.Count);

            TypeSummarySet tss = new TypeSummarySet();
            int count = 0;
            foreach (var item in list)
            {
                if (item == "ping")
                    continue;
                count++;
                var byteArray = rh.GetT<byte[]>(item);
                string hexValue = BitConverter.ToString(byteArray, 0).Replace("-", string.Empty).ToLower();

                var inputByteArray = new byte[hexValue.Length / 2];
                for (var x = 0; x < inputByteArray.Length; x++)
                {
                    var i = Convert.ToInt32(hexValue.Substring(x * 2, 2), 16);
                    inputByteArray[x] = (byte)i;
                }

                //Rpc Call
                string url = "http://192.168.197.13:8000";
                string method = "get_deserialized_info";
                string parameter = "{\"key\":\"" + item + "\",\"value\":\"" + hexValue + "\"}";
                string code = string.Empty;

                try
                {
                    var request = new RpcRequestManager(url);
                    string response = request.PostRequest(method, parameter, out code);
                    Console.WriteLine($"Key {count} : {item}");
                    Console.WriteLine(response);
                    var result = JObject.Parse(response);
                    string type = result["result"]["TypeName"].ToString();
                    string value = result["result"]["Value"].ToString();

                    var data = new DataResult(item, type, value);
                    tss.AddDataResult(data);
                    tss.AddTypeSummary(data);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Analyze {0} key got exception: {1}", item, ex.Message);
                }
            }

            foreach(var item in tss.SummaryInfo)
            {
                Console.WriteLine($"{item.TypeName} : {item.Count}");
            }
            Assert.IsTrue(true);
        }

        [TestMethod]
        public void TestMultiNodesDbData()
        {
            var rh1 = new RedisHelper("192.168.197.34");
            var rh2 = new RedisHelper("192.168.197.13");
            var rh3 = new RedisHelper("192.168.197.29");

            var list1 = rh1.GetAllKeys();
            var list2 = rh2.GetAllKeys();
            var list3 = rh2.GetAllKeys();

            Console.WriteLine("Node1 total keys: {0}", list1.Count);
            Console.WriteLine("Node2 total keys :{0}", list2.Count);
            Console.WriteLine("Node3 total keys :{0}", list3.Count);

            var same12 = RedisHelper.GetIntersection(list1, list2);
            var same13 = RedisHelper.GetIntersection(list1, list3);
            var same23 = RedisHelper.GetIntersection(list2, list3);

            var same123 = RedisHelper.GetIntersection(same12, same13);

            Console.WriteLine("Node12 same keys count: {0}", same12.Count);
            Console.WriteLine("Node13 same keys count: {0}", same13.Count);
            Console.WriteLine("Node23 same keys count: {0}", same23.Count);

            var diff12 = RedisHelper.GetExceptList(list1, list2);
            var diff21 = RedisHelper.GetExceptList(list2, list1);

            Console.WriteLine("Diff12 info(length): {0}", diff12.Count);
            foreach (var item in diff12)
            {
                Console.WriteLine("Diff12: {0}", item);
            }

            Console.WriteLine("Diff21 info(length): {0}", diff21.Count);
            foreach (var item in diff21)
            {
                Console.WriteLine("Diff21: {0}", item);
            }

            var diff13 = RedisHelper.GetExceptList(list1, list3);
            var diff31 = RedisHelper.GetExceptList(list3, list1);

            Console.WriteLine("Diff13 info(length): {0}", diff13.Count);
            foreach (var item in diff13)
            {
                Console.WriteLine("Diff13: {0}", item);
            }

            Console.WriteLine("Diff31 info(length): {0}", diff31.Count);
            foreach (var item in diff31)
            {
                Console.WriteLine("Diff31: {0}", item);
            }

            var diff23 = RedisHelper.GetExceptList(list2, list3);
            var diff32 = RedisHelper.GetExceptList(list3, list2);

            Console.WriteLine("Diff23 info(length): {0}", diff23.Count);
            foreach (var item in diff23)
            {
                Console.WriteLine("Diff23: {0}", item);
            }

            Console.WriteLine("Diff23 info(length): {0}", diff32.Count);
            foreach (var item in diff32)
            {
                Console.WriteLine("Diff32: {0}", item);
            }

            TypeSummarySet tss1 = new TypeSummarySet();
            TypeSummarySet tss2 = new TypeSummarySet();
            TypeSummarySet tss3 = new TypeSummarySet();

            Console.WriteLine("Compare same key with diff data scenario:");
            foreach (var item in same123)
            {
                if (item == "ping")
                    continue;

                //Redis Value
                var byteArray1 = rh1.GetT<byte[]>(item);
                string hexValue1 = BitConverter.ToString(byteArray1, 0).Replace("-", string.Empty).ToLower();

                var byteArray2 = rh2.GetT<byte[]>(item);
                string hexValue2 = BitConverter.ToString(byteArray2, 0).Replace("-", string.Empty).ToLower();

                var byteArray3 = rh3.GetT<byte[]>(item);
                string hexValue3 = BitConverter.ToString(byteArray3, 0).Replace("-", string.Empty).ToLower();

                //Rpc Call
                string method = "get_deserialized_info";
                string url1 = "http://192.168.197.34:8000";
                string url2 = "http://192.168.197.13:8000";
                string url3 = "http://192.168.197.29:8000";

                if (hexValue1 != hexValue2)
                {
                    Console.WriteLine("Vaule1 Value2 diff: {0}", item);

                    string parameter1 = "{\"key\":\"" + item + "\",\"value\":\"" + hexValue1 + "\"}";
                    string code1 = string.Empty;

                    var request1 = new RpcRequestManager(url1);
                    string response1 = request1.PostRequest(method, parameter1, out code1);
                    Console.WriteLine("Db1 rpc data:\r\n" + response1);
                    var result1 = JObject.Parse(response1);
                    string type1 = result1["result"]["TypeName"].ToString();
                    string value1 = result1["result"]["Value"].ToString();

                    var data1 = new DataResult(item, type1, value1);
                    tss1.AddDataResult(data1);
                    tss1.AddTypeSummary(data1);

                    string parameter2 = "{\"key\":\"" + item + "\",\"value\":\"" + hexValue2 + "\"}";
                    string code2 = string.Empty;

                    var request2 = new RpcRequestManager(url2);
                    string response2 = request2.PostRequest(method, parameter2, out code2);
                    Console.WriteLine("Db2 rpc data:\r\n" + response2);
                    var result2 = JObject.Parse(response2);
                    string type2 = result2["result"]["TypeName"].ToString();
                    string value2 = result2["result"]["Value"].ToString();

                    var data2 = new DataResult(item, type2, value2);
                    tss2.AddDataResult(data2);
                    tss2.AddTypeSummary(data2);
                }

                if(hexValue1 != hexValue3)
                {
                    Console.WriteLine("Vaule1 Value3 diff: {0}", item);
                    string parameter3 = "{\"key\":\"" + item + "\",\"value\":\"" + hexValue3 + "\"}";
                    string code3 = string.Empty;

                    var request3 = new RpcRequestManager(url3);
                    string response3 = request3.PostRequest(method, parameter3, out code3);
                    Console.WriteLine("Db3 rpc data:\r\n" + response3);
                    var result3 = JObject.Parse(response3);
                    string type3 = result3["result"]["TypeName"].ToString();
                    string value3 = result3["result"]["Value"].ToString();

                    var data3 = new DataResult(item, type3, value3);
                    tss3.AddDataResult(data3);
                    tss3.AddTypeSummary(data3);
                }
                Assert.IsTrue(true);
            }
        }
    }
}
