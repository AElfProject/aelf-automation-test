namespace AElf.Automation.ProposalTest
{
    public class VoterInfo
    {
        public readonly string Voter;
        public readonly long Quantity;
        public readonly string ProposalId;

        public VoterInfo(string voter, int quantity, string proposalId)
        {
            Voter = voter;
            Quantity = quantity;
            ProposalId = proposalId;
        }
    }
}