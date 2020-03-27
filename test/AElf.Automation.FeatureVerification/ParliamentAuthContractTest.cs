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
        public ParliamentAuthContract Parliament;
        public string Symbol;
        public TokenContract Token;
        public INodeManager NodeManager { get; set; }
        public AElfClient ApiClient { get; set; }
        protected static int MinersCount { get; set; }
        protected List<string> Miners { get; set; }
        public string InitAccount { get; } = "28Y8JA1i2cN6oHvdv7EraXJr9a1gY6D1PpJXw9QtRMRwKcBQMK";
        public string TestAccount { get; } = "28Y8JA1i2cN6oHvdv7EraXJr9a1gY6D1PpJXw9QtRMRwKcBQMK";
        public string Full { get; } = "2V2UjHQGH8WT4TWnzebxnzo9uVboo67ZFbLjzJNTLrervAxnws";
        private static string RpcUrl { get; } = "http://18.223.158.83:8000";

        [TestInitialize]
        public void Initialize()
        {
            #region Basic Preparation

            //Init Logger
            Log4NetHelper.LogInit("ParliamentTest_");
            NodeInfoHelper.SetConfig("nodes-online-test-main");

            #endregion

            NodeManager = new NodeManager(RpcUrl);
            ApiClient = NodeManager.ApiClient;
            var contractServices = new ContractManager(NodeManager, InitAccount);
            Parliament = contractServices.ParliamentAuth;
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
        [DataRow("f2437bef3e0ad87773c9d504a0de2dd088ee154c19a00ff40951c77b630cf8d4")]
        public void GetProposal(string proposalId)
        {
            var result =
                Parliament.CallViewMethod<ProposalOutput>(ParliamentMethod.GetProposal,
                    HashHelper.HexStringToHash(proposalId));
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
        }

        [TestMethod]
        [DataRow("d00bea66e43d59df5a73c272379fd91a2a6f918b2a3375629d5f39184dba5422",
            "ZuTnjdqwK8vNcyypzn34YXfCeM1c6yDTGfrKvJuwmWqnSePSm")]
        public void Approve(string proposalId, string contractAddress)
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
                    Parliament.ExecuteMethodWithResult(ParliamentMethod.Approve, HashHelper.HexStringToHash(proposalId)
                    );
                result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            }
        }

        [TestMethod]
        [DataRow("daa3215f3832e61b4360caebd976c97419644bae4af647645b0b8e33033fca5b",
            "F5d3S7YJhSLvcBWtGw6nJ6Rx64MBgK4RpdMt6EAEDcns36qYs")]
        public void ApproveWithFullNode(string proposalId, string contractAddress)
        {
            var balance = Token.GetUserBalance(Full, Symbol);
            if (balance <= 0)
            {
                Token.SetAccount(InitAccount);
                Token.TransferBalance(InitAccount, Full, 1000_0000000, Symbol);
            }

            Parliament.SetAccount(Full);
            var result =
                Parliament.ExecuteMethodWithResult(ParliamentMethod.Approve, HashHelper.HexStringToHash(proposalId)
                );
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
        }

        [TestMethod]
        [DataRow("d00bea66e43d59df5a73c272379fd91a2a6f918b2a3375629d5f39184dba5422",
            "ZuTnjdqwK8vNcyypzn34YXfCeM1c6yDTGfrKvJuwmWqnSePSm")]
        public void Release(string proposalId, string contractAddress)
        {
            Parliament.SetAccount(TestAccount);
            var result =
                Parliament.ExecuteMethodWithResult(ParliamentMethod.Release, HashHelper.HexStringToHash(proposalId));
            result.Status.ShouldBe("MINED");
        }

        [TestMethod]
        public void GetOrganization()
        {
            var info = Parliament.GetOrganization(
                AddressHelper.Base58StringToAddress("aeXhTqNwLWxCG6AzxwnYKrPMWRrzZBskW3HWVD9YREMx1rJxG"));
            _logger.Info($"{info.ProposalReleaseThreshold.MaximalAbstentionThreshold}");
            _logger.Info($"{info.ProposalReleaseThreshold.MaximalRejectionThreshold}");
            _logger.Info($"{info.ProposalReleaseThreshold.MinimalApprovalThreshold}");
            _logger.Info($"{info.ProposalReleaseThreshold.MinimalVoteThreshold}");
        }
    }
}