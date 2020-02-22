using System.Collections.Generic;

namespace AElfChain.Contract
{
    public class ContractDescriptor
    {
        public ContractDescriptor()
        {
            MessageInfos = new List<MessageInfo>();
            Methods = new List<MethodInfo>();
        }

        public List<MessageInfo> MessageInfos { get; set; }
        public List<MethodInfo> Methods { get; set; }
    }
}