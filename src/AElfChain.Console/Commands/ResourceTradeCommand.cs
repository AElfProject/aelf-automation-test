using AElf.Contracts.TokenConverter;
using AElfChain.Common;
using AElfChain.Common.Contracts;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using Volo.Abp.Threading;

namespace AElfChain.Console.Commands
{
    public class ResourceTradeCommand : BaseCommand
    {
        public ResourceTradeCommand(INodeManager nodeManager, ContractManager contractManager)
            : base(nodeManager, contractManager)
        {
        }

        public override void RunCommand()
        {
            var parameters = InputParameters();
            if (parameters == null)
                return;

            var beforeNativeToken = Services.Token.GetUserBalance(parameters[0]);
            var beforeResourceToken = Services.Token.GetUserBalance(parameters[0], parameters[2]);
            $"Account: {parameters[0]}, {NodeOption.NativeTokenSymbol}={beforeNativeToken}, {parameters[2]}={beforeResourceToken}"
                .WriteSuccessLine();

            var tokenConverter = Services.Genesis.GetTokenConverterStub(parameters[0]);
            long transactionFee = 0;
            var tradeAmount = long.Parse(parameters[3]);
            if (parameters[1].Equals("buy"))
            {
                var transactionResult = AsyncHelper.RunSync(() => tokenConverter.Buy.SendAsync(new BuyInput
                {
                    Symbol = parameters[2],
                    Amount = tradeAmount,
                    PayLimit = 0
                }));
                transactionFee = transactionResult.TransactionResult.GetDefaultTransactionFee();
            }

            if (parameters[1].Equals("sell"))
            {
                var transactionResult = AsyncHelper.RunSync(() => tokenConverter.Sell.SendAsync(new SellInput
                {
                    Symbol = parameters[2],
                    Amount = tradeAmount,
                    ReceiveLimit = 0
                }));
                transactionFee = transactionResult.TransactionResult.GetDefaultTransactionFee();
            }

            var afterNativeToken = Services.Token.GetUserBalance(parameters[0]);
            var afterResourceToken = Services.Token.GetUserBalance(parameters[0], parameters[2]);

            $"Account: {parameters[0]}, {NodeOption.NativeTokenSymbol}={afterNativeToken}, {parameters[2]}={afterResourceToken}"
                .WriteSuccessLine();
            $"Price({NodeOption.NativeTokenSymbol}/{parameters[2]}): {(float) (beforeNativeToken - afterNativeToken - transactionFee) / (float) (afterResourceToken - beforeResourceToken)}"
                .WriteSuccessLine();
        }

        public override CommandInfo GetCommandInfo()
        {
            return new CommandInfo
            {
                Name = "resource",
                Description = "Resource buy and sell"
            };
        }

        public override string[] InputParameters()
        {
            var from = "ZCP9k7YPHgeMM1XF94BjayULQ6hm3E5QFrsXxuPfUtJFz6sGP";
            var operation1 = "buy";
            var operation2 = "sell";
            var symbol = "STA";
            var amount = 1000;

            "Parameter: [From] [Operation] [Symbol] [Amount]".WriteSuccessLine();
            $"eg1: {from} {operation1} {symbol} {amount}".WriteSuccessLine();
            $"eg2: {from} {operation2} {symbol} {amount}".WriteSuccessLine();

            return CommandOption.InputParameters(4);
        }
    }
}