using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Acs0;
using Acs3;
using AElfChain.Common.Contracts;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using AElf.Contracts.ParliamentAuth;
using AElf.Kernel;
using AElf.Types;
using AElfChain.SDK.Models;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using log4net;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace AElf.Automation.Contracts.ScenarioTest
{
    [TestClass]
    public class GenesisContractTest
    {
        protected static readonly ILog _logger = Log4NetHelper.GetLogger();
        
        protected ContractTester MainTester;
        protected ContractTester SideTester;
        protected ContractTester SideTester2;
        public INodeManager SideCH { get; set; }
        public INodeManager SideCH2 { get; set; }

        public INodeManager MainCH { get; set; }
        public List<string> UserList { get; set; }
        public string InitAccount { get; } = "28Y8JA1i2cN6oHvdv7EraXJr9a1gY6D1PpJXw9QtRMRwKcBQMK";
        public string Creator { get; } = "28Y8JA1i2cN6oHvdv7EraXJr9a1gY6D1PpJXw9QtRMRwKcBQMK";
        public string OtherAccount { get; } = "h6CRCFAhyozJPwdFRd7i8A5zVAqy171AVty3uMQUQp1MB9AKa";
        private static string MainRpcUrl { get; } = "http://13.53.125.186:8000";
        private static string SideRpcUrl { get; } = "http://13.53.127.35:8000";
        private static string SideRpcUrl2 { get; } = "http://13.48.55.124:8000";

        [TestInitialize]
        public void Initialize()
        {
            #region Basic Preparation

            //Init Logger
            Log4NetHelper.LogInit("ContractTest_");
            
            #endregion

            MainCH = new NodeManager(MainRpcUrl);
            SideCH = new NodeManager(SideRpcUrl, CommonHelper.GetDefaultDataDir());
            SideCH2 = new NodeManager(SideRpcUrl2);

            var mainContractServices = new ContractServices(MainCH, InitAccount, "Main");
            var sideContractServices = new ContractServices(SideCH, InitAccount, "Side");
            var sideContractServices2 = new ContractServices(SideCH2, InitAccount, "Side");

            MainTester = new ContractTester(mainContractServices);
            SideTester = new ContractTester(sideContractServices);
            SideTester2 = new ContractTester(sideContractServices2);
            
        }


//        side-2: 2pNhc2Yz7eUPeD7EKE9QZh7c2XfURryZdLF8gW3giVLgprpcJB
//        side-1: mkGKKat9jBFQa75Ty9QYiUnhssHJifYs9wPNafKZedx1TZx4s
        [TestMethod]
        public void SideChangeOwner()
        {
            var address = SideTester.GenesisService.CallViewMethod<Address>(GenesisMethod.GetContractAuthor,
                AddressHelper.Base58StringToAddress("mkGKKat9jBFQa75Ty9QYiUnhssHJifYs9wPNafKZedx1TZx4s"));
            _logger.Info($"contract owner is {address}");

            SideTester.GenesisService.SetAccount(InitAccount);
            var result = SideTester.GenesisService.ExecuteMethodWithResult(GenesisMethod.ChangeContractAuthor,
                new ChangeContractAuthorInput
                {
                    ContractAddress =
                        AddressHelper.Base58StringToAddress("mkGKKat9jBFQa75Ty9QYiUnhssHJifYs9wPNafKZedx1TZx4s"),
                    NewAuthor =
                        AddressHelper.Base58StringToAddress("W4xEKTZcvPKXRAmdu9xEpM69ArF7gUxDh9MDgtsKnu7JfePXo")
                });
            var address1 = SideTester.GenesisService.CallViewMethod<Address>(GenesisMethod.GetContractAuthor,
                AddressHelper.Base58StringToAddress("mkGKKat9jBFQa75Ty9QYiUnhssHJifYs9wPNafKZedx1TZx4s"));
            _logger.Info($"contract new owner is {address1}");
        }

        [TestMethod]
        public void SideChangeOwnerThroughProposal()
        {
            var address = SideTester.GenesisService.CallViewMethod<Address>(GenesisMethod.GetContractAuthor,
                AddressHelper.Base58StringToAddress("mkGKKat9jBFQa75Ty9QYiUnhssHJifYs9wPNafKZedx1TZx4s"));
            _logger.Info($"contract owner is {address}");
            var organizationAddress = CreateOrganization(SideTester, false, address);

            var changeContractAuthorInput = new ChangeContractAuthorInput
            {
                ContractAddress =
                    AddressHelper.Base58StringToAddress("mkGKKat9jBFQa75Ty9QYiUnhssHJifYs9wPNafKZedx1TZx4s"),
                NewAuthor = AddressHelper.Base58StringToAddress("W4xEKTZcvPKXRAmdu9xEpM69ArF7gUxDh9MDgtsKnu7JfePXo")
            };

            SideTester.GenesisService.SetAccount("W4xEKTZcvPKXRAmdu9xEpM69ArF7gUxDh9MDgtsKnu7JfePXo");
            var proposal = SideTester.ParliamentService.ExecuteMethodWithResult(ParliamentMethod.CreateProposal,
                new CreateProposalInput
                {
                    ContractMethodName = nameof(GenesisMethod.ChangeContractAuthor),
                    ExpiredTime = DateTime.UtcNow.AddDays(1).ToTimestamp(),
                    Params = changeContractAuthorInput.ToByteString(),
                    ToAddress = AddressHelper.Base58StringToAddress(SideTester.GenesisService.ContractAddress),
                    OrganizationAddress = organizationAddress
                });
            var proposalId = proposal.ReadableReturnValue.Replace("\"", "");
            SideTester.GenesisService.SetAccount(InitAccount);
            var approve =
                SideTester.ParliamentService.ExecuteMethodWithResult(ParliamentMethod.Approve, new ApproveInput
                {
                    ProposalId = HashHelper.HexStringToHash(proposalId)
                });
            var approveResult = approve.ReadableReturnValue;

            approveResult.ShouldBe("true");

            SideTester.GenesisService.SetAccount("W4xEKTZcvPKXRAmdu9xEpM69ArF7gUxDh9MDgtsKnu7JfePXo");
            var result =
                SideTester.ParliamentService.ExecuteMethodWithResult(ParliamentMethod.Release,
                    HashHelper.HexStringToHash(proposalId));
            result.Status.ShouldBe("FAILED");
        }


        [TestMethod]
        public void MainChangeContractOwner()
        {
            var contractOwnerAddress =
                MainTester.GenesisService.CallViewMethod<Address>(GenesisMethod.GetContractAuthor,
                    AddressHelper.Base58StringToAddress("x7G7VYqqeVAH8aeAsb7gYuTQ12YS1zKuxur9YES3cUj72QMxJ"));
            _logger.Info($"contract owner address is {contractOwnerAddress}");

            var organizationAddress =
                MainTester.ParliamentService.CallViewMethod<Address>(ParliamentMethod.GetDefaultOrganizationAddress,
                    new Empty());
            _logger.Info($"organization address is {organizationAddress} ");

            var result =
                MainTester.GenesisService.ExecuteMethodWithResult(GenesisMethod.ChangeContractAuthor,
                    AddressHelper.Base58StringToAddress("2RCLmZQ2291xDwSbDEJR6nLhFJcMkyfrVTq1i1YxWC4SdY49a6"));
        }

        [TestMethod]
        public void MainChangeZeroOwner()
        {
            var input = AddressHelper.Base58StringToAddress(InitAccount);
            var organizationAddress =
                MainTester.ParliamentService.CallViewMethod<Address>(ParliamentMethod.GetDefaultOrganizationAddress,
                    new Empty());
            _logger.Info($"organization address is {organizationAddress} ");

            var contractOwnerAddress =
                MainTester.GenesisService.CallViewMethod<Address>(GenesisMethod.GetContractAuthor,
                    AddressHelper.Base58StringToAddress("x7G7VYqqeVAH8aeAsb7gYuTQ12YS1zKuxur9YES3cUj72QMxJ"));
            _logger.Info($"contract owner address is {contractOwnerAddress}");


            var proposal = MainTester.ParliamentService.ExecuteMethodWithResult(ParliamentMethod.CreateProposal,
                new CreateProposalInput
                {
                    ContractMethodName = nameof(GenesisMethod.ChangeGenesisOwner),
                    ExpiredTime = DateTime.UtcNow.AddDays(1).ToTimestamp(),
                    Params = input.ToByteString(),
                    ToAddress = AddressHelper.Base58StringToAddress(MainTester.GenesisService.ContractAddress),
                    OrganizationAddress = organizationAddress
                });
            var proposalId = proposal.ReadableReturnValue.Replace("\"", "");

            MainTester.ParliamentService.SetAccount("2RCLmZQ2291xDwSbDEJR6nLhFJcMkyfrVTq1i1YxWC4SdY49a6");
            var approve =
                MainTester.ParliamentService.ExecuteMethodWithResult(ParliamentMethod.Approve, new ApproveInput
                {
                    ProposalId = HashHelper.HexStringToHash(proposalId)
                });
            var approveResult = approve.ReadableReturnValue;

            approveResult.ShouldBe("true");

            MainTester.GenesisService.SetAccount(InitAccount);
            var result =
                MainTester.ParliamentService.CallViewMethod<Address>(ParliamentMethod.GetDefaultOrganizationAddress,
                    new Empty());
            _logger.Info($"organization address is {result} ");
        }


        [TestMethod]
        public void MainChangeZeroOwnerByUser()
        {
            ;
            var organizationAddress =
                MainTester.ParliamentService.CallViewMethod<Address>(ParliamentMethod.GetDefaultOrganizationAddress,
                    new Empty());
            _logger.Info($"organization address is {organizationAddress} ");
            var input = organizationAddress;

            var contractOwnerAddress =
                MainTester.GenesisService.CallViewMethod<Address>(GenesisMethod.GetContractAuthor,
                    AddressHelper.Base58StringToAddress("x7G7VYqqeVAH8aeAsb7gYuTQ12YS1zKuxur9YES3cUj72QMxJ"));
            _logger.Info($"contract owner address is {contractOwnerAddress}");


            var result = MainTester.GenesisService.ExecuteMethodWithResult(GenesisMethod.ChangeGenesisOwner, input);
            var account =
                MainTester.ParliamentService.CallViewMethod<Address>(ParliamentMethod.GetDefaultOrganizationAddress,
                    new Empty());
            _logger.Info($"organization address is {account} ");
        }


        [TestMethod]
        public void SideChangeZeroOwner()
        {
            var input = AddressHelper.Base58StringToAddress(InitAccount);
            var organizationAddress =
                SideTester.ParliamentService.CallViewMethod<Address>(ParliamentMethod.GetDefaultOrganizationAddress,
                    new Empty());
            _logger.Info($"organization address is {organizationAddress} ");

            var contractOwnerAddress =
                SideTester.GenesisService.CallViewMethod<Address>(GenesisMethod.GetContractAuthor,
                    AddressHelper.Base58StringToAddress("x7G7VYqqeVAH8aeAsb7gYuTQ12YS1zKuxur9YES3cUj72QMxJ"));
            _logger.Info($"contract owner address is {contractOwnerAddress}");


            var proposal = SideTester.ParliamentService.ExecuteMethodWithResult(ParliamentMethod.CreateProposal,
                new CreateProposalInput
                {
                    ContractMethodName = nameof(TokenMethod.Transfer),
                    ExpiredTime = DateTime.UtcNow.AddDays(1).ToTimestamp(),
                    Params = input.ToByteString(),
                    ToAddress =
                        AddressHelper.Base58StringToAddress("x7G7VYqqeVAH8aeAsb7gYuTQ12YS1zKuxur9YES3cUj72QMxJ"),
                    OrganizationAddress = organizationAddress
                });
            var proposalId = proposal.ReadableReturnValue.Replace("\"", "");

            SideTester.ParliamentService.SetAccount("2RCLmZQ2291xDwSbDEJR6nLhFJcMkyfrVTq1i1YxWC4SdY49a6");
            var approve =
                SideTester.ParliamentService.ExecuteMethodWithResult(ParliamentMethod.Approve, new ApproveInput
                {
                    ProposalId = HashHelper.HexStringToHash(proposalId)
                });
            var approveResult = approve.ReadableReturnValue;

            approveResult.ShouldBe("true");

            SideTester.GenesisService.SetAccount(InitAccount);
            var result = SideTester.GenesisService.ExecuteMethodWithResult(GenesisMethod.ChangeGenesisOwner,
                AddressHelper.Base58StringToAddress("2RCLmZQ2291xDwSbDEJR6nLhFJcMkyfrVTq1i1YxWC4SdY49a6"));
            result.Status.ShouldBe("MINED");
        }

        #region SideChain IsAuthoiryRequired == true; IsPrivilegePreserved == true;

        // Deploy contract should use create proposal through GenesisOwnerAddress;
        // Only creator and miner can create this proposal.

        [TestMethod]
        public void SideChainAuthDeploySystemContract_UserDeploy()
        {
            var contractReader = new SmartContractReader();
            var codeArray = contractReader.Read("AElf.Contracts.MultiToken");

            var result = SideTester2.GenesisService.ExecuteMethodWithResult(GenesisMethod.DeploySystemSmartContract,
                new SystemContractDeploymentInput
                {
                    Code = ByteString.CopyFrom(codeArray),
                    Category = KernelConstants.DefaultRunnerCategory,
                    TransactionMethodCallList =
                        new SystemContractDeploymentInput.Types.SystemTransactionMethodCallList()
                });
            result.Status.ShouldBe("FAILED");
            result.Error.Contains("Unauthorized behavior.").ShouldBeTrue();
        }

        [TestMethod]
        public void SideChainAuthUpdateContract_UserUpdate()
        {
            var input = ContractUpdateInput("AElf.Contracts.Vote", SideTester2.TokenService.ContractAddress);
            var organization = GetGenesisOwnerAddress(SideTester2);
            var proposal = CreateProposal(SideTester2, InitAccount, GenesisMethod.UpdateSmartContract, input,
                organization);
            Approve(SideTester2, proposal);
            var release = Release(SideTester, proposal, InitAccount);
            release.Status.ShouldBe("FAILED");
            release.Error.Contains("Unauthorized behavior.").ShouldBeTrue();
        }

        [TestMethod]
        public void SideChainAuthDeploySystemContract_MinerCreate_OtherOrganizationDeploy()
        {
            var input = ContractDeploymentInput("AElf.Contracts.MultiToken");
            var organization = CreateOrganization(SideTester2, false, AddressHelper.Base58StringToAddress(InitAccount));
            var proposal = CreateProposal(SideTester2, InitAccount, GenesisMethod.DeploySmartContract, input,
                organization);
            Approve(SideTester2, proposal);
            var release = Release(SideTester2, proposal, InitAccount);
            release.Status.ShouldBe("FAILED");
            release.Error.Contains("Unauthorized behavior.").ShouldBeTrue();
        }

        [TestMethod]
        public void SideChainAuthUpdateContract_MinerCreate_OtherOrganizationUpdate()
        {
            var input = ContractUpdateInput("AElf.Contracts.MultiToken", SideTester2.TokenService.ContractAddress);
            var organization = CreateOrganization(SideTester2, false, AddressHelper.Base58StringToAddress(InitAccount));
            var proposal = CreateProposal(SideTester2, InitAccount, GenesisMethod.UpdateSmartContract, input,
                organization);
            Approve(SideTester2, proposal);
            var release = Release(SideTester2, proposal, InitAccount);
            release.Status.ShouldBe("FAILED");
            release.Error.Contains("Unauthorized behavior.").ShouldBeTrue();
        }

        [TestMethod]
        public void OtherAccount_UseGenesisOwnerAddress_CreateProposal()
        {
            var input = ContractUpdateInput("AElf.Contracts.MultiToken", SideTester2.TokenService.ContractAddress);
            var organization = GetGenesisOwnerAddress(SideTester2);
            SideTester2.ParliamentService.SetAccount(OtherAccount);
            var proposal = SideTester2.ParliamentService.ExecuteMethodWithResult(ParliamentMethod.CreateProposal,
                new CreateProposalInput
                {
                    ContractMethodName = nameof(GenesisMethod.UpdateSmartContract),
                    ExpiredTime = DateTime.UtcNow.AddDays(1).ToTimestamp(),
                    Params = input.ToByteString(),
                    ToAddress = AddressHelper.Base58StringToAddress(SideTester2.GenesisService.ContractAddress),
                    OrganizationAddress = organization
                });
            proposal.Status.ShouldBe("FAILED");
        }

        [TestMethod]
        public void OtherAccount_UseOtherOrganization_UseGenesisOwnerAddress_CreateProposal()
        {
            var input = ContractUpdateInput("AElf.Contracts.MultiToken", SideTester2.TokenService.ContractAddress);
            var organization = CreateOrganization(SideTester2, false, AddressHelper.Base58StringToAddress(InitAccount));
            SideTester2.ParliamentService.SetAccount(OtherAccount);
            var proposal = SideTester2.ParliamentService.ExecuteMethodWithResult(ParliamentMethod.CreateProposal,
                new CreateProposalInput
                {
                    ContractMethodName = nameof(GenesisMethod.UpdateSmartContract),
                    ExpiredTime = DateTime.UtcNow.AddDays(1).ToTimestamp(),
                    Params = input.ToByteString(),
                    ToAddress = AddressHelper.Base58StringToAddress(SideTester2.GenesisService.ContractAddress),
                    OrganizationAddress = organization
                });
            proposal.Status.ShouldBe("MINED");
        }

        [TestMethod]
        public void SideChainAuthDeploySystemContract_UseGenesisOwnerAddress_MinerCreate_Success()
        {
            var input = ContractDeploymentInput("AElf.Contracts.MultiToken");
            var organization = GetGenesisOwnerAddress(SideTester2);
            var proposal = CreateProposal(SideTester2, InitAccount, GenesisMethod.DeploySmartContract, input,
                organization);
            Approve(SideTester2, proposal);
            var release = Release(SideTester2, proposal, InitAccount);
            release.Status.ShouldBe("MINED");
            var byteString = ByteString.FromBase64(release.Logs[0].NonIndexed);
            var deployAddress = ContractDeployed.Parser.ParseFrom(byteString).Address;
            _logger.Info($"{deployAddress}");
        }
        
        [TestMethod]
        public void SideChainProposalDeploy_GenesisOwnerAddress_MinerCreate_Success()
        {
            SideTester.IssueTokenToMiner(Creator);
            SideTester.IssueToken(Creator,OtherAccount);
            var input = ContractDeploymentInput("AElf.Contracts.MultiToken");
            var organization = GetGenesisOwnerAddress(SideTester);
            var proposal = CreateProposal(SideTester, Creator, GenesisMethod.ProposeNewContract, input,
                organization);
            Approve(SideTester, proposal);
            var release = Release(SideTester, proposal, Creator);
            release.Status.ShouldBe("MINED");
            var byteString = ByteString.FromBase64(release.Logs.Last().NonIndexed);
            var deployProposal = ProposalCreated.Parser.ParseFrom(byteString).ProposalId;
            var byteString2 = ByteString.FromBase64(release.Logs[1].NonIndexed);
            var proposedContractInputHash = CodeCheckRequired.Parser.ParseFrom(byteString2).ProposedContractInputHash;
            _logger.Info($"{deployProposal}\n {proposedContractInputHash}");
        }
        
        [TestMethod]
        public void SideChainProposalUpdate_GenesisOwnerAddress_MinerCreate_Success()
        {
            SideTester.IssueTokenToMiner(Creator);
            SideTester.IssueToken(Creator,OtherAccount);
            var input = ContractUpdateInput("AElf.Contracts.Election",SideTester.AssociationService.ContractAddress);
            var organization = GetGenesisOwnerAddress(SideTester);
            var proposal = CreateProposal(SideTester, Creator, GenesisMethod.ProposeUpdateContract, input,
                organization);
            Approve(SideTester, proposal);
            var release = Release(SideTester, proposal, Creator);
            release.Status.ShouldBe("MINED");
            var byteString = ByteString.FromBase64(release.Logs.Last().NonIndexed);
            var deployProposal = ProposalCreated.Parser.ParseFrom(byteString).ProposalId;
            var byteString2 = ByteString.FromBase64(release.Logs[1].NonIndexed);
            var proposedContractInputHash = CodeCheckRequired.Parser.ParseFrom(byteString2).ProposedContractInputHash;
            _logger.Info($"{deployProposal}\n {proposedContractInputHash}");
        }
        
        [TestMethod]
        [DataRow("4c163214134a39313dde81df9c77ed98583a2e1af8b9e6d5180d38da18514eed","edcd361ce28618d02a1bd33b8729010de6ff746b3548b7c699c32b1ad8e5a6ac")]
        public void SideReleaseApprovedContract(string proposal, string hash)
        {
            var proposalId = HashHelper.HexStringToHash(proposal);
            var proposalHash = HashHelper.HexStringToHash(hash);
            var releaseApprovedContractInput = new ReleaseApprovedContractInput()
            {
                ProposedContractInputHash = proposalHash,
                ProposalId = proposalId
            };

            var release = ReleaseApprove(SideTester,releaseApprovedContractInput, Creator);
            release.Status.ShouldBe("MINED");
            var byteString = ByteString.FromBase64(release.Logs.Last().NonIndexed);
            var deployAddress = ContractDeployed.Parser.ParseFrom(byteString).Address;
            _logger.Info($"{deployAddress}");
        }
        
        #endregion

        #region SideChain2 IsAuthoiryRequired == true; IsPrivilegePreserved == false;

        [TestMethod]
        public void SideChain2ProposalDeploy_GenesisOwnerAddress_MinerCreate_Success()
        {
            SideTester2.IssueTokenToMiner(Creator);
            SideTester2.IssueToken(Creator,OtherAccount);
            var input = ContractDeploymentInput("AElf.Contracts.Election");
            var organization = GetGenesisOwnerAddress(SideTester2);
            var proposal = CreateProposal(SideTester2, OtherAccount, GenesisMethod.ProposeNewContract, input,
                organization);
            Approve(SideTester2, proposal);
            var release = Release(SideTester2, proposal, OtherAccount);
            release.Status.ShouldBe("MINED");
            var byteString = ByteString.FromBase64(release.Logs.Last().NonIndexed);
            var deployProposal = ProposalCreated.Parser.ParseFrom(byteString).ProposalId;
            var byteString2 = ByteString.FromBase64(release.Logs[1].NonIndexed);
            var proposedContractInputHash = CodeCheckRequired.Parser.ParseFrom(byteString2).ProposedContractInputHash;
            _logger.Info($"{deployProposal}\n {proposedContractInputHash}");
        }
        
        [TestMethod]
        public void SideChain2ProposalUpdate_GenesisOwnerAddress_MinerCreate_Success()
        {
            SideTester2.IssueTokenToMiner(Creator);
            SideTester2.IssueToken(Creator,OtherAccount);
            var input = ContractUpdateInput("AElf.Contracts.ParliamentAuth","2RXWKnH1perc3pANSefCBGEMio6WihCqWQnX8Sx1NNVnVQSzJf");
            var organization = GetGenesisOwnerAddress(SideTester2);
            var proposal = CreateProposal(SideTester2, OtherAccount, GenesisMethod.ProposeUpdateContract, input,
                organization);
            Approve(SideTester2, proposal);
            var release = Release(SideTester2, proposal, OtherAccount);
            release.Status.ShouldBe("MINED");
            var byteString = ByteString.FromBase64(release.Logs.Last().NonIndexed);
            var deployProposal = ProposalCreated.Parser.ParseFrom(byteString).ProposalId;
            var byteString2 = ByteString.FromBase64(release.Logs[1].NonIndexed);
            var proposedContractInputHash = CodeCheckRequired.Parser.ParseFrom(byteString2).ProposedContractInputHash;
            _logger.Info($"{deployProposal}\n {proposedContractInputHash}");
        }

        [TestMethod]
        [DataRow("c961fbc4ec8fc1c8c016c993153e106e67ff1d7d1b3e51b21ef9a217fd0ad4fe","edcd361ce28618d02a1bd33b8729010de6ff746b3548b7c699c32b1ad8e5a6ac")]
        public void Side2ReleaseApprovedContract(string proposal, string hash)
        {
            var proposalId = HashHelper.HexStringToHash(proposal);
            var proposalHash = HashHelper.HexStringToHash(hash);
            var releaseApprovedContractInput = new ReleaseApprovedContractInput()
            {
                ProposedContractInputHash = proposalHash,
                ProposalId = proposalId
            };

            var release = ReleaseApprove(SideTester2,releaseApprovedContractInput, OtherAccount);
            release.Status.ShouldBe("MINED");
            var byteString = ByteString.FromBase64(release.Logs.Last().NonIndexed);
            var deployAddress = ContractDeployed.Parser.ParseFrom(byteString).Address;
            _logger.Info($"{deployAddress}");
        }
        
        [TestMethod]
        [DataRow("b35ae792214ff60afbb37a4c9764f58f82918b3cca3e5b1ea77535bdb3614a6f","8182e7e35e03cdff96ca958ffff69dbde8de8fe2ac8e8c40656cdec3682aaef5")]
        public void Side2ReleaseUpdateApprovedContract(string proposal, string hash)
        {
            var proposalId = HashHelper.HexStringToHash(proposal);
            var proposalHash = HashHelper.HexStringToHash(hash);
            var releaseApprovedContractInput = new ReleaseApprovedContractInput()
            {
                ProposedContractInputHash = proposalHash,
                ProposalId = proposalId
            };

            var release = ReleaseApprove(SideTester2,releaseApprovedContractInput, OtherAccount);
            release.Status.ShouldBe("MINED");
            var byteString = ByteString.FromBase64(release.Logs.Last().Indexed.First());
            var deployAddress = CodeUpdated.Parser.ParseFrom(byteString).Address;
            _logger.Info($"{deployAddress}");
        }
        

        [TestMethod]
        public void SideChainAuthDeploySystemContract_CreatorCreate_Success()
        {
            var input = ContractDeploymentInput("AElf.Contracts.MultiToken");
            var organization = GetGenesisOwnerAddress(SideTester2);
            var proposal = CreateProposal(SideTester2, Creator, GenesisMethod.DeploySmartContract, input, organization);
            Approve(SideTester2, proposal);
            var release = Release(SideTester2, proposal, Creator);
            release.Status.ShouldBe("MINED");
            var byteString = ByteString.FromBase64(release.Logs[0].NonIndexed);
            var deployAddress = ContractDeployed.Parser.ParseFrom(byteString).Address;
            _logger.Info($"{deployAddress}");
        }

        [TestMethod]
        public void SideChainAuthUpdateContract_MinerCreate_Success()
        {
            var input = ContractUpdateInput("AElf.Contracts.MultiToken", SideTester2.TokenService.ContractAddress);
            var organization = GetGenesisOwnerAddress(SideTester2);
            var proposal = CreateProposal(SideTester2, InitAccount, GenesisMethod.UpdateSmartContract, input,
                organization);
            Approve(SideTester2, proposal);
            var release = Release(SideTester2, proposal, InitAccount);
            release.Status.ShouldBe("MINED");

            var contractAddress =
                CodeUpdated.Parser.ParseFrom(ByteString.FromBase64(release.Logs[0].Indexed[0])).Address;
            contractAddress.ShouldBe(AddressHelper.Base58StringToAddress(SideTester2.TokenService.ContractAddress));
            var codeHash = Hash.FromRawBytes(input.Code.ToByteArray());
            var newHash = CodeUpdated.Parser.ParseFrom(ByteString.FromBase64(release.Logs[0].NonIndexed)).NewCodeHash;
            newHash.ShouldBe(codeHash);
        }

        [TestMethod]
        public void SideChainAuthUpdateContract_CreatorCreate_Success()
        {
            var input = ContractUpdateInput("AElf.Contracts.MultiToken", SideTester2.TokenService.ContractAddress);
            var organization = GetGenesisOwnerAddress(SideTester2);
            var proposal = CreateProposal(SideTester2, Creator, GenesisMethod.UpdateSmartContract, input, organization);
            Approve(SideTester2, proposal);
            var release = Release(SideTester2, proposal, Creator);
            release.Status.ShouldBe("MINED");

            var contractAddress =
                CodeUpdated.Parser.ParseFrom(ByteString.FromBase64(release.Logs[0].Indexed[0])).Address;
            contractAddress.ShouldBe(AddressHelper.Base58StringToAddress(SideTester2.TokenService.ContractAddress));
            var codeHash = Hash.FromRawBytes(input.Code.ToByteArray());
            var newHash = CodeUpdated.Parser.ParseFrom(ByteString.FromBase64(release.Logs[0].NonIndexed)).NewCodeHash;
            newHash.ShouldBe(codeHash);
        }

        #endregion

        #region Main Chain IsAuthoiryRequired == true; IsPrivilegePreserved == false;

        [TestMethod]
        public void MainDeploySmartContract_UserDeploy()
        {
            var contractReader = new SmartContractReader();
            var codeArray = contractReader.Read("AElf.Contracts.MultiToken");

            var result = MainTester.GenesisService.ExecuteMethodWithResult(GenesisMethod.DeploySystemSmartContract,
                new SystemContractDeploymentInput
                {
                    Code = ByteString.CopyFrom(codeArray),
                    Category = KernelConstants.DefaultRunnerCategory,
                    TransactionMethodCallList =
                        new SystemContractDeploymentInput.Types.SystemTransactionMethodCallList()
                });
            result.Status.ShouldBe("FAILED");
            result.Error.Contains("Unauthorized behavior.").ShouldBeTrue();
        }

        [TestMethod]
        public void MainDeploySmartContract_ThroughOtherOrganization()
        {
            var input = ContractDeploymentInput("AElf.Contracts.MultiToken");
            var organization = CreateOrganization(MainTester, false, AddressHelper.Base58StringToAddress(InitAccount));
            var proposal = CreateProposal(MainTester, InitAccount, GenesisMethod.DeploySmartContract, input,
                organization);
            Approve(MainTester, proposal);
            var release = Release(MainTester, proposal, InitAccount);
            release.Status.ShouldBe("FAILED");
            release.Error.Contains("Unauthorized behavior.").ShouldBeTrue();
        }


        [TestMethod]
        public void Main_OtherAccount_UseGenesisOwnerAddress_CreateProposal()
        {
            var input = ContractUpdateInput("AElf.Contracts.MultiToken", MainTester.TokenService.ContractAddress);
            var organization = GetGenesisOwnerAddress(MainTester);
            SideTester2.ParliamentService.SetAccount(OtherAccount);
            var proposal = MainTester.ParliamentService.ExecuteMethodWithResult(ParliamentMethod.CreateProposal,
                new CreateProposalInput
                {
                    ContractMethodName = nameof(GenesisMethod.UpdateSmartContract),
                    ExpiredTime = DateTime.UtcNow.AddDays(1).ToTimestamp(),
                    Params = input.ToByteString(),
                    ToAddress = AddressHelper.Base58StringToAddress(MainTester.GenesisService.ContractAddress),
                    OrganizationAddress = organization
                });
            proposal.Status.ShouldBe("MINED");
        }

        [TestMethod]
        public void MainChainDeploySystemContract_UseGenesisOwnerAddress_MinerCreate_Success()
        {
            var input = ContractDeploymentInput("AElf.Contracts.MultiToken");
            var organization = GetGenesisOwnerAddress(MainTester);
            var proposal = CreateProposal(MainTester, InitAccount, GenesisMethod.DeploySmartContract, input,
                organization);
            Approve(MainTester, proposal);
            var release = Release(MainTester, proposal, InitAccount);
            release.Status.ShouldBe("MINED");
            var byteString = ByteString.FromBase64(release.Logs.Last().NonIndexed);
            var deployAddress = ContractDeployed.Parser.ParseFrom(byteString).Address;
            _logger.Info($"{deployAddress}");
        }
        
        [TestMethod]
        public void MainChainProposalDeploy_GenesisOwnerAddress_MinerCreate_Success()
        {
            MainTester.TransferTokenToMiner(InitAccount);
            var input = ContractDeploymentInput("AElf.Contracts.MultiToken");
            var organization = GetGenesisOwnerAddress(MainTester);
            var proposal = CreateProposal(MainTester, InitAccount, GenesisMethod.ProposeNewContract, input,
                organization);
            Approve(MainTester, proposal);
            var release = Release(MainTester, proposal, InitAccount);
            release.Status.ShouldBe("MINED");
            var byteString = ByteString.FromBase64(release.Logs.Last().NonIndexed);
            var deployProposal = ProposalCreated.Parser.ParseFrom(byteString).ProposalId;
            var byteString2 = ByteString.FromBase64(release.Logs[1].NonIndexed);
            var proposedContractInputHash = CodeCheckRequired.Parser.ParseFrom(byteString2).ProposedContractInputHash;
            _logger.Info($"{deployProposal}\n {proposedContractInputHash}");
        }
        
        [TestMethod]
        public void MainChainProposalUpdate_GenesisOwnerAddress_MinerCreate_Success()
        {
            MainTester.TransferTokenToMiner(InitAccount);
            MainTester.TransferToken(OtherAccount);
            var input = ContractUpdateInput("AElf.Contracts.Election",MainTester.AssociationService.ContractAddress);
            var organization = GetGenesisOwnerAddress(MainTester);
            var proposal = CreateProposal(MainTester, OtherAccount, GenesisMethod.ProposeUpdateContract, input,
                organization);
            Approve(MainTester, proposal);
            var release = Release(MainTester, proposal, OtherAccount);
            release.Status.ShouldBe("MINED");
            var byteString = ByteString.FromBase64(release.Logs.Last().NonIndexed);
            var deployProposal = ProposalCreated.Parser.ParseFrom(byteString).ProposalId;
            var byteString2 = ByteString.FromBase64(release.Logs[1].NonIndexed);
            var proposedContractInputHash = CodeCheckRequired.Parser.ParseFrom(byteString2).ProposedContractInputHash;
            _logger.Info($"{deployProposal}\n {proposedContractInputHash}");
        }
        
        
        [TestMethod]
        [DataRow("GiIKIKOmGZC08DAoVVq4bnxr6WsfKAUpflGo1WLHAKS9g+SD")]
        public void test(string input)
        {
            var byteString = ByteString.FromBase64(input);
            var deployProposal = ContractDeployed.Parser.ParseFrom(byteString).Address;
            _logger.Info($"{deployProposal}\n");
        }

        [TestMethod]
        [DataRow("09f05afc7b3d94391afd7e3fd168ec592d562237451f1af142cbbc6e68d7192b","53c38d25cdf9f359af3c64e66b0c5de542011d8cb115f8415cf7f7564f99763c")]
        public void ReleaseApprovedContract(string proposal, string hash)
        {
            var proposalId = HashHelper.HexStringToHash(proposal);
            var proposalHash = HashHelper.HexStringToHash(hash);
            var releaseApprovedContractInput = new ReleaseApprovedContractInput()
            {
                ProposedContractInputHash = proposalHash,
                ProposalId = proposalId
            };

            var release = ReleaseApprove(MainTester,releaseApprovedContractInput, InitAccount);
            release.Status.ShouldBe("MINED");
            var byteString = ByteString.FromBase64(release.Logs.Last().NonIndexed);
            var deployAddress = ContractDeployed.Parser.ParseFrom(byteString).Address;
            _logger.Info($"{deployAddress}");
        }
        
        [TestMethod]
        [DataRow("632e7545fd2daec80e346b0af1d40e48902cab668065a2af1e2383556629bf5f","394b36258126612b04b2e4a2b47cfd7c469468abfc04a76bbbf3f6f4b292b6bb")]
        public void ReleaseApprovedUpdateContract(string proposal, string hash)
        {
            var proposalId = HashHelper.HexStringToHash(proposal);
            var proposalHash = HashHelper.HexStringToHash(hash);
            var releaseApprovedContractInput = new ReleaseApprovedContractInput()
            {
                ProposedContractInputHash = proposalHash,
                ProposalId = proposalId
            };

            var release = ReleaseApprove(MainTester,releaseApprovedContractInput, InitAccount);
            release.Status.ShouldBe("MINED");
            var byteString = ByteString.FromBase64(release.Logs.Last().Indexed.First());
            var deployAddress = CodeUpdated.Parser.ParseFrom(byteString).Address;
            _logger.Info($"{deployAddress}");
        }
        

        [TestMethod]
        [DataRow("09f05afc7b3d94391afd7e3fd168ec592d562237451f1af142cbbc6e68d7192b")]
        public void CheckProposal(string proposalId)
        {
            var proposal = HashHelper.HexStringToHash(proposalId);
            var result = SideTester.ParliamentService.CallViewMethod<ProposalOutput>(ParliamentMethod.GetProposal,
                proposal);
            _logger.Info($"{result.ToBeReleased}");
            _logger.Info($"{result.ExpiredTime}");
        }

        [TestMethod]
        public void GetBalance()
        {
            var miners = MainTester.GetMiners();
            foreach (var miner in miners)
            {
                var balance = MainTester.TokenService.GetUserBalance(miner);
                _logger.Info($"{balance}");
            }
        }

        #endregion

        #region private method

        private Address CreateOrganization(ContractTester tester, bool isAuthority, Address proposer)
        {
            var address = tester.ParliamentService.ExecuteMethodWithResult(ParliamentMethod.CreateOrganization,
                new CreateOrganizationInput
                {
                    ReleaseThreshold = 6666
                });
            var organization = address.ReadableReturnValue.Replace("\"", "");
            return AddressHelper.Base58StringToAddress(organization);
        }

        private Address GetGenesisOwnerAddress(ContractTester tester)
        {
            return tester.ParliamentService.CallViewMethod<Address>(ParliamentMethod.GetDefaultOrganizationAddress,
                new Empty());
        }

        private Hash CreateProposal(ContractTester tester, string account, GenesisMethod method, IMessage input,
            Address organizationAddress)
        {
            tester.ParliamentService.SetAccount(account);
            var proposal = tester.ParliamentService.ExecuteMethodWithResult(ParliamentMethod.CreateProposal,
                new CreateProposalInput
                {
                    ContractMethodName = method.ToString(),
                    ExpiredTime = DateTime.UtcNow.AddDays(1).ToTimestamp(),
                    Params = input.ToByteString(),
                    ToAddress = AddressHelper.Base58StringToAddress(tester.GenesisService.ContractAddress),
                    OrganizationAddress = organizationAddress
                });
            var proposalId = proposal.ReadableReturnValue.Replace("\"", "");
            return HashHelper.HexStringToHash(proposalId);
        }

        private void Approve(ContractTester tester, Hash proposalId)
        {
            var miners = tester.GetMiners();
            foreach (var miner in miners)
            {
                tester.ParliamentService.SetAccount(miner);
                var approve =
                    tester.ParliamentService.ExecuteMethodWithResult(ParliamentMethod.Approve, new ApproveInput
                    {
                        ProposalId = proposalId
                    });
                var approveResult = approve.ReadableReturnValue;

                approveResult.ShouldBe("true");
                if (tester.ParliamentService.CheckProposal(proposalId).ToBeReleased) break;
            }
        }

        private TransactionResultDto Release(ContractTester tester, Hash proposalId, string account)
        {
            tester.ParliamentService.SetAccount(account);
            var result =
                tester.ParliamentService.ExecuteMethodWithResult(ParliamentMethod.Release,
                    proposalId);
            return result;
        }
        
        private TransactionResultDto ReleaseApprove(ContractTester tester,ReleaseApprovedContractInput input , string account)
        {
            tester.GenesisService.SetAccount(account);
            var result =
                tester.GenesisService.ExecuteMethodWithResult(GenesisMethod.ReleaseApprovedContract, new ReleaseApprovedContractInput
                {
                    ProposalId = input.ProposalId,
                    ProposedContractInputHash = input.ProposedContractInputHash
                });
            return result;
        }

        private ContractDeploymentInput ContractDeploymentInput(string name)
        {
            var contractReader = new SmartContractReader();
            var codeArray = contractReader.Read(name);

            var input = new ContractDeploymentInput
            {
                Category = KernelConstants.DefaultRunnerCategory,
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
                Address = AddressHelper.Base58StringToAddress(contractAddress),
                Code = ByteString.CopyFrom(codeArray)
            };

            return input;
        }

        #endregion
    }
}