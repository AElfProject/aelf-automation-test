using AElfChain.Common.Contracts;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Volo.Abp.Threading;

namespace AElfChain.Console.Commands
{
    public class TransactionLimitCommand : BaseCommand
    {
        public TransactionLimitCommand(INodeManager nodeManager, ContractServices contractServices)
            : base(nodeManager, contractServices)
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
                AsyncHelper.RunSync(() => configurationStub.GetBlockTransactionLimit.CallAsync(new Empty()));
            $"Block transaction limit: {limitResult.Value}".WriteSuccessLine();

            if (parameters.Length == 1)
                return;

            var limit = int.Parse(parameters[1]);
            if (limit == limitResult.Value)
            {
                Logger.Info("No need to set limit, same number.");
                return;
            }

            var configuration = Services.Genesis.GetConfigurationContract();
            var genesisOwner = Services.Authority.GetGenesisOwnerAddress();
            var miners = Services.Authority.GetCurrentMiners();
            var input = new Int32Value {Value = limit};
            var transactionResult = Services.Authority.ExecuteTransactionWithAuthority(configuration.ContractAddress,
                "SetBlockTransactionLimit", input,
                genesisOwner, miners, configuration.CallAddress);
            transactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var queryResult =
                AsyncHelper.RunSync(() => configurationStub.GetBlockTransactionLimit.CallAsync(new Empty()));
            $"New block transaction limit: {queryResult.Value}".WriteSuccessLine();
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