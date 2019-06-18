using System;
using System.IO;
using AElf.Automation.Common.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace AElf.Automation.RpcTesting
{
    [TestClass]
    public class RpcApiTest
    {
        private WebApiHelper _rh;
        private const string ServiceUrl = "http://192.168.197.34:8000";
        private readonly ILogHelper _log = LogHelper.GetLogHelper();

        [TestInitialize]
        public void Initialize()
        {
            string logName = "RpcApiTest" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".log";
            string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", logName);
            _log.InitLogHelper(dir);
            _rh = new WebApiHelper(ServiceUrl);
        }

        [TestMethod]
        public void TestConnectChain()
        {
            var command = new CommandInfo(ApiMethods.GetChainInformation);
            _rh.GetChainInformation(command);

            command.InfoMsg.ShouldBeNull();
        }
    }
}