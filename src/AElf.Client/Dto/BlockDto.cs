namespace AElf.Client.Dto
{
    public class BlockDto
    {
        public string BlockHash { get; set; }
        
        public BlockHeaderDto Header { get; set; }
        
        public BlockBodyDto Body { get; set; }
    }
}