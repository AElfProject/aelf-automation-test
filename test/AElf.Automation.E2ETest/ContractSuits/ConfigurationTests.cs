using System.Threading.Tasks;
using AElf.Types;
using AElfChain.Common.Contracts;
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
            var beforeTxLimit = await ContractManager.ConfigurationStub.GetBlockTransactionLimit.CallAsync(new Empty());
            var releaseResult = ContractManager.Authority.ExecuteTransactionWithAuthority(
                ContractManager.Configuration.ContractAddress, nameof(ConfigurationMethod.SetBlockTransactionLimit),
                new Int32Value
                {
                    Value = beforeTxLimit.Value + 10
                }, ContractManager.CallAddress);
            releaseResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var afterTxLimit = await ContractManager.ConfigurationStub.GetBlockTransactionLimit.CallAsync(new Empty());
            afterTxLimit.Value.ShouldBe(beforeTxLimit.Value + 10);
        }

        [TestMethod]
        public async Task ChangeConfigurationController_Test()
        {
            var defaultOwner = await ContractManager.ParliamentAuthStub.GetDefaultOrganizationAddress.CallAsync(new Empty());
            var owner = await ContractManager.ConfigurationStub.GetConfigurationController.CallAsync(new Empty());
            if (owner.Equals(defaultOwner))
            {
                //set to first bp
                var releaseResult = ContractManager.Authority.ExecuteTransactionWithAuthority(
                    ContractManager.Configuration.ContractAddress, nameof(ConfigurationMethod.ChangeConfigurationController),
                    ContractManager.CallAccount,
                    ContractManager.CallAddress);
                releaseResult.Status.ShouldBe(TransactionResultStatus.Mined);
                var newOwner = await ContractManager.ConfigurationStub.GetConfigurationController.CallAsync(new Empty());
                newOwner.ShouldBe(ContractManager.CallAccount);
            }

            //recover
            var configurationStub = ContractManager.Genesis.GetConfigurationStub(ContractManager.CallAddress);
            var transactionResult = await configurationStub.ChangeConfigurationController.SendAsync(defaultOwner);
            transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            owner = await ContractManager.ConfigurationStub.GetConfigurationController.CallAsync(new Empty());
            owner.ShouldBe(defaultOwner);
        }
    }
}