using Acs3;
using AElf;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using AElf.Contracts.ParliamentAuth;
using AElf.Kernel;
using AElf.Sdk.CSharp;
using AElf.Types;
using AElfChain.Common.Utils;
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
        CreateProposal,
        GetProposal,
        Release,
        CreateOrganization,

        //View
        GetDefaultOrganizationAddress,
        GetOrganization
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
            var tester = GetTestStub<ParliamentAuthContractContainer.ParliamentAuthContractStub>(caller);
            var createProposalInput = new CreateProposalInput
            {
                ContractMethodName = method,
                ToAddress = contractAddress.ConvertAddress(),
                Params = input.ToByteString(),
                ExpiredTime = TimestampHelper.GetUtcNow().AddHours(1),
                OrganizationAddress = organizationAddress
            };
            var proposal = AsyncHelper.RunSync(() => tester.CreateProposal.SendAsync(createProposalInput));
            proposal.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var returnValue = proposal.TransactionResult.ReadableReturnValue.Replace("\"", "");
            Logger.Info($"Proposal {returnValue} created success by {caller ?? CallAddress}.");
            var proposalId =
                HashHelper.HexStringToHash(returnValue);

            return proposalId;
        }

        public void ApproveProposal(Hash proposalId, string caller = null)
        {
            var tester = GetTestStub<ParliamentAuthContractContainer.ParliamentAuthContractStub>(caller);
            var transactionResult = AsyncHelper.RunSync(() => tester.Approve.SendAsync(new ApproveInput
            {
                ProposalId = proposalId
            }));
            transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            Logger.Info($"Proposal {proposalId} approved success by {caller ?? CallAddress}");
        }

        public TransactionResult ReleaseProposal(Hash proposalId, string caller = null)
        {
            var tester = GetTestStub<ParliamentAuthContractContainer.ParliamentAuthContractStub>(caller);
            var result = AsyncHelper.RunSync(() => tester.Release.SendAsync(proposalId));
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            Logger.Info($"Proposal {proposalId} release success by {caller ?? CallAddress}");

            return result.TransactionResult;
        }

        public Address GetGenesisOwnerAddress()
        {
            return CallViewMethod<Address>(ParliamentMethod.GetDefaultOrganizationAddress, new Empty());
        }

        public ProposalOutput CheckProposal(Hash proposalId)
        {
            return CallViewMethod<ProposalOutput>(ParliamentMethod.GetProposal,
                proposalId);
        }
    }
}