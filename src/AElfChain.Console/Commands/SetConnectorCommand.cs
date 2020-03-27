using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.TokenConverter;
using AElf.Types;
using AElfChain.Common;
using AElfChain.Common.Contracts;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using Newtonsoft.Json;
using Shouldly;
using Volo.Abp.Threading;

namespace AElfChain.Console.Commands
{
    public class SetConnectorCommand : BaseCommand
    {
        public SetConnectorCommand(INodeManager nodeManager, ContractManager contractManager)
            : base(nodeManager, contractManager)
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
            var connector = new PairConnectorParam
            {
                ResourceConnectorSymbol = parameters[0],
                ResourceWeight = parameters[1],
                NativeWeight = parameters[2],
                NativeVirtualBalance = long.Parse(parameters[3])
            };
            var bp = NodeInfoHelper.Config.Nodes.First().Account;
            var transactionResult = authority.ExecuteTransactionWithAuthority(Services.TokenConverter.ContractAddress,
                nameof(TokenConverterContractContainer.TokenConverterContractStub.AddPairConnector), connector,
                orgAddress, miners, bp);
            transactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            AsyncHelper.RunSync(() => GetTokenConnector(parameters[0]));
        }

        public override CommandInfo GetCommandInfo()
        {
            return new CommandInfo
            {
                Name = "connector",
                Description = "Set token connector"
            };
        }

        public override string[] InputParameters()
        {
            var symbol = "STA";
            var resourceWeight = "0.05";
            var nativeWeight = "0.5";
            var virtualBalance = "10000000000";

            "Parameter: [Symbol] [ResourceWeight] [NativeWeight] [NativeVirtualBalance]"
                .WriteSuccessLine();
            $"eg: {symbol} {resourceWeight} {nativeWeight} {virtualBalance}".WriteSuccessLine();

            return CommandOption.InputParameters(5);
        }

        private async Task GetTokenConnector(string symbol)
        {
            var tokenConverter = Services.Genesis.GetTokenConverterStub();
            var result = await tokenConverter.GetPairConnector.CallAsync(new TokenSymbol
            {
                Symbol = symbol
            });

            Logger.Info($"Connector: {JsonConvert.SerializeObject(result)}");
        }
    }
}