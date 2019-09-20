using Microsoft.Extensions.CommandLineUtils;

namespace AElfChain.Console.Commands
{
    public class MainCreateProposalCommand : BaseCommand
    {
        public MainCreateProposalCommand()
        {
            Name = "create-proposal";
            Description = "Create proposal";
            
            HelpOption("-? | -h | --help");
            OnExecute(RunCommand);
        }
        
        protected override int RunCommand()
        {
            throw new System.NotImplementedException();
        }
    }
}