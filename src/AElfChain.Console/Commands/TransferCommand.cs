using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;

namespace AElfChain.Console.Commands
{
    public class TransferCommand : BaseCommand
    {
        public TransferCommand(INodeManager nodeManager, ContractManager contractManager)
            : base(nodeManager, contractManager)
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

            $"Account: {parameters[1]}, Symbol: {parameters[2]}, Before={beforeBalance}, After={afterBalance}"
                .WriteSuccessLine();
        }

        public override CommandInfo GetCommandInfo()
        {
            return new CommandInfo
            {
                Name = "token-transfer",
                Description = "Transfer token to tester"
            };
        }

        public override string[] InputParameters()
        {
            var from = "ZCP9k7YPHgeMM1XF94BjayULQ6hm3E5QFrsXxuPfUtJFz6sGP";
            var to = "2ERBTcqx8CzgMP7fvQS4DnKQX1AM98CSwAGFyRCQvn9Bvs4Qt1";
            var symbol = "STA";
            var amount = 1000;
            "Parameter: [From] [To] [Symbol] [Amount]".WriteSuccessLine();
            $"eg: {from} {to} {symbol} {amount}".WriteSuccessLine();

            return CommandOption.InputParameters(4);
        }
    }
}