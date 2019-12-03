using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Acs3;
using AElfChain.Common.Contracts;
using AElf.Contracts.MultiToken;
using AElf.Contracts.AssociationAuth;
using AElf.Kernel;
using AElf.Sdk.CSharp;
using AElf.Types;
using AElfChain.SDK.Models;
using Google.Protobuf;
using ApproveInput = Acs3.ApproveInput;

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

        private Dictionary<Address, ReviewerInfo> OrganizationList { get; set; }
        private Dictionary<KeyValuePair<Address, ReviewerInfo>, List<string>> ProposalList { get; set; }
        private Dictionary<string, string> ReleaseProposalList { get; set; }
        private List<ReviewerInfo> ReviewerInfos { get; set; }
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
            ReviewerInfos = SetReviewers();
            OrganizationList = new Dictionary<Address, ReviewerInfo>();
            var txIdList = new Dictionary<KeyValuePair<ReviewerInfo, CreateOrganizationInput>, string>();
            var inputList = new Dictionary<ReviewerInfo, CreateOrganizationInput>();

            foreach (var reviewerList in ReviewerInfos)
            {
                var releaseThreshold = reviewerList.TotalWeight - reviewerList.MaxWeight;
                if (releaseThreshold == 0 && reviewerList.MaxWeight > 1)
                    releaseThreshold = reviewerList.MaxWeight - 1;

                var createOrganizationInput = new CreateOrganizationInput
                {
                    ProposerThreshold = 0,
                    ReleaseThreshold = releaseThreshold
                };
                foreach (var reviewer in reviewerList.Reviewers) createOrganizationInput.Reviewers.Add(reviewer);

                inputList.Add(reviewerList, createOrganizationInput);
            }

            foreach (var input in inputList)
            {
                var txId =
                    Association.ExecuteMethodWithTxId(AssociationMethod.CreateOrganization, input.Value);
                txIdList.Add(input, txId);
            }

            foreach (var (key, value) in txIdList)
            {
                var checkTime = 5;
                var result = Association.NodeManager.CheckTransactionResult(value);
                var status = result.Status.ConvertTransactionResultStatus();
                while (status == TransactionResultStatus.NotExisted && checkTime > 0)
                {
                    checkTime--;
                    Thread.Sleep(2000);
                }

                if (status != TransactionResultStatus.Mined)
                {
                    Logger.Error("Create organization address failed.");
                }
                else
                {
                    var organizationAddress =
                        AddressHelper.Base58StringToAddress(result.ReadableReturnValue.Replace("\"", ""));
                    if (OrganizationList.ContainsKey(organizationAddress)) continue;
                    OrganizationList.Add(organizationAddress, key.Key);
                }
            }

            foreach (var (key, value) in OrganizationList)
            {
                Logger.Info($"AssociationAuth organization : {key}");
                foreach (var reviewer in value.Reviewers) Logger.Info($"Reviewer is {reviewer}");
            }
        }

        private void CreateProposal()
        {
            Logger.Info("Create Proposal: ");
            var txIdInfos = new Dictionary<KeyValuePair<Address, ReviewerInfo>, List<string>>();
            ProposalList = new Dictionary<KeyValuePair<Address, ReviewerInfo>, List<string>>();
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

                    var sender = organizationAddress.Value.Reviewers.FirstOrDefault();
                    Association.SetAccount(sender.Address.GetFormatted());
                    var txId = Association.ExecuteMethodWithTxId(AssociationMethod.CreateProposal,
                        createProposalInput);
                    txIdList.Add(txId);
                }

                txIdInfos.Add(organizationAddress, txIdList);
            }


            foreach (var (key, value) in txIdInfos)
            {
                var proposalIds = new List<string>();
                foreach (var txId in value)
                {
                    var checkTime = 5;
                    var result = Association.NodeManager.CheckTransactionResult(txId);
                    var status = result.Status.ConvertTransactionResultStatus();
                    while (status == TransactionResultStatus.NotExisted && checkTime > 0)
                    {
                        checkTime--;
                        Thread.Sleep(2000);
                    }

                    if (status != TransactionResultStatus.Mined)
                    {
                        Logger.Error("Create proposal Failed.");
                    }
                    else
                    {
                        var proposal = result.ReadableReturnValue.Replace("\"", "");
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
            var proposalApproveList =
                new Dictionary<KeyValuePair<Address, ReviewerInfo>, Dictionary<string, Dictionary<Reviewer, string>>>();
            ReleaseProposalList = new Dictionary<string, string>();
            foreach (var proposal in ProposalList)
            {
                Logger.Info($"Organization address: {proposal.Key.Key}: ");
                var approveTxIds = new Dictionary<string, Dictionary<Reviewer, string>>();
                foreach (var proposalId in proposal.Value)
                {
                    var txInfoList = new Dictionary<Reviewer, string>();
                    var approveCount = proposal.Key.Value.Reviewers.Count;
                    for (var i = 0; i < approveCount; i++)
                    {
                        var reviewer = proposal.Key.Value.Reviewers[i].Address.GetFormatted();
                        Association.SetAccount(reviewer);
                        var txId = Association.ExecuteMethodWithTxId(AssociationMethod.Approve,
                            new ApproveInput
                            {
                                ProposalId = HashHelper.HexStringToHash(proposalId)
                            });
                        txInfoList.Add(proposal.Key.Value.Reviewers[i], txId);
                    }

                    approveTxIds.Add(proposalId, txInfoList);
                }

                proposalApproveList.Add(proposal.Key, approveTxIds);
            }

            foreach (var (key, value) in proposalApproveList)
            foreach (var proposalApprove in value)
            {
                var approveMinedCount = 0;
                foreach (var txInfo in proposalApprove.Value)
                {
                    var checkTime = 5;
                    var result = Association.NodeManager.CheckTransactionResult(txInfo.Value);
                    var status = result.Status.ConvertTransactionResultStatus();
                    while (status == TransactionResultStatus.NotExisted && checkTime > 0)
                    {
                        checkTime--;
                        Thread.Sleep(2000);
                    }

                    if (status != TransactionResultStatus.Mined)
                    {
                        Logger.Error("Approve proposal Failed.");
                    }
                    else
                    {
                        approveMinedCount += txInfo.Key.Weight;
                        Logger.Info($"{txInfo.Key} approve proposal {proposalApprove.Key} successful");
                    }
                }

                var expectedReleaseThreshold = key.Value.TotalWeight - key.Value.MaxWeight;
                if (expectedReleaseThreshold == 0)
                    expectedReleaseThreshold = key.Value.MaxWeight;

                if (approveMinedCount <= expectedReleaseThreshold)
                {
                    Logger.Info($"Approve is not enough. {proposalApprove.Key} ");
                    continue;
                }

                var sender = key.Value.Reviewers.FirstOrDefault();
                ReleaseProposalList.Add(proposalApprove.Key, sender.Address.GetFormatted());
            }
        }

        private void ReleaseProposal()
        {
            Logger.Info("Release proposal: ");
            var releaseTxIds = new List<string>();
            foreach (var proposalId in ReleaseProposalList)
            {
                var sender = proposalId.Value;
                Association.SetAccount(sender);
                var txId = Association.ExecuteMethodWithTxId(AssociationMethod.Release,
                    HashHelper.HexStringToHash(proposalId.Key));
                releaseTxIds.Add(txId);
            }

            foreach (var txId in releaseTxIds)
            {
                var checkTime = 5;
                var result = Association.NodeManager.CheckTransactionResult(txId);
                var status = result.Status.ConvertTransactionResultStatus();
                while (status == TransactionResultStatus.NotExisted && checkTime > 0)
                {
                    checkTime--;
                    Thread.Sleep(2000);
                }

                if (status != TransactionResultStatus.Mined) Logger.Error("Release proposal Failed.");
            }
        }

        private void CheckTheBalance()
        {
            Logger.Info("After Association test, check the balance of organization address:");
            foreach (var organization in OrganizationList)
            {
                var balance = Token.GetUserBalance(organization.Key.GetFormatted(), Symbol);
                Logger.Info($"{organization.Key} {Symbol} balance is {balance}");
            }

            Logger.Info("After Association test, check the balance of tester:");
            foreach (var tester in Tester)
            {
                var balance = Token.GetUserBalance(tester, NativeToken);
                Logger.Info($"{tester} {NativeToken} balance is {balance}");
            }
        }

        private void TransferToVirtualAccount()
        {
            foreach (var organization in OrganizationList)
            {
                var balance = Token.GetUserBalance(organization.Key.GetFormatted(), Symbol);
                if (balance > 10_00000000) continue;
                Token.ExecuteMethodWithResult(TokenMethod.Transfer, new TransferInput
                {
                    Symbol = Symbol,
                    To = organization.Key,
                    Amount = 100_00000000,
                    Memo = "Transfer to organization address"
                });

                balance = Token.GetUserBalance(organization.Key.GetFormatted(), Symbol);
                Logger.Info($"{organization.Key} {Symbol} balance is {balance}");
            }
        }

        private List<ReviewerInfo> SetReviewers()
        {
            Logger.Info("Set reviewers: ");
            var reviewerInfos = new List<ReviewerInfo>();
            for (var i = 0; i < 4; i++)
            {
                var reviewers = new List<Reviewer>();
                for (var j = 0; j < 5; j++)
                {
                    var randomNo = GenerateRandomNumber(0, Tester.Count - 1);
                    var account = Tester[randomNo];
                    if (reviewers.Count != 0 && reviewers.Exists(o => o.Address.GetFormatted().Equals(account)))
                        continue;
                    var weight = GenerateRandomNumber(0, 3);
                    var reviewer = new Reviewer
                    {
                        Address = AddressHelper.Base58StringToAddress(account),
                        Weight = weight
                    };
                    reviewers.Add(reviewer);
                }

                Logger.Info("Check the weight");

                if (reviewers.Count > 1 && reviewers.FindAll(o => o.Weight.Equals(0)).Count == reviewers.Count)
                {
                    Logger.Info("All the reviewers weight is 0, reset the reviewer weight ");
                    foreach (var reviewer in reviewers)
                    {
                        var weight = GenerateRandomNumber(1, 3);
                        reviewer.Weight = weight;
                    }
                }

                var reviewerInfo = new ReviewerInfo(reviewers);
                foreach (var reviewer in reviewerInfo.Reviewers)
                    Logger.Info($"Reviewers is {reviewer.Address} weight is {reviewer.Weight}");

                reviewerInfos.Add(reviewerInfo);
            }

            return reviewerInfos;
        }
    }
}