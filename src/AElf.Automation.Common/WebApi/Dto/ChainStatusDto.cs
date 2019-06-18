using System.Collections.Generic;

namespace AElf.Automation.Common.WebApi.Dto
{
    public class ChainStatusDto
    {
        public string ChainId { get; set; }

        public Dictionary<string, long> Branches { get; set; }

        public List<NotLinkedBlockDto> NotLinkedBlocks { get; set; }

        public long LongestChainHeight { get; set; }

        public string LongestChainHash { get; set; }

        public string GenesisBlockHash { get; set; }

        public string GenesisContractAddress { get; set; }

        public string LastIrreversibleBlockHash { get; set; }

        public long LastIrreversibleBlockHeight { get; set; }

        public string BestChainHash { get; set; }

        public long BestChainHeight { get; set; }
    }
}