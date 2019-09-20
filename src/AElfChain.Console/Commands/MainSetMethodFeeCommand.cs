using Microsoft.Extensions.CommandLineUtils;

namespace AElfChain.Console.Commands
{
    public class MainSetMethodFeeCommand : BaseCommand
    {
        public MainSetMethodFeeCommand()
        {
            Name = "set-fee";
            Description = "Set token transaction method fee";
            
            HelpOption("-? | -h | --help");
            OnExecute(RunCommand);
        }
        protected override int RunCommand()
        {
            throw new System.NotImplementedException();
        }
    }
}