using System;
using System.IO;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AElf.Automation.Contracts.ScenarioTest
{
    [TestClass]
    public class NetworkTest
    {
        private readonly ILogHelper _logger = LogHelper.GetLogger();

        private readonly string MainChainAddress2 = "192.168.197.56:6802";
        private readonly string MainChainAddress3 = "192.168.197.56:6803";
        private readonly string MainChainAddress4 = "192.168.197.56:6804";
        private INodeManager _ch1 { get; set; }
        private INodeManager _ch2 { get; set; }
        private INodeManager _ch3 { get; set; }
        private INodeManager _ch4 { get; set; }

        private INodeManager _s1ch1 { get; set; }
        private INodeManager _s1ch2 { get; set; }
        private INodeManager _s1ch3 { get; set; }
        private INodeManager _s1ch4 { get; set; }

        private INodeManager _s2ch1 { get; set; }
        private INodeManager _s2ch2 { get; set; }
        private INodeManager _s2ch3 { get; set; }
        private INodeManager _s2ch4 { get; set; }

        [TestInitialize]
        public void InitTest()
        {
            //Init Logger
            var logName = "ContractTest_" + DateTime.Now.ToString("MMddHHmmss") + ".log";
            var dir = Path.Combine(CommonHelper.AppRoot, "logs", logName);
            _logger.InitLogHelper(dir);

            _ch1 = new NodeManager("http://192.168.197.56:8001");
            _ch2 = new NodeManager("http://192.168.197.56:8002");
            _ch3 = new NodeManager("http://192.168.197.56:8003");
            _ch4 = new NodeManager("http://192.168.197.56:8004");

            _s1ch1 = new NodeManager("http://192.168.197.56:8011");
            _s1ch2 = new NodeManager("http://192.168.197.56:8012");
            _s1ch3 = new NodeManager("http://192.168.197.56:8013");
            _s1ch4 = new NodeManager("http://192.168.197.56:8014");

            _s2ch1 = new NodeManager("http://192.168.197.70:8011");
            _s2ch2 = new NodeManager("http://192.168.197.70:8012");
            _s2ch3 = new NodeManager("http://192.168.197.70:8013");
            _s2ch4 = new NodeManager("http://192.168.197.70:8014");
        }

        [TestMethod]
        public void TestGetPeer()
        {
            GetPeer(_ch2);
//            GetPeer(_ch2);
//            GetPeer(_ch3);
//            GetPeer(_ch4);  
        }

        [TestMethod]
        public void TestRemovePeer()
        {
            RemovePeer(MainChainAddress2, _ch1);
            RemovePeer(MainChainAddress3, _ch1);
            RemovePeer(MainChainAddress4, _ch1);

            GetPeer(_ch1);
            GetPeer(_ch2);
        }

        [TestMethod]
        public void TestAddPeer()
        {
            AddPeer(MainChainAddress2, _ch1);
            AddPeer(MainChainAddress3, _ch1);
            AddPeer(MainChainAddress4, _ch1);

            GetPeer(_ch1);
            GetPeer(_ch2);
        }


        public void GetPeer(INodeManager wa)
        {
            var peers = wa.NetGetPeers();

            foreach (var res in peers) _logger.Info(res.IpAddress);
        }


        public void AddPeer(string address, INodeManager wa)
        {
            var result = wa.NetAddPeer(address);
            Assert.IsTrue(result, "Add peer request failed.");
        }

        public void RemovePeer(string address, INodeManager wa)
        {
            var result = wa.NetRemovePeer(address);
            Assert.IsTrue(result, "Remove peer request failed.");
        }

        [TestMethod]
        [DataRow("192.168.197.13:6800")]
        [DataRow("192.168.197.29:6800")]
        public void TestAddPeer1(string address)
        {
            var result = _ch1.NetAddPeer(address);
            Assert.IsTrue(result, "Add peer request failed.");
            TestGetPeer();
        }

        [TestMethod]
        [DataRow("192.168.197.13:6800")]
        [DataRow("192.168.197.29:6800")]
        public void TestRemovePeer1(string address)
        {
            var result = _ch1.NetRemovePeer(address);
            Assert.IsTrue(result, "Remove peer request failed.");

            TestGetPeer();
        }

        [TestMethod]
        public void TestGetPeer2()
        {
            var peers = _ch2.NetGetPeers();
            _logger.Info(peers.ToString());
        }

        [TestMethod]
        [DataRow("192.168.197.34:6800")]
        [DataRow("192.168.197.29:6800")]
        public void TestAddPeer2(string address)
        {
            var result = _ch2.NetAddPeer(address);
            Assert.IsTrue(result, "Add peer request failed.");

            TestGetPeer2();
        }

        [TestMethod]
        [DataRow("192.168.197.34:6800")]
        [DataRow("192.168.197.29:6800")]
        public void TestRemovePeer2(string address)
        {
            var result = _ch2.NetRemovePeer(address);
            Assert.IsTrue(result, "Remove peer request failed.");

            TestGetPeer();
        }

        [TestMethod]
        public void TestGetPeer3()
        {
            var peers = _ch3.NetGetPeers();
            _logger.Info(peers.ToString());
        }

        [TestMethod]
        [DataRow("192.168.197.34:6800")]
        [DataRow("192.168.197.13:6800")]
        public void TestAddPeer3(string address)
        {
            var result = _ch3.NetAddPeer(address);
            Assert.IsTrue(result, "Add peer request failed.");

            TestGetPeer3();
        }

        [TestMethod]
        [DataRow("192.168.197.34:6800")]
        [DataRow("192.168.197.13:6800")]
        public void TestRemovePeer3(string address)
        {
            var result = _ch3.NetRemovePeer(address);
            Assert.IsTrue(result, "Remove peer request failed.");

            TestGetPeer3();
        }
    }
}