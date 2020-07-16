using System.Linq;
using AElf.Client.Service;
using AElfChain.Common;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using log4net;

namespace AElf.Automation.E2ETest
{
    public class ApiTestBase
    {
        public ApiTestBase()
        {
            Log4NetHelper.LogInit("ApiTest");
            Logger = Log4NetHelper.GetLogger();

            NodeInfoHelper.SetConfig(ContractTestBase.MainConfig);
            var endpoint = NodeInfoHelper.Config.Nodes.First().Endpoint;
            var caller = NodeInfoHelper.Config.Nodes.First().Account;
            NodeManager = new NodeManager(endpoint);
            SideNodeManager = new NodeManager(SideChainEndPoint);
            CrossChainManager = new CrossChainManager(NodeManager,SideNodeManager,caller);
        }

        public ILog Logger { get; set; } 
        public INodeManager NodeManager { get; set; }
        public INodeManager SideNodeManager { get; set; }
        public CrossChainManager CrossChainManager { get; set; }
        public AElfClient Client => NodeManager.ApiClient;
        public string SideChainEndPoint = "192.168.197.21:8001";
    }
}