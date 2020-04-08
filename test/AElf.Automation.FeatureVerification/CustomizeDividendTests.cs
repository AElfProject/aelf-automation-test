using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElfChain.Common;
using AElfChain.Common.Contracts;
using AElf.Contracts.Profit;
using AElf.Contracts.Treasury;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using Google.Protobuf.WellKnownTypes;
using log4net;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Shouldly;

namespace AElf.Automation.Contracts.ScenarioTest
{
    [TestClass]
    public class CustomizeDividendTests
    {
        private ILog Logger { get; }
        public INodeManager NodeManager { get; set; }
        public ContractManager ContractManager { get; set; }
        public Dictionary<SchemeType, Scheme> Schemes { get; set; }
        
        public CustomizeDividendTests()
        {
            Log4NetHelper.LogInit("CustomizeDividendTests");
            Logger = Log4NetHelper.GetLogger();

            NodeInfoHelper.SetConfig("nodes-env2-main");
            var node = NodeInfoHelper.Config.Nodes.First();

            NodeManager = new NodeManager(node.Endpoint);
            ContractManager = new ContractManager(NodeManager, node.Account);
            ContractManager.Profit.GetTreasurySchemes(ContractManager.Treasury.ContractAddress);
            Schemes = ProfitContract.Schemes;
        }

        [TestMethod]
        public async Task Treasury_Dividend_Pool_Weight_Update_Test()
        {
            var defaultWeightSetting =
                await ContractManager.TreasuryStub.GetDividendPoolWeightProportion.CallAsync(new Empty());
            defaultWeightSetting.BackupSubsidyProportionInfo.Proportion.ShouldBe(5);
            defaultWeightSetting.CitizenWelfareProportionInfo.Proportion.ShouldBe(75);
            defaultWeightSetting.MinerRewardProportionInfo.Proportion.ShouldBe(20);
            var newWeightSetting = new DividendPoolWeightSetting
            {
                BackupSubsidyWeight = 1,
                CitizenWelfareWeight = 1,
                MinerRewardWeight = 8
            };
            ContractManager.Authority.ExecuteTransactionWithAuthority(ContractManager.Treasury.ContractAddress,
                nameof(TreasuryContractContainer.TreasuryContractStub.SetDividendPoolWeightSetting),
                newWeightSetting, ContractManager.CallAddress);
            var updatedWeightSetting =
                await ContractManager.TreasuryStub.GetDividendPoolWeightProportion.CallAsync(new Empty());
            updatedWeightSetting.BackupSubsidyProportionInfo.Proportion.ShouldBe(10);
            updatedWeightSetting.CitizenWelfareProportionInfo.Proportion.ShouldBe(10);
            updatedWeightSetting.MinerRewardProportionInfo.Proportion.ShouldBe(80);

            var treasuryProfit =
                await ContractManager.ProfitStub.GetScheme.CallAsync(Schemes[SchemeType.Treasury].SchemeId);
            var subSchemes = treasuryProfit.SubSchemes;
            subSchemes.Count.ShouldBe(3);
            var weightOneCount = subSchemes.Count(x => x.Shares == 1);
            weightOneCount.ShouldBe(2);
            var weightEightCount = subSchemes.Count(x => x.Shares == 8);
            weightEightCount.ShouldBe(1);
        }

        [TestMethod]
        public async Task Treasury_Dividend_Pool_Weight_Update_To_Miner_Reward_Weight_Test()
        {
            var newWeightSetting = new DividendPoolWeightSetting
            {
                BackupSubsidyWeight = 1,
                CitizenWelfareWeight = 1,
                MinerRewardWeight = 8
            };
            ContractManager.Authority.ExecuteTransactionWithAuthority(ContractManager.Treasury.ContractAddress,
                nameof(TreasuryContractContainer.TreasuryContractStub.SetDividendPoolWeightSetting),
                newWeightSetting, ContractManager.CallAddress);
            var defaultWeightSetting =
                await ContractManager.TreasuryStub.GetMinerRewardWeightProportion.CallAsync(new Empty());
            defaultWeightSetting.BasicMinerRewardProportionInfo.Proportion.ShouldBe(50);
            defaultWeightSetting.ReElectionRewardProportionInfo.Proportion.ShouldBe(25);
            defaultWeightSetting.VotesWeightRewardProportionInfo.Proportion.ShouldBe(25);
        }

        [TestMethod]
        public async Task Miner_Reward_Weight_Update_Test()
        {
            var defaultWeightSetting =
                await ContractManager.TreasuryStub.GetMinerRewardWeightProportion.CallAsync(new Empty());
            defaultWeightSetting.BasicMinerRewardProportionInfo.Proportion.ShouldBe(50);
            defaultWeightSetting.ReElectionRewardProportionInfo.Proportion.ShouldBe(25);
            defaultWeightSetting.VotesWeightRewardProportionInfo.Proportion.ShouldBe(25);
            var newWeightSetting = new MinerRewardWeightSetting
            {
                BasicMinerRewardWeight = 1,
                ReElectionRewardWeight = 1,
                VotesWeightRewardWeight = 8
            };
            ContractManager.Authority.ExecuteTransactionWithAuthority(ContractManager.Treasury.ContractAddress,
                nameof(TreasuryContractContainer.TreasuryContractStub.SetMinerRewardWeightSetting),
                newWeightSetting, ContractManager.CallAddress);
            var updatedWeightSetting =
                await ContractManager.TreasuryStub.GetMinerRewardWeightProportion.CallAsync(new Empty());
            updatedWeightSetting.BasicMinerRewardProportionInfo.Proportion.ShouldBe(10);
            updatedWeightSetting.ReElectionRewardProportionInfo.Proportion.ShouldBe(10);
            updatedWeightSetting.VotesWeightRewardProportionInfo.Proportion.ShouldBe(80);

            var minerRewardProfit =
                await ContractManager.ProfitStub.GetScheme.CallAsync(Schemes[SchemeType.MinerReward].SchemeId);
            var subSchemes = minerRewardProfit.SubSchemes;
            subSchemes.Count.ShouldBe(3);
            var weightOneCount = subSchemes.Count(x => x.Shares == 1);
            weightOneCount.ShouldBe(2);
            var weightEightCount = subSchemes.Count(x => x.Shares == 8);
            weightEightCount.ShouldBe(1);
        }

        [TestMethod]
        public async Task QueryDividends_Test()
        {
            var dividends = await ContractManager.TreasuryStub.GetDividendPoolWeightProportion.CallAsync(new Empty());
            Logger.Info("GetDividendPoolWeightProportion");
            Logger.Info(JsonConvert.SerializeObject(dividends));

            var minerReward = await ContractManager.TreasuryStub.GetMinerRewardWeightProportion.CallAsync(new Empty());
            Logger.Info("GetMinerRewardWeightProportion");
            Logger.Info(JsonConvert.SerializeObject(minerReward));
        }
    }
}