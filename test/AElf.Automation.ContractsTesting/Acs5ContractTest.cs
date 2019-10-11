using AElf.Automation.Common.Contracts;
using AElf.Automation.Common.Helpers;
using AElf.Automation.Common.Managers;
using log4net;

namespace AElf.Automation.ContractsTesting
{
    public class Acs5ContractTest
    {
        public INodeManager NodeManager { get; set; }
        public ILog Logger = Log4NetHelper.GetLogger();
        public ExecutionPluginForAcs5Contract Acs5Contract { get; set; }
        public string Tester = "mPxf7UnKAGqkKRcwHTHv8Y9eTCG4vfbJpAfV1FLgMDS7wJGzt";
        public string ContractAddress = "g2ZYgctCYkJmgUKgUBoU2KvxkTtYhr3sHQyNKsUL3fyUe53pa";

        public Acs5ContractTest(INodeManager nodeManager)
        {
            nodeManager = nodeManager;
        }
    }
}