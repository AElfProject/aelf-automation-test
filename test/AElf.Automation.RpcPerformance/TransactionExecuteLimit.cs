using System.Threading.Tasks;
using AElf.Contracts.Configuration;
using AElf.Types;
using AElfChain.Common.Contracts;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using log4net;
using Shouldly;
using Volo.Abp.Threading;

namespace AElf.Automation.RpcPerformance
{
    public class TransactionExecuteLimit
    {
        private static readonly ILog Logger = Log4NetHelper.GetLogger();
        private readonly string _account;
        private readonly INodeManager _nodeManager;
        private readonly NodeTransactionOption _nodeTransactionOption;

        private Address _configurationContractAddress;

        public TransactionExecuteLimit(INodeManager nodeManager, string account)
        {
            _account = account;

            _nodeManager = nodeManager;
            _nodeTransactionOption = RpcConfig.ReadInformation.NodeTransactionOption;
        }

        public bool WhetherEnableTransactionLimit()
        {
            return _nodeTransactionOption.EnableLimit;
        }

        public void SetExecutionSelectTransactionLimit()
        {
            var configurationStub = GetConfigurationContractStub();
            var limitCount = _nodeTransactionOption.MaxTransactionSelect;
            AsyncHelper.RunSync(() => SetSelectTransactionLimit(configurationStub, limitCount));
        }

        private ConfigurationContainer.ConfigurationStub GetConfigurationContractStub()
        {
            var gensis = GenesisContract.GetGenesisContract(_nodeManager, _account);

            _configurationContractAddress = gensis.GetContractAddressByName(NameProvider.Configuration);

            return gensis.GetConfigurationStub();
        }

        private async Task SetSelectTransactionLimit(ConfigurationContainer.ConfigurationStub configurationStub,
            int limitCount)
        {
            var beforeResult = await configurationStub.GetConfiguration.CallAsync(new StringValue
                {Value = nameof(ConfigurationNameProvider.BlockTransactionLimit)});
            var beforeValue = SInt32Value.Parser.ParseFrom(beforeResult.Value).Value;
            Logger.Info($"Old transaction limit number: {beforeValue}");

            if (beforeValue == limitCount)
                return;

            var authorityManager = new AuthorityManager(_nodeManager, _account);
            var minersList = authorityManager.GetCurrentMiners();
            var gensisOwner = authorityManager.GetGenesisOwnerAddress();
            var transactionResult = authorityManager.ExecuteTransactionWithAuthority(
                _configurationContractAddress.GetFormatted(),
                nameof(configurationStub.SetConfiguration),
                new SetConfigurationInput
                {
                    Key = nameof(ConfigurationNameProvider.BlockTransactionLimit),
                    Value = new SInt32Value {Value = limitCount}.ToByteString()
                },
                gensisOwner,
                minersList,
                _account
            );
            transactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var afterResult = await configurationStub.GetConfiguration.CallAsync(new StringValue
                {Value = nameof(ConfigurationNameProvider.BlockTransactionLimit)});
            var afterValue = SInt32Value.Parser.ParseFrom(afterResult.Value).Value;
            Logger.Info($"New transaction limit number: {afterValue}");
            if (afterValue == limitCount)
                Logger.Info("Transaction limit set successful.");
            else
                Logger.Error($"Transaction limit set number verify failed. {afterValue}/{limitCount}");
        }
    }
}