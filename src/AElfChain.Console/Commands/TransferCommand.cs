using AElf.Automation.Common.Helpers;
using AElf.Automation.Common.Managers;

namespace AElfChain.Console.Commands
{
    public class TransferCommand : BaseCommand
    {
        public TransferCommand(INodeManager nodeManager) 
            : base(nodeManager)
        {
        }

        public override void RunCommand()
        {
            var parameters = InputParameters();
            if (parameters == null)
                return;

            var beforeBalance = Services.Token.GetUserBalance(parameters[1], parameters[2]);
            Services.Token.TransferBalance(parameters[0], parameters[1], long.Parse(parameters[3]), parameters[2]);
            var afterBalance = Services.Token.GetUserBalance(parameters[1], parameters[2]);
            
            Logger.Info($"Account: {parameters[1]}, Symbol: {parameters[2]}, Before={beforeBalance}, After={afterBalance}");
        }

        public override string GetCommandInfo()
        {
            return "Transfer token to tester";
        }

        public override string[] InputParameters()
        {
            var from = "ZCP9k7YPHgeMM1XF94BjayULQ6hm3E5QFrsXxuPfUtJFz6sGP";
            var to = "2ERBTcqx8CzgMP7fvQS4DnKQX1AM98CSwAGFyRCQvn9Bvs4Qt1";
            var symbol = "STA";
            var amount = 1000;
            "Parameter: [FROM] [TO] [SYMBOL] [AMOUNT]".WriteSuccessLine();
            $"eg: {from} {to} {symbol} {amount}".WriteSuccessLine();
            
            return CommandOption.InputParameters(4);
        }
    }
}