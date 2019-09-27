using System.Linq;
using System.Threading.Tasks;
using AElf.Automation.Common;
using AElf.Automation.Common.Contracts;
using AElf.Automation.Common.Helpers;
using AElf.Automation.Common.Managers;
using AElf.Contracts.TokenConverter;
using AElf.Types;
using Newtonsoft.Json;
using Shouldly;
using Volo.Abp.Threading;

namespace AElfChain.Console.Commands
{
    public class SetConnectorCommand : BaseCommand
    {
        public SetConnectorCommand(INodeManager nodeManager, ContractServices contractServices) 
            : base(nodeManager, contractServices)
        {
        }

        public override void RunCommand()
        {
            var parameters = InputParameters();
            if (parameters == null)
                return;
            
            var authority = Services.Authority;
            var orgAddress = authority.GetGenesisOwnerAddress();
            var miners = authority.GetCurrentMiners();
            var connector = new Connector
            {
                Symbol = parameters[0],
                IsPurchaseEnabled = bool.Parse(parameters[1]),
                IsVirtualBalanceEnabled = bool.Parse(parameters[2]),
                Weight = parameters[3],
                VirtualBalance = long.Parse(parameters[4])
            };
            var bp = NodeInfoHelper.Config.Nodes.First().Account;
            var transactionResult = authority.ExecuteTransactionWithAuthority(Services.TokenConverter.ContractAddress,
                "SetConnector", connector, orgAddress, miners, bp);
            transactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            AsyncHelper.RunSync(()=>GetTokenConnector(parameters[0]));
        }

        public override string GetCommandInfo()
        {
            return "Set token connector";
        }

        public override string[] InputParameters()
        {
            var symbol = "STA";
            var isPurchaseEnabled = "true";
            var isVirtualBalanceEnabled = "true";
            var weight = "0.5";
            var virtualBalance = "10000000000";
            
            "Parameter: [Symbol] [IsPurchaseEnabled] [IsVirtualBalanceEnabled] [Weight] [VirtualBalance]".WriteSuccessLine();
            $"eg: {symbol} {isPurchaseEnabled} {isVirtualBalanceEnabled} {weight} {virtualBalance}".WriteSuccessLine();
            
            return CommandOption.InputParameters(5);
        }
        
        private async Task GetTokenConnector(string symbol)
        {
            var tokenConverter = Services.Genesis.GetTokenConverterStub();

            var result = await tokenConverter.GetConnector.CallAsync(new TokenSymbol
            {
                Symbol = symbol
            });

            Logger.Info($"Connector: {JsonConvert.SerializeObject(result)}");
        }
    }
}