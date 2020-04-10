using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using AElf.Contracts.TokenConverter;
using AElf.Types;
using AElfChain.Common.Contracts;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using log4net;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace AElf.Automation.Contracts.ScenarioTest
{
    [TestClass]
    public class ResourceContractsTest
    {
        private GenesisContract _genesisContract;
        private TokenContract _tokenContract;
        private TokenConverterContract _tokenConverterContract;
        private TokenConverterContractContainer.TokenConverterContractStub _tokenConverterSub;
        private TokenContractContainer.TokenContractStub _tokenSub;

        private readonly List<string> ResourceSymbol = new List<string>
            {"CPU", "NET", "DISK", "RAM", "READ", "WRITE", "STORAGE", "TRAFFIC"};

        private ILog Logger { get; set; }
        private INodeManager NodeManager { get; set; }
        private string InitAccount { get; } = "28Y8JA1i2cN6oHvdv7EraXJr9a1gY6D1PpJXw9QtRMRwKcBQMK";

        private string BpAccount { get; } = "28Y8JA1i2cN6oHvdv7EraXJr9a1gY6D1PpJXw9QtRMRwKcBQMK";

//        private static string RpcUrl { get; } = "18.212.240.254:8000";
        private static string RpcUrl { get; } = "192.168.197.40:8000";
        private string Symbol { get; } = "TEST";

        [TestInitialize]
        public void Initialize()
        {
            Log4NetHelper.LogInit("ContractTest");
            Logger = Log4NetHelper.GetLogger();

            NodeManager = new NodeManager(RpcUrl);
            _genesisContract = GenesisContract.GetGenesisContract(NodeManager, InitAccount);
            _tokenContract = _genesisContract.GetTokenContract(InitAccount);
            _tokenConverterContract = _genesisContract.GetTokenConverterContract(InitAccount);
            var tester = new ContractTesterFactory(NodeManager);
            _tokenSub = _genesisContract.GetTokenStub(InitAccount);
            _tokenConverterSub = _genesisContract.GetTokenConverterStub(InitAccount);
        }

        [TestMethod]
        public async Task BuyResource()
        {
            foreach (var resource in ResourceSymbol)
            {
                var balance = await _tokenSub.GetBalance.CallAsync(new GetBalanceInput
                {
                    Owner = AddressHelper.Base58StringToAddress(InitAccount),
                    Symbol = NodeManager.GetNativeTokenSymbol()
                });
                var otherTokenBalance = await _tokenSub.GetBalance.CallAsync(new GetBalanceInput
                {
                    Owner = AddressHelper.Base58StringToAddress(InitAccount),
                    Symbol = resource
                });

                Logger.Info($"user ELF balance is {balance} user {resource} balance is {otherTokenBalance}");

                var result = await _tokenConverterSub.Buy.SendAsync(new BuyInput
                {
                    Amount = 100000_00000000,
                    Symbol = resource
                });
                var size = result.Transaction.CalculateSize();
                Logger.Info($"transfer size is: {size}");

                var afterBalance = await _tokenSub.GetBalance.CallAsync(new GetBalanceInput
                {
                    Owner = AddressHelper.Base58StringToAddress(InitAccount),
                    Symbol = NodeManager.GetNativeTokenSymbol()
                });

                var afterOtherTokenBalance = await _tokenSub.GetBalance.CallAsync(new GetBalanceInput
                {
                    Owner = AddressHelper.Base58StringToAddress(InitAccount),
                    Symbol = resource
                });

                Logger.Info(
                    $"After buy token, user ELF balance is {afterBalance} user {resource} balance is {afterOtherTokenBalance}");
            }
        }

        [TestMethod]
        public async Task SellResource()
        {
            var balance = await _tokenSub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = AddressHelper.Base58StringToAddress(InitAccount),
                Symbol = NodeManager.GetNativeTokenSymbol()
            });
            var otherTokenBalance = await _tokenSub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = AddressHelper.Base58StringToAddress(InitAccount),
                Symbol = "CPU"
            });

            Logger.Info($"user ELF balance is {balance} user {Symbol} balance is {otherTokenBalance}");

            var result = await _tokenConverterSub.Sell.SendAsync(new SellInput
            {
                Amount = 200000000,
                Symbol = "CPU"
            });
            var size = result.Transaction.CalculateSize();
            Logger.Info($"transfer size is: {size}");

            var afterBalance = await _tokenSub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = AddressHelper.Base58StringToAddress(InitAccount),
                Symbol = NodeManager.GetNativeTokenSymbol()
            });

            var afterOtherTokenBalance = await _tokenSub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = AddressHelper.Base58StringToAddress(InitAccount),
                Symbol = "CPU"
            });

            Logger.Info(
                $"After sell token, user ELF balance is {afterBalance} user {Symbol} balance is {afterOtherTokenBalance}");
        }

        [TestMethod]
        public async Task BurnResourceToken()
        {
            foreach (var symbol in ResourceSymbol)
            {
                var result = await _tokenSub.Burn.SendAsync(new BurnInput
                {
                    Amount = 1000_00000000,
                    Symbol = symbol
                });
                result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

                var info = await _tokenSub.GetTokenInfo.CallAsync(new GetTokenInfoInput {Symbol = symbol});
                info.Burned.ShouldBe(1000_00000000);
                info.Supply.ShouldBe(info.TotalSupply - info.Burned);
            }
        }

        [TestMethod]
        [DataRow("uSXxaGWKDBPV6Z8EG8Et9sjaXhH1uMWEpVvmo2KzKEaueWzSe")]
        public async Task TransferResourceToContract(string contract)
        {
            foreach (var resource in ResourceSymbol)
                await _tokenSub.Transfer.SendAsync(new TransferInput
                {
                    To = contract.ConvertAddress(),
                    Amount = 50000_00000000L,
                    Symbol = resource,
                    Memo = "transfer resource"
                });

            foreach (var resource in ResourceSymbol)
            {
                var balance = await _tokenSub.GetBalance.CallAsync(new GetBalanceInput
                {
                    Symbol = resource,
                    Owner = contract.ConvertAddress()
                });
                Logger.Info($"Token: {resource}, Balance: {balance.Balance}");
            }
        }
    }
}