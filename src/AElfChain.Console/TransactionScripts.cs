using System;
using System.Linq;
using System.Threading.Tasks;
using AElf.Automation.Common;
using AElf.Automation.Common.Contracts;
using AElf.Automation.Common.Helpers;
using AElf.Automation.Common.Managers;
using AElf.Automation.Common.Utils;
using AElf.Contracts.MultiToken;
using AElfChain.SDK;
using log4net;

namespace AElfChain.Console
{
    public class TransactionScripts
    {
        private INodeManager NodeManager { get; set; }
        private GenesisContract Genesis { get; set; }

        private ILog Logger = Log4NetHelper.GetLogger();

        public TransactionScripts(INodeManager nodeManager)
        {
            NodeManager = nodeManager;
            Genesis = GenesisContract.GetGenesisContract(nodeManager);
        }

        public async Task ExecuteTransactionCommand()
        {
            UsageInfo();
            var input = System.Console.ReadLine();
            while (true)
            {
                var result = int.TryParse(input, out var select);
                if (!result)
                {
                    Logger.Error("Wrong input.");
                    continue;
                }

                switch (select)
                {
                    case 1 :
                        await TransferToken();
                        break;
                    case 2:
                        await DeployContract();
                        break;
                    default:
                        Logger.Error("Wrong input.");
                        continue;
                }

                "Quit transaction execution(yes/no)? ".WriteWarningLine();
                input = System.Console.ReadLine();
                if(input.ToLower().Trim().Equals("yes"))
                    break;
                UsageInfo();
            }
        }

        private async Task TransferToken(string address = "", string symbol = "ELF")
        {
            var bp = NodeInfoHelper.Config.Nodes.First();
            var tokenStub = Genesis.GetTokenStub(bp.Account);
            var tokenContract = Genesis.GetTokenContract();

            if (address == "")
            {
                "Please input transfer account address: ".WriteSuccessLine();
                address = System.Console.ReadLine();
            }
            var beforeBalance = tokenContract.GetUserBalance(address, symbol);
            
            await tokenStub.Transfer.SendAsync(new TransferInput
            {
                Symbol = symbol,
                To = address.ConvertAddress(),
                Amount = 1000_00000000L,
                Memo = $"Transfer for test - {Guid.NewGuid()}"
            });
            
            var afterBalance = tokenContract.GetUserBalance(address, symbol);
            Logger.Info($"Account : {address}");
            Logger.Info($"Before balance: {beforeBalance}, after balance: {afterBalance}");
        }

        private async Task DeployContract()
        {
            "Input transfer account address: ".WriteSuccessLine();
            var address = System.Console.ReadLine();
            "Input contract file name: ".WriteSuccessLine();
            var filename = System.Console.ReadLine();
            
            var tokenContract = Genesis.GetTokenContract();
            var balance = tokenContract.GetUserBalance(address);
            if (balance == 0)
            {
                Logger.Error("user account token balance is 0 and cannot deploy contract.");
                return;
            }
            
            var authority = new AuthorityManager(NodeManager, address);
            authority.DeployContractWithAuthority(address, filename);
        }

        private void UsageInfo()
        {
            "1. Transfer token to tester".WriteSuccessLine();
            "2. Deploy contract".WriteSuccessLine();
            "Select item you want execute: ".WriteSuccessLine();
        }
    }
}