using System;
using System.Collections.Generic;
using System.Threading;
using Acs3;
using AElf.Client.Service;
using AElf.Contracts.MultiToken;
using AElf.Contracts.Referendum;
using AElfChain.Common;
using AElfChain.Common.Contracts;
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
        public ReferendumAuthContract NewReferendum;
        public ReferendumAuthContract Referendum;
        public string Symbol = NodeOption.NativeTokenSymbol;
        protected ContractTester Tester;
        public INodeManager NodeManager { get; set; }
        public AElfClient ApiClient { get; set; }
        public List<string> UserList { get; set; }

        public string InitAccount { get; } = "28Y8JA1i2cN6oHvdv7EraXJr9a1gY6D1PpJXw9QtRMRwKcBQMK";
        public string TestAccount { get; } = "28Y8JA1i2cN6oHvdv7EraXJr9a1gY6D1PpJXw9QtRMRwKcBQMK";

        private static string RpcUrl { get; } = "http://54.169.99.49:8000";

        [TestInitialize]
        public void Initialize()
        {
            #region Basic Preparation

            //Init Logger
            Log4NetHelper.LogInit("ParliamentTest_");

            #endregion

            NodeManager = new NodeManager(RpcUrl);
            ApiClient = NodeManager.ApiClient;
            var contractServices = new ContractServices(NodeManager, InitAccount, "Main");
            Tester = new ContractTester(contractServices);
            Referendum = Tester.ReferendumService;
            //DeployAndInitialize();
        }

        [TestMethod]
        public void CreateOrganization()
        {
            var result = Referendum.ExecuteMethodWithResult(ReferendumMethod.CreateOrganization,
                new CreateOrganizationInput
                {
                    ProposalReleaseThreshold = new ProposalReleaseThreshold(),
                    TokenSymbol = Symbol
                });
            var organizationAddress = result.ReadableReturnValue.Replace("\"", "");
            _logger.Info($"organization address is : {organizationAddress}");

            var organization =
                Referendum.CallViewMethod<Organization>(ReferendumMethod.GetOrganization,
                    AddressHelper.Base58StringToAddress(organizationAddress));

            Tester.TokenService.SetAccount(InitAccount);
            var transfer = Tester.TokenService.ExecuteMethodWithResult(TokenMethod.Transfer, new TransferInput
            {
                Symbol = Symbol,
                Amount = 1000,
                Memo = "transfer to Organization",
                To = AddressHelper.Base58StringToAddress(organizationAddress)
            });
        }
        //2pT1BzA5MRQ5oPzfH32WRWJXULgq5ZZB9yP9axh6sejPnw1dKd

        [TestMethod]
        [DataRow("2pT1BzA5MRQ5oPzfH32WRWJXULgq5ZZB9yP9axh6sejPnw1dKd")]
        public void CreateProposal(string organizationAddress)
        {
            var transferInput = new TransferInput
            {
                Symbol = Symbol,
                Amount = 100,
                To = AddressHelper.Base58StringToAddress(TestAccount),
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

            Referendum.SetAccount(TestAccount);
            var result =
                Referendum.ExecuteMethodWithResult(ReferendumMethod.CreateProposal,
                    createProposalInput);
            var proposal = result.ReadableReturnValue;
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
        [DataRow("d04236479e2d4e881208316117ca349abdea66beb80f6d4ed55e8eac52ec4939",
            "2ZYyxEH6j8zAyJjef6Spa99Jx2zf5GbFktyAQEBPWLCvuSAn8D")]
        public void Approve(string proposalId, string account)
        {
            var beforeBalance = Tester.TokenService.GetUserBalance(account, Symbol);
            _logger.Info($"{account} token balance is {beforeBalance}");

            Referendum.SetAccount(account);
            var result =
                Referendum.ExecuteMethodWithResult(ReferendumMethod.Approve,HashHelper.HexStringToHash(proposalId));
            _logger.Info($"Approve is {result.ReadableReturnValue}");

            var balance = Tester.TokenService.GetUserBalance(account, Symbol);
            _logger.Info($"{account} token balance is {balance}");
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

        private void DeployAndInitialize()
        {
            var authority = new AuthorityManager(NodeManager, InitAccount);
            var contractAddress =
                authority.DeployContractWithAuthority(InitAccount, "AElf.Contracts.ReferendumAuth.dll");

            Thread.Sleep(2000);

            _logger.Info($"{contractAddress.GetFormatted()}");
            NewReferendum = new ReferendumAuthContract(NodeManager, InitAccount, contractAddress.GetFormatted());
        }
    }
}