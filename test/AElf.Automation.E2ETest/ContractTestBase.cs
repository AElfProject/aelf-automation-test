using System.Collections.Generic;
using System.IO;
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
        public ContractManager ContractManager { get; set; }
        public ILog Logger { get; set; }

        public List<Node> ConfigNodes { get; set; }

        public ContractTestBase()
        {
            Log4NetHelper.LogInit("ContractTest");
            Logger = Log4NetHelper.GetLogger();
            
            NodeInfoHelper.SetConfig(MainConfig);
            ConfigNodes = NodeInfoHelper.Config.Nodes;
            var firstBp = ConfigNodes.First();
            
            NodeManager = new NodeManager(firstBp.Endpoint);
            ContractManager = new ContractManager(NodeManager, firstBp.Account);
        }

        public string MainConfig = "nodes-env1-main";
        public string SideConfig = CommonHelper.MapPath("config/nodes-env1-side1.json");
    }
}