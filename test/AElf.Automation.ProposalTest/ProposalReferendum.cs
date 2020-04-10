using System;
using System.Collections.Generic;
using System.Linq;
using Acs3;
using AElf.Contracts.MultiToken;
using AElf.Contracts.Referendum;
using AElf.CSharp.Core.Extension;
using AElf.Types;
using AElfChain.Common.Contracts;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Helpers;
using Google.Protobuf;
using Shouldly;

namespace AElf.Automation.ProposalTest
{
    public class ProposalReferendum : ProposalBase
    {
        public ProposalReferendum()
        {
            Initialize();
            Referendum = Services.Referendum;
            Token = Services.Token;
        }

        private List<Address> OrganizationList { get; set; }
        private Dictionary<Address, List<Hash>> ProposalList { get; set; }
        private List<Hash> ReleaseProposalList { get; set; }
        private Dictionary<Hash, List<ApproveInfo>> ProposalApproveList { get; set; }
        private Dictionary<Address, long> BalanceInfo { get; set; }

        private ReferendumAuthContract Referendum { get; }
        private TokenContract Token { get; }

        public void ReferendumJob()
        {
            ExecuteStandaloneTask(new Action[]
            {
                TransferToTester,
                CreateOrganization,
                TransferToVirtualAccount,
                CreateProposal,
                ApproveProposal,
                ReleaseProposal,
                ReclaimVoteToken,
                CheckTheBalance
            });
        }

        // Create organization
        private void CreateOrganization()
        {
            Logger.Info("Create organization:");
            OrganizationList = new List<Address>();
            var txIdList = new Dictionary<CreateOrganizationInput, string>();
            var inputList = new List<CreateOrganizationInput>();
            foreach (var proposer in Tester)
            {
                var rd = GenerateRandomNumber(500, 1000);
                var createOrganizationInput = new CreateOrganizationInput
                {
                    ProposalReleaseThreshold = new ProposalReleaseThreshold
                    {
                        MaximalAbstentionThreshold = 1000 - rd,
                        MaximalRejectionThreshold = 1000 - rd,
                        MinimalApprovalThreshold = rd,
                        MinimalVoteThreshold = rd
                    },
                    ProposerWhiteList = new ProposerWhiteList
                    {
                        Proposers = {proposer.ConvertAddress()}
                    },
                    TokenSymbol = Token.GetNativeTokenSymbol()
                };
                inputList.Add(createOrganizationInput);
            }

            foreach (var input in inputList)
            {
                var txId =
                    Referendum.ExecuteMethodWithTxId(ReferendumMethod.CreateOrganization, input);
                txIdList.Add(input, txId);
            }

            foreach (var (key, value) in txIdList)
            {
                var result = Referendum.NodeManager.CheckTransactionResult(value);
                var status = result.Status.ConvertTransactionResultStatus();
                if (status != TransactionResultStatus.Mined)
                {
                    Logger.Error("Create organization address failed.");
                }
                else
                {
                    var organizationAddress =
                        Address.Parser.ParseFrom(ByteArrayHelper.HexStringToByteArray(result.ReturnValue));
                    var info = Referendum.GetOrganization(organizationAddress);
                    info.OrganizationAddress.ShouldBe(organizationAddress);
                    info.ProposalReleaseThreshold.MaximalAbstentionThreshold.ShouldBe(key.ProposalReleaseThreshold
                        .MaximalAbstentionThreshold);
                    info.ProposalReleaseThreshold.MaximalRejectionThreshold.ShouldBe(key.ProposalReleaseThreshold
                        .MaximalRejectionThreshold);
                    info.ProposalReleaseThreshold.MinimalApprovalThreshold.ShouldBe(key.ProposalReleaseThreshold
                        .MinimalApprovalThreshold);
                    info.ProposalReleaseThreshold.MinimalVoteThreshold.ShouldBe(key.ProposalReleaseThreshold
                        .MinimalVoteThreshold);
                    OrganizationList.Add(organizationAddress);
                    Logger.Info(
                        $"Referendum organization : {organizationAddress}, MinimalVoteThreshold is {key.ProposalReleaseThreshold.MinimalVoteThreshold}");
                }
            }
        }

        private void CreateProposal()
        {
            Logger.Info("Create Proposal");
            var txIdInfos = new Dictionary<Address, List<string>>();
            ProposalList = new Dictionary<Address, List<Hash>>();
            foreach (var organizationAddress in OrganizationList)
            {
                var balance = Token.GetUserBalance(organizationAddress.GetFormatted(), Symbol);
                if (balance < 100 * OrganizationList.Count) continue;
                var txIdList = new List<string>();
                foreach (var toOrganizationAddress in OrganizationList)
                {
                    if (toOrganizationAddress.Equals(organizationAddress)) continue;

                    var transferInput = new TransferInput
                    {
                        To = toOrganizationAddress,
                        Symbol = Symbol,
                        Amount = 100,
                        Memo = "virtual account transfer virtual account"
                    };

                    var createProposalInput = new CreateProposalInput
                    {
                        ToAddress = Token.ContractAddress.ConvertAddress(),
                        OrganizationAddress = organizationAddress,
                        ContractMethodName = TokenMethod.Transfer.ToString(),
                        ExpiredTime = KernelHelper.GetUtcNow().AddDays(2),
                        Params = transferInput.ToByteString()
                    };

                    var proposer = Referendum.GetOrganization(organizationAddress).ProposerWhiteList.Proposers.First();
                    Referendum.SetAccount(proposer.GetFormatted());
                    var txId = Referendum.ExecuteMethodWithTxId(ReferendumMethod.CreateProposal, createProposalInput);
                    txIdList.Add(txId);
                }

                txIdInfos.Add(organizationAddress, txIdList);
            }

            foreach (var (key, value) in txIdInfos)
            {
                var proposalIds = new List<Hash>();
                foreach (var txId in value)
                {
                    var result = Referendum.NodeManager.CheckTransactionResult(txId);
                    var status = result.Status.ConvertTransactionResultStatus();
                    if (status != TransactionResultStatus.Mined)
                    {
                        Logger.Error("Create proposal Failed.");
                    }
                    else
                    {
                        var proposal = Hash.Parser.ParseFrom(ByteArrayHelper.HexStringToByteArray(result.ReturnValue));
                        Logger.Info($"Create proposal {proposal} through organization address {key}");
                        proposalIds.Add(proposal);
                    }
                }

                ProposalList.Add(key, proposalIds);
            }
        }

        private void ApproveProposal()
        {
            Logger.Info("Approve proposal: ");
            ProposalApproveList = new Dictionary<Hash, List<ApproveInfo>>();
            ReleaseProposalList = new List<Hash>();

            foreach (var proposal in ProposalList)
            {
                var organization = proposal.Key;
                var info = Referendum.GetOrganization(organization);
                var approveCount = info.ProposalReleaseThreshold.MinimalApprovalThreshold;
                var abstentionCount = info.ProposalReleaseThreshold.MaximalAbstentionThreshold;
                var rejectionCount = info.ProposalReleaseThreshold.MaximalRejectionThreshold;

                foreach (var proposalId in proposal.Value)
                {
                    var approveTotal = 0;
                    var rejectTotal = 0;
                    var approveTester = new List<string>();
                    var voterInfos = new List<ApproveInfo>();
                    foreach (var tester in Tester)
                    {
                        var rd = GenerateRandomNumber(100, (int) approveCount);
                        Referendum.SetAccount(tester);
                        var beforeBalance = Token.GetUserBalance(tester, TokenSymbol);
                        var fee = Token.ApproveToken(tester, Referendum.ContractAddress, rd, TokenSymbol)
                            .GetDefaultTransactionFee();
                        var transaction = Referendum.Approve(proposalId, tester);
                        var voteFee = transaction.GetDefaultTransactionFee();
                        var balance = Token.GetUserBalance(tester, TokenSymbol);
                        balance.ShouldBe(beforeBalance - rd - voteFee - fee);

                        var approveInfo = new ApproveInfo(nameof(ReferendumMethod.Approve), tester, proposalId, rd);
                        voterInfos.Add(approveInfo);
                        approveTotal += rd;
                        approveTester.Add(tester);
                        if (approveTotal >= approveCount) break;
                    }

                    var otherVoter = Tester.Where(m => !approveTester.Contains(m)).ToList();
                    var abrd = GenerateRandomNumber(1, (int) (abstentionCount - 1));
                    if (otherVoter.Count == 0)
                    {
                        ProposalApproveList.Add(proposalId, voterInfos);
                        continue;
                    }

                    var abstainTester = otherVoter.First();
                    Referendum.SetAccount(abstainTester);
                    var abBeforeBalance = Token.GetUserBalance(abstainTester, TokenSymbol);
                    var abApproveTokenFee =
                        Token.ApproveToken(abstainTester, Referendum.ContractAddress, abrd, TokenSymbol)
                            .GetDefaultTransactionFee();
                    var abTransaction = Referendum.Abstain(proposalId, abstainTester);
                    var abVoteFee = abTransaction.GetDefaultTransactionFee();
                    var abBalance = Token.GetUserBalance(abstainTester, TokenSymbol);
                    abBalance.ShouldBe(abBeforeBalance - abrd - abVoteFee - abApproveTokenFee);

                    var abstainInfo =
                        new ApproveInfo(nameof(ReferendumMethod.Abstain), abstainTester, proposalId, abrd);
                    voterInfos.Add(abstainInfo);
                    if (otherVoter.Count == 1)
                    {
                        ProposalApproveList.Add(proposalId, voterInfos);
                        continue;
                    }

                    var rejectionTesters = otherVoter.Where(r => !abstainTester.Equals(r)).ToList();
                    foreach (var rejectionTester in rejectionTesters)
                    {
                        var rjrd = GenerateRandomNumber(1, (int) (rejectionCount - 1));
                        rejectTotal += rjrd;
                        if (rejectTotal >= rejectionCount) break;
                        Referendum.SetAccount(rejectionTester);
                        var rjBeforeBalance = Token.GetUserBalance(rejectionTester, TokenSymbol);
                        var rjApproveTokenFee =
                            Token.ApproveToken(rejectionTester, Referendum.ContractAddress, rjrd, TokenSymbol)
                                .GetDefaultTransactionFee();
                        var rjTransaction = Referendum.Reject(proposalId, rejectionTester);
                        var rjVoteFee = rjTransaction.GetDefaultTransactionFee();

                        var rjBalance = Token.GetUserBalance(rejectionTester, TokenSymbol);
                        rjBalance.ShouldBe(rjBeforeBalance - rjrd - rjVoteFee - rjApproveTokenFee);

                        var rejectionInfo = new ApproveInfo(nameof(ReferendumMethod.Reject), rejectionTester,
                            proposalId,
                            rjrd);
                        voterInfos.Add(rejectionInfo);
                    }

                    ProposalApproveList.Add(proposalId, voterInfos);
                }
            }

            foreach (var (key, value) in ProposalApproveList)
            {
                var proposalStatue = Referendum.CheckProposal(key);
                proposalStatue.ToBeReleased.ShouldBeTrue();
            }
        }

        private void ReleaseProposal()
        {
            Logger.Info("Release proposal: ");
            foreach (var (key, value) in ProposalList)
            {
                var sender = Referendum.GetOrganization(key).ProposerWhiteList.Proposers.First();
                Referendum.SetAccount(sender.GetFormatted());
                foreach (var proposalId in value)
                {
                    var toBeReleased = Referendum.CheckProposal(proposalId).ToBeReleased;
                    if (!toBeReleased) continue;
                    var balance = Token.GetUserBalance(key.GetFormatted(), Symbol);
                    var result = Referendum.ExecuteMethodWithResult(ReferendumMethod.Release,
                        proposalId);
                    result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
                    var newBalance = Token.GetUserBalance(key.GetFormatted(), Symbol);
                    newBalance.ShouldBe(balance - 100);
                }
            }
        }

        private void ReclaimVoteToken()
        {
            Logger.Info("Reclaim token: ");

            foreach (var (key, value) in ProposalApproveList)
            foreach (var voterInfo in value)
            {
                Referendum.SetAccount(voterInfo.Account);
                var balance = Token.GetUserBalance(voterInfo.Account, TokenSymbol);
                Logger.Info(
                    $"Before reclaim vote token the account {voterInfo.Account} balance is {balance}, reclaim token is {voterInfo.Amount}");

                var result = Referendum.ExecuteMethodWithResult(ReferendumMethod.ReclaimVoteToken, voterInfo.Proposal);
                var status = result.Status.ConvertTransactionResultStatus();
                if (status != TransactionResultStatus.Mined) Logger.Error("Reclaim token failed");
                var reclaimFee = result.GetDefaultTransactionFee();
                var newBalance = Token.GetUserBalance(voterInfo.Account, TokenSymbol);
                ;
                newBalance.ShouldBe(balance + voterInfo.Amount - reclaimFee);
                Logger.Info(
                    $"After reclaim vote token the account {voterInfo.Account} balance is {newBalance},reclaim fee is {reclaimFee}");
            }
        }

        private void CheckTheBalance()
        {
            Logger.Info("After Referendum test, check the balance of organization address:");
            foreach (var balanceInfo in BalanceInfo)
            {
                var balance = Token.GetUserBalance(balanceInfo.Key.GetFormatted(), Symbol);
                balance.ShouldBe(balanceInfo.Value);
                Logger.Info($"{balanceInfo.Key} {Symbol} balance is {balance}");
            }

            Logger.Info("After Referendum test, check the balance of tester:");
            foreach (var tester in Tester)
            {
                var balance = Token.GetUserBalance(tester, TokenSymbol);
                Logger.Info($"{tester} {TokenSymbol} balance is {balance}");
            }
        }


        private void TransferToVirtualAccount()
        {
            BalanceInfo = new Dictionary<Address, long>();
            foreach (var organization in OrganizationList)
            {
                var balance = Token.GetUserBalance(organization.GetFormatted(), Symbol);
                if (balance >= 1000_00000000)
                {
                    BalanceInfo.Add(organization, balance);
                    continue;
                }

                Token.TransferBalance(InitAccount, organization.GetFormatted(), 1000_00000000, Symbol);
                balance = Token.GetUserBalance(organization.GetFormatted(), Symbol);
                BalanceInfo.Add(organization, balance);
                Logger.Info($"{organization} {Symbol} token balance is {balance}");
            }
        }
    }
}