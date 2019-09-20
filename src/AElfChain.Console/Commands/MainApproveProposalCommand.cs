using Microsoft.Extensions.CommandLineUtils;

namespace AElfChain.Console.Commands
{
    public class MainApproveProposalCommand : BaseCommand
    {
        public MainApproveProposalCommand()
        {
            Name = "approve";
            Description = "Approve proposal";
            
            HelpOption("-? | -h | --help");
            OnExecute(RunCommand);
        }
        
        protected override int RunCommand()
        {
            throw new System.NotImplementedException();
        }
    }
}