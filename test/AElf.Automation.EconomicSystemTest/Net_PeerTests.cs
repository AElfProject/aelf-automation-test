using System;
using System.IO;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace AElf.Automation.EconomicSystemTest
{
    [TestClass]
    public class Net_PeerTests
    {
        public static string Bp1Url = "http://192.168.197.13:8000";

        protected readonly ILogHelper _logger = LogHelper.GetLogger();
        public string Bp2Url = "http://192.168.197.28:8000";
        public string Bp3Url = "http://192.168.197.33:8000";

        public string Full1Url = "http://192.168.199.205:8100";
        public string Full2Url = "http://192.168.199.205:8200";
        public string Full3Url = "http://192.168.199.205:8300";
        public string Full4Url = "http://192.168.199.205:8400";
        protected INodeManager CH { get; set; }

        [TestInitialize]
        public void InitializeTest()
        {
            //Init Logger
            var logName = "NetPeersTest_" + DateTime.Now.ToString("MMddHHmmss") + ".log";
            var dir = Path.Combine(CommonHelper.AppRoot, "logs", logName);
            _logger.InitLogHelper(dir);
        }

        [TestMethod]
        [DataRow("http://192.168.197.13:8000")]
        public void GetPeers(string url)
        {
            var service = AElfClientExtension.GetClient(url);
            var list = service.GetPeersAsync(true).Result;
            _logger.Info($"Peer {url} information");
            foreach (var peer in list) _logger.Info(JsonConvert.SerializeObject(peer));
        }

        [TestMethod]
        [DataRow("http://192.168.197.13:8000", "192.168.197.205:6810")]
        public void AddPeers(string url, params string[] addressArray)
        {
            var service = AElfClientExtension.GetClient(url);
            if (addressArray == null) return;
            foreach (var address in addressArray)
            {
                var result = service.AddPeerAsync(address).Result;
                _logger.Info($"Add peer {address} result: {result}");
            }
        }

        [TestMethod]
        [DataRow("http://192.168.197.13:8000", "192.168.197.28:6800")]
        public void RemovePeers(string url, params string[] addressArray)
        {
            var service = AElfClientExtension.GetClient(url);
            if (addressArray == null) return;
            foreach (var address in addressArray)
            {
                var result = service.RemovePeerAsync(address).Result;
                _logger.Info($"Remove peer {address} result: {result}");
            }

            GetPeers(url);
        }
    }
}