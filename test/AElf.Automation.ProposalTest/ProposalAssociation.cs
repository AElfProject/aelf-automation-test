using System;
using System.Collections.Generic;
using System.Linq;
using Acs3;
using AElf.Contracts.Association;
using AElf.Contracts.MultiToken;
using AElf.CSharp.Core.Extension;
using AElf.Types;
using AElfChain.Common.Contracts;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Helpers;
using Google.Protobuf;
using Shouldly;

namespace AElf.Automation.ProposalTest
{
    public class ProposalAssociation : ProposalBase
    {
        public ProposalAssociation()
        {
            Initialize();
            Association = Services.Association;
            Token = Services.Token;
        }

        private Dictionary<Address, Organization> OrganizationList { get; set; }
        private Dictionary<KeyValuePair<Address, Organization>, List<Hash>> ProposalList { get; set; }
        private List<OrganizationMemberList> OrganizationMemberInfos { get; set; }
        private Dictionary<Address, long> BalanceInfo { get; set; }
        private AssociationAuthContract Association { get; }
        private TokenContract Token { get; }

        public void AssociationJob()
        {
            ExecuteStandaloneTask(new Action[]
            {
                TransferToTester,
                CreateOrganization,
                TransferToVirtualAccount,
                CreateProposal,
                ApproveProposal,
                ReleaseProposal,
                CheckTheBalance
            });
        }

        // Create organization
        private void CreateOrganization()
        {
            Logger.Info("Create organization:");
            OrganizationList = new Dictionary<Address, Organization>();
            OrganizationMemberInfos = SetMemberLists();
            var txIdList = new Dictionary<CreateOrganizationInput, string>();
            var inputList = new List<CreateOrganizationInput>();

            foreach (var organizationMemberList in OrganizationMemberInfos)
            {
                var count = organizationMemberList.OrganizationMembers.Count;
                var approveCount = count == 1 ? 1 : count * 2 / 3;
                var creatOrganizationInput = new CreateOrganizationInput
                {
                    ProposalReleaseThreshold = new ProposalReleaseThreshold
                    {
                        MaximalAbstentionThreshold = count - approveCount,
                        MaximalRejectionThreshold = count - approveCount,
                        MinimalApprovalThreshold = approveCount,
                        MinimalVoteThreshold = count
                    },
                    ProposerWhiteList = new ProposerWhiteList
                    {
                        Proposers = {organizationMemberList.OrganizationMembers.First()}
                    },
                    OrganizationMemberList = organizationMemberList
                };
                inputList.Add(creatOrganizationInput);
            }

            foreach (var input in inputList)
            {
                var txId =
                    Association.ExecuteMethodWithTxId(AssociationMethod.CreateOrganization, input);
                txIdList.Add(input, txId);
            }

            foreach (var (key, value) in txIdList)
            {
                var result = Association.NodeManager.CheckTransactionResult(value);
                var status = result.Status.ConvertTransactionResultStatus();
                if (status != TransactionResultStatus.Mined)
                {
                    Logger.Error("Create organization address failed.");
                }
                else
                {
                    var organizationAddress =
                        Address.Parser.ParseFrom(ByteArrayHelper.HexStringToByteArray(result.ReturnValue));
                    var info = Association.GetOrganization(organizationAddress);
                    info.OrganizationAddress.ShouldBe(organizationAddress);
                    info.ProposalReleaseThreshold.MaximalAbstentionThreshold.ShouldBe(key.ProposalReleaseThreshold
                        .MaximalAbstentionThreshold);
                    info.ProposalReleaseThreshold.MaximalRejectionThreshold.ShouldBe(key.ProposalReleaseThreshold
                        .MaximalRejectionThreshold);
                    info.ProposalReleaseThreshold.MinimalApprovalThreshold.ShouldBe(key.ProposalReleaseThreshold
                        .MinimalApprovalThreshold);
                    info.ProposalReleaseThreshold.MinimalVoteThreshold.ShouldBe(key.ProposalReleaseThreshold
                        .MinimalVoteThreshold);
                    info.OrganizationMemberList.ShouldBe(key.OrganizationMemberList);
                    info.ProposerWhiteList.ShouldBe(key.ProposerWhiteList);
                    if (OrganizationList.ContainsKey(organizationAddress)) continue;
                    OrganizationList.Add(organizationAddress, info);
                }

                foreach (var (address, organization) in OrganizationList)
                {
                    Logger.Info($"AssociationAuth organization : {address}");
                    var members = organization.OrganizationMemberList.OrganizationMembers;
                    var proposer = organization.ProposerWhiteList.Proposers;
                    foreach (var member in members) Logger.Info($"Member is {member}");
                    foreach (var p in proposer) Logger.Info($"Proposer is {p}");
                }
            }
        }

        private void CreateProposal()
        {
            Logger.Info("Create Proposal: ");
            var txIdInfos = new Dictionary<KeyValuePair<Address, Organization>, List<string>>();
            ProposalList = new Dictionary<KeyValuePair<Address, Organization>, List<Hash>>();
            foreach (var organizationAddress in OrganizationList)
            {
                var balance = Token.GetUserBalance(organizationAddress.Key.GetFormatted(), Symbol);
                if (balance < 100 * OrganizationList.Count) continue;
                var txIdList = new List<string>();
                foreach (var toOrganizationAddress in OrganizationList)
                {
                    if (toOrganizationAddress.Equals(organizationAddress)) continue;
                    var transferInput = new TransferInput
                    {
                        To = toOrganizationAddress.Key,
                        Symbol = Symbol,
                        Amount = 100,
                        Memo = "virtual account transfer virtual account"
                    };

                    var createProposalInput = new CreateProposalInput
                    {
                        ToAddress = AddressHelper.Base58StringToAddress(Token.ContractAddress),
                        OrganizationAddress = organizationAddress.Key,
                        ContractMethodName = TokenMethod.Transfer.ToString(),
                        ExpiredTime = KernelHelper.GetUtcNow().AddHours(2),
                        Params = transferInput.ToByteString()
                    };

                    var sender = organizationAddress.Value.ProposerWhiteList.Proposers.First();
                    Association.SetAccount(sender.GetFormatted());
                    var txId = Association.ExecuteMethodWithTxId(AssociationMethod.CreateProposal,
                        createProposalInput);
                    txIdList.Add(txId);
                }

                txIdInfos.Add(organizationAddress, txIdList);
            }

            foreach (var (key, value) in txIdInfos)
            {
                var proposalIds = new List<Hash>();
                foreach (var txId in value)
                {
                    var result = Association.NodeManager.CheckTransactionResult(txId);
                    var status = result.Status.ConvertTransactionResultStatus();

                    if (status != TransactionResultStatus.Mined)
                    {
                        Logger.Error("Create proposal Failed.");
                    }
                    else
                    {
                        var proposal = Hash.Parser.ParseFrom(ByteArrayHelper.HexStringToByteArray(result.ReturnValue));
                        Logger.Info($"Create proposal {proposal} through organization address {key.Key}");
                        proposalIds.Add(proposal);
                    }
                }

                ProposalList.Add(key, proposalIds);
            }
        }

        private void ApproveProposal()
        {
            Logger.Info("Approve/Abstain/Reject proposal: ");
            var proposalApproveList = new Dictionary<Hash, List<ApproveInfo>>();
            foreach (var proposal in ProposalList)
            {
                var organization = proposal.Key.Key;
                Logger.Info($"Organization address: {organization}: ");
                var info = proposal.Key.Value;
                var minimalApprovalThreshold = info.ProposalReleaseThreshold.MinimalApprovalThreshold;
                var approveCount = minimalApprovalThreshold;
                var members = info.OrganizationMemberList.OrganizationMembers;

                foreach (var proposalId in proposal.Value)
                {
                    var approveTxInfos = new List<ApproveInfo>();
                    var approveMember = members.Take((int) approveCount).ToList();
                    foreach (var member in approveMember)
                    {
                        var txId = Association.Approve(proposalId, member.GetFormatted());
                        var approveInfo =
                            new ApproveInfo(nameof(AssociationMethod.Approve), member.GetFormatted(), txId);
                        approveTxInfos.Add(approveInfo);
                    }

                    var otherMembers = members.Where(m => !approveMember.Contains(m)).ToList();
                    if (otherMembers.Count == 0)
                    {
                        proposalApproveList.Add(proposalId, approveTxInfos);
                        continue;
                    }

                    var abstentionMember = otherMembers.First();
                    var abstentionTxId = Association.Abstain(proposalId, abstentionMember.GetFormatted());
                    var abstentionInfo =
                        new ApproveInfo(nameof(AssociationMethod.Abstain), abstentionMember.GetFormatted(),
                            abstentionTxId);
                    approveTxInfos.Add(abstentionInfo);

                    var rejectionMiners = otherMembers.Where(r => !abstentionMember.Equals(r)).ToList();
                    if (rejectionMiners.Count == 0)
                    {
                        proposalApproveList.Add(proposalId, approveTxInfos);
                        continue;
                    }

                    foreach (var rm in rejectionMiners)
                    {
                        var txId = Association.Reject(proposalId, rm.GetFormatted());
                        var rejectInfo = new ApproveInfo(nameof(AssociationMethod.Reject), rm.GetFormatted(), txId);
                        approveTxInfos.Add(rejectInfo);
                    }

                    proposalApproveList.Add(proposalId, approveTxInfos);
                }
            }

            foreach (var (key, value) in proposalApproveList)
            foreach (var proposalApprove in value)
            {
                var result = Association.NodeManager.CheckTransactionResult(proposalApprove.TxId);
                var status = result.Status.ConvertTransactionResultStatus();

                if (status != TransactionResultStatus.Mined)
                    Logger.Error($"{proposalApprove.Type} proposal Failed.");
                Logger.Info($"{proposalApprove.Account} {proposalApprove.Type} proposal {key} successful");
            }

            foreach (var (key, value) in proposalApproveList)
            {
                var proposalStatue = Association.CheckProposal(key);
                var approveCount = value.Count(a => a.Type.Equals("Approve"));
                var abstainCount = value.Count(a => a.Type.Equals("Abstain"));
                var rejectCount = value.Count(a => a.Type.Equals("Reject"));
                proposalStatue.AbstentionCount.ShouldBe(abstainCount);
                proposalStatue.RejectionCount.ShouldBe(rejectCount);
                proposalStatue.ApprovalCount.ShouldBe(approveCount);
                proposalStatue.ToBeReleased.ShouldBeTrue();
            }
        }

        private void ReleaseProposal()
        {
            Logger.Info("Release proposal: ");
            foreach (var (key, value) in ProposalList)
            {
                var sender = key.Value.ProposerWhiteList.Proposers.First();
                foreach (var proposalId in value)
                {
                    var toBeReleased = Association.CheckProposal(proposalId).ToBeReleased;
                    if (!toBeReleased) continue;
                    var balance = Token.GetUserBalance(key.Key.GetFormatted(), Symbol);
                    Association.SetAccount(sender.GetFormatted());
                    var result = Association.ExecuteMethodWithResult(AssociationMethod.Release, proposalId);
                    result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
                    var newBalance = Token.GetUserBalance(key.Key.GetFormatted(), Symbol);
                    newBalance.ShouldBe(balance - 100);
                }
            }
        }

        private void CheckTheBalance()
        {
            Logger.Info("After Association test, check the balance of organization address:");
            foreach (var balanceInfo in BalanceInfo)
            {
                var balance = Token.GetUserBalance(balanceInfo.Key.GetFormatted(), Symbol);
                balance.ShouldBe(balanceInfo.Value);
                Logger.Info($"{balanceInfo.Key} {Symbol} balance is {balance}");
            }

            Logger.Info("After Association test, check the balance of tester:");
            foreach (var tester in AssociationTester)
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
                var balance = Token.GetUserBalance(organization.Key.GetFormatted(), Symbol);
                if (balance >= 100_00000000)
                {
                    BalanceInfo.Add(organization.Key, balance);
                    continue;
                }

                Token.TransferBalance(InitAccount, organization.Key.GetFormatted(), 100_00000000, Symbol);
                balance = Token.GetUserBalance(organization.Key.GetFormatted(), Symbol);
                BalanceInfo.Add(organization.Key, balance);
                Logger.Info($"{organization.Key} {Symbol} token balance is {balance}");
            }
        }

        private List<OrganizationMemberList> SetMemberLists()
        {
            Logger.Info("Set members and check balance: ");
            var reviewerInfos = new List<OrganizationMemberList>();
            for (var i = 0; i < 10; i++)
            {
                var reviewers = new List<Address>();
                var membersCount = GenerateRandomNumber(1, AssociationTester.Count);
                for (var j = 0; j < membersCount; j++)
                {
                    var randomNo = GenerateRandomNumber(0, AssociationTester.Count - 1);
                    var account = AddressHelper.Base58StringToAddress(AssociationTester[randomNo]);
                    if (reviewers.Contains(account))
                        continue;
                    reviewers.Add(account);
                }

                var organizationMemberList = new OrganizationMemberList
                {
                    OrganizationMembers = {reviewers}
                };

                foreach (var reviewer in reviewers)
                {
                    var balance = Token.GetUserBalance(reviewer.GetFormatted(), TokenSymbol);
                    if (balance >= 100_00000000)
                        continue;
                    Token.TransferBalance(InitAccount, reviewer.GetFormatted(), 100_00000000, TokenSymbol);
                    balance = Token.GetUserBalance(reviewer.GetFormatted(), TokenSymbol);
                    Logger.Info($"{reviewer} {TokenSymbol} token balance is {balance}");
                }

                reviewerInfos.Add(organizationMemberList);
            }

            return reviewerInfos;
        }
    }
}