namespace AElf.Automation.Common.WebApi.Dto
{
    public class NotLinkedBlockDto
    {
        public string BlockHash { get; set; }

        public long Height { get; set; }

        public string PreviousBlockHash { get; set; }
    }
}