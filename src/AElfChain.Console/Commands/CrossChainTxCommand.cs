using System.Collections.Generic;
using AElfChain.Common.Contracts;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Sharprompt;

namespace AElfChain.Console.Commands
{
    public class CrossChainTxCommand : BaseCommand
    {
        private NodeManager _mainManager;
        private NodeManager _sideManager;
        public CrossChainTxCommand(INodeManager nodeManager, ContractServices contractServices)
            : base(nodeManager, contractServices)
        {
            Logger = Log4NetHelper.GetLogger();
        }

        public override void RunCommand()
        {
            var mainEndpoint = Prompt.Input<string>("Input main chain endpoint");
            _mainManager = new NodeManager(mainEndpoint);
            var mainGenesis = _mainManager.GetGenesisContract();
            var mainToken = mainGenesis.GetTokenStub();
            var mainTokenInfo = mainToken.GetNativeTokenInfo.CallAsync(new Empty()).Result;
            Logger.Info(JsonFormatter.ToDiagnosticString(mainTokenInfo), Format.Json);
            
            var sideEndpoint = Prompt.Input<string>("Input side chain endpoint");
            _sideManager = new NodeManager(sideEndpoint);
            var sideGenesis = _sideManager.GetGenesisContract();
            var sideToken = sideGenesis.GetTokenStub();
            var sideTokenInfo = sideToken.GetNativeTokenInfo.CallAsync(new Empty()).Result;
            Logger.Info(JsonFormatter.ToDiagnosticString(sideTokenInfo), Format.Json);

            var quitCommand = false;
            while (!quitCommand)
            {
                var command = Prompt.Select("Select crosschain tx command", GetSubCommands());
                switch (command)
                {
                    case "Register":
                        Register();
                        break;
                    case "Transfer[Main-Side]":
                        TransferMain2Side();
                        break;
                    case "Transfer[Side-Main]":
                        TransferSide2Main();
                        break;
                    case "Validation":
                        Validation();
                        break;
                    case "Exit":
                        quitCommand = true;
                        break;
                    default:
                        Logger.Error("Not supported transaction method.");
                        var subCommands = GetSubCommands();
                        string.Join("\r\n", subCommands).WriteSuccessLine();
                        break;
                }
            }
        }

        private void Register()
        {
            
        }

        private void TransferMain2Side()
        {
            
        }

        private void TransferSide2Main()
        {
            
        }

        private void Validation()
        {
            
        }

        public override CommandInfo GetCommandInfo()
        {
            return new CommandInfo
            {
                Name = "cross-chain-tx",
                Description = "Cross chain transactions"
            };
        }

        public override string[] InputParameters()
        {
            throw new System.NotImplementedException();
        }
        
        private IEnumerable<string> GetSubCommands()
        {
            return new List<string>
            {
                "Register",
                "Transfer[Main-Side]",
                "Transfer[Side-Main]",
                "Validation",
                "Exit"
            };
        }
    }
}