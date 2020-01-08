using System;
using System.Linq;
using Acs0;
using Acs3;
using AElf.Contracts.Association;
using AElf.Contracts.Genesis;
using AElf.Contracts.MultiToken;
using AElfChain.Common.Contracts;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using AElf.Kernel;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using log4net;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using CreateOrganizationInput = AElf.Contracts.Association.CreateOrganizationInput;
using Organization = AElf.Contracts.Association.Organization;

namespace AElf.Automation.Contracts.ScenarioTest
{
    [TestClass]
    public class GenesisContractTest
    {
        protected static readonly ILog _logger = Log4NetHelper.GetLogger();

        protected ContractTester Tester;

        public INodeManager NM { get; set; }
        public string InitAccount { get; } = "28Y8JA1i2cN6oHvdv7EraXJr9a1gY6D1PpJXw9QtRMRwKcBQMK";
        public string Creator { get; } = "28Y8JA1i2cN6oHvdv7EraXJr9a1gY6D1PpJXw9QtRMRwKcBQMK";
        public string Member { get; } = "2frDVeV6VxUozNqcFbgoxruyqCRAuSyXyfCaov6bYWc7Gkxkh2";

        public string OtherAccount { get; } = "h6CRCFAhyozJPwdFRd7i8A5zVAqy171AVty3uMQUQp1MB9AKa";
        private static string MainRpcUrl { get; } = "http://192.168.197.14:8000";
        private static string SideRpcUrl { get; } = "http://192.168.197.14:8001";
        private static string SideRpcUrl2 { get; } = "http://192.168.197.14:8002";
        private string Type { get; } = "Main";
        private readonly bool isOrganization = false;

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
            if (Type == "Side"&&!isOrganization)
            {
                Tester.IssueTokenToMiner(Creator);
                Tester.IssueToken(Creator, InitAccount);
                Tester.IssueToken(Creator, OtherAccount);
            }else if (isOrganization)
            {
                Tester.TokenService.TransferBalance(OtherAccount,Member,100_00000000,Tester.TokenService.GetPrimaryTokenSymbol());
                Tester.TokenService.TransferBalance(OtherAccount,InitAccount,100_00000000,Tester.TokenService.GetPrimaryTokenSymbol());
                var creator = CreateAssociationOrganization(Tester);
                IssueTokenToMinerThroughOrganization(Tester,OtherAccount,creator);
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
            var proposal = CreateProposal(Tester, InitAccount, GenesisMethod.ProposeNewContract, input,
                organization);
            ApproveByMiner(Tester, proposal);
            var release = Tester.ParliamentService.ReleaseProposal(proposal, InitAccount);
            release.Status.ShouldBe(TransactionResultStatus.Failed);
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
            ApproveByMiner(Tester, proposal);
            var release = Tester.ParliamentService.ReleaseProposal(proposal, InitAccount);
            release.Status.ShouldBe(TransactionResultStatus.Failed);
            release.Error.Contains("Contract proposing data not found.").ShouldBeTrue();
        }
        
        [TestMethod]
        public void ProposalDeploy_MinerProposalContract_Success()
        {
            var input = ContractDeploymentInput("AElf.Contracts.MultiToken4");
            var contractProposalInfo = ProposalNewContract(Tester, Creator, input);
//            var contractProposalInfo = new ReleaseContractInput
//            {
//                ProposalId = HashHelper.HexStringToHash("6ab5d9c4a7ff3c38277a9f7647acc7c458203d0adbf0e9de87424b82cbccfde4"),
//                ProposedContractInputHash = HashHelper.HexStringToHash("ad8b21fcc5ab497942cffe3de55fae9de62dc6bd16eb5f2cb81248c8a7684eb9")
//            };

            ApproveByMiner(Tester, contractProposalInfo.ProposalId);
            var release = Tester.GenesisService.ReleaseApprovedContract(contractProposalInfo, Creator);
            release.Status.ShouldBe("MINED");
            var byteString =
                ByteString.FromBase64(release.Logs.First(l => l.Name.Contains(nameof(ProposalCreated))).NonIndexed);
            var deployProposal = ProposalCreated.Parser.ParseFrom(byteString).ProposalId;

            _logger.Info($"{deployProposal}\n {contractProposalInfo.ProposedContractInputHash}");
        }

        [TestMethod]
        public void ProposalDeploy_ProposalContractWithOrganizationCreator_Success()
        {
            var deploymentInput = ContractDeploymentInput("AElf.Contracts.MultiToken1");
            Tester.AssociationService.SetAccount(OtherAccount);
            var creator = AddressHelper.Base58StringToAddress("s31xt16WnoYEhLxgSx7Jofy3ZkezEaf5mSieKd7LpR99NsKaW");
            var associationCreateProposal = Tester.AssociationService.CreateProposal(
                Tester.GenesisService.ContractAddress, nameof(GenesisMethod.ProposeNewContract), deploymentInput,
                creator, OtherAccount);
            ApproveWithAssociation(Tester,associationCreateProposal,creator);
            var createResult = Tester.AssociationService.ReleaseProposal(associationCreateProposal, OtherAccount);
            var proposalId = ProposalCreated.Parser
                .ParseFrom(ByteString.FromBase64(createResult.Logs.First(l => l.Name.Contains(nameof(ProposalCreated))).NonIndexed)).ProposalId;
            var proposalHash = ContractProposed.Parser
                .ParseFrom(ByteString.FromBase64(createResult.Logs.First(l => l.Name.Contains(nameof(ContractProposed))).NonIndexed))
                .ProposedContractInputHash;
            var contractProposalInfo = new ReleaseContractInput
            {
                ProposalId = proposalId,
                ProposedContractInputHash = proposalHash
            };
            ApproveByMiner(Tester, contractProposalInfo.ProposalId);
            
            var releaseProposal = Tester.AssociationService.CreateProposal(
                Tester.GenesisService.ContractAddress, nameof(GenesisMethod.ReleaseApprovedContract), contractProposalInfo,
                creator, OtherAccount);
            ApproveWithAssociation(Tester,releaseProposal,creator);
            var releaseResult = Tester.AssociationService.ReleaseProposal(releaseProposal, OtherAccount);
            var byteString =
                ByteString.FromBase64(releaseResult.Logs.First(l => l.Name.Contains(nameof(ProposalCreated))).NonIndexed);
            var deployProposal = ProposalCreated.Parser.ParseFrom(byteString).ProposalId;

            _logger.Info($"{deployProposal}\n {contractProposalInfo.ProposedContractInputHash}");
        }
        
        [TestMethod]
        public void ProposalDeploy_OrganizationProposalContractWithOtherOrganization_Success()
        {
            var input = ContractDeploymentInput("AElf.Contracts.MultiToken3");
            var contractProposalInfo = ProposalNewContract(Tester, OtherAccount, input);
//            var contractProposalInfo = new ReleaseContractInput
//            {
//                ProposalId = HashHelper.HexStringToHash("9d6ee285b090b4f1261eeb76dfac83055b50fcff01507596f3201aa18f1a44da"),
//                ProposedContractInputHash = HashHelper.HexStringToHash("ad8b21fcc5ab497942cffe3de55fae9de62dc6bd16eb5f2cb81248c8a7684eb9")
//            };
            var organizationAddress = AddressHelper.Base58StringToAddress("2EBXKkQfGz4fD1xacTiAXp7JksTpECTXJy5MSuYyEzdLbsanZW");
            ApproveWithAssociation(Tester,contractProposalInfo.ProposalId,organizationAddress);
            var release = Tester.GenesisService.ReleaseApprovedContract(contractProposalInfo, OtherAccount);
            release.Status.ShouldBe("MINED");
            var byteString =
                ByteString.FromBase64(release.Logs.First(l => l.Name.Contains(nameof(ProposalCreated))).NonIndexed);
            var deployProposal = ProposalCreated.Parser.ParseFrom(byteString).ProposalId;

            _logger.Info($"{deployProposal}\n {contractProposalInfo.ProposedContractInputHash}");
        }

        [TestMethod]
        [DataRow("ea2439a554df11143e020384bf4e97a0c117a93c854fb91c3c0d8efeb568cc91",
            "6e256995ba37bf00314ff85cc666bff225292e70d3c7a734bc0f28c67904eaa7")]
        public void ReleaseDeployCodeCheck(string proposal, string hash)
        {
            var proposalId = HashHelper.HexStringToHash(proposal);
            var proposalHash = HashHelper.HexStringToHash(hash);
            var releaseApprovedContractInput = new ReleaseContractInput()
            {
                ProposedContractInputHash = proposalHash,
                ProposalId = proposalId
            };

            var release = Tester.GenesisService.ReleaseCodeCheckedContract(releaseApprovedContractInput, InitAccount);
            release.Status.ShouldBe("MINED");
            var byteString =
                ByteString.FromBase64(release.Logs.First(l => l.Name.Contains(nameof(ContractDeployed))).NonIndexed);
            var deployAddress = ContractDeployed.Parser.ParseFrom(byteString).Address;
            _logger.Info($"{deployAddress}");
        }
        
        [TestMethod]
        [DataRow("a4f7d2bbd6d2817eef34ba60607831560e030d8abf2b418d9981dcdc8059f460",
            "354ace8d615c866cc0948810e55587fc451c45b40c5447e9040e44aa9ab7eddc")]
        public void ReleaseDeployCodeCheckWithOrganization(string proposal, string hash)
        {
            var proposalId = HashHelper.HexStringToHash(proposal);
            var proposalHash = HashHelper.HexStringToHash(hash);
            var releaseApprovedContractInput = new ReleaseContractInput()
            {
                ProposedContractInputHash = proposalHash,
                ProposalId = proposalId
            };
            var creator = AddressHelper.Base58StringToAddress("s31xt16WnoYEhLxgSx7Jofy3ZkezEaf5mSieKd7LpR99NsKaW");
            var releaseProposal = Tester.AssociationService.CreateProposal(
                Tester.GenesisService.ContractAddress, nameof(GenesisMethod.ReleaseCodeCheckedContract), releaseApprovedContractInput,
                creator, OtherAccount);
            ApproveWithAssociation(Tester,releaseProposal,creator);
            var releaseResult = Tester.AssociationService.ReleaseProposal(releaseProposal, OtherAccount);
            releaseResult.Status.ShouldBe("MINED");
            var byteString =
                ByteString.FromBase64(releaseResult.Logs.First(l => l.Name.Contains(nameof(ContractDeployed))).NonIndexed);
            var deployAddress = ContractDeployed.Parser.ParseFrom(byteString).Address;
            _logger.Info($"{deployAddress}");
        }
        

        [TestMethod]
        public void ProposalUpdate_MinerProposalUpdateContract_Success()
        {
            var input = ContractUpdateInput("AElf.Contracts.MultiToken4", Tester.ReferendumService.ContractAddress);
            var contractProposalInfo = ProposalUpdateContract(Tester, InitAccount, input);
            ApproveByMiner(Tester, contractProposalInfo.ProposalId);
            var release = Tester.GenesisService.ReleaseApprovedContract(contractProposalInfo, InitAccount);
            release.Status.ShouldBe("MINED");
            var byteString =
                ByteString.FromBase64(release.Logs.First(l => l.Name.Contains(nameof(ProposalCreated))).NonIndexed);
            var deployProposal = ProposalCreated.Parser.ParseFrom(byteString).ProposalId;

            _logger.Info($"{deployProposal}\n {contractProposalInfo.ProposedContractInputHash}");
        }

        [TestMethod]
        [DataRow("aa7a9a13b494dc3a113f591d5d7139635a7479e926cde06b902d3a38737aa86e",
            "ad8b21fcc5ab497942cffe3de55fae9de62dc6bd16eb5f2cb81248c8a7684eb9")]
        public void ReleaseUpdateCodeCheck(string proposal, string hash)
        {
            var proposalId = HashHelper.HexStringToHash(proposal);
            var proposalHash = HashHelper.HexStringToHash(hash);
            var releaseApprovedContractInput = new ReleaseContractInput()
            {
                ProposedContractInputHash = proposalHash,
                ProposalId = proposalId
            };

            var release = Tester.GenesisService.ReleaseCodeCheckedContract(releaseApprovedContractInput, InitAccount);
            release.Status.ShouldBe("MINED");
            var byteString =
                ByteString.FromBase64(release.Logs.First(l => l.Name.Contains(nameof(CodeUpdated))).Indexed.First());
            var updateAddress = CodeUpdated.Parser.ParseFrom(byteString).Address;
            _logger.Info($"{updateAddress}");
        }

        [TestMethod]
        public void ProposalDeploy_OtherUserProposalContract_Failed()
        {
            var input = ContractDeploymentInput("AElf.Contracts.MultiToken");
            Tester.GenesisService.SetAccount(OtherAccount);
            var result = Tester.GenesisService.ExecuteMethodWithResult(GenesisMethod.ProposeNewContract, input);
            result.Status.ShouldBe("FAILED");
            result.Error.Contains("Proposer authority validation failed.").ShouldBeTrue();
        }

        [TestMethod]
        public void ProposalUpdate_OtherUserUpdate_Failed()
        {
            var input = ContractUpdateInput("AElf.Contracts.MultiToken", Tester.ReferendumService.ContractAddress);
            Tester.GenesisService.SetAccount(OtherAccount);
            var result = Tester.GenesisService.ExecuteMethodWithResult(GenesisMethod.ProposeUpdateContract, input);
            result.Status.ShouldBe("FAILED");
            result.Error.Contains("No permission.").ShouldBeTrue();
        }

        [TestMethod]
        public void ChangeContractDeploymentController()
        {
            var changeAddress = CreateAssociationOrganization(Tester);
            var input = new AuthorityStuff
            {
                ContractAddress = AddressHelper.Base58StringToAddress(Tester.AssociationService.ContractAddress),
                OwnerAddress = changeAddress
            };

            var contractDeploymentController =
                Tester.GenesisService.CallViewMethod<AuthorityStuff>(GenesisMethod.GetContractDeploymentController,
                    new Empty());
            _logger.Info($"owner address is {contractDeploymentController.OwnerAddress} ");

            Tester.ParliamentService.SetAccount(InitAccount);
            var proposal = Tester.ParliamentService.ExecuteMethodWithResult(ParliamentMethod.CreateProposal,
                new CreateProposalInput
                {
                    ContractMethodName = nameof(GenesisMethod.ChangeContractDeploymentController),
                    ExpiredTime = DateTime.UtcNow.AddDays(1).ToTimestamp(),
                    Params = input.ToByteString(),
                    ToAddress = AddressHelper.Base58StringToAddress(Tester.GenesisService.ContractAddress),
                    OrganizationAddress = contractDeploymentController.OwnerAddress
                });
            var proposalId = HashHelper.HexStringToHash(proposal.ReadableReturnValue.Replace("\"", ""));
            
            ApproveByMiner(Tester,proposalId);
            var release = Tester.ParliamentService.ReleaseProposal(proposalId, InitAccount);
            release.Status.ShouldBe(TransactionResultStatus.Mined);
            contractDeploymentController =
                Tester.GenesisService.CallViewMethod<AuthorityStuff>(GenesisMethod.GetContractDeploymentController,
                    new Empty());
            contractDeploymentController.OwnerAddress.ShouldBe(changeAddress);
            _logger.Info($"Owner address is {contractDeploymentController.OwnerAddress} ");
        }
        
        [TestMethod]
        public void ChangeCodeCheckController()
        {
            var changeAddress = CreateParliamentOrganization(Tester);
            var input = new AuthorityStuff
            {
                ContractAddress = AddressHelper.Base58StringToAddress(Tester.ParliamentService.ContractAddress),
                OwnerAddress = changeAddress
            };

            var contractCodeCheckController =
                Tester.GenesisService.CallViewMethod<AuthorityStuff>(GenesisMethod.GetCodeCheckController,
                    new Empty());
            _logger.Info($"owner address is {contractCodeCheckController.OwnerAddress} ");

            Tester.ParliamentService.SetAccount(InitAccount);
            var proposal = Tester.ParliamentService.ExecuteMethodWithResult(ParliamentMethod.CreateProposal,
                new CreateProposalInput
                {
                    ContractMethodName = nameof(GenesisMethod.ChangeCodeCheckController),
                    ExpiredTime = DateTime.UtcNow.AddDays(1).ToTimestamp(),
                    Params = input.ToByteString(),
                    ToAddress = AddressHelper.Base58StringToAddress(Tester.GenesisService.ContractAddress),
                    OrganizationAddress = contractCodeCheckController.OwnerAddress
                });
            var proposalId = HashHelper.HexStringToHash(proposal.ReadableReturnValue.Replace("\"", ""));
            
            ApproveByMiner(Tester,proposalId);
            var release = Tester.ParliamentService.ReleaseProposal(proposalId, InitAccount);
            release.Status.ShouldBe(TransactionResultStatus.Mined);
            contractCodeCheckController =
                Tester.GenesisService.CallViewMethod<AuthorityStuff>(GenesisMethod.GetCodeCheckController,
                    new Empty());
            contractCodeCheckController.OwnerAddress.ShouldBe(changeAddress);
            _logger.Info($"Code check controller address is {contractCodeCheckController.OwnerAddress} ");
        }

        [TestMethod]
        public void CheckController()
        {
            var contractCodeCheckController =
                Tester.GenesisService.CallViewMethod<AuthorityStuff>(GenesisMethod.GetCodeCheckController,
                    new Empty());
            _logger.Info($"Code check controller address is {contractCodeCheckController.OwnerAddress} ");
        }

        [TestMethod]
        [DataRow("5b8f8ba5aa1e1815bdfafc6b37383ca52cd641d188384affaee9aaa9a3648f4a")]
        public void CheckProposal(string proposalId)
        {
            var proposal = HashHelper.HexStringToHash(proposalId);
            var result = Tester.AssociationService.CallViewMethod<ProposalOutput>(AssociationMethod.GetProposal,
                proposal);
            _logger.Info($"{result.ToBeReleased}");
            _logger.Info($"{result.ExpiredTime}");
            _logger.Info($"{result.Proposer}");
            _logger.Info($"{result.OrganizationAddress}");
        }

        [TestMethod]
        [DataRow("SuaPmtyFjozAVCbubchFHL2yLUrpgWYM67CMgNES1v16xanq9")]
        public void CheckOwner(string contract)
        {
            var address =
                Tester.GenesisService.CallViewMethod<Address>(GenesisMethod.GetContractAuthor,
                    AddressHelper.Base58StringToAddress(contract));
            _logger.Info($"{address.GetFormatted()}");
        }

        #region private method

        private Address CreateAssociationOrganization(ContractTester tester)
        {
            var address = tester.AssociationService.ExecuteMethodWithResult(AssociationMethod.CreateOrganization,
                new CreateOrganizationInput
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
                            AddressHelper.Base58StringToAddress(OtherAccount),
                            AddressHelper.Base58StringToAddress(InitAccount),
                            AddressHelper.Base58StringToAddress(Member)
                        }
                    },
                    ProposerWhiteList = new ProposerWhiteList
                    {
                        Proposers = {AddressHelper.Base58StringToAddress(OtherAccount)}
                    }
                });
            var organization = address.ReadableReturnValue.Replace("\"", "");
            _logger.Info($"Association organization is: {organization}");
            return AddressHelper.Base58StringToAddress(organization);
        }
        
        private Address CreateParliamentOrganization(ContractTester tester)
        {
            var address = tester.ParliamentService.ExecuteMethodWithResult(ParliamentMethod.CreateOrganization,
                new AElf.Contracts.Parliament.CreateOrganizationInput()
                {
                    ProposalReleaseThreshold = new ProposalReleaseThreshold
                    {
                        MaximalAbstentionThreshold = 1000,
                        MaximalRejectionThreshold = 1000,
                        MinimalApprovalThreshold = 3000,
                        MinimalVoteThreshold = 3000
                    },
                    ProposerAuthorityRequired = false
                });
            var organization = address.ReadableReturnValue.Replace("\"", "");
            _logger.Info($"Association organization is: {organization}");
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

        public void IssueTokenToMinerThroughOrganization(ContractTester tester,string account,Address organization)
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
                    To = AddressHelper.Base58StringToAddress(miner)
                };
                var createProposal = tester.AssociationService.CreateProposal(tester.TokenService.ContractAddress,
                    nameof(TokenMethod.Issue), input, organization, account);
                ApproveWithAssociation(tester,createProposal,organization);
                tester.AssociationService.ReleaseProposal(createProposal,account);
            }
        }

        public void ApproveWithAssociation(ContractTester tester,Hash proposalId,Address association)
        {
            var organization = tester.AssociationService.CallViewMethod<Organization>(AssociationMethod.GetOrganization,
                    association);
            var members = organization.OrganizationMemberList.OrganizationMembers.ToList();
            foreach (var member in members)
            {
                tester.AssociationService.SetAccount(member.GetFormatted());
                var approve = tester.AssociationService.ExecuteMethodWithResult(AssociationMethod.Approve, proposalId);
                approve.Status.ShouldBe("MINED");                
                if (tester.AssociationService.CheckProposal(proposalId).ToBeReleased) return;
            }
        }

        #endregion
    }
}