using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Automation.Common;
using AElf.Automation.Common.Helpers;
using AElf.Automation.Common.Managers;
using AElfChain.Console.Commands;
using log4net;

namespace AElfChain.Console
{
    public class TransactionScripts
    {
        private INodeManager NodeManager { get; set; }

        private ContractServices Contracts { get; set; }

        private ILog Logger = Log4NetHelper.GetLogger();

        public List<BaseCommand> Commands;

        public TransactionScripts(INodeManager nodeManager)
        {
            NodeManager = nodeManager;
            var bp = NodeInfoHelper.Config.Nodes.First();
            Contracts = new ContractServices(nodeManager, bp.Account);
            Commands = new List<BaseCommand>();
            InitializeCommands();
        }

        public void ExecuteTransactionCommand()
        {
            while (true)
            {
                var input = GetUsageInfo();
                var result = int.TryParse(input, out var select);
                if (!result || select > Commands.Count)
                {
                    Logger.Error("Wrong input selection.");
                    continue;
                }

                var command = Commands[select - 1];
                $"Name: {command.GetCommandInfo()}".WriteSuccessLine();
                try
                {
                    command.RunCommand();
                }
                catch (Exception e)
                {
                    Logger.Error(e.Message);
                }

                "Quit execution transaction(yes/no)? ".WriteWarningLine(changeLine: false);
                input = System.Console.ReadLine();
                if (input.ToLower().Trim().Equals("yes"))
                    break;
            }
        }

        public void InitializeCommands()
        {
            Commands.Add(new SwitchOtherChainCommand(NodeManager, Contracts));
            Commands.Add(new QueryContractCommand(NodeManager, Contracts));
            Commands.Add(new QueryTokenCommand(NodeManager, Contracts));
            Commands.Add(new QueryProposalCommand(NodeManager, Contracts));
            Commands.Add(new ConsensusCommand(NodeManager, Contracts));
            Commands.Add(new DeployCommand(NodeManager, Contracts));
            Commands.Add(new TransferCommand(NodeManager, Contracts));
            Commands.Add(new ResourceTradeCommand(NodeManager, Contracts));
            Commands.Add(new SetConnectorCommand(NodeManager, Contracts));
            Commands.Add(new SetTransactionFeeCommand(NodeManager, Contracts));
            Commands.Add(new TransactionLimitCommand(NodeManager,Contracts));
        }

        private string GetUsageInfo()
        {
            var count = 1;
            "==================== Command ====================".WriteSuccessLine();
            foreach (var command in Commands)
            {
                $"{count:00}. {command.GetCommandInfo()}".WriteSuccessLine();
                count++;
            }

            "=================================================".WriteSuccessLine();
            "Please input command order number to execution: ".WriteSuccessLine(changeLine: false);
            
            return System.Console.ReadLine();
        }
    }
}