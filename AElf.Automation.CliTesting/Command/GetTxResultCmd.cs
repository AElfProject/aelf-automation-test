using System.Linq;
using AElf.Automation.CliTesting.Parsing;
using AElf.Automation.CliTesting.RPC;
using Newtonsoft.Json.Linq;

namespace AElf.Automation.CliTesting.Command
{
    public class GetTxResultCmd : CliCommandDefinition
    {
        public new const string Name = "get_tx_result";
        
        public GetTxResultCmd() : base(Name)
        {
            
        }

        public override string GetUsage()
        {
            return "get_tx_result <txhash>";
        }

        public override string Validate(CmdParseResult parsedCmd)
        {
            if (parsedCmd.Args == null || parsedCmd.Args.Count != 1)
            {
                return "Invalid number of arguments.";
            }

            return null;
        }
        
        public override JObject BuildRequest(CmdParseResult parsedCmd)
        {
            var reqParams = new JObject { ["txhash"] = parsedCmd.Args.ElementAt(0) };
            var req = JsonRpcHelpers.CreateRequest(reqParams, "get_tx_result", 1);

            return req;
        }

        public override string GetPrintString(JObject resp)
        {
            var jobj = JObject.FromObject(resp["result"]);
            return jobj.ToString();
        }
    }
}