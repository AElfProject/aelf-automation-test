using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Acs3;
using AElf.Automation.Common.Contracts;
using AElf.Contracts.MultiToken;
using AElf.Contracts.ReferendumAuth;
using AElf.Kernel;
using AElf.Sdk.CSharp;
using AElf.Types;
using AElfChain.SDK.Models;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using ApproveInput = Acs3.ApproveInput;

namespace AElf.Automation.ProposalTest
{
    public class ProposalReferendum : ProposalBase
    {
        private Dictionary<Address, long> OrganizationList { get; set; }
        private Dictionary<KeyValuePair<Address, long>, List<string>> ProposalList { get; set; }
        private List<string> ReleaseProposalList { get; set; }
        private List<string> ReleaseMinedProposal { get; set; }

        private List<VoterInfo> VoterInfos { get; set; }

        private ReferendumAuthContract Referendum { get; }
        private TokenContract Token { get; }

        public ProposalReferendum()
        {
            Initialize();
            Referendum = Services.ReferendumService;
            Token = Services.TokenService;
        }

        public void ReferendumJob()
        {
            ExecuteStandaloneTask(new Action[]
            {
                CreateOrganization,
                TransferToVoter,
                TransferToVirtualAccount,
                CreateProposal,
                ApproveProposal,
                ReleaseProposal,
                ReclaimVoteToken
            });
        }

        // Create organization
        private void CreateOrganization()
        {
            Logger.Info("Create organization:");
            OrganizationList = new Dictionary<Address, long>();
            var txIdList = new Dictionary<CreateOrganizationInput, string>();
            var inputList = new Dictionary<long, CreateOrganizationInput>();
            for (int i = 0; i < 4; i++)
            {
                var rd = GenerateRandomNumber(1, 1000);
                var createOrganizationInput = new CreateOrganizationInput
                {
                    ReleaseThreshold = rd,
                    TokenSymbol = NativeToken
                };
                inputList.Add(rd, createOrganizationInput);
            }

            foreach (var input in inputList)
            {
                var txId =
                    Referendum.ExecuteMethodWithTxId(ReferendumMethod.CreateOrganization, input.Value);
                txIdList.Add(input.Value, txId);
            }

            foreach (var (key, value) in txIdList)
            {
                var checkTime = 5;
                var result = Referendum.CheckTransactionResult(value);
                var status = result.Status.ConvertTransactionResultStatus();
                while (status == TransactionResultStatus.NotExisted && checkTime > 0)
                {
                    checkTime--;
                    Thread.Sleep(2000);
                }

                if (status != TransactionResultStatus.Mined)
                {
                    Logger.Error($"Create organization address failed. ReleaseThreshold is {key.ReleaseThreshold}");
                }
                else
                {
                    var organizationAddress =
                        AddressHelper.Base58StringToAddress(result.ReadableReturnValue.Replace("\"", ""));
                    if (OrganizationList.Keys.Contains(organizationAddress)) continue;
                    OrganizationList.Add(organizationAddress, key.ReleaseThreshold);
                }
            }

            foreach (var (key, value) in OrganizationList)
            {
                Logger.Info($"Referendum organization : {key}, ReleaseThreshold is {value}");
            }
        }

        private void CreateProposal()
        {
            Logger.Info("Create Proposal");
            var txIdInfos = new Dictionary<KeyValuePair<Address, long>, List<string>>();
            ProposalList = new Dictionary<KeyValuePair<Address, long>, List<string>>();
            foreach (var organizationAddress in OrganizationList)
            {
                var txIdList = new List<string>();
                foreach (var toOrganizationAddress in OrganizationList)
                {
                    if (toOrganizationAddress.Equals(organizationAddress)) continue;

                    var transferInput = new TransferInput
                    {
                        To = toOrganizationAddress.Key,
                        Symbol = NativeToken,
                        Amount = 100,
                        Memo = "virtual account transfer virtual account"
                    };

                    var createProposalInput = new CreateProposalInput()
                    {
                        ToAddress = AddressHelper.Base58StringToAddress(Token.ContractAddress),
                        OrganizationAddress = organizationAddress.Key,
                        ContractMethodName = TokenMethod.Transfer.ToString(),
                        ExpiredTime = TimestampHelper.GetUtcNow().AddMinutes(3),
                        Params = transferInput.ToByteString()
                    };

                    var txId = Referendum.ExecuteMethodWithTxId(ReferendumMethod.CreateProposal, createProposalInput);
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
                    var result = Referendum.CheckTransactionResult(txId);
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
                new Dictionary<KeyValuePair<Address, long>, Dictionary<string, Dictionary<string, VoterInfo>>>();
            ReleaseProposalList = new List<string>();
            VoterInfos = new List<VoterInfo>();

            foreach (var proposal in ProposalList)
            {
                var approveTxIds = new Dictionary<string, Dictionary<string, VoterInfo>>();
                foreach (var proposalId in proposal.Value)
                {
                    var txInfoList = new Dictionary<string, VoterInfo>();
                    foreach (var tester in Tester)
                    {
                        var total = 0;
                        var rd = GenerateRandomNumber(1, 1000);
                        Referendum.SetAccount(tester);
                        var txId = Referendum.ExecuteMethodWithTxId(ReferendumMethod.Approve, new ApproveInput
                        {
                            ProposalId = HashHelper.HexStringToHash(proposalId),
                            Quantity = rd
                        });
                        var voterInfo = new VoterInfo(tester, rd, proposalId);
                        VoterInfos.Add(voterInfo);
                        txInfoList.Add(txId, voterInfo);
                        total += rd;
                        if (total >= proposal.Key.Value)
                            break;
                    }

                    approveTxIds.Add(proposalId, txInfoList);
                }

                proposalApproveList.Add(proposal.Key, approveTxIds);
            }

            foreach (var (key, value) in proposalApproveList)
            {
                foreach (var proposalApprove in value)
                {
                    long approveMinedCount = 0;
                    foreach (var txInfo in proposalApprove.Value)
                    {
                        var checkTime = 5;
                        var result = Referendum.CheckTransactionResult(txInfo.Key);
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
                            approveMinedCount += txInfo.Value.Quantity;
                            Logger.Info($"{txInfo.Key} approve proposal {proposalApprove.Key} successful");
                        }
                    }

                    var expectedCount = key.Value;
                    if (approveMinedCount >= expectedCount)
                    {
                        ReleaseProposalList.Add(proposalApprove.Key);
                    }
                }
            }
        }

        private void ReleaseProposal()
        {
            Logger.Info("Release proposal: ");
            var releaseTxInfos = new Dictionary<string, string>();
            ReleaseMinedProposal = new List<string>();
            foreach (var proposalId in ReleaseProposalList)
            {
                var txId = Referendum.ExecuteMethodWithTxId(ReferendumMethod.Release,
                    HashHelper.HexStringToHash(proposalId));
                releaseTxInfos.Add(proposalId, txId);
            }

            foreach (var txInfo in releaseTxInfos)
            {
                var checkTime = 5;
                var result = Referendum.CheckTransactionResult(txInfo.Value);
                var status = result.Status.ConvertTransactionResultStatus();
                while (status == TransactionResultStatus.NotExisted && checkTime > 0)
                {
                    checkTime--;
                    Thread.Sleep(2000);
                }

                if (status != TransactionResultStatus.Mined)
                {
                    Logger.Error("Release proposal Failed.");
                }

                ReleaseMinedProposal.Add(txInfo.Key);
            }

            Logger.Info("Check the balance of organization address:");
            foreach (var organization in OrganizationList)
            {
                var balance = Token.GetUserBalance(organization.Key.GetFormatted(), Symbol);
                Logger.Info($"{organization.Key} balance is {balance}");
            }
        }

        private void ReclaimVoteToken()
        {
            Logger.Info("Waiting for reclaim:");
            Thread.Sleep(300000);
            Logger.Info("Reclaim token: ");

            var oldBalance = new Dictionary<string, long>();
            var newBalance = new Dictionary<string, long>();
            foreach (var voter in VoterInfos)
            {
                var balance = Token.GetUserBalance(voter.Voter, Symbol);
                oldBalance.Add(voter.Voter, balance);
                Logger.Info($"{voter.Voter} {Symbol} token balance is {balance}");
            }

            var txInfos = new Dictionary<string, string>();

            foreach (var voter in VoterInfos)
            {
                if (!ReleaseMinedProposal.Contains(voter.ProposalId)) continue;
                var txId = Referendum.ExecuteMethodWithTxId(ReferendumMethod.ReclaimVoteToken,
                    HashHelper.Base64ToHash(voter.ProposalId));
                txInfos.Add(voter.Voter, txId);
            }

            foreach (var txInfo in txInfos)
            {
                var checkTime = 5;
                var result = Referendum.CheckTransactionResult(txInfo.Value);
                var status = result.Status.ConvertTransactionResultStatus();
                while (status == TransactionResultStatus.NotExisted && checkTime > 0)
                {
                    checkTime--;
                    Thread.Sleep(2000);
                }

                if (status != TransactionResultStatus.Mined)
                {
                    Logger.Error("Reclaim token failed");
                }
                else
                {
                    var balance = Token.GetUserBalance(txInfo.Key, Symbol);
                    newBalance.Add(txInfo.Key, balance);
                    var token = balance - oldBalance[txInfo.Key];
                    var quantity = VoterInfos.Find(o => o.Voter.Equals(txInfo.Key)).Quantity;

                    Logger.Info(
                        $"{txInfo.Key} {Symbol} token balance is {balance}, vote quantity is {quantity}, reclaim token is {token}");
                }
            }
        }

        private void TransferToVirtualAccount()
        {
            foreach (var organization in OrganizationList)
            {
                var balance = Token.GetUserBalance(organization.Key.GetFormatted());
                while (balance == 0)
                {
                    Token.ExecuteMethodWithResult(TokenMethod.Transfer, new TransferInput
                    {
                        Symbol = Symbol,
                        To = organization.Key,
                        Amount = 10000,
                        Memo = "Transfer to organization address"
                    });

                    balance = Token.GetUserBalance(organization.Key.GetFormatted());
                }

                Logger.Info($"{organization.Key} {Symbol} balance is {balance}");
            }
        }

        private void TransferToVoter()
        {
            foreach (var tester in Tester)
            {
                var balance = Token.GetUserBalance(tester);
                while (balance < 10000)
                {
                    Token.ExecuteMethodWithResult(TokenMethod.Transfer, new TransferInput
                    {
                        Symbol = NativeToken,
                        To = AddressHelper.Base58StringToAddress(tester),
                        Amount = 10000,
                        Memo = "Transfer to organization address"
                    });

                    balance = Token.GetUserBalance(tester);
                }

                Logger.Info($"{tester} {NativeToken} token balance is {balance}");
            }
        }
    }
}