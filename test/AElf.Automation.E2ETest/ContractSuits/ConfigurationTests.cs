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
            var beforeTxLimit = await ChainManager.ConfigurationStub.GetBlockTransactionLimit.CallAsync(new Empty());
            var releaseResult = ChainManager.Authority.ExecuteTransactionWithAuthority(ChainManager.ConfigurationService.ContractAddress, nameof(ConfigurationMethod.SetBlockTransactionLimit), new Int32Value
            {
                Value = beforeTxLimit.Value + 10
            },  ChainManager.CallAddress);
            releaseResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var afterTxLimit = await ChainManager.ConfigurationStub.GetBlockTransactionLimit.CallAsync(new Empty());
            afterTxLimit.Value.ShouldBe(beforeTxLimit.Value + 10);
        }

        [TestMethod]
        public async Task ChangeOwnerAddress_Test()
        {
            var defaultOwner = await ChainManager.ParliamentStub.GetDefaultOrganizationAddress.CallAsync(new Empty());
            var owner = await ChainManager.ConfigurationStub.GetOwnerAddress.CallAsync(new Empty());
            if (owner.Equals(defaultOwner))
            {
                //set to first bp
                var releaseResult = ChainManager.Authority.ExecuteTransactionWithAuthority(
                    ChainManager.ConfigurationService.ContractAddress, nameof(ConfigurationMethod.ChangeOwnerAddress),
                    ChainManager.CallAccount,
                    ChainManager.CallAddress);
                releaseResult.Status.ShouldBe(TransactionResultStatus.Mined);
                var newOwner = await ChainManager.ConfigurationStub.GetOwnerAddress.CallAsync(new Empty());
                newOwner.ShouldBe(ChainManager.CallAccount);
            }
            
            //recover
            var configurationStub = ChainManager.GenesisService.GetConfigurationStub(ChainManager.CallAddress);
            var transactionResult = await configurationStub.ChangeOwnerAddress.SendAsync(defaultOwner);
            transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            owner = await ChainManager.ConfigurationStub.GetOwnerAddress.CallAsync(new Empty());
            owner.ShouldBe(defaultOwner);
        }
    }
}