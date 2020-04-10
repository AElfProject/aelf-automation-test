using System;
using System.Collections.Generic;
using Acs3;
using AElf.Client.Service;
using AElf.Contracts.MultiToken;
using AElf.Contracts.Referendum;
using AElf.Types;
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
    public class ReferendumAuthContractTest
    {
        private static readonly ILog _logger = Log4NetHelper.GetLogger();
        public ReferendumAuthContract Referendum;
        public string Symbol = "ELF";
        protected ContractTester Tester;
        public TokenContract Token;
        public INodeManager NodeManager { get; set; }
        public AElfClient ApiClient { get; set; }
        public List<string> UserList { get; set; }

        public string InitAccount { get; } = "2RCLmZQ2291xDwSbDEJR6nLhFJcMkyfrVTq1i1YxWC4SdY49a6";
        public string TestAccount { get; } = "2RCLmZQ2291xDwSbDEJR6nLhFJcMkyfrVTq1i1YxWC4SdY49a6";

        private static string RpcUrl { get; } = "http://192.168.197.14:8000";

        [TestInitialize]
        public void Initialize()
        {
            #region Basic Preparation

            //Init Logger
            Log4NetHelper.LogInit("ParliamentTest_");

            #endregion

            NodeManager = new NodeManager(RpcUrl);
            ApiClient = NodeManager.ApiClient;
            var contractServices = new ContractServices(NodeManager, InitAccount, "Side");
            Tester = new ContractTester(contractServices);
            Referendum = Tester.ReferendumService;
            Token = Tester.TokenService;
        }

        [TestMethod]
        public void CreateOrganization()
        {
            var result = Referendum.ExecuteMethodWithResult(ReferendumMethod.CreateOrganization,
                new CreateOrganizationInput
                {
                    TokenSymbol = Token.GetPrimaryTokenSymbol(),
                    ProposalReleaseThreshold = new ProposalReleaseThreshold
                    {
                        MaximalAbstentionThreshold = 100,
                        MinimalVoteThreshold = 1000,
                        MinimalApprovalThreshold = 1000,
                        MaximalRejectionThreshold = 100
                    },
                    ProposerWhiteList = new ProposerWhiteList
                    {
                        Proposers = {InitAccount.ConvertAddress()}
                    }
                });
            var returnValue = result.ReturnValue;
            var organizationAddress =
                Address.Parser.ParseFrom(ByteArrayHelper.HexStringToByteArray(returnValue));
            _logger.Info($"organization address is : {organizationAddress}");

            var organization =
                Referendum.CallViewMethod<Organization>(ReferendumMethod.GetOrganization,
                    organizationAddress);

            Tester.TokenService.SetAccount(InitAccount);
            var transfer = Tester.TokenService.ExecuteMethodWithResult(TokenMethod.Transfer, new TransferInput
            {
                Symbol = Symbol,
                Amount = 1000,
                Memo = "transfer to Organization",
                To = organization.OrganizationAddress
            });
        }
        //2pT1BzA5MRQ5oPzfH32WRWJXULgq5ZZB9yP9axh6sejPnw1dKd

        [TestMethod]
        [DataRow("2kUQH1eJBJ3jFkNnKeEp4Ezik4u6h3Wn2kVtGZG8UDjwiHjCxa")]
        public void CreateProposal(string organizationAddress)
        {
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
                ToAddress = AddressHelper.Base58StringToAddress(Tester.TokenService.ContractAddress),
                Params = transferInput.ToByteString(),
                ExpiredTime = DateTime.UtcNow.AddMinutes(60).ToTimestamp(),
                OrganizationAddress = AddressHelper.Base58StringToAddress(organizationAddress)
            };

            Referendum.SetAccount(InitAccount);
            var result =
                Referendum.ExecuteMethodWithResult(ReferendumMethod.CreateProposal,
                    createProposalInput);
            var returnValue = result.ReturnValue;
            var proposal = Hash.Parser.ParseFrom(ByteArrayHelper.HexStringToByteArray(returnValue));
            _logger.Info($"Proposal is : {proposal}");
        }


        [TestMethod]
        [DataRow("d04236479e2d4e881208316117ca349abdea66beb80f6d4ed55e8eac52ec4939")]
        public void GetProposal(string proposalId)
        {
            var result =
                Referendum.CallViewMethod<ProposalOutput>(ReferendumMethod.GetProposal,
                    HashHelper.HexStringToHash(proposalId));
            var toBeRelease = result.ToBeReleased;
            var time = result.ExpiredTime;

            _logger.Info($"proposal is {toBeRelease}");
            _logger.Info($"proposal expired time is {time} ");
        }

        [TestMethod]
        [DataRow("95fc268ccfdd5da9f173c93725aae35fa2fccb8eff0237065e0283eedfb28d65")]
        public void Approve(string proposalId)
        {
            var beforeBalance = Tester.TokenService.GetUserBalance(InitAccount, Token.GetPrimaryTokenSymbol());
            _logger.Info($"{InitAccount} token balance is {beforeBalance}");

            var approveResult = Token.ApproveToken(InitAccount, Referendum.ContractAddress, 1000,
                Token.GetPrimaryTokenSymbol());
            approveResult.Status.ShouldBe("MINED");
            Referendum.SetAccount(InitAccount);
            var result =
                Referendum.ExecuteMethodWithResult(ReferendumMethod.Approve, HashHelper.HexStringToHash(proposalId));
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var balance = Tester.TokenService.GetUserBalance(InitAccount, Token.GetPrimaryTokenSymbol());
            _logger.Info($"{InitAccount} token balance is {balance}");
        }

        [TestMethod]
        [DataRow("d04236479e2d4e881208316117ca349abdea66beb80f6d4ed55e8eac52ec4939")]
        public void Release(string proposalId)
        {
            Referendum.SetAccount(TestAccount);
            var result =
                Referendum.ExecuteMethodWithResult(ReferendumMethod.Release, HashHelper.HexStringToHash(proposalId));
            result.Status.ShouldBe("MINED");
        }

        [TestMethod]
        [DataRow("d04236479e2d4e881208316117ca349abdea66beb80f6d4ed55e8eac52ec4939",
            "2ZYyxEH6j8zAyJjef6Spa99Jx2zf5GbFktyAQEBPWLCvuSAn8D")]
        public void ReclaimVoteToken(string proposalId, string account)
        {
            var beforeBalance = Tester.TokenService.GetUserBalance(account, Symbol);
            _logger.Info($"{account} token balance is {beforeBalance}");

            Referendum.SetAccount(account);
            var result =
                Referendum.ExecuteMethodWithResult(ReferendumMethod.ReclaimVoteToken,
                    HashHelper.HexStringToHash(proposalId));
            result.Status.ShouldBe("MINED");

            var balance = Tester.TokenService.GetUserBalance(account, Symbol);
            _logger.Info($"{account} token balance is {balance}");
        }

        [TestMethod]
        [DataRow("2pT1BzA5MRQ5oPzfH32WRWJXULgq5ZZB9yP9axh6sejPnw1dKd")]
        public void GetBalance(string account)
        {
            var balance = Tester.TokenService.GetUserBalance(account, Symbol);
            _logger.Info($"organization {account} token balance is {balance}");
        }
    }
}