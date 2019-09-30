using AElf.Automation.Common.Helpers;
using AElf.Automation.Common.Managers;

namespace AElfChain.Console.Commands
{
    public class DeployCommand : BaseCommand
    {
        public DeployCommand(INodeManager nodeManager, ContractServices contractServices) 
            : base(nodeManager, contractServices)
        {
        }

        public override void RunCommand()
        {
            var parameters = InputParameters();
            if (parameters == null)
                return;

            Services.Authority.DeployContractWithAuthority(parameters[0], parameters[1]);
        }

        public override CommandInfo GetCommandInfo()
        {
            return new CommandInfo
            {
                Name = "deploy",
                Description = "Deploy contract with authority permission"
            };
        }

        public override string[] InputParameters()
        {
            string from = "28Y8JA1i2cN6oHvdv7EraXJr9a1gY6D1PpJXw9QtRMRwKcBQMK";
            string filename = "AElf.Contract.MultiToken";
            
            "Parameter: [From] [ContractFileName]".WriteSuccessLine();
            $"eg: {from} {filename}".WriteSuccessLine();
            
            return CommandOption.InputParameters(2);
        }
    }
}