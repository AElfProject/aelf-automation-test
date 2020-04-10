using System.IO;
using System.Linq;
using Acs0;
using AElf.Contracts.Configuration;
using AElf.Types;
using AElfChain.Common.Contracts;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using log4net;
using Shouldly;

namespace AElf.Automation.ContractsTesting
{
    public class ConfigurationTransaction
    {
        private static readonly ILog Logger = Log4NetHelper.GetLogger();
        private readonly INodeManager _nodeManager;
        private readonly ContractTesterFactory _stub;
        private string _account;
        private ConfigurationContainer.ConfigurationStub _configurationStub;
        private GenesisContract _genesisContract;

        public ConfigurationTransaction(string serviceUrl)
        {
            var keyStorePath = CommonHelper.GetCurrentDataDir();
            _nodeManager = new NodeManager(serviceUrl, keyStorePath);
            _stub = new ContractTesterFactory(_nodeManager);

            GetOrCreateTestAccount();
            GetGenesisContract();
            GetConfigurationStub();
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
            var addressInfo = _genesisContract.GetContractAddressByName(NameProvider.Configuration);
            var address = addressInfo == new Address() ? DeployConfigurationContract() : addressInfo.GetFormatted();

            _configurationStub = _stub.Create<ConfigurationContainer.ConfigurationStub>(
                AddressHelper.Base58StringToAddress(address), _account);
        }

        private string DeployConfigurationContract()
        {
            var code = File.ReadAllBytes(
                "/Users/ericshu/.local/share/aelf/contracts/AElf.Contracts.Configuration.dll");
            var transactionResult = _genesisContract.ExecuteMethodWithResult(GenesisMethod.DeploySystemSmartContract,
                new SystemContractDeploymentInput
                {
                    Category = 30,
                    Code = ByteString.CopyFrom(code),
                    Name = GenesisContract.NameProviderInfos[NameProvider.Configuration],
                    TransactionMethodCallList =
                        new SystemContractDeploymentInput.Types.SystemTransactionMethodCallList()
                });
            var byteString =
                ByteString.FromBase64(transactionResult.Logs.First(l => l.Name.Contains(nameof(ContractDeployed)))
                    .NonIndexed);
            var deployAddress = ContractDeployed.Parser.ParseFrom(byteString).Address;

            return deployAddress.GetFormatted();
        }

        public void SetTransactionLimit(int transactionCount)
        {
            var result = _configurationStub.SetConfiguration.SendAsync(new SetConfigurationInput
            {
                Key = nameof(ConfigurationNameProvider.BlockTransactionLimit),
                Value = new SInt32Value {Value = transactionCount}.ToByteString()
            }).Result;
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            Logger.Info($"TransactionResult: {result.TransactionResult}");
        }

        public int GetTransactionLimit()
        {
            var queryResult = _configurationStub.GetConfiguration.CallAsync(new StringValue
                {Value = nameof(ConfigurationNameProvider.BlockTransactionLimit)}).Result;
            Logger.Info($"TransactionLimit: {queryResult.Value}");

            return SInt32Value.Parser.ParseFrom(queryResult.Value).Value;
        }
    }
}