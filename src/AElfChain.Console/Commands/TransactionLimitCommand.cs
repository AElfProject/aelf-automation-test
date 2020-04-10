using AElf.Contracts.Configuration;
using AElf.Types;
using AElfChain.Common.Contracts;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Volo.Abp.Threading;

namespace AElfChain.Console.Commands
{
    public class TransactionLimitCommand : BaseCommand
    {
        public TransactionLimitCommand(INodeManager nodeManager, ContractManager contractManager)
            : base(nodeManager, contractManager)
        {
            Logger = Log4NetHelper.GetLogger();
        }

        public override void RunCommand()
        {
            var parameters = InputParameters();
            if (parameters == null)
                return;

            var configurationStub = Services.Genesis.GetConfigurationStub();

            var limitResult =
                AsyncHelper.RunSync(() => configurationStub.GetConfiguration.CallAsync(new StringValue
                    {Value = nameof(ConfigurationNameProvider.BlockTransactionLimit)}));
            var value = SInt32Value.Parser.ParseFrom(limitResult.Value).Value;
            $"Block transaction limit: {value}".WriteSuccessLine();

            if (parameters.Length == 1)
                return;

            var limit = int.Parse(parameters[1]);
            if (limit == value)
            {
                Logger.Info("No need to set limit, same number.");
                return;
            }

            var configuration = Services.Genesis.GetConfigurationContract();
            var genesisOwner = Services.Authority.GetGenesisOwnerAddress();
            var miners = Services.Authority.GetCurrentMiners();
            var input = new SetConfigurationInput
            {
                Key = nameof(ConfigurationNameProvider.BlockTransactionLimit),
                Value = new SInt32Value {Value = value}.ToByteString()
            };
            var transactionResult = Services.Authority.ExecuteTransactionWithAuthority(configuration.ContractAddress,
                nameof(ConfigurationMethod.SetConfiguration), input,
                genesisOwner, miners, configuration.CallAddress);
            transactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var queryResult =
                AsyncHelper.RunSync(() => configurationStub.GetConfiguration.CallAsync(new StringValue
                    {Value = nameof(ConfigurationNameProvider.BlockTransactionLimit)}));
            var newValue = SInt32Value.Parser.ParseFrom(queryResult.Value).Value;
            $"New block transaction limit: {newValue}".WriteSuccessLine();
        }

        public override CommandInfo GetCommandInfo()
        {
            return new CommandInfo
            {
                Name = "tx-limit",
                Description = "Get/Set transaction execution limit"
            };
        }

        public override string[] InputParameters()
        {
            "Parameter: [Method] [TxCount]".WriteSuccessLine();
            "eg1: get".WriteSuccessLine();
            "eg2: set 100".WriteSuccessLine();

            return CommandOption.InputParameters(1);
        }
    }
}