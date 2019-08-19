using System;
using Acs3;
using AElf.Automation.Common.Helpers;
using AElf.Contracts.ParliamentAuth;
using AElf.Kernel;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Volo.Abp.Threading;

namespace AElf.Automation.Common.Contracts
{
    public enum ParliamentMethod
    {
        //Action
        Approve,
        CreateProposal,
        GetProposal,
        Release,
        CreateOrganization,
        
        //View
        GetGenesisOwnerAddress
    }

    public class ParliamentAuthContract : BaseContract<ParliamentMethod>
    {
        public ParliamentAuthContract(IApiHelper ch, string callAddress, string contractAddress) : base(ch,
            contractAddress)
        {
            CallAddress = callAddress;
            UnlockAccount(CallAddress);
        }

        public Hash CreateProposal(string contractAddress, string method, IMessage input, Address organizationAddress, string caller = null)
        {
            var tester = GetParliamentAuthContractTester(caller);
            var createProposalInput = new CreateProposalInput
            {
                ContractMethodName = method,
                ToAddress = AddressHelper.Base58StringToAddress(contractAddress),
                Params = input.ToByteString(),
                ExpiredTime = TimestampHelper.GetUtcNow().AddHours(1),
                OrganizationAddress = organizationAddress
            };
            var proposal = AsyncHelper.RunSync(()=>tester.CreateProposal.SendAsync(createProposalInput));
            proposal.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var returnValue = proposal.TransactionResult.ReadableReturnValue.Replace("\"", "");
            Logger.Info($"Proposal {returnValue} created success by {caller ?? CallAddress}.");
            var proposalId =
                HashHelper.HexStringToHash(returnValue);
            
            return proposalId;
        }

        public void ApproveProposal(Hash proposalId, string caller = null)
        {
            var tester = GetParliamentAuthContractTester(caller);
            var transactionResult = AsyncHelper.RunSync(() => tester.Approve.SendAsync(new ApproveInput
            {
                ProposalId = proposalId
            }));
            transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            Logger.Info($"Proposal {proposalId} approved success by {caller ?? CallAddress}");
        }

        public TransactionResult ReleaseProposal(Hash proposalId, string caller = null)
        {
            var tester = GetParliamentAuthContractTester(caller);
            var result = AsyncHelper.RunSync(() => tester.Release.SendAsync(proposalId));
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            Logger.Info($"Proposal {proposalId} release success by {caller ?? CallAddress}");

            return result.TransactionResult;
        }

        public ParliamentAuthContractContainer.ParliamentAuthContractStub GetParliamentAuthContractTester(string callAddress = null)
        {
            var caller = callAddress ?? CallAddress;
            var stub = new ContractTesterFactory(ApiHelper.GetApiUrl());
            var contractStub =
                stub.Create<ParliamentAuthContractContainer.ParliamentAuthContractStub>(AddressHelper.Base58StringToAddress(ContractAddress), caller);
            return contractStub;
        }
        
        public Address GetGenesisOwnerAddress()
        {
            return CallViewMethod<Address>(ParliamentMethod.GetGenesisOwnerAddress, new Empty());
        }
    }
}