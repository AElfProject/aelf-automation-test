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
            NodeManager = new NodeManager(endpoint);
        }

        public ILog Logger { get; set; }

        public INodeManager NodeManager { get; set; }
        public AElfClient Client => NodeManager.ApiClient;
    }
}