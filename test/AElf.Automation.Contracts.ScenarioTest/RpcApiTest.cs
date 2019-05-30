using System;
using System.IO;
using AElf.Automation.Common.OptionManagers;
using AElf.Automation.Common.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AElf.Automation.Contracts.ScenarioTest
{
    [TestClass]
    public class RpcApiTest
    {
        private readonly ILogHelper _logger = LogHelper.GetLogHelper();
        private WebApiHelper Ch { get; set; }
        private const string ServiceUrl = "http://192.168.197.15:8020";

        [TestInitialize]
        public void InitTest()
        {
            //Init Logger
            string logName = "RpcApiTest.log";
            string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", logName);
            _logger.InitLogHelper(dir);

            Ch = new WebApiHelper(ServiceUrl, AccountManager.GetDefaultDataDir());
        }

        [TestMethod]
        [DataRow(2441)]
        public void VerifyTransactionByHeight(int height)
        {
            var ci = new CommandInfo(ApiMethods.GetBlockInfo);
            ci.Parameter = $"{height.ToString()} {true}";
            Ch.GetBlockByHeight(ci);
            Assert.IsTrue(ci.Result, "Request block info failed.");

            DataHelper.TryGetArrayFromJson(out var txArray, ci.InfoMsg.ToString(), "result", "result", "Body", "Transactions");

            foreach (var txId in txArray)
            {
                var txCi = new CommandInfo(ApiMethods.GetTransactionResult);
                txCi.Parameter = txId;
                Ch.GetTxResult(txCi);
                Assert.IsTrue(txCi.Result, "Request transaction result failed.");

                DataHelper.TryGetValueFromJson(out var status, txCi.InfoMsg.ToString(), "result", "result", "tx_status");
                if(status == "Mined")
                    _logger.WriteInfo($"{txId}: Mined");
                else
                    _logger.WriteError(txCi.InfoMsg.ToString());
            }
        }
    }
}