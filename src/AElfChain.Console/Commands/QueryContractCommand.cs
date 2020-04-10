using System;
using AElf.Types;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;

namespace AElfChain.Console.Commands
{
    public class QueryContractCommand : BaseCommand
    {
        public QueryContractCommand(INodeManager nodeManager, ContractManager contractManager)
            : base(nodeManager, contractManager)
        {
            Logger = Log4NetHelper.GetLogger();
        }

        public override void RunCommand()
        {
            var contracts = Services.Genesis.GetAllSystemContracts();
            foreach (var key in contracts.Keys)
            {
                var address = contracts[key] == new Address() ? "None" : contracts[key].GetFormatted();
                $"Contract name: {key.ToString().PadRight(16)} Address: {address}".WriteSuccessLine();
            }
        }

        public override CommandInfo GetCommandInfo()
        {
            return new CommandInfo
            {
                Name = "system-contracts",
                Description = "Query all system contracts"
            };
        }

        public override string[] InputParameters()
        {
            throw new NotImplementedException();
        }
    }
}