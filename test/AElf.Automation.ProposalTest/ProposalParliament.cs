using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Acs3;
using AElfChain.Common.Contracts;
using AElf.Contracts.MultiToken;
using AElf.Contracts.ParliamentAuth;
using AElf.Kernel;
using AElf.Sdk.CSharp;
using AElf.Types;
using AElfChain.SDK.Models;
using Google.Protobuf;
using ApproveInput = Acs3.ApproveInput;

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

        private Dictionary<Address, int> OrganizationList { get; set; }
        private Dictionary<KeyValuePair<Address, int>, List<string>> ProposalList { get; set; }
        private List<string> ReleaseProposalList { get; set; }

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
            OrganizationList = new Dictionary<Address, int>();
            var txIdList = new Dictionary<CreateOrganizationInput, string>();
            var inputList = new Dictionary<int, CreateOrganizationInput>();

            Logger.Info("GetDefault organization address:");
            var address = Parliament.GetGenesisOwnerAddress();
            var addressInfo = Parliament.CallViewMethod<Organization>(ParliamentMethod.GetOrganization, address);
            OrganizationList.Add(address, addressInfo.ReleaseThreshold);

            for (var i = 1; i <= MinersCount; i++)
            {
                var createOrganizationInput = new CreateOrganizationInput
                {
                    ReleaseThreshold = 10000 / i
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
                var checkTime = 5;
                var result = Parliament.NodeManager.CheckTransactionResult(value);
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
                    OrganizationList.Add(organizationAddress, key.ReleaseThreshold);
                }
            }

            foreach (var (key, value) in OrganizationList)
                Logger.Info($"Parliament Organization : {key}, ReleaseThreshold is {value}");
        }

        private void CreateProposal()
        {
            Logger.Info("Create Proposal");
            var txIdInfos = new Dictionary<KeyValuePair<Address, int>, List<string>>();
            ProposalList = new Dictionary<KeyValuePair<Address, int>, List<string>>();
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

                    var txId = Parliament.ExecuteMethodWithTxId(ParliamentMethod.CreateProposal, createProposalInput);

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
                    var result = Parliament.NodeManager.CheckTransactionResult(txId);
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
                new Dictionary<KeyValuePair<Address, int>, Dictionary<string, Dictionary<string, string>>>();
            ReleaseProposalList = new List<string>();
            foreach (var proposal in ProposalList)
            {
                var approveTxIds = new Dictionary<string, Dictionary<string, string>>();
                foreach (var proposalId in proposal.Value)
                {
                    var txInfoList = new Dictionary<string, string>();
                    var approveCount = Math.Ceiling(MinersCount * proposal.Key.Value / (double) 10000);

                    var miners = Miners.Take((int) approveCount).ToList();

                    foreach (var miner in miners)
                    {
                        Parliament.SetAccount(miner);
                        var txId = Parliament.ExecuteMethodWithTxId(ParliamentMethod.Approve, new ApproveInput
                        {
                            ProposalId = HashHelper.HexStringToHash(proposalId)
                        });
                        txInfoList.Add(miner, txId);
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
                    var result = Parliament.NodeManager.CheckTransactionResult(txInfo.Value);
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
                        approveMinedCount++;
                        Logger.Info($"{txInfo.Key} approve proposal {proposalApprove.Key} successful");
                    }
                }

                var expectedCount = 10000 / key.Value;
                if (approveMinedCount >= expectedCount) ReleaseProposalList.Add(proposalApprove.Key);
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
                    HashHelper.HexStringToHash(proposalId));
                releaseTxIds.Add(txId);
            }

            foreach (var txId in releaseTxIds)
            {
                var checkTime = 5;
                var result = Parliament.NodeManager.CheckTransactionResult(txId);
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
            Logger.Info("After Parliament test, check the balance of organization address:");
            foreach (var organization in OrganizationList)
            {
                var balance = Token.GetUserBalance(organization.Key.GetFormatted(), Symbol);
                Logger.Info($"{organization.Key} {Symbol} balance is {balance}");
            }

            Logger.Info("After Parliament test, check the balance of tester:");
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
                if (balance >= 10_00000000) continue;
                Token.ExecuteMethodWithResult(TokenMethod.Transfer, new TransferInput
                {
                    Symbol = Symbol,
                    To = organization.Key,
                    Amount = 100_00000000,
                    Memo = "Transfer to organization address"
                });

                balance = Token.GetUserBalance(organization.Key.GetFormatted(), Symbol);
                Logger.Info($"{organization.Key} {Symbol} token balance is {balance}");
            }
        }
    }
}