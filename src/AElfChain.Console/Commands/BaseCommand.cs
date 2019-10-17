using System;
using System.Collections.Generic;
using System.Linq;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using log4net;

namespace AElfChain.Console.Commands
{
    public class CommandInfo
    {
        public string Name { get; set; }
        public string Description { get; set; }
    }
    
    public abstract class BaseCommand
    {
        public INodeManager NodeManager { get; set; }
        
        public ContractServices Services { get; set; }

        public ILog Logger = Log4NetHelper.GetLogger();
        
        public BaseCommand(INodeManager nodeManager, ContractServices contractServices)
        {
            NodeManager = nodeManager;
            Services = contractServices;
        }
        
        public abstract void RunCommand();

        public abstract CommandInfo GetCommandInfo();

        public abstract string[] InputParameters();
    }
}