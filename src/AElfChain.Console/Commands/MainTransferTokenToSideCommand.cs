using Microsoft.Extensions.CommandLineUtils;

namespace AElfChain.Console.Commands
{
    public class MainTransferTokenToSideCommand : BaseCommand
    {
        public MainTransferTokenToSideCommand()
        {
            Name = "transfer-main";
            Description = "Transfer main chain token to side chain";
            
            HelpOption("-? | -h | --help");
            OnExecute(RunCommand);
        }
        protected override int RunCommand()
        {
            throw new System.NotImplementedException();
        }
    }
}