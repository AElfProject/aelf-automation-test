using AElf.Automation.Common.Managers;
using Microsoft.Extensions.CommandLineUtils;

namespace AElfChain.Console.CommandOptions
{
    public class EndpointCommandOption : ICommandOption
    {
        public CommandOption EndpointOption { get; set; }
        public CommandOption IsSideChainOption { get; set; }
        public string Endpoint { get; set; }
        public bool IsSideChain { get; set; }
        
        public INodeManager NodeManager => new NodeManager(Endpoint);

        public bool HasInputErrors { get; protected set; }

        public virtual void AddOptionToCommandLineApplication(CommandLineApplication commandLineApplication)
        {
            EndpointOption = commandLineApplication.AddOptionEndpoint();
            IsSideChainOption = commandLineApplication.AddOptionIsMainChain();
        }

        public virtual void ParseAndValidateInput()
        {
            Endpoint = EndpointOption.TryParseRequiredString(HasInputErrors);
            IsSideChain = EndpointOption.TryParseAndValidateBool(HasInputErrors);
        }
    }
}