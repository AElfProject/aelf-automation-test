using AElf.Types;

namespace AElf.Automation.ProposalTest
{
    public class ApproveInfo
    {
        public ApproveInfo(string type, string account, string txId)
        {
            Type = type;
            TxId = txId;
            Account = account;
        }

        public ApproveInfo(string type, string account, Hash proposal, long amount)
        {
            Type = type;
            Proposal = proposal;
            Account = account;
            Amount = amount;
        }

        public string Type { get; }
        public string TxId { get; }
        public Hash Proposal { get; }

        public string Account { get; }
        public long Amount { get; }
    }
}