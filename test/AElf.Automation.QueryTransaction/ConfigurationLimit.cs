using AElfChain.Common.Contracts;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using AElf.Contracts.Configuration;
using AElfChain.Common.Managers;
using Google.Protobuf.WellKnownTypes;
using log4net;

namespace AElf.Automation.QueryTransaction
{
    public class ConfigurationLimit
    {
        private static readonly ILog Logger = Log4NetHelper.GetLogger();
        private readonly INodeManager _nodeManager;
        private GenesisContract _genesisContract;
        private readonly ContractTesterFactory _stub;
        private ConfigurationContainer.ConfigurationStub _configurationStub;
        private string _account;
        
        public ConfigurationLimit(string serviceUrl)
        {
            var keyStorePath = CommonHelper.GetCurrentDataDir();
            _nodeManager = new NodeManager(serviceUrl, keyStorePath);
            _stub = new ContractTesterFactory(_nodeManager);

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
            _account = _nodeManager.NewAccount();
        }

        private void GetGenesisContract()
        {
            _genesisContract = GenesisContract.GetGenesisContract(_nodeManager, _account);
        }

        private void GetConfigurationStub()
        {
            var configurationAddress = _genesisContract.GetContractAddressByName(NameProvider.Configuration);

            _configurationStub = _stub.Create<ConfigurationContainer.ConfigurationStub>(
                configurationAddress, _account);
        }
    }
}