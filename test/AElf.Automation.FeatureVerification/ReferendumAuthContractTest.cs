using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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
        private static readonly ILog Logger = Log4NetHelper.GetLogger();
        public ReferendumContract Referendum;
        public ReferendumContractContainer.ReferendumContractStub ReferendumStub;

        public string Symbol = "ELF";
        protected ContractTester Tester;
        public TokenContract Token;
        public INodeManager NodeManager { get; set; }
        public AElfClient ApiClient { get; set; }
        public List<string> UserList { get; set; }

        public string InitAccount { get; } = "28Y8JA1i2cN6oHvdv7EraXJr9a1gY6D1PpJXw9QtRMRwKcBQMK";
        public string TestAccount { get; } = "2RCLmZQ2291xDwSbDEJR6nLhFJcMkyfrVTq1i1YxWC4SdY49a6";
        public string OtherAccount { get; } = "2frDVeV6VxUozNqcFbgoxruyqCRAuSyXyfCaov6bYWc7Gkxkh2";
        
        private static string RpcUrl { get; } = "http://192.168.197.21:8000";

        [TestInitialize]
        public void Initialize()
        {
            #region Basic Preparation

            //Init Logger
            Log4NetHelper.LogInit("ReferendumTest_");

            #endregion

            NodeManager = new NodeManager(RpcUrl);
            ApiClient = NodeManager.ApiClient;
            var contractServices = new ContractServices(NodeManager, InitAccount, "Main");
            Tester = new ContractTester(contractServices);
            ReferendumStub = Tester.GenesisService.GetReferendumAuthStub();
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
            Logger.Info($"organization address is : {organizationAddress}");

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
                ToAddress = Tester.TokenService.Contract,
                Params = transferInput.ToByteString(),
                ExpiredTime = DateTime.UtcNow.AddMinutes(60).ToTimestamp(),
                OrganizationAddress = organizationAddress.ConvertAddress()
            };

            Referendum.SetAccount(TestAccount);
            var result =
                Referendum.ExecuteMethodWithResult(ReferendumMethod.CreateProposal,
                    createProposalInput);
            var returnValue = result.ReturnValue;
            var proposal = Hash.Parser.ParseFrom(ByteArrayHelper.HexStringToByteArray(returnValue));
            Logger.Info($"Proposal is : {proposal}");
        }
        

        [TestMethod]
        [DataRow("d04236479e2d4e881208316117ca349abdea66beb80f6d4ed55e8eac52ec4939")]
        public void GetProposal(string proposalId)
        {
            var result =
                Referendum.CallViewMethod<ProposalOutput>(ReferendumMethod.GetProposal,
                    Hash.LoadFromHex(proposalId));
            var toBeRelease = result.ToBeReleased;
            var time = result.ExpiredTime;

            Logger.Info($"proposal is {toBeRelease}");
            Logger.Info($"proposal expired time is {time} ");
        }
        
        [TestMethod]
        [DataRow("e186a023924b31354f1e77c9898842adadaec732a3f613a534d7fff709fd27fe")]
        public async Task GetVirtualAddress(string proposalId)
        {
            var result = await ReferendumStub.GetProposalVirtualAddress.CallAsync(Hash.LoadFromHex(proposalId));
            
            Logger.Info($"proposal virtual address is: {result} ");
        }
        
        [TestMethod]
        [DataRow("8ebea60abf387423954df9dc78e8562818108bb47fdd05e88e6350f7813d1a7e")]
        public async Task Approve(string proposalId)
        {
            var beforeBalance = Tester.TokenService.GetUserBalance(InitAccount, Token.GetPrimaryTokenSymbol());
            if (beforeBalance < 1000_00000000)
            {
                Tester.TokenService.TransferBalance(InitAccount, TestAccount, 1000_00000000);
                beforeBalance = Tester.TokenService.GetUserBalance(TestAccount, Token.GetPrimaryTokenSymbol());
            }
            Logger.Info($"{InitAccount} token balance is {beforeBalance}");
            var virtualAddress = await ReferendumStub.GetProposalVirtualAddress.CallAsync(Hash.LoadFromHex(proposalId));
            var approveResult = Token.ApproveToken(InitAccount, virtualAddress.ToBase58(), 1000,
                Token.GetPrimaryTokenSymbol());
            approveResult.Status.ShouldBe("MINED");
            var approveFee = approveResult.GetDefaultTransactionFee();
            
            Referendum.SetAccount(InitAccount);
            var result =
                Referendum.ExecuteMethodWithResult(ReferendumMethod.Approve, Hash.LoadFromHex(proposalId));
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var fee = result.GetDefaultTransactionFee();
            var balance = Tester.TokenService.GetUserBalance(InitAccount, Token.GetPrimaryTokenSymbol());
            balance.ShouldBe(beforeBalance - 1000 - fee - approveFee);
            Logger.Info($"{InitAccount} token balance is {balance}");

            // var virtualBalance = Tester.TokenService.GetUserBalance(Referendum.ContractAddress);
            // virtualBalance.ShouldBe(1000);
        }
        
        [TestMethod]
        [DataRow("87aa359c9193d4cc4e62fe626200fd88ce646455ec801b967e4048f25041b477")]
        public async Task Abstain(string proposalId)
        {
            var beforeBalance = Tester.TokenService.GetUserBalance(TestAccount, Token.GetPrimaryTokenSymbol());
            if (beforeBalance < 1000_00000000)
            {
                Tester.TokenService.TransferBalance(InitAccount, TestAccount, 1000_00000000);
                beforeBalance = Tester.TokenService.GetUserBalance(TestAccount, Token.GetPrimaryTokenSymbol());
            }
            Logger.Info($"{TestAccount} token balance is {beforeBalance}");
            var virtualAddress = await ReferendumStub.GetProposalVirtualAddress.CallAsync(Hash.LoadFromHex(proposalId));
            var approveResult = Token.ApproveToken(TestAccount,virtualAddress.ToBase58(), 50,
                Token.GetPrimaryTokenSymbol());
            approveResult.Status.ShouldBe("MINED");
            var approveFee = approveResult.GetDefaultTransactionFee();
            
            Referendum.SetAccount(TestAccount);
            var result =
                Referendum.ExecuteMethodWithResult(ReferendumMethod.Abstain, Hash.LoadFromHex(proposalId));
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var fee = result.GetDefaultTransactionFee();
            var balance = Tester.TokenService.GetUserBalance(TestAccount, Token.GetPrimaryTokenSymbol());
            balance.ShouldBe(beforeBalance - 50 - fee - approveFee);
            Logger.Info($"{TestAccount} token balance is {balance}");
        }
        
        [TestMethod]
        [DataRow("f903cde6f2e8892545e9a9fe182f26db288443f47f8cda430960f7bd42d92492")]
        public async Task Reject(string proposalId)
        {
            var beforeBalance = Tester.TokenService.GetUserBalance(OtherAccount, Token.GetPrimaryTokenSymbol());
            if (beforeBalance < 1000_00000000)
            {
                Tester.TokenService.TransferBalance(InitAccount, OtherAccount, 1000_00000000);
                beforeBalance = Tester.TokenService.GetUserBalance(OtherAccount, Token.GetPrimaryTokenSymbol());
            }
            Logger.Info($"{OtherAccount} token balance is {beforeBalance}");
            var virtualAddress = await ReferendumStub.GetProposalVirtualAddress.CallAsync(Hash.LoadFromHex(proposalId));
            var approveResult = Token.ApproveToken(OtherAccount, virtualAddress.ToBase58(), 50,
                Token.GetPrimaryTokenSymbol());
            approveResult.Status.ShouldBe("MINED");
            var approveFee = approveResult.GetDefaultTransactionFee();
            
            Referendum.SetAccount(OtherAccount);
            var result =
                Referendum.ExecuteMethodWithResult(ReferendumMethod.Reject, Hash.LoadFromHex(proposalId));
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var fee = result.GetDefaultTransactionFee();
            var balance = Tester.TokenService.GetUserBalance(OtherAccount, Token.GetPrimaryTokenSymbol());
            balance.ShouldBe(beforeBalance - 50 - fee - approveFee);
            Logger.Info($"{OtherAccount} token balance is {balance}");
        }

        [TestMethod]
        [DataRow("d04236479e2d4e881208316117ca349abdea66beb80f6d4ed55e8eac52ec4939")]
        public void Release(string proposalId)
        {
            Referendum.SetAccount(InitAccount);
            var result =
                Referendum.ExecuteMethodWithResult(ReferendumMethod.Release, Hash.LoadFromHex(proposalId));
            result.Status.ShouldBe("MINED");
        }

        [TestMethod]
        [DataRow("d04236479e2d4e881208316117ca349abdea66beb80f6d4ed55e8eac52ec4939",
            "2ZYyxEH6j8zAyJjef6Spa99Jx2zf5GbFktyAQEBPWLCvuSAn8D")]
        public void ReclaimVoteToken(string proposalId, string account)
        {
            var beforeBalance = Tester.TokenService.GetUserBalance(account, Symbol);
            Logger.Info($"{account} token balance is {beforeBalance}");

            Referendum.SetAccount(account);
            var result =
                Referendum.ExecuteMethodWithResult(ReferendumMethod.ReclaimVoteToken,
                    Hash.LoadFromHex(proposalId));
            result.Status.ShouldBe("MINED");
            var fee = result.GetDefaultTransactionFee();

            var balance = Tester.TokenService.GetUserBalance(account, Symbol);
            balance.ShouldBe(beforeBalance + 50 - fee);
            Logger.Info($"{account} token balance is {balance}");
        }
    }
}