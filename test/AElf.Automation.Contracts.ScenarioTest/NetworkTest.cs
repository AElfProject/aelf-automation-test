using System;
using System.Collections.Generic;
using System.IO;
using AElf.Automation.Common.Helpers;
using AElfChain.SDK.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AElf.Automation.Contracts.ScenarioTest
{
    [TestClass]
    public class NetworkTest
    {
        private ILogHelper _logger = LogHelper.GetLogger();
        private WebApiHelper _ch1 { get; set; }
        private WebApiHelper _ch2 { get; set; }
        private WebApiHelper _ch3 { get; set; }
        private WebApiHelper _ch4 { get; set; }

        private WebApiHelper _s1ch1 { get; set; }
        private WebApiHelper _s1ch2 { get; set; }
        private WebApiHelper _s1ch3 { get; set; }
        private WebApiHelper _s1ch4 { get; set; }

        private WebApiHelper _s2ch1 { get; set; }
        private WebApiHelper _s2ch2 { get; set; }
        private WebApiHelper _s2ch3 { get; set; }
        private WebApiHelper _s2ch4 { get; set; }

        private string MainChainAddress2 = "192.168.197.56:6802";
        private string MainChainAddress3 = "192.168.197.56:6803";
        private string MainChainAddress4 = "192.168.197.56:6804";

        [TestInitialize]
        public void InitTest()
        {
            //Init Logger
            string logName = "ContractTest_" + DateTime.Now.ToString("MMddHHmmss") + ".log";
            string dir = Path.Combine(CommonHelper.AppRoot, "logs", logName);
            _logger.InitLogHelper(dir);

            _ch1 = new WebApiHelper("http://192.168.197.56:8001", CommonHelper.GetCurrentDataDir());
            _ch2 = new WebApiHelper("http://192.168.197.56:8002", CommonHelper.GetCurrentDataDir());
            _ch3 = new WebApiHelper("http://192.168.197.56:8003", CommonHelper.GetCurrentDataDir());
            _ch4 = new WebApiHelper("http://192.168.197.56:8004", CommonHelper.GetCurrentDataDir());

            _s1ch1 = new WebApiHelper("http://192.168.197.56:8011", CommonHelper.GetCurrentDataDir());
            _s1ch2 = new WebApiHelper("http://192.168.197.56:8012", CommonHelper.GetCurrentDataDir());
            _s1ch3 = new WebApiHelper("http://192.168.197.56:8013", CommonHelper.GetCurrentDataDir());
            _s1ch4 = new WebApiHelper("http://192.168.197.56:8014", CommonHelper.GetCurrentDataDir());

            _s2ch1 = new WebApiHelper("http://192.168.197.70:8011", CommonHelper.GetCurrentDataDir());
            _s2ch2 = new WebApiHelper("http://192.168.197.70:8012", CommonHelper.GetCurrentDataDir());
            _s2ch3 = new WebApiHelper("http://192.168.197.70:8013", CommonHelper.GetCurrentDataDir());
            _s2ch4 = new WebApiHelper("http://192.168.197.70:8014", CommonHelper.GetCurrentDataDir());
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


        public void GetPeer(WebApiHelper wa)
        {
            var ci = new CommandInfo(ApiMethods.GetPeers);
            wa.NetGetPeers(ci);
            Assert.IsTrue(ci.Result, "Get peers request failed.");
            var result = ci.InfoMsg as List<PeerDto>;

            foreach (var res in result)
            {
                _logger.Info(res.IpAddress);
            }
        }


        public void AddPeer(string address, WebApiHelper wa)
        {
            var ci = new CommandInfo(ApiMethods.AddPeer);
            ci.Parameter = address;
            wa.NetAddPeer(ci);
            Assert.IsTrue(ci.Result, "Add peer request failed.");
        }

        public void RemovePeer(string address, WebApiHelper wa)
        {
            var ci = new CommandInfo("remove_peer");
            ci.Parameter = address;
            wa.NetRemovePeer(ci);
            Assert.IsTrue(ci.Result, "Remove peer request failed.");
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
            _logger.Info(ci.JsonInfo.ToString());

            TestGetPeer();
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
            _logger.Info(ci.JsonInfo.ToString());

            TestGetPeer();
        }

        [TestMethod]
        public void TestGetPeer2()
        {
            var ci = new CommandInfo("get_peers");
            _ch2.NetGetPeers(ci);
            Assert.IsTrue(ci.Result, "Get peers request failed.");
            ci.GetJsonInfo();
            _logger.Info(ci.JsonInfo.ToString());
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
            _logger.Info(ci.JsonInfo.ToString());

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
            _logger.Info(ci.JsonInfo.ToString());

            TestGetPeer();
        }

        [TestMethod]
        public void TestGetPeer3()
        {
            var ci = new CommandInfo("get_peers");
            _ch3.NetGetPeers(ci);
            Assert.IsTrue(ci.Result, "Get peers request failed.");
            ci.GetJsonInfo();
            _logger.Info(ci.JsonInfo.ToString());
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
            _logger.Info(ci.JsonInfo.ToString());

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
            _logger.Info(ci.JsonInfo.ToString());

            TestGetPeer3();
        }
    }
}