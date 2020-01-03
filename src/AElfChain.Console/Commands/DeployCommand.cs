using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;

namespace AElfChain.Console.Commands
{
    public class DeployCommand : BaseCommand
    {
        public DeployCommand(INodeManager nodeManager, ContractManager contractManager)
            : base(nodeManager, contractManager)
        {
        }

        public override void RunCommand()
        {
            var parameters = InputParameters();
            if (parameters == null)
                return;

            var address = Services.Authority.DeployContractWithAuthority(parameters[0], parameters[1]);
            $"Deployed contract address: {address}".WriteSuccessLine();
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
            var from = "28Y8JA1i2cN6oHvdv7EraXJr9a1gY6D1PpJXw9QtRMRwKcBQMK";
            var filename = "AElf.Contracts.MultiToken";

            "Parameter: [From] [ContractFileName]".WriteSuccessLine();
            $"eg: {from} {filename}".WriteSuccessLine();

            return CommandOption.InputParameters(2);
        }
    }
}