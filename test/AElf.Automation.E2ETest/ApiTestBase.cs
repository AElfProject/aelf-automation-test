using AElf.Client.Service;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using log4net;

namespace AElf.Automation.E2ETest
{
    public class ApiTestBase
    {
        public ILog Logger { get; set; }
        
        public INodeManager NodeManager { get; set; }
        public AElfClient Client => NodeManager.ApiClient;

        public ApiTestBase()
        {
            Log4NetHelper.LogInit("ApiTest");
            Logger = Log4NetHelper.GetLogger();
            
            NodeManager = new NodeManager(Endpoint);
        }

        private const string Endpoint = "192.168.197.40:8000";
    }
}