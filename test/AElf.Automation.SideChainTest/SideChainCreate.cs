using System.Linq;
using AElf.Standards.ACS3;
using AElf.Contracts.CrossChain;
using AElf.Contracts.MultiToken;
using AElf.CSharp.Core.Extension;
using AElf.Standards.ACS7;
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
            var associationOrganization = AuthorityManager.CreateAssociationOrganization(Members);
            foreach (var member in Members)
                MainServices.TokenService.TransferBalance(InitAccount, member, 5000_00000000, "ELF");
            MainServices.TokenService.TransferBalance(InitAccount, associationOrganization.ToBase58(), 500000000,
                "ELF");
            var proposer = MainServices.AssociationService.GetOrganization(associationOrganization).ProposerWhiteList
                .Proposers.First().ToBase58();
            ApproveAndTransferOrganizationBalanceAsync(MainServices, associationOrganization, 400000, proposer);
            var sideChainTokenCreationRequest = new SideChainTokenCreationRequest
            {
                SideChainTokenSymbol = "STD",
                SideChainTokenName = "Side chain token STD",
                SideChainTokenDecimals = 8,
                SideChainTokenTotalSupply = 10_00000000_00000000
            };
            var issueAccount = new SideChainTokenInitialIssue
            {
                Address = OtherAccount.ConvertAddress(),
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
                SideChainTokenCreationRequest = sideChainTokenCreationRequest,
                InitialResourceAmount = {{"CPU", 2}, {"RAM", 4}, {"DISK", 512}, {"NET", 1024}},
                SideChainTokenInitialIssueList = {issueAccount, issueOrganization}
            };

            var createProposal = MainServices.AssociationService.CreateProposal(
                MainServices.CrossChainService.ContractAddress,
                nameof(CrossChainContractMethod.RequestSideChainCreation), input, associationOrganization,
                proposer);
            ApproveWithAssociation(MainServices, createProposal, associationOrganization);
            var release = ReleaseWithAssociation(MainServices, createProposal, proposer);

            var createSideChainProposalId = ProposalCreated.Parser
                .ParseFrom(ByteString.FromBase64(release.Logs.First(l => l.Name.Contains(nameof(ProposalCreated)))
                    .NonIndexed)).ProposalId;
            ApproveProposal(MainServices, createSideChainProposalId);

            var releaseInput = new ReleaseSideChainCreationInput {ProposalId = createSideChainProposalId};
            var releaseProposal = MainServices.AssociationService.CreateProposal(
                MainServices.CrossChainService.ContractAddress,
                nameof(CrossChainContractMethod.ReleaseSideChainCreation), releaseInput, associationOrganization,
                proposer);
            ApproveWithAssociation(MainServices, releaseProposal, associationOrganization);
            var releaseRelease = ReleaseWithAssociation(MainServices, releaseProposal, proposer);

            var sideChainCreatedEvent = SideChainCreatedEvent.Parser
                .ParseFrom(ByteString.FromBase64(releaseRelease.Logs
                    .First(l => l.Name.Contains(nameof(SideChainCreatedEvent)))
                    .NonIndexed));
            var chainId = sideChainCreatedEvent.ChainId;
            var creator = sideChainCreatedEvent.Creator;
            creator.ShouldBe(associationOrganization);
            Logger.Info($"chainId : {chainId}; creator :{creator}");
        }

        [TestMethod]
        public void CheckToken()
        {
            var associationOrganization = AuthorityManager.CreateAssociationOrganization(Members);
            var tokenInfo = MainServices.TokenService.GetTokenInfo("STD");
            tokenInfo.Issuer.ShouldBe(associationOrganization);
            var input = new ChangeTokenIssuerInput
            {
                NewTokenIssuer = OtherAccount.ConvertAddress(),
                Symbol = "STD"
            };
            var proposer = MainServices.AssociationService.GetOrganization(associationOrganization).ProposerWhiteList
                .Proposers.First().ToBase58();
            var changeProposal = MainServices.AssociationService.CreateProposal(
                MainServices.TokenService.ContractAddress,
                nameof(TokenMethod.ChangeTokenIssuer), input, associationOrganization,
                proposer);
            ApproveWithAssociation(MainServices, changeProposal, associationOrganization);
            var release = ReleaseWithAssociation(MainServices, changeProposal, proposer);
            release.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            tokenInfo = MainServices.TokenService.GetTokenInfo("STD");
            tokenInfo.Issuer.ShouldBe(OtherAccount.ConvertAddress());
        }

        [TestMethod]
        public void CheckSideToken()
        {
            var symbol = SideAServices.TokenService.GetPrimaryTokenSymbol();
            foreach (var member in Members)
                SideAServices.TokenService.TransferBalance(OtherAccount, member, 100_00000000, symbol);

            var associationOrganization = SideAuthorityManager.CreateAssociationOrganization(Members);
            var account = "2uNAVbedkZQENDayADxRqW5AEXH9H2eLr7Rke82otyVrSLbDpm";
            var tokenInfo = SideAServices.TokenService.GetTokenInfo(symbol);
            tokenInfo.Issuer.ShouldBe(associationOrganization);
            var balance = SideAServices.TokenService.GetUserBalance(account, symbol);
            var userBalance = SideAServices.TokenService.GetUserBalance(OtherAccount, symbol);

            var input = new ChangeTokenIssuerInput
            {
                NewTokenIssuer = OtherAccount.ConvertAddress(),
                Symbol = "STD"
            };
            var proposer = SideAServices.AssociationService.GetOrganization(associationOrganization).ProposerWhiteList
                .Proposers.First().ToBase58();
            var changeProposal = SideAServices.AssociationService.CreateProposal(
                SideAServices.TokenService.ContractAddress,
                nameof(TokenMethod.ChangeTokenIssuer), input, associationOrganization,
                proposer);
            ApproveWithAssociation(SideAServices, changeProposal, associationOrganization);
            var release = ReleaseWithAssociation(SideAServices, changeProposal, proposer);
            release.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            tokenInfo = SideAServices.TokenService.GetTokenInfo(symbol);
            tokenInfo.Issuer.ShouldBe(OtherAccount.ConvertAddress());
        }

        [TestMethod]
        public void RequestSideChainCreation()
        {
            foreach (var member in Members)
                MainServices.TokenService.TransferBalance(InitAccount, member, 1000_000000000);
            var controller = MainServices.CrossChainService.GetSideChainLifetimeController().OwnerAddress;
            var proposer = MainServices.AssociationService.GetOrganization(controller).ProposerWhiteList.Proposers
                .First();
            TokenApprove(MainServices, proposer.ToBase58(), 1000000);

            var sideChainTokenCreationRequest = new SideChainTokenCreationRequest
            {
                SideChainTokenDecimals = 8,
                SideChainTokenName = $"Side chain token STD",
                SideChainTokenSymbol = "STD",
                SideChainTokenTotalSupply = 10_00000000_00000000
            };
            var proposal =
                RequestSideChainCreation(MainServices, proposer.ToBase58(), "123", 1, 1000000, true, sideChainTokenCreationRequest);
            Logger.Info($"proposal id is: {proposal}");
        }

        [TestMethod]
        [DataRow("512f7b0371f4bf13f224c13d61c95d68bca278c5854ef5e29c7f61e7f29159a0")]
        public void ApproveProposalThroughOtherAuthory(string proposalId)
        {
            var proposal = Hash.LoadFromHex(proposalId);
            var controller = MainServices.CrossChainService.GetSideChainLifetimeController().OwnerAddress;
            ApproveWithAssociation(MainServices, proposal, controller);
        }

        [TestMethod]
        [DataRow("4c075565962d2c8f90fe45817145ce0b3eafe65b818daccabf91f3d3ed9cce1c")]
        public void ApproveProposal(string proposalId)
        {
            var proposal = Hash.LoadFromHex(proposalId);
            ApproveProposal(MainServices, proposal);
        }

        [TestMethod]
        [DataRow("512f7b0371f4bf13f224c13d61c95d68bca278c5854ef5e29c7f61e7f29159a0")]
        public void ReleaseSideChainCreation(string proposalId)
        {
            var controller = MainServices.CrossChainService.GetSideChainLifetimeController().OwnerAddress;
            var proposer = MainServices.AssociationService.GetOrganization(controller).ProposerWhiteList.Proposers
                .First();
            MainServices.CrossChainService.SetAccount(proposer.ToBase58());
            var result
                = MainServices.CrossChainService.ExecuteMethodWithResult(
                    CrossChainContractMethod.ReleaseSideChainCreation,
                    new ReleaseSideChainCreationInput
                    {
                        ProposalId = Hash.LoadFromHex(proposalId)
                    });
            var release = result.Logs.First(l => l.Name.Contains(nameof(SideChainCreatedEvent)))
                .NonIndexed;
            var byteString = ByteString.FromBase64(release);
            var sideChainCreatedEvent = SideChainCreatedEvent.Parser
                .ParseFrom(byteString);
            var chainId = sideChainCreatedEvent.ChainId;
            var creator = sideChainCreatedEvent.Creator;
            creator.ShouldBe(proposer);
            Logger.Info($"SideChain id is {chainId}, creator {creator}");
        }

        [TestMethod]
        public void CreateProposal()
        {
            TokenApprove(MainServices, InitAccount, 400000);

            var sideChainTokenCreationRequest = new SideChainTokenCreationRequest
            {
                SideChainTokenDecimals = 8,
                SideChainTokenName = "Side chain token SSTA",
                SideChainTokenSymbol = "SSTA",
                SideChainTokenTotalSupply = 10_00000000_00000000
            };
            
            var proposalId = RequestSideChainCreation(MainServices, InitAccount, "123", 1, 400000, true, sideChainTokenCreationRequest);
            ApproveProposal(MainServices, proposalId);
            var releaseResult = ReleaseSideChainCreation(MainServices, InitAccount, proposalId);
            var release = releaseResult.Logs.First(l => l.Name.Contains(nameof(SideChainCreatedEvent)))
                .NonIndexed;
            var byteString = ByteString.FromBase64(release);
            var sideChainCreatedEvent = SideChainCreatedEvent.Parser
                .ParseFrom(byteString);
            var chainId = sideChainCreatedEvent.ChainId;
            var creator = sideChainCreatedEvent.Creator;

            Logger.Info($"Side chain id is {chainId}, creator is {creator}");
        }

        [TestMethod]
        public void CreateProposalOnSideChain()
        {
            var side = SideServices.Last();
            TokenApprove(side, InitAccount, 400000);
            var sideChainTokenCreationRequest = new SideChainTokenCreationRequest
            {
                SideChainTokenDecimals = 8,
                SideChainTokenName = "Side chain token SSTA",
                SideChainTokenSymbol = "SSTA",
                SideChainTokenTotalSupply = 10_00000000_00000000
            };
            var proposalId = RequestSideChainCreation(side, InitAccount, "123", 1, 400000, true, sideChainTokenCreationRequest);
            ApproveProposal(side, proposalId);
            var releaseResult = ReleaseSideChainCreation(side, InitAccount, proposalId);
            var release = releaseResult.Logs.First(l => l.Name.Contains(nameof(SideChainCreatedEvent)))
                .NonIndexed;
            var byteString = ByteString.FromBase64(release);
            var sideChainCreatedEvent = SideChainCreatedEvent.Parser
                .ParseFrom(byteString);
            var chainId = sideChainCreatedEvent.ChainId;
            var creator = sideChainCreatedEvent.Creator;

            Logger.Info($"Side chain id is {chainId}, creator is {creator}");
        }

        [TestMethod]
        public void GetProposal()
        {
//            var proposalId = ProposalCreated.Parser
//                .ParseFrom(ByteString.FromBase64("CiIKIMO8feW9yiU85cN3SXgfIzJ5Od35XIhfYvwsmHmAcuhM")).ProposalId;
            var proposalId =
                Hash.LoadFromHex("006844e445373e45cd196cffc696744eba46f6ea2741826b3817558771578cd2");
            var result = MainServices.AssociationService.CheckProposal(proposalId);
            Logger.Info(
                $"proposal message is {result.ToBeReleased} {result.ApprovalCount}");
        }

        [TestMethod]
        public void GetChainInitializationData()
        {
            var chainId = ChainHelper.ConvertBase58ToChainId("tDVV");
            var sideChainInfo = MainServices.CrossChainService.GetChainInitializationData(chainId);
            Logger.Info($"{ sideChainInfo.CreationHeightOnParentChain},{sideChainInfo.Creator},{sideChainInfo.ChainCreatorPrivilegePreserved}");
        }

        [TestMethod]
        // [DataRow("tDVW")]
        [DataRow("tDVV")]
        public void CheckStatus(string chainId)
        {
            var intChainId = ChainHelper.ConvertBase58ToChainId(chainId);
            var status = MainServices.CrossChainService.GetChainStatus(intChainId).Status;
            Logger.Info($"side chain is {status}");
        }

        [TestMethod]
        // [DataRow("tDVW")]
        [DataRow("tDVV")]
        public void CheckSideChainBalance(string chainId)
        {
            var intChainId = ChainHelper.ConvertBase58ToChainId(chainId);
            var balance = MainServices.CrossChainService.GetSideChainBalance(intChainId);
            Logger.Info($"side chain is {balance}");
        }

        [TestMethod]
        [DataRow("tDVW")]
        public void Recharge(string chainId)
        {
            var intChainId = ChainHelper.ConvertBase58ToChainId(chainId);
            var balance = MainServices.CrossChainService.GetSideChainBalance(intChainId);
            var status = MainServices.CrossChainService.GetChainStatus(intChainId);
            if (status.Status.Equals(SideChainStatus.IndexingFeeDebt))
                balance.ShouldBe(0);
            var indexingFeeDebt =
                MainServices.CrossChainService.CallViewMethod<Int64Value>(
                    CrossChainContractMethod.GetSideChainIndexingFeeDebt, new Int32Value {Value = intChainId});
            var approve = MainServices.TokenService.ExecuteMethodWithResult(TokenMethod.Approve, new ApproveInput
            {
                Spender = MainServices.CrossChainService.Contract,
                Symbol = MainServices.TokenService.GetNativeTokenSymbol(),
                Amount = indexingFeeDebt.Value + 10000
            });
            approve.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var result = Recharge(MainServices, InitAccount, intChainId, indexingFeeDebt.Value + 10000);
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            status = MainServices.CrossChainService.GetChainStatus(intChainId);
            status.Status.ShouldBe(SideChainStatus.Active);
            var afterBalance = MainServices.CrossChainService.GetSideChainBalance(intChainId);
            afterBalance.ShouldBe(balance + 10000);
        }

        [TestMethod]
        public void DisposeSideChain()
        {
            TransferToken(MainServices, InitAccount, OtherAccount, 1000_00000000,
                MainServices.TokenService.GetPrimaryTokenSymbol());
            var chainId = ChainHelper.ConvertBase58ToChainId("tDVV");
            var chainBalance = MainServices.CrossChainService.GetSideChainBalance(chainId);
            var creator = MainServices.CrossChainService.GetChainInitializationData(chainId).Creator;
            var input = new Int32Value {Value = chainId};
            var organization = MainServices.CrossChainService.GetSideChainLifetimeController();
            var balance = MainServices.TokenService.GetUserBalance(creator.ToBase58());

            var createProposal = MainServices.ParliamentService.CreateProposal(
                MainServices.CrossChainService.ContractAddress, nameof(CrossChainContractMethod.DisposeSideChain),
                input, organization.OwnerAddress, InitAccount);
            foreach (var miner in Miners) MainServices.ParliamentService.ApproveProposal(createProposal, miner);

            MainServices.ParliamentService.ReleaseProposal(createProposal, InitAccount);
            var chainStatue = MainServices.CrossChainService.GetChainStatus(chainId).Status;
            chainStatue.ShouldBe(SideChainStatus.Terminated);
            var afterBalance = MainServices.TokenService.GetUserBalance(creator.ToBase58());
            afterBalance.ShouldBeLessThanOrEqualTo(balance + chainBalance);
            Logger.Info($"before balance is {balance}, after balance is {afterBalance}");
        }

        [TestMethod]
        public void DisposeSideChainThroughAssociation()
        {
            TransferToken(MainServices, InitAccount, OtherAccount, 1000_00000000,
                MainServices.TokenService.GetPrimaryTokenSymbol());
            var chainId = ChainHelper.ConvertBase58ToChainId("tDVX");
            var input = new Int32Value {Value = chainId};
            var organization = MainServices.CrossChainService.GetSideChainLifetimeController();
            if (organization.ContractAddress.Equals(MainServices.ParliamentService.Contract))
            {
                var result = AuthorityManager.ExecuteTransactionWithAuthority(
                    MainServices.CrossChainService.ContractAddress, nameof(CrossChainContractMethod.DisposeSideChain),
                    input, InitAccount, organization.OwnerAddress);
                result.Status.ShouldBe(TransactionResultStatus.Mined);
            }
            else if (organization.ContractAddress.Equals(MainServices.AssociationService.Contract))
            {
                var createProposal = MainServices.AssociationService.CreateProposal(
                    MainServices.CrossChainService.ContractAddress, nameof(CrossChainContractMethod.DisposeSideChain),
                    input, organization.OwnerAddress, InitAccount);
                MainServices.AssociationService.ApproveWithAssociation(createProposal, organization.OwnerAddress);
                MainServices.AssociationService.ReleaseProposal(createProposal, InitAccount);
            }

            var chainStatue = MainServices.CrossChainService.GetChainStatus(chainId).Status;
            chainStatue.ShouldBe(SideChainStatus.Terminated);
            CheckBalance(InitAccount);
        }

        [TestMethod]
        [DataRow("28Y8JA1i2cN6oHvdv7EraXJr9a1gY6D1PpJXw9QtRMRwKcBQMK")]
        public void CheckBalance(string account)
        {
            var balance = GetBalance(MainServices, MainServices.CrossChainService.ContractAddress,
                NodeOption.NativeTokenSymbol);
            Logger.Info($"side chain balance is {balance}");

            var userBalance = GetBalance(MainServices, account, NodeOption.NativeTokenSymbol);
            Logger.Info($"user balance is {userBalance}");
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
            var controllerInfo = MainServices.AssociationService.GetOrganization(association);
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
            var getIndexControllerInfo = MainServices.CrossChainService.GetCrossChainIndexingController();
            var indexController = getIndexControllerInfo.OwnerAddress;
            var indexControllerContract = getIndexControllerInfo.ContractAddress;
            controllerInfo.ProposerWhiteList.Proposers.Contains(indexController).ShouldBeTrue();
            if (indexControllerContract.Equals(MainServices.ParliamentService.ContractAddress.ConvertAddress()))
            {
                var approveProposalId = MainServices.ParliamentService.CreateProposal(
                    MainServices.AssociationService.ContractAddress, nameof(AssociationMethod.Approve), proposalId,
                    indexController, InitAccount);
                foreach (var miner in Miners) MainServices.ParliamentService.ApproveProposal(approveProposalId, miner);

                MainServices.ParliamentService.ReleaseProposal(approveProposalId, InitAccount);
            }
            else
            {
                var proposer = MainServices.AssociationService.GetOrganization(indexController).ProposerWhiteList
                    .Proposers.First();
                var approveProposalId = MainServices.AssociationService.CreateProposal(
                    MainServices.AssociationService.ContractAddress, nameof(AssociationMethod.Approve), proposalId,
                    indexController, proposer.ToBase58());
                ApproveWithAssociation(MainServices, approveProposalId, indexController);
                MainServices.AssociationService.ReleaseProposal(approveProposalId, proposer.ToBase58());
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
            Logger.Info($"life controller is {lifeController}");
            var controllerInfo = MainServices.CrossChainService.GetSideChainIndexingFeeController(chainId);
            var info = MainServices.AssociationService.GetOrganization(controllerInfo.OwnerAddress);
            Logger.Info(
                $"proposer are : {info.ProposerWhiteList.Proposers}, members are {info.OrganizationMemberList.OrganizationMembers}");
            Logger.Info($"indexing fee controller is {controllerInfo.OwnerAddress}");

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
                ToAddress = MainServices.CrossChainService.Contract
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
            var creator = ("2EBXKkQfGz4fD1xacTiAXp7JksTpECTXJy5MSuYyEzdLbsanZW").ConvertAddress();
            var createProposalToApproveByCreator = MainServices.AssociationService.CreateProposal(
                MainServices.AssociationService.ContractAddress, nameof(AssociationMethod.Approve),
                proposalId, creator, OtherAccount);
            ApproveWithAssociation(MainServices, createProposalToApproveByCreator, creator);
            MainServices.AssociationService.ReleaseProposal(createProposalToApproveByCreator, OtherAccount);

            var proposalStatue = MainServices.AssociationService.CheckProposal(proposalId).ToBeReleased;
            Logger.Info($"Adjust indexing fee proposal statue is {proposalStatue}");

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
            foreach (var member in Members)
            {
                var balance = MainServices.TokenService.GetUserBalance(member, "ELF");
                if (member.Equals(InitAccount) || balance > 100_00000000)
                    continue;
                MainServices.TokenService.TransferBalance(InitAccount, member, 1000_00000000);
            }

            var associationOrganization = AuthorityManager.CreateAssociationOrganization(Members);
            var input = new ChangeSideChainIndexingFeeControllerInput
            {
                AuthorityInfo = new Acs1.AuthorityInfo
                {
                    ContractAddress = MainServices.AssociationService.Contract,
                    OwnerAddress = associationOrganization
                },
                ChainId = chainId
            };
            var controllerOrganization =
                MainServices.CrossChainService.GetSideChainIndexingFeeController(chainId);

            var createProposal = MainServices.AssociationService.CreateProposal(
                MainServices.CrossChainService.ContractAddress,
                nameof(CrossChainContractMethod.ChangeSideChainIndexingFeeController), input,
                controllerOrganization.OwnerAddress,
                InitAccount);
            MainServices.AssociationService.SetAccount(InitAccount);
            MainServices.AssociationService.ApproveProposal(createProposal, InitAccount);

            var getIndexControllerInfo = MainServices.CrossChainService.GetCrossChainIndexingController();
            var indexController = getIndexControllerInfo.OwnerAddress;
            var indexControllerContract = getIndexControllerInfo.ContractAddress;
            if (indexControllerContract.Equals(MainServices.ParliamentService.ContractAddress.ConvertAddress()))
            {
                var approveProposalId = MainServices.ParliamentService.CreateProposal(
                    MainServices.AssociationService.ContractAddress, nameof(AssociationMethod.Approve), createProposal,
                    indexController, Miners.First());
                foreach (var miner in Miners) MainServices.ParliamentService.ApproveProposal(approveProposalId, miner);

                MainServices.ParliamentService.ReleaseProposal(approveProposalId, Miners.First());
            }

            MainServices.AssociationService.ReleaseProposal(createProposal, InitAccount);

            var updateControllerOrganization =
                MainServices.CrossChainService.GetSideChainIndexingFeeController(chainId).OwnerAddress;
            updateControllerOrganization.ShouldBe(associationOrganization);

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
                adjustIndexingFeeInput, associationOrganization, proposer.ToBase58());
            MainServices.AssociationService.ApproveWithAssociation(changeProposal, associationOrganization);
            var changeRelease =
                MainServices.AssociationService.ReleaseProposal(changeProposal, proposer.ToBase58());
            changeRelease.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var afterPrice = MainServices.CrossChainService.GetSideChainIndexingFeePrice(chainId);
            afterPrice.ShouldBe(checkPrice + 1);

            //recover
            var recoverInput = new AuthorityInfo
            {
                ContractAddress = controllerOrganization.ContractAddress,
                OwnerAddress = controllerOrganization.OwnerAddress
            };

            var recoverProposal = MainServices.AssociationService.CreateProposal(
                MainServices.CrossChainService.ContractAddress,
                nameof(CrossChainContractMethod.ChangeSideChainIndexingFeeController),
                recoverInput, associationOrganization, proposer.ToBase58());
            MainServices.AssociationService.ApproveWithAssociation(recoverProposal, associationOrganization);
            var recoverRelease =
                MainServices.AssociationService.ReleaseProposal(recoverProposal, proposer.ToBase58());
            recoverRelease.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var recoverController =
                MainServices.CrossChainService.GetSideChainIndexingFeeController(chainId).OwnerAddress;
            recoverController.ShouldBe(controllerOrganization.OwnerAddress);
        }

        [TestMethod]
        public void ChangeSideChainIndexingControllerToParliament()
        {
            var chainId = ChainHelper.ConvertBase58ToChainId("tDVW");
            var getIndexControllerInfo = MainServices.CrossChainService.GetCrossChainIndexingController();
            var associationOrganization = AuthorityManager.CreateAssociationOrganization(Members);
            var proposer = MainServices.AssociationService.GetOrganization(associationOrganization).ProposerWhiteList
                .Proposers.First();

            var input = new ChangeSideChainIndexingFeeControllerInput
            {
                AuthorityInfo = new Acs1.AuthorityInfo
                {
                    ContractAddress = getIndexControllerInfo.ContractAddress,
                    OwnerAddress = getIndexControllerInfo.OwnerAddress
                },
                ChainId = chainId
            };
            var controllerOrganization =
                MainServices.CrossChainService.GetSideChainIndexingFeeController(chainId);

            var changeProposal = MainServices.AssociationService.CreateProposal(
                MainServices.CrossChainService.ContractAddress,
                nameof(CrossChainContractMethod.ChangeSideChainIndexingFeeController),
                input, associationOrganization, proposer.ToBase58());
            MainServices.AssociationService.ApproveWithAssociation(changeProposal, associationOrganization);
            var changeRelease =
                MainServices.AssociationService.ReleaseProposal(changeProposal, proposer.ToBase58());
            changeRelease.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var updateController =
                MainServices.CrossChainService.GetSideChainIndexingFeeController(chainId).OwnerAddress;
            updateController.ShouldBe(getIndexControllerInfo.OwnerAddress);

            //change index fee
            var checkPrice = MainServices.CrossChainService.GetSideChainIndexingFeePrice(chainId);
            var adjustIndexingFeeInput = new AdjustIndexingFeeInput
            {
                IndexingFee = (int) (checkPrice + 1),
                SideChainId = chainId
            };

            var changeFeeProposal = MainServices.ParliamentService.CreateProposal(
                MainServices.CrossChainService.ContractAddress, nameof(CrossChainContractMethod.AdjustIndexingFeePrice),
                adjustIndexingFeeInput, getIndexControllerInfo.OwnerAddress, Miners.First());
            MainServices.ParliamentService.MinersApproveProposal(changeFeeProposal, Miners);
            var changeFeeRelease =
                MainServices.ParliamentService.ReleaseProposal(changeFeeProposal, Miners.First());
            changeFeeRelease.Status.ShouldBe(TransactionResultStatus.Mined);
            var afterPrice = MainServices.CrossChainService.GetSideChainIndexingFeePrice(chainId);
            afterPrice.ShouldBe(checkPrice + 1);
        }

        [TestMethod]
        public void ChangeLifeController()
        {
            var associationOrganization = AuthorityManager.CreateAssociationOrganization(Members);
            var input = new AuthorityInfo
            {
                ContractAddress = MainServices.AssociationService.Contract,
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
            var associationOrganization = AuthorityManager.CreateAssociationOrganization(Members);
            var input = new AuthorityInfo
            {
                ContractAddress = MainServices.AssociationService.Contract,
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
            var parliamentOrganization = AuthorityManager.CreateNewParliamentOrganization(InitAccount);
            var input = new AuthorityInfo
            {
                ContractAddress = MainServices.ParliamentService.Contract,
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

        [TestMethod]
        public void GetIndexingFeeController()
        {
            var chainId = ChainHelper.ConvertBase58ToChainId("tDVW");
            var defaultOrganization = MainServices.CrossChainService.GetSideChainIndexingFeeController(chainId);
            var price = MainServices.CrossChainService.GetSideChainIndexingFeePrice(chainId);

            var getIndexControllerInfo = MainServices.CrossChainService.GetCrossChainIndexingController();
            var indexController = getIndexControllerInfo.OwnerAddress;
            var indexControllerContract = getIndexControllerInfo.ContractAddress;

            Logger.Info($"{defaultOrganization} {price}" +
                        $"{getIndexControllerInfo}");

            var info = MainServices.ParliamentService.GetOrganization(indexController);
            Logger.Info($"{info}");

            var associationInfo = MainServices.AssociationService.GetOrganization(defaultOrganization.OwnerAddress);
            Logger.Info($"{associationInfo}");
        }
    }
}