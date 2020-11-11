using System;
using System.Linq;
using System.Threading.Tasks;
using Acs1;
using AElf.Contracts.Configuration;
using AElf.Contracts.MultiToken;
using AElf.Kernel.SmartContractExecution;
using AElf.Types;
using AElfChain.Common.Contracts;
using AElfChain.Common.DtoExtension;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace AElf.Automation.E2ETest.ContractSuits
{
    [TestClass]
    public class ConfigurationTests : ContractTestBase
    {
        [TestMethod]
        public async Task TransactionLimit_Test()
        {
            var beforeTxLimit = await ContractManager.ConfigurationStub.GetConfiguration.CallAsync(new StringValue
                {Value = nameof(ConfigurationNameProvider.BlockTransactionLimit)});
            var beforeValue = Int32Value.Parser.ParseFrom(beforeTxLimit.Value).Value;
            var releaseResult = ContractManager.Authority.ExecuteTransactionWithAuthority(
                ContractManager.Configuration.ContractAddress, nameof(ConfigurationMethod.SetConfiguration),
                new SetConfigurationInput
                {
                    Key = nameof(ConfigurationNameProvider.BlockTransactionLimit),
                    Value = new Int32Value {Value = beforeValue + 30}.ToByteString()
                }, ContractManager.CallAddress);
            releaseResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var afterTxLimit = await ContractManager.ConfigurationStub.GetConfiguration.CallAsync(new StringValue
                {Value = nameof(ConfigurationNameProvider.BlockTransactionLimit)});
            var afterValue = Int32Value.Parser.ParseFrom(afterTxLimit.Value).Value;
            afterValue.ShouldBe(beforeValue + 30);
        }

        [TestMethod]
        public async Task StateLimitSize_Test()
        {
            var stateSize = 100 * 1024;
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
        public async Task StateLimitSize_Failed_Test()
        {
            var executionBranchThreshold = 1000;
            var executionCallThreshold = 100;
            var beforeStateLimit = await ContractManager.ConfigurationStub.GetConfiguration.CallAsync(new StringValue
                {Value = nameof(ConfigurationNameProvider.StateSizeLimit)});
            var beforeValue = Int32Value.Parser.ParseFrom(beforeStateLimit.Value).Value;
            var releaseResult = ContractManager.Authority.ExecuteTransactionWithAuthority(
                ContractManager.Configuration.ContractAddress, nameof(ConfigurationMethod.SetConfiguration),
                new SetConfigurationInput
                {
                    Key = nameof(ConfigurationNameProvider.StateSizeLimit),
                    Value = new ExecutionObserverThreshold
                    {
                        ExecutionBranchThreshold = executionBranchThreshold,
                        ExecutionCallThreshold = executionCallThreshold
                    }.ToByteString()
                }, ContractManager.CallAddress);
            releaseResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var afterStateLimit = await ContractManager.ConfigurationStub.GetConfiguration.CallAsync(new StringValue
                {Value = nameof(ConfigurationNameProvider.StateSizeLimit)});
        }
        
        [TestMethod]
        public async Task ExecutionObserverThreshold_Test()
        {
            var beforeObserverLimit = await ContractManager.ConfigurationStub.GetConfiguration.CallAsync(new StringValue
                {Value = nameof(ConfigurationNameProvider.ExecutionObserverThreshold)});
            var beforeValue = ExecutionObserverThreshold.Parser.ParseFrom(beforeObserverLimit.Value);
            var executionBranchThreshold = 1000;
            var executionCallThreshold = 100;
            
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

        [TestMethod]
        public async Task ChangeConfigurationController_Test()
        {
            var defaultOwner =
                await ContractManager.ParliamentAuthStub.GetDefaultOrganizationAddress.CallAsync(new Empty());
            var owner = await ContractManager.ConfigurationStub.GetConfigurationController.CallAsync(new Empty());
           
            var newControllerManager = AssociationOrganization;
            var proposer = ContractManager.Association.GetOrganization(newControllerManager).ProposerWhiteList.Proposers
                .First();
            if (owner.OwnerAddress.Equals(defaultOwner))
            {
                //set to first bp
                var releaseResult = ContractManager.Authority.ExecuteTransactionWithAuthority(
                    ContractManager.Configuration.ContractAddress,
                    nameof(ConfigurationMethod.ChangeConfigurationController),
                    new AuthorityInfo
                    {
                        ContractAddress = ContractManager.Association.Contract,
                        OwnerAddress = newControllerManager
                    },
                    ContractManager.CallAddress);
                releaseResult.Status.ShouldBe(TransactionResultStatus.Mined);
                var newOwner =
                    await ContractManager.ConfigurationStub.GetConfigurationController.CallAsync(new Empty());
                newOwner.ContractAddress.ShouldBe(ContractManager.Association.Contract);
                newOwner.OwnerAddress.ShouldBe(newControllerManager);
            }
            var association = ContractManager.Association;
            var members = ConfigNodes.Select(l => l.Account).ToList().Select(member => member.ConvertAddress()).Take(3);
            //create association organization
            var enumerable = members as Address[] ?? members.ToArray();
            //use new controller set configuration
            var beforeTxLimit = await ContractManager.ConfigurationStub.GetConfiguration.CallAsync(new StringValue
                {Value = nameof(ConfigurationNameProvider.BlockTransactionLimit)});
            var beforeValue = Int32Value.Parser.ParseFrom(beforeTxLimit.Value).Value;
            
            foreach (var member in enumerable)
            {
                var balance = ContractManager.Token.GetUserBalance(member.ToBase58());
                if (balance< 10_00000000)
                {
                    ContractManager.Token.TransferBalance(ContractManager.CallAddress, member.ToBase58(), 100_00000000);
                }
            }

            var setConfigurationInput = new SetConfigurationInput
            {
                Key = nameof(ConfigurationNameProvider.BlockTransactionLimit),
                Value = new Int32Value {Value = beforeValue + 10}.ToByteString()
            };
            var setConfigurationProposal = association.CreateProposal(ContractManager.Configuration.ContractAddress,
                nameof(ConfigurationMethod.SetConfiguration), setConfigurationInput, newControllerManager, proposer.ToBase58());
            foreach (var member in enumerable)
            {
                association.ApproveProposal(setConfigurationProposal, member.ToBase58());
            }
            var setConfigurationRelease = association.ReleaseProposal(setConfigurationProposal, proposer.ToBase58());
            setConfigurationRelease.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            
            var afterTxLimit = await ContractManager.ConfigurationStub.GetConfiguration.CallAsync(new StringValue
                {Value = nameof(ConfigurationNameProvider.BlockTransactionLimit)});
            var afterValue = Int32Value.Parser.ParseFrom(afterTxLimit.Value).Value;
            afterValue.ShouldBe(beforeValue + 10);

            var input = new AuthorityInfo
            {
                ContractAddress = ContractManager.Parliament.Contract,
                OwnerAddress = defaultOwner
            };
            //recover
            var setManagerResult = association.CreateProposal(ContractManager.Configuration.ContractAddress,
                nameof(ConfigurationMethod.ChangeConfigurationController), input, newControllerManager, proposer.ToBase58());
            foreach (var member in enumerable)
            {
                association.ApproveProposal(setManagerResult, member.ToBase58());
            }

            var release = association.ReleaseProposal(setManagerResult, proposer.ToBase58());
            release.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            
            owner = await ContractManager.TokenconverterStub.GetControllerForManageConnector.CallAsync(new Empty());
            owner.OwnerAddress.ShouldBe(defaultOwner);
        }
    }
}