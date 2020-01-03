using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using log4net;

namespace AElfChain.Console.Commands
{
    public class CommandInfo
    {
        public string Name { get; set; }
        public string Description { get; set; }
    }

    public abstract class BaseCommand
    {
        public ILog Logger = Log4NetHelper.GetLogger();

        public BaseCommand(INodeManager nodeManager, ContractManager contractManager)
        {
            NodeManager = nodeManager;
            Services = contractManager;
        }

        public INodeManager NodeManager { get; set; }

        public ContractManager Services { get; set; }

        public abstract void RunCommand();

        public abstract CommandInfo GetCommandInfo();

        public abstract string[] InputParameters();
    }
}