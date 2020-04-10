using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using AElf.Types;
using AElfChain.Common;
using AElfChain.Common.Contracts;
using AElfChain.Common.Helpers;
using Google.Protobuf.WellKnownTypes;
using log4net;
using Shouldly;

namespace AElf.Automation.SideChainEconomicTest.EconomicTest
{
    public class SideChainManager
    {
        public static ILog Logger = Log4NetHelper.GetLogger();

        public SideChainManager()
        {
            SideChains = new Dictionary<int, ContractServices>();
        }

        public Dictionary<int, ContractServices> SideChains { get; set; }

        public ContractServices InitializeSideChain(string serviceUrl, string account, int chainId)
        {
            var contractServices = new ContractServices(serviceUrl, account, NodeOption.DefaultPassword);

            SideChains.Add(chainId, contractServices);

            return contractServices;
        }

        public async Task WaitMainChainIndex(ContractServices mainChain, long blockNumber)
        {
            Logger.Info($"Wait side chain index target height: {blockNumber}");
            var crossStub = mainChain.GenesisService.GetCrossChainStub();
            while (true)
            {
                var chainStatus = await mainChain.ApiClient.GetChainStatusAsync();
                if (chainStatus.LastIrreversibleBlockHeight >= blockNumber)
                {
                    try
                    {
                        var indexHeight = await crossStub.GetParentChainHeight.CallAsync(new Empty());
                        if (indexHeight.Value > blockNumber)
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

        public async Task RunCheckSideChainTokenInfo(ContractServices chain, string tokenSymbol)
        {
            var tokenTester = chain.GenesisService.GetTokenStub();
            var tokenInfo = await tokenTester.GetTokenInfo.CallAsync(new GetTokenInfoInput
            {
                Symbol = tokenSymbol
            });
            Logger.Info($"Token info: {tokenInfo}");

            //transfer some other owner
            var account = chain.NodeManager.AccountManager.GetRandomAccount();
            var address = AddressHelper.Base58StringToAddress(account);
            await tokenTester.Transfer.SendAsync(new TransferInput
            {
                Symbol = tokenSymbol,
                Amount = 1000_000,
                To = address,
                Memo = $"Transfer for execution test {Guid.NewGuid()}"
            });
            var issuerBalance = await tokenTester.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = chain.CallAccount,
                Symbol = tokenSymbol
            });
            Logger.Info($"Issuer token balance: {issuerBalance.Balance}");

            var balance = await tokenTester.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = address,
                Symbol = tokenSymbol
            });
            Logger.Info($"Random owner token balance: {balance.Balance}");

            tokenTester = chain.TokenService.GetTestStub<TokenContractContainer.TokenContractStub>(account);
            var transactionResult = await tokenTester.Approve.SendAsync(new ApproveInput
            {
                Symbol = tokenSymbol,
                Amount = 1000_000,
                Spender = chain.CallAccount
            });
            transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            account = chain.NodeManager.AccountManager.GetRandomAccount();
            tokenTester = chain.TokenService.GetTestStub<TokenContractContainer.TokenContractStub>(account);
            transactionResult = await tokenTester.Approve.SendAsync(new ApproveInput
            {
                Symbol = tokenSymbol,
                Amount = 1000_000,
                Spender = chain.CallAccount
            });
            transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.NotExisted);
        }
    }
}