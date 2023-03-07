using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.Configuration;
using AElf.Kernel.CodeCheck;
using AElf.Kernel.SmartContractExecution;
using AElf.Types;
using AElfChain.Common;
using AElfChain.Common.Contracts;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using log4net;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Shouldly;

namespace AElf.Automation.Contracts.ScenarioTest
{
    [TestClass]
    public class AcsCodeCheck
    {
        private static readonly ILog Logger = Log4NetHelper.GetLogger();

        public AcsCodeCheck()
        {
            Log4NetHelper.LogInit();
            NodeInfoHelper.SetConfig("nodes-local");
            var firstNode = NodeInfoHelper.Config.Nodes.First();
            var endPoint = "127.0.0.1:8001";

            NodeManager = new NodeManager(endPoint);
            ContractManager = new ContractManager(NodeManager, firstNode.Account);
        }

        public INodeManager NodeManager { get; set; }
        public ContractManager ContractManager { get; set; }

        [TestMethod]
        public async Task QueryRequiredAcsContracts()
        {
            var acsResult = await ContractManager.ConfigurationStub.GetConfiguration.CallAsync(new StringValue
                {Value = nameof(ConfigurationNameProvider.RequiredAcsInContracts)});
            var acsInfo = RequiredAcsInContracts.Parser.ParseFrom(acsResult.Value);
            Logger.Info($"{JsonConvert.SerializeObject(acsInfo)}");
        }

        [TestMethod]
        public async Task SetEnableRequireAllAcs()
        {
            var transactionResult = ContractManager.Authority.ExecuteTransactionWithAuthority(
                ContractManager.Configuration.ContractAddress,
                nameof(ContractManager.ConfigurationStub.SetConfiguration),
                new SetConfigurationInput
                {
                    Key = nameof(ConfigurationNameProvider.RequiredAcsInContracts),
                    Value = new RequiredAcsInContracts
                    {
                        AcsList = {"acs1"},
                        RequireAll = false
                    }.ToByteString()
                },
                ContractManager.CallAddress);
            transactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            await QueryRequiredAcsContracts();
        }
        
        [TestMethod]
        public async Task StateLimitSize_Test()
        {
            var stateSize = 128 * 1024;
            var beforeStateLimit = await ContractManager.ConfigurationStub.GetConfiguration.CallAsync(new StringValue
                {Value = nameof(ConfigurationNameProvider.StateSizeLimit)});
            var beforeValue = Int32Value.Parser.ParseFrom(beforeStateLimit.Value).Value;
            var releaseResult = ContractManager.Authority.ExecuteTransactionWithAuthority(
                ContractManager.Configuration.ContractAddress, nameof(ConfigurationMethod.SetConfiguration),
                new SetConfigurationInput
                {
                    Key = nameof(ConfigurationNameProvider.StateSizeLimit),
                    Value = new Int32Value {Value = stateSize}.ToByteString()
                }, ContractManager.CallAddress);
            releaseResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var afterStateLimit = await ContractManager.ConfigurationStub.GetConfiguration.CallAsync(new StringValue
                {Value = nameof(ConfigurationNameProvider.StateSizeLimit)});
            var afterValue = Int32Value.Parser.ParseFrom(afterStateLimit.Value).Value;
            afterValue.ShouldBe(stateSize);
        }
        
        [TestMethod]
        public async Task ExecutionObserverThreshold_Test()
        {
            var beforeObserverLimit = await ContractManager.ConfigurationStub.GetConfiguration.CallAsync(new StringValue
                {Value = nameof(ConfigurationNameProvider.ExecutionObserverThreshold)});
            var beforeValue = ExecutionObserverThreshold.Parser.ParseFrom(beforeObserverLimit.Value);
            var executionBranchThreshold = 15000;
            var executionCallThreshold = 15000;
            
            var releaseResult = ContractManager.Authority.ExecuteTransactionWithAuthority(
                ContractManager.Configuration.ContractAddress, nameof(ConfigurationMethod.SetConfiguration),
                new SetConfigurationInput
                {
                    Key = nameof(ConfigurationNameProvider.ExecutionObserverThreshold),
                    Value = new ExecutionObserverThreshold
                    {
                        ExecutionBranchThreshold = executionBranchThreshold,
                        ExecutionCallThreshold = executionCallThreshold
                    }.ToByteString()
                }, ContractManager.CallAddress);
            releaseResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var afterObserverLimit = await ContractManager.ConfigurationStub.GetConfiguration.CallAsync(new StringValue
                {Value = nameof(ConfigurationNameProvider.ExecutionObserverThreshold)});
            var afterValue = ExecutionObserverThreshold.Parser.ParseFrom(afterObserverLimit.Value);
            afterValue.ExecutionBranchThreshold.ShouldBe(executionBranchThreshold);
            afterValue.ExecutionCallThreshold.ShouldBe(executionCallThreshold);
        }
    }
}