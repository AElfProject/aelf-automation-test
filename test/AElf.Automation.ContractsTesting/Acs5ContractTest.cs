using AElfChain.Common.Contracts;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using log4net;

namespace AElf.Automation.ContractsTesting
{
    public class Acs5ContractTest
    {
        public string ContractAddress = "g2ZYgctCYkJmgUKgUBoU2KvxkTtYhr3sHQyNKsUL3fyUe53pa";
        public ILog Logger = Log4NetHelper.GetLogger();
        public string Tester = "mPxf7UnKAGqkKRcwHTHv8Y9eTCG4vfbJpAfV1FLgMDS7wJGzt";

        public Acs5ContractTest(INodeManager nodeManager)
        {
            NodeManager = nodeManager;
        }

        public INodeManager NodeManager { get; set; }
        public ExecutionPluginForAcs5Contract Acs5Contract { get; set; }
    }
}