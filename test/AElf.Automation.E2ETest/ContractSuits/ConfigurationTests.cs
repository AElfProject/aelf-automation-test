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
    }
}