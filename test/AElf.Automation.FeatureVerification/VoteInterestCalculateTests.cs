using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.Election;
using AElf.Contracts.MultiToken;
using AElf.CSharp.Core;
using AElf.CSharp.Core.Extension;
using AElf.Types;
using AElfChain.Common;
using AElfChain.Common.Contracts;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using Google.Protobuf.WellKnownTypes;
using log4net;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Shouldly;
using Volo.Abp.Threading;
using GetWelfareRewardAmountSampleInput = AElf.Contracts.Treasury.GetWelfareRewardAmountSampleInput;

namespace AElf.Automation.Contracts.ScenarioTest
{
    [TestClass]
    public class VoteInterestCalculateTests
    {
        private const int DaySec = 86400;
        private static readonly ILog Logger = Log4NetHelper.GetLogger();

        public VoteInterestCalculateTests()
        {
            Log4NetHelper.LogInit();
            NodeInfoHelper.SetConfig("nodes-env2-main");
            Reviwers = NodeInfoHelper.Config.Nodes.Select(o => o.Account).ToList().GetRange(4, 5);

            var firstNode = NodeInfoHelper.Config.Nodes.First();
            NodeManager = new NodeManager(firstNode.Endpoint);
            AuthorityManager = new AuthorityManager(NodeManager, firstNode.Account);
            ContractManager = new ContractManager(NodeManager, firstNode.Account);
            InterestInstance = new InterestCalculate(ContractManager);
        }

        public INodeManager NodeManager { get; set; }
        public AuthorityManager AuthorityManager { get; set; }
        public ContractManager ContractManager { get; set; }
        public InterestCalculate InterestInstance { get; set; }

        public List<string> Reviwers { get; set; }

        [TestMethod]
        public async Task ChangeOrganization_Test()
        {
            var newControllerAddress = AuthorityManager.CreateAssociationOrganization(Reviwers);
            //Query manager address
            var beforeController =
                await ContractManager.TokenconverterStub.GetControllerForManageConnector.CallAsync(new Empty());
            Logger.Info($"Manager address: {beforeController}");
            beforeController.OwnerAddress.ShouldBe(ContractManager.Parliament.GetGenesisOwnerAddress());

            //Set manager address
            var transactionResult = ContractManager.Authority.ExecuteTransactionWithAuthority(
                ContractManager.TokenConverter.ContractAddress,
                nameof(ContractManager.TokenconverterStub.ChangeConnectorController),
                new AuthorityInfo
                {
                    ContractAddress = ContractManager.Association.Contract,
                    OwnerAddress = newControllerAddress
                },
                ContractManager.CallAddress);
            transactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            //verify manager permission
            var afterController =
                await ContractManager.TokenconverterStub.GetControllerForManageConnector.CallAsync(new Empty());
            Logger.Info($"Manager address: {afterController}");
            afterController.OwnerAddress.ShouldBe(newControllerAddress);
        }

        [TestMethod]
        [DataRow("65ngL8Rp8mZQHCXqht9GH3xT4tk9CaU95hHaMNE1VmqR7dJ4W")]
        public async Task RevertOrganization_Test(string organizationAddress)
        {
            var defaultOrganization = ContractManager.Parliament.GetGenesisOwnerAddress();
            //revert back
            var controller =
                await ContractManager.TokenconverterStub.GetControllerForManageConnector.CallAsync(new Empty());
            var createProposal = ContractManager.Association.CreateProposal(
                ContractManager.TokenConverter.ContractAddress,
                nameof(ContractManager.TokenconverterStub.ChangeConnectorController), new AuthorityInfo
                {
                    ContractAddress = controller.ContractAddress,
                    OwnerAddress = defaultOrganization
                }, organizationAddress.ConvertAddress(), Reviwers.First());
            foreach (var approver in Reviwers)
            {
                var approveResult = ContractManager.Association.ApproveWithResult(createProposal, approver);
                approveResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            }

            var releaseResult = ContractManager.Association.ReleaseProposal(createProposal, Reviwers.First());
            releaseResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            //query again
            var orgAddress =
                await ContractManager.TokenconverterStub.GetControllerForManageConnector.CallAsync(new Empty());
            Logger.Info($"Manager address: {orgAddress}");
            orgAddress.OwnerAddress.ShouldBe(ContractManager.Parliament.GetGenesisOwnerAddress());
        }

        [TestMethod]
        public async Task SetVoteInterest_Test()
        {
            var queryResult = await ContractManager.ElectionStub.GetVoteWeightSetting.CallAsync(new Empty());
            Logger.Info($"{JsonConvert.SerializeObject(queryResult)}");
            
            var transactionResult = ContractManager.Authority.ExecuteTransactionWithAuthority(
                ContractManager.Election.ContractAddress,
                nameof(ContractManager.ElectionStub.SetVoteWeightInterest),
                new VoteWeightInterestList
                {
                    VoteWeightInterestInfos =
                    {
                        new VoteWeightInterest
                        {
                            Day = 90,
                            Capital = 10000,
                            Interest = 10
                        },
                        new VoteWeightInterest
                        {
                            Day = 180,
                            Capital = 10000,
                            Interest = 12
                        },
                        new VoteWeightInterest
                        {
                            Day = 270,
                            Capital = 10000,
                            Interest = 15
                        },
                        new VoteWeightInterest
                        {
                            Day = 360,
                            Capital = 10000,
                            Interest = 18
                        },
                        new VoteWeightInterest
                        {
                            Day = 720,
                            Capital = 10000,
                            Interest = 20
                        },
                        new VoteWeightInterest
                        {
                            Day = 1080,
                            Capital = 10000,
                            Interest = 24
                        }
                    }
                }, ContractManager.CallAddress);
            transactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            //Query interest
            queryResult = await ContractManager.ElectionStub.GetVoteWeightSetting.CallAsync(new Empty());
            Logger.Info($"{JsonConvert.SerializeObject(queryResult)}");
        }

        [TestMethod]
        [DataRow(100, 90)]
        [DataRow(100, 180)]
        [DataRow(100, 270)]
        [DataRow(100, 360)]
        [DataRow(100, 720)]
        [DataRow(100, 1080)]
        public void GetDifferentVoteInterest(long voteAmounts, long lockDay)
        {
            var lockTime = lockDay * DaySec;
            var weight = InterestInstance.GetVotesWeight(voteAmounts, lockTime);
            Logger.Info($"VoteAmount: {voteAmounts}, LockTime: {lockTime}, Weight: {weight}");
        }

        [TestMethod]
        public async Task VoteMembers_Test()
        {
            var accounts = new List<string>
            {
                "cVR3HUxMoyttSXxnMs4P9GXY89rbPUrKzuzkRzB8BmotTLAai",
                "W4xEKTZcvPKXRAmdu9xEpM69ArF7gUxDh9MDgtsKnu7JfePXo",
                "ieESD3Hg2vjhVFiXPgiDbtdRvFFW4VWksfjk9sXuknvEZjaMs",
                "2aQvd1jNCsWvQ5PC9xpdwZ9nzoNyqudaDJzayeeeP3rmBBN8y2",
                "2NwLnsXeVxA7VivcBbKgsKteDcp3EWwkKYrjVATk9SfAYcJEDJ",
                "2sosaZrhx64NwA3nxoyReYjGKvULAxoZLyzuNM94Sg4wknpdRU"
            };

            //prepare token
            foreach (var account in accounts)
            {
                var transferResult = await ContractManager.TokenStub.Transfer.SendAsync(new TransferInput
                {
                    To = account.ConvertAddress(),
                    Symbol = "ELF",
                    Amount = 200_00000000,
                    Memo = "vote token"
                });
                transferResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            }

            //vote
            var voteDay = new[] {90, 180, 270, 360, 720, 1079};
            for (var i = 0; i < accounts.Count; i++)
            {
                var electionStub = ContractManager.Genesis.GetElectionStub(accounts[i]);
                var voteResult = await electionStub.Vote.SendAsync(new VoteMinerInput
                {
                    CandidatePubkey =
                        "044958d5c48f003c771769f4a31413cd18053516615cbde502441af8452fb53441a80cc48a7f3b0f2552fd030cacbe9012ba055a3d553b70003f2e637d55fa0f23",
                    Amount = 100,
                    EndTimestamp = KernelHelper.GetUtcNow().AddDays(voteDay[i]).AddHours(1)
                });
                voteResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            }
        }

        [TestMethod]
        public async Task GetWelfareRewardAmountSample_Test()
        {
            const long lockTime1 = 90 * 24 * 60 * 60;
            const long lockTime2 = 180 * 24 * 60 * 60;
            const long lockTime3 = 270 * 24 * 60 * 60;
            const long lockTime4 = 360 * 24 * 60 * 60;
            const long lockTime5 = 720 * 24 * 60 * 60;

            var result = await ContractManager.TreasuryStub.GetWelfareRewardAmountSample.CallAsync(
                new GetWelfareRewardAmountSampleInput
                {
                    Value = {lockTime1, lockTime2, lockTime3, lockTime4, lockTime5}
                });
            Logger.Info($"{JsonConvert.SerializeObject(result)}");
        }

        [TestMethod]
        public async Task PrepareSomeTokenForTest()
        {
            var nodeAccounts = NodeInfoHelper.Config.Nodes.Select(o => o.Account);
            var first = nodeAccounts.First();
            var tokenStub = ContractManager.Genesis.GetTokenStub(first);
            foreach (var account in nodeAccounts)
            {
                if (account == first) continue;
                var balance = ContractManager.Token.GetUserBalance(account);
                if (balance <= 100_00000000)
                {
                    var txResult = await tokenStub.Transfer.SendAsync(new TransferInput
                    {
                        To = account.ConvertAddress(),
                        Amount = 100_00000000L,
                        Symbol = "ELF",
                        Memo = "token execution"
                    });
                    txResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
                }
            }
        }

        [TestMethod]
        public async Task GetVoteWeightInterestController()
        {
            var controller = await ContractManager.ElectionStub.GetVoteWeightInterestController.CallAsync(new Empty());
            Logger.Info($"{controller.ContractAddress} {controller.OwnerAddress}");
        }

        [TestMethod]
        public async Task GetVoteWeightSetting()
        {
            var queryResult = await ContractManager.ElectionStub.GetVoteWeightSetting.CallAsync(new Empty());
            Logger.Info($"{JsonConvert.SerializeObject(queryResult)}");
        }
    }

    public class InterestCalculate
    {
        private const int DaySec = 86400;
        private const int Scale = 10000;

        public InterestCalculate(ContractManager cm)
        {
            CM = cm;
            InterestList = AsyncHelper.RunSync(GetVoteWeightInterestList);
        }

        public ContractManager CM { get; set; }
        public VoteWeightInterestList InterestList { get; set; }

        public long GetVotesWeight(long votesAmount, long lockTime)
        {
            long calculated = 1;
            var lockDays = lockTime.Div(DaySec);

            foreach (var instMap in InterestList.VoteWeightInterestInfos)
            {
                if (lockDays > instMap.Day)
                    continue;
                var initBase = 1 + (decimal) instMap.Interest / instMap.Capital;
                calculated = calculated.Mul((long) (Pow(initBase, (uint) lockDays) * Scale));
                break;
            }

            if (calculated == 1)
            {
                var maxInterestInfo = InterestList.VoteWeightInterestInfos.Last();
                var maxInterestBase = 1 + (decimal) maxInterestInfo.Interest / maxInterestInfo.Capital;
                calculated = calculated.Mul((long) (Pow(maxInterestBase, (uint) lockDays) * Scale));
            }

            return votesAmount.Mul(calculated).Add(votesAmount.Div(2)); // weight = lockTime + voteAmount 
        }

        private static decimal Pow(decimal x, uint y)
        {
            if (y == 1)
                return (long) x;
            var a = 1m;
            if (y == 0)
                return a;
            var e = new BitArray(BitConverter.GetBytes(y));
            var t = e.Count;
            for (var i = t - 1; i >= 0; --i)
            {
                a *= a;
                if (e[i]) a *= x;
            }

            return a;
        }

        private async Task<VoteWeightInterestList> GetVoteWeightInterestList()
        {
            return await CM.ElectionStub.GetVoteWeightSetting.CallAsync(new Empty());
        }
    }
}