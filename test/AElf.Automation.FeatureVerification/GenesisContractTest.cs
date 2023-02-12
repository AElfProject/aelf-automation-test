using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
                Tester.IssueToken(Creator, Member);
                Tester.IssueToken(Creator, OtherAccount);
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
                // Tester.TransferTokenToMiner(InitAccount);
                // Tester.TransferToken(OtherAccount);
            }
            Members = new List<string>{InitAccount,Member,OtherAccount};
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
            var result = Tester.GenesisService.ExecuteMethodWithResult(GenesisMethod.DeploySystemSmartContract,input);
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
            var deployAddress = ContractDeployed.Parser.ParseFrom(byteString).Address;
            var byteStringIndexed =
                ByteString.FromBase64(release.Logs.First(l => l.Name.Contains(nameof(ContractDeployed))).Indexed.First());
            var author = ContractDeployed.Parser.ParseFrom(byteStringIndexed).Author;
            Logger.Info($"{deployAddress}, {author}, {release.BlockNumber}");
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
            var release = Tester.GenesisService.ReleaseApprovedContract(contractProposalInfo, InitAccount);
            release.Status.ShouldBe("MINED");
            var byteString =
                ByteString.FromBase64(release.Logs.First(l => l.Name.Contains(nameof(ProposalCreated))).NonIndexed);
            var deployProposal = ProposalCreated.Parser.ParseFrom(byteString).ProposalId;
            Logger.Info($"{deployProposal}\n {contractProposalInfo.ProposedContractInputHash}");
        }

        [TestMethod]
        [DataRow("6171bff09116c62a2bc5fd0ec547a99b8c8c95532b0eebaf611d713d796c4016",
            "d65391e08082dff24e708caf5c4a664a57998f7f35e6fc9ea7b6ae118f0e2191")]
        public void ReleaseUpdateCodeCheck(string proposal, string hash)
        {
            var proposalId = Hash.LoadFromHex(proposal);
            var proposalHash = Hash.LoadFromHex(hash);
            var releaseApprovedContractInput = new ReleaseContractInput
            {
                ProposedContractInputHash = proposalHash,
                ProposalId = proposalId
            };

            var release = Tester.GenesisService.ReleaseCodeCheckedContract(releaseApprovedContractInput, InitAccount);
            release.Status.ShouldBe("MINED");
            var byteString =
                ByteString.FromBase64(release.Logs.First(l => l.Name.Contains(nameof(CodeUpdated))).Indexed.First());
            var updateAddress = CodeUpdated.Parser.ParseFrom(byteString).Address;
            Logger.Info($"{updateAddress}, {release.BlockNumber}");
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
        [DataRow("f28e51e01bbddb77b0a647680a55d700f6d7a78d38fab8a0cb62f1f73367345c")]
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
        public void ParliamentChangeWhiteList()
        {
            var parliament = Tester.ParliamentService;
            var defaultAddress = parliament.GetGenesisOwnerAddress();
            var existResult =
                parliament.CallViewMethod<BoolValue>(ParliamentMethod.ValidateOrganizationExist, defaultAddress);
            existResult.Value.ShouldBeTrue();

            var miners = Tester.GetMiners();

            var changeInput = new ProposerWhiteList
            {
                Proposers = {Member.ConvertAddress(), Creator.ConvertAddress()}
            };

            var proposalId = parliament.CreateProposal(parliament.ContractAddress,
                nameof(ParliamentMethod.ChangeOrganizationProposerWhiteList), changeInput, defaultAddress,
                miners.First());
            parliament.MinersApproveProposal(proposalId, miners);
            parliament.SetAccount(miners.First());
            var release = parliament.ReleaseProposal(proposalId, miners.First());
            release.Status.ShouldBe(TransactionResultStatus.Mined);

            var proposalWhiteList =
                parliament.CallViewMethod<ProposerWhiteList>(
                    ParliamentMethod.GetProposerWhiteList, new Empty());
            proposalWhiteList.Proposers.Contains(Member.ConvertAddress()).ShouldBeTrue();
        }

        [TestMethod]
        public void CheckContractInfo()
        {
            var contract = "2RHf2fxsnEaM3wb6N1yGqPupNZbcCY98LgWbGSFWmWzgEs5Sjo";
            var tokenContract =
                Tester.GenesisService.CallViewMethod<ContractInfo>(GenesisMethod.GetContractInfo,
                    contract.ConvertAddress());
            tokenContract.Category.ShouldBe(0);
            tokenContract.IsSystemContract.ShouldBeFalse();
            tokenContract.SerialNumber.ShouldNotBe(0L);
            tokenContract.Author.ShouldBe(Tester.GenesisService.Contract);
            tokenContract.Version.ShouldBe(2);
        }

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

        #endregion
    }
}