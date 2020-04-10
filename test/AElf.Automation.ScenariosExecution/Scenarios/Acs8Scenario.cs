using System;
using System.Linq;
using System.Threading;
using AElf.Client.Service;
using AElf.Contracts.MultiToken;
using AElf.Contracts.TestContract.TransactionFees;
using AElf.Contracts.TokenConverter;
using AElf.Types;
using AElfChain.Common;
using AElfChain.Common.Contracts;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Helpers;
using Google.Protobuf.WellKnownTypes;
using log4net;
using Shouldly;
using Volo.Abp.Threading;

namespace AElf.Automation.ScenariosExecution.Scenarios
{
    public class Acs8Scenario : BaseScenario
    {
        public new static readonly ILog Logger = Log4NetHelper.GetLogger();

        public Acs8Scenario()
        {
            InitializeScenario();
            CheckResourceTokenTime = DateTime.Now;
            Calculator = new TransactionFeeCalculator();

            Token = Services.TokenService;
            TokenConverter = Services.TokenConverterService;
            TxFees = TransactionFeesContract.GetOrDeployTxFeesContract(Services.NodeManager, Services.CallAddress);
            PrepareAcs8ResourceToken();
            TxFees.InitializeTxFees(TxFees.Contract); //tx contract itself, just for test acs8.
        }

        public TransactionFeesContract TxFees { get; set; }
        public TokenConverterContract TokenConverter { get; set; }

        public AElfClient Client => Services.NodeManager.ApiClient;

        public DateTime CheckResourceTokenTime { get; set; }

        public TokenContract Token { get; set; }

        public TransactionFeeCalculator Calculator { get; set; }

        public void RunAcs8ScenarioJob()
        {
            ExecuteStandaloneTask(new Action[]
            {
                OnlyCpuCounterAction,
                OnlyRamCounterAction,
                BothCpuAndRamCounterAction,
                NoCpuAndRamCounterAction,
                CheckAcs8ContractTokenAction,
                UpdateEndpointAction
            });
        }

        private void OnlyCpuCounterAction()
        {
            var beforeResource = TxFees.QueryContractResource();
            var randNo = CommonHelper.GenerateRandomNumber(1, 10);
            var txResult = TxFees.ExecuteMethodWithResult(TxFeesMethod.ReadCpuCountTest, new Int32Value
            {
                Value = randNo
            });
            if (txResult.Status.ConvertTransactionResultStatus() == TransactionResultStatus.Mined)
            {
                WaitOneBlock(txResult.BlockNumber);
                var afterResource = TxFees.QueryContractResource();

                var txSize = txResult.Transaction.GetTxSize();
                var cpuFee = Calculator.Cpu.GetSizeFee(randNo);
                var netFee = Calculator.Net.GetSizeFee(txSize);
                var stoFee = Calculator.Net.GetSizeFee(txSize);

                //assert result
                beforeResource["READ"].ShouldBe(afterResource["READ"] + cpuFee);
                beforeResource["WRITE"].ShouldBe(afterResource["WRITE"]);
                beforeResource["TRAFFIC"].ShouldBe(afterResource["TRAFFIC"] + netFee);
                beforeResource["STORAGE"].ShouldBe(afterResource["STORAGE"] + stoFee);
            }
        }

        private void OnlyRamCounterAction()
        {
            var beforeResource = TxFees.QueryContractResource();
            var randNo = CommonHelper.GenerateRandomNumber(1, 10);
            var txResult = TxFees.ExecuteMethodWithResult(TxFeesMethod.WriteRamCountTest, new Int32Value
            {
                Value = randNo
            });
            if (txResult.Status.ConvertTransactionResultStatus() == TransactionResultStatus.Mined)
            {
                WaitOneBlock(txResult.BlockNumber);
                var afterResource = TxFees.QueryContractResource();

                var txFee = txResult.GetDefaultTransactionFee();
                //assert result
                beforeResource["READ"].ShouldBe(afterResource["READ"]);
                beforeResource["WRITE"].ShouldBeGreaterThan(afterResource["WRITE"]);
                beforeResource["TRAFFIC"].ShouldBe(afterResource["TRAFFIC"] + txFee);
                beforeResource["STORAGE"].ShouldBe(afterResource["STORAGE"] + txFee);
            }
        }

        private void BothCpuAndRamCounterAction()
        {
            var beforeResource = TxFees.QueryContractResource();
            var randNo1 = CommonHelper.GenerateRandomNumber(1, 10);
            var randNo2 = CommonHelper.GenerateRandomNumber(1, 10);
            var txResult = TxFees.ExecuteMethodWithResult(TxFeesMethod.ComplexCountTest, new ReadWriteInput
            {
                Read = randNo1,
                Write = randNo2
            });
            if (txResult.Status.ConvertTransactionResultStatus() == TransactionResultStatus.Mined)
            {
                WaitOneBlock(txResult.BlockNumber);
                var afterResource = TxFees.QueryContractResource();

                var txFee = txResult.GetDefaultTransactionFee();
                //assert result
                beforeResource["READ"].ShouldBeGreaterThan(afterResource["READ"]);
                beforeResource["WRITE"].ShouldBeGreaterThan(afterResource["WRITE"]);
                beforeResource["TRAFFIC"].ShouldBe(afterResource["TRAFFIC"] + txFee);
                beforeResource["STORAGE"].ShouldBe(afterResource["STORAGE"] + txFee);
            }
        }

        private void NoCpuAndRamCounterAction()
        {
            var beforeResource = TxFees.QueryContractResource();
            var txResult = TxFees.ExecuteMethodWithResult(TxFeesMethod.NoReadWriteCountTest, new StringValue
            {
                Value = $"NoReadWriteCountTest-{Guid.NewGuid().ToString()}"
            });
            if (txResult.Status.ConvertTransactionResultStatus() == TransactionResultStatus.Mined)
            {
                WaitOneBlock(txResult.BlockNumber);
                var afterResource = TxFees.QueryContractResource();

                var txFee = txResult.GetDefaultTransactionFee();
                //assert result
                beforeResource["READ"].ShouldBeGreaterThan(afterResource["READ"]);
                beforeResource["WRITE"].ShouldBeGreaterThan(afterResource["WRITE"]);
                beforeResource["TRAFFIC"].ShouldBe(afterResource["TRAFFIC"] + txFee);
                beforeResource["STORAGE"].ShouldBe(afterResource["STORAGE"] + txFee);
            }
        }

        private void CheckAcs8ContractTokenAction()
        {
            var timeSpan = DateTime.Now - CheckResourceTokenTime;
            if (timeSpan.Minutes < 1) return;

            Console.WriteLine();
            CheckResourceTokenTime = DateTime.Now;
            CheckAcs8ResourceToken();
        }

        public void PrepareAcs8ResourceToken()
        {
            var symbols = new[] {"READ", "WRITE", "TRAFFIC", "STORAGE"};
            var firstBp = AllNodes.First().Account;
            TokenConverter.SetAccount(firstBp);
            Token.SetAccount(firstBp);
            Logger.Info("Prepare advance resource token for acs8 contract.");
            foreach (var symbol in symbols)
            {
                var balance = Token.GetUserBalance(TxFees.ContractAddress, symbol);
                if (balance <= 100_00000000)
                {
                    //buy resource
                    var buyResult = TokenConverter.ExecuteMethodWithResult(TokenConverterMethod.Buy, new BuyInput
                    {
                        Amount = 10000_00000000,
                        Symbol = symbol
                    });
                    buyResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

                    //transfer to
                    var transferResult = Token.ExecuteMethodWithResult(TokenMethod.AdvanceResourceToken,
                        new AdvanceResourceTokenInput
                        {
                            ContractAddress = TxFees.Contract,
                            ResourceTokenSymbol = symbol,
                            Amount = 10000_00000000
                        });
                    transferResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
                }
            }
        }

        public void CheckAcs8ResourceToken()
        {
            var resources = TxFees.CallViewMethod<ResourcesOutput>(TxFeesMethod.QueryContractResource, new Empty());
            var firstBp = AllNodes.First().Account;
            TokenConverter.SetAccount(firstBp);
            Token.SetAccount(firstBp);
            Logger.Info("Check acs8 contract resource task.");
            foreach (var resource in resources.Resources)
                if (resource.Amount <= 100_00000000 && resource.Symbol != NodeOption.NativeTokenSymbol)
                {
                    //buy resource
                    var buyResult = TokenConverter.ExecuteMethodWithResult(TokenConverterMethod.Buy, new BuyInput
                    {
                        Amount = 10000_00000000,
                        Symbol = resource.Symbol
                    });
                    buyResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

                    //transfer to
                    var transferResult = Token.ExecuteMethodWithResult(TokenMethod.AdvanceResourceToken,
                        new AdvanceResourceTokenInput
                        {
                            ContractAddress = TxFees.Contract,
                            ResourceTokenSymbol = resource.Symbol,
                            Amount = 10000_00000000
                        });
                    transferResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
                }
        }

        private void WaitOneBlock(long blockHeight)
        {
            while (true)
            {
                var height = AsyncHelper.RunSync(Client.GetBlockHeightAsync);
                if (height >= blockHeight + 1)
                    return;
                Thread.Sleep(500);
            }
        }
    }
}