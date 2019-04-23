using System;
using System.IO;
using AElf.Automation.Common.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AElf.Automation.RpcTesting
{
    [TestClass]
    public class RpcApiTest
    {
        private RpcHelper _rh;
        private const string ServiceUrl = "http://192.168.197.34:8000";
        private readonly ILogHelper _log = LogHelper.GetLogHelper();

        [TestInitialize]
        public void Initialize()
        {
            string logName = "RpcApiTest" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".log";
            string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", logName);
            _log.InitLogHelper(dir);
            _rh = new RpcHelper(ServiceUrl);
        }

        [TestMethod]
        public void TestConnectChain()
        {
            var info = _rh.GetChainInformation();
        }

        [TestMethod]
        [DataRow("ELF_6jPmaBi4U8Q6E1skXLM9ubDykwYmMUpm3QbeqZSZV3NyotD")]
        public void QueryContractAbi(string address)
        {
            var abiInfo = _rh.QueryContractAbi(address);
        }
    }
}