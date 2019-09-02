using System.Threading.Tasks;
using AElf.Automation.Common.Contracts;
using AElf.Automation.Common.Helpers;
using AElf.Automation.Common.OptionManagers.Authority;
using AElf.Contracts.Configuration;
using AElf.Contracts.Genesis;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using log4net;
using Shouldly;
using Volo.Abp.Threading;

namespace AElf.Automation.RpcPerformance
{
    public class TransactionExecuteLimit
    {
        private readonly IApiHelper _apiHelper;
        private readonly string _account;
        private readonly ContractTesterFactory _stub;
        private readonly NodeTransactionOption _nodeTransactionOption;
        private static readonly ILog Logger = Log4NetHelper.GetLogger();

        private Address _configurationContractAddress; 

        public TransactionExecuteLimit(string url, string account)
        {
            _account = account;

            _apiHelper = new WebApiHelper(url);
            _stub = new ContractTesterFactory(_apiHelper);
            _nodeTransactionOption = ConfigInfoHelper.Config.NodeTransactionOption;
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
            var gensis = GenesisContract.GetGenesisContract(_apiHelper, _account);

            var configurationContract = gensis.GetConfigurationContract();
            _configurationContractAddress = configurationContract.Contract;
            
            return gensis.GetConfigurationStub();
        }

        private async Task SetSelectTransactionLimit(ConfigurationContainer.ConfigurationStub configurationStub,
            int limitCount)
        {
            var beforeResult = await configurationStub.GetBlockTransactionLimit.CallAsync(new Empty());
            Logger.Info($"Old transaction limit number: {beforeResult.Value}");

            if (beforeResult.Value == limitCount)
                return;

            var authorityManager = new AuthorityManager(_apiHelper, _account);
            var minersList = authorityManager.GetCurrentMiners();
            var gensisOwner = authorityManager.GetGenesisOwnerAddress();
            var transactionResult = authorityManager.ExecuteTransactionWithAuthority(_configurationContractAddress.GetFormatted(),
                nameof(configurationStub.SetBlockTransactionLimit),
                new Int32Value {Value = limitCount},
                gensisOwner,
                minersList,
                _account
            );
            transactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            
            var afterResult = await configurationStub.GetBlockTransactionLimit.CallAsync(new Empty());
            Logger.Info($"New transaction limit number: {afterResult.Value}");
            if (afterResult.Value == limitCount)
                Logger.Info("Transaction limit set successful.");
        }
    }
}