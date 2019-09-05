using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Automation.Common.Contracts;
using AElf.Automation.Common.Helpers;
using AElf.Automation.Common.OptionManagers;
using AElf.Automation.Common.OptionManagers.Authority;
using AElf.Contracts.MultiToken;
using AElf.Types;
using AElfChain.SDK;
using Google.Protobuf.WellKnownTypes;
using log4net;
using Shouldly;

namespace AElf.Automation.SideChainEconomicTest.EconomicTest
{
    public class SideChainManager
    {
        public Dictionary<int, ContractServices> SideChains { get; set; }
        private List<string> Symbols = new List<string>{"CPU", "NET", "STO"};

        public static ILog Logger = Log4NetHelper.GetLogger();

        public SideChainManager()
        {
            SideChains = new Dictionary<int, ContractServices>();
        }

        public ContractServices InitializeSideChain(string serviceUrl, string account, int chainId)
        {
            var contractServices = new ContractServices(serviceUrl, account, Account.DefaultPassword);
            
            SideChains.Add(chainId, contractServices);

            return contractServices;
        }

        public void SetResourceUnitPrice(ContractServices services)
        {
            Logger.Info("Set resource token price");
            var authority = new AuthorityManager(services.ApiHelper, services.CallAddress);
            var ownerAddress = services.ParliamentService.GetGenesisOwnerAddress();
            //set resource token price
            var contract = services.TokenService.ContractAddress;
            const string method = "SetResourceTokenUnitPrice";
            var input = new SetResourceTokenUnitPriceInput
            {
                CpuUnitPrice = 100,
                NetUnitPrice = 100,
                StoUnitPrice = 100
            };
            var miners = NodeInfoHelper.Config.Nodes.Select(o => o.Account).ToList();
            var transactionResult = authority.ExecuteTransactionWithAuthority(contract, method, input, ownerAddress, miners, services.CallAddress);
            transactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        }
        
        public async Task WaitMainChainIndex(ContractServices mainChain, long blockNumber)
        {
            Logger.Info($"Wait side chain index target height: {blockNumber}");
            var crossStub = mainChain.GenesisService.GetCrossChainStub();
            while (true)
            {
                var chainStatus = await mainChain.ApiService.GetChainStatusAsync();
                if (chainStatus.LastIrreversibleBlockHeight >= blockNumber)
                {
                    try
                    {
                        var indexHeight = await crossStub.GetParentChainHeight.CallAsync(new Empty());
                        if(indexHeight.Value > blockNumber)
                            break;
                        
                        Logger.Info($"Current index height: {indexHeight.Value}");
                        await Task.Delay(4000);
                    }
                    catch (Exception e)
                    {
                        Logger.Error(e.Message);
                    }
                }
                else
                {
                    Logger.Info($"mainChain lib height: {chainStatus.LastIrreversibleBlockHeight}");
                    await Task.Delay(10000);
                }
            }
        }
    }
}