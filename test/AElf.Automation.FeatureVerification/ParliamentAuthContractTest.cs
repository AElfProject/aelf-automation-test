using System;
using System.Collections.Generic;
using System.Linq;
using Acs3;
using AElf.Client.Service;
using AElf.Contracts.MultiToken;
using AElf.Contracts.Parliament;
using AElf.Types;
using AElfChain.Common;
using AElfChain.Common.Contracts;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using log4net;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace AElf.Automation.Contracts.ScenarioTest
{
    [TestClass]
    public class ParliamentAuthContractTest
    {
        private static readonly ILog _logger = Log4NetHelper.GetLogger();
        public ParliamentContract Parliament;
        public string Symbol;
        public TokenContract Token;
        public INodeManager NodeManager { get; set; }
        public AElfClient ApiClient { get; set; }
        protected static int MinersCount { get; set; }
        protected List<string> Miners { get; set; }
        public string InitAccount { get; } = "28Y8JA1i2cN6oHvdv7EraXJr9a1gY6D1PpJXw9QtRMRwKcBQMK";
        public string TestAccount { get; } = "28Y8JA1i2cN6oHvdv7EraXJr9a1gY6D1PpJXw9QtRMRwKcBQMK";
        public string Full { get; } = "2V2UjHQGH8WT4TWnzebxnzo9uVboo67ZFbLjzJNTLrervAxnws";
        private static string RpcUrl { get; } = "http://192.168.197.14:8000";

        [TestInitialize]
        public void Initialize()
        {
            #region Basic Preparation

            //Init Logger
            Log4NetHelper.LogInit("ParliamentTest_");
            NodeInfoHelper.SetConfig("nodes-env1-main");

            #endregion

            NodeManager = new NodeManager(RpcUrl);
            ApiClient = NodeManager.ApiClient;
            var contractServices = new ContractManager(NodeManager, InitAccount);
            Parliament = contractServices.Parliament;
            Token = contractServices.Token;
            Symbol = contractServices.Token.GetPrimaryTokenSymbol();
            Miners = contractServices.Authority.GetCurrentMiners();
            MinersCount = Miners.Count;
        }

        [TestMethod]
        public void CreateOrganization()
        {
            var result = Parliament.ExecuteMethodWithResult(ParliamentMethod.CreateOrganization,
                new CreateOrganizationInput
                {
                    ProposalReleaseThreshold = new ProposalReleaseThreshold
                    {
                        MaximalAbstentionThreshold = 1000,
                        MaximalRejectionThreshold = 1000,
                        MinimalApprovalThreshold = 5000,
                        MinimalVoteThreshold = 6000
                    },
                    ProposerAuthorityRequired = true,
                    ParliamentMemberProposingAllowed = true
                });
            var organizationAddress =
                Address.Parser.ParseFrom(ByteArrayHelper.HexStringToByteArray(result.ReturnValue));
            _logger.Info($"organization address is : {organizationAddress}");

            var organization =
                Parliament.GetOrganization(organizationAddress);
            organization.ProposalReleaseThreshold.MaximalAbstentionThreshold.ShouldBe(1000);
            organization.ProposalReleaseThreshold.MaximalRejectionThreshold.ShouldBe(1000);
            organization.ProposalReleaseThreshold.MinimalApprovalThreshold.ShouldBe(5000);
            organization.ProposalReleaseThreshold.MinimalVoteThreshold.ShouldBe(6000);
            organization.ProposerAuthorityRequired.ShouldBeTrue();
            organization.ParliamentMemberProposingAllowed.ShouldBeTrue();
        }

        [TestMethod]
        [DataRow("4TbVeRPki6dQQWVoWHRibXKSPsLjYYgKc6sFnmUonFGewujEm",
            "ZuTnjdqwK8vNcyypzn34YXfCeM1c6yDTGfrKvJuwmWqnSePSm")]
        public void CreateProposal(string organizationAddress, string contractAddress)
        {
            Token.TransferBalance(InitAccount, organizationAddress, 1000, Symbol);
            var transferInput = new TransferInput
            {
                Symbol = Symbol,
                Amount = 100,
                To = TestAccount.ConvertAddress(),
                Memo = "Transfer"
            };
            var createProposalInput = new CreateProposalInput
            {
                ContractMethodName = nameof(TokenMethod.Transfer),
                ToAddress = Token.Contract,
                Params = transferInput.ToByteString(),
                ExpiredTime = DateTime.UtcNow.AddDays(1).ToTimestamp(),
                OrganizationAddress = organizationAddress.ConvertAddress()
            };

            Parliament.SetAccount(Miners.First());
            var result =
                Parliament.ExecuteMethodWithResult(ParliamentMethod.CreateProposal,
                    createProposalInput);
            var proposal = Hash.Parser.ParseFrom(ByteArrayHelper.HexStringToByteArray(result.ReturnValue));
            _logger.Info($"Proposal is : {proposal}");
        }

        [TestMethod]
        [DataRow("56dbad640a677142bbc057767c2ce43baff5efb5084ec89fb4bbae2b3920c19c")]
        public void GetProposal(string proposalId)
        {
            var result =
                Parliament.CallViewMethod<ProposalOutput>(ParliamentMethod.GetProposal,
                    Hash.LoadFromHex(proposalId));
            var toBeRelease = result.ToBeReleased;
            var time = result.ExpiredTime;
            var organizationAddress = result.OrganizationAddress;
            var contractMethodName = result.ContractMethodName;
            var toAddress = result.ToAddress;

            _logger.Info($"proposal is {toBeRelease}");
            _logger.Info($"proposal expired time is {time} ");
            _logger.Info($"proposal organization is {organizationAddress}");
            _logger.Info($"proposal method name is {contractMethodName}");
            _logger.Info($"proposal to address is {toAddress}");
            _logger.Info($"proposer is {result.Proposer}");
        }

        [TestMethod]
        [DataRow("0160c62bcdc9b616e742fb5ff9de67226fe25a4f9ae906287be6332c81ef5c02")]
        public void Approve(string proposalId)
        {
            foreach (var miner in Miners)
            {
                var balance = Token.GetUserBalance(miner, Symbol);
                _logger.Info($"{miner} balance is {balance}");
                if (balance <= 0)
                {
                    Token.SetAccount(InitAccount);
                    Token.TransferBalance(InitAccount, miner, 1000_0000000, Symbol);
                }

                Parliament.SetAccount(miner);
                var result =
                    Parliament.ExecuteMethodWithResult(ParliamentMethod.Approve, Hash.LoadFromHex(proposalId)
                    );
                result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            }
        }

        [TestMethod]
        [DataRow("0160c62bcdc9b616e742fb5ff9de67226fe25a4f9ae906287be6332c81ef5c02")]
        public void ApproveWithFullNode(string proposalId)
        {
            var balance = Token.GetUserBalance(Full, Symbol);
            if (balance <= 0)
            {
                Token.SetAccount(InitAccount);
                Token.TransferBalance(InitAccount, Full, 1000_0000000, Symbol);
            }

            Parliament.SetAccount(Full);
            var result =
                Parliament.ExecuteMethodWithResult(ParliamentMethod.Approve, Hash.LoadFromHex(proposalId)
                );
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
        }

        [TestMethod]
        [DataRow("0160c62bcdc9b616e742fb5ff9de67226fe25a4f9ae906287be6332c81ef5c02")]
        public void Release(string proposalId)
        {
            var proposalInfo =
                Parliament.CallViewMethod<ProposalOutput>(ParliamentMethod.GetProposal,
                    Hash.LoadFromHex(proposalId));
            Parliament.SetAccount(proposalInfo.Proposer.ToBase58());
            var result =
                Parliament.ExecuteMethodWithResult(ParliamentMethod.Release, Hash.LoadFromHex(proposalId));
            result.Status.ShouldBe("MINED");
        }

        [TestMethod]
        public void GetOrganization()
        {
            var info = Parliament.GetOrganization(
                "aeXhTqNwLWxCG6AzxwnYKrPMWRrzZBskW3HWVD9YREMx1rJxG".ConvertAddress());
            _logger.Info($"{info.ProposalReleaseThreshold.MaximalAbstentionThreshold}");
            _logger.Info($"{info.ProposalReleaseThreshold.MaximalRejectionThreshold}");
            _logger.Info($"{info.ProposalReleaseThreshold.MinimalApprovalThreshold}");
            _logger.Info($"{info.ProposalReleaseThreshold.MinimalVoteThreshold}");
        }
    }
}