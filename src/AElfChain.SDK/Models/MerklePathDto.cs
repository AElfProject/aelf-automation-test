using System.Collections.Generic;

namespace AElfChain.SDK.Models
{
    public class MerklePathDto
    {
        public List<MerklePathNodeDto> MerklePathNodes;
    }

    public class MerklePathNodeDto
    {
        public string Hash { get; set; }
        public bool IsLeftChildNode { get; set; }
    }
}