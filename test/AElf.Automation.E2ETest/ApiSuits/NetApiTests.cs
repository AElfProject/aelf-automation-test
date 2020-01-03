using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace AElf.Automation.E2ETest.ApiSuits
{
    [TestClass]
    public class NetApiTests : ApiTestBase
    {
        [TestMethod]
        public async Task GetNetworkInfo_Test()
        {
            var networkInfo = await Client.GetNetworkInfoAsync();
            networkInfo.Version.ShouldBe("1.0.0.0");
            networkInfo.ProtocolVersion.ShouldBe(1);
            networkInfo.Connections.ShouldBeGreaterThanOrEqualTo(1);
        }

        [TestMethod]
        public async Task AddPeer_Test()
        {
            var result = await Client.AddPeerAsync(FakePeer);
            result.ShouldBeFalse();
        }

        [TestMethod]
        public async Task RemovePeer_Test()
        {
            var result = await Client.RemovePeerAsync(FakePeer);
            result.ShouldBeFalse();
        }

        [TestMethod]
        public async Task GetPeers_Test()
        {
            var peers = await Client.GetPeersAsync(false);
            peers.Count.ShouldBeGreaterThanOrEqualTo(1);
            peers.Select(o=>o.ProtocolVersion).ShouldAllBe(o=>o == 1);
            peers.Select(o=>o.RequestMetrics).ShouldAllBe(o=>o == null);

            var peers1 = await Client.GetPeersAsync(true);
            peers1.Select(o=>o.RequestMetrics).ShouldAllBe(o=>o != null);
            peers1.Count.ShouldBe(peers.Count);
        }

        private const string FakePeer = "127.0.0.1:9999";
    }
}