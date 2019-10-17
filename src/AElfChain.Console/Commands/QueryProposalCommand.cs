using AElf;
using AElf.Automation.Common.Helpers;
using AElf.Automation.Common.Managers;
using Newtonsoft.Json;
using Volo.Abp.Threading;

namespace AElfChain.Console.Commands
{
    public class QueryProposalCommand : BaseCommand
    {
        public QueryProposalCommand(INodeManager nodeManager, ContractServices contractServices) : base(nodeManager, contractServices)
        {
        }

        public override void RunCommand()
        {
            var parameters = InputParameters();
            if (parameters == null)
                return;
            var hash = HashHelper.HexStringToHash(parameters[0]);
            var proposalInfo = AsyncHelper.RunSync(()=>Services.ParliamentAuthStub.GetProposal.CallAsync(hash));
            
            $"ProposalId: {parameters[0]} info".WriteSuccessLine();
            JsonConvert.SerializeObject(proposalInfo, Formatting.Indented).WriteSuccessLine();
        }

        public override CommandInfo GetCommandInfo()
        {
            return new CommandInfo
            {
                Name = "proposal",
                Description = "Query Proposal info by Id"
            };
        }

        public override string[] InputParameters()
        {
            var proposalId = "1e91820cf9a6758ec9222dd1bde9e0be5e45beac31cd8ccc8aa3ead2799414ff";
            "Parameter: [ProposalId]".WriteSuccessLine();
            $"eg: {proposalId}".WriteSuccessLine();
            
            return CommandOption.InputParameters(1);
        }
    }
}