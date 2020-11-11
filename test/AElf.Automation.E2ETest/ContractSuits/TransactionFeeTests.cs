using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Standards.ACS3;
using AElf.Contracts.Association;
using AElf.Contracts.MultiToken;
using AElf.CSharp.Core.Extension;
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
using Volo.Abp.Threading;

namespace AElf.Automation.E2ETest.ContractSuits
{
    [TestClass]
    public class TransactionFeeTests
    {
        private static readonly ILog Logger = Log4NetHelper.GetLogger();

        public TransactionFeeTests()
        {
            Log4NetHelper.LogInit();
            NodeInfoHelper.SetConfig(ContractTestBase.MainConfig);
            NodeUsers = NodeInfoHelper.Config.Nodes.Select(o => o.Account).ToList();

            var firstNode = NodeInfoHelper.Config.Nodes.First();
            NodeManager = new NodeManager(firstNode.Endpoint);
            ContractManager = new ContractManager(NodeManager, firstNode.Account);
            AuthorityManager = new AuthorityManager(NodeManager, firstNode.Account);
            Miners = ContractManager.Authority.GetCurrentMiners();

            AsyncHelper.RunSync(InitializeAuthorizedOrganization);
            AsyncHelper.RunSync(InitializeReferendumAllowance);
        }

        public INodeManager NodeManager { get; set; }
        public ContractManager ContractManager { get; set; }
        public AuthorityManager AuthorityManager { get; set; }
        public DeveloperFeeController DeveloperFeeAddresses { get; set; }
        public UserFeeController UserFeeAddresses { get; set; }
        public List<string> NodeUsers { get; set; }
        public List<string> Miners { get; set; }

        [TestMethod]
        public async Task SetAvailableTokenInfos()
        {
            var availableTokenInfo = new SymbolListToPayTxSizeFee
            {
                SymbolsToPayTxSizeFee =
                {
                    new SymbolToPayTxSizeFee
                    {
                        TokenSymbol = "ELF",
                        AddedTokenWeight = 1,
                        BaseTokenWeight = 1
                    },
                    new SymbolToPayTxSizeFee
                    {
                        TokenSymbol = "CPU",
                        AddedTokenWeight = 50,
                        BaseTokenWeight = 1
                    },
                    new SymbolToPayTxSizeFee
                    {
                        TokenSymbol = "RAM",
                        AddedTokenWeight = 50,
                        BaseTokenWeight = 1
                    },
                    new SymbolToPayTxSizeFee
                    {
                        TokenSymbol = "NET",
                        AddedTokenWeight = 50,
                        BaseTokenWeight = 1
                    }
                }
            };

            var miners = AuthorityManager.GetCurrentMiners();
            var transactionResult = ContractManager.Authority.ExecuteTransactionWithAuthority(
                ContractManager.Token.ContractAddress, nameof(ContractManager.TokenStub.SetSymbolsToPayTxSizeFee),
                availableTokenInfo, miners.First());
            transactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var symbolListInfo = await QueryAvailableTokenInfos();
            symbolListInfo.ShouldBe(availableTokenInfo);
        }

        [TestMethod]
        public async Task Update_Storage_With_Developer()
        {
            const int feeType = (int) FeeTypeEnum.Storage;
            var piece1 = new CalculateFeePieceCoefficients
            {
                Value =
                {
                    500000,
                    1, 4, 2000
                }
            };
            var piece2 = new CalculateFeePieceCoefficients
            {
                Value =
                {
                    1000000,
                    1, 64, 2,
                    100, 250, 600
                }
            };
            var updateInput = new UpdateCoefficientsInput
            {
                PieceNumbers = {1, 2},
                Coefficients = new CalculateFeeCoefficients
                {
                    FeeTokenType = feeType,
                    PieceCoefficientsList =
                    {
                        piece1, piece2
                    }
                }
            };

            var proposalId = await CreateToRootForDeveloperFeeByTwoLayer(updateInput);
            await ApproveToRootForDeveloperFeeByTwoLayer(proposalId);

            var middleApproveProposalId = await ApproveToRootForDeveloperFeeByMiddleLayer(proposalId);
            await ApproveThenReleaseMiddleProposalForDeveloper(middleApproveProposalId);

            await ReleaseToRootForDeveloperFeeByTwoLayer(proposalId);

            var userCoefficient =
                await ContractManager.TokenStub.GetCalculateFeeCoefficientsForContract.CallAsync(new Int32Value
                {
                    Value = feeType
                });
            userCoefficient.FeeTokenType.ShouldBe(feeType);
            var pieceCoefficientsList = userCoefficient.PieceCoefficientsList;
            pieceCoefficientsList.First(o => o.Value[0] == 1000000).ShouldBe(piece2);
        }

        [TestMethod]
        public async Task Update_Write_With_Developer()
        {
            const int feeType = (int) FeeTypeEnum.Write;
            var piece2 = new CalculateFeePieceCoefficients
            {
                Value =
                {
                    100,
                    1, 1, 5
                }
            };
            var piece3 = new CalculateFeePieceCoefficients
            {
                Value =
                {
                    10000,
                    1, 1, 4,
                    2, 25, 16
                }
            };
            var updateInput = new UpdateCoefficientsInput
            {
                PieceNumbers = {2, 3},
                Coefficients = new CalculateFeeCoefficients
                {
                    FeeTokenType = feeType,
                    PieceCoefficientsList =
                    {
                        piece2, piece3
                    }
                }
            };

            var proposalId = await CreateToRootForDeveloperFeeByTwoLayer(updateInput);
            await ApproveToRootForDeveloperFeeByTwoLayer(proposalId);

            var middleApproveProposalId = await ApproveToRootForDeveloperFeeByMiddleLayer(proposalId);
            await ApproveThenReleaseMiddleProposalForDeveloper(middleApproveProposalId);

            await ReleaseToRootForDeveloperFeeByTwoLayer(proposalId);

            var userCoefficient =
                await ContractManager.TokenStub.GetCalculateFeeCoefficientsForContract.CallAsync(new Int32Value
                {
                    Value = feeType
                });
            userCoefficient.FeeTokenType.ShouldBe(feeType);
            var pieceCoefficientsList = userCoefficient.PieceCoefficientsList;
            pieceCoefficientsList.First(o => o.Value[0] == 100).ShouldBe(piece2);
            pieceCoefficientsList.First(o => o.Value[0] == 10000).ShouldBe(piece3);
        }

        [TestMethod]
        public async Task Update_Tx_With_User()
        {
            const int pieceUpperBound1 = 500000;
            const int pieceUpperBound2 = 800000;
            const int feeType = (int) FeeTypeEnum.Tx;
            var piece1 = new CalculateFeePieceCoefficients
            {
                Value =
                {
                    pieceUpperBound1,
                    1, 1, 900,
                    0, 1, 100000000
                }
            };
            var piece2 = new CalculateFeePieceCoefficients
            {
                Value =
                {
                    pieceUpperBound2,
                    1, 1, 1000,
                    2, 1, 50000
                }
            };
            var updateInput = new UpdateCoefficientsInput
            {
                PieceNumbers = {1, 2},
                Coefficients = new CalculateFeeCoefficients
                {
                    FeeTokenType = feeType,
                    PieceCoefficientsList =
                    {
                        piece1, piece2
                    }
                }
            };
            var proposalId = await CreateToRootForUserFeeByTwoLayer(updateInput);
            await ApproveToRootForUserFeeByTwoLayer(proposalId);
            await VoteToReferendum(proposalId);
            await ReleaseToRootForUserFeeByTwoLayer(proposalId);

            var userCoefficient =
                await ContractManager.TokenStub.GetCalculateFeeCoefficientsForSender.CallAsync(new Empty());
            userCoefficient.FeeTokenType.ShouldBe(feeType);
            var pieceCoefficientsList = userCoefficient.PieceCoefficientsList;
            pieceCoefficientsList.First(o => o.Value[0] == pieceUpperBound1).ShouldBe(piece1);
            pieceCoefficientsList.First(o => o.Value[0] == pieceUpperBound2).ShouldBe(piece2);
        }

        [TestMethod]
        public async Task GetCalculateFeeCoefficientsForSender()
        {
            const int feeType = (int) FeeTypeEnum.Tx;
            var userCoefficient =
                await ContractManager.TokenStub.GetCalculateFeeCoefficientsForSender.CallAsync(new Empty());
            userCoefficient.FeeTokenType.ShouldBe(feeType);
            var pieceCoefficientsList = userCoefficient.PieceCoefficientsList;
            Logger.Info(pieceCoefficientsList);
        }

        [TestMethod]
        public async Task Recover_Tx_With_User()
        {
            const int pieceUpperBound1 = 2000000;
            const int pieceUpperBound2 = 10000000;
            const int feeType = (int) FeeTypeEnum.Tx;
            var piece1 = new CalculateFeePieceCoefficients
            {
                Value =
                {
                    pieceUpperBound1,
                    1, 1, 800,
                    0, 1, 10000
                }
            };
            var piece2 = new CalculateFeePieceCoefficients
            {
                Value =
                {
                    pieceUpperBound2,
                    1, 1, 800,
                    2, 1, 10000
                }
            };
            var updateInput = new UpdateCoefficientsInput
            {
                PieceNumbers = {1, 2},
                Coefficients = new CalculateFeeCoefficients
                {
                    FeeTokenType = feeType,
                    PieceCoefficientsList =
                    {
                        piece1, piece2
                    }
                }
            };
            var proposalId = await CreateToRootForUserFeeByTwoLayer(updateInput);
            await ApproveToRootForUserFeeByTwoLayer(proposalId);
            await VoteToReferendum(proposalId);
            await ReleaseToRootForUserFeeByTwoLayer(proposalId);

            var userCoefficient =
                await ContractManager.TokenStub.GetCalculateFeeCoefficientsForSender.CallAsync(new Empty());
            userCoefficient.FeeTokenType.ShouldBe(feeType);
            var pieceCoefficientsList = userCoefficient.PieceCoefficientsList;
            pieceCoefficientsList.First(o => o.Value[0] == pieceUpperBound1).ShouldBe(piece1);
            pieceCoefficientsList.First(o => o.Value[0] == pieceUpperBound2).ShouldBe(piece2);
        }

        [TestMethod]
        [Ignore]
        public async Task ChangeSymbolsToPayTxSizeFeeController()
        {
            var defaultController =
                await ContractManager.TokenImplStub.GetSymbolsToPayTXSizeFeeController.CallAsync(new Empty());
            defaultController.ContractAddress.ShouldBe(ContractManager.Parliament.Contract);

            var newOrganization = CreateAssociationOrganization();
            var input = new AuthorityInfo
            {
                ContractAddress = ContractManager.Association.Contract,
                OwnerAddress = newOrganization
            };
            var result = AuthorityManager.ExecuteTransactionWithAuthority(ContractManager.Token.ContractAddress,
                nameof(TokenContractImplContainer.TokenContractImplStub.ChangeSymbolsToPayTXSizeFeeController), input,
                Miners.First(), defaultController.OwnerAddress);
            result.Status.ShouldBe(TransactionResultStatus.Mined);

            var updateController =
                await ContractManager.TokenImplStub.GetSymbolsToPayTXSizeFeeController.CallAsync(new Empty());
            updateController.ContractAddress.ShouldBe(ContractManager.Association.Contract);

            var availableTokenInfo = new SymbolListToPayTxSizeFee
            {
                SymbolsToPayTxSizeFee =
                {
                    new SymbolToPayTxSizeFee
                    {
                        TokenSymbol = "ELF",
                        AddedTokenWeight = 1,
                        BaseTokenWeight = 1
                    },
                    new SymbolToPayTxSizeFee
                    {
                        TokenSymbol = "CPU",
                        AddedTokenWeight = 20,
                        BaseTokenWeight = 1
                    },
                    new SymbolToPayTxSizeFee
                    {
                        TokenSymbol = "RAM",
                        AddedTokenWeight = 20,
                        BaseTokenWeight = 1
                    },
                    new SymbolToPayTxSizeFee
                    {
                        TokenSymbol = "NET",
                        AddedTokenWeight = 20,
                        BaseTokenWeight = 1
                    }
                }
            };

            var proposalId = ContractManager.Association.CreateProposal(ContractManager.Token.ContractAddress,
                nameof(ContractManager.TokenStub.SetSymbolsToPayTxSizeFee), availableTokenInfo, newOrganization,
                Miners.First());
            ContractManager.Association.ApproveWithAssociation(proposalId, newOrganization);
            var transactionResult = ContractManager.Association.ReleaseProposal(proposalId, Miners.First());
            transactionResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            var recoverInput = new AuthorityInfo
            {
                ContractAddress = defaultController.ContractAddress,
                OwnerAddress = defaultController.OwnerAddress
            };
            var recoverProposalId = ContractManager.Association.CreateProposal(ContractManager.Token.ContractAddress,
                nameof(ContractManager.TokenImplStub.ChangeSymbolsToPayTXSizeFeeController), recoverInput,
                newOrganization, Miners.First());
            ContractManager.Association.ApproveWithAssociation(recoverProposalId, newOrganization);
            var recoverTransactionResult =
                ContractManager.Association.ReleaseProposal(recoverProposalId, Miners.First());
            recoverTransactionResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            var recoverController =
                await ContractManager.TokenImplStub.GetSymbolsToPayTXSizeFeeController.CallAsync(new Empty());
            recoverController.ContractAddress.ShouldBe(defaultController.ContractAddress);
        }

        [TestMethod]
        [Ignore]
        public async Task ChangeUserFeeController()
        {
            var defaultController =
                await ContractManager.TokenImplStub.GetUserFeeController.CallAsync(new Empty());
            defaultController.RootController.ContractAddress.ShouldBe(ContractManager.Association.Contract);
            defaultController.ParliamentController.ContractAddress.ShouldBe(ContractManager.Parliament.Contract);

            var newOrganization = CreateAssociationOrganization();
            var proposer = ContractManager.Association.GetOrganization(newOrganization).ProposerWhiteList.Proposers
                .First();
            var input = new AuthorityInfo
            {
                ContractAddress = ContractManager.Association.Contract,
                OwnerAddress = newOrganization
            };
            var createNestProposalInput = new CreateProposalInput
            {
                ToAddress = ContractManager.Token.Contract,
                Params = input.ToByteString(),
                OrganizationAddress = UserFeeAddresses.RootController.OwnerAddress,
                ContractMethodName = nameof(TokenContractImplContainer.TokenContractImplStub.ChangeUserFeeController),
                ExpiredTime = KernelHelper.GetUtcNow().AddHours(1)
            };

            var createProposalInput = new CreateProposalInput
            {
                ToAddress = ContractManager.Association.Contract,
                Params = createNestProposalInput.ToByteString(),
                OrganizationAddress = UserFeeAddresses.ParliamentController.OwnerAddress,
                ContractMethodName = nameof(AssociationContractContainer.AssociationContractStub.CreateProposal),
                ExpiredTime = KernelHelper.GetUtcNow().AddHours(1)
            };
            var parliamentCreateProposal =
                await ContractManager.ParliamentAuthStub.CreateProposal.SendAsync(createProposalInput);
            parliamentCreateProposal.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var parliamentProposalId = parliamentCreateProposal.Output;
            ContractManager.Parliament.MinersApproveProposal(parliamentProposalId, Miners);
            var releaseRet =
                ContractManager.Parliament.ReleaseProposal(parliamentProposalId, ContractManager.CallAddress);
            var id = ProposalCreated.Parser
                .ParseFrom(releaseRet.Logs.First(l => l.Name.Contains(nameof(ProposalCreated)))
                    .NonIndexed).ProposalId;
            await ApproveToRootForUserFeeByTwoLayer(id);
            await VoteToReferendum(id);
            await ReleaseToRootForUserFeeByTwoLayer(id);

            var updateController =
                await ContractManager.TokenImplStub.GetUserFeeController.CallAsync(new Empty());
            updateController.RootController.ContractAddress.ShouldBe(ContractManager.Association.Contract);
            updateController.RootController.OwnerAddress.ShouldBe(newOrganization);

            const int pieceUpperBound1 = 500000;
            const int pieceUpperBound2 = 800000;
            const int feeType = (int) FeeTypeEnum.Tx;
            var piece1 = new CalculateFeePieceCoefficients
            {
                Value =
                {
                    pieceUpperBound1,
                    1, 1, 900,
                    0, 1, 1000
                }
            };
            var piece2 = new CalculateFeePieceCoefficients
            {
                Value =
                {
                    pieceUpperBound2,
                    1, 1, 100,
                    2, 1, 5000
                }
            };
            var updateInput = new UpdateCoefficientsInput
            {
                PieceNumbers = {1, 2},
                Coefficients = new CalculateFeeCoefficients
                {
                    FeeTokenType = feeType,
                    PieceCoefficientsList =
                    {
                        piece1, piece2
                    }
                }
            };

            var proposalId = ContractManager.Association.CreateProposal(ContractManager.Token.ContractAddress,
                nameof(TokenContractImplContainer.TokenContractImplStub.UpdateCoefficientsForSender), updateInput,
                newOrganization, proposer.ToBase58());
            ContractManager.Association.ApproveWithAssociation(proposalId, newOrganization);
            var release = ContractManager.Association.ReleaseProposal(proposalId, proposer.ToBase58());
            release.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            var userCoefficient =
                await ContractManager.TokenStub.GetCalculateFeeCoefficientsForSender.CallAsync(new Empty());
            userCoefficient.FeeTokenType.ShouldBe(feeType);
            var pieceCoefficientsList = userCoefficient.PieceCoefficientsList;
            pieceCoefficientsList.First(o => o.Value[0] == pieceUpperBound1).ShouldBe(piece1);
            pieceCoefficientsList.First(o => o.Value[0] == pieceUpperBound2).ShouldBe(piece2);

            //recover
            var recoverInput = new AuthorityInfo
            {
                OwnerAddress = defaultController.RootController.OwnerAddress,
                ContractAddress = defaultController.RootController.ContractAddress
            };
            var recoverProposalId = ContractManager.Association.CreateProposal(ContractManager.Token.ContractAddress,
                nameof(TokenContractImplContainer.TokenContractImplStub.ChangeUserFeeController), recoverInput,
                newOrganization, proposer.ToBase58());
            ContractManager.Association.ApproveWithAssociation(recoverProposalId, newOrganization);
            var recoverRelease = ContractManager.Association.ReleaseProposal(recoverProposalId, proposer.ToBase58());
            recoverRelease.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var recoverController =
                await ContractManager.TokenImplStub.GetUserFeeController.CallAsync(new Empty());
            recoverController.RootController.OwnerAddress.ShouldBe(defaultController.RootController.OwnerAddress);
        }

        private async Task InitializeAuthorizedOrganization()
        {
//            await ContractManager.TokenImplStub.InitializeAuthorizedController.SendAsync(new Empty());
//            initializeResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            DeveloperFeeAddresses =
                await ContractManager.TokenImplStub.GetDeveloperFeeController.CallAsync(new Empty());
            Logger.Info($"Developer RootController: {DeveloperFeeAddresses.RootController}");
            Logger.Info($"Developer ParliamentController: {DeveloperFeeAddresses.ParliamentController}");
            Logger.Info($"Developer DeveloperController: {DeveloperFeeAddresses.DeveloperController}");

            UserFeeAddresses = await ContractManager.TokenImplStub.GetUserFeeController.CallAsync(new Empty());
            Logger.Info($"User RootController: {UserFeeAddresses.RootController}");
            Logger.Info($"User ParliamentController: {UserFeeAddresses.ParliamentController}");
            Logger.Info($"User ReferendumController: {UserFeeAddresses.ReferendumController}");
        }

        private async Task<SymbolListToPayTxSizeFee> QueryAvailableTokenInfos()
        {
            var tokenInfos = await ContractManager.TokenStub.GetSymbolsToPayTxSizeFee.CallAsync(new Empty());
            if (tokenInfos.Equals(new SymbolListToPayTxSizeFee()))
            {
                Logger.Info("GetAvailableTokenInfos: Null");
                return null;
            }

            foreach (var info in tokenInfos.SymbolsToPayTxSizeFee)
                Logger.Info(
                    $"Symbol: {info.TokenSymbol}, TokenWeight: {info.AddedTokenWeight}, BaseWeight: {info.BaseTokenWeight}");

            return tokenInfos;
        }

        #region Developer actions

        private async Task<Hash> CreateToRootForDeveloperFeeByTwoLayer(UpdateCoefficientsInput input)
        {
            var createNestProposalInput = new CreateProposalInput
            {
                ToAddress = ContractManager.Token.Contract,
                Params = input.ToByteString(),
                OrganizationAddress = DeveloperFeeAddresses.RootController.OwnerAddress,
                ContractMethodName = nameof(TokenContractContainer.TokenContractStub.UpdateCoefficientsForContract),
                ExpiredTime = KernelHelper.GetUtcNow().AddHours(1)
            };
            var createProposalInput = new CreateProposalInput
            {
                ToAddress = ContractManager.Association.Contract,
                Params = createNestProposalInput.ToByteString(),
                OrganizationAddress = DeveloperFeeAddresses.ParliamentController.OwnerAddress,
                ContractMethodName = nameof(AssociationContractContainer.AssociationContractStub.CreateProposal),
                ExpiredTime = KernelHelper.GetUtcNow().AddHours(1)
            };
            var parliamentProposal =
                await ContractManager.ParliamentAuthStub.CreateProposal.SendAsync(createProposalInput);
            parliamentProposal.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var parliamentProposalId = parliamentProposal.Output;
            ContractManager.Parliament.MinersApproveProposal(parliamentProposalId, Miners);

            var releaseResult = await ContractManager.ParliamentAuthStub.Release.SendAsync(parliamentProposalId);
            var id = ProposalCreated.Parser
                .ParseFrom(releaseResult.TransactionResult.Logs.First(l => l.Name.Contains(nameof(ProposalCreated)))
                    .NonIndexed).ProposalId;

            return id;
        }

        private async Task ApproveToRootForDeveloperFeeByTwoLayer(Hash input)
        {
            var approveProposalInput = new CreateProposalInput
            {
                ToAddress = ContractManager.Association.Contract,
                Params = input.ToByteString(),
                OrganizationAddress = DeveloperFeeAddresses.ParliamentController.OwnerAddress,
                ContractMethodName = nameof(AssociationContractContainer.AssociationContractStub.Approve),
                ExpiredTime = KernelHelper.GetUtcNow().AddHours(1)
            };
            var parliamentCreateProposal =
                await ContractManager.ParliamentAuthStub.CreateProposal.SendAsync(approveProposalInput);
            parliamentCreateProposal.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var parliamentProposalId = parliamentCreateProposal.Output;
            var miners = ContractManager.Authority.GetCurrentMiners();
            ContractManager.Parliament.MinersApproveProposal(parliamentProposalId, miners);
            ContractManager.Parliament.ReleaseProposal(parliamentProposalId, ContractManager.CallAddress);
        }

        private async Task<Hash> ApproveToRootForDeveloperFeeByMiddleLayer(Hash input)
        {
            var approveMidProposalInput = new CreateProposalInput
            {
                ToAddress = ContractManager.Association.Contract,
                Params = input.ToByteString(),
                OrganizationAddress = DeveloperFeeAddresses.DeveloperController.OwnerAddress,
                ContractMethodName = nameof(AssociationContractContainer.AssociationContractStub.Approve),
                ExpiredTime = KernelHelper.GetUtcNow().AddHours(1)
            };
            var approveLeafProposalInput = new CreateProposalInput
            {
                ToAddress = ContractManager.Association.Contract,
                Params = approveMidProposalInput.ToByteString(),
                OrganizationAddress = DeveloperFeeAddresses.ParliamentController.OwnerAddress,
                ContractMethodName = nameof(AssociationContractContainer.AssociationContractStub.CreateProposal),
                ExpiredTime = KernelHelper.GetUtcNow().AddHours(1)
            };

            var parliamentCreateProposal =
                await ContractManager.ParliamentAuthStub.CreateProposal.SendAsync(approveLeafProposalInput);
            parliamentCreateProposal.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var parliamentProposalId = parliamentCreateProposal.Output;

            ContractManager.Parliament.MinersApproveProposal(parliamentProposalId, Miners);
            var newCreateProposalRet = await ContractManager.ParliamentAuthStub.Release.SendAsync(parliamentProposalId);
            var middleProposalId = ProposalCreated.Parser
                .ParseFrom(newCreateProposalRet.TransactionResult.Logs
                    .First(l => l.Name.Contains(nameof(ProposalCreated)))
                    .NonIndexed).ProposalId;
            return middleProposalId;
        }

        private async Task ApproveThenReleaseMiddleProposalForDeveloper(Hash input)
        {
            var approveLeafProposalInput = new CreateProposalInput
            {
                ToAddress = ContractManager.Association.Contract,
                Params = input.ToByteString(),
                OrganizationAddress = DeveloperFeeAddresses.ParliamentController.OwnerAddress,
                ContractMethodName = nameof(AssociationContractContainer.AssociationContractStub.Approve),
                ExpiredTime = KernelHelper.GetUtcNow().AddHours(1)
            };
            var parliamentCreateProposal =
                await ContractManager.ParliamentAuthStub.CreateProposal.SendAsync(approveLeafProposalInput);
            parliamentCreateProposal.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var parliamentProposalId = parliamentCreateProposal.Output;

            ContractManager.Parliament.MinersApproveProposal(parliamentProposalId, Miners);
            ContractManager.Parliament.ReleaseProposal(parliamentProposalId, ContractManager.CallAddress);

            approveLeafProposalInput = new CreateProposalInput
            {
                ToAddress = ContractManager.Association.Contract,
                Params = input.ToByteString(),
                OrganizationAddress = DeveloperFeeAddresses.ParliamentController.OwnerAddress,
                ContractMethodName = nameof(AssociationContractContainer.AssociationContractStub.Release),
                ExpiredTime = KernelHelper.GetUtcNow().AddHours(1)
            };
            parliamentCreateProposal =
                await ContractManager.ParliamentAuthStub.CreateProposal.SendAsync(approveLeafProposalInput);
            parliamentCreateProposal.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            parliamentProposalId = parliamentCreateProposal.Output;

            ContractManager.Parliament.MinersApproveProposal(parliamentProposalId, Miners);
            ContractManager.Parliament.ReleaseProposal(parliamentProposalId, ContractManager.CallAddress);
        }

        private async Task ReleaseToRootForDeveloperFeeByTwoLayer(Hash input)
        {
            var releaseProposalInput = new CreateProposalInput
            {
                ToAddress = ContractManager.Association.Contract,
                Params = input.ToByteString(),
                OrganizationAddress = DeveloperFeeAddresses.ParliamentController.OwnerAddress,
                ContractMethodName = nameof(AssociationContractContainer.AssociationContractStub.Release),
                ExpiredTime = KernelHelper.GetUtcNow().AddHours(1)
            };
            var parliamentCreateProposal =
                await ContractManager.ParliamentAuthStub.CreateProposal.SendAsync(releaseProposalInput);
            parliamentCreateProposal.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var parliamentProposalId = parliamentCreateProposal.Output;

            ContractManager.Parliament.MinersApproveProposal(parliamentProposalId, Miners);
            ContractManager.Parliament.ReleaseProposal(parliamentProposalId, ContractManager.CallAddress);
        }

        #endregion

        #region User action

        private async Task<Hash> CreateToRootForUserFeeByTwoLayer(UpdateCoefficientsInput input)
        {
            var createNestProposalInput = new CreateProposalInput
            {
                ToAddress = ContractManager.Token.Contract,
                Params = input.ToByteString(),
                OrganizationAddress = UserFeeAddresses.RootController.OwnerAddress,
                ContractMethodName = nameof(TokenContractContainer.TokenContractStub.UpdateCoefficientsForSender),
                ExpiredTime = KernelHelper.GetUtcNow().AddHours(1)
            };

            var createProposalInput = new CreateProposalInput
            {
                ToAddress = ContractManager.Association.Contract,
                Params = createNestProposalInput.ToByteString(),
                OrganizationAddress = UserFeeAddresses.ParliamentController.OwnerAddress,
                ContractMethodName = nameof(AssociationContractContainer.AssociationContractStub.CreateProposal),
                ExpiredTime = KernelHelper.GetUtcNow().AddHours(1)
            };
            var parliamentCreateProposal =
                await ContractManager.ParliamentAuthStub.CreateProposal.SendAsync(createProposalInput);
            parliamentCreateProposal.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var parliamentProposalId = parliamentCreateProposal.Output;
            ContractManager.Parliament.MinersApproveProposal(parliamentProposalId, Miners);
            var releaseRet =
                ContractManager.Parliament.ReleaseProposal(parliamentProposalId, ContractManager.CallAddress);
            var id = ProposalCreated.Parser
                .ParseFrom(releaseRet.Logs.First(l => l.Name.Contains(nameof(ProposalCreated)))
                    .NonIndexed).ProposalId;

            return id;
        }

        private async Task ApproveToRootForUserFeeByTwoLayer(Hash input)
        {
            var approveProposalInput = new CreateProposalInput
            {
                ToAddress = ContractManager.Association.Contract,
                Params = input.ToByteString(),
                OrganizationAddress = UserFeeAddresses.ParliamentController.OwnerAddress,
                ContractMethodName = nameof(AssociationContractContainer.AssociationContractStub.Approve),
                ExpiredTime = KernelHelper.GetUtcNow().AddHours(1)
            };
            var parliamentCreateProposal =
                await ContractManager.ParliamentAuthStub.CreateProposal.SendAsync(approveProposalInput);
            parliamentCreateProposal.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var parliamentProposalId = parliamentCreateProposal.Output;

            ContractManager.Parliament.MinersApproveProposal(parliamentProposalId, Miners);
            ContractManager.Parliament.ReleaseProposal(parliamentProposalId, ContractManager.CallAddress);
        }

        private async Task VoteToReferendum(Hash input)
        {
            var referendumProposal = new CreateProposalInput
            {
                ToAddress = ContractManager.Association.Contract,
                Params = input.ToByteString(),
                OrganizationAddress = UserFeeAddresses.ReferendumController.OwnerAddress,
                ContractMethodName = nameof(AssociationContractContainer.AssociationContractStub.Approve),
                ExpiredTime = KernelHelper.GetUtcNow().AddHours(1)
            };
            var parliamentProposal = new CreateProposalInput
            {
                ToAddress = ContractManager.Referendum.Contract,
                Params = referendumProposal.ToByteString(),
                OrganizationAddress = UserFeeAddresses.ParliamentController.OwnerAddress,
                ContractMethodName = nameof(ContractManager.ReferendumStub.CreateProposal),
                ExpiredTime = KernelHelper.GetUtcNow().AddHours(1)
            };
            var parliamentCreateProposal =
                await ContractManager.ParliamentAuthStub.CreateProposal.SendAsync(parliamentProposal);
            parliamentCreateProposal.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var parliamentProposalId = parliamentCreateProposal.Output;

            ContractManager.Parliament.MinersApproveProposal(parliamentProposalId, Miners);
            var ret = ContractManager.Parliament.ReleaseProposal(parliamentProposalId, ContractManager.CallAddress);
            var id = ProposalCreated.Parser
                .ParseFrom(ret.Logs.First(l => l.Name.Contains(nameof(ProposalCreated)))
                    .NonIndexed).ProposalId;
            var virtualAddress = ContractManager.Referendum.GetProposalVirtualAddress(id);
            var approveTokenAmount =
                ContractManager.Referendum.GetOrganization(UserFeeAddresses.ReferendumController.OwnerAddress)
                    .ProposalReleaseThreshold.MinimalApprovalThreshold;
            var tokenApprove = ContractManager.Token.ApproveToken(ContractManager.Referendum.CallAddress,
                virtualAddress.ToBase58(), approveTokenAmount);
            tokenApprove.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var approveResult = await ContractManager.ReferendumStub.Approve.SendAsync(id);
            approveResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            parliamentProposal = new CreateProposalInput
            {
                ToAddress = ContractManager.Referendum.Contract,
                Params = id.ToByteString(),
                OrganizationAddress = UserFeeAddresses.ParliamentController.OwnerAddress,
                ContractMethodName = nameof(ContractManager.ReferendumStub.Release),
                ExpiredTime = KernelHelper.GetUtcNow().AddHours(1)
            };
            parliamentCreateProposal =
                await ContractManager.ParliamentAuthStub.CreateProposal.SendAsync(parliamentProposal);
            parliamentCreateProposal.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            parliamentProposalId = parliamentCreateProposal.Output;

            ContractManager.Parliament.MinersApproveProposal(parliamentProposalId, Miners);
            ContractManager.Parliament.ReleaseProposal(parliamentProposalId, ContractManager.CallAddress);
        }

        private async Task ReleaseToRootForUserFeeByTwoLayer(Hash input)
        {
            var parliamentProposal = new CreateProposalInput
            {
                ToAddress = ContractManager.Association.Contract,
                Params = input.ToByteString(),
                OrganizationAddress = UserFeeAddresses.ParliamentController.OwnerAddress,
                ContractMethodName = nameof(AssociationContractContainer.AssociationContractStub.Release),
                ExpiredTime = KernelHelper.GetUtcNow().AddHours(1)
            };
            var parliamentCreateProposal =
                await ContractManager.ParliamentAuthStub.CreateProposal.SendAsync(parliamentProposal);
            parliamentCreateProposal.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var parliamentProposalId = parliamentCreateProposal.Output;

            ContractManager.Parliament.MinersApproveProposal(parliamentProposalId, Miners);
            ContractManager.Parliament.ReleaseProposal(parliamentProposalId, ContractManager.CallAddress);
        }

        private async Task InitializeReferendumAllowance()
        {
            var primaryToken = NodeManager.GetPrimaryTokenSymbol();
            var approveResult = await ContractManager.TokenStub.Approve.SendAsync(new ApproveInput
            {
                Spender = ContractManager.Referendum.Contract,
                Symbol = primaryToken,
                Amount = 1000_00000000
            });
            approveResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        }

        private Address CreateAssociationOrganization()
        {
            var miners = ContractManager.Authority.GetCurrentMiners();
            var association = ContractManager.Association;
            var members = NodeInfoHelper.Config.Nodes.Select(l => l.Account).ToList()
                .Select(member => member.ConvertAddress()).Take(3);
            //create association organization
            var enumerable = members as Address[] ?? members.ToArray();
            var createInput = new CreateOrganizationInput
            {
                ProposalReleaseThreshold = new ProposalReleaseThreshold
                {
                    MaximalAbstentionThreshold = 1,
                    MaximalRejectionThreshold = 1,
                    MinimalApprovalThreshold = 2,
                    MinimalVoteThreshold = 2
                },
                ProposerWhiteList = new ProposerWhiteList {Proposers = {miners.First().ConvertAddress()}},
                OrganizationMemberList = new OrganizationMemberList {OrganizationMembers = {enumerable}}
            };
            association.SetAccount(miners.First());
            var result = association.ExecuteMethodWithResult(AssociationMethod.CreateOrganization,
                createInput);
            var organizationAddress =
                Address.Parser.ParseFrom(ByteArrayHelper.HexStringToByteArray(result.ReturnValue));
            return organizationAddress;
        }

        #endregion
    }
}