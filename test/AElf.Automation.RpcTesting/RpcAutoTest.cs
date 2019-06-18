using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using AElf.Automation.Common.OptionManagers;
using AElf.Automation.Common.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;

namespace AElf.Automation.RpcTesting
{
    [TestClass]
    public class RpcAutoTest
    {
        private readonly ILogHelper _logger = LogHelper.GetLogHelper();
        private const string ApiUrl = "http://192.168.197.34:8000";
        private RpcRequestManager _request;

        [TestInitialize]
        public void InitTestLog()
        {
            string logName = "RpcAutoTest_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".log";
            string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", logName);
            _logger.InitLogHelper(dir);
            _request = new RpcRequestManager(ApiUrl);
        }

        [TestMethod]
        public void GetConnectChain()
        {
            string method = "GetChainInformation";
            string parameter = "{}";

            string response = _request.PostRequest(method, parameter, out var code);
            Console.WriteLine(response);
            Assert.AreEqual("OK", code);
            Assert.IsTrue(response.Contains("chain_id"));
        }

        [TestMethod]
        public void GetBlockHeight()
        {
            string method = "GetBlockHeight";
            string parameter = "{}";

            string response = _request.PostRequest(method, parameter, out var code);
            Console.WriteLine(response);
            Assert.AreEqual("OK", code);
            Assert.IsTrue(response.Contains("block_height"));
        }

        [TestMethod]
        [DataRow(24)]
        public void GetBlockInfo(int height)
        {
            var method = "GetBlockInfo";
            var parameter = "{\"block_height\":\"" + height.ToString() + "\"}";

            string response = _request.PostRequest(method, parameter, out var code);
            Console.WriteLine(response);
            Assert.AreEqual("OK", code);
            Assert.IsTrue(response.Contains("Blockhash"));
        }

        [TestMethod]
        public void GetCommands()
        {
            var method = "GetCommands";
            var parameter = "{}";

            string response = _request.PostRequest(method, parameter, out var code);
            Console.WriteLine(response);
            Assert.AreEqual("OK", code);
            Assert.IsTrue(response.Contains("commands"));
        }

        [TestMethod]
        public void GetContractAbi()
        {
            string method0 = "GetChainInformation";
            string parameter0 = "{}";
            string response0 = _request.PostRequest(method0, parameter0, out var code0);
            Console.WriteLine(response0);
            Assert.AreEqual("OK", code0);
            var result = DataHelper.TryGetValueFromJson(out var genesisAbi, response0, "result", "result",
                "GenesisContractAddress");
            Assert.IsTrue(result, "Genesis token abi is not exist.");

            var method = "GetContractAbi";
            var parameter = "{\"address\":\"" + genesisAbi + "\"}";
            string response = _request.PostRequest(method, parameter, out var code);
            Console.WriteLine(response);
            Assert.AreEqual("OK", code);
            Assert.IsTrue(response.Contains("address"));
        }

        [DataTestMethod]
        [DataRow("90a624f2481cd48bf16b613dbb287a470dde579eb03031327a4a8dcb72a2be0c")]
        public void GetTxResult(string txId)
        {
            var method = "GetTransactionResult";
            var parameter = "{\"transactionId\":\"" + txId + "\"}";

            string response = _request.PostRequest(method, parameter, out var code);
            Console.WriteLine(response);
            Assert.AreEqual("OK", code);
            Assert.IsTrue(response.Contains("tx_info"));
        }

        [DataTestMethod]
        [DataRow("http://192.168.197.34:8000/chain")]
        public void GetAllBlocksInfo(string rpcUrl)
        {
            var ch = new WebApiHelper(rpcUrl);
            List<string> transactionIds = new List<string>();

            string method = "GetBlockHeight";
            var ci = new CommandInfo(method);
            ch.ExecuteCommand(ci);
            ci.GetJsonInfo();
            var result = ci.JsonInfo;
            string countStr = result["result"].ToString();
            int currentHeight = Int32.Parse(countStr);

            for (int i = 1; i <= currentHeight; i++)
            {
                method = "GetBlockInfo";
                ci = new CommandInfo(method);
                ci.Parameter = $"{i.ToString()} true";
                ch.ExecuteCommand(ci);
                ci.GetJsonInfo();
                result = ci.JsonInfo;
                string txcount = result["result"]["Body"]["TransactionsCount"].ToString();
                string[] transactions = result["result"]["Body"]["Transactions"].ToString().Replace("[\n", "")
                    .Replace("\n]", "").Replace("\"", "").Split(",");
                foreach (var tx in transactions)
                {
                    if (tx.Trim() != "")
                        transactionIds.Add(tx.Trim());
                }

                string txPoolSize = result["result"]["CurrentTransactionPoolSize"].ToString();
                _logger.WriteInfo("Height: {0},  TxCount: {1}, TxPoolSize: {2}, Time: {3}", i, txcount, txPoolSize,
                    DateTime.Now.ToString(CultureInfo.CurrentCulture));
                Thread.Sleep(50);
            }

            //Query tx result informtion
            _logger.WriteInfo("Begin tx result query.");
            for (int i = 0; i < 10; i++)
            {
                foreach (var tx in transactionIds)
                {
                    method = "GetTransactionResult";
                    ci = new CommandInfo(method);
                    ci.Parameter = tx;
                    ch.ExecuteCommand(ci);
                    Thread.Sleep(10);
                }
            }

            _logger.WriteInfo("Complete all query operation.");
        }

        [DataTestMethod]
        public void GetInTimeBlockInfo(string rpcUrl)
        {
            int value = 0;
            for (int i = 1; i > 0; i++)
            {
                string method = "GetBlockHeight";
                string parameter = "{}";

                var request = new RpcRequestManager(rpcUrl);
                string response = request.PostRequest(method, parameter, out var code);
                Console.WriteLine(response);
                Assert.AreEqual("OK", code);
                var result = JObject.Parse(response);
                string countStr = result["result"].ToString();
                if (value == Int32.Parse(countStr))
                    continue;

                value = Int32.Parse(countStr);
                string count = (Int32.Parse(countStr) - 1).ToString();
                method = "GetBlockInfo";

                parameter = "{\"block_height\":\"" + count + "\",\"include_txs\":\"true\"}";

                request = new RpcRequestManager(rpcUrl);
                response = request.PostRequest(method, parameter, out code);
                Console.WriteLine(response);
                Assert.AreEqual("OK", code);
                result = JObject.Parse(response);
                string txcount = result["result"]["Body"]["TransactionsCount"].ToString();
                string txPoolSize = result["result"]["CurrentTransactionPoolSize"].ToString();
                System.Diagnostics.Debug.WriteLine("Height: {0},  TxCount: {1}, TxPoolSize: {2}, Time: {3}", count,
                    txcount, txPoolSize, DateTime.Now.ToString(CultureInfo.CurrentCulture));
                Thread.Sleep(1000);
            }
        }

        [DataTestMethod]
        [DataRow(90, 110)]
        public void GetTxCounts(int begin, int end)
        {
            List<object> blockInfos = new List<object>();
            string url = "http://192.168.199.221:8000/chain";
            var ch = new WebApiHelper(url);
            var ci = new CommandInfo(ApiMethods.GetBlockInfo);
            for (int i = begin; i <= end; i++)
            {
                dynamic blockInfo = new System.Dynamic.ExpandoObject();
                int height = i;
                ci.Parameter = height.ToString();
                ch.ExecuteCommand(ci);
                Assert.IsTrue(ci.Result);
                ci.GetJsonInfo();

                var result = ci.JsonInfo;
                string count = result["result"]["Body"]["TransactionsCount"].ToString();
                string txpoolSize = result["result"]["CurrentTransactionPoolSize"].ToString();
                blockInfo.Height = height;
                blockInfo.TxCount = count;
                blockInfo.PoolSize = txpoolSize;
                blockInfos.Add(blockInfo);
            }

            foreach (dynamic item in blockInfos)
            {
                string message =
                    $"Height: {item.Height}, TxCount: {item.TxCount}, CurrentTransactionPoolSize: {item.PoolSize}";
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
            string method = "GetBlockHeight";
            string parameter = "{}";

            var request = new RpcRequestManager(url1);
            string response = request.PostRequest(method, parameter, out var code);
            var result = JObject.Parse(response);

            Assert.AreEqual("OK", code);
            Console.WriteLine(result["result"]);
            int height = Int32.Parse(result["result"].ToString());
            for (int i = 0; i < height; i++)
            {
                method = "GetBlockInfo";
                parameter = "{\"block_height\":\"" + i + "\"}";
                var request1 = new RpcRequestManager(url1);
                var request2 = new RpcRequestManager(url2);

                string response1 = request1.PostRequest(method, parameter, out var code1);
                string response2 = request2.PostRequest(method, parameter, out var code2);
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
            string method = "GetBlockHeight";
            string parameter = "{}";

            var request = new RpcRequestManager(url1);
            string response = request.PostRequest(method, parameter, out var code);
            var result = JObject.Parse(response);

            Assert.AreEqual("OK", code);
            Console.WriteLine(result["result"]);
            int height = Int32.Parse(result["result"].ToString());
            for (int i = 0; i < height; i++)
            {
                method = "GetBlockInfo";
                parameter = "{\"block_height\":\"" + i + "\"}";

                var request1 = new RpcRequestManager(url1);
                var request2 = new RpcRequestManager(url2);

                string response1 = request1.PostRequest(method, parameter, out var code1);
                string response2 = request2.PostRequest(method, parameter, out var code2);
                Assert.AreEqual("OK", code1);
                Assert.AreEqual("OK", code2);
                Assert.AreEqual(response1, response2);
            }
        }
    }
}