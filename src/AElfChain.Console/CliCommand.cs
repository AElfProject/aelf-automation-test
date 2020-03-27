using System;
using System.Collections.Generic;
using System.Linq;
using AElfChain.Common;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using AElfChain.Console.Commands;
using AElfChain.Console.InputOption;
using log4net;
using Shouldly;

namespace AElfChain.Console
{
    public class CliCommand
    {
        private readonly ILog Logger = Log4NetHelper.GetLogger();
        public List<BaseCommand> Commands;

        public CliCommand(INodeManager nodeManager)
        {
            NodeManager = nodeManager;
            var bp = NodeInfoHelper.Config.Nodes.First();
            ContractManager = new ContractManager(nodeManager, bp.Account);
            Commands = new List<BaseCommand>();
            RegisterCommands();
        }

        private INodeManager NodeManager { get; }

        private ContractManager ContractManager { get; }

        public List<string> CommandNames => Commands.Select(o => o.GetCommandInfo().Name).ToList();

        public ConsoleReader InputReader { get; set; }

        public void ExecuteTransactionCommand()
        {
            CommandNames.Add("config");
            InputReader = new ConsoleReader(new CommandsCompletionEngine(CommandNames));
            GetUsageInfo();
            while (true)
            {
                "[Input command order/name]=> ".WriteWarningLine(changeLine: false);
                var input = InputReader.ReadLine();

                //quit command
                var quitCommand = new List<string> {"quit", "exit", "close"};
                if (quitCommand.Contains(input.ToLower().Trim()))
                {
                    "CLI was been closed.".WriteWarningLine();
                    break;
                }

                //clear console
                if (input.ToLower().Trim() == "clear")
                {
                    System.Console.Clear();
                    continue;
                }

                //config info
                if (input.ToLower().Trim() == "config")
                {
                    $"Endpoint: {NodeManager.GetApiUrl()}".WriteSuccessLine();
                    $"ConfigFile: {NodeInfoHelper.ConfigFile}".WriteSuccessLine();
                    foreach (var node in NodeInfoHelper.Config.Nodes)
                    {
                        $"Name: {node.Name}  Endpoint: {node.Endpoint}".WriteSuccessLine();
                        $"Account: {node.Account}".WriteSuccessLine();
                        $"PublicKey: {NodeManager.GetAccountPublicKey(node.Account, node.Password)}".WriteSuccessLine();
                    }

                    continue;
                }

                //execute command
                var command = Commands.FirstOrDefault(o => o.GetCommandInfo().Name.Equals(input));
                if (command == null)
                {
                    if (input == "list" || input == "help" || input == "?")
                    {
                        GetUsageInfo();
                        continue;
                    }

                    var result = int.TryParse(input, out var select);
                    if (!result || select > Commands.Count)
                    {
                        Logger.Error("Wrong input selection, please refer following command list.");
                        GetUsageInfo();
                        continue;
                    }

                    command = Commands[select - 1];
                }

                $"Name: {command.GetCommandInfo().Description}".WriteSuccessLine();
                try
                {
                    command.RunCommand();
                }
                catch (ArgumentException e)
                {
                    Logger.Error($"Error: {e.GetType().Name}, Message: {e.Message}");
                }
                catch (FormatException e)
                {
                    Logger.Error($"Error: {e.GetType().Name}, Message: {e.Message}");
                }
                catch (TimeoutException e)
                {
                    Logger.Error($"Error: {e.GetType().Name}, Message: {e.Message}");
                }
                catch (ShouldAssertException e)
                {
                    Logger.Error($"Error: {e.GetType().Name}, Message: {e.Message}");
                }
                catch (Exception e)
                {
                    Logger.Error(e);
                }
            }
        }

        private void RegisterCommands()
        {
            Commands.Add(new ChainApiCommand(NodeManager, ContractManager));
            Commands.Add(new CrossChainTxCommand(NodeManager, ContractManager));
            Commands.Add(new AnalyzeCommand(NodeManager, ContractManager));
            Commands.Add(new ContractQueryCommand(NodeManager, ContractManager));
            Commands.Add(new ContractExecutionCommand(NodeManager, ContractManager));
            Commands.Add(new QueryContractCommand(NodeManager, ContractManager));
            Commands.Add(new QueryTokenCommand(NodeManager, ContractManager));
            Commands.Add(new ProposalCommand(NodeManager, ContractManager));
            Commands.Add(new ConsensusCommand(NodeManager, ContractManager));
            Commands.Add(new DeployCommand(NodeManager, ContractManager));
            Commands.Add(new UpdateCommand(NodeManager, ContractManager));
            Commands.Add(new TransferCommand(NodeManager, ContractManager));
            Commands.Add(new ResourceTradeCommand(NodeManager, ContractManager));
            Commands.Add(new SetConnectorCommand(NodeManager, ContractManager));
            Commands.Add(new SetTransactionFeeCommand(NodeManager, ContractManager));
            Commands.Add(new TransactionLimitCommand(NodeManager, ContractManager));
        }

        private void GetUsageInfo()
        {
            var count = 1;
            "======================= Command =======================".WriteSuccessLine();
            foreach (var command in Commands)
            {
                $"{count:00}. [{command.GetCommandInfo().Name}]-{command.GetCommandInfo().Description}"
                    .WriteSuccessLine();
                count++;
            }

            "=======================================================".WriteSuccessLine();
        }
    }
}