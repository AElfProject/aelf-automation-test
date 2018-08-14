using AElf.Automation.CliTesting.Parsing;
using AElf.Automation.CliTesting.RPC;
using Newtonsoft.Json.Linq;

namespace AElf.Automation.CliTesting.Command
{
    public class GetBlockHeightCmd : CliCommandDefinition
    {
        public new const string Name = "get_block_height";
        
        public GetBlockHeightCmd() : base(Name)
        {
            
        }

        public override string GetUsage()
        {
            return "get_block_height";
        }

        public override string Validate(CmdParseResult parsedCmd)
        {
            return null;
        }
        
        public override JObject BuildRequest(CmdParseResult parsedCmd)
        {
            var reqParams = new JObject();
            var req = JsonRpcHelpers.CreateRequest(reqParams, "get_block_height", 1);

            return req;
        }

        public override string GetPrintString(JObject resp)
        {
            var j = JObject.FromObject(resp["result"]);
            
            return j.ToString();
        }
    }
}