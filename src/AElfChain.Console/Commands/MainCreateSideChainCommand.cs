using Microsoft.Extensions.CommandLineUtils;

namespace AElfChain.Console.Commands
{
    public class MainCreateSideChainCommand : BaseCommand
    {
        public MainCreateSideChainCommand()
        {
            Name = "create-side";
            Description = "Create side chain";
            
            HelpOption("-? | -h | --help");
            OnExecute(RunCommand);
        }
        protected override int RunCommand()
        {
            throw new System.NotImplementedException();
        }
    }
}