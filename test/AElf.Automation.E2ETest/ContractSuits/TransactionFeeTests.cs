using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Acs3;
using AElf.Contracts.Association;
using AElf.Contracts.MultiToken;
using AElf.Kernel;
using AElf.Sdk.CSharp;
using AElf.Types;
using AElfChain.Common;
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
        public INodeManager NodeManager { get; set; }
        public ContractManager ContractManager { get; set; }
        public DeveloperFeeController DeveloperFeeAddresses { get; set; }
        public UserFeeController UserFeeAddresses { get; set; }
        public List<string> NodeUsers { get; set; }
        public List<string> Miners { get; set; }

        public TransactionFeeTests()
        {
            Log4NetHelper.LogInit();
            NodeInfoHelper.SetConfig(ContractTestBase.MainConfig);
            NodeUsers = NodeInfoHelper.Config.Nodes.Select(o => o.Account).ToList();

            var firstNode = NodeInfoHelper.Config.Nodes.First();
            NodeManager = new NodeManager(firstNode.Endpoint);
            ContractManager = new ContractManager(NodeManager, firstNode.Account);
            Miners = ContractManager.Authority.GetCurrentMiners();

            AsyncHelper.RunSync(InitializeAuthorizedOrganization);
            AsyncHelper.RunSync(InitializeReferendumAllowance);
        }

        [TestMethod]
        public async Task Update_Storage_With_Developer()
        {
            const int feeType = (int) FeeTypeEnum.Storage;
            var piece1 = new CalculateFeePieceCoefficients
            {
                Value = {0, 500000, 1, 4, 2000}
            };
            var piece2 = new CalculateFeePieceCoefficients
            {
                Value = {1, 1000000, 1, 64, 2, 100, 250, 600}
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
                await ContractManager.TokenStub.GetCalculateFeeCoefficientsForContract.CallAsync(new SInt32Value
                {
                    Value = feeType
                });
            userCoefficient.FeeTokenType.ShouldBe(feeType);
            var pieceCoefficientsList = userCoefficient.PieceCoefficientsList;
            pieceCoefficientsList.First(o => o.Value[0] == 1).ShouldBe(piece2);
        }

        [TestMethod]
        public async Task Update_Write_With_Developer()
        {
            const int feeType = (int) FeeTypeEnum.Write;
            var piece2 = new CalculateFeePieceCoefficients
            {
                Value = {0, 1000, 1, 4, 30000}
            };
            var piece3 = new CalculateFeePieceCoefficients
            {
                Value = {1, 1000000, 1, 4, 2, 2, 250, 60}
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
                await ContractManager.TokenStub.GetCalculateFeeCoefficientsForContract.CallAsync(new SInt32Value
                {
                    Value = feeType
                });
            userCoefficient.FeeTokenType.ShouldBe(feeType);
            var pieceCoefficientsList = userCoefficient.PieceCoefficientsList;
            pieceCoefficientsList.First(o => o.Value[0] == 1).ShouldBe(piece3);
        }
        
        [TestMethod]
        public async Task Update_Tx_With_User()
        {
            const int pieceUpperBound1 = 500000;
            const int pieceUpperBound2 = 800000;
            const int feeType = (int) FeeTypeEnum.Tx;
            var piece1 = new CalculateFeePieceCoefficients
            {
                Value = {0, pieceUpperBound1, 1, 800, 50000000}
            };
            var piece2 = new CalculateFeePieceCoefficients
            {
                Value = {1, pieceUpperBound2, 1, 800, 2, 100, 1, 3}
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
            pieceCoefficientsList.First(o => o.Value[0] == 0).ShouldBe(piece1);
            pieceCoefficientsList.First(o => o.Value[0] == 1).ShouldBe(piece2);
        }

        private async Task InitializeAuthorizedOrganization()
        {
            await ContractManager.TokenImplStub.InitializeAuthorizedController.SendAsync(new Empty());
            //initializeResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

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

        #region Developer actions

        private async Task<Hash> CreateToRootForDeveloperFeeByTwoLayer(UpdateCoefficientsInput input)
        {
            var createNestProposalInput = new CreateProposalInput
            {
                ToAddress = ContractManager.Token.Contract,
                Params = input.ToByteString(),
                OrganizationAddress = DeveloperFeeAddresses.RootController.OwnerAddress,
                ContractMethodName = nameof(TokenContractContainer.TokenContractStub.UpdateCoefficientsForContract),
                ExpiredTime = TimestampHelper.GetUtcNow().AddHours(1)
            };
            var createProposalInput = new CreateProposalInput
            {
                ToAddress = ContractManager.Association.Contract,
                Params = createNestProposalInput.ToByteString(),
                OrganizationAddress = DeveloperFeeAddresses.ParliamentController.OwnerAddress,
                ContractMethodName = nameof(AssociationContractContainer.AssociationContractStub.CreateProposal),
                ExpiredTime = TimestampHelper.GetUtcNow().AddHours(1)
            };
            var parliamentProposal =
                await ContractManager.ParliamentAuthStub.CreateProposal.SendAsync(createProposalInput);
            parliamentProposal.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var returnValue = parliamentProposal.TransactionResult.ReadableReturnValue.Replace("\"", "");
            var parliamentProposalId = HashHelper.HexStringToHash(returnValue);
            ContractManager.ParliamentAuth.MinersApproveProposal(parliamentProposalId, Miners);

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
                ExpiredTime = TimestampHelper.GetUtcNow().AddHours(1)
            };
            var parliamentCreateProposal =
                await ContractManager.ParliamentAuthStub.CreateProposal.SendAsync(approveProposalInput);
            parliamentCreateProposal.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var returnValue = parliamentCreateProposal.TransactionResult.ReadableReturnValue.Replace("\"", "");
            var parliamentProposalId = HashHelper.HexStringToHash(returnValue);

            var miners = ContractManager.Authority.GetCurrentMiners();
            ContractManager.ParliamentAuth.MinersApproveProposal(parliamentProposalId, miners);
            ContractManager.ParliamentAuth.ReleaseProposal(parliamentProposalId, ContractManager.CallAddress);
        }

        private async Task<Hash> ApproveToRootForDeveloperFeeByMiddleLayer(Hash input)
        {
            var approveMidProposalInput = new CreateProposalInput
            {
                ToAddress = ContractManager.Association.Contract,
                Params = input.ToByteString(),
                OrganizationAddress = DeveloperFeeAddresses.DeveloperController.OwnerAddress,
                ContractMethodName = nameof(AssociationContractContainer.AssociationContractStub.Approve),
                ExpiredTime = TimestampHelper.GetUtcNow().AddHours(1)
            };
            var approveLeafProposalInput = new CreateProposalInput
            {
                ToAddress = ContractManager.Association.Contract,
                Params = approveMidProposalInput.ToByteString(),
                OrganizationAddress = DeveloperFeeAddresses.ParliamentController.OwnerAddress,
                ContractMethodName = nameof(AssociationContractContainer.AssociationContractStub.CreateProposal),
                ExpiredTime = TimestampHelper.GetUtcNow().AddHours(1)
            };

            var parliamentCreateProposal =
                await ContractManager.ParliamentAuthStub.CreateProposal.SendAsync(approveLeafProposalInput);
            parliamentCreateProposal.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var returnValue = parliamentCreateProposal.TransactionResult.ReadableReturnValue.Replace("\"", "");
            var parliamentProposalId = HashHelper.HexStringToHash(returnValue);

            ContractManager.ParliamentAuth.MinersApproveProposal(parliamentProposalId, Miners);
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
                ExpiredTime = TimestampHelper.GetUtcNow().AddHours(1)
            };
            var parliamentCreateProposal =
                await ContractManager.ParliamentAuthStub.CreateProposal.SendAsync(approveLeafProposalInput);
            parliamentCreateProposal.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var returnValue = parliamentCreateProposal.TransactionResult.ReadableReturnValue.Replace("\"", "");
            var parliamentProposalId = HashHelper.HexStringToHash(returnValue);

            ContractManager.ParliamentAuth.MinersApproveProposal(parliamentProposalId, Miners);
            ContractManager.ParliamentAuth.ReleaseProposal(parliamentProposalId, ContractManager.CallAddress);

            approveLeafProposalInput = new CreateProposalInput
            {
                ToAddress = ContractManager.Association.Contract,
                Params = input.ToByteString(),
                OrganizationAddress = DeveloperFeeAddresses.ParliamentController.OwnerAddress,
                ContractMethodName = nameof(AssociationContractContainer.AssociationContractStub.Release),
                ExpiredTime = TimestampHelper.GetUtcNow().AddHours(1)
            };
            parliamentCreateProposal =
                await ContractManager.ParliamentAuthStub.CreateProposal.SendAsync(approveLeafProposalInput);
            parliamentCreateProposal.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            returnValue = parliamentCreateProposal.TransactionResult.ReadableReturnValue.Replace("\"", "");
            parliamentProposalId = HashHelper.HexStringToHash(returnValue);

            ContractManager.ParliamentAuth.MinersApproveProposal(parliamentProposalId, Miners);
            ContractManager.ParliamentAuth.ReleaseProposal(parliamentProposalId, ContractManager.CallAddress);
        }

        private async Task ReleaseToRootForDeveloperFeeByTwoLayer(Hash input)
        {
            var releaseProposalInput = new CreateProposalInput
            {
                ToAddress = ContractManager.Association.Contract,
                Params = input.ToByteString(),
                OrganizationAddress = DeveloperFeeAddresses.ParliamentController.OwnerAddress,
                ContractMethodName = nameof(AssociationContractContainer.AssociationContractStub.Release),
                ExpiredTime = TimestampHelper.GetUtcNow().AddHours(1)
            };
            var parliamentCreateProposal =
                await ContractManager.ParliamentAuthStub.CreateProposal.SendAsync(releaseProposalInput);
            parliamentCreateProposal.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var returnValue = parliamentCreateProposal.TransactionResult.ReadableReturnValue.Replace("\"", "");
            var parliamentProposalId = HashHelper.HexStringToHash(returnValue);

            ContractManager.ParliamentAuth.MinersApproveProposal(parliamentProposalId, Miners);
            ContractManager.ParliamentAuth.ReleaseProposal(parliamentProposalId, ContractManager.CallAddress);
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
                ExpiredTime = TimestampHelper.GetUtcNow().AddHours(1)
            };

            var createProposalInput = new CreateProposalInput
            {
                ToAddress = ContractManager.Association.Contract,
                Params = createNestProposalInput.ToByteString(),
                OrganizationAddress = UserFeeAddresses.ParliamentController.OwnerAddress,
                ContractMethodName = nameof(AssociationContractContainer.AssociationContractStub.CreateProposal),
                ExpiredTime = TimestampHelper.GetUtcNow().AddHours(1)
            };
            var parliamentCreateProposal =
                await ContractManager.ParliamentAuthStub.CreateProposal.SendAsync(createProposalInput);
            parliamentCreateProposal.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var returnValue = parliamentCreateProposal.TransactionResult.ReadableReturnValue.Replace("\"", "");
            var parliamentProposalId = HashHelper.HexStringToHash(returnValue);

            ContractManager.ParliamentAuth.MinersApproveProposal(parliamentProposalId, Miners);
            var releaseRet =
                ContractManager.ParliamentAuth.ReleaseProposal(parliamentProposalId, ContractManager.CallAddress);
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
                ExpiredTime = TimestampHelper.GetUtcNow().AddHours(1)
            };
            var parliamentCreateProposal =
                await ContractManager.ParliamentAuthStub.CreateProposal.SendAsync(approveProposalInput);
            parliamentCreateProposal.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var returnValue = parliamentCreateProposal.TransactionResult.ReadableReturnValue.Replace("\"", "");
            var parliamentProposalId = HashHelper.HexStringToHash(returnValue);

            ContractManager.ParliamentAuth.MinersApproveProposal(parliamentProposalId, Miners);
            ContractManager.ParliamentAuth.ReleaseProposal(parliamentProposalId, ContractManager.CallAddress);
        }

        private async Task VoteToReferendum(Hash input)
        {
            var referendumProposal = new CreateProposalInput
            {
                ToAddress = ContractManager.Association.Contract,
                Params = input.ToByteString(),
                OrganizationAddress = UserFeeAddresses.ReferendumController.OwnerAddress,
                ContractMethodName = nameof(AssociationContractContainer.AssociationContractStub.Approve),
                ExpiredTime = TimestampHelper.GetUtcNow().AddHours(1)
            };
            var parliamentProposal = new CreateProposalInput
            {
                ToAddress = ContractManager.Referendum.Contract,
                Params = referendumProposal.ToByteString(),
                OrganizationAddress = UserFeeAddresses.ParliamentController.OwnerAddress,
                ContractMethodName = nameof(ContractManager.ReferendumStub.CreateProposal),
                ExpiredTime = TimestampHelper.GetUtcNow().AddHours(1)
            };
            var parliamentCreateProposal =
                await ContractManager.ParliamentAuthStub.CreateProposal.SendAsync(parliamentProposal);
            parliamentCreateProposal.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var returnValue = parliamentCreateProposal.TransactionResult.ReadableReturnValue.Replace("\"", "");
            var parliamentProposalId = HashHelper.HexStringToHash(returnValue);

            ContractManager.ParliamentAuth.MinersApproveProposal(parliamentProposalId, Miners);
            var ret = ContractManager.ParliamentAuth.ReleaseProposal(parliamentProposalId, ContractManager.CallAddress);
            var id = ProposalCreated.Parser
                .ParseFrom(ret.Logs.First(l => l.Name.Contains(nameof(ProposalCreated)))
                    .NonIndexed).ProposalId;
            var approveResult = await ContractManager.ReferendumStub.Approve.SendAsync(id);
            approveResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            parliamentProposal = new CreateProposalInput
            {
                ToAddress = ContractManager.Referendum.Contract,
                Params = id.ToByteString(),
                OrganizationAddress = UserFeeAddresses.ParliamentController.OwnerAddress,
                ContractMethodName = nameof(ContractManager.ReferendumStub.Release),
                ExpiredTime = TimestampHelper.GetUtcNow().AddHours(1)
            };
            parliamentCreateProposal =
                await ContractManager.ParliamentAuthStub.CreateProposal.SendAsync(parliamentProposal);
            parliamentCreateProposal.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            returnValue = parliamentCreateProposal.TransactionResult.ReadableReturnValue.Replace("\"", "");
            parliamentProposalId = HashHelper.HexStringToHash(returnValue);

            ContractManager.ParliamentAuth.MinersApproveProposal(parliamentProposalId, Miners);
            ContractManager.ParliamentAuth.ReleaseProposal(parliamentProposalId, ContractManager.CallAddress);
        }

        private async Task ReleaseToRootForUserFeeByTwoLayer(Hash input)
        {
            var parliamentProposal = new CreateProposalInput
            {
                ToAddress = ContractManager.Association.Contract,
                Params = input.ToByteString(),
                OrganizationAddress = UserFeeAddresses.ParliamentController.OwnerAddress,
                ContractMethodName = nameof(AssociationContractContainer.AssociationContractStub.Release),
                ExpiredTime = TimestampHelper.GetUtcNow().AddHours(1)
            };
            var parliamentCreateProposal =
                await ContractManager.ParliamentAuthStub.CreateProposal.SendAsync(parliamentProposal);
            parliamentCreateProposal.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var returnValue = parliamentCreateProposal.TransactionResult.ReadableReturnValue.Replace("\"", "");
            var parliamentProposalId = HashHelper.HexStringToHash(returnValue);

            ContractManager.ParliamentAuth.MinersApproveProposal(parliamentProposalId, Miners);
            ContractManager.ParliamentAuth.ReleaseProposal(parliamentProposalId, ContractManager.CallAddress);
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

        #endregion
    }
}