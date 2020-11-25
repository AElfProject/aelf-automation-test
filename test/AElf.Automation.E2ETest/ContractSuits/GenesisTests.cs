using System.Threading.Tasks;
using AElf.Types;
using AElfChain.Common.Managers;
using Google.Protobuf.WellKnownTypes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace AElf.Automation.E2ETest.ContractSuits
{
    [TestClass]
    public class GenesisTests : ContractTestBase
    {
        [TestMethod]
        public async Task GetContractInfo_Test()
        {
            var genesis = ContractManager.NodeManager.GetGenesisContract();
            var contract = await ContractManager.GenesisImplStub.GetContractInfo.CallAsync(genesis.Contract);
            contract.Category.ShouldBe(0);
            contract.IsSystemContract.ShouldBeTrue();
            contract.SerialNumber.ShouldBe(0L);
            contract.Author.ShouldBe(genesis.Contract);

            var tokenContract =
                await ContractManager.GenesisImplStub.GetContractInfo.CallAsync(ContractManager.Token.Contract);
            tokenContract.Category.ShouldBe(0);
            tokenContract.IsSystemContract.ShouldBeTrue();
            tokenContract.SerialNumber.ShouldNotBe(0L);
            tokenContract.Author.ShouldBe(genesis.Contract);
        }

        [TestMethod]
        public async Task CurrentContractSerialNumber()
        {
            var serialNumber = await ContractManager.GenesisImplStub.CurrentContractSerialNumber.CallAsync(new Empty());
            serialNumber.Value.ShouldBeGreaterThan(1U);
        }

        [TestMethod]
        public async Task GetContractAuthor_Test()
        {
            var tokenAuthor =
                await ContractManager.GenesisImplStub.GetContractAuthor.CallAsync(ContractManager.Token.Contract);
            tokenAuthor.ShouldBe(ContractManager.Genesis.Contract);
        }

        [TestMethod]
        public async Task GetContractHash_Test()
        {
            var tokenAddress = ContractManager.Token.Contract;
            var tokenHash = await ContractManager.GenesisImplStub.GetContractHash.CallAsync(tokenAddress);
            var contractInfo = await ContractManager.GenesisImplStub.GetContractInfo.CallAsync(tokenAddress);
            var registrationInfo =
                await ContractManager.GenesisImplStub.GetSmartContractRegistrationByAddress.CallAsync(tokenAddress);
            var codeHash = HashHelper.ComputeFrom(registrationInfo.Code.ToByteArray());
            tokenHash.ShouldBe(codeHash);
            contractInfo.CodeHash.ShouldBe(codeHash);
        }
    }
}