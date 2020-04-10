using System.Linq;
using System.Threading.Tasks;
using Acs1;
using Acs3;
using AElf.Contracts.MultiToken;
using AElf.Contracts.Parliament;
using AElf.Contracts.Profit;
using AElf.Contracts.Treasury;
using AElf.Types;
using AElfChain.Common;
using AElfChain.Common.Contracts;
using AElfChain.Common.DtoExtension;
using Google.Protobuf.WellKnownTypes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

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
            var treasuryBalance = await ContractManager.TreasuryStub.GetCurrentTreasuryBalance.CallAsync(new Empty());
            treasuryBalance.Value.ShouldBeGreaterThanOrEqualTo(100_00000000);
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
            var interestList = await ContractManager.TreasuryStub.GetVoteWeightSetting.CallAsync(new Empty());
            interestList.VoteWeightInterestInfos.Count.ShouldBe(3);
            var newInterest = new VoteWeightInterestList();
            newInterest.VoteWeightInterestInfos.Add(new VoteWeightInterest
            {
                Capital = 1000,
                Interest = 16,
                Day = 400
            });
            var authorityResult = ContractManager.Authority.ExecuteTransactionWithAuthority(
                ContractManager.Treasury.ContractAddress,
                nameof(ContractManager.TreasuryStub.SetVoteWeightInterest),
                newInterest,
                ContractManager.CallAddress);
            authorityResult.Status.ShouldBe(TransactionResultStatus.Mined);
            interestList = await ContractManager.TreasuryStub.GetVoteWeightSetting.CallAsync(new Empty());
            interestList.VoteWeightInterestInfos.Count.ShouldBe(1);
            interestList.VoteWeightInterestInfos[0].Capital.ShouldBe(1000);
            interestList.VoteWeightInterestInfos[0].Interest.ShouldBe(16);
            interestList.VoteWeightInterestInfos[0].Day.ShouldBe(400);
        }

        [TestMethod]
        public async Task TransferAuthorizationForVoteInterest_Test()
        {
            var newInterest = new VoteWeightInterestList();
            newInterest.VoteWeightInterestInfos.Add(new VoteWeightInterest
            {
                Capital = 1000,
                Interest = 16,
                Day = 400
            });
            var updateInterestRet = (await ContractManager.TreasuryStub.SetVoteWeightInterest.SendAsync(newInterest))
                .TransactionResult;
            updateInterestRet.Status.ShouldBe(TransactionResultStatus.Failed);

            var defaultController =
                await ContractManager.ElectionStub.GetVoteWeightInterestController.CallAsync(new Empty());
            var newOrganization = await CreateParliamentOrganization();
            var authorityResult = ContractManager.Authority.ExecuteTransactionWithAuthority(
                ContractManager.Election.ContractAddress,
                nameof(ContractManager.ElectionStub.ChangeVoteWeightInterestController),
                new AuthorityInfo
                {
                    ContractAddress = defaultController.ContractAddress,
                    OwnerAddress = newOrganization
                },
                ContractManager.CallAddress);
            authorityResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var miners = ContractManager.Authority.GetCurrentMiners();
            var result = ContractManager.Authority.ExecuteTransactionWithAuthority(
                ContractManager.Treasury.ContractAddress,
                nameof(TreasuryContractContainer.TreasuryContractStub.SetVoteWeightInterest),
                newInterest, newOrganization, miners,
                ContractManager.CallAddress);
            result.Status.ShouldBe(TransactionResultStatus.Mined);

            var interestList = await ContractManager.TreasuryStub.GetVoteWeightSetting.CallAsync(new Empty());
            interestList.VoteWeightInterestInfos.Count.ShouldBe(1);
            interestList.VoteWeightInterestInfos[0].Capital.ShouldBe(1000);
            interestList.VoteWeightInterestInfos[0].Interest.ShouldBe(16);
            interestList.VoteWeightInterestInfos[0].Day.ShouldBe(400);

            //recover back
            var defaultOwner = ContractManager.Authority.GetGenesisOwnerAddress();
            authorityResult = ContractManager.Authority.ExecuteTransactionWithAuthority(
                ContractManager.Election.ContractAddress,
                nameof(ContractManager.ElectionStub.ChangeVoteWeightInterestController),
                new AuthorityInfo
                {
                    ContractAddress = defaultController.ContractAddress,
                    OwnerAddress = defaultOwner
                }, newOrganization, miners,
                ContractManager.CallAddress);
            authorityResult.Status.ShouldBe(TransactionResultStatus.Mined);
        }

        private async Task<Address> CreateParliamentOrganization()
        {
            var newParliament = new CreateOrganizationInput
            {
                ProposerAuthorityRequired = false,
                ProposalReleaseThreshold = new ProposalReleaseThreshold
                {
                    MaximalAbstentionThreshold = 1,
                    MaximalRejectionThreshold = 1,
                    MinimalApprovalThreshold = 1,
                    MinimalVoteThreshold = 1
                },
                ParliamentMemberProposingAllowed = false
            };
            var result = await ContractManager.ParliamentAuthStub.CreateOrganization.SendAsync(newParliament);
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            return result.Output;
        }
    }
}