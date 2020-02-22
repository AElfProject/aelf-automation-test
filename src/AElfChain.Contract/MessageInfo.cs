using System.Collections.Generic;

namespace AElfChain.Contract
{
    public class MessageInfo
    {
        public MessageInfo(string name)
        {
            Name = name;
            Fields = new List<string>();
        }

        public MessageInfo(string name, List<string> fields)
        {
            Name = name;
            Fields = fields;
        }

        public string Name { get; set; }
        public List<string> Fields { get; set; }
    }
}