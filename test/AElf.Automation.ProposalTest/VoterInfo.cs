namespace AElf.Automation.ProposalTest
{
    public class VoterInfo
    {
        public readonly string ProposalId;
        public readonly long Quantity;
        public readonly string Voter;

        public VoterInfo(string voter, int quantity, string proposalId)
        {
            Voter = voter;
            Quantity = quantity;
            ProposalId = proposalId;
        }
    }
}