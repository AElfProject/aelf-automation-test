using AElfChain.Console.Commands;
using Microsoft.Extensions.CommandLineUtils;

namespace AElfChain.Console
{
    public class App : CommandLineApplication
    {
        public App()
        {
            //main
            //Commands.Add(new MainSetConnectorCommand());
            //Commands.Add(new MainResourceTradeCommand());
            //Commands.Add(new MainCreateSideChainCommand());
            //Commands.Add(new MainCreateProposalCommand());
            //Commands.Add(new MainApproveProposalCommand());
            //Commands.Add(new MainReleaseProposalCommand());
            //Commands.Add(new MainSetMethodFeeCommand());
            //Commands.Add(new MainTransferTokenToSideCommand());
            
            //side
            //Commands.Add(new SideTransferTokenToMainCommand());
            
            //common
            Commands.Add(new CommonTransferToken());

            HelpOption("-h | -H | -? | --help");
        }
    }
}