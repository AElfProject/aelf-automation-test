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
        public ContractManager ContractManager { get; set; }
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
            ContractManager = new ContractManager(NodeManager, firstBp.Account);
            TransferToNodes();
        }

        private const string ConfigFile = "nodes-env1-main";

        public void TransferToNodes()
        {
            foreach (var node in ConfigNodes)
            {
                if (node.Account.Equals(ContractManager.CallAddress)) continue;
                ContractManager.Token.TransferBalance(ContractManager.CallAddress, node.Account, 100000000000,
                    ContractManager.Token.GetPrimaryTokenSymbol());
            }
        }
    }
}