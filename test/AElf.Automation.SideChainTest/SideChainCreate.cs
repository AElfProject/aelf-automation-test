using System.Linq;
using Acs3;
using Acs7;
using AElf.Contracts.Association;
using AElfChain.Common;
using AElf.Contracts.CrossChain;
using AElf.Kernel;
using AElf.Sdk.CSharp;
using AElf.Types;
using AElfChain.Common.Contracts;
using Google.Protobuf;
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
        [DataRow("CnVL7BcRcaYGVovoiz4eiv4ZZFyJR7vjBmqgcZqgmicVXKKpx")]
        public void TransferSideChain(string account)
        {
            TransferToken(MainServices, InitAccount, account, 121000_00000000, NodeOption.NativeTokenSymbol);
            TransferToken(SideAServices, InitAccount, account, 121000_00000000, GetPrimaryTokenSymbol(SideAServices));
            TransferToken(SideBServices, InitAccount, account, 121000_00000000, GetPrimaryTokenSymbol(SideBServices));

            _logger.Info($"{GetBalance(MainServices, InitAccount, NodeOption.NativeTokenSymbol).Balance}");
            _logger.Info($"{GetBalance(SideAServices, InitAccount, GetPrimaryTokenSymbol(SideAServices)).Balance}");
            _logger.Info($"{GetBalance(SideBServices, InitAccount, GetPrimaryTokenSymbol(SideBServices)).Balance}");

            _logger.Info($"{GetBalance(MainServices, account, NodeOption.NativeTokenSymbol)}");
            _logger.Info($"{GetBalance(SideAServices, account, GetPrimaryTokenSymbol(SideAServices)).Balance}");
            _logger.Info($"{GetBalance(SideBServices, account, GetPrimaryTokenSymbol(SideBServices)).Balance}");
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
        [DataRow("4401e46059f2f829cfb3f69f97fe8b1f4ee3d58356d5a74717c13d4925a8b024")]
        [DataRow("2323f166cfaa67f611b428bbcd5cb0ba47c027b41e6e28a536d02873329dbc48")]
        public void ApproveProposal(string proposalId)
        {
            foreach (var bp in Miners)
            {
                var result = Approve(MainServices, bp, proposalId);
                _logger.Info($"Approve is {result.ReadableReturnValue}");
            }
        }

        [TestMethod]
        public void CreateProposal()
        {
            TokenApprove(MainServices, InitAccount, 400000);
            var tokenInfo = new SideChainTokenInfo
            {
                Symbol = "STD",
                TokenName = "Side chain token STD",
                Decimals = 8,
                IsBurnable = true,
                Issuer = AddressHelper.Base58StringToAddress(InitAccount),
                TotalSupply = 10_0000_0000_00000000
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
        public void GetProposal()
        {
            var proposalId = ProposalCreated.Parser.ParseFrom(ByteString.FromBase64("CiIKIMO8feW9yiU85cN3SXgfIzJ5Od35XIhfYvwsmHmAcuhM")).ProposalId;
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
        [DataRow("tDVY")]
        public void CheckStatus(string chainId)
        {
            var intChainId = ChainHelper.ConvertBase58ToChainId(chainId);
            var status = GetChainStatus(MainServices, intChainId);
            _logger.Info($"side chain is {status}");
        }

        [TestMethod]
        [DataRow("tDVW")]
        public void Recharge(string chainId)
        {
            var intChainId = ChainHelper.ConvertBase58ToChainId(chainId);
            var status = Recharge(MainServices, InitAccount, intChainId, 200000);
            _logger.Info($" Transaction is {status.Status}");
        }


        [TestMethod]
        public void DisposeSideChain()
        {
            var chainId = ChainHelper.ConvertBase58ToChainId("tDVY");
            var input = new SInt32Value {Value = chainId};
            var organization = MainServices.CrossChainService.GetSideChainLifetimeController();
            var createProposal = MainServices.ParliamentService.CreateProposal(
                MainServices.CrossChainService.ContractAddress, nameof(CrossChainContractMethod.DisposeSideChain),
                input, organization.OwnerAddress, InitAccount);
            foreach (var miner in Miners)
            {
                MainServices.ParliamentService.ApproveProposal(createProposal, miner);
            }

            MainServices.ParliamentService.ReleaseProposal(createProposal, InitAccount);
            var chainStatue = GetChainStatus(MainServices, chainId);
            chainStatue.Value.ShouldBe(2);
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
            var chainId = ChainHelper.ConvertBase58ToChainId("tDVV");

            var checkPrice = MainServices.CrossChainService.GetSideChainIndexingFeePrice(chainId);
            var association = MainServices.CrossChainService.GetSideChainIndexingFeeController(chainId).AuthorityStuff
                .OwnerAddress;
            var adjustIndexingFeeInput = new AdjustIndexingFeeInput
            {
                IndexingFee = (int) (checkPrice + 10),
                SideChainId = chainId
            };
            var proposalId = MainServices.AssociationService.CreateProposal(
                MainServices.CrossChainService.ContractAddress,
                nameof(CrossChainContractMethod.AdjustIndexingFeePrice), adjustIndexingFeeInput, association,
                InitAccount);
            MainServices.AssociationService.SetAccount(InitAccount);
            MainServices.AssociationService.ApproveProposal(proposalId, InitAccount);
            var defaultOrganization = MainServices.ParliamentService.GetGenesisOwnerAddress();
            var approveProposalId = MainServices.ParliamentService.CreateProposal(
                MainServices.AssociationService.ContractAddress, nameof(AssociationMethod.Approve), proposalId,
                defaultOrganization, InitAccount);
            foreach (var miner in Miners)
            {
                MainServices.ParliamentService.ApproveProposal(approveProposalId, miner);
            }

            MainServices.ParliamentService.ReleaseProposal(approveProposalId, InitAccount);
            MainServices.AssociationService.ReleaseProposal(proposalId, InitAccount);

            var afterCheckPrice = MainServices.CrossChainService.GetSideChainIndexingFeePrice(chainId);
            afterCheckPrice.ShouldBe(checkPrice + 10);
        }

        [TestMethod]
        public void ChangeIndexFeeWithOtherAddress()
        {
            var chainId = ChainHelper.ConvertBase58ToChainId("tDVV");
            var lifeController = MainServices.CrossChainService.GetSideChainLifetimeController().OwnerAddress;
            _logger.Info($"life controller is {lifeController}");
            var controllerInfo = MainServices.CrossChainService.GetSideChainIndexingFeeController(chainId);
            var input = CreateOrganizationInput.Parser.ParseFrom(controllerInfo.OrganizationCreationInputBytes);
            _logger.Info($"proposer are : {input.ProposerWhiteList.Proposers}, members are {input.OrganizationMemberList.OrganizationMembers}");
            var address = CreateAssociationController(InitAccount, input);
            _logger.Info($"indexing fee controller is {address}");
            
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
                OrganizationAddress = address,
                ExpiredTime = TimestampHelper.GetUtcNow().AddDays(1),
                ToAddress = AddressHelper.Base58StringToAddress(MainServices.CrossChainService.ContractAddress)
            };
            var createProposalToAdjust = MainServices.AssociationService.CreateProposal(
                MainServices.AssociationService.ContractAddress, nameof(AssociationMethod.CreateProposal),
                createProposalInput, lifeController, OtherAccount);
            ApproveWithAssociation(MainServices,createProposalToAdjust,lifeController);
            var releaseResult = MainServices.AssociationService.ReleaseProposal(createProposalToAdjust, OtherAccount);
            var proposalId = ProposalCreated.Parser.ParseFrom(ByteString.FromBase64(releaseResult.Logs
                .First(l => l.Name.Contains(nameof(ProposalCreated))).NonIndexed)).ProposalId;
            
            //create proposal to Approve by controller
            var createProposalToApprove = MainServices.AssociationService.CreateProposal(
                MainServices.AssociationService.ContractAddress, nameof(AssociationMethod.Approve),
                proposalId, lifeController, OtherAccount);
            ApproveWithAssociation(MainServices,createProposalToApprove,lifeController);
            MainServices.AssociationService.ReleaseProposal(createProposalToApprove, OtherAccount);
            
            //create proposal to Approve by creator
            var creator = AddressHelper.Base58StringToAddress("2EBXKkQfGz4fD1xacTiAXp7JksTpECTXJy5MSuYyEzdLbsanZW");
            var createProposalToApproveByCreator = MainServices.AssociationService.CreateProposal(
                MainServices.AssociationService.ContractAddress, nameof(AssociationMethod.Approve),
                proposalId, creator, OtherAccount);
            ApproveWithAssociation(MainServices,createProposalToApproveByCreator,creator);
            MainServices.AssociationService.ReleaseProposal(createProposalToApproveByCreator, OtherAccount);
            
            var proposalStatue = MainServices.AssociationService.CheckProposal(proposalId).ToBeReleased;
            _logger.Info($"Adjust indexing fee proposal statue is {proposalStatue}");
           
            // create proposal to release
            var createProposalToRelease = MainServices.AssociationService.CreateProposal(
                MainServices.AssociationService.ContractAddress, nameof(AssociationMethod.Release),
                proposalId, lifeController, OtherAccount);
            ApproveWithAssociation(MainServices,createProposalToRelease,lifeController);
            MainServices.AssociationService.ReleaseProposal(createProposalToRelease, OtherAccount);

            var afterCheckPrice = MainServices.CrossChainService.GetSideChainIndexingFeePrice(chainId);
            afterCheckPrice.ShouldBe(checkPrice + 10);
        }

        [TestMethod]
        public void ChangeLifeController()
        {
            var associationOrganization = CreateAssociationOrganization(MainServices);
            var input = new AuthorityStuff
            {
                ContractAddress = AddressHelper.Base58StringToAddress(MainServices.AssociationService.ContractAddress),
                OwnerAddress = associationOrganization
            };
            var controllerOrganization = MainServices.CrossChainService.GetSideChainLifetimeController().OwnerAddress;
            var createProposal = MainServices.ParliamentService.CreateProposal(
                MainServices.CrossChainService.ContractAddress,
                nameof(CrossChainContractMethod.ChangeSideChainLifetimeController), input, controllerOrganization,
                InitAccount);
            foreach (var miner in Miners)
            {
                MainServices.ParliamentService.ApproveProposal(createProposal, miner);
            }

            MainServices.ParliamentService.ReleaseProposal(createProposal, InitAccount);
        }
        
        [TestMethod]
        public void ChangeLifeControllerWithOtherController()
        {
            TransferToken(MainServices, InitAccount, OtherAccount, 10000_00000000, "ELF");
            var associationOrganization = CreateAssociationOrganization(MainServices);
            var input = new AuthorityStuff
            {
                ContractAddress = AddressHelper.Base58StringToAddress(MainServices.AssociationService.ContractAddress),
                OwnerAddress = associationOrganization
            };
            var controllerOrganization = MainServices.CrossChainService.GetSideChainLifetimeController().OwnerAddress;
            var createProposal = MainServices.AssociationService.CreateProposal(
                MainServices.CrossChainService.ContractAddress,
                nameof(CrossChainContractMethod.ChangeSideChainLifetimeController), input, controllerOrganization,
                OtherAccount);
            ApproveWithAssociation(MainServices,createProposal,associationOrganization);
            MainServices.AssociationService.ReleaseProposal(createProposal, OtherAccount);
        }

        [TestMethod]
        public void ChangeIndexingController()
        {
            var parliamentOrganization = CreateParliamentOrganization(SideBServices);
            var input = new AuthorityStuff
            {
                ContractAddress = AddressHelper.Base58StringToAddress(SideBServices.ParliamentService.ContractAddress),
                OwnerAddress = parliamentOrganization
            };
            var defaultOrganization = SideBServices.CrossChainService.GetCrossChainIndexingController().OwnerAddress;
            var createProposal = SideBServices.ParliamentService.CreateProposal(
                SideBServices.CrossChainService.ContractAddress,
                nameof(CrossChainContractMethod.ChangeCrossChainIndexingController), input, defaultOrganization,
                InitAccount);
            foreach (var miner in Miners)
            {
                SideBServices.TokenService.IssueBalance(InitAccount, miner,
                    100_00000000,SideBServices.TokenService.GetPrimaryTokenSymbol());
                SideBServices.ParliamentService.ApproveProposal(createProposal, miner);
            }

            SideBServices.ParliamentService.ReleaseProposal(createProposal, InitAccount);
        }

        [TestMethod]
        public void GetSideChainBalance()
        {
            var balance1 =
                MainServices.CrossChainService.GetSideChainBalance(ChainHelper.ConvertBase58ToChainId("tDVV"));
            var balance2 =
                MainServices.CrossChainService.GetSideChainBalance(ChainHelper.ConvertBase58ToChainId("tDVW"));
            _logger.Info($"chain tDVV {balance1}\n chain tDVW {balance2}");
        }
        
        [TestMethod]
        public void GetSideChainIndex()
        {
            var index1 =
                MainServices.CrossChainService.GetSideChainHeight(ChainHelper.ConvertBase58ToChainId("tDVV"));
            var index2 =
                MainServices.CrossChainService.GetSideChainHeight(ChainHelper.ConvertBase58ToChainId("tDVW"));
            _logger.Info($"chain tDVV {index1}\n chain tDVW {index2}");
            
            var index3 =
                SideAServices.CrossChainService.GetParentChainHeight();
            var index4 =
                SideBServices.CrossChainService.GetParentChainHeight();
            _logger.Info($"chain tDVV index main chain {index3}\n chain tDVW index main chain {index4}");
        }
    }
}