using AElf.Automation.Common.Helpers;
using AElf.Automation.Common.Managers;
using log4net;

namespace AElfChain.Console.Commands
{
    public abstract class BaseCommand
    {
        public INodeManager NodeManager { get; set; }
        
        public ContractServices Services { get; set; }

        public ILog Logger = Log4NetHelper.GetLogger();
        
        public BaseCommand(INodeManager nodeManager, ContractServices contractServices)
        {
            NodeManager = nodeManager;
            Services = contractServices;
        }
        
        public abstract void RunCommand();

        public abstract string GetCommandInfo();

        public abstract string[] InputParameters();
    }
}