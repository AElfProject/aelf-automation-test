using System.Linq;
using System.Threading.Tasks;
using Acs1;
using Acs10;
using Acs3;
using AElf.Contracts.Election;
using AElf.Contracts.MultiToken;
using AElf.Contracts.Referendum;
using AElf.Contracts.Profit;
using AElf.Contracts.Treasury;
using AElf.Types;
using AElfChain.Common;
using AElfChain.Common.Contracts;
using AElfChain.Common.DtoExtension;
using Google.Protobuf.WellKnownTypes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using GetWelfareRewardAmountSampleInput = AElf.Contracts.Treasury.GetWelfareRewardAmountSampleInput;

namespace AElf.Automation.E2ETest.ContractSuits
{
    [TestClass]
    public class TreasuryTests : ContractTestBase
    {
        [TestMethod]
        public async Task Donate_Test()
        {
            var nodeAccounts = NodeInfoHelper.Config.Nodes.Select(o => o.Account).ToList();
            //prepare tester
            string tester;
            while (true)
            {
                tester = ContractManager.NodeManager.GetRandomAccount();
                if (nodeAccounts.Contains(tester)) continue;
                var transferResult = await ContractManager.TokenStub.Transfer.SendAsync(new TransferInput
                {
                    To = tester.ConvertAddress(),
                    Symbol = "ELF",
                    Amount = 100_00000000,
                    Memo = "Vote test"
                });
                transferResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
                break;
            }

            //donate
            var beforeBalance = ContractManager.Token.GetUserBalance(tester);
            var treasuryStub = ContractManager.Genesis.GetTreasuryStub(tester);
            var donateResult = await treasuryStub.Donate.SendAsync(new DonateInput
            {
                Symbol = "ELF",
                Amount = 50_00000000
            });
            donateResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var transactionFee = donateResult.TransactionResult.GetDefaultTransactionFee();
            var afterBalance = ContractManager.Token.GetUserBalance(tester);
            beforeBalance.ShouldBe(afterBalance + transactionFee + 50_00000000);

            //donate all
            var donateAllResult = await treasuryStub.DonateAll.SendAsync(new DonateAllInput
            {
                Symbol = "ELF"
            });
            donateAllResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var balance = ContractManager.Token.GetUserBalance(tester);
            balance.ShouldBe(0);

            //query treasury balance
            var treasuryBalance = await ContractManager.TreasuryStub.GetUndistributedDividends.CallAsync(new Empty());
            treasuryBalance.Value.First().Value.ShouldBeGreaterThanOrEqualTo(100_00000000);
        }

        [TestMethod]
        public async Task GetTreasurySchemeId_Test()
        {
            var profitIds = (await ContractManager.ProfitStub.GetManagingSchemeIds.CallAsync(
                new GetManagingSchemeIdsInput
                {
                    Manager = ContractManager.Treasury.Contract
                })).SchemeIds;

            var schemeId = await ContractManager.TreasuryStub.GetTreasurySchemeId.CallAsync(new Empty());
            schemeId.ShouldNotBe(Hash.Empty);
            schemeId.ShouldBe(profitIds[0]);
        }

        [TestMethod]
        [Ignore("If current term without vote or candidates, cases will be failure.")]
        public async Task GetRewardAmountSample_Test()
        {
            var profitIds = (await ContractManager.ProfitStub.GetManagingSchemeIds.CallAsync(
                new GetManagingSchemeIdsInput
                {
                    Manager = ContractManager.Election.Contract
                })).SchemeIds;
            var citizenWelfareHash = profitIds[1];
            var termNo = await ContractManager.ConsensusStub.GetCurrentTermNumber.CallAsync(new Empty());
            var contributeProfitsResult = await ContractManager.ProfitStub.ContributeProfits.SendAsync(
                new ContributeProfitsInput
                {
                    Amount = 800_00000000,
                    Period = termNo.Value,
                    Symbol = "ELF",
                    SchemeId = citizenWelfareHash
                });
            contributeProfitsResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            const int lockTime1 = 300 * 24 * 60 * 60;
            const int lockTime2 = 600 * 24 * 60 * 60;
            const int lockTime3 = 900 * 24 * 60 * 60;
            var rewardAmount = await ContractManager.TreasuryStub.GetWelfareRewardAmountSample.CallAsync(
                new GetWelfareRewardAmountSampleInput
                {
                    Value = {lockTime1, lockTime2, lockTime3}
                });
            rewardAmount.Value.Count.ShouldBe(3);
            var rewardMoney = rewardAmount.Value.ToArray();
            rewardMoney[0].ShouldBeGreaterThan(0);
            rewardMoney[1].ShouldBeGreaterThan(rewardMoney[0]);
            rewardMoney[2].ShouldBeGreaterThan(rewardMoney[1]);
        }

        [TestMethod]
        public async Task ModifyVoteInterest_Test()
        {
            var interestList = await ContractManager.ElectionStub.GetVoteWeightSetting.CallAsync(new Empty());
//            interestList.VoteWeightInterestInfos.Count.ShouldBe(1);
            var newInterest = new VoteWeightInterestList();
            newInterest.VoteWeightInterestInfos.Add(new VoteWeightInterest
            {
                Capital = 1100,
                Interest = 15,
                Day = 500
            });
            var authorityResult = ContractManager.Authority.ExecuteTransactionWithAuthority(
                ContractManager.Election.ContractAddress,
                nameof(ContractManager.ElectionStub.SetVoteWeightInterest),
                newInterest,
                ContractManager.CallAddress);
            authorityResult.Status.ShouldBe(TransactionResultStatus.Mined);
            interestList = await ContractManager.ElectionStub.GetVoteWeightSetting.CallAsync(new Empty());
            interestList.VoteWeightInterestInfos[0].Capital.ShouldBe(1100);
            interestList.VoteWeightInterestInfos[0].Interest.ShouldBe(15);
            interestList.VoteWeightInterestInfos[0].Day.ShouldBe(500);
        }

        [TestMethod]
        public async Task SetVoteWeightProportion_Test()
        {
            var defaultSetting = await ContractManager.ElectionStub.GetVoteWeightProportion.CallAsync(new Empty());
            defaultSetting.TimeProportion.ShouldBe(2);
            defaultSetting.AmountProportion.ShouldBe(1);        
            var input = new VoteWeightProportion
            {
                TimeProportion = 3,
                AmountProportion = 3
            };
            
            var authorityResult = ContractManager.Authority.ExecuteTransactionWithAuthority(
                ContractManager.Election.ContractAddress,
                nameof(ContractManager.ElectionStub.SetVoteWeightProportion),
                input,
                ContractManager.CallAddress);
            authorityResult.Status.ShouldBe(TransactionResultStatus.Mined);
            
            var updateSetting = await ContractManager.ElectionStub.GetVoteWeightProportion.CallAsync(new Empty());
            updateSetting.TimeProportion.ShouldBe(3);
            updateSetting.AmountProportion.ShouldBe(3);       
        }

        [TestMethod]
        public async Task TransferAuthorizationForVoteInterest_Test()
        {
            var referendum = ContractManager.Referendum;
            var proposer = ConfigNodes.First().Account.ConvertAddress();
            var newInterest = new VoteWeightInterestList();
            newInterest.VoteWeightInterestInfos.Add(new VoteWeightInterest
            {
                Capital = 1000,
                Interest = 16,
                Day = 400
            });
            
            var miners = ContractManager.Authority.GetCurrentMiners();
            var defaultController = ContractManager.Parliament.GetGenesisOwnerAddress();
            var newOrganization = ReferendumOrganization;
            var authorityResult = ContractManager.Authority.ExecuteTransactionWithAuthority(
                ContractManager.Election.ContractAddress,
                nameof(ContractManager.ElectionStub.ChangeVoteWeightInterestController),
                new AuthorityInfo
                {
                    ContractAddress = ContractManager.Referendum.Contract,
                    OwnerAddress = newOrganization
                },
                miners.First());
            authorityResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var interestProposalId = referendum.CreateProposal( ContractManager.Election.ContractAddress,
                nameof(ElectionContractContainer.ElectionContractStub.SetVoteWeightInterest), newInterest,
                newOrganization, proposer.GetFormatted());
            ContractManager.Token.ApproveToken(proposer.GetFormatted(), referendum.ContractAddress,
                2000_00000000, "ELF");
            referendum.SetAccount(proposer.GetFormatted());
            var interestApproveResult = referendum.ExecuteMethodWithResult(ReferendumMethod.Approve, interestProposalId);
            interestApproveResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            
            var interestReleaseResult = referendum.ReleaseProposal(interestProposalId, proposer.GetFormatted());
            interestReleaseResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var interestList = await ContractManager.ElectionStub.GetVoteWeightSetting.CallAsync(new Empty());
            interestList.VoteWeightInterestInfos.Count.ShouldBe(1);
            interestList.VoteWeightInterestInfos[0].Capital.ShouldBe(1000);
            interestList.VoteWeightInterestInfos[0].Interest.ShouldBe(16);
            interestList.VoteWeightInterestInfos[0].Day.ShouldBe(400);

            //recover back
            var input = new AuthorityInfo
            {
                ContractAddress = ContractManager.Parliament.Contract,
                OwnerAddress = defaultController
            };
            var proposalId = referendum.CreateProposal(ContractManager.Election.ContractAddress,
                nameof(ContractManager.ElectionStub.ChangeVoteWeightInterestController), input,
                newOrganization, proposer.GetFormatted());
            ContractManager.Token.ApproveToken(proposer.GetFormatted(), referendum.ContractAddress,
                2000, "ELF");
            referendum.SetAccount(proposer.GetFormatted());
            var approveResult = referendum.ExecuteMethodWithResult(ReferendumMethod.Approve, proposalId);
            approveResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            
            var releaseResult = referendum.ReleaseProposal(proposalId, proposer.GetFormatted());
            releaseResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var controller =
                await ContractManager.ElectionStub.GetVoteWeightInterestController.CallAsync(new Empty());
            controller.ContractAddress.ShouldBe(ContractManager.Parliament.Contract);
        }

        [TestMethod]
        public async Task ChangeTreasuryController()
        {
            var referendum = ContractManager.Referendum;
            var proposer = ConfigNodes.First().Account.ConvertAddress();
            var miners = ContractManager.Authority.GetCurrentMiners();
            var newOrganization = ReferendumOrganization;
            ContractManager.Token.ApproveToken(proposer.GetFormatted(), referendum.ContractAddress,
                2000, "ELF");
            
            var defaultController = await ContractManager.TreasuryStub.GetTreasuryController.CallAsync(new Empty());
            defaultController.ContractAddress.ShouldBe(ContractManager.Parliament.Contract);
            var input = new AuthorityInfo
            {
                ContractAddress = ContractManager.Referendum.Contract,
                OwnerAddress = newOrganization
            };
            var authorityResult = ContractManager.Authority.ExecuteTransactionWithAuthority(
                ContractManager.Treasury.ContractAddress,
                nameof(ContractManager.TreasuryStub.ChangeTreasuryController),
                input,
                ContractManager.CallAddress);;
            authorityResult.Status.ShouldBe(TransactionResultStatus.Mined);
            
            //SetDividendPoolWeightSetting
            var dividendPoolWeight =
                await ContractManager.TreasuryStub.GetDividendPoolWeightProportion.CallAsync(new Empty());
            dividendPoolWeight.BackupSubsidyProportionInfo.Proportion.ShouldBe(5);
            dividendPoolWeight.CitizenWelfareProportionInfo.Proportion.ShouldBe(75);
            dividendPoolWeight.MinerRewardProportionInfo.Proportion.ShouldBe(20);

            var setInput = new DividendPoolWeightSetting
            {
                BackupSubsidyWeight = 10,
                CitizenWelfareWeight = 70,
                MinerRewardWeight = 20
            };
            var setDividendId = referendum.CreateProposal( ContractManager.Treasury.ContractAddress,
                nameof(TreasuryContractContainer.TreasuryContractStub.SetDividendPoolWeightSetting),setInput,
                newOrganization, proposer.GetFormatted());
            ContractManager.Token.ApproveToken(proposer.GetFormatted(), referendum.ContractAddress,
                2000_00000000, "ELF");
            referendum.SetAccount(proposer.GetFormatted());
            var setApproveResult = referendum.ExecuteMethodWithResult(ReferendumMethod.Approve, setDividendId);
            setApproveResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            
            var setReleaseResult = referendum.ReleaseProposal(setDividendId, proposer.GetFormatted());
            setReleaseResult.Status.ShouldBe(TransactionResultStatus.Mined);
            
            var updateDividendPoolWeight =
                await ContractManager.TreasuryStub.GetDividendPoolWeightProportion.CallAsync(new Empty());
            updateDividendPoolWeight.BackupSubsidyProportionInfo.Proportion.ShouldBe(10);
            updateDividendPoolWeight.CitizenWelfareProportionInfo.Proportion.ShouldBe(70);
            updateDividendPoolWeight.MinerRewardProportionInfo.Proportion.ShouldBe(20);
            
            //recover back
            var recoverInput = new AuthorityInfo
            {
                ContractAddress = ContractManager.Parliament.Contract,
                OwnerAddress = defaultController.OwnerAddress
            };
            var proposalId = referendum.CreateProposal(ContractManager.Treasury.ContractAddress,
                nameof(ContractManager.TreasuryStub.ChangeTreasuryController), recoverInput,
                newOrganization, proposer.GetFormatted());
            ContractManager.Token.ApproveToken(proposer.GetFormatted(), referendum.ContractAddress,
                2000, "ELF");
            referendum.SetAccount(proposer.GetFormatted());
            var approveResult = referendum.ExecuteMethodWithResult(ReferendumMethod.Approve, proposalId);
            approveResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            
            var releaseResult = referendum.ReleaseProposal(proposalId, proposer.GetFormatted());
            releaseResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var controller =
                await ContractManager.TreasuryStub.GetTreasuryController.CallAsync(new Empty());
            controller.ContractAddress.ShouldBe(ContractManager.Parliament.Contract);
        }
    }
}