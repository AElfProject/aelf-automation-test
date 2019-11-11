using System;
using System.Collections.Generic;
using System.IO;
using Acs0;
using Acs3;
using AElfChain.Common.Contracts;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using AElf.Contracts.ParliamentAuth;
using AElf.Kernel;
using AElf.Types;
using AElfChain.Common.Managers;
using AElfChain.SDK.Models;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace AElf.Automation.Contracts.ScenarioTest
{
    [TestClass]
    public class GenesisContractTest
    {
        private readonly ILogHelper _logger = LogHelper.GetLogger();
        protected ContractTester MainTester;
        protected ContractTester SideTester;
        protected ContractTester SideTester2;
        public INodeManager SideCH { get; set; }
        public INodeManager SideCH2 { get; set; }

        public INodeManager MainCH { get; set; }
        public List<string> UserList { get; set; }
        public string InitAccount { get; } = "2RCLmZQ2291xDwSbDEJR6nLhFJcMkyfrVTq1i1YxWC4SdY49a6";
        public string Creator { get; } = "W4xEKTZcvPKXRAmdu9xEpM69ArF7gUxDh9MDgtsKnu7JfePXo";
        public string OtherAccount { get; } = "28qLVdGMokanMAp9GwfEqiWnzzNifh8LS9as6mzJFX1gQBB823";
        private static string MainRpcUrl { get; } = "http://192.168.197.56:8001";
        private static string SideRpcUrl { get; } = "http://127.0.0.1:9001";
        private static string SideRpcUrl2 { get; } = "http://127.0.0.1:9002";

        [TestInitialize]
        public void Initialize()
        {
            #region Basic Preparation

            //Init Logger
            var logName = "ContractTest_" + DateTime.Now.ToString("MMddHHmmss") + ".log";
            var dir = Path.Combine(CommonHelper.AppRoot, "logs", logName);
            _logger.InitLogHelper(dir);

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

        #region SideChain2 IsAuthoiryRequired == true; IsPrivilegePreserved == true;

        // Deploy contract should use create proposal through GenesisOwnerAddress;
        // Only creator and miner can create this proposal.

        [TestMethod]
        public void SideChainAuthDeploySystemContract_UserDeploy()
        {
            var contractReader = new SmartContractReader();
            var codeArray = contractReader.Read("AElf.Contracts.MultiToken");

            var result = SideTester2.GenesisService.ExecuteMethodWithResult(GenesisMethod.DeploySystemSmartContract,
                new SystemContractDeploymentInput()
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
            Approve(SideTester2, InitAccount, proposal);
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
            Approve(SideTester2, InitAccount, proposal);
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
            Approve(SideTester2, InitAccount, proposal);
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
            Approve(SideTester2, InitAccount, proposal);
            var release = Release(SideTester2, proposal, InitAccount);
            release.Status.ShouldBe("MINED");
            var byteString = ByteString.FromBase64(release.Logs[0].NonIndexed);
            var deployAddress = ContractDeployed.Parser.ParseFrom(byteString).Address;
            _logger.Info($"{deployAddress}");
        }

        [TestMethod]
        public void SideChainAuthDeploySystemContract_CreatorCreate_Success()
        {
            var input = ContractDeploymentInput("AElf.Contracts.MultiToken");
            var organization = GetGenesisOwnerAddress(SideTester2);
            var proposal = CreateProposal(SideTester2, Creator, GenesisMethod.DeploySmartContract, input, organization);
            Approve(SideTester2, InitAccount, proposal);
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
            Approve(SideTester2, InitAccount, proposal);
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
            Approve(SideTester2, InitAccount, proposal);
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
                new SystemContractDeploymentInput()
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
            Approve(MainTester, InitAccount, proposal);
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
            Approve(MainTester, InitAccount, proposal);
            var release = Release(MainTester, proposal, InitAccount);
            release.Status.ShouldBe("MINED");
            var byteString = ByteString.FromBase64(release.Logs[0].NonIndexed);
            var deployAddress = ContractDeployed.Parser.ParseFrom(byteString).Address;
            _logger.Info($"{deployAddress}");
        }

        #endregion


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
                new ChangeContractAuthorInput()
                {
                    ContractAddress =
                        AddressHelper.Base58StringToAddress("mkGKKat9jBFQa75Ty9QYiUnhssHJifYs9wPNafKZedx1TZx4s"),
                    NewAuthor =
                        AddressHelper.Base58StringToAddress("W4xEKTZcvPKXRAmdu9xEpM69ArF7gUxDh9MDgtsKnu7JfePXo"),
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

            var changeContractAuthorInput = new ChangeContractAuthorInput()
            {
                ContractAddress =
                    AddressHelper.Base58StringToAddress("mkGKKat9jBFQa75Ty9QYiUnhssHJifYs9wPNafKZedx1TZx4s"),
                NewAuthor = AddressHelper.Base58StringToAddress("W4xEKTZcvPKXRAmdu9xEpM69ArF7gUxDh9MDgtsKnu7JfePXo"),
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
                MainTester.ParliamentService.CallViewMethod<Address>(ParliamentMethod.GetGenesisOwnerAddress,
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
                MainTester.ParliamentService.CallViewMethod<Address>(ParliamentMethod.GetGenesisOwnerAddress,
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
                MainTester.ParliamentService.CallViewMethod<Address>(ParliamentMethod.GetGenesisOwnerAddress,
                    new Empty());
            _logger.Info($"organization address is {result} ");
        }


        [TestMethod]
        public void MainChangeZeroOwnerByUser()
        {
            ;
            var organizationAddress =
                MainTester.ParliamentService.CallViewMethod<Address>(ParliamentMethod.GetGenesisOwnerAddress,
                    new Empty());
            _logger.Info($"organization address is {organizationAddress} ");
            var input = organizationAddress;

            var contractOwnerAddress =
                MainTester.GenesisService.CallViewMethod<Address>(GenesisMethod.GetContractAuthor,
                    AddressHelper.Base58StringToAddress("x7G7VYqqeVAH8aeAsb7gYuTQ12YS1zKuxur9YES3cUj72QMxJ"));
            _logger.Info($"contract owner address is {contractOwnerAddress}");


            var result = MainTester.GenesisService.ExecuteMethodWithResult(GenesisMethod.ChangeGenesisOwner, input);
            var account =
                MainTester.ParliamentService.CallViewMethod<Address>(ParliamentMethod.GetGenesisOwnerAddress,
                    new Empty());
            _logger.Info($"organization address is {account} ");
        }


        [TestMethod]
        public void SideChangeZeroOwner()
        {
            var input = AddressHelper.Base58StringToAddress(InitAccount);
            var organizationAddress =
                SideTester.ParliamentService.CallViewMethod<Address>(ParliamentMethod.GetGenesisOwnerAddress,
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

        #region private method

        private Address CreateOrganization(ContractTester tester, bool isAuthority, Address proposer)
        {
            var address = tester.ParliamentService.ExecuteMethodWithResult(ParliamentMethod.CreateOrganization,
                new CreateOrganizationInput
                {
                    ReleaseThreshold = 6666,
                    ProposerAuthorityRequired = isAuthority,
                    ProposerWhiteList = {proposer}
                });
            var organization = address.ReadableReturnValue.Replace("\"", "");
            return AddressHelper.Base58StringToAddress(organization);
        }

        private Address GetGenesisOwnerAddress(ContractTester tester)
        {
            return tester.ParliamentService.CallViewMethod<Address>(ParliamentMethod.GetGenesisOwnerAddress,
                new Empty());
        }

        private Hash CreateProposal(ContractTester tester, string account, GenesisMethod method, IMessage input,
            Address organizationAddress)
        {
            tester.ParliamentService.SetAccount(account);
            var proposal = tester.ParliamentService.ExecuteMethodWithResult(ParliamentMethod.CreateProposal,
                new CreateProposalInput
                {
                    ContractMethodName = nameof(method),
                    ExpiredTime = DateTime.UtcNow.AddDays(1).ToTimestamp(),
                    Params = input.ToByteString(),
                    ToAddress = AddressHelper.Base58StringToAddress(tester.GenesisService.ContractAddress),
                    OrganizationAddress = organizationAddress
                });
            var proposalId = proposal.ReadableReturnValue.Replace("\"", "");
            return HashHelper.HexStringToHash(proposalId);
        }

        private void Approve(ContractTester tester, string account, Hash proposalId)
        {
            tester.ParliamentService.SetAccount(account);
            var approve =
                tester.ParliamentService.ExecuteMethodWithResult(ParliamentMethod.Approve, new ApproveInput
                {
                    ProposalId = proposalId
                });
            var approveResult = approve.ReadableReturnValue;

            approveResult.ShouldBe("true");
        }

        private TransactionResultDto Release(ContractTester tester, Hash proposalId, string account)
        {
            tester.ParliamentService.SetAccount(account);
            var result =
                tester.ParliamentService.ExecuteMethodWithResult(ParliamentMethod.Release,
                    proposalId);
            return result;
        }

        private ContractDeploymentInput ContractDeploymentInput(string name)
        {
            var contractReader = new SmartContractReader();
            var codeArray = contractReader.Read(name);

            var input = new ContractDeploymentInput
            {
                Category = KernelConstants.DefaultRunnerCategory,
                Code = ByteString.CopyFrom(codeArray),
            };
            return input;
        }

        private ContractUpdateInput ContractUpdateInput(string name, string contractAddress)
        {
            var contractReader = new SmartContractReader();
            var codeArray = contractReader.Read(name);

            var input = new ContractUpdateInput()
            {
                Address = AddressHelper.Base58StringToAddress(contractAddress),
                Code = ByteString.CopyFrom(codeArray)
            };

            return input;
        }

        #endregion
    }
}