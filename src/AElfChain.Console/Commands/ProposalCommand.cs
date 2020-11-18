using System;
using System.Collections.Generic;
using AElf;
using AElf.Standards.ACS3;
using AElf.Types;
using AElfChain.Common.Contracts;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using Newtonsoft.Json;
using Sharprompt;
using Shouldly;
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
                case "ChangeOrganizationThreshold":
                    ChangeOrganizationThreshold();
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
            var proposalId = Hash.LoadFromHex(hashInput);
            var proposalInfo = AsyncHelper.RunSync(() => Services.ParliamentContractImplStub.GetProposal.CallAsync(proposalId));

            $"ProposalId: {proposalId} info".WriteSuccessLine();
            JsonConvert.SerializeObject(proposalInfo, Formatting.Indented).WriteSuccessLine();
        }

        private void ApproveProposal()
        {
        }

        private void MinersApproveProposal()
        {
            var hashInput = Prompt.Input<string>("Input ProposalId");
            var proposalId = Hash.LoadFromHex(hashInput);
            var miners = Services.Authority.GetMinApproveMiners();
            Services.Parliament.MinersApproveProposal(proposalId, miners);
        }

        private void ReleaseProposal()
        {
            var hashInput = Prompt.Input<string>("Input ProposalId");
            var proposalId = Hash.LoadFromHex(hashInput);
            Services.Parliament.ReleaseProposal(proposalId);
        }

        private void ChangeOrganizationThreshold()
        {
            var parameters = InputParameters();
            var organizationAddress = parameters[0];
            var creator = parameters[1];
            var info = Services.Parliament.GetOrganization(organizationAddress.ConvertAddress());
            Logger.Info($"Before change: {info}");

            var input = new ProposalReleaseThreshold
            {
                MaximalAbstentionThreshold = long.Parse(parameters[2]),
                MaximalRejectionThreshold = long.Parse(parameters[3]),
                MinimalApprovalThreshold = long.Parse(parameters[4]),
                MinimalVoteThreshold = long.Parse(parameters[5]),
            };
            
            var result = Services.Authority.ExecuteTransactionWithAuthority(Services.Parliament.ContractAddress,
                nameof(ParliamentMethod.ChangeOrganizationThreshold), input, creator, organizationAddress.ConvertAddress());
            result.Status.ShouldBe(TransactionResultStatus.Mined);
            info = Services.Parliament.GetOrganization(organizationAddress.ConvertAddress());
            Logger.Info($"After change: {info}");
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
            var organization = "28Y8JA1i2cN6oHvdv7EraXJr9a1gY6D1PpJXw9QtRMRwKcBQMK";
            var creator = "28Y8JA1i2cN6oHvdv7EraXJr9a1gY6D1PpJXw9QtRMRwKcBQMK";
            var maximalAbstentionThreshold  = 1000;
            var maximalRejectionThreshold  = 1000;
            var minimalApprovalThreshold  = 1000;
            var minimalVoteThreshold  = 1000;


            "Parameter: [organization] [creator] [maximalAbstentionThreshold] [maximalRejectionThreshold] [minimalApprovalThreshold] [minimalVoteThreshold]".WriteSuccessLine();
            $"eg: {organization} {creator} {maximalAbstentionThreshold} {maximalRejectionThreshold} {minimalApprovalThreshold} {minimalVoteThreshold}".WriteSuccessLine();

            return CommandOption.InputParameters(6);
        }

        private IEnumerable<string> GetSubCommands()
        {
            return new List<string>
            {
                "QueryProposal",
                "ApproveProposal",
                "MinersApproveProposal",
                "ReleaseProposal",
                "ChangeOrganizationThreshold"
            };
        }
    }
}