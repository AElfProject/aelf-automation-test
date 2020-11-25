using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Standards.ACS1;
using AElf.Standards.ACS3;
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
        private static readonly ILog Logger = Log4NetHelper.GetLogger();
        private AssociationContract Association;
        private ContractManager ContractManager;
        private List<string> Miners;
        public string Symbol = NodeOption.NativeTokenSymbol;
        public INodeManager NodeManager { get; set; }
        public string InitAccount { get; } = "28Y8JA1i2cN6oHvdv7EraXJr9a1gY6D1PpJXw9QtRMRwKcBQMK";
        public string ReviewAccount1 { get; } = "2RCLmZQ2291xDwSbDEJR6nLhFJcMkyfrVTq1i1YxWC4SdY49a6";
        public string ReviewAccount2 { get; } = "28qLVdGMokanMAp9GwfEqiWnzzNifh8LS9as6mzJFX1gQBB823";
        public string ReviewAccount3 { get; } = "eFU9Quc8BsztYpEHKzbNtUpu9hGKgwGD2tyL13MqtFkbnAoCZ";
        public string NewMember { get; } = "h6CRCFAhyozJPwdFRd7i8A5zVAqy171AVty3uMQUQp1MB9AKa";

        private static string RpcUrl { get; } = "http://192.168.197.22:8000";

        [TestInitialize]
        public void Initialize()
        {
            #region Basic Preparation

            //Init Logger
            Log4NetHelper.LogInit("AssociationTest_");
            NodeInfoHelper.SetConfig("nodes-env2-main");

            #endregion

            NodeManager = new NodeManager(RpcUrl);
            ContractManager = new ContractManager(NodeManager, InitAccount);
            Association = ContractManager.Association;
            Miners = ContractManager.Authority.GetCurrentMiners();
        }

        [TestMethod]
        public void PrepareTest()
        {
            var token = ContractManager.Token;
            token.TransferBalance(InitAccount, ReviewAccount1, 1000_0000000, "ELF");
            token.TransferBalance(InitAccount, ReviewAccount2, 1000_0000000, "ELF");
            token.TransferBalance(InitAccount, ReviewAccount3, 1000_0000000, "ELF");
            token.TransferBalance(InitAccount, NewMember, 1000_0000000, "ELF");
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
                            ReviewAccount3.ConvertAddress(),InitAccount.ConvertAddress()
                        }
                    },
                    ProposerWhiteList = new ProposerWhiteList
                    {
                        Proposers = {InitAccount.ConvertAddress(),ReviewAccount2.ConvertAddress()}
                    },
                    CreationToken = HashHelper.ComputeFrom("ABC")
                });
            var organizationAddress =
                Address.Parser.ParseFrom(ByteArrayHelper.HexStringToByteArray(result.ReturnValue));
            Logger.Info($"organization address is : {organizationAddress}");
            var fee = result.GetDefaultTransactionFee();
            Logger.Info($"Transaction fee is {fee}");

            var organization =
                ContractManager.Association.CallViewMethod<Organization>(AssociationMethod.GetOrganization,
                    organizationAddress);
            organization.OrganizationMemberList.OrganizationMembers.Contains(ReviewAccount2.ConvertAddress())
                .ShouldBeTrue();
            organization.ProposerWhiteList.Proposers.Contains(InitAccount.ConvertAddress()).ShouldBeTrue();
            organization.ProposalReleaseThreshold.MinimalVoteThreshold.ShouldBe(3);
            organization.CreationToken.ShouldBe(HashHelper.ComputeFrom("ABC"));
//            ContractManager.Token.SetAccount(InitAccount);
//            var transfer = ContractManager.Token.ExecuteMethodWithResult(TokenMethod.Transfer, new TransferInput
//            {
//                Symbol = Symbol,
//                Amount = 1000,
//                Memo = "transfer to Organization",
//                To = organizationAddress
//            });
        }

        [TestMethod]
        [DataRow("hK7vZT8NsjLC6Jnt7Dq8urSvnaZ5SEyJK3cUZ6k8noXWav5cL")]
        public void GetOrganization(string organizationAddress)
        {
            var organization =
                ContractManager.Association.CallViewMethod<Organization>(AssociationMethod.GetOrganization,
                    organizationAddress.ConvertAddress());
            foreach (var member in organization.OrganizationMemberList.OrganizationMembers) Logger.Info($"{member}");

            Logger.Info(
                $"{organization.OrganizationAddress} maximal abstention threshold is {organization.ProposalReleaseThreshold.MaximalAbstentionThreshold}");
            Logger.Info($"{organization}");
        }

        [TestMethod]
        [DataRow("251FcF7xUDqf9Md6jdqiJA4BnQJ63M1FTX8XrtE36WpvZqmS8c")]
        public void CreateProposal(string organizationAddress)
        {
            var transferInput = new TransferInput
            {
                Symbol = Symbol,
                Amount = 100,
                To = ReviewAccount1.ConvertAddress(),
                Memo = "Transfer"
            };
            var createProposalInput = new CreateProposalInput
            {
                ContractMethodName = nameof(TokenMethod.Transfer),
                ToAddress = ContractManager.Token.Contract,
                Params = transferInput.ToByteString(),
                ExpiredTime = DateTime.UtcNow.AddDays(1).ToTimestamp(),
                OrganizationAddress = organizationAddress.ConvertAddress(),
                ProposalDescriptionUrl = "http://192.168.197.27"
            };

            ContractManager.Association.SetAccount(ReviewAccount1);
            var result =
                ContractManager.Association.ExecuteMethodWithResult(AssociationMethod.CreateProposal,
                    createProposalInput);
            var proposal = Hash.Parser.ParseFrom(ByteArrayHelper.HexStringToByteArray(result.ReturnValue));
            Logger.Info($"Proposal is : {proposal}");
        }

        [TestMethod]
        [DataRow("8b9bd67374186006aef1d4c443356865d6da6c7a5c6c5737747fe6103310e7bf")]
        public void GetProposal(string proposalId)
        {
            var result =
                ContractManager.Association.CallViewMethod<ProposalOutput>(AssociationMethod.GetProposal,
                    Hash.LoadFromHex(proposalId));
            var toBeRelease = result.ToBeReleased;

            Logger.Info($"proposal is {toBeRelease}");
        }

        [TestMethod]
        [DataRow("913a971647aaaf121ee2e1d71c27c0f25eb8877b76e4994b9ec90600e4ae8e24")]
        public void Approve(string proposalId)
        {
            Association.SetAccount(ReviewAccount1);
            var result = Association.ExecuteMethodWithResult(AssociationMethod.Approve,
                Hash.LoadFromHex(proposalId));
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var byteString =
                ByteString.FromBase64(result.Logs.First(l => l.Name.Contains(nameof(ReceiptCreated))).NonIndexed);
            var info = ReceiptCreated.Parser.ParseFrom(byteString);
            info.ProposalId.ShouldBe(Hash.LoadFromHex(proposalId));
            info.ReceiptType.ShouldBe(nameof(AssociationMethod.Approve));
            info.Address.ShouldBe(ReviewAccount1.ConvertAddress());
        }

        [TestMethod]
        [DataRow("913a971647aaaf121ee2e1d71c27c0f25eb8877b76e4994b9ec90600e4ae8e24")]
        public void Release(string proposalId)
        {
            Association.SetAccount(ReviewAccount1);
            var result = Association.ExecuteMethodWithResult(AssociationMethod.Release,
                Hash.LoadFromHex(proposalId));
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
           }

        [TestMethod]
        [DataRow("DCMn2iZ5VjDxg51wzpuJxcUDfarG1dnKwd4TngSH8TS2vJsE2")]
        public void GetBalance(string account)
        {
            var balance = ContractManager.Token.GetUserBalance(account, Symbol);
            Logger.Info($"{account} balance is {balance}");
        }

        [TestMethod]
        public async Task GetMethodFeeController()
        {
            var controller = await ContractManager.AssociationImplStub.GetMethodFeeController.CallAsync(new Empty());
            Logger.Info($"{controller.ContractAddress} controller is {controller.OwnerAddress}");
        }

        [TestMethod]
        public async Task SetMethodFeeThroughDefaultController()
        {
            var methodFee = await ContractManager.AssociationImplStub.GetMethodFee.CallAsync(new StringValue
            {
                Value = nameof(AssociationMethod.CreateOrganization)
            });
            methodFee.ShouldBe(new MethodFees());

            var controller = await ContractManager.AssociationImplStub.GetMethodFeeController.CallAsync(new Empty());
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
            var createProposal = ContractManager.Parliament.CreateProposal(associationContract, "SetMethodFee",
                input, controller.OwnerAddress, InitAccount);
            ContractManager.Parliament.MinersApproveProposal(createProposal, Miners);
            var release = ContractManager.Parliament.ReleaseProposal(createProposal, InitAccount);
            release.Status.ShouldBe(TransactionResultStatus.Mined);

            methodFee = await ContractManager.AssociationImplStub.GetMethodFee.CallAsync(new StringValue
            {
                Value = nameof(AssociationMethod.CreateOrganization)
            });
            methodFee.Fees.Select(f => f.BasicFee).First().ShouldBe(10000000);
            methodFee.Fees.Select(f => f.Symbol).First().ShouldBe("ELF");
        }

        [TestMethod]
        public void ChangeMembers()
        {
            var organization = "hK7vZT8NsjLC6Jnt7Dq8urSvnaZ5SEyJK3cUZ6k8noXWav5cL";
            var input = new ChangeMemberInput
            {
                NewMember = NewMember.ConvertAddress(),
                OldMember = ReviewAccount1.ConvertAddress()
            };
            var createProposal = Association.CreateProposal(Association.ContractAddress,
                nameof(AssociationMethod.ChangeMember), input, organization.ConvertAddress(), InitAccount);
            
            var reviewers = Association.GetOrganization(organization.ConvertAddress());
            foreach (var member in reviewers.OrganizationMemberList.OrganizationMembers)
                Association.ApproveProposal(createProposal,member.ToBase58());
            
            var release = Association.ReleaseProposal(createProposal, InitAccount);
            release.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
        }
        
        [TestMethod]
        public void AddMembers()
        {
            var organization = "hK7vZT8NsjLC6Jnt7Dq8urSvnaZ5SEyJK3cUZ6k8noXWav5cL";
            var input = ReviewAccount1.ConvertAddress();
            var createProposal = Association.CreateProposal(Association.ContractAddress,
                nameof(AssociationMethod.AddMember), input, organization.ConvertAddress(), InitAccount);
            var reviewers = Association.GetOrganization(organization.ConvertAddress());
            foreach (var member in reviewers.OrganizationMemberList.OrganizationMembers)
                Association.ApproveProposal(createProposal,member.ToBase58());
            var release = Association.ReleaseProposal(createProposal, InitAccount);
            release.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
        }
        
        [TestMethod]
        public void RemoveMembers()
        {
            var organization = "hK7vZT8NsjLC6Jnt7Dq8urSvnaZ5SEyJK3cUZ6k8noXWav5cL";
            var input = InitAccount.ConvertAddress();
            var createProposal = Association.CreateProposal(Association.ContractAddress,
                nameof(AssociationMethod.RemoveMember), input, organization.ConvertAddress(), InitAccount);
            var reviewers = Association.GetOrganization(organization.ConvertAddress());
            foreach (var member in reviewers.OrganizationMemberList.OrganizationMembers)
                Association.ApproveProposal(createProposal,member.ToBase58());
            var release = Association.ReleaseProposal(createProposal, InitAccount);
            release.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
        }

        [TestMethod]
        public async Task ChangeMethodFeeController()
        {
            var controller = await ContractManager.AssociationImplStub.GetMethodFeeController.CallAsync(new Empty());
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
            var createProposal = ContractManager.Parliament.CreateProposal(associationContract,
                nameof(AssociationMethod.ChangeMethodFeeController),
                changeInput, controller.OwnerAddress, InitAccount);
            ContractManager.Parliament.MinersApproveProposal(createProposal, Miners);
            var release = ContractManager.Parliament.ReleaseProposal(createProposal, InitAccount);
            release.Status.ShouldBe(TransactionResultStatus.Mined);
            controller = await ContractManager.AssociationImplStub.GetMethodFeeController.CallAsync(new Empty());
            controller.ContractAddress.ShouldBe(associationContract.ConvertAddress());
            controller.OwnerAddress.ShouldBe(newController);
        }

        [TestMethod]
        public async Task SetMethodFeeThroughNewController()
        {
            var methodFee = await ContractManager.AssociationImplStub.GetMethodFee.CallAsync(new StringValue
            {
                Value = nameof(AssociationMethod.CreateOrganization)
            });
            methodFee.Fees.Select(l => l.BasicFee).First().ShouldBe(10000000);

            var controller = await ContractManager.AssociationImplStub.GetMethodFeeController.CallAsync(new Empty());
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

            methodFee = await ContractManager.AssociationImplStub.GetMethodFee.CallAsync(new StringValue
            {
                Value = nameof(AssociationMethod.CreateOrganization)
            });
            methodFee.Fees.Select(f => f.BasicFee).First().ShouldBe(100000000);
            methodFee.Fees.Select(f => f.Symbol).First().ShouldBe("ELF");
        }

        [TestMethod]
        public async Task ChangeMethodFeeControllerThroughOtherController()
        {
            var controller = await ContractManager.AssociationImplStub.GetMethodFeeController.CallAsync(new Empty());
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
            controller = await ContractManager.AssociationImplStub.GetMethodFeeController.CallAsync(new Empty());
            controller.ContractAddress.ShouldBe(associationContract.ConvertAddress());
            controller.OwnerAddress.ShouldBe(newController);
        }
    }
}