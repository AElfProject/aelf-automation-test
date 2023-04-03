using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.Client.Dto;
using AElf.Standards.ACS0;
using AElf.Standards.ACS3;
using AElf.Contracts.MultiToken;
using AElf.Types;
using AElfChain.Common.Contracts;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using AElfChain.Common;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using log4net;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using Volo.Abp.Threading;

namespace AElf.Automation.Contracts.ScenarioTest
{
    [TestClass]
    public class GenesisContractTest
    {
        protected static readonly ILog Logger = Log4NetHelper.GetLogger();
        private readonly bool isOrganization = false;

        protected ContractTester Tester;

        public INodeManager NM { get; set; }
        public ContractManager MainManager { get; set; }
        public static string InitAccount { get; } = "28Y8JA1i2cN6oHvdv7EraXJr9a1gY6D1PpJXw9QtRMRwKcBQMK";
        public static string Creator { get; } = "28Y8JA1i2cN6oHvdv7EraXJr9a1gY6D1PpJXw9QtRMRwKcBQMK";
        public static string Member { get; } = "2frDVeV6VxUozNqcFbgoxruyqCRAuSyXyfCaov6bYWc7Gkxkh2";
        public static string OtherAccount { get; } = "W4xEKTZcvPKXRAmdu9xEpM69ArF7gUxDh9MDgtsKnu7JfePXo";

        public static string Author = "W4xEKTZcvPKXRAmdu9xEpM69ArF7gUxDh9MDgtsKnu7JfePXo";

        public List<string> Members;
        private static string MainRpcUrl { get; } = "http://192.168.197.21:8000";
        private static string SideRpcUrl { get; } = "http://192.168.197.21:8001";
        private static string SideRpcUrl2 { get; } = "http://192.168.197.21:8002";
        private string Type { get; } = "Main";

        [TestInitialize]
        public void Initialize()
        {
            #region Basic Preparation

            //Init Logger
            Log4NetHelper.LogInit("ContractTest_");
            NodeInfoHelper.SetConfig("nodes-env2-main");

            #endregion

            NM = new NodeManager(MainRpcUrl);
            var services = new ContractServices(NM, InitAccount, Type);
            MainManager = new ContractManager(NM, InitAccount);

            Tester = new ContractTester(services);
            if (Type == "Side2" && !isOrganization)
            {
                Tester.IssueTokenToMiner(Creator);
                Tester.IssueToken(Creator, Author);
            }
            else if (isOrganization)
            {
                Tester.TokenService.TransferBalance(OtherAccount, Member, 100_00000000,
                    Tester.TokenService.GetPrimaryTokenSymbol());
                Tester.TokenService.TransferBalance(OtherAccount, InitAccount, 100_00000000,
                    Tester.TokenService.GetPrimaryTokenSymbol());
                var creator = Tester.AuthorityManager.CreateAssociationOrganization(Members);
                IssueTokenToMinerThroughOrganization(Tester, OtherAccount, creator);
            }
            else
            {
                Tester.TransferTokenToMiner(InitAccount);
                Tester.TransferToken(Author);
            }

            Members = new List<string> { InitAccount, Member, OtherAccount };
        }

        #region Proposal Deploy/Update

        // SideChain:  IsAuthoiryRequired == true; IsPrivilegePreserved == true;
        // Only creator can deploy and update contract.
        // Only creator can update system contract.
        // SideChain: IsAuthoiryRequired == true; IsPrivilegePreserved == false;
        // all account can deploy and update contract; only miner and creator can update system contracts
        // MainChain: IsAuthoiryRequired == true; IsPrivilegePreserved == false;
        // only miner can deploy and update contracts
        // all the contracts' author on main chain is genesis contract
        [TestMethod]
        public void UpdateSmartContract_UserUpdate()
        {
            var input = ContractUpdateInput("AElf.Contracts.Election", Tester.AssociationService.ContractAddress);

            Tester.TokenService.SetAccount(InitAccount);
            var result = Tester.GenesisService.ExecuteMethodWithResult(GenesisMethod.UpdateSmartContract, input);
            result.Status.ShouldBe("FAILED");
            result.Error.Contains("Unauthorized behavior.").ShouldBeTrue();
        }

        [TestMethod]
        public void DeploySmartContract_ThroughGenesisOwnerAddress()
        {
            var input = ContractDeploymentInput("AElf.Contracts.MultiToken");
            var organization = GetGenesisOwnerAddress(Tester);
            var proposal = Tester.ParliamentService.CreateProposal(Tester.GenesisService.ContractAddress,
                nameof(GenesisMethod.ProposeNewContract), input,
                organization, InitAccount);
            ApproveByMiner(Tester, proposal);
            var release = Tester.ParliamentService.ReleaseProposal(proposal, InitAccount);
            release.Status.ShouldBe(TransactionResultStatus.Failed);
            release.Error.Contains("Proposer authority validation failed.").ShouldBeTrue();
        }

        // ContractDeploymentAuthorityRequired == false
        [TestMethod]
        public void DeploySmartContract_AuthorityRequiredFalse()
        {
            var input = ContractDeploymentInput("AElf.Contracts.MultiToken");
            Tester.GenesisService.SetAccount(InitAccount);
            var result = Tester.GenesisService.ExecuteMethodWithResult(GenesisMethod.DeploySystemSmartContract, input);
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
        }

        [TestMethod]
        public void DeploySmartContract_UserDeploy()
        {
            var input = ContractDeploymentInput("AElf.Contracts.MultiToken");
            Tester.TokenService.SetAccount(InitAccount);
            var result = Tester.GenesisService.ExecuteMethodWithResult(GenesisMethod.DeploySmartContract, input);
            result.Status.ShouldBe("FAILED");
            result.Error.Contains("Unauthorized behavior.").ShouldBeTrue();
        }

        [TestMethod]
        public void DeploySystemContract_()
        {
            var input = ContractDeploymentInput("AElf.Contracts.MultiToken");
            var organization = GetGenesisOwnerAddress(Tester);
            var proposal = Tester.ParliamentService.CreateProposal(Tester.GenesisService.ContractAddress,
                nameof(GenesisMethod.DeploySystemSmartContract), input,
                organization, InitAccount);
            ApproveByMiner(Tester, proposal);
            var release = Tester.ParliamentService.ReleaseProposal(proposal, InitAccount);
            release.Status.ShouldBe(TransactionResultStatus.Failed);
            release.Error.Contains("Contract proposing data not found.").ShouldBeTrue();
        }

        [TestMethod]
        public async Task ProposalDeploy_MinerProposalContract_Success_stub()
        {
            var genesis = MainManager.GenesisImplStub;
            var input = ContractDeploymentInput("AElf.Contracts.MultiToken");
            var result = await genesis.ProposeNewContract.SendAsync(input);
            var size = result.Transaction.CalculateSize();
            var fee = TransactionFeeCharged.Parser.ParseFrom(result.TransactionResult.Logs
                .First(l => l.Name.Equals(nameof(TransactionFeeCharged))).NonIndexed).Amount;
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            Logger.Info($"{size}");
        }

        [TestMethod]
        public void ProposalDeploy_MinerProposalContract_Success()
        {
            var input = ContractDeploymentInput("AElf.Contracts.MultiToken");
            var contractProposalInfo = ProposalNewContract(Tester, Creator, input);
//            var proposalId = ProposalCreated.Parser
//                .ParseFrom(ByteString.FromBase64("CiIKIEBMlAb4Acvr6Pj3Rsq7+EJ3DGhOFQEX9E2dqI0R1aYu")).ProposalId;
//            var proposalHash = ContractProposed.Parser
//                .ParseFrom(ByteString.FromBase64("CiIKIOt1LiiSK5YRP9vUpMGUFNt2rjuF3IpEAYC0J/vS0Tj0"))
//                .ProposedContractInputHash;
//            var contractProposalInfo = new ReleaseContractInput
//            {
//                ProposalId = proposalId,
//                ProposedContractInputHash = proposalHash
//            };
            ApproveByMiner(Tester, contractProposalInfo.ProposalId);
            var release = Tester.GenesisService.ReleaseApprovedContract(contractProposalInfo, Creator);
            release.Status.ShouldBe("MINED");
            var byteString =
                ByteString.FromBase64(release.Logs.First(l => l.Name.Contains(nameof(ProposalCreated))).NonIndexed);
            var deployProposal = ProposalCreated.Parser.ParseFrom(byteString).ProposalId;
            
            Logger.Info($"{deployProposal}\n {contractProposalInfo.ProposedContractInputHash}");
        }

        [TestMethod]
        public void ProposalDeploy_Success()
        {
            var input = ContractDeploymentInput("AElf.Contracts.MultiToken");
            var contractProposalInfo = ProposalNewContract(Tester, Member, input);
//            var proposalId = ProposalCreated.Parser
//                .ParseFrom(ByteString.FromBase64("CiIKIEBMlAb4Acvr6Pj3Rsq7+EJ3DGhOFQEX9E2dqI0R1aYu")).ProposalId;
//            var proposalHash = ContractProposed.Parser
//                .ParseFrom(ByteString.FromBase64("CiIKIOt1LiiSK5YRP9vUpMGUFNt2rjuF3IpEAYC0J/vS0Tj0"))
//                .ProposedContractInputHash;
//            var contractProposalInfo = new ReleaseContractInput
//            {
//                ProposalId = proposalId,
//                ProposedContractInputHash = proposalHash
//            };

            Logger.Info($"{contractProposalInfo.ProposalId}\n {contractProposalInfo.ProposedContractInputHash}");
        }

        [TestMethod]
        public void ProposalDeploy_ProposalContractWithOrganizationCreator_Success()
        {
            var deploymentInput = ContractDeploymentInput("AElf.Contracts.MultiToken1");
            Tester.AssociationService.SetAccount(OtherAccount);
            var creator = ("s31xt16WnoYEhLxgSx7Jofy3ZkezEaf5mSieKd7LpR99NsKaW").ConvertAddress();
            var associationCreateProposal = Tester.AssociationService.CreateProposal(
                Tester.GenesisService.ContractAddress, nameof(GenesisMethod.ProposeNewContract), deploymentInput,
                creator, OtherAccount);
            Tester.AssociationService.ApproveWithAssociation(associationCreateProposal, creator);
            var createResult = Tester.AssociationService.ReleaseProposal(associationCreateProposal, OtherAccount);
            var proposalId = ProposalCreated.Parser
                .ParseFrom(ByteString.FromBase64(createResult.Logs.First(l => l.Name.Contains(nameof(ProposalCreated)))
                    .NonIndexed)).ProposalId;
            var proposalHash = ContractProposed.Parser
                .ParseFrom(ByteString.FromBase64(createResult.Logs.First(l => l.Name.Contains(nameof(ContractProposed)))
                    .NonIndexed))
                .ProposedContractInputHash;
            var contractProposalInfo = new ReleaseContractInput
            {
                ProposalId = proposalId,
                ProposedContractInputHash = proposalHash
            };
            ApproveByMiner(Tester, contractProposalInfo.ProposalId);

            var releaseProposal = Tester.AssociationService.CreateProposal(
                Tester.GenesisService.ContractAddress, nameof(GenesisMethod.ReleaseApprovedContract),
                contractProposalInfo,
                creator, OtherAccount);
            Tester.AssociationService.ApproveWithAssociation(releaseProposal, creator);
            var releaseResult = Tester.AssociationService.ReleaseProposal(releaseProposal, OtherAccount);
            var byteString =
                ByteString.FromBase64(
                    releaseResult.Logs.First(l => l.Name.Contains(nameof(ProposalCreated))).NonIndexed);
            var deployProposal = ProposalCreated.Parser.ParseFrom(byteString).ProposalId;

            Logger.Info($"{deployProposal}\n {contractProposalInfo.ProposedContractInputHash}");
        }

        [TestMethod]
        public void ProposalDeploy_OrganizationProposalContractWithOtherOrganization_Success()
        {
            var input = ContractDeploymentInput("AElf.Contracts.MultiToken");
            var contractProposalInfo = ProposalNewContract(Tester, OtherAccount, input);
//            var contractProposalInfo = new ReleaseContractInput
//            {
//                ProposalId = Hash.LoadFromHex("9d6ee285b090b4f1261eeb76dfac83055b50fcff01507596f3201aa18f1a44da"),
//                ProposedContractInputHash = Hash.LoadFromHex("ad8b21fcc5ab497942cffe3de55fae9de62dc6bd16eb5f2cb81248c8a7684eb9")
//            };
            var organizationAddress =
                ("2EBXKkQfGz4fD1xacTiAXp7JksTpECTXJy5MSuYyEzdLbsanZW").ConvertAddress();
            Tester.AssociationService.ApproveWithAssociation(contractProposalInfo.ProposalId, organizationAddress);
            var release = Tester.GenesisService.ReleaseApprovedContract(contractProposalInfo, OtherAccount);
            release.Status.ShouldBe("MINED");
            var byteString =
                ByteString.FromBase64(release.Logs.First(l => l.Name.Contains(nameof(ProposalCreated))).NonIndexed);
            var deployProposal = ProposalCreated.Parser.ParseFrom(byteString).ProposalId;

            Logger.Info($"{deployProposal}\n {contractProposalInfo.ProposedContractInputHash}");
        }
        
        [TestMethod]
        [DataRow("6bf5db3a326d99bf4c508e7b39df0f22543271cade52c6a001dba293f132251f",
            "ce3b565c1003b2a61937c540ff7e9db15a4f63dcd7ee6ffdff31b1a47eab4ad1")]
        public void ReleaseDeployCodeCheck(string proposal, string hash)
        {
            var proposalId = Hash.LoadFromHex(proposal);
            var proposalHash = Hash.LoadFromHex(hash);
            var releaseApprovedContractInput = new ReleaseContractInput
            {
                ProposedContractInputHash = proposalHash,
                ProposalId = proposalId
            };

            var release = Tester.GenesisService.ReleaseCodeCheckedContract(releaseApprovedContractInput, Creator);
            release.Status.ShouldBe("MINED");
            var byteString =
                ByteString.FromBase64(release.Logs.First(l => l.Name.Contains(nameof(ContractDeployed))).NonIndexed);
            var byteStringIndexed =
                ByteString.FromBase64(
                    release.Logs.First(l => l.Name.Contains(nameof(ContractDeployed))).Indexed.First());
            var contractDeployed = ContractDeployed.Parser.ParseFrom(byteString);
            var deployAddress = contractDeployed.Address;
            var contractVersion = contractDeployed.ContractVersion;
            var author = ContractDeployed.Parser.ParseFrom(byteStringIndexed).Author;
            Logger.Info($"{deployAddress}, {author}, {release.BlockNumber}");

            var contractInfo =
                Tester.GenesisService.CallViewMethod<ContractInfo>(GenesisMethod.GetContractInfo,
                    deployAddress);
            Logger.Info(contractInfo);
            contractInfo.ContractVersion.ShouldBe(contractVersion);
        }

        [TestMethod]
        [DataRow("9114cedf21b4273803a4dfcd1a8260200e0416ebb24ceb2492a1e5fe052bdc34",
            "6e256995ba37bf00314ff85cc666bff225292e70d3c7a734bc0f28c67904eaa7")]
        public void ReleaseDeployCodeCheckWithOrganization(string proposal, string hash)
        {
            var proposalId = Hash.LoadFromHex(proposal);
            var proposalHash = Hash.LoadFromHex(hash);
            var releaseApprovedContractInput = new ReleaseContractInput
            {
                ProposedContractInputHash = proposalHash,
                ProposalId = proposalId
            };
            var creator = ("s31xt16WnoYEhLxgSx7Jofy3ZkezEaf5mSieKd7LpR99NsKaW").ConvertAddress();
            var releaseProposal = Tester.AssociationService.CreateProposal(
                Tester.GenesisService.ContractAddress, nameof(GenesisMethod.ReleaseCodeCheckedContract),
                releaseApprovedContractInput,
                creator, OtherAccount);
            Tester.AssociationService.ApproveWithAssociation(releaseProposal, creator);
            var releaseResult = Tester.AssociationService.ReleaseProposal(releaseProposal, OtherAccount);
            releaseResult.Status.ShouldBe("MINED");
            var byteString =
                ByteString.FromBase64(releaseResult.Logs.First(l => l.Name.Contains(nameof(ContractDeployed)))
                    .NonIndexed);
            var deployAddress = ContractDeployed.Parser.ParseFrom(byteString).Address;
            Logger.Info($"{deployAddress}");
        }


        [TestMethod]
        public void ProposalUpdate_MinerProposalUpdateContract_Success()
        {
            var input = ContractUpdateInput("AElf.Contracts.MultiToken", Tester.ReferendumService.ContractAddress);
            var contractProposalInfo = ProposalUpdateContract(Tester, InitAccount, input);
            ApproveByMiner(Tester, contractProposalInfo.ProposalId);
            Logger.Info($"{contractProposalInfo.ProposalId}\n {contractProposalInfo.ProposedContractInputHash}");

            var release = Tester.GenesisService.ReleaseApprovedContract(contractProposalInfo, Creator);
            release.Status.ShouldBe("MINED");
            var byteString =
                ByteString.FromBase64(release.Logs.First(l => l.Name.Contains(nameof(ProposalCreated))).NonIndexed);
            var deployProposal = ProposalCreated.Parser.ParseFrom(byteString).ProposalId;
            Logger.Info($"{deployProposal}\n {contractProposalInfo.ProposedContractInputHash}");
        }

        [TestMethod]
        public void ReleaseUpdateCodeCheck_InSameBlock()
        {
            var input1 = new ReleaseContractInput
            {
                ProposedContractInputHash =
                    Hash.LoadFromHex("f2316113de586b5893e96f4567c9f63de2d0db5cb918dfc5797401d7e0d94a06"),
                ProposalId = Hash.LoadFromHex("75bb5f4da874c8b9f77f4125b594ebfcb73587892acdee7336c43f0b3a3a3f5b")
            };
            var input2 = new ReleaseContractInput
            {
                ProposedContractInputHash =
                    Hash.LoadFromHex("6a2ce65aa2c2fb30a011d8418beae13351ffe598f7223673fb29f2f79427b8af"),
                ProposalId = Hash.LoadFromHex("4f9e76a8071033abe0b15b7626d035509ea6b8a9e0137e03707318fcbb747495")
            };

            var release1 = Tester.GenesisService.ExecuteMethodWithTxId(GenesisMethod.ReleaseCodeCheckedContract,
                new ReleaseContractInput
                {
                    ProposalId = input1.ProposalId,
                    ProposedContractInputHash = input1.ProposedContractInputHash
                });

            var release2 = Tester.GenesisService.ExecuteMethodWithTxId(GenesisMethod.ReleaseCodeCheckedContract,
                new ReleaseContractInput
                {
                    ProposalId = input2.ProposalId,
                    ProposedContractInputHash = input2.ProposedContractInputHash
                });

            var updateAddress = "2WHXRoLRjbUTDQsuqR5CntygVfnDb125qdJkudev4kVNbLhTdG";

            var contractInfo =
                Tester.GenesisService.CallViewMethod<ContractInfo>(GenesisMethod.GetContractInfo,
                    Address.FromBase58(updateAddress));
            Logger.Info(contractInfo);
        }

        [TestMethod]
        public void ProposalUpdate_MinerProposalUpdateContract_Success_online()
        {
            // Tester.TokenService.TransferBalance(InitAccount, Creator, 100000_00000000, "ELF");
            var input = ContractUpdateInput("AElf.Contracts.MultiToken", Tester.TokenService.ContractAddress);
            var contractProposalInfo = ProposalUpdateContract(Tester, Creator, input);
            ApproveByMiner(Tester, contractProposalInfo.ProposalId);
            Logger.Info($"Proposal: {contractProposalInfo.ProposalId.ToHex()}");
            Logger.Info($"Hash: {contractProposalInfo.ProposedContractInputHash.ToHex()}");
        }
        //7d6a34c8baf8aada2271e3568ebc1cbac92cbc590a8f5d2b126e4f3d66519c8f
        //1b998f5d669eaa9e17433e8d8ff4406eb0c7977a7afde4c645a829a9cd9c7acf

        [TestMethod]
        public void ReleaseApprove()
        {
            var contractProposalInfo = new ReleaseContractInput
            {
                ProposalId = Hash.LoadFromHex("9a62795d47d70d124676136cf8954fa35c39a939237d5abf19bce6dbbfb74711"),
                ProposedContractInputHash =
                    Hash.LoadFromHex("71d418cb62865d6d3e912a9996c3f172a33c0010568fc47514fd317b38bd0cd3")
            };
            var release = Tester.GenesisService.ReleaseApprovedContract(contractProposalInfo, Creator);
            release.Status.ShouldBe("MINED");
            var byteString =
                ByteString.FromBase64(release.Logs.First(l => l.Name.Contains(nameof(ProposalCreated))).NonIndexed);
            var deployProposal = ProposalCreated.Parser.ParseFrom(byteString).ProposalId;
            Logger.Info($"{deployProposal}\n {contractProposalInfo.ProposedContractInputHash}");
        }

        [TestMethod]
        public void CheckLogs()
        {
            var proposalId = ProposalCreated.Parser
                .ParseFrom(ByteString.FromBase64("CiIKIKzZ4zK7LIy5XkGg0DUIw7RuYsRuFrVsJ//ORrth0X6X")).ProposalId;
            var proposalHash = Hash.LoadFromHex("2207ecceace5548098886487fbe07ab7b869dd57f6196e8f0b6cb3264aa64cc5");
            var contractProposalInfo = new ReleaseContractInput
            {
                ProposalId = proposalId,
                ProposedContractInputHash = proposalHash
            };

            var release = Tester.GenesisService.ReleaseApprovedContract(contractProposalInfo, Creator);
            release.Status.ShouldBe("MINED");
            var byteString =
                ByteString.FromBase64(release.Logs.First(l => l.Name.Contains(nameof(ProposalCreated))).NonIndexed);
            var deployProposal = ProposalCreated.Parser.ParseFrom(byteString).ProposalId;
            Logger.Info($"{deployProposal}\n {contractProposalInfo.ProposedContractInputHash}");
        }


        [TestMethod]
        [DataRow("a588b918b4c2c9398aa3d12f1553793cb82a69b65a59fd3aac7c0176bd3280fe",
            "427d479d26bca0d340e3e6d8149b38aada9897bae33cf69e285ce40cacd72507")]
        public void ReleaseUpdateCodeCheck(string proposal, string hash)
        {
            var proposalId = Hash.LoadFromHex(proposal);
            var proposalHash = Hash.LoadFromHex(hash);
            var releaseApprovedContractInput = new ReleaseContractInput
            {
                ProposedContractInputHash = proposalHash,
                ProposalId = proposalId
            };

            var release = Tester.GenesisService.ReleaseCodeCheckedContract(releaseApprovedContractInput, Creator);
            release.Status.ShouldBe("MINED");
            var byteString =
                ByteString.FromBase64(release.Logs.First(l => l.Name.Contains(nameof(CodeUpdated))).Indexed.First());
            var updateAddress = CodeUpdated.Parser.ParseFrom(byteString).Address;
            var nonIndexed =
                ByteString.FromBase64(release.Logs.First(l => l.Name.Contains(nameof(CodeUpdated))).NonIndexed);
            var contractVersion = CodeUpdated.Parser.ParseFrom(nonIndexed).ContractVersion;
            Logger.Info($"{updateAddress}, {contractVersion}, {release.BlockNumber}");

            var contractInfo =
                Tester.GenesisService.CallViewMethod<ContractInfo>(GenesisMethod.GetContractInfo,
                    updateAddress);
            Logger.Info(contractInfo);
            contractInfo.ContractVersion.ShouldBe(contractVersion);
        }

        [TestMethod]
        public void ProposalDeploy_OtherUserProposalContract_Failed()
        {
            var input = ContractDeploymentInput("AElf.Contracts.MultiToken4");
            Tester.GenesisService.SetAccount(OtherAccount);
            var result = Tester.GenesisService.ExecuteMethodWithResult(GenesisMethod.ProposeNewContract, input);
            result.Status.ShouldBe("FAILED");
            result.Error.Contains("Unauthorized to propose.").ShouldBeTrue();
        }

        [TestMethod]
        public void ProposalUpdate_OtherUserUpdate_Failed()
        {
            var input = ContractUpdateInput("AElf.Contracts.MultiToken", Tester.ReferendumService.ContractAddress);
            Tester.GenesisService.SetAccount(OtherAccount);
            var result = Tester.GenesisService.ExecuteMethodWithResult(GenesisMethod.ProposeUpdateContract, input);
            result.Status.ShouldBe("FAILED");
            result.Error.Contains("Unauthorized to propose.").ShouldBeTrue();
        }

        #endregion

        #region Controller

        [TestMethod]
        public void ChangeContractDeploymentController()
        {
            var changeAddress = Tester.AuthorityManager.CreateAssociationOrganization(Members);
            var input = new AuthorityInfo
            {
                ContractAddress = Tester.AssociationService.Contract,
                OwnerAddress = changeAddress
            };

            var contractDeploymentController =
                Tester.GenesisService.CallViewMethod<AuthorityInfo>(GenesisMethod.GetContractDeploymentController,
                    new Empty());
            Logger.Info($"owner address is {contractDeploymentController.OwnerAddress} ");

            var miners = Tester.GetMiners();
            Tester.ParliamentService.SetAccount(miners.First());
            var proposal = Tester.ParliamentService.ExecuteMethodWithResult(ParliamentMethod.CreateProposal,
                new CreateProposalInput
                {
                    ContractMethodName = nameof(GenesisMethod.ChangeContractDeploymentController),
                    ExpiredTime = DateTime.UtcNow.AddDays(1).ToTimestamp(),
                    Params = input.ToByteString(),
                    ToAddress = Tester.GenesisService.Contract,
                    OrganizationAddress = contractDeploymentController.OwnerAddress
                });
            var proposalId = Hash.Parser.ParseFrom(ByteArrayHelper.HexStringToByteArray(proposal.ReturnValue));
            ApproveByMiner(Tester, proposalId);
            var release = Tester.ParliamentService.ReleaseProposal(proposalId, miners.First());
            release.Status.ShouldBe(TransactionResultStatus.Mined);
            contractDeploymentController =
                Tester.GenesisService.CallViewMethod<AuthorityInfo>(GenesisMethod.GetContractDeploymentController,
                    new Empty());
            contractDeploymentController.OwnerAddress.ShouldBe(changeAddress);
            contractDeploymentController.ContractAddress.ShouldBe(Tester.AssociationService.Contract);
            Logger.Info($"Owner address is {contractDeploymentController.OwnerAddress} ");
        }

        [TestMethod]
        public void ChangeCodeCheckController()
        {
            var changeAddress = Tester.AuthorityManager.CreateAssociationOrganization(Members);
            var input = new AuthorityInfo
            {
                ContractAddress = Tester.AssociationService.Contract,
                OwnerAddress = changeAddress
            };

            var contractCodeCheckController =
                Tester.GenesisService.CallViewMethod<AuthorityInfo>(GenesisMethod.GetCodeCheckController,
                    new Empty());
            Logger.Info($"owner address is {contractCodeCheckController.OwnerAddress} ");

            var miners = Tester.GetMiners();
            Tester.ParliamentService.SetAccount(miners.First());
            var proposal = Tester.ParliamentService.ExecuteMethodWithResult(ParliamentMethod.CreateProposal,
                new CreateProposalInput
                {
                    ContractMethodName = nameof(GenesisMethod.ChangeCodeCheckController),
                    ExpiredTime = DateTime.UtcNow.AddDays(1).ToTimestamp(),
                    Params = input.ToByteString(),
                    ToAddress = Tester.GenesisService.Contract,
                    OrganizationAddress = contractCodeCheckController.OwnerAddress
                });
            var proposalId = Hash.Parser.ParseFrom(ByteArrayHelper.HexStringToByteArray(proposal.ReturnValue));
            ApproveByMiner(Tester, proposalId);
            var release = Tester.ParliamentService.ReleaseProposal(proposalId, miners.First());
            release.Status.ShouldBe(TransactionResultStatus.Mined);
            contractCodeCheckController =
                Tester.GenesisService.CallViewMethod<AuthorityInfo>(GenesisMethod.GetCodeCheckController,
                    new Empty());
            contractCodeCheckController.OwnerAddress.ShouldBe(changeAddress);
            contractCodeCheckController.ContractAddress.ShouldBe(Tester.AssociationService.Contract);
            Logger.Info($"Code check controller address is {contractCodeCheckController.OwnerAddress} ");
        }

        [TestMethod]
        public void CheckController()
        {
            var contractCodeCheckController =
                Tester.GenesisService.CallViewMethod<AuthorityInfo>(GenesisMethod.GetContractDeploymentController,
                    new Empty());
            contractCodeCheckController.ContractAddress.ShouldBe(Tester.AssociationService.Contract);
            Logger.Info($"Code check controller address is {contractCodeCheckController.OwnerAddress} ");
        }

        [TestMethod]
        public void ParliamentChangeWhiteList()
        {
            var parliament = Tester.ParliamentService;
            var proposalWhiteList =
                parliament.CallViewMethod<ProposerWhiteList>(
                    ParliamentMethod.GetProposerWhiteList, new Empty());
            Logger.Info(proposalWhiteList);

            var defaultAddress = parliament.GetGenesisOwnerAddress();
            var existResult =
                parliament.CallViewMethod<BoolValue>(ParliamentMethod.ValidateOrganizationExist, defaultAddress);
            existResult.Value.ShouldBeTrue();
            var addList = new List<Address>
            {
                Tester.GenesisService.Contract
            };
            proposalWhiteList.Proposers.AddRange(addList);
            var miners = Tester.GetMiners();

            var changeInput = new ProposerWhiteList
            {
                Proposers = { proposalWhiteList.Proposers }
            };

            var proposalId = parliament.CreateProposal(parliament.ContractAddress,
                nameof(ParliamentMethod.ChangeOrganizationProposerWhiteList), changeInput, defaultAddress,
                miners.First());
            parliament.MinersApproveProposal(proposalId, miners);

            Thread.Sleep(10000);
            parliament.SetAccount(miners.First());
            var release = parliament.ReleaseProposal(proposalId, miners.First());
            release.Status.ShouldBe(TransactionResultStatus.Mined);

            proposalWhiteList =
                parliament.CallViewMethod<ProposerWhiteList>(
                    ParliamentMethod.GetProposerWhiteList, new Empty());
            Logger.Info(proposalWhiteList);
        }

        [TestMethod]
        public void GetParliamentChangeWhiteList()
        {
            var parliament = Tester.ParliamentService;
            var proposalWhiteList =
                parliament.CallViewMethod<ProposerWhiteList>(
                    ParliamentMethod.GetProposerWhiteList, new Empty());
            Logger.Info(proposalWhiteList);
        }

        #endregion

        #region Check

        [TestMethod]
        [DataRow("34c46a83b260bc9e9bfc2d4930ea794dfb2eb93c1221cd5146310504a5dd21c5")]
        public void CheckProposal(string proposalId)
        {
            var proposal = Hash.LoadFromHex(proposalId);
            var result = Tester.ParliamentService.CallViewMethod<ProposalOutput>(ParliamentMethod.GetProposal,
                proposal);
            Logger.Info($"{result.ToBeReleased}");
            Logger.Info($"{result.ExpiredTime}");
            Logger.Info($"{result.Proposer}");
            Logger.Info($"{result.OrganizationAddress}");
        }

        [TestMethod]
        [DataRow("SuaPmtyFjozAVCbubchFHL2yLUrpgWYM67CMgNES1v16xanq9")]
        public void CheckOwner(string contract)
        {
            var address =
                Tester.GenesisService.CallViewMethod<Address>(GenesisMethod.GetContractAuthor,
                    contract.ConvertAddress());
            Logger.Info($"{address.ToBase58()}");
        }

        [TestMethod]
        public void CheckContractInfo()
        {
            var contract = "JRmBduh4nXWi1aXgdUsj5gJrzeZb2LxmrAbf7W99faZSvoAaE";
            var contractInfo =
                Tester.GenesisService.CallViewMethod<ContractInfo>(GenesisMethod.GetContractInfo,
                    contract.ConvertAddress());
            Logger.Info(contractInfo);
        }
        
        [TestMethod]
        public void CheckContract()
        {
            var code = "a5105e37575800d643dfd6efac78d048bdee5fe18f685fff0f4dfcc711d50954";
            var contractInfo =
                Tester.GenesisService.CallViewMethod<ContractInfo>(GenesisMethod.GetSmartContractRegistrationByCodeHash,
                    Hash.LoadFromHex(code));
            Logger.Info(contractInfo);

            var address = "xsnQafDAhNTeYcooptETqWnYBksFGGXxfcQyJJ5tmu6Ak9ZZt"; 
            contractInfo =
                Tester.GenesisService.CallViewMethod<ContractInfo>(GenesisMethod.GetSmartContractRegistrationByAddress,
                    address.ConvertAddress());
            Logger.Info(contractInfo);
        }

        #endregion

        #region DeployUserSmartContract/UpdateUserSmartContract

        [TestMethod]
        [DataRow("AElf.Contracts.TestContract.BasicSecurity-nopatched-1.3.0-1")]
        public void DeployUserSmartContract(string contractFileName)
        {
            var result = Tester.GenesisService.DeployUserSmartContract(contractFileName, Author);
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            var logEvent = result.Logs.First(l => l.Name.Equals(nameof(CodeCheckRequired))).NonIndexed;
            var codeCheckRequired = CodeCheckRequired.Parser.ParseFrom(ByteString.FromBase64(logEvent));
            codeCheckRequired.Category.ShouldBe(0);
            codeCheckRequired.IsSystemContract.ShouldBeFalse();
            codeCheckRequired.IsUserContract.ShouldBeTrue();
            var proposalLogEvent = result.Logs.First(l => l.Name.Equals(nameof(ProposalCreated))).NonIndexed;
            var proposalId = ProposalCreated.Parser.ParseFrom(ByteString.FromBase64(proposalLogEvent)).ProposalId;

            var returnValue = DeployUserSmartContractOutput.Parser.ParseFrom(ByteArrayHelper.HexStringToByteArray(result.ReturnValue));
            var codeHash = returnValue.CodeHash;
            Logger.Info(
                $"Code hash: {codeHash.ToHex()}\n ProposalInput: {codeCheckRequired.ProposedContractInputHash.ToHex()}\n Proposal Id: {proposalId.ToHex()}");

            // var check = CheckProposal(proposalId);
            // check.ShouldBeTrue();
            Thread.Sleep(20000);

            var currentHeight = AsyncHelper.RunSync(Tester.NodeManager.ApiClient.GetBlockHeightAsync);
            var smartContractRegistration = Tester.GenesisService.GetSmartContractRegistrationByCodeHash(codeHash);
            smartContractRegistration.ShouldNotBeNull();
            Logger.Info($"Check height: {result.BlockNumber} - {currentHeight}");

            var release = FindReleaseApprovedUserSmartContractMethod(result.BlockNumber, currentHeight);
            Logger.Info(release.TransactionId);

            var releaseLogEvent = release.Logs.First(l => l.Name.Equals(nameof(ContractDeployed)));
            var indexed = releaseLogEvent.Indexed;
            var nonIndexed = releaseLogEvent.NonIndexed;
            foreach (var i in indexed)
            {
                var contractDeployedIndexed = ContractDeployed.Parser.ParseFrom(ByteString.FromBase64(i));
                Logger.Info(contractDeployedIndexed.Author == null
                    ? $"Code hash: {contractDeployedIndexed.CodeHash.ToHex()}"
                    : $"Author: {contractDeployedIndexed.Author}");
            }

            var contractDeployedNonIndexed = ContractDeployed.Parser.ParseFrom(ByteString.FromBase64(nonIndexed));
            Logger.Info($"Address: {contractDeployedNonIndexed.Address}\n" +
                        $"{contractDeployedNonIndexed.Name}\n" +
                        $"{contractDeployedNonIndexed.Version}\n" +
                        $"{contractDeployedNonIndexed.ContractVersion}");
        } 

        [TestMethod]
        [DataRow("AElf.Contracts.TestContract.BasicSecurity-nopatched-1.1.0",
            "RXcxgSXuagn8RrvhQAV81Z652EEYSwR6JLnqHYJ5UVpEptW8Y")]
        public void UpdateUserSmartContract(string contractFileName, string contractAddress)
        {
            var author = Tester.GenesisService.GetContractAuthor(Address.FromBase58(contractAddress));
            Tester.TokenService.TransferBalance(InitAccount, author.ToBase58(), 10000_00000000, "STA");
            // var author = Address.FromBase58(InitAccount);
            var result =
                Tester.GenesisService.UpdateUserSmartContract(contractFileName, contractAddress, author.ToBase58());
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            var logEvent = result.Logs.First(l => l.Name.Equals(nameof(CodeCheckRequired))).NonIndexed;
            var codeCheckRequired = CodeCheckRequired.Parser.ParseFrom(ByteString.FromBase64(logEvent));
            codeCheckRequired.Category.ShouldBe(0);
            codeCheckRequired.IsSystemContract.ShouldBeFalse();
            codeCheckRequired.IsUserContract.ShouldBeTrue();
            var proposalLogEvent = result.Logs.First(l => l.Name.Equals(nameof(ProposalCreated))).NonIndexed;
            var proposalId = ProposalCreated.Parser.ParseFrom(ByteString.FromBase64(proposalLogEvent)).ProposalId;

            Logger.Info(
                $"ProposalInput: {codeCheckRequired.ProposedContractInputHash.ToHex()}");

            // var check = CheckProposal(proposalId);
            // check.ShouldBeTrue();
            Thread.Sleep(20000);

            var currentHeight = AsyncHelper.RunSync(Tester.NodeManager.ApiClient.GetBlockHeightAsync);

            var release = FindReleaseApprovedUserSmartContractMethod(result.BlockNumber, currentHeight);
            Logger.Info(release.TransactionId);

            var releaseLogEvent = release.Logs.First(l => l.Name.Equals(nameof(CodeUpdated)));
            var indexed = releaseLogEvent.Indexed;
            var nonIndexed = releaseLogEvent.NonIndexed;
            var codeUpdatedIndexed = CodeUpdated.Parser.ParseFrom(ByteString.FromBase64(indexed.First()));
            Logger.Info($"Address: {codeUpdatedIndexed.Address}");

            var codeUpdatedNonIndexed = CodeUpdated.Parser.ParseFrom(ByteString.FromBase64(nonIndexed));
            Logger.Info($"NewCodeHash: {codeUpdatedNonIndexed.NewCodeHash}\n" +
                        $"{codeUpdatedNonIndexed.OldCodeHash}\n" +
                        $"{codeUpdatedNonIndexed.Version}\n" +
                        $"{codeUpdatedNonIndexed.ContractVersion}");

            var smartContractRegistration =
                Tester.GenesisService.GetSmartContractRegistrationByCodeHash(codeUpdatedNonIndexed.NewCodeHash);
            smartContractRegistration.ShouldNotBeNull();
            var contractInfo = Tester.GenesisService.CallViewMethod<ContractInfo>(GenesisMethod.GetContractInfo,
                contractAddress.ConvertAddress());
            Logger.Info(contractInfo);

            contractInfo.CodeHash.ShouldBe(codeUpdatedNonIndexed.NewCodeHash);
            contractInfo.Version.ShouldBe(codeUpdatedNonIndexed.Version);
            contractInfo.ContractVersion.ShouldBe(codeUpdatedNonIndexed.ContractVersion);
        }
        
        [TestMethod]
        [DataRow("", "redis")]
        public void CheckCodeHash(string codeHashString, string type)
        {
            var codeHash = type switch
            {
                "redis" => Hash.LoadFromByteArray(ByteString.FromBase64(codeHashString).ToByteArray()),
                "return" => Hash.Parser.ParseFrom(ByteArrayHelper.HexStringToByteArray(codeHashString)),
                _ => null
            };
            Logger.Info(codeHash?.ToHex());
            var smartContractRegistration = Tester.GenesisService.GetSmartContractRegistrationByCodeHash(codeHash);
            Logger.Info(smartContractRegistration);
        }

        [TestMethod]
        [DataRow("c5d0ddeecca4724b89281903f546132d61b3fd1b73e841f16eec842fe07f8cb0")]
        public void CheckReleaseLogEvent(string txId)
        {
            var txResult = AsyncHelper.RunSync(() => Tester.NodeManager.ApiClient.GetTransactionResultAsync(txId));
            var logs = txResult.Logs;
            if (logs.Any(l => l.Name.Equals(nameof(ContractDeployed))))
            {
                var logsEvent = logs.First(l => l.Name.Equals(nameof(ContractDeployed)));
                var indexed = logsEvent.Indexed;
                var nonIndexed = logsEvent.NonIndexed;
                foreach (var i in indexed)
                {
                    var contractDeployedIndexed = ContractDeployed.Parser.ParseFrom(ByteString.FromBase64(i));
                    Logger.Info(contractDeployedIndexed.Author == null
                        ? $"Code hash: {contractDeployedIndexed.CodeHash.ToHex()}"
                        : $"Author: {contractDeployedIndexed.Author}");
                }

                var contractDeployedNonIndexed = ContractDeployed.Parser.ParseFrom(ByteString.FromBase64(nonIndexed));
                Logger.Info($"Address: {contractDeployedNonIndexed.Address}\n" +
                            $"{contractDeployedNonIndexed.Name}\n" +
                            $"{contractDeployedNonIndexed.Version}\n" +
                            $"{contractDeployedNonIndexed.ContractVersion}");
            }
            else
            {
                var logsEvent = logs.First(l => l.Name.Equals(nameof(CodeUpdated)));
                var indexed = logsEvent.Indexed;
                var nonIndexed = logsEvent.NonIndexed;
                var codeUpdatedIndexed = CodeUpdated.Parser.ParseFrom(ByteString.FromBase64(indexed.First()));
                Logger.Info($"Address: {codeUpdatedIndexed.Address}");

                var codeUpdatedNonIndexed = CodeUpdated.Parser.ParseFrom(ByteString.FromBase64(nonIndexed));
                Logger.Info($"NewCodeHash: {codeUpdatedNonIndexed.NewCodeHash}\n" +
                            $"{codeUpdatedNonIndexed.OldCodeHash}\n" +
                            $"{codeUpdatedNonIndexed.Version}\n" +
                            $"{codeUpdatedNonIndexed.ContractVersion}");
            }
        }

        [TestMethod]
        public void SetContractAuthor()
        {
            var contract = "RXcxgSXuagn8RrvhQAV81Z652EEYSwR6JLnqHYJ5UVpEptW8Y";
            var author = Tester.GenesisService.GetContractAuthor(contract);
            var newAuthor = Tester.NodeManager.NewAccount("12345678");
            var primaryToken = Tester.TokenService.GetPrimaryTokenSymbol();
            Tester.TokenService.TransferBalance(InitAccount, newAuthor, 10000000000, primaryToken);
            Tester.GenesisService.SetAccount(author.ToBase58());
            var result = Tester.GenesisService.ExecuteMethodWithResult(GenesisMethod.SetContractAuthor,
                new SetContractAuthorInput
                {
                    NewAuthor = newAuthor.ConvertAddress(),
                    ContractAddress = contract.ConvertAddress()
                });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            author = Tester.GenesisService.GetContractAuthor(contract);
            author.ShouldBe(newAuthor.ConvertAddress());
        }

        #endregion

        #region Deploy/UpdateUserSmartContrac Exception

        [TestMethod]
        public void DeployInSameBlockWithSameFile()
        {
            var file = "AElf.Contracts.TestContract.BasicSecurity-nopatched-1.1.0";
            var txList = new List<string>();
            var txResult1 = Tester.GenesisService.DeployUserSmartContractWithoutResult(file, InitAccount);
            Thread.Sleep(500);
            var txResult2 = Tester.GenesisService.DeployUserSmartContractWithoutResult(file, Author);
            txList.Add(txResult1);
            txList.Add(txResult2);
            Thread.Sleep(2000);
            foreach (var result in txList.Select(tx =>
                         AsyncHelper.RunSync(() => Tester.NodeManager.ApiClient.GetTransactionResultAsync(tx))))
                Logger.Info($"{result.Status.ConvertTransactionResultStatus()},{result.Error}");
        }
        
        [TestMethod]
        public void DeployInSameBlockWithSameAssembly()
        {
            var fileList = new List<string>
            {
                "AElf.Contracts.TestContract.BasicFunction-patched-1.0.0",
                "AElf.Contracts.TestContract.BasicFunction-patched-1.1.0"
            };
            var rawTxList = new List<string>();
            var txList = new List<string>();
            foreach (var file in fileList)
            {
                var rawTx = Tester.GenesisService.GenerateDeployUserSmartContract(file, InitAccount);
                rawTxList.Add(rawTx);
            }
            var rawTransactions = string.Join(",", rawTxList);
            txList.AddRange(Tester.NodeManager.SendTransactions(rawTransactions));
            
            foreach (var result in txList.Select(tx =>
                         AsyncHelper.RunSync(() => Tester.NodeManager.ApiClient.GetTransactionResultAsync(tx))))
                Logger.Info($"{result.Status.ConvertTransactionResultStatus()},{result.Error}");
        }
        
        [TestMethod]
        public void DeployUpdateInSameBlockWithSameFile()
        {
            var deployFile = "AElf.Contracts.TestContract.BasicFunction-nopatched-1.2.0-1";
            var updateFile = "AElf.Contracts.TestContract.BasicFunction-nopatched-1.2.0-2";

            var contractAddress = "2WHXRoLRjbUTDQsuqR5CntygVfnDb125qdJkudev4kVNbLhTdG";
            var author = Tester.GenesisService.GetContractAuthor(contractAddress);
            var txList = new List<string>();
            var txResult1 = Tester.GenesisService.DeployUserSmartContractWithoutResult(deployFile, InitAccount);
            var txResult2 = Tester.GenesisService.UpdateUserSmartContractWithoutResult(updateFile, contractAddress, author.ToBase58());
            txList.Add(txResult1);
            txList.Add(txResult2);
            Thread.Sleep(2000);
            foreach (var result in txList.Select(tx =>
                         AsyncHelper.RunSync(() => Tester.NodeManager.ApiClient.GetTransactionResultAsync(tx))))
                Logger.Info($"{result.Status.ConvertTransactionResultStatus()},{result.Error}");
        }

        [TestMethod]
        [DataRow("AElf.Contracts.TestContract.BasicSecurity-patched-1.3.0-1",
            "AElf.Contracts.TestContract.BasicFunction-patched-noACS1-1.1.0")]
        public void DeployAndReleaseApprovedInSameBlock(string deployUserContractFile, string proposalContractFile)
        {
            var txList = new List<string>();
            var input = ContractDeploymentInput(proposalContractFile);
            var contractProposalInfo = ProposalNewContract(Tester, Creator, input); 
            ApproveByMiner(Tester, contractProposalInfo.ProposalId);
            
            var releaseTxId = Tester.GenesisService.ReleaseApprovedContractWithoutResult(contractProposalInfo, Creator);
            var deployTxId = Tester.GenesisService.DeployUserSmartContractWithoutResult(deployUserContractFile, Author);
            
            txList.Add(releaseTxId);
            txList.Add(deployTxId);

            Thread.Sleep(5000);
            foreach (var result in txList.Select(tx =>
                         AsyncHelper.RunSync(() => Tester.NodeManager.ApiClient.GetTransactionResultAsync(tx))))
                Logger.Info(result.Status.ConvertTransactionResultStatus());

            var checkRelease =
                AsyncHelper.RunSync(() => Tester.NodeManager.ApiClient.GetTransactionResultAsync(releaseTxId));
            var logs = checkRelease.Logs.First(l => l.Name.Equals(nameof(ProposalCreated)));
            var proposalId = ProposalCreated.Parser
                .ParseFrom(ByteString.FromBase64(logs.NonIndexed)).ProposalId;
            var hash = contractProposalInfo.ProposedContractInputHash;
            Logger.Info($"ProposalId: {proposalId.ToHex()}\n" +
                        $"ContractInputHash: {hash.ToHex()}");
            
            Thread.Sleep(5000);
            
            var releaseCodeCheckInput = new ReleaseContractInput
            {
                ProposalId = proposalId,
                ProposedContractInputHash = hash
            };
            var releaseCodeCheck = Tester.GenesisService.ReleaseCodeCheckedContract(releaseCodeCheckInput, Creator);
            releaseCodeCheck.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var byteString =
                ByteString.FromBase64(releaseCodeCheck.Logs.First(l => l.Name.Contains(nameof(ContractDeployed))).NonIndexed);
            var byteStringIndexed =
                ByteString.FromBase64(
                    releaseCodeCheck.Logs.First(l => l.Name.Contains(nameof(ContractDeployed))).Indexed.First());
            var contractDeployed = ContractDeployed.Parser.ParseFrom(byteString);
            var deployAddress = contractDeployed.Address;
            // var contractVersion = contractDeployed.ContractVersion;
            var author = ContractDeployed.Parser.ParseFrom(byteStringIndexed).Author;
            Logger.Info($"{deployAddress}, {author}, {releaseCodeCheck.BlockNumber}");

            var contractInfo =
                Tester.GenesisService.CallViewMethod<ContractInfo>(GenesisMethod.GetContractInfo,
                    deployAddress);
            Logger.Info(contractInfo);
        }

        [TestMethod]
        [DataRow("AElf.Contracts.TestContract.BasicSecurity-nopatched-1.0.0",
            "AElf.Contracts.TestContract.BasicSecurity-nopatched-1.0.0")]
        public void DeployAndProposalNewInSameBlock(string noPatchedFile, string patchedFile)
        {
            var txList = new List<string>();
            var input = ContractDeploymentInput(patchedFile);
            var proposalTxId = Tester.GenesisService.ProposeNewContractWithoutResult(input,Creator);
            var deployTxId = Tester.GenesisService.DeployUserSmartContractWithoutResult(noPatchedFile, Author);
            
            txList.Add(proposalTxId);
            txList.Add(deployTxId);

            Thread.Sleep(5000);
            foreach (var result in txList.Select(tx =>
                         AsyncHelper.RunSync(() => Tester.NodeManager.ApiClient.GetTransactionResultAsync(tx))))
                Logger.Info(result.Status.ConvertTransactionResultStatus());
        }

        [TestMethod]
        public void ApproveCodeCheckNotPassProposal()
        {
            var proposalId = "2581dbb5f886a14294f155a71eec95a8a15985b5506b8a5f82f1e2ab89511c9d";
            var proposalHash = "3e7595fad07c53cdac28ed815c812193a56bd50f8fa437e506dba7f896121060";
            ApproveByMiner(Tester, Hash.LoadFromHex(proposalId));
            
            var result = Tester.GenesisService.ExecuteMethodWithResult(GenesisMethod.ReleaseApprovedUserSmartContract, 
                new ReleaseContractInput
                {
                    ProposalId = Hash.LoadFromHex(proposalId),
                    ProposedContractInputHash = Hash.LoadFromHex(proposalHash)
                });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
        }
        
        
        [TestMethod]
        public void ReleaseCodeCheckNotPassProposal()
        {
            var proposalId = "79dde0e6d1257a02c0bacbe5030a6543b359887294a85f77c4139b810d103890";
            var proposalHash = ContractProposed.Parser.ParseFrom(ByteString.FromBase64("CiIKIBMaz6DuMUU/igtMaAwwpE1qf6cA7AqKfoGqkZw22iAg")).ProposedContractInputHash;
            var result = Tester.GenesisService.ExecuteMethodWithResult(GenesisMethod.ReleaseApprovedUserSmartContract, 
                new ReleaseContractInput
                {
                    ProposalId = Hash.LoadFromHex(proposalId),
                    ProposedContractInputHash = proposalHash
                });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
        }
        
        [TestMethod]
        public void PerformDeployUserSmartContract_UnauthorizedBehavior()
        {
            var contractReader = new SmartContractReader();
            var codeArray = contractReader.Read("AElf.Contracts.TestContract.BasicSecurity-patched-1.1.0");
            var result = Tester.GenesisService.ExecuteMethodWithResult(
                GenesisMethod.PerformDeployUserSmartContract, new ContractDeploymentInput
                {
                    Code = ByteString.CopyFrom(codeArray),
                    Category = 0
                });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
            result.Error.ShouldContain("Unauthorized behavior.");
        }
        
        [TestMethod]
        public void PerformDeployUserSmartContract_WithoutCodeCheck()
        {
            var contractReader = new SmartContractReader();
            var codeArray = contractReader.Read("AElf.Contracts.TestContract.BasicSecurity-patched-1.1.0");
            var controller = Tester.GenesisService.GetContractDeploymentController();
            var deployInput = new ContractDeploymentInput
            {
                Code = ByteString.CopyFrom(codeArray),
                Category = 0
            };
            var result = Tester.AuthorityManager.ExecuteTransactionWithAuthority(Tester.GenesisService.ContractAddress,
                nameof(GenesisMethod.PerformDeployUserSmartContract), deployInput, InitAccount,
                controller.OwnerAddress);
            result.Status.ShouldBe(TransactionResultStatus.NodeValidationFailed);
            result.Error.ShouldContain("Invalid contract proposing status");
        }
        
        [TestMethod]
        public void PerformDeployUserSmartContract_AlreadyClearPropsal()
        {
            var contractReader = new SmartContractReader();
            var codeArray = contractReader.Read("AElf.Contracts.TestContract.BasicSecurity-nopatched-1.1.0");
            var controller = Tester.GenesisService.GetContractDeploymentController();
            var deployInput = new ContractDeploymentInput
            {
                Code = ByteString.CopyFrom(codeArray),
                Category = 0
            };
            var result = Tester.AuthorityManager.ExecuteTransactionWithAuthority(Tester.GenesisService.ContractAddress,
                nameof(GenesisMethod.PerformDeployUserSmartContract), deployInput, InitAccount,
                controller.OwnerAddress);
            result.Status.ShouldBe(TransactionResultStatus.NodeValidationFailed);
            result.Error.ShouldContain("Contract proposing data not found.");
        }

        #endregion

        #region private method

        private Address GetGenesisOwnerAddress(ContractTester tester)
        {
            return tester.ParliamentService.CallViewMethod<Address>(ParliamentMethod.GetDefaultOrganizationAddress,
                new Empty());
        }

        private ReleaseContractInput ProposalNewContract(ContractTester tester, string account,
            ContractDeploymentInput input)
        {
            var result = tester.GenesisService.ProposeNewContract(input, account);
            result.Status.ShouldBe(TransactionResultStatus.Mined);
            var proposalId = ProposalCreated.Parser
                .ParseFrom(result.Logs.First(l => l.Name.Contains(nameof(ProposalCreated))).NonIndexed).ProposalId;
            var proposalHash = ContractProposed.Parser
                .ParseFrom(result.Logs.First(l => l.Name.Contains(nameof(ContractProposed))).NonIndexed)
                .ProposedContractInputHash;
            return new ReleaseContractInput
            {
                ProposalId = proposalId,
                ProposedContractInputHash = proposalHash
            };
        }

        private ReleaseContractInput ProposalUpdateContract(ContractTester tester, string account,
            ContractUpdateInput input)
        {
            var result = tester.GenesisService.ProposeUpdateContract(input, account);
            result.Status.ShouldBe(TransactionResultStatus.Mined);
            var proposalId = ProposalCreated.Parser
                .ParseFrom(result.Logs.First(l => l.Name.Contains(nameof(ProposalCreated))).NonIndexed).ProposalId;
            var proposalHash = ContractProposed.Parser
                .ParseFrom(result.Logs.First(l => l.Name.Contains(nameof(ContractProposed))).NonIndexed)
                .ProposedContractInputHash;
            return new ReleaseContractInput
            {
                ProposalId = proposalId,
                ProposedContractInputHash = proposalHash
            };
        }

        private void ApproveByMiner(ContractTester tester, Hash proposalId)
        {
            var miners = tester.GetMiners();
            foreach (var miner in miners)
            {
                tester.ParliamentService.SetAccount(miner);
                var approve =
                    tester.ParliamentService.ExecuteMethodWithResult(ParliamentMethod.Approve, proposalId);
                approve.Status.ShouldBe("MINED");
                if (tester.ParliamentService.CheckProposal(proposalId).ToBeReleased) return;
            }
        }

        private ContractDeploymentInput ContractDeploymentInput(string name)
        {
            var contractReader = new SmartContractReader();
            var codeArray = contractReader.Read(name);

            var input = new ContractDeploymentInput
            {
                Category = KernelHelper.DefaultRunnerCategory,
                Code = ByteString.CopyFrom(codeArray)
            };
            return input;
        }

        private ContractUpdateInput ContractUpdateInput(string name, string contractAddress)
        {
            var contractReader = new SmartContractReader();
            var codeArray = contractReader.Read(name);

            var input = new ContractUpdateInput
            {
                Address = contractAddress.ConvertAddress(),
                Code = ByteString.CopyFrom(codeArray)
            };

            return input;
        }

        private void IssueTokenToMinerThroughOrganization(ContractTester tester, string account, Address organization)
        {
            var symbol = tester.TokenService.GetPrimaryTokenSymbol();
            var miners = tester.GetMiners();
            foreach (var miner in miners)
            {
                var balance = tester.TokenService.GetUserBalance(miner, symbol);
                if (account == miner || balance > 1000_00000000) continue;
                var input = new IssueInput
                {
                    Amount = 1000_00000000,
                    Symbol = symbol,
                    To = miner.ConvertAddress()
                };
                var createProposal = tester.AssociationService.CreateProposal(tester.TokenService.ContractAddress,
                    nameof(TokenMethod.Issue), input, organization, account);
                tester.AssociationService.ApproveWithAssociation(createProposal, organization);
                tester.AssociationService.ReleaseProposal(createProposal, account);
            }
        }

        private TransactionResultDto FindReleaseApprovedUserSmartContractMethod(long startBlock, long currentHeight)
        {
            var releaseTransaction = new TransactionResultDto();
            for (var i = startBlock; i < currentHeight; i++)
            {
                var block = AsyncHelper.RunSync(() => Tester.NodeManager.ApiClient.GetBlockByHeightAsync(i));
                var transactionList = AsyncHelper.RunSync(() =>
                    Tester.NodeManager.ApiClient.GetTransactionResultsAsync(block.BlockHash));
                var find = transactionList.Find(
                    t => t.Transaction.MethodName.Equals("ReleaseApprovedUserSmartContract"));
                releaseTransaction = find ?? releaseTransaction;
            }

            return releaseTransaction;
        }

        private bool CheckProposal(Hash proposalId)
        {
            var proposalInfo = Tester.ParliamentService.CheckProposal(proposalId);
            var checkTimes = 20;
            while (!proposalInfo.ToBeReleased && checkTimes > 0)
            {
                Thread.Sleep(1000);
                proposalInfo = Tester.ParliamentService.CheckProposal(proposalId);
                checkTimes--;
            }

            return proposalInfo.ToBeReleased;
        }

        #endregion
    }
}