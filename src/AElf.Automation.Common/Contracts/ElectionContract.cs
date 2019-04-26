using AElf.Automation.Common.Helpers;

namespace AElf.Automation.Common.Contracts
{
    public enum ElectionMethod
    {
        //action
        InitialElectionContract,
        AnnounceElection,
        QuitElection,
        Vote,
        Withdraw,
        UpdateTermNumber,
        
        //view
        GetElectionResult
    }
    public class ElectionContract : BaseContract<ElectionMethod>
    {
        public ElectionContract(RpcApiHelper ch, string callAddress, string electionAddress) :
            base(ch, electionAddress)
        {
            CallAddress = callAddress;
            UnlockAccount(CallAddress);
        }

        public ElectionContract(RpcApiHelper ch, string callAddress)
            :base(ch, "AElf.Contracts.Election", callAddress)
        {
        }
    }
}