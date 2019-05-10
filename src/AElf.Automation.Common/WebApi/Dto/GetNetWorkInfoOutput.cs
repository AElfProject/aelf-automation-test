namespace AElf.Automation.Common.WebApi.Dto
{
    public class GetNetWorkInfoOutput
    {
        public string Version { get; set; }

        public int ProtocolVersion { get; set; }
        
        public int Connections { get; set; }
    }
}