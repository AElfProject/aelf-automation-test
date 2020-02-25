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
        public async Task Update_Coefficient_With_Developer()
        {
            const int pieceKey = 1000000;
            const FeeTypeEnum feeType = FeeTypeEnum.Storage;
            var updateInput = new CoefficientFromContract
            {
                FeeType = feeType,
                Coefficient = new CoefficientFromSender
                {
                    LinerCoefficient = new LinerCoefficient
                    {
                        ConstantValue = 1,
                        Denominator = 5,
                        Numerator = 999
                    },
                    PieceKey = pieceKey,
                    IsLiner = true
                }
            };

            var proposalId = await CreateToRootForDeveloperFeeByTwoLayer(updateInput);
            await ApproveToRootForDeveloperFeeByTwoLayer(proposalId);

            var middleApproveProposalId = await ApproveToRootForDeveloperFeeByMiddleLayer(proposalId);
            await ApproveThenReleaseMiddleProposalForDeveloper(middleApproveProposalId);

            await ReleaseToRootForDeveloperFeeByTwoLayer(proposalId);

            var userCoefficient =
                await ContractManager.TokenStub.GetCalculateFeeCoefficientOfContract.CallAsync(new SInt32Value
                {
                    Value = (int) feeType
                });
            var hasModified = userCoefficient.Coefficients.Single(x => x.PieceKey == pieceKey);
            hasModified.CoefficientDic["ConstantValue".ToLower()].ShouldBe(1);
            hasModified.CoefficientDic["Denominator".ToLower()].ShouldBe(5);
            hasModified.CoefficientDic["Numerator".ToLower()].ShouldBe(999);
        }

        [TestMethod]
        public async Task Update_Coefficient_With_User()
        {
            const int pieceKey = 1000000;
            var updateInput = new CoefficientFromSender
            {
                LinerCoefficient = new LinerCoefficient
                {
                    ConstantValue = 1000,
                    Denominator = 500,
                    Numerator = 1
                },
                PieceKey = pieceKey,
                IsLiner = true
            };
            var proposalId = await CreateToRootForUserFeeByTwoLayer(updateInput);
            await ApproveToRootForUserFeeByTwoLayer(proposalId);
            await VoteToReferendum(proposalId);
            await ReleaseToRootForUserFeeByTwoLayer(proposalId);

            var userCoefficient =
                await ContractManager.TokenStub.GetCalculateFeeCoefficientOfSender.CallAsync(new Empty());
            var hasModified = userCoefficient.Coefficients.Single(x => x.PieceKey == pieceKey);
            hasModified.CoefficientDic["ConstantValue".ToLower()].ShouldBe(1000);
            hasModified.CoefficientDic["Denominator".ToLower()].ShouldBe(500);
            hasModified.CoefficientDic["Numerator".ToLower()].ShouldBe(1);
        }

        private async Task InitializeAuthorizedOrganization()
        {
            await ContractManager.TokenImplStub.InitializeAuthorizedController.SendAsync(new Empty());
            //initializeResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            DeveloperFeeAddresses = await ContractManager.TokenImplStub.GetDeveloperFeeController.CallAsync(new Empty());
            Logger.Info($"Developer RootController: {DeveloperFeeAddresses.RootController}");
            Logger.Info($"Developer ParliamentController: {DeveloperFeeAddresses.ParliamentController}");
            Logger.Info($"Developer DeveloperController: {DeveloperFeeAddresses.DeveloperController}");

            UserFeeAddresses = await ContractManager.TokenImplStub.GetUserFeeController.CallAsync(new Empty());
            Logger.Info($"User RootController: {UserFeeAddresses.RootController}");
            Logger.Info($"User ParliamentController: {UserFeeAddresses.ParliamentController}");
            Logger.Info($"User ReferendumController: {UserFeeAddresses.ReferendumController}");
        }
        
        #region Developer actions

        private async Task<Hash> CreateToRootForDeveloperFeeByTwoLayer(CoefficientFromContract input)
        {
            var createNestProposalInput = new CreateProposalInput
            {
                ToAddress = ContractManager.Token.Contract,
                Params = input.ToByteString(),
                OrganizationAddress = DeveloperFeeAddresses.RootController.OwnerAddress,
                ContractMethodName = nameof(TokenContractContainer.TokenContractStub.UpdateCoefficientFromContract),
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

        private async Task<Hash> CreateToRootForUserFeeByTwoLayer(CoefficientFromSender input)
        {
            var createNestProposalInput = new CreateProposalInput
            {
                ToAddress = ContractManager.Token.Contract,
                Params = input.ToByteString(),
                OrganizationAddress = UserFeeAddresses.RootController.OwnerAddress,
                ContractMethodName = nameof(TokenContractContainer.TokenContractStub.UpdateCoefficientFromSender),
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
            var releaseRet = ContractManager.ParliamentAuth.ReleaseProposal(parliamentProposalId, ContractManager.CallAddress);
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