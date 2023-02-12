using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using log4net;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Shouldly;

namespace AElf.Automation.Contracts.ScenarioTest
{
    [TestClass]
    public class NetworkTest
    {
        private static readonly ILog Logger = Log4NetHelper.GetLogger();

        private readonly string MainChainAddress1 = "http://192.168.67.47:8000";
        private readonly string MainChainAddress2 = "http://127.0.0.1:8001";
        private readonly HttpClient Client = new HttpClient();
        private readonly string username = "";
        private readonly string password = "";
        private INodeManager _ch1 { get; set; }
        private INodeManager _ch2 { get; set; }

        [TestInitialize]
        public void InitTest()
        {
            //Init Logger
            var logName = "ContractTest_" + DateTime.Now.ToString("MMddHHmmss") + ".log";
            var dir = Path.Combine(CommonHelper.AppRoot, "logs", logName);
            Log4NetHelper.LogInit("Network_");

            _ch1 = new NodeManager("http://127.0.0.1:8000");
            _ch2 = new NodeManager("http://127.0.0.1:8001");
        }

        [TestMethod]
        public void TestGetPeer()
        {
            GetPeer(_ch2);
        }

        [TestMethod]
        public void TestRemovePeer()
        {
            RemovePeer(MainChainAddress2, _ch1);

            GetPeer(_ch1);
            GetPeer(_ch2);
        }

        [TestMethod]
        public void TestAddPeer()
        {
            AddPeer(MainChainAddress2, _ch1);

            GetPeer(_ch1);
            GetPeer(_ch2);
        }


        public void GetPeer(INodeManager wa)
        {
            var peers = wa.NetGetPeers();

            foreach (var res in peers) Logger.Info(res.IpAddress);
        }

        [TestMethod]
        public async Task AddPeer()
        {
            var parameters = new Dictionary<string, string>
            {
                { "address", "127.0.0.1:6801" }
            };
            var postString = await PostResponseAsStringAsync($"{MainChainAddress2}/api/net/peer", parameters,
                basicAuth: new BasicAuth
                {
                    UserName = "full",
                    Password = "12345678"
                });
        }


        public void AddPeer(string address, INodeManager wa)
        {
            var result = wa.NetAddPeer(address, username, password);
            Assert.IsTrue(result, "Add peer request failed.");
        }

        public void RemovePeer(string address, INodeManager wa)
        {
            var result = wa.NetRemovePeer(address, username, password);
            Assert.IsTrue(result, "Remove peer request failed.");
        }

        [TestMethod]
        [DataRow("192.168.197.13:6800")]
        [DataRow("192.168.197.29:6800")]
        public void TestAddPeer1(string address)
        {
            var result = _ch1.NetAddPeer(address, username, password);
            Assert.IsTrue(result, "Add peer request failed.");
            TestGetPeer();
        }

        [TestMethod]
        [DataRow("192.168.197.13:6800")]
        [DataRow("192.168.197.29:6800")]
        public void TestRemovePeer1(string address)
        {
            var result = _ch1.NetRemovePeer(address, username, password);
            Assert.IsTrue(result, "Remove peer request failed.");

            TestGetPeer();
        }

        [TestMethod]
        public void TestGetPeer2()
        {
            var peers = _ch2.NetGetPeers();
            Logger.Info(peers.ToString());
        }

        [TestMethod]
        [DataRow("192.168.197.34:6800")]
        [DataRow("192.168.197.29:6800")]
        public void TestAddPeer2(string address)
        {
            var result = _ch2.NetAddPeer(address, username, password);
            Assert.IsTrue(result, "Add peer request failed.");

            TestGetPeer2();
        }

        [TestMethod]
        [DataRow("192.168.197.34:6800")]
        [DataRow("192.168.197.29:6800")]
        public void TestRemovePeer2(string address)
        {
            var result = _ch2.NetRemovePeer(address, username, password);
            Assert.IsTrue(result, "Remove peer request failed.");

            TestGetPeer();
        }


        protected async Task<string> PostResponseAsStringAsync(string url, Dictionary<string, string> paramters,
            string version = null, HttpStatusCode expectedStatusCode = HttpStatusCode.OK, BasicAuth basicAuth = null)
        {
            var response = await PostResponseAsync(url, paramters, version, expectedStatusCode, basicAuth);
            return await response.Content.ReadAsStringAsync();
        }

        private async Task<HttpResponseMessage> PostResponseAsync(string url, Dictionary<string, string> paramters,
            string version = null, HttpStatusCode expectedStatusCode = HttpStatusCode.OK, BasicAuth basicAuth = null,
            string reason = null)
        {
            version = !string.IsNullOrWhiteSpace(version) ? $";v={version}" : string.Empty;
            if (basicAuth != null)
            {
                var byteArray = Encoding.ASCII.GetBytes($"{basicAuth.UserName}:{basicAuth.Password}");
                Client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));
            }

            Client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            var paramsStr = JsonConvert.SerializeObject(paramters);
            var content = new StringContent(paramsStr, Encoding.UTF8, "application/json");
            content.Headers.ContentType = MediaTypeHeaderValue.Parse($"application/json{version}");

            var response = await Client.PostAsync(url, content);
            response.StatusCode.ShouldBe(expectedStatusCode);
            if (reason != null) response.ReasonPhrase.ShouldBe(reason);
            return response;
        }

        protected async Task<string> DeleteResponseAsStringAsync(string url, string version = null,
            HttpStatusCode expectedStatusCode = HttpStatusCode.OK, BasicAuth basicAuth = null)
        {
            var response = await DeleteResponseAsync(url, version, expectedStatusCode, basicAuth);
            return await response.Content.ReadAsStringAsync();
        }

        private async Task<HttpResponseMessage> DeleteResponseAsync(string url, string version = null,
            HttpStatusCode expectedStatusCode = HttpStatusCode.OK, BasicAuth basicAuth = null, string reason = null)
        {
            version = !string.IsNullOrWhiteSpace(version) ? $";v={version}" : string.Empty;
            Client.DefaultRequestHeaders.Accept.Clear();
            Client.DefaultRequestHeaders.Accept.Add(
                MediaTypeWithQualityHeaderValue.Parse($"application/json{version}"));
            if (basicAuth != null)
            {
                var byteArray = Encoding.ASCII.GetBytes($"{basicAuth.UserName}:{basicAuth.Password}");
                Client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));
            }

            var response = await Client.DeleteAsync(url);
            response.StatusCode.ShouldBe(expectedStatusCode);
            if (reason != null) response.ReasonPhrase.ShouldBe(reason);
            return response;
        }

        public class BasicAuth
        {
            public static readonly string DefaultUserName = "user";

            public static string DefaultPassword = "password";

            public static readonly BasicAuth Default = new BasicAuth
            {
                UserName = DefaultUserName,
                Password = DefaultPassword
            };

            public string UserName { get; set; }

            public string Password { get; set; }
        }
    }
}