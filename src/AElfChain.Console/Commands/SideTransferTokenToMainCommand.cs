using Microsoft.Extensions.CommandLineUtils;

namespace AElfChain.Console.Commands
{
    public class SideTransferTokenToMainCommand : BaseCommand
    {
        public SideTransferTokenToMainCommand()
        {
            Name = "transfer-side";
            Description = "Transfer side chain token to main chain";
            
            HelpOption("-? | -h | --help");
            OnExecute(RunCommand);
        }
        
        protected override int RunCommand()
        {
            throw new System.NotImplementedException();
        }
    }
}