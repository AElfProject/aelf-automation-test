namespace AElf.Client.Dto
{
    public class NetworkInfoOutput
    {
        /// <summary>
        /// node version
        /// </summary>
        public string Version { get; set; }
        
        /// <summary>
        /// network protocol version
        /// </summary>
        public int ProtocolVersion { get; set; }
        
        /// <summary>
        /// total number of open connections between this node and other nodes
        /// </summary>
        public int Connections { get; set; }
    }
}