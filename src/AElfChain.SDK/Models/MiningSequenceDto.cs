namespace AElfChain.SDK.Models
{
    public class MiningSequenceDto
    {
        public string Pubkey { get; set; }
        public string MiningTime { get; set; }
        public string Behaviour { get; set; }
        public long BlockHeight { get; set; }
        public string PreviousBlockHash { get; set; }
    }
}