using System;
using System.Linq;
using AElf.Automation.CliTesting.Parsing;
using AElf.Automation.CliTesting.RPC;
using Newtonsoft.Json.Linq;

namespace AElf.Automation.CliTesting.Command
{
    public class BroadcastBlockCmd : CliCommandDefinition
    {
        private const string CommandName = "broadcast_block";
        
        public BroadcastBlockCmd() : base(CommandName)
        {
        }

        public override bool IsLocal { get; } = true;

        public override string GetUsage()
        {
            return "usage broadcast_block";
        }

        public override string Validate(CmdParseResult parsedCommand)
        {
            if (parsedCommand == null)
                return "no command";

            return null;
        }
        
        public override JObject BuildRequest(CmdParseResult parsedCommand)
        {
            throw new NotImplementedException();
        }
        
        public override string GetPrintString(JObject resp)
        {
            throw new NotImplementedException();
        }
    }
}