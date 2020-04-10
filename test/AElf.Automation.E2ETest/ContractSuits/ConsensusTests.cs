using System.Threading.Tasks;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Cryptography;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace AElf.Automation.E2ETest.ContractSuits
{
    [TestClass]
    public class ConsensusTests : ContractTestBase
    {
        [TestMethod]
        public async Task GetMaximumBlocksCount_Test()
        {
            var blocksCount = await ContractManager.ConsensusStub.GetMaximumBlocksCount.CallAsync(new Empty());
            blocksCount.Value.ShouldBe(8);
        }

        [TestMethod]
        public async Task GetCurrentMinerListWithRoundNumber_Test()
        {
            var information =
                await ContractManager.ConsensusStub.GetCurrentMinerListWithRoundNumber.CallAsync(new Empty());
            information.MinerList.Pubkeys.Count.ShouldBeGreaterThanOrEqualTo(1);
            information.RoundNumber.ShouldBeGreaterThan(1);
        }

        [TestMethod]
        public async Task GetRoundInformation_Test()
        {
            var currentRoundNo = await ContractManager.ConsensusStub.GetCurrentRoundNumber.CallAsync(new Empty());
            var roundInformation = await ContractManager.ConsensusStub.GetRoundInformation.CallAsync(currentRoundNo);
            roundInformation.ShouldNotBe(new Round());
            roundInformation.RoundNumber.ShouldBeGreaterThanOrEqualTo(1);
            roundInformation.RealTimeMinersInformation.Keys.Count.ShouldBeGreaterThanOrEqualTo(1);

            var termNo = await ContractManager.ConsensusStub.GetCurrentTermNumber.CallAsync(new Empty());
            termNo.Value.ShouldBe(roundInformation.TermNumber);
        }

        [TestMethod]
        public async Task GetPreviousRoundInformation_Test()
        {
            var currentRoundNo = await ContractManager.ConsensusStub.GetCurrentRoundNumber.CallAsync(new Empty());
            var previousRound = await ContractManager.ConsensusStub.GetPreviousRoundInformation.CallAsync(new Empty());
            previousRound.RoundNumber.ShouldBe(currentRoundNo.Value - 1);
            previousRound.RealTimeMinersInformation.Keys.Count.ShouldBeGreaterThanOrEqualTo(1);
        }

        [TestMethod]
        public async Task GetCurrentWelfareReward_Test()
        {
            var welfareReward = await ContractManager.ConsensusStub.GetCurrentWelfareReward.CallAsync(new Empty());
            welfareReward.Value.ShouldBeGreaterThan(0);
        }

        [TestMethod]
        public async Task GetMinerList_Test()
        {
            var minerList = await ContractManager.ConsensusStub.GetCurrentMinerList.CallAsync(new Empty());
            minerList.Pubkeys.Count.ShouldBeGreaterThanOrEqualTo(1);

            var previousMinerList = await ContractManager.ConsensusStub.GetPreviousMinerList.CallAsync(new Empty());
            previousMinerList.Pubkeys.Count.ShouldBeGreaterThanOrEqualTo(1);
        }

        [TestMethod]
        public async Task GetNextElectCountDown_Test()
        {
            var count = await ContractManager.ConsensusStub.GetNextElectCountDown.CallAsync(new Empty());
            count.Value.ShouldBeGreaterThan(1);
            await Task.Delay(1000);
            var count1 = await ContractManager.ConsensusStub.GetNextElectCountDown.CallAsync(new Empty());
            count1.Value.ShouldBeGreaterThan(1);
            count.Value.ShouldNotBe(count1.Value);
        }

        [TestMethod]
        public async Task Miner_Test()
        {
            var minerPubkey = await ContractManager.ConsensusStub.GetCurrentMinerPubkey.CallAsync(new Empty());
            var otherTest = Address.FromPublicKey(CryptoHelper.GenerateKeyPair().PublicKey);
            var minerAccount = Address.FromPublicKey(ByteArrayHelper.HexStringToByteArray(minerPubkey.Value));

            var isCurrentMiner = await ContractManager.ConsensusStub.IsCurrentMiner.CallAsync(otherTest);
            isCurrentMiner.Value.ShouldBeFalse();

            isCurrentMiner = await ContractManager.ConsensusStub.IsCurrentMiner.CallAsync(minerAccount);
            isCurrentMiner.Value.ShouldBeTrue();

            var nextMinerPubkey = await ContractManager.ConsensusStub.GetNextMinerPubkey.CallAsync(new Empty());
            nextMinerPubkey.Value.ShouldNotBeNullOrEmpty();
        }

        [TestMethod]
        public async Task GetPreviousMinerList_Test()
        {
            var minerList = await ContractManager.ConsensusStub.GetPreviousMinerList.CallAsync(new Empty());
            minerList.Pubkeys.Count.ShouldBeGreaterThanOrEqualTo(1);
        }

        [TestMethod]
        public async Task GetMinerBlocksOfPreviousTerm_Test()
        {
            var result = await ContractManager.ConsensusStub.GetMinedBlocksOfPreviousTerm.CallAsync(new Empty());
            result.Value.ShouldBeGreaterThan(0);
        }
    }
}