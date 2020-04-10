using System;
using System.Collections.Generic;
using AElf;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using Newtonsoft.Json;
using Sharprompt;
using Volo.Abp.Threading;

namespace AElfChain.Console.Commands
{
    public class ProposalCommand : BaseCommand
    {
        public ProposalCommand(INodeManager nodeManager, ContractManager contractManager)
            : base(nodeManager, contractManager)
        {
        }

        public override void RunCommand()
        {
            var command = Prompt.Select("Select api command", GetSubCommands());
            switch (command)
            {
                case "QueryProposal":
                    QueryProposalInfo();
                    break;
                case "ApproveProposal":
                    ApproveProposal();
                    break;
                case "MinersApproveProposal":
                    MinersApproveProposal();
                    break;
                case "ReleaseProposal":
                    ReleaseProposal();
                    break;
                default:
                    Logger.Error("Not supported api method.");
                    var subCommands = GetSubCommands();
                    string.Join("\r\n", subCommands).WriteSuccessLine();
                    break;
            }
        }

        private void QueryProposalInfo()
        {
            var hashInput = Prompt.Input<string>("Input ProposalId");
            var proposalId = HashHelper.HexStringToHash(hashInput);
            var proposalInfo = AsyncHelper.RunSync(() => Services.ParliamentAuthStub.GetProposal.CallAsync(proposalId));

            $"ProposalId: {proposalId} info".WriteSuccessLine();
            JsonConvert.SerializeObject(proposalInfo, Formatting.Indented).WriteSuccessLine();
        }

        private void ApproveProposal()
        {
        }

        private void MinersApproveProposal()
        {
            var hashInput = Prompt.Input<string>("Input ProposalId");
            var proposalId = HashHelper.HexStringToHash(hashInput);
            var miners = Services.Authority.GetMinApproveMiners();
            Services.ParliamentAuth.MinersApproveProposal(proposalId, miners);
        }

        private void ReleaseProposal()
        {
        }

        public override CommandInfo GetCommandInfo()
        {
            return new CommandInfo
            {
                Name = "proposal",
                Description = "Proposal related commands."
            };
        }

        public override string[] InputParameters()
        {
            throw new NotImplementedException();
        }

        private IEnumerable<string> GetSubCommands()
        {
            return new List<string>
            {
                "QueryProposal",
                "ApproveProposal",
                "MinersApproveProposal",
                "ReleaseProposal"
            };
        }
    }
}