using System;
using System.Collections.Generic;
using System.IO;
using Acs3;
using AElf.Automation.Common.Contracts;
using AElf.Automation.Common.Helpers;
using AElf.Automation.Common.Managers;
using AElf.Contracts.AssociationAuth;
using AElf.Contracts.MultiToken;
using AElfChain.SDK.Models;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using log4net;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ApproveInput = Acs3.ApproveInput;

namespace AElf.Automation.Contracts.ScenarioTest
{
    [TestClass]
    public class AssociationAuthContractTest
    {
        private static readonly ILog _logger = Log4NetHelper.GetLogger();
        protected ContractTester Tester;
        public INodeManager CH { get; set; }
        public List<string> UserList { get; set; }

        public string InitAccount { get; } = "2876Vk2deM5ZnaXr1Ns9eySMSjpuvd53XatHTc37JXeW6HjiPs";

        public string ReviewAccount1 { get; } = "2a6MGBRVLPsy6pu4SVMWdQqHS5wvmkZv8oas9srGWHJk7GSJPV";
        public string ReviewAccount2 { get; } = "2cv45MBBUHjZqHva2JMfrGWiByyScNbEBjgwKoudWQzp6vX8QX";
        public string ReviewAccount3 { get; } = "2Dyh4ASm6z7CaJ1J1WyvMPe2sJx5TMBW8CMTKeVoTMJ3ugQi3P";
        public List<Reviewer> ReviewerList { get; set; }

        private static string RpcUrl { get; } = "http://192.168.197.56:8001";

        [TestInitialize]
        public void Initialize()
        {
            #region Basic Preparation

            //Init Logger
            Log4NetHelper.LogInit("AssociationTest_");

            #endregion
            
            CH = new WebApiHelper(RpcUrl, CommonHelper.GetCurrentDataDir());
            var contractServices = new ContractServices(CH, InitAccount, "Main");
            Tester = new ContractTester(contractServices);
            
            ReviewerList = new List<Reviewer>();
            var review1 = new Reviewer {Address = AddressHelper.Base58StringToAddress(ReviewAccount1), Weight = 1};
            var review2 = new Reviewer {Address = AddressHelper.Base58StringToAddress(ReviewAccount1), Weight = 2};
            var review3 = new Reviewer {Address = AddressHelper.Base58StringToAddress(ReviewAccount1), Weight = 3};
            ReviewerList.Add(review1);
            ReviewerList.Add(review2);
            ReviewerList.Add(review3);
        }

        [TestMethod]
        public void CreateOrganization()
        {
            var result = Tester.AssociationService.ExecuteMethodWithResult(AssociationMethod.CreateOrganization,
                new CreateOrganizationInput
                {
                    Reviewers = {ReviewerList[0], ReviewerList[1], ReviewerList[2]},
                    ReleaseThreshold = 3,
                    ProposerThreshold = 0
                });
            var organizationAddress = result.ReadableReturnValue.Replace("\"", "");
            _logger.Info($"organization address is : {organizationAddress}");

            var organization =
                Tester.AssociationService.CallViewMethod<Organization>(AssociationMethod.GetOrganization,
                    AddressHelper.Base58StringToAddress(organizationAddress));
            foreach (var reviewer in organization.Reviewers)
            {
                _logger.Info($"organization review is : {reviewer}");
            }

            Tester.TokenService.SetAccount(InitAccount);
            var transfer = Tester.TokenService.ExecuteMethodWithResult(TokenMethod.Transfer, new TransferInput
            {
                Symbol = "ELF",
                Amount = 1000,
                Memo = "transfer to Organization",
                To = AddressHelper.Base58StringToAddress(organizationAddress)
            });
        }

        [TestMethod]
        [DataRow("")]
        public void GetOrganization(string organizationAddress)
        {
            var organization =
                Tester.AssociationService.CallViewMethod<Organization>(AssociationMethod.GetOrganization,
                    AddressHelper.Base58StringToAddress(organizationAddress));
            foreach (var reviewer in organization.Reviewers)
            {
                _logger.Info($"organization review is : {reviewer}");
            }
        }

        [TestMethod]
        [DataRow("2876Vk2deM5ZnaXr1Ns9eySMSjpuvd53XatHTc37JXeW6HjiPs",
            "tospBuj9P6GAoutVZHnxVe8BqYmRFxgcumjwPncB1fTvAPvpu")]
        public void CreateProposal(string account, string organizationAddress)
        {
            var _transferInput = new TransferInput()
            {
                Symbol = "ELF",
                Amount = 100,
                To = AddressHelper.Base58StringToAddress(account),
                Memo = "Transfer"
            };
            var _createProposalInput = new CreateProposalInput
            {
                ContractMethodName = nameof(TokenMethod.Transfer),
                ToAddress = AddressHelper.Base58StringToAddress(Tester.TokenService.ContractAddress),
                Params = _transferInput.ToByteString(),
                ExpiredTime = DateTime.UtcNow.AddDays(1).ToTimestamp(),
                OrganizationAddress = AddressHelper.Base58StringToAddress(organizationAddress)
            };


            var result =
                Tester.AssociationService.ExecuteMethodWithResult(AssociationMethod.CreateProposal,
                    _createProposalInput);
            var proposal = result.ReadableReturnValue;
            _logger.Info($"Proposal is : {proposal}");
        }

        [TestMethod]
        [DataRow("81a350581513bb60219b178ff7b8ecf52398deaf460b46949c6a8fc45fb27e27")]
        public void GetProposal(string proposalId)
        {
            var result =
                Tester.AssociationService.CallViewMethod<ProposalOutput>(AssociationMethod.GetProposal,
                    HashHelper.HexStringToHash(proposalId));
            var toBeRelease = result.ToBeReleased;

            _logger.Info($"proposal is {toBeRelease}");
        }

        [TestMethod]
        [DataRow("81a350581513bb60219b178ff7b8ecf52398deaf460b46949c6a8fc45fb27e27")]
        public void Approve(string proposalId)
        {
            Tester.AssociationService.SetAccount(ReviewAccount1);
            var result =
                Tester.AssociationService.ExecuteMethodWithResult(AssociationMethod.Approve, new ApproveInput
                {
                    ProposalId = HashHelper.HexStringToHash(proposalId)
                });
            _logger.Info($"Approve is {result.ReadableReturnValue}");
        }
    }
}