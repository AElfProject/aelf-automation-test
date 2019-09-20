using System;
using AElf.Automation.Common.Contracts;
using AElf.Automation.Common.Helpers;
using AElfChain.Console.CommandOptions;
using Microsoft.Extensions.CommandLineUtils;

namespace AElfChain.Console.Commands
{
    public class CommonTransferToken : BaseCommand
    {
        protected TransferTokenCommandOption _transferTokenCommandOption;
        public CommonTransferToken()
        {
            Name = "transfer";
            Description = "Transfer token";
            
            HelpOption("-? | -h | --help");
            OnExecute((Func<int>)RunCommand);
        }

        protected override void InitOptions()
        {
            base.InitOptions();
            _transferTokenCommandOption = new TransferTokenCommandOption();
            _transferTokenCommandOption.AddOptionToCommandLineApplication(this);
        }

        protected override int RunCommand()
        {
            var nodeManager = _transferTokenCommandOption.NodeManager;
            var genesis = GenesisContract.GetGenesisContract(nodeManager, _transferTokenCommandOption.From);
            var token = genesis.GetTokenContract();

            var before = token.GetUserBalance(_transferTokenCommandOption.To, _transferTokenCommandOption.Symbol);
            token.TransferBalance(_transferTokenCommandOption.From, _transferTokenCommandOption.To,
                _transferTokenCommandOption.Amount, _transferTokenCommandOption.Symbol);
            var after = token.GetUserBalance(_transferTokenCommandOption.To, _transferTokenCommandOption.Symbol);
            $"User {_transferTokenCommandOption.To} token info: Before={before}, After={after}".WriteSuccessLine();

            return 0;
        }
    }
}