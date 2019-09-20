using Microsoft.Extensions.CommandLineUtils;

namespace AElfChain.Console.Commands
{
    public class MainReleaseProposalCommand : BaseCommand
    {
        public MainReleaseProposalCommand()
        {
            Name = "release";
            Description = "Release proposal";
            
            HelpOption("-? | -h | --help");
            OnExecute(RunCommand);
        }
        protected override int RunCommand()
        {
            throw new System.NotImplementedException();
        }
    }
}