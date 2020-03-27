using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Acs1;
using Acs3;
using AElf.Contracts.Association;
using AElf.Contracts.MultiToken;
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
    public class AssociationAuthContractTest
    {
        private static readonly ILog _logger = Log4NetHelper.GetLogger();
        private AssociationAuthContract Association;
        private ContractManager ContractManager;
        private List<string> Miners;
        public string Symbol = NodeOption.NativeTokenSymbol;
        public INodeManager NodeManager { get; set; }
        public string InitAccount { get; } = "28Y8JA1i2cN6oHvdv7EraXJr9a1gY6D1PpJXw9QtRMRwKcBQMK";
        public string ReviewAccount1 { get; } = "2RCLmZQ2291xDwSbDEJR6nLhFJcMkyfrVTq1i1YxWC4SdY49a6";
        public string ReviewAccount2 { get; } = "28qLVdGMokanMAp9GwfEqiWnzzNifh8LS9as6mzJFX1gQBB823";
        public string ReviewAccount3 { get; } = "eFU9Quc8BsztYpEHKzbNtUpu9hGKgwGD2tyL13MqtFkbnAoCZ";

        private static string RpcUrl { get; } = "http://192.168.197.14:8000";

        [TestInitialize]
        public void Initialize()
        {
            #region Basic Preparation

            //Init Logger
            Log4NetHelper.LogInit("AssociationTest_");
            NodeInfoHelper.SetConfig("nodes-env1-main");

            #endregion

            NodeManager = new NodeManager(RpcUrl);
            ContractManager = new ContractManager(NodeManager, InitAccount);
            Association = ContractManager.Association;
            Miners = ContractManager.Authority.GetCurrentMiners();
            ContractManager.Token.TransferBalance(InitAccount, ReviewAccount1, 1000_0000000, "ELF");
            ContractManager.Token.TransferBalance(InitAccount, ReviewAccount2, 1000_0000000, "ELF");
            ContractManager.Token.TransferBalance(InitAccount, ReviewAccount3, 1000_0000000, "ELF");
        }

        [TestMethod]
        public void CreateOrganization()
        {
            var result = ContractManager.Association.ExecuteMethodWithResult(AssociationMethod.CreateOrganization,
                new CreateOrganizationInput
                {
                    ProposalReleaseThreshold = new ProposalReleaseThreshold
                    {
                        MaximalAbstentionThreshold = 2,
                        MaximalRejectionThreshold = 1,
                        MinimalApprovalThreshold = 1,
                        MinimalVoteThreshold = 3
                    },
                    OrganizationMemberList = new OrganizationMemberList
                    {
                        OrganizationMembers =
                        {
                            ReviewAccount1.ConvertAddress(), ReviewAccount2.ConvertAddress(),
                            ReviewAccount3.ConvertAddress(), InitAccount.ConvertAddress()
                        }
                    },
                    ProposerWhiteList = new ProposerWhiteList
                    {
                        Proposers = {InitAccount.ConvertAddress()}
                    }
                });
            var organizationAddress =
                Address.Parser.ParseFrom(ByteArrayHelper.HexStringToByteArray(result.ReturnValue));
            _logger.Info($"organization address is : {organizationAddress}");
            var fee = result.GetDefaultTransactionFee();
            _logger.Info($"Transaction fee is {fee}");

            var organization =
                ContractManager.Association.CallViewMethod<Organization>(AssociationMethod.GetOrganization,
                    organizationAddress);
            organization.OrganizationMemberList.OrganizationMembers.Contains(ReviewAccount2.ConvertAddress())
                .ShouldBeTrue();
            organization.ProposerWhiteList.Proposers.Contains(InitAccount.ConvertAddress()).ShouldBeTrue();
            organization.ProposalReleaseThreshold.MinimalVoteThreshold.ShouldBe(3);
            ContractManager.Token.SetAccount(InitAccount);
            var transfer = ContractManager.Token.ExecuteMethodWithResult(TokenMethod.Transfer, new TransferInput
            {
                Symbol = Symbol,
                Amount = 1000,
                Memo = "transfer to Organization",
                To = organizationAddress
            });
        }

        [TestMethod]
        [DataRow("2UicBDXQszyhSaimjj4sbqeMzEBMqLCx3vsjgR49WUyrcocZd9")]
        public void GetOrganization(string organizationAddress)
        {
            var organization =
                ContractManager.Association.CallViewMethod<Organization>(AssociationMethod.GetOrganization,
                    AddressHelper.Base58StringToAddress(organizationAddress));
            foreach (var member in organization.OrganizationMemberList.OrganizationMembers) _logger.Info($"{member}");

            _logger.Info(
                $"{organization.OrganizationAddress} maximal abstention threshold is {organization.ProposalReleaseThreshold.MaximalAbstentionThreshold}");
        }

        [TestMethod]
        [DataRow("2UicBDXQszyhSaimjj4sbqeMzEBMqLCx3vsjgR49WUyrcocZd9")]
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
                ToAddress = AddressHelper.Base58StringToAddress(ContractManager.Token.ContractAddress),
                Params = transferInput.ToByteString(),
                ExpiredTime = DateTime.UtcNow.AddDays(1).ToTimestamp(),
                OrganizationAddress = AddressHelper.Base58StringToAddress(organizationAddress),
                ProposalDescriptionUrl = "http://192.168.197.27"
            };

            ContractManager.Association.SetAccount(ReviewAccount1);
            var result =
                ContractManager.Association.ExecuteMethodWithResult(AssociationMethod.CreateProposal,
                    createProposalInput);
            var proposal = Hash.Parser.ParseFrom(ByteArrayHelper.HexStringToByteArray(result.ReturnValue));
            _logger.Info($"Proposal is : {proposal}");
        }

        [TestMethod]
        [DataRow("913a971647aaaf121ee2e1d71c27c0f25eb8877b76e4994b9ec90600e4ae8e24")]
        public void GetProposal(string proposalId)
        {
            var result =
                ContractManager.Association.CallViewMethod<ProposalOutput>(AssociationMethod.GetProposal,
                    HashHelper.HexStringToHash(proposalId));
            var toBeRelease = result.ToBeReleased;

            _logger.Info($"proposal is {toBeRelease}");
        }

        [TestMethod]
        [DataRow("913a971647aaaf121ee2e1d71c27c0f25eb8877b76e4994b9ec90600e4ae8e24")]
        public void Approve(string proposalId)
        {
            Association.SetAccount(ReviewAccount1);
            var result = Association.ExecuteMethodWithResult(AssociationMethod.Approve,
                HashHelper.HexStringToHash(proposalId));
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var byteString =
                ByteString.FromBase64(result.Logs.First(l => l.Name.Contains(nameof(ReceiptCreated))).NonIndexed);
            var info = ReceiptCreated.Parser.ParseFrom(byteString);
            info.ProposalId.ShouldBe(HashHelper.HexStringToHash(proposalId));
            info.ReceiptType.ShouldBe(nameof(AssociationMethod.Approve));
            info.Address.ShouldBe(ReviewAccount1.ConvertAddress());
        }

        [TestMethod]
        [DataRow("913a971647aaaf121ee2e1d71c27c0f25eb8877b76e4994b9ec90600e4ae8e24")]
        public void Release(string proposalId)
        {
            Association.SetAccount(ReviewAccount1);
            var result = Association.ExecuteMethodWithResult(AssociationMethod.Release,
                HashHelper.HexStringToHash(proposalId));
            Assert.AreSame(result.Status, "MINED");
        }

        [TestMethod]
        [DataRow("DCMn2iZ5VjDxg51wzpuJxcUDfarG1dnKwd4TngSH8TS2vJsE2")]
        public void GetBalance(string account)
        {
            var balance = ContractManager.Token.GetUserBalance(account, Symbol);
            _logger.Info($"{account} balance is {balance}");
        }

        [TestMethod]
        public async Task GetMethodFeeController()
        {
            var controller = await ContractManager.AssociationStub.GetMethodFeeController.CallAsync(new Empty());
            _logger.Info($"{controller.ContractAddress} controller is {controller.OwnerAddress}");
        }

        [TestMethod]
        public async Task SetMethodFeeThroughDefaultController()
        {
            var methodFee = await ContractManager.AssociationStub.GetMethodFee.CallAsync(new StringValue
            {
                Value = nameof(AssociationMethod.CreateOrganization)
            });
            methodFee.ShouldBe(new MethodFees());

            var controller = await ContractManager.AssociationStub.GetMethodFeeController.CallAsync(new Empty());
            var input = new MethodFees
            {
                MethodName = nameof(AssociationMethod.CreateOrganization),
                Fees =
                {
                    new MethodFee
                    {
                        BasicFee = 10000000,
                        Symbol = "ELF"
                    }
                }
            };

            var associationContract = ContractManager.Association.ContractAddress;
            var createProposal = ContractManager.ParliamentAuth.CreateProposal(associationContract, "SetMethodFee",
                input, controller.OwnerAddress, InitAccount);
            ContractManager.ParliamentAuth.MinersApproveProposal(createProposal, Miners);
            var release = ContractManager.ParliamentAuth.ReleaseProposal(createProposal, InitAccount);
            release.Status.ShouldBe(TransactionResultStatus.Mined);

            methodFee = await ContractManager.AssociationStub.GetMethodFee.CallAsync(new StringValue
            {
                Value = nameof(AssociationMethod.CreateOrganization)
            });
            methodFee.Fees.Select(f => f.BasicFee).First().ShouldBe(10000000);
            methodFee.Fees.Select(f => f.Symbol).First().ShouldBe("ELF");
        }

        [TestMethod]
        public async Task ChangeMethodFeeController()
        {
            var controller = await ContractManager.AssociationStub.GetMethodFeeController.CallAsync(new Empty());
            var input = new CreateOrganizationInput
            {
                ProposalReleaseThreshold = new ProposalReleaseThreshold
                {
                    MaximalAbstentionThreshold = 1,
                    MaximalRejectionThreshold = 1,
                    MinimalApprovalThreshold = 2,
                    MinimalVoteThreshold = 3
                },
                OrganizationMemberList = new OrganizationMemberList
                {
                    OrganizationMembers =
                    {
                        ReviewAccount1.ConvertAddress(), ReviewAccount2.ConvertAddress(),
                        ReviewAccount3.ConvertAddress()
                    }
                },
                ProposerWhiteList = new ProposerWhiteList
                {
                    Proposers = {InitAccount.ConvertAddress()}
                }
            };
            var associationContract = ContractManager.Association.ContractAddress;
            var newController = ContractManager.Association.CreateOrganization(input);
            var changeInput = new AuthorityInfo
            {
                ContractAddress = associationContract.ConvertAddress(),
                OwnerAddress = newController
            };
            var createProposal = ContractManager.ParliamentAuth.CreateProposal(associationContract,
                nameof(AssociationMethod.ChangeMethodFeeController),
                changeInput, controller.OwnerAddress, InitAccount);
            ContractManager.ParliamentAuth.MinersApproveProposal(createProposal, Miners);
            var release = ContractManager.ParliamentAuth.ReleaseProposal(createProposal, InitAccount);
            release.Status.ShouldBe(TransactionResultStatus.Mined);
            controller = await ContractManager.AssociationStub.GetMethodFeeController.CallAsync(new Empty());
            controller.ContractAddress.ShouldBe(associationContract.ConvertAddress());
            controller.OwnerAddress.ShouldBe(newController);
        }

        [TestMethod]
        public async Task SetMethodFeeThroughNewController()
        {
            var methodFee = await ContractManager.AssociationStub.GetMethodFee.CallAsync(new StringValue
            {
                Value = nameof(AssociationMethod.CreateOrganization)
            });
            methodFee.Fees.Select(l => l.BasicFee).First().ShouldBe(10000000);

            var controller = await ContractManager.AssociationStub.GetMethodFeeController.CallAsync(new Empty());
            var input = new MethodFees
            {
                MethodName = nameof(AssociationMethod.CreateOrganization),
                Fees =
                {
                    new MethodFee
                    {
                        BasicFee = 100000000,
                        Symbol = "ELF"
                    }
                }
            };

            var associationContract = ContractManager.Association.ContractAddress;
            var createProposal = ContractManager.Association.CreateProposal(associationContract, "SetMethodFee",
                input, controller.OwnerAddress, InitAccount);
            ContractManager.Association.ApproveWithAssociation(createProposal, controller.OwnerAddress);
            var release = ContractManager.Association.ReleaseProposal(createProposal, InitAccount);
            release.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            methodFee = await ContractManager.AssociationStub.GetMethodFee.CallAsync(new StringValue
            {
                Value = nameof(AssociationMethod.CreateOrganization)
            });
            methodFee.Fees.Select(f => f.BasicFee).First().ShouldBe(100000000);
            methodFee.Fees.Select(f => f.Symbol).First().ShouldBe("ELF");
        }

        [TestMethod]
        public async Task ChangeMethodFeeControllerThroughOtherController()
        {
            var controller = await ContractManager.AssociationStub.GetMethodFeeController.CallAsync(new Empty());
            var input = new CreateOrganizationInput
            {
                ProposalReleaseThreshold = new ProposalReleaseThreshold
                {
                    MaximalAbstentionThreshold = 1,
                    MaximalRejectionThreshold = 1,
                    MinimalApprovalThreshold = 3,
                    MinimalVoteThreshold = 4
                },
                OrganizationMemberList = new OrganizationMemberList
                {
                    OrganizationMembers =
                    {
                        ReviewAccount1.ConvertAddress(), ReviewAccount2.ConvertAddress(),
                        ReviewAccount3.ConvertAddress(), InitAccount.ConvertAddress()
                    }
                },
                ProposerWhiteList = new ProposerWhiteList
                {
                    Proposers = {InitAccount.ConvertAddress(), ReviewAccount1.ConvertAddress()}
                }
            };
            var associationContract = ContractManager.Association.ContractAddress;
            var newController = ContractManager.Association.CreateOrganization(input);
            var changeInput = new AuthorityInfo
            {
                ContractAddress = associationContract.ConvertAddress(),
                OwnerAddress = newController
            };
            var createProposal = ContractManager.Association.CreateProposal(associationContract,
                nameof(AssociationMethod.ChangeMethodFeeController),
                changeInput, controller.OwnerAddress, InitAccount);
            ContractManager.Association.ApproveWithAssociation(createProposal, controller.OwnerAddress);
            var release = ContractManager.Association.ReleaseProposal(createProposal, InitAccount);
            release.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            controller = await ContractManager.AssociationStub.GetMethodFeeController.CallAsync(new Empty());
            controller.ContractAddress.ShouldBe(associationContract.ConvertAddress());
            controller.OwnerAddress.ShouldBe(newController);
        }
    }
}