using System;
using System.Linq;
using Acs1;
using Acs3;
using Acs7;
using AElf.Contracts.Association;
using AElf.Contracts.CrossChain;
using AElf.Contracts.MultiToken;
using AElf.CSharp.Core.Extension;
using AElf.Types;
using AElfChain.Common;
using AElfChain.Common.Contracts;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Helpers;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace AElf.Automation.SideChainTests
{
    [TestClass]
    public class SideChainCreate : SideChainTestBase
    {
        [TestInitialize]
        public void InitializeNodeTests()
        {
            Initialize();
        }

        [TestMethod]
        public void CreateSideThroughOrganization()
        {
            var associationOrganization = CreateAssociationOrganization(MainServices);
            MainServices.TokenService.TransferBalance(InitAccount, OtherAccount, 5000_00000000, "ELF");
            MainServices.TokenService.TransferBalance(InitAccount, associationOrganization.GetFormatted(), 500000000,
                "ELF");

            ApproveAndTransferOrganizationBalanceAsync(MainServices, associationOrganization, 400000, OtherAccount);
            var tokenInfo = new SideChainTokenInfo
            {
                Symbol = "STC",
                TokenName = "Side chain token STC",
                Decimals = 8,
                IsBurnable = true,
                Issuer = AddressHelper.Base58StringToAddress(InitAccount),
                TotalSupply = 10_00000000_00000000
            };
            var issueAccount = new SideChainTokenInitialIssue
            {
                Address = AddressHelper.Base58StringToAddress(OtherAccount),
                Amount = 1000_0000_00000000
            };
            var issueOrganization = new SideChainTokenInitialIssue
            {
                Address = associationOrganization,
                Amount = 1000_0000_00000000
            };
            var input = new SideChainCreationRequest
            {
                IndexingPrice = 1,
                LockedTokenAmount = 400000,
                IsPrivilegePreserved = true,
                SideChainTokenDecimals = tokenInfo.Decimals,
                SideChainTokenName = tokenInfo.TokenName,
                SideChainTokenSymbol = tokenInfo.Symbol,
                SideChainTokenTotalSupply = tokenInfo.TotalSupply,
                IsSideChainTokenBurnable = tokenInfo.IsBurnable,
                InitialResourceAmount = {{"CPU", 2}, {"RAM", 4}, {"DISK", 512}, {"NET", 1024}},
                SideChainTokenInitialIssueList = {issueAccount, issueOrganization}
            };

            var createProposal = MainServices.AssociationService.CreateProposal(
                MainServices.CrossChainService.ContractAddress,
                nameof(CrossChainContractMethod.RequestSideChainCreation), input, associationOrganization,
                OtherAccount);
            ApproveWithAssociation(MainServices, createProposal, associationOrganization);
            var release = ReleaseWithAssociation(MainServices, createProposal, OtherAccount);

            var createSideChainProposalId = ProposalCreated.Parser
                .ParseFrom(ByteString.FromBase64(release.Logs.First(l => l.Name.Contains(nameof(ProposalCreated)))
                    .NonIndexed)).ProposalId;
            ApproveProposal(MainServices, createSideChainProposalId);

            var releaseInput = new ReleaseSideChainCreationInput {ProposalId = createSideChainProposalId};
            var releaseProposal = MainServices.AssociationService.CreateProposal(
                MainServices.CrossChainService.ContractAddress,
                nameof(CrossChainContractMethod.ReleaseSideChainCreation), releaseInput, associationOrganization,
                OtherAccount);
            ApproveWithAssociation(MainServices, releaseProposal, associationOrganization);
            var releaseRelease = ReleaseWithAssociation(MainServices, releaseProposal, OtherAccount);

            var sideChainCreatedEvent = SideChainCreatedEvent.Parser
                .ParseFrom(ByteString.FromBase64(releaseRelease.Logs
                    .First(l => l.Name.Contains(nameof(SideChainCreatedEvent)))
                    .NonIndexed));
            var chainId = sideChainCreatedEvent.ChainId;
            var creator = sideChainCreatedEvent.Creator;
            creator.ShouldBe(associationOrganization);
            _logger.Info($"chainId : {chainId}");
        }

        [TestMethod]
        public void RequestSideChainCreation()
        {
            TokenApprove(MainServices, OtherAccount, 1000000);
            var tokenInfo = new SideChainTokenInfo
            {
                Symbol = "STD",
                TokenName = "Side chain token STC",
                Decimals = 8,
                IsBurnable = true,
                Issuer = AddressHelper.Base58StringToAddress(OtherAccount),
                TotalSupply = 10_00000000_00000000,
                IsProfitable = true
            };
            var proposal = RequestSideChainCreation(MainServices, OtherAccount, "123", 1, 1000000, true, tokenInfo);
            _logger.Info($"proposal id is: {proposal}");
        }

        [TestMethod]
        [DataRow("512f7b0371f4bf13f224c13d61c95d68bca278c5854ef5e29c7f61e7f29159a0")]
        public void ApproveProposalThroughOtherAuthory(string proposalId)
        {
            var proposal = HashHelper.HexStringToHash(proposalId);
            var input = new CreateOrganizationInput
            {
                ProposalReleaseThreshold = new ProposalReleaseThreshold
                {
                    MaximalAbstentionThreshold = 1,
                    MaximalRejectionThreshold = 1,
                    MinimalApprovalThreshold = 2,
                    MinimalVoteThreshold = 2
                },
                OrganizationMemberList = new OrganizationMemberList
                {
                    OrganizationMembers =
                    {
                        AddressHelper.Base58StringToAddress(OtherAccount),
                        AddressHelper.Base58StringToAddress(InitAccount),
                        AddressHelper.Base58StringToAddress(MemberAccount)
                    }
                },
                ProposerWhiteList = new ProposerWhiteList
                {
                    Proposers = {AddressHelper.Base58StringToAddress(OtherAccount)}
                }
            };
            var organization = MainServices.AssociationService.CalculateOrganizationAddress(input);
            ApproveWithAssociation(MainServices, proposal, organization);
        }

        [TestMethod]
        [DataRow("4c075565962d2c8f90fe45817145ce0b3eafe65b818daccabf91f3d3ed9cce1c")]
        public void ApproveProposal(string proposalId)
        {
            var proposal = HashHelper.HexStringToHash(proposalId);
            ApproveProposal(MainServices, proposal);
        }

        [TestMethod]
        [DataRow("512f7b0371f4bf13f224c13d61c95d68bca278c5854ef5e29c7f61e7f29159a0")]
        public void ReleaseSideChainCreation(string proposalId)
        {
            MainServices.CrossChainService.SetAccount(OtherAccount);
            var result
                = MainServices.CrossChainService.ExecuteMethodWithResult(
                    CrossChainContractMethod.ReleaseSideChainCreation,
                    new ReleaseSideChainCreationInput
                    {
                        ProposalId = HashHelper.HexStringToHash(proposalId)
                    });
            var release = result.Logs.First(l => l.Name.Contains(nameof(SideChainCreatedEvent)))
                .NonIndexed;
            var byteString = ByteString.FromBase64(release);
            var sideChainCreatedEvent = SideChainCreatedEvent.Parser
                .ParseFrom(byteString);
            var chainId = sideChainCreatedEvent.ChainId;
            var creator = sideChainCreatedEvent.Creator;
            var organization = OrganizationCreated.Parser
                .ParseFrom(ByteString.FromBase64(result.Logs.First(l => l.Name.Contains(nameof(OrganizationCreated)))
                    .NonIndexed)).OrganizationAddress;
            creator.ShouldBe(OtherAccount.ConvertAddress());
            _logger.Info($"SideChain id is {chainId}, controller address is {organization}");
        }

        [TestMethod]
        public void CreateProposal()
        {
            TokenApprove(MainServices, InitAccount, 400000);
            var tokenInfo = new SideChainTokenInfo
            {
                Symbol = "SSTA",
                TokenName = "Side chain token SSTA",
                Decimals = 8,
                IsBurnable = true,
                IsProfitable = true,
                Issuer = InitAccount.ConvertAddress(),
                TotalSupply = 10_00000000_00000000
            };
            var proposalId = RequestSideChainCreation(MainServices, InitAccount, "123", 1, 400000, true, tokenInfo);
            ApproveProposal(MainServices, proposalId);
            var releaseResult = ReleaseSideChainCreation(MainServices, InitAccount, proposalId);
            var release = releaseResult.Logs.First(l => l.Name.Contains(nameof(SideChainCreatedEvent)))
                .NonIndexed;
            var byteString = ByteString.FromBase64(release);
            var sideChainCreatedEvent = SideChainCreatedEvent.Parser
                .ParseFrom(byteString);
            var chainId = sideChainCreatedEvent.ChainId;
            var creator = sideChainCreatedEvent.Creator;

            _logger.Info($"Side chain id is {chainId}, creator is {creator}");
        }

        [TestMethod]
        public void CreateProposalOnSideChain()
        {
            var sideAServices = SideServices.First();
            TokenApprove(sideAServices, InitAccount, 400000);
            var tokenInfo = new SideChainTokenInfo
            {
                Symbol = "SSTA",
                TokenName = "Side chain token SSTA",
                Decimals = 8,
                IsBurnable = true,
                IsProfitable = true,
                Issuer = InitAccount.ConvertAddress(),
                TotalSupply = 10_00000000_00000000
            };
            var proposalId = RequestSideChainCreation(sideAServices, InitAccount, "123", 1, 400000, true, tokenInfo);
            ApproveProposal(sideAServices, proposalId);
            var releaseResult = ReleaseSideChainCreation(sideAServices, InitAccount, proposalId);
            var release = releaseResult.Logs.First(l => l.Name.Contains(nameof(SideChainCreatedEvent)))
                .NonIndexed;
            var byteString = ByteString.FromBase64(release);
            var sideChainCreatedEvent = SideChainCreatedEvent.Parser
                .ParseFrom(byteString);
            var chainId = sideChainCreatedEvent.ChainId;
            var creator = sideChainCreatedEvent.Creator;

            _logger.Info($"Side chain id is {chainId}, creator is {creator}");
        }

        [TestMethod]
        public void GetProposal()
        {
//            var proposalId = ProposalCreated.Parser
//                .ParseFrom(ByteString.FromBase64("CiIKIMO8feW9yiU85cN3SXgfIzJ5Od35XIhfYvwsmHmAcuhM")).ProposalId;
            var proposalId =
                HashHelper.HexStringToHash("006844e445373e45cd196cffc696744eba46f6ea2741826b3817558771578cd2");
            var result = MainServices.AssociationService.CheckProposal(proposalId);
            _logger.Info(
                $"proposal message is {result.ToBeReleased} {result.ApprovalCount}");
        }

        [TestMethod]
        public void GetChainInitializationData()
        {
            var chainId = ChainHelper.ConvertBase58ToChainId("tDVV");
            var sideChainInfo = MainServices.CrossChainService.GetChainInitializationData(chainId);
            _logger.Info($"{sideChainInfo.Creator},{sideChainInfo.ChainCreatorPrivilegePreserved}");
        }

        [TestMethod]
        [DataRow("tDVW")]
        [DataRow("tDVV")]
        public void CheckStatus(string chainId)
        {
            var intChainId = ChainHelper.ConvertBase58ToChainId(chainId);
            var status = MainServices.CrossChainService.GetChainStatus(intChainId).Status;
            _logger.Info($"side chain is {status}");
        }

        [TestMethod]
        [DataRow("tDVW")]
        public void Recharge(string chainId)
        {
            var intChainId = ChainHelper.ConvertBase58ToChainId(chainId);
            var approve = MainServices.TokenService.ExecuteMethodWithResult(TokenMethod.Approve, new ApproveInput
            {
                Spender = MainServices.CrossChainService.Contract,
                Symbol = MainServices.TokenService.GetNativeTokenSymbol(),
                Amount = 200000
            });
            approve.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var status = Recharge(MainServices, InitAccount, intChainId, 200000);
            _logger.Info($" Transaction is {status.Status}");
        }

        [TestMethod]
        public void DisposeSideChain()
        {
            TransferToken(MainServices, InitAccount, OtherAccount, 1000_00000000,
                MainServices.TokenService.GetPrimaryTokenSymbol());
            var chainId = ChainHelper.ConvertBase58ToChainId("tDVV");
            var input = new Int32Value {Value = chainId};
            var organization = MainServices.CrossChainService.GetSideChainLifetimeController();
            var createProposal = MainServices.ParliamentService.CreateProposal(
                MainServices.CrossChainService.ContractAddress, nameof(CrossChainContractMethod.DisposeSideChain),
                input, organization.OwnerAddress, OtherAccount);
            foreach (var miner in Miners) MainServices.ParliamentService.ApproveProposal(createProposal, miner);

            MainServices.ParliamentService.ReleaseProposal(createProposal, OtherAccount);
            var chainStatue = MainServices.CrossChainService.GetChainStatus(chainId).Status;
            chainStatue.ShouldBe(SideChainStatus.Terminated);
        }

        [TestMethod]
        public void DisposeSideChainThroughAssociation()
        {
            TransferToken(MainServices, InitAccount, OtherAccount, 1000_00000000,
                MainServices.TokenService.GetPrimaryTokenSymbol());
            var chainId = ChainHelper.ConvertBase58ToChainId("tDVX");
            var input = new Int32Value {Value = chainId};
            var organization = MainServices.CrossChainService.GetSideChainLifetimeController();
            var createProposal = MainServices.AssociationService.CreateProposal(
                MainServices.CrossChainService.ContractAddress, nameof(CrossChainContractMethod.DisposeSideChain),
                input, organization.OwnerAddress, OtherAccount);
            MainServices.AssociationService.ApproveWithAssociation(createProposal, organization.OwnerAddress);
            MainServices.AssociationService.ReleaseProposal(createProposal, OtherAccount);
            var chainStatue = MainServices.CrossChainService.GetChainStatus(chainId).Status;
            chainStatue.ShouldBe(SideChainStatus.Terminated);
        }

        [TestMethod]
        [DataRow("28Y8JA1i2cN6oHvdv7EraXJr9a1gY6D1PpJXw9QtRMRwKcBQMK")]
        public void CheckBalance(string account)
        {
            var balance = GetBalance(MainServices, MainServices.CrossChainService.ContractAddress,
                NodeOption.NativeTokenSymbol);
            _logger.Info($"side chain balance is {balance}");

            var userBalance = GetBalance(MainServices, account, NodeOption.NativeTokenSymbol);
            _logger.Info($"user balance is {userBalance}");
        }

        [TestMethod]
        public void ChangeIndexFee()
        {
            var chainId = ChainHelper.ConvertBase58ToChainId("tDVW");

            var checkPrice = MainServices.CrossChainService.GetSideChainIndexingFeePrice(chainId);
            var getInfo = MainServices.CrossChainService.GetSideChainIndexingFeeController(chainId);
            var association = getInfo.OwnerAddress;
            var checkValidate = MainServices.AssociationService.ValidateOrganizationExist(association).Value;
//            if (!checkValidate)
//            {
//                _logger.Info("Because of changed LifeTimeController, create new IndexingFeeController");
//                var createSideChainIndexingFeeController =
//                    MainServices.AssociationService.CreateOrganization(
//                        CreateOrganizationInput.Parser.ParseFrom(getInfo.OrganizationCreationInputBytes));
//                var associationInfo =
//                    MainServices.AssociationService.GetOrganization(createSideChainIndexingFeeController);
//                associationInfo.OrganizationAddress.Equals(association).ShouldBeTrue();
//            }

            var adjustIndexingFeeInput = new AdjustIndexingFeeInput
            {
                IndexingFee = (int) (checkPrice + 1),
                SideChainId = chainId
            };
            var proposalId = MainServices.AssociationService.CreateProposal(
                MainServices.CrossChainService.ContractAddress,
                nameof(CrossChainContractMethod.AdjustIndexingFeePrice), adjustIndexingFeeInput, association,
                InitAccount);
            MainServices.AssociationService.SetAccount(InitAccount);
            MainServices.AssociationService.ApproveProposal(proposalId, InitAccount);
            var getLifeTimeInfo = MainServices.CrossChainService.GetSideChainLifetimeController();
            var lifeTimeController = getLifeTimeInfo.OwnerAddress;
            var lifeTimeControllerContract = getLifeTimeInfo.ContractAddress;
            if (lifeTimeControllerContract.Equals(MainServices.ParliamentService.ContractAddress.ConvertAddress()))
            {
                var approveProposalId = MainServices.ParliamentService.CreateProposal(
                    MainServices.AssociationService.ContractAddress, nameof(AssociationMethod.Approve), proposalId,
                    lifeTimeController, InitAccount);
                foreach (var miner in Miners) MainServices.ParliamentService.ApproveProposal(approveProposalId, miner);

                MainServices.ParliamentService.ReleaseProposal(approveProposalId, InitAccount);
            }
            else
            {
                var proposer = MainServices.AssociationService.GetOrganization(lifeTimeController).ProposerWhiteList
                    .Proposers.First();
                var approveProposalId = MainServices.AssociationService.CreateProposal(
                    MainServices.AssociationService.ContractAddress, nameof(AssociationMethod.Approve), proposalId,
                    lifeTimeController, proposer.GetFormatted());
                ApproveWithAssociation(MainServices, approveProposalId, lifeTimeController);
                MainServices.AssociationService.ReleaseProposal(approveProposalId, proposer.GetFormatted());
            }

            MainServices.AssociationService.ReleaseProposal(proposalId, InitAccount);

            var afterCheckPrice = MainServices.CrossChainService.GetSideChainIndexingFeePrice(chainId);
            afterCheckPrice.ShouldBe(checkPrice + 1);
        }

        [TestMethod]
        public void ChangeIndexFeeWithOtherAddress()
        {
            var chainId = ChainHelper.ConvertBase58ToChainId("tDVV");
            var lifeController = MainServices.CrossChainService.GetSideChainLifetimeController().OwnerAddress;
            _logger.Info($"life controller is {lifeController}");
            var controllerInfo = MainServices.CrossChainService.GetSideChainIndexingFeeController(chainId);
            var info = MainServices.AssociationService.GetOrganization(controllerInfo.OwnerAddress);
            _logger.Info(
                $"proposer are : {info.ProposerWhiteList.Proposers}, members are {info.OrganizationMemberList.OrganizationMembers}");
            _logger.Info($"indexing fee controller is {controllerInfo.OwnerAddress}");

            var checkPrice = MainServices.CrossChainService.GetSideChainIndexingFeePrice(chainId);
            var adjustIndexingFeeInput = new AdjustIndexingFeeInput
            {
                IndexingFee = (int) (checkPrice + 10),
                SideChainId = chainId
            };
            //create proposal to adjust indexing fee
            var createProposalInput = new CreateProposalInput
            {
                Params = adjustIndexingFeeInput.ToByteString(),
                ContractMethodName = nameof(CrossChainContractMethod.AdjustIndexingFeePrice),
                OrganizationAddress = controllerInfo.OwnerAddress,
                ExpiredTime = KernelHelper.GetUtcNow().AddDays(1),
                ToAddress = AddressHelper.Base58StringToAddress(MainServices.CrossChainService.ContractAddress)
            };
            var createProposalToAdjust = MainServices.AssociationService.CreateProposal(
                MainServices.AssociationService.ContractAddress, nameof(AssociationMethod.CreateProposal),
                createProposalInput, lifeController, OtherAccount);
            ApproveWithAssociation(MainServices, createProposalToAdjust, lifeController);
            var releaseResult = MainServices.AssociationService.ReleaseProposal(createProposalToAdjust, OtherAccount);
            var proposalId = ProposalCreated.Parser.ParseFrom(ByteString.FromBase64(releaseResult.Logs
                .First(l => l.Name.Contains(nameof(ProposalCreated))).NonIndexed)).ProposalId;

            //create proposal to Approve by controller
            var createProposalToApprove = MainServices.AssociationService.CreateProposal(
                MainServices.AssociationService.ContractAddress, nameof(AssociationMethod.Approve),
                proposalId, lifeController, OtherAccount);
            ApproveWithAssociation(MainServices, createProposalToApprove, lifeController);
            MainServices.AssociationService.ReleaseProposal(createProposalToApprove, OtherAccount);

            //create proposal to Approve by creator
            var creator = AddressHelper.Base58StringToAddress("2EBXKkQfGz4fD1xacTiAXp7JksTpECTXJy5MSuYyEzdLbsanZW");
            var createProposalToApproveByCreator = MainServices.AssociationService.CreateProposal(
                MainServices.AssociationService.ContractAddress, nameof(AssociationMethod.Approve),
                proposalId, creator, OtherAccount);
            ApproveWithAssociation(MainServices, createProposalToApproveByCreator, creator);
            MainServices.AssociationService.ReleaseProposal(createProposalToApproveByCreator, OtherAccount);

            var proposalStatue = MainServices.AssociationService.CheckProposal(proposalId).ToBeReleased;
            _logger.Info($"Adjust indexing fee proposal statue is {proposalStatue}");

            // create proposal to release
            var createProposalToRelease = MainServices.AssociationService.CreateProposal(
                MainServices.AssociationService.ContractAddress, nameof(AssociationMethod.Release),
                proposalId, lifeController, OtherAccount);
            ApproveWithAssociation(MainServices, createProposalToRelease, lifeController);
            MainServices.AssociationService.ReleaseProposal(createProposalToRelease, OtherAccount);

            var afterCheckPrice = MainServices.CrossChainService.GetSideChainIndexingFeePrice(chainId);
            afterCheckPrice.ShouldBe(checkPrice + 10);
        }

        [TestMethod]
        public void ChangeSideChainIndexingFeeController()
        {
            var chainId = ChainHelper.ConvertBase58ToChainId("tDVW");
            var associationOrganization = CreateAssociationOrganization(MainServices);
            var input = new AuthorityInfo
            {
                ContractAddress = MainServices.AssociationService.Contract,
                OwnerAddress = associationOrganization
            };
            var controllerOrganization =
                MainServices.CrossChainService.GetSideChainIndexingFeeController(chainId).OwnerAddress;

            var createProposal = MainServices.AssociationService.CreateProposal(
                MainServices.CrossChainService.ContractAddress,
                nameof(CrossChainContractMethod.ChangeSideChainIndexingFeeController), input, controllerOrganization,
                InitAccount);
            MainServices.AssociationService.SetAccount(InitAccount);
            MainServices.AssociationService.ApproveProposal(createProposal, InitAccount);

            var getLifeTimeInfo = MainServices.CrossChainService.GetSideChainLifetimeController();
            var lifeTimeController = getLifeTimeInfo.OwnerAddress;
            var lifeTimeControllerContract = getLifeTimeInfo.ContractAddress;
            if (lifeTimeControllerContract.Equals(MainServices.ParliamentService.ContractAddress.ConvertAddress()))
            {
                var approveProposalId = MainServices.ParliamentService.CreateProposal(
                    MainServices.AssociationService.ContractAddress, nameof(AssociationMethod.Approve), createProposal,
                    lifeTimeController, InitAccount);
                foreach (var miner in Miners) MainServices.ParliamentService.ApproveProposal(approveProposalId, miner);

                MainServices.ParliamentService.ReleaseProposal(approveProposalId, InitAccount);
            }

            MainServices.AssociationService.ReleaseProposal(createProposal, InitAccount);
            controllerOrganization =
                MainServices.CrossChainService.GetSideChainIndexingFeeController(chainId).OwnerAddress;
            controllerOrganization.ShouldBe(associationOrganization);

            //change index fee
            var proposer = MainServices.AssociationService.GetOrganization(associationOrganization).ProposerWhiteList
                .Proposers.First();
            var checkPrice = MainServices.CrossChainService.GetSideChainIndexingFeePrice(chainId);
            var adjustIndexingFeeInput = new AdjustIndexingFeeInput
            {
                IndexingFee = (int) (checkPrice + 1),
                SideChainId = chainId
            };

            var changeProposal = MainServices.AssociationService.CreateProposal(
                MainServices.CrossChainService.ContractAddress, nameof(CrossChainContractMethod.AdjustIndexingFeePrice),
                adjustIndexingFeeInput, associationOrganization, proposer.GetFormatted());
            MainServices.AssociationService.ApproveWithAssociation(changeProposal, associationOrganization);
            var changeRelease =
                MainServices.AssociationService.ReleaseProposal(changeProposal, proposer.GetFormatted());

            var afterPrice = MainServices.CrossChainService.GetSideChainIndexingFeePrice(chainId);
            afterPrice.ShouldBe(checkPrice + 1);
        }

        [TestMethod]
        public void ChangeLifeController()
        {
            var associationOrganization = CreateAssociationOrganization(MainServices);
            var input = new AuthorityInfo
            {
                ContractAddress = AddressHelper.Base58StringToAddress(MainServices.AssociationService.ContractAddress),
                OwnerAddress = associationOrganization
            };
            var controllerOrganization = MainServices.CrossChainService.GetSideChainLifetimeController().OwnerAddress;
            var createProposal = MainServices.ParliamentService.CreateProposal(
                MainServices.CrossChainService.ContractAddress,
                nameof(CrossChainContractMethod.ChangeSideChainLifetimeController), input, controllerOrganization,
                InitAccount);
            foreach (var miner in Miners) MainServices.ParliamentService.ApproveProposal(createProposal, miner);

            MainServices.ParliamentService.ReleaseProposal(createProposal, InitAccount);
            controllerOrganization = MainServices.CrossChainService.GetSideChainLifetimeController().OwnerAddress;
            controllerOrganization.ShouldBe(associationOrganization);
        }

        [TestMethod]
        public void ChangeLifeControllerWithOtherController()
        {
            TransferToken(MainServices, InitAccount, OtherAccount, 10000_00000000, "ELF");
            var associationOrganization = CreateAssociationOrganization(MainServices);
            var input = new AuthorityInfo
            {
                ContractAddress = AddressHelper.Base58StringToAddress(MainServices.AssociationService.ContractAddress),
                OwnerAddress = associationOrganization
            };
            var controllerOrganization = MainServices.CrossChainService.GetSideChainLifetimeController().OwnerAddress;
            var createProposal = MainServices.AssociationService.CreateProposal(
                MainServices.CrossChainService.ContractAddress,
                nameof(CrossChainContractMethod.ChangeSideChainLifetimeController), input, controllerOrganization,
                OtherAccount);
            ApproveWithAssociation(MainServices, createProposal, associationOrganization);
            MainServices.AssociationService.ReleaseProposal(createProposal, OtherAccount);
        }

        [TestMethod]
        public void ChangeIndexingController()
        {
            var parliamentOrganization = CreateParliamentOrganization(MainServices);
            var input = new AuthorityInfo
            {
                ContractAddress = AddressHelper.Base58StringToAddress(MainServices.ParliamentService.ContractAddress),
                OwnerAddress = parliamentOrganization
            };
            var defaultOrganization = MainServices.CrossChainService.GetCrossChainIndexingController().OwnerAddress;
            var createProposal = MainServices.ParliamentService.CreateProposal(
                MainServices.CrossChainService.ContractAddress,
                nameof(CrossChainContractMethod.ChangeCrossChainIndexingController), input, defaultOrganization,
                InitAccount);
            foreach (var miner in Miners)
            {
//                MainServices.TokenService.IssueBalance(InitAccount, miner,
//                    100_00000000, MainServices.TokenService.GetPrimaryTokenSymbol());
                MainServices.ParliamentService.ApproveProposal(createProposal, miner);
            }

            MainServices.ParliamentService.ReleaseProposal(createProposal, InitAccount);

            var updateOrganization = MainServices.CrossChainService.GetCrossChainIndexingController();
            updateOrganization.ContractAddress.ShouldBe(MainServices.ParliamentService.Contract);
        }

        [TestMethod]
        public void GetIndexingController()
        {
            var defaultOrganization = MainServices.CrossChainService.GetCrossChainIndexingController();
            defaultOrganization.ContractAddress.ShouldBe(MainServices.AssociationService.Contract);
        }
    }
}