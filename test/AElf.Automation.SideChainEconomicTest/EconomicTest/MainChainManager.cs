using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Acs0;
using AElf.Client.Service;
using AElf.Contracts.MultiToken;
using AElf.Contracts.TokenConverter;
using AElf.Types;
using AElfChain.Common;
using AElfChain.Common.Contracts;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using log4net;
using Shouldly;

namespace AElf.Automation.SideChainEconomicTest.EconomicTest
{
    public class MainChainManager
    {
        public static ILog Logger = Log4NetHelper.GetLogger();

        public List<string> Symbols = new List<string> {"READ", "WRITE", "STORAGE", "TRAFFIC"};


        public MainChainManager(string serviceUrl, string account)
        {
            MainChain = new ContractServices(serviceUrl, account, NodeOption.DefaultPassword);
        }

        public ContractServices MainChain { get; set; }

        public GenesisContract Genesis => MainChain.GenesisService;

        public TokenContract Token => MainChain.TokenService;

        public INodeManager NodeManager => MainChain.NodeManager;

        public AElfClient ApiClient => MainChain.NodeManager.ApiClient;

        public async Task BuyResources(string account, long amount)
        {
            var tokenConverter = Genesis.GetContractAddressByName(NameProvider.TokenConverter);
            var tokenContract = new TokenConverterContract(NodeManager, account, tokenConverter.GetFormatted());
            var converter =
                tokenContract.GetTestStub<TokenConverterContractContainer.TokenConverterContractStub>(
                    account);

            foreach (var symbol in Symbols)
            {
                var beforeBalance = Token.GetUserBalance(account, symbol);
                //buy
                var transactionResult = await converter.Buy.SendAsync(new BuyInput
                {
                    Symbol = symbol,
                    Amount = amount * 10000_0000
                });
                transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

                var afterBalance = Token.GetUserBalance(account, symbol);
                Logger.Info($"Token '{symbol}' balance: before = {beforeBalance}, after: {afterBalance}");
            }
        }

        public async Task ValidateMainChainTokenAddress(ContractServices sideServices)
        {
            var genesisStub = Genesis.GetGensisStub();
            var validateResult = await genesisStub.ValidateSystemContractAddress.SendAsync(
                new ValidateSystemContractAddressInput
                {
                    Address = Token.Contract,
                    SystemContractHashName = GenesisContract.NameProviderInfos[NameProvider.Token]
                });
            validateResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            //wait to index
            await WaitSideChainIndex(sideServices, validateResult.TransactionResult.BlockNumber);

            //verify
            var transaction = validateResult.Transaction;
            var merklePath = await MainChain.GetMerklePath(validateResult.TransactionResult.TransactionId.ToHex());
            var sideToken = sideServices.GenesisService.GetTokenStub();
            var receiveResult = await sideToken.CrossChainReceiveToken.SendAsync(new CrossChainReceiveTokenInput
            {
                FromChainId = ChainConstInfo.MainChainId,
                MerklePath = merklePath,
                TransferTransactionBytes = transaction.ToByteString()
            });
            receiveResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        }

        public async Task WaitSideChainIndex(ContractServices sideChain, long blockNumber)
        {
            Logger.Info($"Wait side chain index target height: {blockNumber}");
            var crossStub = sideChain.GenesisService.GetCrossChainStub();
            while (true)
            {
                var chainStatus = await ApiClient.GetChainStatusAsync();
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
                    Logger.Info($"Chain lib height: {chainStatus.LastIrreversibleBlockHeight}");
                    await Task.Delay(10000);
                }
            }
        }
    }
}