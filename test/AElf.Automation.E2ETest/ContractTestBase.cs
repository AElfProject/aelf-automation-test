using System.Collections.Generic;
using System.Linq;
using AElfChain.Common;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using log4net;

namespace AElf.Automation.E2ETest
{
    public class ContractTestBase
    {
        public INodeManager NodeManager { get; set; }
        public ChainManager ChainManager { get; set; }
        public ILog Logger { get; set; }

        public List<Node> ConfigNodes { get; set; }

        public ContractTestBase()
        {
            Log4NetHelper.LogInit("ContractTest");
            Logger = Log4NetHelper.GetLogger();
            
            NodeInfoHelper.SetConfig(ConfigFile);
            ConfigNodes = NodeInfoHelper.Config.Nodes;
            var firstBp = ConfigNodes.First();
            
            NodeManager = new NodeManager(firstBp.Endpoint);
            ChainManager = new ChainManager(NodeManager, firstBp.Account);
        }

        private const string ConfigFile = "nodes-env2-main";
    }
}