using ProtoBuf;

namespace AElf.Automation.CliTesting.Data.Protobuf
{
    [ProtoContract]
    public class Block
    {
        [ProtoMember(1)] 
        public BlockHeader Header { get; set; }
        
        [ProtoMember(2)] 
        public BlockBody Body { get; set; }
    }
}