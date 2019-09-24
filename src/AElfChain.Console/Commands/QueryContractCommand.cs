using AElf.Automation.Common.Helpers;
using AElf.Automation.Common.Managers;

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
            Logger.Info($"Genesis contract address: {Services.Genesis.ContractAddress}");
            Services.Genesis.GetAllSystemContracts();
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