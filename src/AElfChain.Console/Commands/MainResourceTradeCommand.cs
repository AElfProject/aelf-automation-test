using Microsoft.Extensions.CommandLineUtils;

namespace AElfChain.Console.Commands
{
    public class MainResourceTradeCommand : BaseCommand
    {
        public MainResourceTradeCommand()
        {
            Name = "resource";
            Description = "Resource token buy and sell";
            
            HelpOption("-? | -h | --help");
            OnExecute(RunCommand);
        }
        
        protected override int RunCommand()
        {
            throw new System.NotImplementedException();
        }
    }
}