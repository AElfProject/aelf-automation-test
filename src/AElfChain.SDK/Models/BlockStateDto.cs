using System.Collections.Generic;

namespace AElfChain.SDK.Models
{
    public class BlockStateDto
    {
        public string BlockHash { get; set; }

        public string PreviousHash { get; set; }

        public long BlockHeight { get; set; }

        public Dictionary<string, string> Changes { get; set; }
    }
}