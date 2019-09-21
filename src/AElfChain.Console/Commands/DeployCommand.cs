using AElf.Automation.Common.Helpers;
using AElf.Automation.Common.Managers;

namespace AElfChain.Console.Commands
{
    public class DeployCommand : BaseCommand
    {
        public DeployCommand(INodeManager nodeManager) : base(nodeManager)
        {
        }

        public override void RunCommand()
        {
            var parameters = InputParameters();
            if (parameters == null)
                return;

            Services.Authority.DeployContractWithAuthority(parameters[0], parameters[1]);
        }

        public override string GetCommandInfo()
        {
            return "Deploy contract";
        }

        public override string[] InputParameters()
        {
            string from = "28Y8JA1i2cN6oHvdv7EraXJr9a1gY6D1PpJXw9QtRMRwKcBQMK";
            string filename = "AElf.Contract.MultiToken";
            
            "Parameter: [FROM] [Contract_FileName]".WriteSuccessLine();
            $"eg: {from} {filename}".WriteSuccessLine();
            
            return CommandOption.InputParameters(2);
        }
    }
}