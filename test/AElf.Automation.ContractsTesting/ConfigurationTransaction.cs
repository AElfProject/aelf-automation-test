using System.IO;
using Acs0;
using AElf.Automation.Common.Contracts;
using AElf.Automation.Common.Helpers;
using AElf.Automation.Common.WebApi.Dto;
using AElf.Types;
using Configuration;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Shouldly;

namespace AElf.Automation.ContractsTesting
{
    public class ConfigurationTransaction
    {
        private static readonly ILogHelper Logger = LogHelper.GetLogHelper();
        private readonly IApiHelper _apiHelper;
        private GenesisContract _genesisContract;
        private readonly ContractTesterFactory _stub;
        private ConfigurationContainer.ConfigurationStub _configurationStub;
        private string _account;

        public ConfigurationTransaction(string serviceUrl)
        {
            var keyStorePath = CommonHelper.GetCurrentDataDir();
            _apiHelper = new WebApiHelper(serviceUrl, keyStorePath);
            _stub = new ContractTesterFactory(serviceUrl, keyStorePath);

            GetOrCreateTestAccount();
            GetGenesisContract();
            GetConfigurationStub();
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
            var addressInfo = _genesisContract.GetContractAddressByName(NameProvider.Configuration);
            var address = addressInfo == new Address() ? DeployConfigurationContract() : addressInfo.GetFormatted();

            _configurationStub = _stub.Create<ConfigurationContainer.ConfigurationStub>(
                AddressHelper.Base58StringToAddress(address), _account);
        }

        private string DeployConfigurationContract()
        {
            var code = File.ReadAllBytes(
                $"/Users/ericshu/.local/share/aelf/contracts/AElf.Contracts.Configuration.dll");
            var commonInfo = _genesisContract.ExecuteMethodWithResult(GenesisMethod.DeploySystemSmartContract,
                new SystemContractDeploymentInput
                {
                    Category = 30,
                    Code = ByteString.CopyFrom(code),
                    Name = GenesisContract.NameProviderInfos[NameProvider.Configuration],
                    TransactionMethodCallList =
                        new SystemContractDeploymentInput.Types.SystemTransactionMethodCallList()
                });
            var transactionResultDto = commonInfo.InfoMsg as TransactionResultDto;

            return transactionResultDto.ReadableReturnValue.Replace("\"", "");
        }

        public void SetTransactionLimit(int transactionCount)
        {
            var result = _configurationStub.SetBlockTransactionLimit.SendAsync(new Int32Value
            {
                Value = transactionCount
            }).Result;
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            Logger.Info($"TransactionResult: {result.TransactionResult}");
        }

        public int GetTransactionLimit()
        {
            var queryResult = _configurationStub.GetBlockTransactionLimit.CallAsync(new Empty()).Result;
            Logger.Info($"TransactionLimit: {queryResult.Value}");

            return queryResult.Value;
        }
    }
}