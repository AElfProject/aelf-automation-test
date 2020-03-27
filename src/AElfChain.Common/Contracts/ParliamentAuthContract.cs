using System.Collections.Generic;
using Acs3;
using AElf.Contracts.Parliament;
using AElf.CSharp.Core.Extension;
using AElf.Types;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Volo.Abp.Threading;

namespace AElfChain.Common.Contracts
{
    public enum ParliamentMethod
    {
        //Action
        Initialize,
        Approve,
        Reject,
        Abstain,
        CreateProposal,
        GetProposal,
        Release,
        CreateOrganization,
        ChangeOrganizationThreshold,
        ChangeOrganizationProposerWhiteList,
        ClearProposal,

        //View
        GetDefaultOrganizationAddress,
        GetOrganization,
        ValidateOrganizationExist,
        CalculateOrganizationAddress,
        ValidateAddressIsParliamentMember,
        GetProposerWhiteList
    }

    public class ParliamentAuthContract : BaseContract<ParliamentMethod>
    {
        public ParliamentAuthContract(INodeManager nm, string callAddress, string contractAddress) :
            base(nm, contractAddress)
        {
            Logger = Log4NetHelper.GetLogger();
            SetAccount(callAddress);
        }

        public Hash CreateProposal(string contractAddress, string method, IMessage input, Address organizationAddress,
            string caller = null)
        {
            var tester = GetTestStub<ParliamentContractContainer.ParliamentContractStub>(caller);
            var createProposalInput = new CreateProposalInput
            {
                ContractMethodName = method,
                ToAddress = contractAddress.ConvertAddress(),
                Params = input.ToByteString(),
                ExpiredTime = KernelHelper.GetUtcNow().AddMinutes(10),
                OrganizationAddress = organizationAddress
            };
            var proposal = AsyncHelper.RunSync(() => tester.CreateProposal.SendAsync(createProposalInput));
            proposal.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined,
                proposal.TransactionResult.TransactionId.ToHex);
            var proposalId = proposal.Output;
            Logger.Info($"Proposal {proposalId} created success by {caller ?? CallAddress}.");

            return proposalId;
        }

        public void ApproveProposal(Hash proposalId, string caller = null)
        {
            var tester = GetTestStub<ParliamentContractContainer.ParliamentContractStub>(caller);
            var transactionResult = AsyncHelper.RunSync(() => tester.Approve.SendAsync(proposalId));
            transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            Logger.Info($"Proposal {proposalId} approved success by {caller ?? CallAddress}");
        }

        public string Approve(Hash proposalId, string caller)
        {
            SetAccount(caller);
            return ExecuteMethodWithTxId(ParliamentMethod.Approve, proposalId);
        }

        public string Abstain(Hash proposalId, string caller)
        {
            SetAccount(caller);
            return ExecuteMethodWithTxId(ParliamentMethod.Abstain, proposalId);
        }

        public string Reject(Hash proposalId, string caller)
        {
            SetAccount(caller);
            return ExecuteMethodWithTxId(ParliamentMethod.Reject, proposalId);
        }

        public void MinersApproveProposal(Hash proposalId, IEnumerable<string> callers)
        {
            var approveTxIds = new List<string>();
            foreach (var user in callers)
            {
                var tester = GetNewTester(user);
                var txId = tester.ExecuteMethodWithTxId(ParliamentMethod.Approve, proposalId);
                approveTxIds.Add(txId);
            }

            NodeManager.CheckTransactionListResult(approveTxIds);
        }

        public TransactionResult ReleaseProposal(Hash proposalId, string caller = null)
        {
            var tester = GetTestStub<ParliamentContractContainer.ParliamentContractStub>(caller);
            var result = AsyncHelper.RunSync(() => tester.Release.SendAsync(proposalId));
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            Logger.Info($"Proposal {proposalId} release success by {caller ?? CallAddress}");

            return result.TransactionResult;
        }

        public Address GetGenesisOwnerAddress()
        {
            return CallViewMethod<Address>(ParliamentMethod.GetDefaultOrganizationAddress, new Empty());
        }

        public Organization GetOrganization(Address organization)
        {
            return CallViewMethod<Organization>(ParliamentMethod.GetOrganization, organization);
        }

        public ProposalOutput CheckProposal(Hash proposalId)
        {
            return CallViewMethod<ProposalOutput>(ParliamentMethod.GetProposal,
                proposalId);
        }
    }
}