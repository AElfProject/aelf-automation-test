using AElf.Automation.Common.Helpers;
using AElf.Automation.Common.Managers;
using AElf.Types;

namespace AElfChain.Console.Commands
{
    public class QueryContractCommand : BaseCommand
    {
        public QueryContractCommand(INodeManager nodeManager, ContractServices contractServices) 
            : base(nodeManager, contractServices)
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

        public override string GetCommandInfo()
        {
            return "Query all system contracts";
        }

        public override string[] InputParameters()
        {
            throw new System.NotImplementedException();
        }
    }
}