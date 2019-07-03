using System.Threading.Tasks;
using AElf.Automation.Common.Contracts;
using AElf.Automation.Common.Helpers;
using AElf.Contracts.Genesis;
using AElf.Types;
using Configuration;
using Google.Protobuf.WellKnownTypes;
using Volo.Abp.Threading;

namespace AElf.Automation.RpcPerformance
{
    public class TransactionExecuteLimit
    {
        private readonly IApiHelper _apiHelper;
        private readonly string _account;
        private readonly ContractTesterFactory _stub;
        private readonly ILogHelper _logger = LogHelper.GetLogHelper();

        public TransactionExecuteLimit(string url, string account)
        {
            _account = account;

            var keyStorePath = CommonHelper.GetCurrentDataDir();
            _apiHelper = new WebApiHelper(url, keyStorePath);
            _stub = new ContractTesterFactory(url, keyStorePath);
        }

        public void SetExecutionSelectTransactionLimit()
        {
            var configurationStub = AsyncHelper.RunSync(GetConfigurationContractStub);
            var limitCount = ConfigInfoHelper.Config.SelectTxLimit;
            AsyncHelper.RunSync(() => SetSelectTransactionLimit(configurationStub, limitCount));
        }

        private async Task<ConfigurationContainer.ConfigurationStub> GetConfigurationContractStub()
        {
            var chainStatus = await _apiHelper.ApiService.GetChainStatus();
            var genesisContractAddress = chainStatus.GenesisContractAddress;
            _logger.WriteInfo($"Genesis contract address: {genesisContractAddress}");

            var basicZeroStub =
                _stub.Create<BasicContractZeroContainer.BasicContractZeroStub>(Address.Parse(genesisContractAddress),
                    _account);
            var configurationAddress =
                await basicZeroStub.GetContractAddressByName.CallAsync(
                    GenesisContract.NameProviderInfos[NameProvider.Configuration]);
            _logger.WriteInfo($"Configuration contract address: {configurationAddress.GetFormatted()}");

            var configurationStub =
                _stub.Create<ConfigurationContainer.ConfigurationStub>(configurationAddress, _account);

            return configurationStub;
        }

        private async Task SetSelectTransactionLimit(ConfigurationContainer.ConfigurationStub configurationStub,
            int limitCount)
        {
            var beforeResult = await configurationStub.GetBlockTransactionLimit.CallAsync(new Empty());
            _logger.WriteInfo($"Old transaction limit number: {beforeResult.Value}");

            if (beforeResult.Value == limitCount)
                return;

            await configurationStub.SetBlockTransactionLimit.SendAsync(new Int32Value
            {
                Value = limitCount
            });
            var afterResult = await configurationStub.GetBlockTransactionLimit.CallAsync(new Empty());
            _logger.WriteInfo($"New transaction limit number: {afterResult.Value}");
            if (afterResult.Value == limitCount)
                _logger.WriteInfo("Transaction limit set successful.");
        }
    }
}