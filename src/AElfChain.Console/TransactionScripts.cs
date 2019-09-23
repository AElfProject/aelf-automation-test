using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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
            Contracts = new ContractServices(nodeManager);
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
                
                "Quit transaction execution(yes/no)? ".WriteWarningLine(changeLine: false);
                input = System.Console.ReadLine();
                if(input.ToLower().Trim().Equals("yes"))
                    break;
            }
        }

        public void InitializeCommands()
        {
            Commands.Add(new TransferCommand(NodeManager, Contracts));
            Commands.Add(new QueryTokenCommand(NodeManager, Contracts));
            Commands.Add(new DeployCommand(NodeManager, Contracts));
            Commands.Add(new ResourceTradeCommand(NodeManager, Contracts));
            Commands.Add(new SetConnectorCommand(NodeManager, Contracts));
            Commands.Add(new SetTransactionFeeCommand(NodeManager, Contracts));
            Commands.Add(new SwitchOtherChainCommand(NodeManager, Contracts));
        }

        public string GetUsageInfo()
        {
            var count = 1;
            $"=====================Command=====================".WriteSuccessLine();
            foreach (var command in Commands)
            {
                $"{count:0}. {command.GetCommandInfo()}".WriteSuccessLine();
                count++;
            }
            $"=================================================".WriteSuccessLine();
            $"Please input which command you want to execution: ".WriteSuccessLine(changeLine:false);
            return System.Console.ReadLine();
        }
    }
}