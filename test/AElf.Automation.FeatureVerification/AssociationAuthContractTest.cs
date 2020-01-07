using System;
using System.Collections.Generic;
using Acs3;
using AElfChain.Common;
using AElfChain.Common.Contracts;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using AElf.Contracts.Association;
using AElf.Contracts.MultiToken;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using log4net;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AElf.Automation.Contracts.ScenarioTest
{
    [TestClass]
    public class AssociationAuthContractTest
    {
        private static readonly ILog _logger = Log4NetHelper.GetLogger();
        public string Symbol = NodeOption.NativeTokenSymbol;
        protected ContractTester Tester;
        public INodeManager NodeManager { get; set; }
        public List<string> UserList { get; set; }

        public string InitAccount { get; } = "2RCLmZQ2291xDwSbDEJR6nLhFJcMkyfrVTq1i1YxWC4SdY49a6";

        public string ReviewAccount1 { get; } = "28Y8JA1i2cN6oHvdv7EraXJr9a1gY6D1PpJXw9QtRMRwKcBQMK";
        public string ReviewAccount2 { get; } = "28qLVdGMokanMAp9GwfEqiWnzzNifh8LS9as6mzJFX1gQBB823";
        public string ReviewAccount3 { get; } = "eFU9Quc8BsztYpEHKzbNtUpu9hGKgwGD2tyL13MqtFkbnAoCZ";

        private static string RpcUrl { get; } = "http://192.168.197.56:8001";

        [TestInitialize]
        public void Initialize()
        {
            #region Basic Preparation

            //Init Logger
            Log4NetHelper.LogInit("AssociationTest_");

            #endregion

            NodeManager = new NodeManager(RpcUrl);
            var contractServices = new ContractServices(NodeManager, InitAccount, "Main");
            Tester = new ContractTester(contractServices);
        }

        [TestMethod]
        public void CreateOrganization()
        {
            var result = Tester.AssociationService.ExecuteMethodWithResult(AssociationMethod.CreateOrganization,
                new CreateOrganizationInput
                {
                    ProposalReleaseThreshold = new ProposalReleaseThreshold(),
                    OrganizationMemberList = new OrganizationMemberList(),
                    ProposerWhiteList = new ProposerWhiteList()
                });
            var organizationAddress = result.ReadableReturnValue.Replace("\"", "");
            _logger.Info($"organization address is : {organizationAddress}");

            var organization =
                Tester.AssociationService.CallViewMethod<Organization>(AssociationMethod.GetOrganization,
                    AddressHelper.Base58StringToAddress(organizationAddress));
//            foreach (var reviewer in organization.Reviewers) _logger.Info($"organization review is : {reviewer}");

            Tester.TokenService.SetAccount(InitAccount);
            var transfer = Tester.TokenService.ExecuteMethodWithResult(TokenMethod.Transfer, new TransferInput
            {
                Symbol = Symbol,
                Amount = 1000,
                Memo = "transfer to Organization",
                To = AddressHelper.Base58StringToAddress(organizationAddress)
            });
        }

        [TestMethod]
        [DataRow("DCMn2iZ5VjDxg51wzpuJxcUDfarG1dnKwd4TngSH8TS2vJsE2")]
        public void GetOrganization(string organizationAddress)
        {
            var organization =
                Tester.AssociationService.CallViewMethod<Organization>(AssociationMethod.GetOrganization,
                    AddressHelper.Base58StringToAddress(organizationAddress));
//            foreach (var reviewer in organization.Reviewers) _logger.Info($"organization review is : {reviewer}");
        }

        [TestMethod]
        [DataRow("DCMn2iZ5VjDxg51wzpuJxcUDfarG1dnKwd4TngSH8TS2vJsE2")]
        public void CreateProposal(string organizationAddress)
        {
            var transferInput = new TransferInput
            {
                Symbol = Symbol,
                Amount = 100,
                To = AddressHelper.Base58StringToAddress(ReviewAccount1),
                Memo = "Transfer"
            };
            var createProposalInput = new CreateProposalInput
            {
                ContractMethodName = nameof(TokenMethod.Transfer),
                ToAddress = AddressHelper.Base58StringToAddress(Tester.TokenService.ContractAddress),
                Params = transferInput.ToByteString(),
                ExpiredTime = DateTime.UtcNow.AddDays(1).ToTimestamp(),
                OrganizationAddress = AddressHelper.Base58StringToAddress(organizationAddress)
            };

            Tester.AssociationService.SetAccount(ReviewAccount1);
            var result =
                Tester.AssociationService.ExecuteMethodWithResult(AssociationMethod.CreateProposal,
                    createProposalInput);
            var proposal = result.ReadableReturnValue;
            _logger.Info($"Proposal is : {proposal}");
        }

        [TestMethod]
        [DataRow("913a971647aaaf121ee2e1d71c27c0f25eb8877b76e4994b9ec90600e4ae8e24")]
        public void GetProposal(string proposalId)
        {
            var result =
                Tester.AssociationService.CallViewMethod<ProposalOutput>(AssociationMethod.GetProposal,
                    HashHelper.HexStringToHash(proposalId));
            var toBeRelease = result.ToBeReleased;

            _logger.Info($"proposal is {toBeRelease}");
        }

        [TestMethod]
        [DataRow("913a971647aaaf121ee2e1d71c27c0f25eb8877b76e4994b9ec90600e4ae8e24")]
        public void Approve(string proposalId)
        {
            Tester.AssociationService.SetAccount(ReviewAccount1);
            var result =
                Tester.AssociationService.ExecuteMethodWithResult(AssociationMethod.Approve, HashHelper.HexStringToHash(proposalId));
            _logger.Info($"Approve is {result.ReadableReturnValue}");
        }

        [TestMethod]
        [DataRow("913a971647aaaf121ee2e1d71c27c0f25eb8877b76e4994b9ec90600e4ae8e24")]
        public void Release(string proposalId)
        {
            Tester.AssociationService.SetAccount(ReviewAccount1);
            var result = Tester.AssociationService.ExecuteMethodWithResult(AssociationMethod.Release,
                HashHelper.HexStringToHash(proposalId));
            Assert.AreSame(result.Status, "MINED");
        }

        [TestMethod]
        [DataRow("DCMn2iZ5VjDxg51wzpuJxcUDfarG1dnKwd4TngSH8TS2vJsE2")]
        public void GetBalance(string account)
        {
            var balance = Tester.TokenService.GetUserBalance(account, Symbol);
            _logger.Info($"{account} balance is {balance}");
        }
    }
}