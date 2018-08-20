using System;
using System.Collections.Generic;
using System.Text;

namespace AElf.Automation.Common.Extensions
{
    public class CommandInfo
    {
        public string Category { get; set; }
        public string Cmd { get; set; }
        public string Parameter { get; set; }

        public bool Result { get; set; }
        public List<string> InfoMsg { get; set; }
        public List<string> ErrorMsg { get; set; }
        public long TimeSpan { get; set; }

        public CommandInfo(string cmd, string category="")
        {
            Category = (category == "") ? cmd : category; 
            Cmd = cmd;
            InfoMsg = new List<string>();
            ErrorMsg = new List<string>();
            Result = false;
            TimeSpan = 0;
        }
    }
}
