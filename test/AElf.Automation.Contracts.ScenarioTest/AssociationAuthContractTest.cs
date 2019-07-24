using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Acs3;
using AElf.Automation.Common.Contracts;
using AElf.Automation.Common.Helpers;
using AElf.Automation.Common.OptionManagers;
using AElf.Automation.Common.WebApi.Dto;
using AElf.Contracts.AssociationAuth;
using AElf.Contracts.MultiToken.Messages;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ApproveInput = Acs3.ApproveInput;

namespace AElf.Automation.Contracts.ScenarioTest
{
    [TestClass]
    public class AssociationAuthContractTest
    {
        private readonly ILogHelper _logger = LogHelper.GetLogHelper();
        protected ContractTester Tester;
        public WebApiHelper CH { get; set; }
        public List<string> UserList { get; set; }

        public string InitAccount { get; } = "2876Vk2deM5ZnaXr1Ns9eySMSjpuvd53XatHTc37JXeW6HjiPs";

        public string ReviewAccount1 { get; } = "2Nbe57CVJQDmqwBB5C4C382PvZbgaXgM37x1k5LfgDUt8XomgW";
        public string ReviewAccount2 { get; } = "2YXAMsMw66Q6tRxyKFfNcFXkb8gH7DgRq6PzYGsSyyrm42fQNz";
        public string ReviewAccount3 { get; } = "2Z6n3FKBDTmzSZTv875Zr1C8Prww3CtQ5ZsrFpVnSpc4QFPdrN";
        public List<Reviewer> ReviewerList { get; set; }

        private static string RpcUrl { get; } = "http://192.168.197.13:8000";

        [TestInitialize]
        public void Initialize()
        {
            CH = new WebApiHelper(RpcUrl,  CommonHelper.GetCurrentDataDir());
            var contractServices = new ContractServices(CH, InitAccount,"Main");
            Tester = new ContractTester(contractServices);

            #region Basic Preparation

            //Init Logger
            var logName = "ContractTest_" + DateTime.Now.ToString("MMddHHmmss") + ".log";
            var dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", logName);
            _logger.InitLogHelper(dir);

            #endregion

            ReviewerList = new List<Reviewer>();
            var review1 = new Reviewer {Address = Address.Parse(ReviewAccount1), Weight = 1};
            var review2 = new Reviewer {Address = Address.Parse(ReviewAccount2), Weight = 2};
            var review3 = new Reviewer {Address = Address.Parse(ReviewAccount3), Weight = 3};
            ReviewerList.Add(review1);
            ReviewerList.Add(review2);
            ReviewerList.Add(review3);
        }

        [TestMethod]
        public void CreateOrganization()
        {
            var result = Tester.AssociationService.ExecuteMethodWithResult(AssociationAuthMethod.CreateOrganization,
                new CreateOrganizationInput
                {
                    Reviewers = {ReviewerList[0], ReviewerList[1], ReviewerList[2]},
                    ReleaseThreshold = 3,
                    ProposerThreshold = 0
                });
            var txResult = result.InfoMsg as TransactionResultDto;
            var organizationAddress = txResult.ReadableReturnValue.Replace("\"", "");
            _logger.WriteInfo($"organization address is : {organizationAddress}");

            var organization =
                Tester.AssociationService.CallViewMethod<Organization>(AssociationAuthMethod.GetOrganization,
                    Address.Parse(organizationAddress));
            foreach (var reviewer in organization.Reviewers)
            {
                _logger.WriteInfo($"organization review is : {reviewer}");
            }

            Tester.TokenService.SetAccount(InitAccount);
            var transfer = Tester.TokenService.ExecuteMethodWithResult(TokenMethod.Transfer, new TransferInput
            {
                Symbol = "ELF",
                Amount = 1000,
                Memo = "transfer to Organization",
                To = Address.Parse(organizationAddress)
            });
        }

        [TestMethod]
        [DataRow("")]
        public void GetOrganization(string organizationAddress)
        {
            var organization =
                Tester.AssociationService.CallViewMethod<Organization>(AssociationAuthMethod.GetOrganization,
                    Address.Parse(organizationAddress));
            foreach (var reviewer in organization.Reviewers)
            {
                _logger.WriteInfo($"organization review is : {reviewer}");
            }
        }

        [TestMethod]
        [DataRow("2A1RKFfxeh2n7nZpcci6t8CcgbJMGz9a7WGpC94THpiTK3U7nG",
            "ywiz4RLJMTJaC9msxBPpaHALEZttUjMrwnhbEShrjhJ98GHAt")]
        public void CreateProposal(string account, string organizationAddress)
        {
            var _transferInput = new TransferInput()
            {
                Symbol = "ELF",
                Amount = 100,
                To = Address.Parse(account),
                Memo = "Transfer"
            };
            var _createProposalInput = new CreateProposalInput
            {
                ContractMethodName = nameof(TokenMethod.Transfer),
                ToAddress = Address.Parse(Tester.TokenService.ContractAddress),
                Params = _transferInput.ToByteString(),
                ExpiredTime = DateTime.UtcNow.AddDays(1).ToTimestamp(),
                OrganizationAddress = Address.Parse(organizationAddress)
            };


            var result =
                Tester.AssociationService.ExecuteMethodWithResult(AssociationAuthMethod.CreateProposal,
                    _createProposalInput);
            var txResult = result.InfoMsg as TransactionResultDto;
            var proposal = txResult.ReadableReturnValue;
            _logger.WriteInfo($"Proposal is : {proposal}");
        }

        [TestMethod]
        [DataRow("b9cc0db02d32e457533e5dc62ea0095045839aca26dbfc80361a8a00a96c6abe")]
        public void GetProposal(string proposalId)
        {
            var result =
                Tester.AssociationService.ExecuteMethodWithResult(AssociationAuthMethod.GetProposal,
                    Hash.LoadHex(proposalId));
            var txResult = result.InfoMsg as TransactionResultDto;

            _logger.WriteInfo($"proposal message is {txResult.ReadableReturnValue}");
        }

        [TestMethod]
        [DataRow("b9cc0db02d32e457533e5dc62ea0095045839aca26dbfc80361a8a00a96c6abe")]
        public void Approve(string proposalId)
        {
            Tester.AssociationService.SetAccount(ReviewAccount3);
            var result =
                Tester.AssociationService.ExecuteMethodWithResult(AssociationAuthMethod.Approve, new ApproveInput
                {
                    ProposalId = Hash.LoadHex(proposalId)
                });
            var resultDto = result.InfoMsg as TransactionResultDto;
            _logger.WriteInfo($"Approve is {resultDto.ReadableReturnValue}");
        }
    }
}