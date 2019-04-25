using System;
using System.IO;
using AElf.Automation.Common.Extensions;
using AElf.Automation.Common.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting.Logging;
using Microsoft.Win32.SafeHandles;

namespace AElf.Automation.Contracts.ScenarioTest
{
    [TestClass]
    public class NetworkTest
    {
        private ILogHelper _logger = LogHelper.GetLogHelper();
        private RpcApiHelper _ch1 { get; set; }
        private RpcApiHelper _ch2 { get; set; }
        private RpcApiHelper _ch3 { get; set; }

        [TestInitialize]
        public void InitTest()
        {
            //Init Logger
            string logName = "ContractTest_" + DateTime.Now.ToString("MMddHHmmss") + ".log";
            string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", logName);
            _logger.InitLogHelper(dir);

            _ch1 = new RpcApiHelper("http://192.168.197.34:8000/net", AccountManager.GetDefaultDataDir());
            _ch2 = new RpcApiHelper("http://192.168.197.13:8000/net", AccountManager.GetDefaultDataDir());
            _ch3 = new RpcApiHelper("http://192.168.197.29:8000/net", AccountManager.GetDefaultDataDir());
        }

        [TestMethod]
        public void TestGetPeer1()
        {
            var ci = new CommandInfo("get_peers");
            _ch1.NetGetPeers(ci);
            Assert.IsTrue(ci.Result, "Get peers request failed.");
            ci.GetJsonInfo();
            _logger.WriteInfo(ci.JsonInfo.ToString());
        }

        [TestMethod]
        [DataRow("192.168.197.13:6800")]
        [DataRow("192.168.197.29:6800")]
        public void TestAddPeer1(string address)
        {
            var ci = new CommandInfo("add_peer");
            ci.Parameter = address;
            _ch1.NetAddPeer(ci);
            Assert.IsTrue(ci.Result, "Add peer request failed.");
            ci.GetJsonInfo();
            _logger.WriteInfo(ci.JsonInfo.ToString());

            TestGetPeer1();
        }

        [TestMethod]
        [DataRow("192.168.197.13:6800")]
        [DataRow("192.168.197.29:6800")]
        public void TestRemovePeer1(string address)
        {
            var ci = new CommandInfo("remove_peer");
            ci.Parameter = address;
            _ch1.NetRemovePeer(ci);
            Assert.IsTrue(ci.Result, "Remove peer request failed.");

            ci.GetJsonInfo();
            _logger.WriteInfo(ci.JsonInfo.ToString());

            TestGetPeer1();
        }

        [TestMethod]
        public void TestGetPeer2()
        {
            var ci = new CommandInfo("get_peers");
            _ch2.NetGetPeers(ci);
            Assert.IsTrue(ci.Result, "Get peers request failed.");
            ci.GetJsonInfo();
            _logger.WriteInfo(ci.JsonInfo.ToString());
        }

        [TestMethod]
        [DataRow("192.168.197.34:6800")]
        [DataRow("192.168.197.29:6800")]
        public void TestAddPeer2(string address)
        {
            var ci = new CommandInfo("add_peer");
            ci.Parameter = address;
            _ch2.NetAddPeer(ci);
            Assert.IsTrue(ci.Result, "Add peer request failed.");
            ci.GetJsonInfo();
            _logger.WriteInfo(ci.JsonInfo.ToString());

            TestGetPeer2();
        }

        [TestMethod]
        [DataRow("192.168.197.34:6800")]
        [DataRow("192.168.197.29:6800")]
        public void TestRemovePeer2(string address)
        {
            var ci = new CommandInfo("remove_peer");
            ci.Parameter = address;
            _ch2.NetRemovePeer(ci);
            Assert.IsTrue(ci.Result, "Remove peer request failed.");

            ci.GetJsonInfo();
            _logger.WriteInfo(ci.JsonInfo.ToString());

            TestGetPeer1();
        }

        [TestMethod]
        public void TestGetPeer3()
        {
            var ci = new CommandInfo("get_peers");
            _ch3.NetGetPeers(ci);
            Assert.IsTrue(ci.Result, "Get peers request failed.");
            ci.GetJsonInfo();
            _logger.WriteInfo(ci.JsonInfo.ToString());
        }

        [TestMethod]
        [DataRow("192.168.197.34:6800")]
        [DataRow("192.168.197.13:6800")]
        public void TestAddPeer3(string address)
        {
            var ci = new CommandInfo("add_peer");
            ci.Parameter = address;
            _ch3.NetAddPeer(ci);
            Assert.IsTrue(ci.Result, "Add peer request failed.");
            ci.GetJsonInfo();
            _logger.WriteInfo(ci.JsonInfo.ToString());

            TestGetPeer3();
        }

        [TestMethod]
        [DataRow("192.168.197.34:6800")]
        [DataRow("192.168.197.13:6800")]
        public void TestRemovePeer3(string address)
        {
            var ci = new CommandInfo("remove_peer");
            ci.Parameter = address;
            _ch3.NetRemovePeer(ci);
            Assert.IsTrue(ci.Result, "Remove peer request failed.");

            ci.GetJsonInfo();
            _logger.WriteInfo(ci.JsonInfo.ToString());

            TestGetPeer3();
        }
    }
}