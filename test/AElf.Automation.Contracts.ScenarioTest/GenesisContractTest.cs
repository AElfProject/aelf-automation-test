using System;
using System.Collections.Generic;
using System.Linq;
using Acs0;
using Acs3;
using AElf.Client.Dto;
using AElfChain.Common.Contracts;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using AElf.Contracts.ParliamentAuth;
using AElf.Kernel;
using AElf.Types;
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
        
        protected ContractTester Tester;

        public INodeManager NM { get; set; }
        public string InitAccount { get; } = "2RCLmZQ2291xDwSbDEJR6nLhFJcMkyfrVTq1i1YxWC4SdY49a6";
        public string Creator { get; } = "28Y8JA1i2cN6oHvdv7EraXJr9a1gY6D1PpJXw9QtRMRwKcBQMK";
        public string OtherAccount { get; } = "h6CRCFAhyozJPwdFRd7i8A5zVAqy171AVty3uMQUQp1MB9AKa";
        private static string MainRpcUrl { get; } = "http://192.168.197.56:8001";
        private static string SideRpcUrl { get; } = "http://192.168.197.56:8011";
        private static string SideRpcUrl2 { get; } = "http://192.168.197.56:8021";
        private string Type { get; } = "Main";

        [TestInitialize]
        public void Initialize()
        {
            #region Basic Preparation

            //Init Logger
            Log4NetHelper.LogInit("ContractTest_");
            
            #endregion
            NM = new NodeManager(MainRpcUrl);
            var services = new ContractServices(NM, InitAccount, Type);
            Tester = new ContractTester(services);
            if (Type == "Side")
            {
                Tester.IssueTokenToMiner(Creator);
                Tester.IssueToken(Creator,InitAccount);
                Tester.IssueToken(Creator,OtherAccount);
            }
            else
            {
                Tester.TransferTokenToMiner(InitAccount);
                Tester.TransferToken(OtherAccount);
            }
        }
        
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
            var input = ContractUpdateInput("AElf.Contracts.Election",Tester.AssociationService.ContractAddress);
            
            Tester.TokenService.SetAccount(InitAccount);
            var result = Tester.GenesisService.ExecuteMethodWithResult(GenesisMethod.UpdateSmartContract,input);
            result.Status.ShouldBe("FAILED");
            result.Error.Contains("Unauthorized behavior.").ShouldBeTrue();
        }

        [TestMethod]
        public void DeploySmartContract_ThroughGenesisOwnerAddress()
        {
            var input = ContractDeploymentInput("AElf.Contracts.MultiToken");
            var organization = GetGenesisOwnerAddress(Tester);
            var proposal = CreateProposal(Tester, InitAccount, GenesisMethod.ProposeNewContract, input,
                organization);
            Approve(Tester, proposal);
            var release = Release(Tester, proposal, InitAccount);
            release.Status.ShouldBe("FAILED");
            release.Error.Contains("Proposer authority validation failed.").ShouldBeTrue();
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
            var proposal = CreateProposal(Tester, InitAccount, GenesisMethod.DeploySmartContract, input,
                organization);
            Approve(Tester, proposal);
            var release = Release(Tester, proposal, InitAccount);
            release.Status.ShouldBe("FAILED");
            release.Error.Contains("Invalid contract proposing status.").ShouldBeTrue();
        }

        [TestMethod]
        public void ProposalDeploy_MinerProposalContract_Success()
        {
            var input = ContractDeploymentInput("AElf.Contracts.MultiToken");
            var contractProposalInfo = ProposalNewContract(Tester, InitAccount,input);
            Approve(Tester, contractProposalInfo.ProposalId);
            var release = ReleaseApprove(Tester, contractProposalInfo, InitAccount);
            
            release.Status.ShouldBe("MINED");
            var byteString = ByteString.FromBase64(release.Logs.First(l => l.Name.Contains(nameof(ProposalCreated))).NonIndexed);
            var deployProposal = ProposalCreated.Parser.ParseFrom(byteString).ProposalId;
           
            _logger.Info($"{deployProposal}\n {contractProposalInfo.ProposedContractInputHash}");
        }
        
        [TestMethod]
        [DataRow("41220cef4143cf3532ac1b46dfc53f183d4bc8718c59f3a0f40b1ccebdde58b5","cc212f7ad88e8e2958ec07165f9b248fc3967cb3263e928136765adb7b3ed9a2")]
        public void ReleaseDeployCodeCheck(string proposal, string hash)
        {
            var proposalId = HashHelper.HexStringToHash(proposal);
            var proposalHash = HashHelper.HexStringToHash(hash);
            var releaseApprovedContractInput = new ReleaseContractInput()
            {
                ProposedContractInputHash = proposalHash,
                ProposalId = proposalId
            };

            var release = ReleaseCodeCheck(Tester,releaseApprovedContractInput, InitAccount);
            release.Status.ShouldBe("MINED");
            var byteString = ByteString.FromBase64(release.Logs.First(l =>l.Name.Contains(nameof(ContractDeployed))).NonIndexed);
            var deployAddress = ContractDeployed.Parser.ParseFrom(byteString).Address;
            _logger.Info($"{deployAddress}");
        }

        [TestMethod]
        public void ProposalUpdate_MinerProposalUpdateContract_Success()
        {
            var input = ContractUpdateInput("AElf.Contracts.Election",Tester.ReferendumService.ContractAddress);
            var contractProposalInfo = ProposalUpdateContract(Tester, InitAccount,input);
            Approve(Tester, contractProposalInfo.ProposalId);
            var release = ReleaseApprove(Tester, contractProposalInfo, InitAccount);
            release.Status.ShouldBe("MINED");
            var byteString = ByteString.FromBase64(release.Logs.First(l => l.Name.Contains(nameof(ProposalCreated))).NonIndexed);
            var deployProposal = ProposalCreated.Parser.ParseFrom(byteString).ProposalId;

            _logger.Info($"{deployProposal}\n {contractProposalInfo.ProposedContractInputHash}");
        }
        
        [TestMethod]
        [DataRow("0659e746a9c4cce109633eebfe58253cf93481d66eb0dd69a166a9ccf9448363","c7cd0d4cd35ddcab912ffe24dfa628d1d74450138d74c28854cd196cbf81eca5")]
        public void ReleaseUpdateCodeCheck(string proposal, string hash)
        {
            var proposalId = HashHelper.HexStringToHash(proposal);
            var proposalHash = HashHelper.HexStringToHash(hash);
            var releaseApprovedContractInput = new ReleaseContractInput()
            {
                ProposedContractInputHash = proposalHash,
                ProposalId = proposalId
            };

            var release = ReleaseCodeCheck(Tester,releaseApprovedContractInput, InitAccount);
            release.Status.ShouldBe("MINED");
            var byteString = ByteString.FromBase64(release.Logs.First(l =>l.Name.Contains(nameof(CodeUpdated))).Indexed.First());
            var updateAddress = CodeUpdated.Parser.ParseFrom(byteString).Address;
            _logger.Info($"{updateAddress}");
        }
        
        [TestMethod]
        public void ProposalDeploy_OtherUserProposalContract_Failed()
        {
            var input = ContractDeploymentInput("AElf.Contracts.MultiToken");
            Tester.GenesisService.SetAccount(OtherAccount);
            var result = Tester.GenesisService.ExecuteMethodWithResult(GenesisMethod.ProposeNewContract,input);
            result.Status.ShouldBe("FAILED");
            result.Error.Contains("Proposer authority validation failed.").ShouldBeTrue();
        }
        
        [TestMethod]
        public void ProposalUpdate_OtherUserUpdate_Failed()
        {
            var input = ContractUpdateInput("AElf.Contracts.MultiToken",Tester.ReferendumService.ContractAddress);
            Tester.GenesisService.SetAccount(OtherAccount);
            var result = Tester.GenesisService.ExecuteMethodWithResult(GenesisMethod.ProposeUpdateContract, input);
            result.Status.ShouldBe("FAILED");
            result.Error.Contains("No permission.").ShouldBeTrue();
        }
        
        
        [TestMethod]
        public void ChangeZeroOwner()
        {
            var input = AddressHelper.Base58StringToAddress(InitAccount);
            var organizationAddress =
                Tester.ParliamentService.CallViewMethod<Address>(ParliamentMethod.GetDefaultOrganizationAddress,
                    new Empty());
            _logger.Info($"organization address is {organizationAddress} ");

            var contractOwnerAddress =
                Tester.GenesisService.CallViewMethod<Address>(GenesisMethod.GetContractAuthor,
                    AddressHelper.Base58StringToAddress("x7G7VYqqeVAH8aeAsb7gYuTQ12YS1zKuxur9YES3cUj72QMxJ"));
            _logger.Info($"contract owner address is {contractOwnerAddress}");


            var proposal = Tester.ParliamentService.ExecuteMethodWithResult(ParliamentMethod.CreateProposal,
                new CreateProposalInput
                {
                    ContractMethodName = nameof(GenesisMethod.ChangeGenesisOwner),
                    ExpiredTime = DateTime.UtcNow.AddDays(1).ToTimestamp(),
                    Params = input.ToByteString(),
                    ToAddress = AddressHelper.Base58StringToAddress(Tester.GenesisService.ContractAddress),
                    OrganizationAddress = organizationAddress
                });
            var proposalId = proposal.ReadableReturnValue.Replace("\"", "");

            Tester.ParliamentService.SetAccount("2RCLmZQ2291xDwSbDEJR6nLhFJcMkyfrVTq1i1YxWC4SdY49a6");
            var approve =
                Tester.ParliamentService.ExecuteMethodWithResult(ParliamentMethod.Approve, new ApproveInput
                {
                    ProposalId = HashHelper.HexStringToHash(proposalId)
                });
            var approveResult = approve.ReadableReturnValue;

            approveResult.ShouldBe("true");

            Tester.GenesisService.SetAccount(InitAccount);
            var result =
                Tester.ParliamentService.CallViewMethod<Address>(ParliamentMethod.GetDefaultOrganizationAddress,
                    new Empty());
            _logger.Info($"organization address is {result} ");
        }
        

        [TestMethod]
        [DataRow("1a0d0f50d4472eb09242383e401ea525b7fa0ce8bd2f866c4fd000b30cede13d")]
        public void CheckProposal(string proposalId)
        {
            var proposal = HashHelper.HexStringToHash(proposalId);
            var result = Tester.ParliamentService.CallViewMethod<ProposalOutput>(ParliamentMethod.GetProposal,
                proposal);
            _logger.Info($"{result.ToBeReleased}");
            _logger.Info($"{result.ExpiredTime}");
            _logger.Info($"{result.Proposer}");
        }
        
        [TestMethod]
        public void GetBalance()
        {
            var miners = Tester.GetMiners();
            foreach (var miner in miners)
            {
                var balance = Tester.TokenService.GetUserBalance(miner);
                _logger.Info($"{balance}");
            }
        }

        [TestMethod]
//        [DataRow("RSr6bPc7Hv6dMJiWdPgBBFMacUJcrgQoeHkVBMjqJ5HURtKK3")]
        [DataRow("SuaPmtyFjozAVCbubchFHL2yLUrpgWYM67CMgNES1v16xanq9")]
        public void CheckOwner(string contract)
        {
            var address =
                Tester.GenesisService.CallViewMethod<Address>(GenesisMethod.GetContractAuthor, AddressHelper.Base58StringToAddress(contract));
            _logger.Info($"{address.GetFormatted()}");
        }
        
        [TestMethod]
        [DataRow("GiIKIKOmGZC08DAoVVq4bnxr6WsfKAUpflGo1WLHAKS9g+SD")]
        public void test(string input)
        {
            var byteString = ByteString.FromBase64(input);
            var deployProposal = ContractDeployed.Parser.ParseFrom(byteString).Address;
            _logger.Info($"{deployProposal}\n");
        }

        #region private method

        private Address CreateOrganization(ContractTester tester)
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

        private ReleaseContractInput ProposalNewContract(ContractTester tester,string account,ContractDeploymentInput input)
        {
            var result = tester.GenesisService.ProposeNewContract(input, account);
            result.Status.ShouldBe(TransactionResultStatus.Mined);
            var proposalId = ProposalCreated.Parser.ParseFrom(result.Logs.First(l => l.Name.Contains(nameof(ProposalCreated))).NonIndexed).ProposalId;
            var proposalHash = ContractProposed.Parser.ParseFrom(result.Logs.First(l => l.Name.Contains(nameof(ContractProposed))).NonIndexed)
                .ProposedContractInputHash;
            return new ReleaseContractInput
            {
                ProposalId = proposalId,
                ProposedContractInputHash = proposalHash
            };
        }
        
        private ReleaseContractInput ProposalUpdateContract(ContractTester tester,string account, ContractUpdateInput input)
        {
            var result = tester.GenesisService.ProposeUpdateContract(input, account);
            result.Status.ShouldBe(TransactionResultStatus.Mined);
            var proposalId = ProposalCreated.Parser.ParseFrom(result.Logs.First(l => l.Name.Contains(nameof(ProposalCreated))).NonIndexed).ProposalId;
            var proposalHash = ContractProposed.Parser.ParseFrom(result.Logs.First(l => l.Name.Contains(nameof(ContractProposed))).NonIndexed)
                .ProposedContractInputHash;
            return new ReleaseContractInput
            {
                ProposalId = proposalId,
                ProposedContractInputHash = proposalHash
            };
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
                if (tester.ParliamentService.CheckProposal(proposalId).ToBeReleased) return;
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
        
        private TransactionResultDto ReleaseApprove(ContractTester tester,ReleaseContractInput input , string account)
        {
            tester.GenesisService.SetAccount(account);
            var result =
                tester.GenesisService.ExecuteMethodWithResult(GenesisMethod.ReleaseApprovedContract, new ReleaseContractInput
                {
                    ProposalId = input.ProposalId,
                    ProposedContractInputHash = input.ProposedContractInputHash
                });
            return result;
        }
        
        private TransactionResultDto ReleaseCodeCheck(ContractTester tester,ReleaseContractInput input , string account)
        {
            tester.GenesisService.SetAccount(account);
            var result =
                tester.GenesisService.ExecuteMethodWithResult(GenesisMethod.ReleaseCodeCheckedContract, new ReleaseContractInput()
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