using AElf.Automation.Common.Contracts;
using AElf.Automation.Common.Helpers;
using AElf.Contracts.Configuration;
using Google.Protobuf.WellKnownTypes;
using log4net;

namespace AElf.Automation.QueryTransaction
{
    public class ConfigurationLimit
    {
        private static readonly ILog Logger = Log4NetHelper.GetLogger();
        private readonly IApiHelper _apiHelper;
        private GenesisContract _genesisContract;
        private readonly ContractTesterFactory _stub;
        private ConfigurationContainer.ConfigurationStub _configurationStub;
        private string _account;
        
        public ConfigurationLimit(string serviceUrl)
        {
            var keyStorePath = CommonHelper.GetCurrentDataDir();
            _apiHelper = new WebApiHelper(serviceUrl, keyStorePath);
            _stub = new ContractTesterFactory(_apiHelper);

            GetOrCreateTestAccount();
            GetGenesisContract();
            GetConfigurationStub();
        }
        public int GetTransactionLimit()
        {
            var queryResult = _configurationStub.GetBlockTransactionLimit.CallAsync(new Empty()).Result;
            Logger.Info($"TransactionLimit: {queryResult.Value}");

            return queryResult.Value;
        }
        
        private void GetOrCreateTestAccount()
        {
            var accountInfo = _apiHelper.NewAccount(
                new CommandInfo(ApiMethods.AccountNew)
                    {Parameter = "123"});
            _account = accountInfo.InfoMsg.ToString();
        }

        private void GetGenesisContract()
        {
            _genesisContract = GenesisContract.GetGenesisContract(_apiHelper, _account);
        }

        private void GetConfigurationStub()
        {
            var configurationAddress = _genesisContract.GetContractAddressByName(NameProvider.Configuration);

            _configurationStub = _stub.Create<ConfigurationContainer.ConfigurationStub>(
                configurationAddress, _account);
        }
    }
}