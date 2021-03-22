using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AElf.Contracts.MultiToken;
using AElf.Types;
using AElfChain.Common;
using AElfChain.Common.Contracts;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using log4net;
using Shouldly;

namespace AElf.Automation.RpcPerformance
{
    public class TesterTokenMonitor
    {
        private static readonly ILog Logger = Log4NetHelper.GetLogger();

        public TesterTokenMonitor(INodeManager nodeManager,string tokenAddress)
        {
            var genesis = GenesisContract.GetGenesisContract(nodeManager);
            Token = genesis.GetTokenContract(tokenAddress);
        }

        public static TokenContract Token { get; set; }

        public string GenerateNotExistTokenSymbol()
        {
            while (true)
            {
                var symbol = CommonHelper.RandomString(8, false);
                var tokenInfo = Token.GetTokenInfo(symbol);
                if (tokenInfo.Equals(new TokenInfo())) return symbol;
            }
        }
    }
}