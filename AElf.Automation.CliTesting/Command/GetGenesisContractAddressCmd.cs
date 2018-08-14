using System.Linq;
using AElf.Automation.CliTesting.Parsing;
using AElf.Automation.CliTesting.RPC;
using Newtonsoft.Json.Linq;

namespace AElf.Automation.CliTesting.Command
{
    public class GetGenesisContractAddressCmd : CliCommandDefinition
    {
        public new const string Name = "connect_chain";
        
        public GetGenesisContractAddressCmd() : base(Name)
        {
            
        }

        public override string GetUsage()
        {
            return "connect_chain";
        }

        public override string Validate(CmdParseResult parsedCmd)
        {
            return null;
        }
        
        public override JObject BuildRequest(CmdParseResult parsedCmd)
        {
            var reqParams = new JObject();
            var req = JsonRpcHelpers.CreateRequest(reqParams, "connect_chain", 1);

            return req;
        }

        public override string GetPrintString(JObject resp)
        {
            var j = JObject.FromObject(resp["result"]);
            
            return j.ToString();
        } 
    }
}