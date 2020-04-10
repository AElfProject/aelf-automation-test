using System.Linq;
using System.Threading.Tasks;
using Acs1;
using Acs3;
using AElf.Contracts.Configuration;
using AElf.Contracts.Parliament;
using AElf.Types;
using AElfChain.Common.Contracts;
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
            var beforeValue = SInt32Value.Parser.ParseFrom(beforeTxLimit.Value).Value;
            var releaseResult = ContractManager.Authority.ExecuteTransactionWithAuthority(
                ContractManager.Configuration.ContractAddress, nameof(ConfigurationMethod.SetConfiguration),
                new SetConfigurationInput
                {
                    Key = nameof(ConfigurationNameProvider.BlockTransactionLimit),
                    Value = new SInt32Value {Value = beforeValue + 10}.ToByteString()
                }, ContractManager.CallAddress);
            releaseResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var afterTxLimit = await ContractManager.ConfigurationStub.GetConfiguration.CallAsync(new StringValue
                {Value = nameof(ConfigurationNameProvider.BlockTransactionLimit)});
            var afterValue = SInt32Value.Parser.ParseFrom(afterTxLimit.Value).Value;
            afterValue.ShouldBe(beforeValue + 10);
        }

        [TestMethod]
        public async Task ChangeConfigurationController_Test()
        {
            var defaultOwner =
                await ContractManager.ParliamentAuthStub.GetDefaultOrganizationAddress.CallAsync(new Empty());
            var owner = await ContractManager.ConfigurationStub.GetConfigurationController.CallAsync(new Empty());
            var miners = ContractManager.Authority.GetCurrentMiners();
            var parliamentStub =
                ContractManager.ParliamentAuth.GetTestStub<ParliamentContractContainer.ParliamentContractStub>(
                    miners.First());
            var createManagerController =
                await parliamentStub.CreateOrganization.SendAsync(new CreateOrganizationInput
                {
                    ProposalReleaseThreshold = new ProposalReleaseThreshold
                    {
                        MaximalAbstentionThreshold = 1000,
                        MaximalRejectionThreshold = 1000,
                        MinimalApprovalThreshold = 2000,
                        MinimalVoteThreshold = 2000
                    }
                });
            var newControllerManager = createManagerController.Output;
            if (owner.OwnerAddress.Equals(defaultOwner))
            {
                //set to first bp
                var releaseResult = ContractManager.Authority.ExecuteTransactionWithAuthority(
                    ContractManager.Configuration.ContractAddress,
                    nameof(ConfigurationMethod.ChangeConfigurationController),
                    new AuthorityInfo
                    {
                        ContractAddress = ContractManager.ParliamentAuth.Contract,
                        OwnerAddress = newControllerManager
                    },
                    ContractManager.CallAddress);
                releaseResult.Status.ShouldBe(TransactionResultStatus.Mined);
                var newOwner =
                    await ContractManager.ConfigurationStub.GetConfigurationController.CallAsync(new Empty());
                newOwner.ContractAddress.ShouldBe(ContractManager.ParliamentAuth.Contract);
                newOwner.OwnerAddress.ShouldBe(newControllerManager);
            }

            //recover
            var setManagerResult = ContractManager.Authority.ExecuteTransactionWithAuthority(
                ContractManager.Configuration.ContractAddress,
                nameof(ConfigurationMethod.ChangeConfigurationController),
                new AuthorityInfo
                {
                    ContractAddress = ContractManager.ParliamentAuth.Contract,
                    OwnerAddress = defaultOwner
                }, miners.First(), newControllerManager);
            setManagerResult.Status.ShouldBe(TransactionResultStatus.Mined);
            owner = await ContractManager.TokenconverterStub.GetControllerForManageConnector.CallAsync(new Empty());
            owner.OwnerAddress.ShouldBe(defaultOwner);
        }
    }
}