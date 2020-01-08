using System;
using System.Collections.Generic;
using System.Linq;
using Acs3;
using AElfChain.Common.Contracts;
using AElf.Contracts.MultiToken;
using AElf.Contracts.Parliament;
using AElf.Kernel;
using AElf.Sdk.CSharp;
using AElf.Types;
using AElfChain.Common.DtoExtension;
using Google.Protobuf;
using Shouldly;

namespace AElf.Automation.ProposalTest
{
    public class ProposalParliament : ProposalBase
    {
        public ProposalParliament()
        {
            Initialize();
            GetMiners();
            Parliament = Services.ParliamentService;
            Token = Services.TokenService;
        }

        private List<Address> OrganizationList { get; set; }
        private Dictionary<Address, List<Hash>> ProposalList { get; set; }
        private List<Hash> ReleaseProposalList { get; set; }
        private Dictionary<Address,long> BalanceInfo { get; set; }
        private ParliamentAuthContract Parliament { get; }
        private TokenContract Token { get; }

        public void ParliamentJob()
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
            OrganizationList = new List<Address>();
            var txIdList = new Dictionary<CreateOrganizationInput, string>();
            var inputList = new Dictionary<int, CreateOrganizationInput>();

            Logger.Info("GetDefault organization address:");
            var address = Parliament.GetGenesisOwnerAddress();
            OrganizationList.Add(address);

            for (var i = 1; i <= MinersCount; i++)
            {
                var a = 10000 / MinersCount;
                var createOrganizationInput = new CreateOrganizationInput
                {
                    ProposalReleaseThreshold = new ProposalReleaseThreshold
                    {
                        MaximalAbstentionThreshold = 10000 - a * i,
                        MaximalRejectionThreshold = 10000 - a * i,
                        MinimalApprovalThreshold = a * i,
                        MinimalVoteThreshold = 10000
                    },
                    ProposerAuthorityRequired = true,
                    ParliamentMemberProposingAllowed = true
                };
                inputList.Add(i, createOrganizationInput);
            }

            foreach (var input in inputList)
            {
                var txId =
                    Parliament.ExecuteMethodWithTxId(ParliamentMethod.CreateOrganization, input.Value);
                txIdList.Add(input.Value, txId);
            }

            foreach (var (key, value) in txIdList)
            {
                var result = Parliament.NodeManager.CheckTransactionResult(value);
                var status = result.Status.ConvertTransactionResultStatus();
                if (status != TransactionResultStatus.Mined)
                {
                    Logger.Error(
                        $"Create organization address failed. organization input is {key.ProposalReleaseThreshold}");
                }
                else
                {
                    var organizationAddress =
                        AddressHelper.Base58StringToAddress(result.ReadableReturnValue.Replace("\"", ""));
                    var info = Parliament.GetOrganization(organizationAddress);
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
                }
            }

            foreach (var organization in OrganizationList)
                Logger.Info($"ParliamentAuth Organization : {organization}");
        }

        private void CreateProposal()
        {
            Logger.Info("Create Proposal");
            var txIdInfos = new Dictionary<Address, List<string>>();
            ProposalList = new Dictionary<Address, List<Hash>>();
            foreach (var organizationAddress in OrganizationList)
            {
                var txIdList = new List<string>();
                foreach (var toOrganizationAddress in OrganizationList)
                {
                    if (toOrganizationAddress.Equals(organizationAddress)) continue;
                    var transferInput = new TransferInput
                    {
                        To = toOrganizationAddress,
                        Symbol = Symbol,
                        Amount = 10,
                        Memo = "virtual account transfer virtual account"
                    };

                    var createProposalInput = new CreateProposalInput
                    {
                        ToAddress = AddressHelper.Base58StringToAddress(Token.ContractAddress),
                        OrganizationAddress = organizationAddress,
                        ContractMethodName = TokenMethod.Transfer.ToString(),
                        ExpiredTime = TimestampHelper.GetUtcNow().AddHours(1),
                        Params = transferInput.ToByteString()
                    };

                    var txId = Parliament.ExecuteMethodWithTxId(ParliamentMethod.CreateProposal, createProposalInput);

                    txIdList.Add(txId);
                }

                txIdInfos.Add(organizationAddress, txIdList);
            }

            foreach (var (key, value) in txIdInfos)
            {
                var proposalIds = new List<Hash>();
                foreach (var txId in value)
                {
                    var result = Parliament.NodeManager.CheckTransactionResult(txId);
                    var status = result.Status.ConvertTransactionResultStatus();

                    if (status != TransactionResultStatus.Mined)
                    {
                        Logger.Error("Create proposal Failed.");
                    }
                    else
                    {
                        var proposal = result.ReadableReturnValue.Replace("\"", "");
                        Logger.Info($"Create proposal {proposal} through organization address {key}");
                        proposalIds.Add(HashHelper.HexStringToHash(proposal));
                    }
                }

                ProposalList.Add(key, proposalIds);
            }
        }

        private void ApproveProposal()
        {
            Logger.Info("Approve/Abstain/Reject proposal: ");
            var proposalApproveList = new Dictionary<Hash, List<ApproveInfo>>();
            ReleaseProposalList = new List<Hash>();
            foreach (var proposal in ProposalList)
            {
                var organization = proposal.Key;
                var info = Parliament.GetOrganization(organization);
                var minimalApprovalThreshold = info.ProposalReleaseThreshold.MinimalApprovalThreshold;
                var approveTxIds = new List<ApproveInfo>();
                var approveCount = Math.Ceiling(MinersCount * minimalApprovalThreshold / (double) 10000);

                foreach (var proposalId in proposal.Value)
                {
                    var miners = Miners.Take((int) approveCount).ToList();
                    foreach (var miner in miners)
                    {
                        var txId = Parliament.Approve(proposalId, miner).TransactionId;
                        var approveInfo = new ApproveInfo(nameof(ParliamentMethod.Approve), miner, txId);
                        approveTxIds.Add(approveInfo);
                    }

                    var otherMiners = Miners.Where(m => !miners.Contains(m)).ToList();
                    var abstentionMiner = otherMiners.First();
                    var abstentionTxId = Parliament.Abstain(proposalId, abstentionMiner).TransactionId;
                    var abstentionInfo =
                        new ApproveInfo(nameof(ParliamentMethod.Abstain), abstentionMiner, abstentionTxId);
                    approveTxIds.Add(abstentionInfo);

                    var rejectionMiners = otherMiners.Where(r => !abstentionMiner.Contains(r)).ToList();
                    foreach (var rm in rejectionMiners)
                    {
                        var txId = Parliament.Reject(proposalId, rm).TransactionId;
                        var rejectInfo = new ApproveInfo(nameof(ParliamentMethod.Reject), rm, txId);
                        approveTxIds.Add(rejectInfo);
                    }

                    proposalApproveList.Add(proposalId, approveTxIds);
                }
            }

            foreach (var (key, value) in proposalApproveList)
            foreach (var proposalApprove in value)
            {
                var result = Parliament.NodeManager.CheckTransactionResult(proposalApprove.TxId);
                var status = result.Status.ConvertTransactionResultStatus();

                if (status != TransactionResultStatus.Mined)
                    Logger.Error($"{proposalApprove.Type} proposal Failed.");
                Logger.Info($"{proposalApprove.Account} {proposalApprove.Type} proposal {key} successful");
            }

            foreach (var (key, value) in proposalApproveList)
            {
                var proposalStatue = Parliament.CheckProposal(key);
                var approveCount = value.Select(a => a.Type.Contains("Approve")).Count();
                var abstainCount = value.Select(a => a.Type.Contains("Abstain")).Count();
                var rejectCount = value.Select(a => a.Type.Contains("Reject")).Count();
                proposalStatue.AbstentionCount.ShouldBe(abstainCount);
                proposalStatue.RejectionCount.ShouldBe(rejectCount);
                proposalStatue.ApprovalCount.ShouldBe(approveCount);

                if (proposalStatue.ToBeReleased)
                    ReleaseProposalList.Add(key);
            }
        }

        private void ReleaseProposal()
        {
            Logger.Info("Release proposal: ");
            var releaseTxIds = new List<string>();
            foreach (var proposalId in ReleaseProposalList)
            {
                Parliament.SetAccount(InitAccount);
                var txId = Parliament.ExecuteMethodWithTxId(ParliamentMethod.Release,
                    proposalId);
                releaseTxIds.Add(txId);
            }

            foreach (var txId in releaseTxIds)
            {
                var result = Parliament.NodeManager.CheckTransactionResult(txId);
                var status = result.Status.ConvertTransactionResultStatus();
                if (status != TransactionResultStatus.Mined) Logger.Error("Release proposal Failed.");
            }
        }

        private void CheckTheBalance()
        {
            Logger.Info("After ParliamentAuth test, check the balance of organization address:");
            foreach (var balanceInfo in BalanceInfo)
            {
                var balance = Token.GetUserBalance(balanceInfo.Key.GetFormatted(), Symbol);
                balance.ShouldBe(balanceInfo.Value+10);
                BalanceInfo[balanceInfo.Key] = balance;
                Logger.Info($"{balanceInfo.Key} {Symbol} balance is {balance}");
            }

            Logger.Info("After ParliamentAuth test, check the balance of tester:");
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
                if (balance >= 100_00000000)
                {
                    BalanceInfo.Add(organization,balance);
                    continue;
                }
                Token.ExecuteMethodWithResult(TokenMethod.Transfer, new TransferInput
                {
                    Symbol = Symbol,
                    To = organization,
                    Amount = 100_00000000,
                    Memo = "Transfer to organization address"
                });

                balance = Token.GetUserBalance(organization.GetFormatted(), Symbol);
                BalanceInfo.Add(organization,balance);
                Logger.Info($"{organization} {Symbol} token balance is {balance}");
            }
        }
    }
}