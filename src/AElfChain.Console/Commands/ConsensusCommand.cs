using System.Linq;
using AElf.Automation.Common;
using AElf.Automation.Common.Helpers;
using AElf.Automation.Common.Managers;
using Google.Protobuf.WellKnownTypes;
using Nethereum.Hex.HexConvertors.Extensions;
using Volo.Abp.Threading;

namespace AElfChain.Console.Commands
{
    public class ConsensusCommand : BaseCommand
    {
        public ConsensusCommand(INodeManager nodeManager, ContractServices contractServices)
            : base(nodeManager, contractServices)
        {
        }

        public override void RunCommand()
        {
            var minerList = AsyncHelper.RunSync(()=> Services.ConsensusStub.GetCurrentMinerList.CallAsync(new Empty()));
            var pubKeys = minerList.Pubkeys.Select(item => item.ToByteArray().ToHex()).ToList();
            var count = 0;
            NodeInfoHelper.Config.CheckNodesAccount();
            
            "Current bp account info:".WriteSuccessLine();
            foreach (var node in NodeOption.AllNodes)
            {
                if (pubKeys.Contains(node.PublicKey))
                {
                    $"{++count:00}. Account: {node.Account}".WriteSuccessLine();
                    $"    PubKey: {node.PublicKey}".WriteSuccessLine();
                }
            }
        }

        public override CommandInfo GetCommandInfo()
        {
            return new CommandInfo
            {
                Name = "consensus",
                Description = "Query current consensus miners"
            };
        }

        public override string[] InputParameters()
        {
            throw new System.NotImplementedException();
        }
    }
}