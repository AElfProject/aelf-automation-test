using AElf.Automation.Common.Helpers;
using AElf.Automation.Common.Managers;

namespace AElfChain.Console.Commands
{
    public class SwitchOtherChainCommand : BaseCommand
    {
        public SwitchOtherChainCommand(INodeManager nodeManager) : base(nodeManager)
        {
        }

        public override void RunCommand()
        {
            var parameters = InputParameters();
            if (parameters == null)
                return;
            
            NodeManager = new NodeManager(parameters[0]);
            Services = new ContractServices(NodeManager);
        }

        public override string GetCommandInfo()
        {
            return "Switch other chain";
        }

        public override string[] InputParameters()
        {
            var endpoint = "192.168.197.14:8000";
            "Parameter: [Endpoint]".WriteSuccessLine();
            $"eg1: {endpoint}".WriteSuccessLine();
            
            return CommandOption.InputParameters(1);
        }
    }
}