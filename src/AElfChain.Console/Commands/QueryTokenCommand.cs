using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;

namespace AElfChain.Console.Commands
{
    public class QueryTokenCommand : BaseCommand
    {
        public QueryTokenCommand(INodeManager nodeManager, ContractServices contractServices) 
            : base(nodeManager, contractServices)
        {
        }

        public override void RunCommand()
        {
            var parameters = InputParameters();
            if (parameters == null)
                return;

            var balance = Services.Token.GetUserBalance(parameters[0], parameters[1]);
            Logger.Info($"Account: {parameters[0]}, {parameters[1]}={balance}");            
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
            
            "Parameter: [Owner] [Symbol]".WriteSuccessLine();
            $"eg1: {owner} {symbol}".WriteSuccessLine();
            
            return CommandOption.InputParameters(2);
        }
    }
}