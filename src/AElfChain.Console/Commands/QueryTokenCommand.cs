using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;

namespace AElfChain.Console.Commands
{
    public class QueryTokenCommand : BaseCommand
    {
        public QueryTokenCommand(INodeManager nodeManager, ContractManager contractManager)
            : base(nodeManager, contractManager)
        {
        }

        public override void RunCommand()
        {
            var parameters = InputParameters();
            if (parameters == null)
                return;
            $"Account: {parameters[0]}".WriteSuccessLine();
            for (var i = 1; i <= parameters.Length - 1; i++)
            {
                var balance = Services.Token.GetUserBalance(parameters[0], parameters[i]);
                $"Symbol: {parameters[i]}, Balance: {balance}".WriteSuccessLine();
            }
        }

        public override CommandInfo GetCommandInfo()
        {
            return new CommandInfo
            {
                Name = "token-balance",
                Description = "Query token balance info"
            };
        }

        public override string[] InputParameters()
        {
            var owner = "mS8xMLs9SuWdNECkrfQPF8SuRXRuQzitpjzghi3en39C3SRvf";
            var symbol = "TELF";

            "Parameter: [Owner] [Symbol] [Symbol]...".WriteSuccessLine();
            $"eg1: {owner} {symbol}".WriteSuccessLine();

            return CommandOption.InputParameters(2);
        }
    }
}