using System;
using System.Collections.Generic;
using System.Linq;
using AElfChain.Common;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using AElfChain.Console.Commands;
using AElfChain.Console.InputOption;
using AElfChain.SDK;
using log4net;
using Shouldly;

namespace AElfChain.Console
{
    public class CliCommand
    {
        public List<BaseCommand> Commands;

        private readonly ILog Logger = Log4NetHelper.GetLogger();

        public CliCommand(INodeManager nodeManager)
        {
            NodeManager = nodeManager;
            var bp = NodeInfoHelper.Config.Nodes.First();
            Contracts = new ContractServices(nodeManager, bp.Account);
            Commands = new List<BaseCommand>();
            InitializeCommands();
        }

        private INodeManager NodeManager { get; }

        private ContractServices Contracts { get; }

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
                //var input = Prompt.Select("[Input command order/name]", GetCommandList());

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
                catch (AElfChainApiException e)
                {
                    Logger.Error($"Error: {e.GetType().Name}, Message: {e.Message}");
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
            Commands.Add(new CrossChainTxCommand(NodeManager, Contracts));
            Commands.Add(new AnalyzeCommand(NodeManager, Contracts));
            Commands.Add(new ContractQueryCommand(NodeManager, Contracts));
            Commands.Add(new ContractExecutionCommand(NodeManager, Contracts));
            Commands.Add(new QueryContractCommand(NodeManager, Contracts));
            Commands.Add(new QueryTokenCommand(NodeManager, Contracts));
            Commands.Add(new QueryProposalCommand(NodeManager, Contracts));
            Commands.Add(new ConsensusCommand(NodeManager, Contracts));
            Commands.Add(new DeployCommand(NodeManager, Contracts));
            Commands.Add(new UpdateCommand(NodeManager, Contracts));
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
                $"{count:00}. [{command.GetCommandInfo().Name}]-{command.GetCommandInfo().Description}"
                    .WriteSuccessLine();
                count++;
            }

            "=======================================================".WriteSuccessLine();
        }
    }
}