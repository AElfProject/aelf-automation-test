using AElf.Automation.Common.Helpers;

namespace AElf.Automation.Common.Contracts
{
    public enum ParliamentMethod
    {
        //Action
        Approve,
        CreateProposal,
        
        
    }

    public class ParliamentAuthContract:BaseContract<ParliamentMethod>
    {
        public ParliamentAuthContract(RpcApiHelper ch, string account) : base(ch, "AElf.Contracts.Parliament", account)
        {
        }

        public ParliamentAuthContract(RpcApiHelper ch, string callAddress, string contractAddress) : base(ch, contractAddress)
        {
            CallAddress = callAddress;
            UnlockAccount(CallAddress);
        }
    }
}