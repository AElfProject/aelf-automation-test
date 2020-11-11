using System;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.Configuration;
using AElf.Kernel.SmartContractExecution;
using AElf.Types;
using AElfChain.Common;
using AElfChain.Common.Contracts;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using log4net;
using Shouldly;

namespace AElf.Automation.SetConfiguration
{
    public class SetConfiguration
    {
        private static readonly ILog Logger = Log4NetHelper.GetLogger();

        public SetConfiguration(INodeManager nodeManager)
        {
            NodeManager = nodeManager;
            Caller = NodeOption.AllNodes.First().Account;
            Genesis = nodeManager.GetGenesisContract(Caller);
        }

        private INodeManager NodeManager { get; }
        private GenesisContract Genesis { get; }
        private string Caller { get; }

        public void SetAllConfiguration(int amount)
        {
            SetBlockTransactionLimit(amount);
            SetStateSizeLimit(amount);
            SetExecutionObserverThreshold(amount);
        }

        public void SetBlockTransactionLimit(int amount)
        {
            var authority = new AuthorityManager(NodeManager,Caller);
            var configuration = Genesis.GetConfigurationContract();

            var releaseResult = authority.ExecuteTransactionWithAuthority(
                configuration.ContractAddress, nameof(ConfigurationMethod.SetConfiguration),
                new SetConfigurationInput
                {
                    Key = nameof(ConfigurationNameProvider.BlockTransactionLimit),
                    Value = new Int32Value {Value = amount}.ToByteString()
                }, Caller);
            releaseResult.Status.ShouldBe(TransactionResultStatus.Mined);
        }
        
        public void SetStateSizeLimit(int amount)
        {
            var authority = new AuthorityManager(NodeManager,Caller);
            var configuration = Genesis.GetConfigurationContract();

            var releaseResult = authority.ExecuteTransactionWithAuthority(
                configuration.ContractAddress, nameof(ConfigurationMethod.SetConfiguration),
                new SetConfigurationInput
                {
                    Key = nameof(ConfigurationNameProvider.StateSizeLimit),
                    Value = new Int32Value {Value = amount}.ToByteString()
                }, Caller);
            releaseResult.Status.ShouldBe(TransactionResultStatus.Mined);
        }
        
        public void SetExecutionObserverThreshold(int amount)
        {
            var authority = new AuthorityManager(NodeManager,Caller);
            var configuration = Genesis.GetConfigurationContract();
            var executionBranchThreshold = amount;
            var executionCallThreshold = amount;
            var releaseResult = authority.ExecuteTransactionWithAuthority(
                configuration.ContractAddress, nameof(ConfigurationMethod.SetConfiguration),
                new SetConfigurationInput
                {
                    Key = nameof(ConfigurationNameProvider.ExecutionObserverThreshold),
                    Value = new ExecutionObserverThreshold
                    {
                        ExecutionBranchThreshold = executionBranchThreshold,
                        ExecutionCallThreshold = executionCallThreshold
                    }.ToByteString()
                }, Caller);
            releaseResult.Status.ShouldBe(TransactionResultStatus.Mined);
        }

        public async Task QueryAllConfiguration()
        {
            Logger.Info("ExecutionObserverThreshold: ");
            var configurationStub = Genesis.GetConfigurationStub();
            var beforeObserverLimit = await configurationStub.GetConfiguration.CallAsync(new StringValue
                {Value = nameof(ConfigurationNameProvider.ExecutionObserverThreshold)});
            var observerLimit = ExecutionObserverThreshold.Parser.ParseFrom(beforeObserverLimit.Value);
            Logger.Info(
                $"ExecutionBranchThreshold: {observerLimit.ExecutionBranchThreshold}; ExecutionCallThreshold: {observerLimit.ExecutionCallThreshold}");

            Logger.Info("StateSizeLimit: ");
            var beforeStateSizeLimit = await configurationStub.GetConfiguration.CallAsync(new StringValue
                {Value = nameof(ConfigurationNameProvider.StateSizeLimit)});
            var stateSizeLimitValue = Int32Value.Parser.ParseFrom(beforeStateSizeLimit.Value);
            Logger.Info($"StateSizeLimit: {stateSizeLimitValue.Value}");

            Logger.Info("BlockTransactionLimit: ");
            var transactionLimit = await configurationStub.GetConfiguration.CallAsync(new StringValue
                {Value = nameof(ConfigurationNameProvider.BlockTransactionLimit)});
            var transactionLimitValue = Int32Value.Parser.ParseFrom(transactionLimit.Value);
            Logger.Info($"BlockTransactionLimit: {transactionLimitValue.Value}");
        }
    }
}