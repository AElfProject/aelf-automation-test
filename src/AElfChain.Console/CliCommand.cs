using System;
using System.Collections.Generic;
using System.Linq;
using AElf.Automation.Common;
using AElf.Automation.Common.Helpers;
using AElf.Automation.Common.Managers;
using AElfChain.Console.Commands;
using AElfChain.Console.InputOption;
using log4net;

namespace AElfChain.Console
{
    public class CliCommand
    {
        private INodeManager NodeManager { get; set; }

        private ContractServices Contracts { get; set; }

        private ILog Logger = Log4NetHelper.GetLogger();

        public List<BaseCommand> Commands;

        public List<string> CommandNames => Commands.Select(o => o.GetCommandInfo()).ToList();

        public ConsoleReader InputReader { get; set; }

        public CliCommand(INodeManager nodeManager)
        {
            NodeManager = nodeManager;
            var bp = NodeInfoHelper.Config.Nodes.First();
            Contracts = new ContractServices(nodeManager, bp.Account);
            Commands = new List<BaseCommand>();
            InitializeCommands();
        }

        public void ExecuteTransactionCommand()
        {
            InputReader = new ConsoleReader(new CommandsCompletionEngine(CommandNames));
            GetUsageInfo();
            while (true)
            {
                "[Input command order/name]=> ".WriteWarningLine(changeLine: false);
                var input = InputReader.ReadLine();
                
                //quit command
                var quitCommand = new List<string>{"quit", "exit", "close"};
                if (quitCommand.Contains(input.ToLower().Trim()))
                {
                    "CLI was been closed.".WriteWarningLine();
                    break;
                }
                
                //execute command
                var command = Commands.FirstOrDefault(o => o.GetCommandInfo().Equals(input));
                if (command == null)
                {
                    var result = int.TryParse(input, out var select);
                    if (!result || select > Commands.Count)
                    {
                        Logger.Error("Wrong input selection, please refer following command list.");
                        GetUsageInfo();
                        continue;
                    }
                    command = Commands[select - 1];
                }

                $"Name: {command.GetCommandInfo()}".WriteSuccessLine();
                try
                {
                    command.RunCommand();
                }
                catch (Exception e)
                {
                    Logger.Error(e);
                }
            }
        }

        private void InitializeCommands()
        {
            Commands.Add(new BlockChainCommand(NodeManager, Contracts));
            Commands.Add(new ContractQueryCommand(NodeManager, Contracts));
            Commands.Add(new ContractExecutionCommand(NodeManager, Contracts));
            Commands.Add(new QueryContractCommand(NodeManager, Contracts));
            Commands.Add(new QueryTokenCommand(NodeManager, Contracts));
            Commands.Add(new QueryProposalCommand(NodeManager, Contracts));
            Commands.Add(new ConsensusCommand(NodeManager, Contracts));
            Commands.Add(new DeployCommand(NodeManager, Contracts));
            Commands.Add(new TransferCommand(NodeManager, Contracts));
            Commands.Add(new ResourceTradeCommand(NodeManager, Contracts));
            Commands.Add(new SetConnectorCommand(NodeManager, Contracts));
            Commands.Add(new SetTransactionFeeCommand(NodeManager, Contracts));
            Commands.Add(new TransactionLimitCommand(NodeManager, Contracts));
        }

        private void GetUsageInfo()
        {
            var count = 1;
            "======================= Command =======================".WriteSuccessLine();
            foreach (var command in Commands)
            {
                $"{count:00}. {command.GetCommandInfo()}".WriteSuccessLine();
                count++;
            }

            "=======================================================".WriteSuccessLine();
        }
    }
}