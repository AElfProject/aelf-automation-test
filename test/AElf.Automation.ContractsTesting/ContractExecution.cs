using System.Threading.Tasks;
using AElf.Automation.Common.Contracts;
using AElf.Automation.Common.Helpers;
using AElf.Contracts.TestContract.BasicFunction;
using AElf.Contracts.TestContract.BasicUpdate;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using log4net;
using Shouldly;
using BetInput = AElf.Contracts.TestContract.BasicFunction.BetInput;
using BetLimitInput = AElf.Contracts.TestContract.BasicUpdate.BetLimitInput;
using InitialBasicContractInput = AElf.Contracts.TestContract.BasicFunction.InitialBasicContractInput;

namespace AElf.Automation.ContractsTesting
{
    public class ContractExecution
    {
        private static readonly ILog Logger = Log4NetHelper.GetLogger();
        private readonly IApiHelper _apiHelper;
        private readonly string _account;

        private GenesisContract _genesisContract;
        private readonly ContractTesterFactory _stub;
        private string _contractAddress;
        private BasicFunctionContractContainer.BasicFunctionContractStub _basicFunctionContractStub;
        private BasicUpdateContractContainer.BasicUpdateContractStub _basicUpdateContractStub;

        public ContractExecution(string serviceUrl)
        {
            var keyStorePath = CommonHelper.GetCurrentDataDir();
            _apiHelper = new WebApiHelper(serviceUrl, keyStorePath);
            _stub = new ContractTesterFactory(serviceUrl, keyStorePath);

            var accountInfo = _apiHelper.NewAccount(
                new CommandInfo(ApiMethods.AccountNew)
                    {Parameter = "123"});
            _account = accountInfo.InfoMsg.ToString();
            _genesisContract = GenesisContract.GetGenesisContract(_apiHelper, _account);
        }

        public void DeployTestContract()
        {
            var functionContract = new BasicFunctionContract(_apiHelper, _account);
            _contractAddress = functionContract.ContractAddress;
        }

        public async Task UpdateContract()
        {
            var owner = _genesisContract.GetContractAuthor(_contractAddress);

            _genesisContract.SetAccount(owner.GetFormatted());
            var result = _genesisContract.UpdateContract(owner.GetFormatted(), _contractAddress,
                BasicUpdateContract.ContractFileName);
            if (result)
                Logger.Info("Contract update successfully.");

            await Task.CompletedTask;
        }

        public async Task ExecuteBasicContractMethods()
        {
            _basicFunctionContractStub = _stub.Create<BasicFunctionContractContainer.BasicFunctionContractStub>(
                Address.Parse(_contractAddress), _account);

            //init contract
            var initResult =
                await _basicFunctionContractStub.InitialBasicFunctionContract
                    .SendAsync(new InitialBasicContractInput
                    {
                        ContractName = "Basic Contract",
                        MinValue = 10L,
                        MaxValue = 1000L,
                        MortgageValue = 1000_000_000L,
                        Manager = Address.Parse(_account)
                    });
            initResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            //execute method
            var executionResult = await _basicFunctionContractStub.UserPlayBet.SendAsync(new BetInput
            {
                Int64Value = 60
            });
            executionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            //call method
            var queryResult = await _basicFunctionContractStub.QueryRewardMoney.CallAsync(new Empty());
            queryResult.Int64Value.ShouldBeGreaterThanOrEqualTo(0);
        }

        public async Task ExecuteUpdateContractMethods()
        {
            _basicUpdateContractStub = _stub.Create<BasicUpdateContractContainer.BasicUpdateContractStub>(
                Address.Parse(_contractAddress), _account);

            //execute method
            var executeResult = await _basicUpdateContractStub.UpdateBetLimit.SendAsync(new BetLimitInput
            {
                MinValue = 60,
                MaxValue = 99
            });
            executeResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            //call method
            var queryResult = await _basicUpdateContractStub.QueryUserLoseMoney.CallAsync(Address.Parse(_account));
            queryResult.Int64Value.ShouldBeGreaterThanOrEqualTo(0);
        }
    }
}