using AElf.Contracts.Configuration;
using AElf.Kernel.SmartContractExecution;
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
    public class ConfigurationCommand : BaseCommand
    {
        public ConfigurationCommand(INodeManager nodeManager, ContractManager contractManager)
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
            var value = Int32Value.Parser.ParseFrom(limitResult.Value).Value;
            $"Block transaction limit: {value}".WriteSuccessLine();

            var observerLimit = AsyncHelper.RunSync(() => configurationStub.GetConfiguration.CallAsync(new StringValue
                {Value = nameof(ConfigurationNameProvider.ExecutionObserverThreshold)}));
            var observerValue = ExecutionObserverThreshold.Parser.ParseFrom(observerLimit.Value);
            $"ExecutionObserverThreshold limit: {observerValue}".WriteSuccessLine();

            var stateLimit = AsyncHelper.RunSync(() => configurationStub.GetConfiguration.CallAsync(new StringValue
                {Value = nameof(ConfigurationNameProvider.StateSizeLimit)}));
            var stateValue = Int32Value.Parser.ParseFrom(stateLimit.Value).Value;
            $"StateSizeLimit limit: {stateValue}".WriteSuccessLine();

            if (parameters.Length == 1)
                return;

            var key = parameters[1];
            var limit = int.Parse(parameters[2]);
            var configuration = Services.Genesis.GetConfigurationContract();
            var genesisOwner = Services.Authority.GetGenesisOwnerAddress();
            var miners = Services.Authority.GetCurrentMiners();
            var input = new SetConfigurationInput();
            switch (key)
            {
                case nameof(ConfigurationNameProvider.BlockTransactionLimit):
                    if (limit == value)
                    {
                        Logger.Info("No need to set limit, same number.");
                        return;
                    }

                    input = new SetConfigurationInput
                    {
                        Key = key,
                        Value = new Int32Value {Value = limit}.ToByteString()
                    };
                    break;
                case nameof(ConfigurationNameProvider.StateSizeLimit):
                    if (limit == stateValue)
                    {
                        Logger.Info("No need to set limit, same number.");
                        return;
                    }

                    input = new SetConfigurationInput
                    {
                        Key = key,
                        Value = new Int32Value {Value = limit}.ToByteString()
                    };
                    break;
                case nameof(ConfigurationNameProvider.ExecutionObserverThreshold):
                    if (parameters.Length != 4)
                        return;
                    var executionBranchThreshold = int.Parse(parameters[2]);
                    var executionCallThreshold = int.Parse(parameters[3]);
                    if (executionCallThreshold == observerValue.ExecutionCallThreshold &&
                        executionBranchThreshold == observerValue.ExecutionBranchThreshold)
                    {
                        Logger.Info("No need to set limit, same number.");
                        return;
                    }

                    input = new SetConfigurationInput
                    {
                        Key = nameof(ConfigurationNameProvider.ExecutionObserverThreshold),
                        Value = new ExecutionObserverThreshold
                        {
                            ExecutionBranchThreshold = executionBranchThreshold,
                            ExecutionCallThreshold = executionCallThreshold
                        }.ToByteString()
                    };
                    break;
            }

            var transactionResult = Services.Authority.ExecuteTransactionWithAuthority(configuration.ContractAddress,
                nameof(ConfigurationMethod.SetConfiguration), input,
                genesisOwner, miners, configuration.CallAddress);
            transactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var queryResult =
                AsyncHelper.RunSync(() => configurationStub.GetConfiguration.CallAsync(new StringValue
                    {Value = nameof(ConfigurationNameProvider.BlockTransactionLimit)}));
            var newValue = Int32Value.Parser.ParseFrom(queryResult.Value).Value;
            $"New block transaction limit: {newValue}".WriteSuccessLine();
            
            var newObserverLimit = AsyncHelper.RunSync(() => configurationStub.GetConfiguration.CallAsync(new StringValue
                {Value = nameof(ConfigurationNameProvider.ExecutionObserverThreshold)}));
            var newObserverValue = ExecutionObserverThreshold.Parser.ParseFrom(newObserverLimit.Value);
            $"New ExecutionObserverThreshold limit: {newObserverValue}".WriteSuccessLine();

            var newStateLimit = AsyncHelper.RunSync(() => configurationStub.GetConfiguration.CallAsync(new StringValue
                {Value = nameof(ConfigurationNameProvider.StateSizeLimit)}));
            var newStateValue = Int32Value.Parser.ParseFrom(newStateLimit.Value).Value;
            $"New StateSizeLimit limit: {newStateValue}".WriteSuccessLine();
        }

        public override CommandInfo GetCommandInfo()
        {
            return new CommandInfo
            {
                Name = "configuration",
                Description = "Get/Set BlockTransactionLimit, StateSizeLimit, ExecutionObserverThreshold "
            };
        }

        public override string[] InputParameters()
        {
            "Parameter: [Method] [TxCount]".WriteSuccessLine();
            "eg1: get".WriteSuccessLine();
            "eg2: set BlockTransactionLimit 100 (BlockTransactionLimit/StateSizeLimit/ExecutionObserverThreshold)"
                .WriteSuccessLine();

            return CommandOption.InputParameters(1);
        }
    }
}