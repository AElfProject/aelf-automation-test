using System;
using Acs3;
using AElf.Automation.Common.Helpers;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Automation.Common.Contracts
{
    public enum ParliamentMethod
    {
        //Action
        Approve,
        CreateProposal,
        
        //View
        GetGenesisOwnerAddress
        
    }

    public class ParliamentAuthContract : BaseContract<ParliamentMethod>
    {
        public ParliamentAuthContract(IApiHelper ch, string account) : base(ch, "AElf.Contracts.Parliament", account)
        {
        }

        public ParliamentAuthContract(IApiHelper ch, string callAddress, string contractAddress) : base(ch,
            contractAddress)
        {
            CallAddress = callAddress;
            UnlockAccount(CallAddress);
        }

        public Hash CreateProposal(GenesisMethod method, IMessage input, Address organizationAddress)
        {
            var proposal = ExecuteMethodWithResult(ParliamentMethod.CreateProposal, new CreateProposalInput
                {
                    ContractMethodName = nameof(method),
                    ExpiredTime = DateTime.UtcNow.AddHours(1).ToTimestamp(),
                    Params = input.ToByteString(),
                    //ToAddress = IApiHelper
                     
                }
            );
            
            return Hash.Empty;
        }

        public Address GetGenesisOwnerAddress()
        {
            return CallViewMethod<Address>(ParliamentMethod.GetGenesisOwnerAddress, new Empty());
        }
    }
}