using System;
using System.Collections.Generic;
using System.Linq;
using Acs3;
using AElf.Contracts.Association;
using AElfChain.Common.Contracts;
using AElf.Contracts.MultiToken;
using AElf.Kernel;
using AElf.Sdk.CSharp;
using AElf.Types;
using AElfChain.Common.DtoExtension;
using Google.Protobuf;
using Shouldly;

namespace AElf.Automation.ProposalTest
{
    public class ProposalAssociation : ProposalBase
    {
        public ProposalAssociation()
        {
            Initialize();
            Association = Services.AssociationService;
            Token = Services.TokenService;
        }

        private Dictionary<Address,Organization> OrganizationList { get; set; }
        private Dictionary<KeyValuePair<Address,Organization>, List<Hash>> ProposalList { get; set; }
        private Dictionary<Hash, Address> ReleaseProposalList { get; set; }
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
                var approveCount = count * 2 / 3;
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
                        AddressHelper.Base58StringToAddress(result.ReadableReturnValue.Replace("\"", ""));
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
                    OrganizationList.Add(organizationAddress,info );
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
                var txIdList = new List<string>();
                foreach (var toOrganizationAddress in OrganizationList)
                {
                    if (toOrganizationAddress.Equals(organizationAddress)) continue;
                    var transferInput = new TransferInput
                    {
                        To = toOrganizationAddress.Key,
                        Symbol = Symbol,
                        Amount = 10,
                        Memo = "virtual account transfer virtual account"
                    };

                    var createProposalInput = new CreateProposalInput
                    {
                        ToAddress = AddressHelper.Base58StringToAddress(Token.ContractAddress),
                        OrganizationAddress = organizationAddress.Key,
                        ContractMethodName = TokenMethod.Transfer.ToString(),
                        ExpiredTime = TimestampHelper.GetUtcNow().AddHours(1),
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
                        var proposal = HashHelper.HexStringToHash(result.ReadableReturnValue.Replace("\"", ""));
                        Logger.Info($"Create proposal {proposal} through organization address {key.Key}");
                        proposalIds.Add(proposal);
                    }
                }
                ProposalList.Add(key, proposalIds);
            }
        }

        private void ApproveProposal()
        {
            Logger.Info("Approve proposal: ");
            var proposalApproveList = new Dictionary<Hash, List<ApproveInfo>>();
            ReleaseProposalList = new Dictionary<Hash, Address>();
            foreach (var proposal in ProposalList)
            {
                var organization = proposal.Key.Key;
                Logger.Info($"Organization address: {organization}: ");
                var info = proposal.Key.Value;
                var minimalApprovalThreshold = info.ProposalReleaseThreshold.MinimalApprovalThreshold;
                var approveTxInfos = new List<ApproveInfo>();
                var approveCount = minimalApprovalThreshold;
                var members = info.OrganizationMemberList.OrganizationMembers;
                
                foreach (var proposalId in proposal.Value)
                {
                    var approveMember = members.Take((int) approveCount).ToList();
                    foreach (var member in approveMember)
                    {
                        var txId = Association.Approve(proposalId, member.GetFormatted()).TransactionId;
                        var approveInfo = new ApproveInfo(nameof(ParliamentMethod.Approve), member.GetFormatted(), txId);
                        approveTxInfos.Add(approveInfo);
                    }

                    var otherMembers = members.Where(m => !approveMember.Contains(m)).ToList();
                    var abstentionMember = otherMembers.First();
                    var abstentionTxId = Association.Abstain(proposalId, abstentionMember.GetFormatted()).TransactionId;
                    var abstentionInfo =
                        new ApproveInfo(nameof(ParliamentMethod.Abstain), abstentionMember.GetFormatted(), abstentionTxId);
                    approveTxInfos.Add(abstentionInfo);

                    var rejectionMiners = otherMembers.Where(r => !abstentionMember.Equals(r)).ToList();
                    foreach (var rm in rejectionMiners)
                    {
                        var txId = Association.Reject(proposalId, rm.GetFormatted()).TransactionId;
                        var rejectInfo = new ApproveInfo(nameof(ParliamentMethod.Reject), rm.GetFormatted(), txId);
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
                var approveCount = value.Select(a => a.Type.Contains("Approve")).Count();
                var abstainCount = value.Select(a => a.Type.Contains("Abstain")).Count();
                var rejectCount = value.Select(a => a.Type.Contains("Reject")).Count();
                proposalStatue.AbstentionCount.ShouldBe(abstainCount);
                proposalStatue.RejectionCount.ShouldBe(rejectCount);
                proposalStatue.ApprovalCount.ShouldBe(approveCount);

                if (proposalStatue.ToBeReleased)
                    ReleaseProposalList.Add(key,proposalStatue.Proposer);
            }
        }

        private void ReleaseProposal()
        {
            Logger.Info("Release proposal: ");
            var releaseTxIds = new List<string>();
            foreach (var proposalId in ReleaseProposalList)
            {
                var sender = proposalId.Value;
                Association.SetAccount(sender.GetFormatted());
                var txId = Association.ExecuteMethodWithTxId(AssociationMethod.Release, proposalId.Key);
                releaseTxIds.Add(txId);
            }

            foreach (var txId in releaseTxIds)
            {
                var result = Association.NodeManager.CheckTransactionResult(txId);
                var status = result.Status.ConvertTransactionResultStatus();
                if (status != TransactionResultStatus.Mined) Logger.Error("Release proposal Failed.");
            }
        }

        private void CheckTheBalance()
        {
            Logger.Info("After Association test, check the balance of organization address:");
            foreach (var balanceInfo in BalanceInfo)
            {
                var balance = Token.GetUserBalance(balanceInfo.Key.GetFormatted(), Symbol);
                balance.ShouldBe(balanceInfo.Value+10);
                BalanceInfo[balanceInfo.Key] = balance;
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

                Token.ExecuteMethodWithResult(TokenMethod.Transfer, new TransferInput
                {
                    Symbol = Symbol,
                    To = organization.Key,
                    Amount = 100_00000000,
                    Memo = "Transfer to organization address"
                });

                balance = Token.GetUserBalance(organization.Key.GetFormatted(), Symbol);
                BalanceInfo.Add(organization.Key, balance);
                Logger.Info($"{organization} {Symbol} token balance is {balance}");
            }
        }

        private List<OrganizationMemberList> SetMemberLists()
        {
            Logger.Info("Set members: ");

            var reviewerInfos = new List<OrganizationMemberList>();
            for (var i = 0; i < 4; i++)
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
                reviewerInfos.Add(organizationMemberList);
            }

            return reviewerInfos;
        }
    }
}