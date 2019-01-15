using System;
using System.IO;
using System.Net.Http.Headers;
using AElf.Automation.Common.Extensions;
using AElf.Automation.Common.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting.Logging;
using Microsoft.Win32.SafeHandles;
using NServiceKit.Common.Extensions;
using QuickGraph;

namespace AElf.Automation.Contracts.ScenarioTest
{
    [TestClass]
    public class RpcApiTest
    {
        private ILogHelper _logger = LogHelper.GetLogHelper();
        private CliHelper _ch { get; set; }
        private const string _serviceUrl = "http://192.168.197.15:8010";

        [TestInitialize]
        public void InitTest()
        {
            //Init Logger
            string logName = "RpcApiTest.log";
            string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", logName);
            _logger.InitLogHelper(dir);

            _ch = new CliHelper(_serviceUrl, AccountManager.GetDefaultDataDir());
        }

        [TestMethod]
        [DataRow(26)]
        public void VerifyTransactionByHeight(int height)
        {
            var ci = new CommandInfo("get_block_info");
            ci.Parameter = $"{height.ToString()} {true}";
            _ch.RpcGetBlockInfo(ci);
            Assert.IsTrue(ci.Result);

            DataHelper.TryGetArrayFromJson(out var txArray, ci.InfoMsg[0], "result", "result", "Body", "Transactions");

            foreach (var txId in txArray)
            {
                var txCi = new CommandInfo("get_tx_result");
                txCi.Parameter = txId;
                _ch.RpcGetTxResult(txCi);
                Assert.IsTrue(txCi.Result);

                DataHelper.TryGetValueFromJson(out var status, txCi.InfoMsg[0], "result", "result", "tx_status");
                if(status == "Mined")
                    _logger.WriteInfo($"{txId}: Mined");
                else
                    _logger.WriteError(txCi.InfoMsg[0]);

            }

        }

    }
}