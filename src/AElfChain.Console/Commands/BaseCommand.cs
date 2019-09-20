using System;
using AElf.Automation.Common.Helpers;
using AElf.Automation.Common.Managers;
using AElfChain.Console.CommandOptions;
using Microsoft.Extensions.CommandLineUtils;

namespace AElfChain.Console.Commands
{
    public abstract class BaseCommand : CommandLineApplication
    {
        protected EndpointCommandOption EndpointOption;
        
        public BaseCommand()
        {
            InitOptions();
            HelpOption("-? | -h | --help");
            OnExecute(RunCommand);
        }

        protected virtual void InitOptions()
        {
            Log4NetHelper.LogInit();
            EndpointOption = new EndpointCommandOption();
            EndpointOption.AddOptionToCommandLineApplication(this);
        }

        protected abstract int RunCommand();
    }
}