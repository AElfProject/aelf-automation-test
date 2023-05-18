using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Standards.ACS1;
using AElf.Standards.ACS3;
using AElf.Contracts.Association;
using AElf.Contracts.Configuration;
using AElf.Contracts.MultiToken;
using AElf.Contracts.Profit;
using AElf.Contracts.TestContract.TransactionFees;
using AElf.Contracts.TokenConverter;
using AElf.CSharp.Core;
using AElf.CSharp.Core.Extension;
using AElf.Standards.ACS12;
using AElf.Types;
using AElfChain.Common;
using AElfChain.Common.Contracts;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using log4net;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Shouldly;
using Volo.Abp.Threading;
using TokenInfo = AElf.Contracts.MultiToken.TokenInfo;
using TransferInput = AElf.Contracts.MultiToken.TransferInput;

namespace AElf.Automation.Contracts.ScenarioTest
{
    [TestClass]
    public class TransactionFeeTest
    {
        private ILog Logger { get; set; }
        private INodeManager NodeManager { get; set; }
        private INodeManager SideNodeManager { get; set; }
        private AuthorityManager AuthorityManager { get; set; }
        private AuthorityManager SideAuthority { get; set; }
        private ContractManager ContractManager { get; set; }
        private ContractManager SideContractManager { get; set; }

        private TokenContract _tokenContract;
        private TokenContract _sideTokenContract;

        private TokenConverterContract _tokenConverter;
        private ParliamentContract _parliament;
        private ParliamentContract _sideParliament;

        private GenesisContract _genesisContract;
        private TreasuryContract _treasury;
        private ProfitContract _profit;

        private TransactionFeesContract _acs8ContractA;
        private TransactionFeesContract _acs8ContractB;
        private TransactionFeesContractContainer.TransactionFeesContractStub _acs8SubA;
        private TransactionFeesContractContainer.TransactionFeesContractStub _acs8SubB;
        private TokenConverterContractContainer.TokenConverterContractStub _tokenConverterSub;
        private TokenContractImplContainer.TokenContractImplStub _tokenContractImpl;
        private TokenContractImplContainer.TokenContractImplStub _sideTokenContractImpl;

        private BasicFunctionContract _basicFunctionContract;

        //aFm1FWZRLt7V6wCBUGVmqxaDcJGv9HvYPDUVxF95C9L7sTwXp
        //NUddzDNy8PBMUgPCAcFW7jkaGmofDTEmr5DUoddXDpdR6E85X
        //V6LuP6FXKPoXqR5V9X2XuufZ8wSwKu4kNJbxY8i9JJ4NDPxib
        //zptx91dhHVJjJRxf5Wg5KAoMrDrWX6i1H2FAyKAiv2q8VZfbg
        private Dictionary<SchemeType, Scheme> Schemes { get; set; }
        private string InitAccount { get; } = "zptx91dhHVJjJRxf5Wg5KAoMrDrWX6i1H2FAyKAiv2q8VZfbg";
        private string Delegatee1 { get; } = "2gmjRSxgrQJznCHU3cBRgTMbD61zRjMPvSGYB4YNxumkKf6rm7";
        private string Delegatee2 { get; } = "zkWrJiNT8B4af6auBzn3WuhNrd3zHtmercyQ4sar7GxM8Xwy9";
        private string Delegatee3 { get; } = "2bs2uYMECtHWjB57RqgqQ3X2LrxgptWHtzCqGEU11y45aWimh4";
        private string Delegatee4 { get; } = "2HxX36oXZS89Jvz7kCeUyuWWDXLTiNRkAzfx3EuXq4KSSkH62W";

        private string TestAccount { get; } = "zptx91dhHVJjJRxf5Wg5KAoMrDrWX6i1H2FAyKAiv2q8VZfbg";

        private string Delegator1 { get; } = "5xaM3r5okUL48KFzSnb3iCtR6kaCKy1zsRx5oRNkEULKWaQGB";
        private string Delegator2 { get; } = "aFm1FWZRLt7V6wCBUGVmqxaDcJGv9HvYPDUVxF95C9L7sTwXp";
        private string Delegator3 { get; } = "2pmw7ZpB8yxL4ifKU8uH233b6bwE3wnhGb6BXxvFWSuiFd7v1G";
        private string Delegator4 { get; } = "2EyLTpDMvfkcBga6EavVZ5mcbCiWR3PtxSPpFrnWWxJ4SwEeAY";

        // private static string RpcUrl { get; } = "54.199.254.157:8000";
        private static string RpcUrl { get; } = "127.0.0.1:8001";
        // private static string SideRpcUrl { get; } = "35.77.60.71:8000";
        private static string SideRpcUrl { get; } = "192.168.66.100:8000";


        private string Symbol { get; } = "TEST";
        private long SymbolFee = 10_00000000;
        private bool isNeedSide = false;
        private List<string> _accountList = new List<string>();
        private List<string> _resourceSymbol = new List<string>
            { "READ", "WRITE", "STORAGE", "TRAFFIC" };

        [TestInitialize]
        public void Initialize()
        {
            Log4NetHelper.LogInit("TransactionFeeTest");
            Logger = Log4NetHelper.GetLogger();
            NodeInfoHelper.SetConfig("single");

            if (isNeedSide)
            {
                SideNodeManager = new NodeManager(SideRpcUrl);
                SideContractManager = new ContractManager(SideNodeManager, InitAccount);
                SideAuthority = new AuthorityManager(SideNodeManager, InitAccount);
                _sideTokenContractImpl = SideContractManager.TokenImplStub;
                // _acs8ContractB = new TransactionFeesContract(SideNodeManager, InitAccount);
                _sideTokenContractImpl = SideContractManager.TokenImplStub;
                _sideParliament = SideContractManager.Parliament;
                _sideTokenContract = SideContractManager.Token;
            }

            NodeManager = new NodeManager(RpcUrl);
            ContractManager = new ContractManager(NodeManager, InitAccount);
            AuthorityManager = new AuthorityManager(NodeManager, InitAccount);

            _genesisContract = GenesisContract.GetGenesisContract(NodeManager, InitAccount);
            _tokenContract = _genesisContract.GetTokenContract(InitAccount);
            _tokenConverter = _genesisContract.GetTokenConverterContract(InitAccount);
            _parliament = _genesisContract.GetParliamentContract(InitAccount);
            _tokenContractImpl = _genesisContract.GetTokenImplStub();

            _treasury = _genesisContract.GetTreasuryContract(InitAccount);
            _profit = _genesisContract.GetProfitContract(InitAccount);
            _profit.GetTreasurySchemes(_treasury.ContractAddress);
            Schemes = ProfitContract.Schemes;

            _tokenConverterSub = _genesisContract.GetTokenConverterStub(InitAccount);
            _basicFunctionContract = new BasicFunctionContract(NodeManager, InitAccount, "2LUmicHyH4RXrMjG4beDwuDsiWJESyLkgkwPdGTR8kahRzq5XS");
            // _acs8ContractA = new TransactionFeesContract(SideNodeManager, InitAccount);
            // _sideTokenContractImpl = SideContractManager.TokenImplStub;
           // TransferFewResource();
            // InitializeFeesContract(_acs8ContractA);
           // InitializeFeesContract(_acs8ContractB);
            // _acs8SubA =
            // _acs8ContractA.GetTestStub<TransactionFeesContractContainer.TransactionFeesContractStub>(InitAccount);
            // _acs8SubB =
                // _acs8ContractB.GetTestStub<TransactionFeesContractContainer.TransactionFeesContractStub>(InitAccount);
            // CreateAndIssueToken(100000_00000000, Symbol);
            // CreateAndIssueToken(100000_00000000, "ABC");
            // CreateAndIssueToken(100000_00000000, "USDT");
        }

        #region ACS8

        [TestMethod]
        public async Task Acs8ContractTest()
        {
            var fees = new Dictionary<string, long>();

            await AdvanceResourceToken();
            var treasuryAmount = _treasury.GetCurrentTreasuryBalance();
            Logger.Info($"treasury  balance : {treasuryAmount}");
            var cpuResult = await _acs8SubA.ReadCpuCountTest.SendAsync(new Int32Value { Value = 20 });
            var afterTreasuryAmount = _treasury.GetCurrentTreasuryBalance();
            Logger.Info($"After treasury  balance : {afterTreasuryAmount}");

            cpuResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var baseFee = TransactionFeeCharged.Parser.ParseFrom(cpuResult.TransactionResult.Logs
                .First(l => l.Name.Equals(nameof(TransactionFeeCharged))).NonIndexed);
            var sizeFee = TransactionFeeCharged.Parser.ParseFrom(cpuResult.TransactionResult.Logs
                .Last(l => l.Name.Equals(nameof(TransactionFeeCharged))).NonIndexed);
            fees.Add(baseFee.Symbol, baseFee.Amount);
            fees.Add(sizeFee.Symbol, sizeFee.Amount);

            var resourceFeeList = cpuResult.TransactionResult.Logs
                .Where(l => l.Name.Equals(nameof(ResourceTokenCharged))).ToList();
            foreach (var resourceFee in resourceFeeList)
            {
                var feeAmount = ResourceTokenCharged.Parser.ParseFrom(resourceFee.NonIndexed).Amount;
                var feeSymbol = ResourceTokenCharged.Parser.ParseFrom(resourceFee.NonIndexed).Symbol;
                fees.Add(feeSymbol, feeAmount);
            }

            foreach (var fee in fees)
            {
                Logger.Info($"transaction fee {fee.Key} is : {fee.Value}");
            }
        }

        [TestMethod]
        public async Task Acs8ContractTestOnSide()
        {
            var fees = new Dictionary<string, long>();

            await AdvanceResourceTokenOnSideChain();
            await CheckSideDividends();
//            var feeReceiver = await SideContractManager.TokenImplStub.GetFeeReceiver.CallAsync(new Empty());
//            foreach (var symbol in _resourceSymbol)
//            {
//                var balance = SideContractManager.Token.GetUserBalance(feeReceiver.ToBase58(), symbol);
//                Logger.Info($"fee receiver {symbol} balance : {balance}");
//            }

            foreach (var symbol in _resourceSymbol)
            {
                var balance =
                    SideContractManager.Token.GetUserBalance(SideContractManager.Consensus.ContractAddress, symbol);
                Logger.Info($"fee consensus {symbol} balance : {balance}");
            }

            var consensusStub = SideContractManager.Genesis.GetConsensusImplStub(InitAccount);
            var unAmount = await consensusStub.GetUndistributedDividends.CallAsync(new Empty());
            Logger.Info($"Symbol amount:{unAmount}");

            var genesis = GenesisContract.GetGenesisContract(SideNodeManager, InitAccount);
            var token = genesis.GetTokenContract();
            token.TransferBalance(InitAccount, TestAccount, 2000_00000000, "TEST");
            token.TransferBalance(InitAccount, TestAccount, 1_00000000);

            var cpuResult = await _acs8SubA.ReadCpuCountTest.SendAsync(new Int32Value { Value = 19 });
            var size = cpuResult.Transaction.CalculateSize();
            Logger.Info(size);

//            foreach (var symbol in _resourceSymbol)
//            {
//                var balance = SideContractManager.Token.GetUserBalance(feeReceiver.ToBase58(), symbol);
//                Logger.Info($"After fee receiver {symbol} balance : {balance}");
//            }

            foreach (var symbol in _resourceSymbol)
            {
                var balance =
                    SideContractManager.Token.GetUserBalance(SideContractManager.Consensus.ContractAddress, symbol);
                Logger.Info($"After consensus {symbol} balance : {balance}");
            }

            consensusStub = SideContractManager.Genesis.GetConsensusImplStub(InitAccount);
            unAmount = await consensusStub.GetUndistributedDividends.CallAsync(new Empty());
            Logger.Info($"Symbol amount:{unAmount}");

            cpuResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var baseFee = TransactionFeeCharged.Parser.ParseFrom(cpuResult.TransactionResult.Logs
                .First(l => l.Name.Equals(nameof(TransactionFeeCharged))).NonIndexed);
            var sizeFee = TransactionFeeCharged.Parser.ParseFrom(cpuResult.TransactionResult.Logs
                .Last(l => l.Name.Equals(nameof(TransactionFeeCharged))).NonIndexed);
            fees.Add(baseFee.Symbol, baseFee.Amount);
            fees.Add(sizeFee.Symbol, sizeFee.Amount);

            var resourceFeeList = cpuResult.TransactionResult.Logs
                .Where(l => l.Name.Equals(nameof(ResourceTokenCharged))).ToList();

            foreach (var resourceFee in resourceFeeList)
            {
                var feeAmount = ResourceTokenCharged.Parser.ParseFrom(resourceFee.NonIndexed).Amount;
                var feeSymbol = ResourceTokenCharged.Parser.ParseFrom(resourceFee.NonIndexed).Symbol;
                fees.Add(feeSymbol, feeAmount);
            }

            foreach (var fee in fees)
            {
                Logger.Info($"transaction fee {fee.Key} is : {fee.Value}");
            }

            await CheckSideDividends();
        }

        [TestMethod]
        public async Task Acs8ContractTest_Owned()
        {
            TransferFewResource();
            var fees = new Dictionary<string, long>();
            var treasuryAmount = _treasury.GetCurrentTreasuryBalance();
            Logger.Info($"treasury  balance : {treasuryAmount}");
            var cpuResult = await _acs8SubA.ReadCpuCountTest.SendAsync(new Int32Value { Value = 20 });

            var afterTreasuryAmount = _treasury.GetCurrentTreasuryBalance();
            Logger.Info($"After treasury  balance : {afterTreasuryAmount}");

            cpuResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var elfFee = TransactionFeeCharged.Parser.ParseFrom(cpuResult.TransactionResult.Logs
                .First(l => l.Name.Equals(nameof(TransactionFeeCharged))).NonIndexed);
            fees.Add(elfFee.Symbol, elfFee.Amount);
            var resourceFeeList = cpuResult.TransactionResult.Logs
                .Where(l => l.Name.Equals(nameof(ResourceTokenCharged))).ToList();
            foreach (var resourceFee in resourceFeeList)
            {
                var feeAmount = ResourceTokenCharged.Parser.ParseFrom(resourceFee.NonIndexed).Amount;
                var feeSymbol = ResourceTokenCharged.Parser.ParseFrom(resourceFee.NonIndexed).Symbol;
                fees.Add(feeSymbol, feeAmount);
            }

            foreach (var fee in fees)
            {
                Logger.Info($"transaction fee {fee.Key} is : {fee.Value}");
            }

            var owedFees = new Dictionary<string, long>();
            var resourceTokenOwnedList = cpuResult.TransactionResult.Logs
                .Where(l => l.Name.Equals(nameof(ResourceTokenOwned))).ToList();
            if (!resourceTokenOwnedList.Count.Equals(0))
            {
                foreach (var resourceOwned in resourceTokenOwnedList)
                {
                    var feeAmount = ResourceTokenOwned.Parser.ParseFrom(resourceOwned.NonIndexed).Amount;
                    var feeSymbol = ResourceTokenOwned.Parser.ParseFrom(resourceOwned.NonIndexed).Symbol;
                    owedFees.Add(feeSymbol, feeAmount);
                }

                foreach (var fee in owedFees)
                {
                    Logger.Info($"transaction owed fee {fee.Key} is : {fee.Value}");
                }
            }

            foreach (var symbol in _resourceSymbol)
            {
                var balance = _tokenContract.GetUserBalance(_acs8ContractA.ContractAddress, symbol);
                // balance.ShouldBe(0);
                Logger.Info($"Contract {symbol} balance : {balance}");
            }
        }

        [TestMethod]
        public void Acs8ContractTest_Owning()
        {
            var transactionList = new List<string>();
            for (int i = 0; i < 30; i++)
            {
                var cpuResult =
                    _acs8ContractA.ExecuteMethodWithTxId(TxFeesMethod.ReadCpuCountTest,
                        new Int32Value { Value = (i + 20) });
                transactionList.Add(cpuResult);
            }

            NodeManager.CheckTransactionListResult(transactionList);
        }

        [TestMethod]
        public async Task TakeResourceTokenBack()
        {
            var balance = _tokenContract.GetUserBalance(_acs8ContractA.ContractAddress, "READ");
            Logger.Info($"Contract A READ balance : {balance}");

//            _tokenContract.TransferBalance(InitAccount, TestAccount, 1000_00000000);
            var other = _genesisContract.GetTokenImplStub(InitAccount);
            var takeBack = await other.TakeResourceTokenBack.SendAsync(new TakeResourceTokenBackInput
            {
                ContractAddress = _acs8ContractA.Contract,
                ResourceTokenSymbol = "READ",
                Amount = 10_00000000
            });
            takeBack.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var afterBalance = _tokenContract.GetUserBalance(_acs8ContractA.ContractAddress, "READ");
            afterBalance.ShouldBe(balance - 10_00000000);
        }

        #endregion

        #region ACS1

        [TestMethod]
        public void GetTokenContractMethodFee()
        {
            var fee = _tokenContract.CallViewMethod<MethodFees>(TokenMethod.GetMethodFee, new StringValue
            {
                Value = nameof(TokenMethod.Transfer)
            });
            Logger.Info(fee);
        }

        [TestMethod]
        public void SetTokenContractMethodFee()
        {
            var symbol = Symbol;
            var fee = _tokenContract.CallViewMethod<MethodFees>(TokenMethod.GetMethodFee, new StringValue
            {
                Value = nameof(TokenMethod.Transfer)
            });
            Logger.Info(fee);
//            if (fee.Fees.Count > 0) return;
            var organization =
                _tokenContract.CallViewMethod<AuthorityInfo>(TokenMethod.GetMethodFeeController, new Empty())
                    .OwnerAddress;
            var input = new MethodFees
            {
                MethodName = nameof(TokenMethod.Transfer),
                Fees =
                {
                    new MethodFee
                    {
                        BasicFee = SymbolFee,
                        Symbol = symbol
                    }
                },
                IsSizeFeeFree = false
            };
            var result = AuthorityManager.ExecuteTransactionWithAuthority(_tokenContract.ContractAddress,
                "SetMethodFee", input,
                InitAccount, organization);
            result.Status.ShouldBe(TransactionResultStatus.Mined);
            fee = _tokenContract.CallViewMethod<MethodFees>(TokenMethod.GetMethodFee, new StringValue
            {
                Value = nameof(TokenMethod.Transfer)
            });
            Logger.Info(fee);
        }

        [TestMethod]
        public void SetFeeContractMethodFee()
        {
            var symbol = "TEST";
            var fee = _acs8ContractA.CallViewMethod<MethodFees>(TxFeesMethod.GetMethodFee, new StringValue
            {
                Value = nameof(TxFeesMethod.ReadCpuCountTest)
            });
            Logger.Info(fee);
            if (fee.Fees.Count > 0) return;

            var input = new MethodFees
            {
                MethodName = nameof(TxFeesMethod.ReadCpuCountTest),
                Fees =
                {
                    new MethodFee
                    {
                        BasicFee = 0,
                        Symbol = symbol
                    }
                },
                IsSizeFeeFree = true
            };
            var result = _acs8ContractA.ExecuteMethodWithResult(TxFeesMethod.SetMethodFee, input);
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
        }

        [TestMethod]
        public void CheckFee()
        {
//            Transfer method
            var result = _tokenContract.TransferBalance(InitAccount, TestAccount, 10000000000, "TEST");
            var fee = result.GetDefaultTransactionFee();
            var eventLogs = result.Logs;
            var baseFee = TransactionFeeCharged.Parser.ParseFrom(
                ByteString.FromBase64(eventLogs.First(n => n.Name.Equals(nameof(TransactionFeeCharged))).NonIndexed));
            var symbol = baseFee.Symbol;
            symbol.ShouldBe("TEST");
        }

        [TestMethod]
        public async Task Acs1ContractTest()
        {
            var feeSymbol = "TEST";
            GetMethodFee();
            _tokenContract.TransferBalance(InitAccount, TestAccount, 100000000, feeSymbol);
            _tokenContract.TransferBalance(InitAccount, TestAccount, 100_00000000, "ELF");

            var balance = _tokenContract.GetUserBalance(TestAccount, Symbol);

            var test = _genesisContract.GetTokenStub(TestAccount);
            var result = await test.Approve.SendAsync(new ApproveInput
            {
                Spender = _tokenContract.Contract,
                Symbol = Symbol,
                Amount = 1000
            });
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var elfFees = result.TransactionResult.Logs
                .Where(l => l.Name.Equals(nameof(TransactionFeeCharged))).ToList();
            foreach (var elfFee in elfFees)
            {
                var fee = TransactionFeeCharged.Parser.ParseFrom(elfFee.NonIndexed);
                var symbol = fee.Symbol;
                var amount = fee.Amount;
                if (symbol == Symbol)
                    amount.ShouldBe(SymbolFee);
            }

            var afterBalance = _tokenContract.GetUserBalance(InitAccount, Symbol);
            afterBalance.ShouldBe(balance - SymbolFee);
        }

        #endregion

        #region ACS12

        [TestMethod]
        public void SetAcs12MethodFee()
        {
            var configuration = _genesisContract.GetConfigurationContract(InitAccount);
            var transactionFee = new UserContractMethodFees
            {
                Fees =
                {
                    new UserContractMethodFee
                    {
                        Symbol = "ELF", 
                        BasicFee = 100000000
                    }
                },
                IsSizeFeeFree = false
            };
            var setConfigurationResult = AuthorityManager.ExecuteTransactionWithAuthority(
                ContractManager.Configuration.ContractAddress, nameof(ConfigurationMethod.SetConfiguration),
                new SetConfigurationInput
                {
                    Key = nameof(ConfigurationNameProvider.UserContractMethodFee),
                    Value = transactionFee.ToByteString()
                }, ContractManager.CallAddress);
            setConfigurationResult.Status.ShouldBe(TransactionResultStatus.Mined);
            
            var methodFees = configuration.CallViewMethod<UserContractMethodFees>(ConfigurationMethod.GetConfiguration, new StringValue
                {Value = nameof(ConfigurationNameProvider.UserContractMethodFee)});
           Logger.Info(methodFees);
        }

        #endregion

        #region Set Free Allowance

        // ConfigMethodFeeFreeAllowances(MethodFeeFreeAllowancesConfig)
        // GetMethodFeeFreeAllowancesConfig MethodFeeFreeAllowancesConfig
        // GetMethodFeeFreeAllowances(address) MethodFeeFreeAllowances
        [TestMethod]
        public void SetFreeAllowance()
        {
            var threshold = 100_00000000;
            var symbol = "ELF";
            var freeAmount = 500000000;
            var organization = _parliament.GetGenesisOwnerAddress();

            var input = new MethodFeeFreeAllowancesConfig
            {
                FreeAllowances = new MethodFeeFreeAllowances
                {
                    Value =
                    {
                        new MethodFeeFreeAllowance
                        {
                            Symbol = symbol,
                            Amount = freeAmount
                        }
                    }
                },
                RefreshSeconds = 86400,
                Threshold = threshold
            };

            var result = AuthorityManager.ExecuteTransactionWithAuthority(_tokenContract.ContractAddress,
            "ConfigMethodFeeFreeAllowances", input,
            InitAccount, organization);
            result.Status.ShouldBe(TransactionResultStatus.Mined);
            
            // var proposalId = _parliament.CreateProposal(_tokenContract.ContractAddress,
            //     "ConfigMethodFeeFreeAllowances", input,
            //     organization, InitAccount);
            // Logger.Info(proposalId.ToHex());

            var config = _tokenContract.GetMethodFeeFreeAllowancesConfig();
            config.Threshold.ShouldBe(threshold);
            config.FreeAllowances.Value.First().Amount.ShouldBe(freeAmount);
            config.FreeAllowances.Value.First().Symbol.ShouldBe(symbol);
            config.RefreshSeconds.ShouldBe(300);
        }

        [TestMethod]
        public void FreeAllowance()
        {
            var newAccount = NodeManager.NewAccount("12345678");
            _tokenContract.TransferBalance(InitAccount, newAccount, 100_00000000);
            _tokenContract.TransferBalance(InitAccount, newAccount, 1000_00000000,"ABC");
            var elfBalance = _tokenContract.GetUserBalance(newAccount);
            var testBalance = _tokenContract.GetUserBalance(newAccount, "TEST");
            var abcBalance = _tokenContract.GetUserBalance(newAccount, "ABC");

            var beforeFreeAllowance = _tokenContract.GetMethodFeeFreeAllowances(newAccount);
            Logger.Info(beforeFreeAllowance);
            var result = _tokenContract.TransferBalance(newAccount, TestAccount, 1000000000, "ABC");

            var eventLogs = result.Logs;
            if (eventLogs.Any(n => n.Name.Equals(nameof(TransactionFeeCharged))))
            {
                var charged = eventLogs.First(n => n.Name.Equals(nameof(TransactionFeeCharged)));
                var fee = TransactionFeeCharged.Parser.ParseFrom(
                    ByteString.FromBase64(charged.NonIndexed));
                Logger.Info($"fee: {fee}");
            }

            var afterFreeAllowance = _tokenContract.GetMethodFeeFreeAllowances(newAccount);
            Logger.Info(afterFreeAllowance);
            var afterElfBalance = _tokenContract.GetUserBalance(newAccount);
            var afterTestBalance = _tokenContract.GetUserBalance(newAccount, "TEST");
            var afterAbcBalance = _tokenContract.GetUserBalance(newAccount, "ABC");


            Logger.Info($"{elfBalance} {testBalance} {abcBalance}\n" +
                        $"{afterElfBalance} {afterTestBalance} {afterAbcBalance}");
        }

        [TestMethod]
        public void FreeAllowance_NotEnough()
        {
            var newAccount = NodeManager.NewAccount("12345678");
            _tokenContract.TransferBalance(InitAccount, newAccount, 100_00000000);
            var elfBalance = _tokenContract.GetUserBalance(newAccount);
            var beforeFreeAllowance = _tokenContract.GetMethodFeeFreeAllowances(newAccount);
            Logger.Info(beforeFreeAllowance);
            var result = _tokenConverter.Buy(newAccount, "RAM", 100000000);
            var eventLogs = result.Logs;
            if (eventLogs.Any(n => n.Name.Equals(nameof(TransactionFeeCharged))))
            {
                var charged = eventLogs.First(n => n.Name.Equals(nameof(TransactionFeeCharged)));
                var fee = TransactionFeeCharged.Parser.ParseFrom(
                    ByteString.FromBase64(charged.NonIndexed));
                Logger.Info($"fee: {fee}");
            }

            var afterFreeAllowance = _tokenContract.GetMethodFeeFreeAllowances(newAccount);
            Logger.Info(afterFreeAllowance);
            var afterElfBalance = _tokenContract.GetUserBalance(newAccount);

            Logger.Info($"balance: {elfBalance}\n" +
                        $"{afterElfBalance}");
            
            var buy = _tokenConverter.Buy(newAccount, "CPU", 100000000);
            var buyEventLogs = buy.Logs;
            if (buyEventLogs.Any(n => n.Name.Equals(nameof(TransactionFeeCharged))))
            {
                var charged = buyEventLogs.First(n => n.Name.Equals(nameof(TransactionFeeCharged)));
                var fee = TransactionFeeCharged.Parser.ParseFrom(
                        ByteString.FromBase64(charged.NonIndexed));
                    Logger.Info($"fee: {fee}");
            }
            afterFreeAllowance = _tokenContract.GetMethodFeeFreeAllowances(newAccount);
            Logger.Info(afterFreeAllowance);
        }
        
        [TestMethod]
        public void FreeAllowance_notEnough()
        {
            var newAccount = NodeManager.NewAccount("12345678");
            _tokenContract.TransferBalance(InitAccount, newAccount, 10_00000000);
            _tokenContract.TransferBalance(InitAccount, newAccount, 1000_00000000, "TEST");
            var elfBalance = _tokenContract.GetUserBalance(newAccount);
            var testBalance = _tokenContract.GetUserBalance(newAccount, "TEST");
            var abcBalance = _tokenContract.GetUserBalance(newAccount, "ABC");

            // var beforeFreeAllowance = _tokenContract.GetMethodFeeFreeAllowances(newAccount);
            // Logger.Info(beforeFreeAllowance);

            var result = _tokenContract.TransferBalance(newAccount, TestAccount, 1_00000000, "TEST");
            var eventLogs = result.Logs;
            if (eventLogs.Any(n => n.Name.Equals(nameof(TransactionFeeCharged))))
            {
                var charged = eventLogs.Where(n => n.Name.Equals(nameof(TransactionFeeCharged)));
                foreach (var c in charged)
                {
                    var fee = TransactionFeeCharged.Parser.ParseFrom(
                        ByteString.FromBase64(c.NonIndexed));
                    Logger.Info(fee);
                }
            }

            var afterFreeAllowance = _tokenContract.GetMethodFeeFreeAllowances(newAccount);
            Logger.Info(afterFreeAllowance);
            var afterElfBalance = _tokenContract.GetUserBalance(newAccount);
            var afterTestBalance = _tokenContract.GetUserBalance(newAccount, "TEST");
            var afterAbcBalance = _tokenContract.GetUserBalance(newAccount, "ABC");

            Logger.Info($"{elfBalance} {testBalance} {abcBalance}\n" +
                        $"{afterElfBalance} {afterTestBalance} {afterAbcBalance}");
        }
        
        [TestMethod]
        public void FreeAllowance_CheckFreeAllowance()
        {
            var account = "";
            var elfBalance = _tokenContract.GetUserBalance(account);
            Logger.Info(elfBalance);

            var beforeFreeAllowance = _tokenContract.GetMethodFeeFreeAllowances(account);
            Logger.Info(beforeFreeAllowance);
        }

        #endregion

        #region Set Delegator

        //SetTransactionFeeDelegations
        //GetTransactionFeeDelegationsOfADelegatee
        //RemoveTransactionFeeDelegator
        //RemoveTransactionFeeDelegatee

        [TestMethod]
        public void SetTransactionFeeDelegations_Add()
        {
            var symbol = "ELF";
            var amount = 10_00000000;
            var delegator = Delegatee1;
            var delegatee = Delegatee2;
            _tokenContract.TransferBalance(InitAccount, delegatee, amount, symbol);
            var originDelegations = _tokenContract.GetTransactionFeeDelegationsOfADelegatee(delegator, delegatee);
            Logger.Info(originDelegations);
            var delegations = new Dictionary<string, long>
            {
                [symbol] = amount
            };
            var input = new SetTransactionFeeDelegationsInput()
            {
                DelegatorAddress = delegator.ConvertAddress(),
                Delegations =
                {
                    delegations
                }
            };
            _tokenContract.SetAccount(delegatee);
            var result = _tokenContract.ExecuteMethodWithResult(TokenMethod.SetTransactionFeeDelegations, input);
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var height = result.BlockNumber;
            var getDelegations = _tokenContract.GetTransactionFeeDelegationsOfADelegatee(delegator, delegatee);
            Logger.Info(getDelegations);
            getDelegations.BlockHeight.ShouldBe(!originDelegations.Equals(new TransactionFeeDelegations())
                ? originDelegations.BlockHeight
                : height);

            if (originDelegations.Equals(new TransactionFeeDelegations()))
            {
                var logs = result.Logs.First(l => l.Name.Equals("TransactionFeeDelegationAdded")).Indexed;
                var logDelegator = TransactionFeeDelegationCancelled.Parser.ParseFrom(ByteString.FromBase64(logs[0]))
                    .Delegator;
                logDelegator.ShouldBe(delegator.ConvertAddress());
                var logDelegatee = TransactionFeeDelegationCancelled.Parser.ParseFrom(ByteString.FromBase64(logs[1]))
                    .Delegatee;
                logDelegatee.ShouldBe(delegatee.ConvertAddress());
                var caller = TransactionFeeDelegationCancelled.Parser.ParseFrom(ByteString.FromBase64(logs[2])).Caller;
                caller.ShouldBe(delegatee.ConvertAddress());
                caller.ShouldBe(result.Transaction.From.ConvertAddress());
            }
            else
            {
                var logs = result.Logs.Any(l => l.Name.Equals("TransactionFeeDelegationAdded"));
                logs.ShouldBe(false);
                getDelegations.Delegations.Count.ShouldBe(originDelegations.Delegations.Keys.Contains(symbol)
                    ? originDelegations.Delegations.Count
                    : originDelegations.Delegations.Count.Add(1));
            }

            getDelegations.Delegations[symbol].ShouldBe(amount);
            BoolValue.Parser.ParseFrom(ByteArrayHelper.HexStringToByteArray(result.ReturnValue)).Value.ShouldBeTrue();
        }

        [TestMethod]
        public void SetTransactionFeeDelegations_MultiToke()
        {
            var symbol1 = "ABC";
            var amount1 = 200000000;
            var symbol2 = "TEST";
            var amount2 = 0;
            var delegator = Delegator1;
            var delegatee = Delegatee1;

            var originDelegations = _tokenContract.GetTransactionFeeDelegationsOfADelegatee(delegator, delegatee);
            Logger.Info(originDelegations);

            var delegations = new Dictionary<string, long>
            {
                [symbol1] = amount1,
                [symbol2] = amount2
            };
            var input = new SetTransactionFeeDelegationsInput()
            {
                DelegatorAddress = delegator.ConvertAddress(),
                Delegations =
                {
                    delegations
                }
            };
            _tokenContract.SetAccount(delegatee);
            var result = _tokenContract.ExecuteMethodWithResult(TokenMethod.SetTransactionFeeDelegations, input);
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var getDelegations = _tokenContract.GetTransactionFeeDelegationsOfADelegatee(delegator, delegatee);
            Logger.Info(getDelegations);
            if (originDelegations.Equals(new TransactionFeeDelegations()))
            {
                var logs = result.Logs.First(l => l.Name.Equals("TransactionFeeDelegationAdded")).Indexed;
                var logDelegator = TransactionFeeDelegationCancelled.Parser.ParseFrom(ByteString.FromBase64(logs[1]))
                    .Delegator;
                logDelegator.ShouldBe(delegator.ConvertAddress());
                var logDelegatee = TransactionFeeDelegationCancelled.Parser.ParseFrom(ByteString.FromBase64(logs[1]))
                    .Delegatee;
                logDelegatee.ShouldBe(delegatee.ConvertAddress());
                var caller = TransactionFeeDelegationCancelled.Parser.ParseFrom(ByteString.FromBase64(logs[1])).Caller;
                caller.ShouldBe(delegatee.ConvertAddress());
                caller.ShouldBe(result.Transaction.From.ConvertAddress());

                getDelegations.Delegations.Count.ShouldBe(delegatee.Length);
            }
            else
            {
                var logs = result.Logs.Any(l => l.Name.Equals("TransactionFeeDelegationAdded"));
                logs.ShouldBe(false);
            }

            if (amount1 > 0)
                getDelegations.Delegations[symbol1].ShouldBe(amount1);
            else
                getDelegations.Delegations.Keys.ShouldNotContain(symbol1);

            if (amount2 > 0)
                getDelegations.Delegations[symbol2].ShouldBe(amount2);
            else
                getDelegations.Delegations.Keys.ShouldNotContain(symbol2);

            BoolValue.Parser.ParseFrom(ByteArrayHelper.HexStringToByteArray(result.ReturnValue)).Value.ShouldBeTrue();
        }

        [TestMethod]
        public void SetTransactionFeeDelegations_Remove()
        {
            var symbol = "ELF";
            // var amount = 100000000;
            var delegator = Delegator1;
            var delegatee = InitAccount;

            var originDelegations = _tokenContract.GetTransactionFeeDelegationsOfADelegatee(delegator, delegatee);
            Logger.Info(originDelegations);

            var delegations = new Dictionary<string, long>
            {
                [symbol] = 0
            };
            var input = new SetTransactionFeeDelegationsInput()
            {
                DelegatorAddress = delegator.ConvertAddress(),
                Delegations =
                {
                    delegations
                }
            };
            _tokenContract.SetAccount(delegatee);
            var result = _tokenContract.ExecuteMethodWithResult(TokenMethod.SetTransactionFeeDelegations, input);
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            var getDelegations = _tokenContract.GetTransactionFeeDelegationsOfADelegatee(delegator, delegatee);
            Logger.Info(getDelegations);
            if (originDelegations.Delegations.Count == 1)
            {
                getDelegations.ShouldBe(new TransactionFeeDelegations());
                var logs = result.Logs.First(l => l.Name.Equals("TransactionFeeDelegationCancelled")).Indexed;
                var logDelegator = TransactionFeeDelegationCancelled.Parser.ParseFrom(ByteString.FromBase64(logs[0]))
                    .Delegator;
                logDelegator.ShouldBe(delegator.ConvertAddress());
                var logDelegatee = TransactionFeeDelegationCancelled.Parser.ParseFrom(ByteString.FromBase64(logs[1]))
                    .Delegatee;
                logDelegatee.ShouldBe(delegatee.ConvertAddress());
                var caller = TransactionFeeDelegationCancelled.Parser.ParseFrom(ByteString.FromBase64(logs[2])).Caller;
                caller.ShouldBe(result.Transaction.From.ConvertAddress());
            }
            else
            {
                getDelegations.Delegations.Keys.ShouldNotContain(symbol);
                var logs = result.Logs.Any(l => l.Name.Equals("TransactionFeeDelegationCancelled"));
                logs.ShouldBe(false);
            }

            BoolValue.Parser.ParseFrom(ByteArrayHelper.HexStringToByteArray(result.ReturnValue)).Value.ShouldBeTrue();
        }

        [TestMethod]
        public void SetTransactionFeeDelegations_Failed()
        {
            {
                var symbol = "AAA";
                var amount = 100000000;
                var delegator = Delegator1;
                var delegatee = Delegatee1;

                var originDelegations = _tokenContract.GetTransactionFeeDelegationsOfADelegatee(delegator, delegatee);
                Logger.Info(originDelegations);

                var delegations = new Dictionary<string, long>
                {
                    [symbol] = amount
                };
                var input = new SetTransactionFeeDelegationsInput()
                {
                    DelegatorAddress = delegator.ConvertAddress(),
                    Delegations =
                    {
                        delegations
                    }
                };
                _tokenContract.SetAccount(delegatee);
                var result = _tokenContract.ExecuteMethodWithResult(TokenMethod.SetTransactionFeeDelegations, input);
                result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Failed);
                result.Error.ShouldContain("Token is not found.");
            }
            {
                var symbol = "ABC";
                var amount = 0;
                var delegator = Delegator2;
                var delegatee = Delegatee1;

                var originDelegations = _tokenContract.GetTransactionFeeDelegationsOfADelegatee(delegator, delegatee);
                Logger.Info(originDelegations);

                var delegations = new Dictionary<string, long>
                {
                    [symbol] = amount
                };
                var input = new SetTransactionFeeDelegationsInput()
                {
                    DelegatorAddress = delegator.ConvertAddress(),
                    Delegations =
                    {
                        delegations
                    }
                };
                _tokenContract.SetAccount(delegatee);
                var result = _tokenContract.ExecuteMethodWithResult(TokenMethod.SetTransactionFeeDelegations, input);
                result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
                var getDelegations = _tokenContract.GetTransactionFeeDelegationsOfADelegatee(delegator, delegatee);
                Logger.Info(getDelegations);
                var logs = result.Logs.Any(l => l.Name.Equals("TransactionFeeDelegationAdded"));
                logs.ShouldBe(false);
                BoolValue.Parser.ParseFrom(ByteArrayHelper.HexStringToByteArray(result.ReturnValue)).Value
                    .ShouldBeTrue();
            }
        }

        [TestMethod]
        public void RemoveTransactionFeeDelegator()
        {
            var delegator = Delegatee1;
            var delegatee = Delegatee2;

            var originDelegations = _tokenContract.GetTransactionFeeDelegationsOfADelegatee(delegator, delegatee);
            Logger.Info(originDelegations);

            _tokenContract.SetAccount(delegatee);
            var result = _tokenContract.ExecuteMethodWithResult(TokenMethod.RemoveTransactionFeeDelegator,
                new RemoveTransactionFeeDelegatorInput
                {
                    DelegatorAddress = delegator.ConvertAddress()
                });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            if (originDelegations.Equals(new TransactionFeeDelegations()))
            {
                var logs = result.Logs.Any(l => l.Name.Equals("TransactionFeeDelegationCancelled"));
                logs.ShouldBeFalse();
                Logger.Info("return: Empty");
            }
            else
            {
                var logs = result.Logs.First(l => l.Name.Equals("TransactionFeeDelegationCancelled")).Indexed;
                var logDelegator = TransactionFeeDelegationCancelled.Parser.ParseFrom(ByteString.FromBase64(logs[0]))
                    .Delegator;
                logDelegator.ShouldBe(delegator.ConvertAddress());
                var logDelegatee = TransactionFeeDelegationCancelled.Parser.ParseFrom(ByteString.FromBase64(logs[1]))
                    .Delegatee;
                logDelegatee.ShouldBe(delegatee.ConvertAddress());
                var caller = TransactionFeeDelegationCancelled.Parser.ParseFrom(ByteString.FromBase64(logs[2])).Caller;
                caller.ShouldBe(delegatee.ConvertAddress());
                caller.ShouldBe(result.Transaction.From.ConvertAddress());
            }

            var getDelegations = _tokenContract.GetTransactionFeeDelegationsOfADelegatee(delegator, delegatee);
            Logger.Info(getDelegations);
            getDelegations.ShouldBe(new TransactionFeeDelegations());
        }

        [TestMethod]
        public void RemoveTransactionFeeDelegatee()
        {
            var delegator = Delegator1;
            var delegatee = Delegatee3;

            var originDelegations = _tokenContract.GetTransactionFeeDelegationsOfADelegatee(delegator, delegatee);
            Logger.Info(originDelegations);
            // _tokenContract.TransferBalance(InitAccount, delegator, 1000_00000000);
            // _tokenContract.SetAccount(delegator);
            var result = _tokenContract.ExecuteMethodWithResult(TokenMethod.RemoveTransactionFeeDelegatee,
                new RemoveTransactionFeeDelegateeInput
                {
                    DelegateeAddress = delegatee.ConvertAddress()
                });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            if (originDelegations.Equals(new TransactionFeeDelegations()))
            {
                var logs = result.Logs.Any(l => l.Name.Equals("TransactionFeeDelegationCancelled"));
                logs.ShouldBeFalse();
                Logger.Info("return: Empty");
            }
            else
            {
                var logs = result.Logs.First(l => l.Name.Equals("TransactionFeeDelegationCancelled")).Indexed;
                var logDelegator = TransactionFeeDelegationCancelled.Parser.ParseFrom(ByteString.FromBase64(logs[0]))
                    .Delegator;
                logDelegator.ShouldBe(delegator.ConvertAddress());
                var logDelegatee = TransactionFeeDelegationCancelled.Parser.ParseFrom(ByteString.FromBase64(logs[1]))
                    .Delegatee;
                logDelegatee.ShouldBe(delegatee.ConvertAddress());
                var caller = TransactionFeeDelegationCancelled.Parser.ParseFrom(ByteString.FromBase64(logs[2])).Caller;
                caller.ShouldBe(delegator.ConvertAddress());
                caller.ShouldBe(result.Transaction.From.ConvertAddress());
            }

            var getDelegations = _tokenContract.GetTransactionFeeDelegationsOfADelegatee(delegator, delegatee);
            Logger.Info(getDelegations);
            getDelegations.ShouldBe(new TransactionFeeDelegations());
        }

        [TestMethod]
        public void CheckDelegate()
        {
            var delegator = Delegator1;
            var delegatee = Delegatee1;

            var originDelegations = _tokenContract.GetTransactionFeeDelegationsOfADelegatee(delegator, delegatee);
            Logger.Info(originDelegations);
            var delegateeBalance = _tokenContract.GetUserBalance(delegatee);
            Logger.Info(delegateeBalance);
            var delegatorBalance = _tokenContract.GetUserBalance(delegator);
            Logger.Info(delegatorBalance);

            var delegatorTestBalance = _tokenContract.GetUserBalance(delegator, "TEST");
            Logger.Info(delegatorTestBalance);
        }

        [TestMethod]
        public void CheckLogs()
        {
            var logsList = new List<string>
            {
                "CiIKIAtCF6RAbm3KDRzE/U9YGMlsNAFczPNyCEzb6aSGBIzj",
                "EiIKIINM7z3nuxD8N+1luMW70yWChzS/YHPRhFLWRYRfyRpM",
                "GiIKIINM7z3nuxD8N+1luMW70yWChzS/YHPRhFLWRYRfyRpM"
            };

            var caller = TransactionFeeDelegationCancelled.Parser.ParseFrom(ByteString.FromBase64(logsList[0]));
            var logDelegatee = TransactionFeeDelegationCancelled.Parser.ParseFrom(ByteString.FromBase64(logsList[1]))
                .Delegatee;
            var logDelegator = TransactionFeeDelegationCancelled.Parser.ParseFrom(ByteString.FromBase64(logsList[2]))
                .Delegator;
            Logger.Info($"logDelegatee: {logDelegatee}\n logDelegator: {logDelegator}\n caller: {caller}");
        }

        [TestMethod]
        public void Transfer_Through_CA_Account_onlyELF()
        {
            var delegator = Delegator1;
            var delegatee = Delegatee1;
            var symbol = "ELF";
            var testSymbol = "ABC";
            CreateAndIssueToken(10000000000, testSymbol);

            _tokenContract.TransferBalance(InitAccount, delegator, 100000, testSymbol);
            var delegatorBalance = _tokenContract.GetUserBalance(delegator, symbol);
            var delegateeBalance = _tokenContract.GetUserBalance(delegatee, symbol);
            Logger.Info($"\ndelegator balance: {delegatorBalance}\n" +
                        $"delegatee(CA) balance: {delegateeBalance}");
            var originDelegations = _tokenContract.GetTransactionFeeDelegationsOfADelegatee(delegator, delegatee);
            Logger.Info(originDelegations);
            _tokenContract.SetAccount(delegator);
            var delegatorTransactionResult = _tokenContract.TransferBalance(delegator, TestAccount, 10000, testSymbol);
            delegatorTransactionResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var fee = delegatorTransactionResult.GetDefaultTransactionFee();
            Logger.Info(fee);

            var afterDelegations = _tokenContract.GetTransactionFeeDelegationsOfADelegatee(delegator, delegatee);
            Logger.Info(afterDelegations);

            var afterDelegatorBalance = _tokenContract.GetUserBalance(delegator, symbol);
            var afterDelegateeBalance = _tokenContract.GetUserBalance(delegatee, symbol);
            Logger.Info($"\nafter delegator balance: {afterDelegatorBalance}\n" +
                        $"after delegatee(CA) balance: {afterDelegateeBalance}");
            if (delegatorBalance < fee)
            {
                afterDelegations.Delegations[symbol].ShouldBe(originDelegations.Delegations[symbol].Sub(fee));
                afterDelegatorBalance.ShouldBe(delegatorBalance);
                afterDelegateeBalance.ShouldBe(delegateeBalance.Sub(fee));
            }
            else
            {
                afterDelegations.Delegations[symbol].ShouldBe(originDelegations.Delegations[symbol]);
                afterDelegatorBalance.ShouldBe(delegatorBalance.Sub(fee));
                afterDelegateeBalance.ShouldBe(delegateeBalance);
            }
        }

        [TestMethod]
        public void Transfer_Through_CA_Account_MultiCAAccount()
        {
            var delegator = Delegator1;
            var delegateeList = new List<string> { Delegatee1, Delegatee2 };
            var symbol = "ELF";
            var delegatorBalance = _tokenContract.GetUserBalance(delegator, symbol);
            Logger.Info($"\ndelegator balance: {delegatorBalance}\n");

            var delegateeListBalances = new List<long>();
            var afterDelegateeListBalance = new List<long>();
            var originDelegationsList = new List<TransactionFeeDelegations>();
            var afterDelegationsList = new List<TransactionFeeDelegations>();
            foreach (var delegatee in delegateeList)
            {
                var delegateeBalance = _tokenContract.GetUserBalance(delegatee, symbol);
                delegateeListBalances.Add(delegateeBalance);
                Logger.Info($"\ndelegatee(CA):{delegatee}\nbalance: {delegateeBalance}\n");
                var originDelegations = _tokenContract.GetTransactionFeeDelegationsOfADelegatee(delegator, delegatee);
                Logger.Info($"\n{originDelegations}");
                originDelegationsList.Add(originDelegations);
            }

            // _tokenContract.TransferBalance(InitAccount, delegator, 10000000);
            _tokenContract.SetAccount(delegator);
            var delegatorTransactionResult = _tokenContract.TransferBalance(delegator, TestAccount, 100000000, "DDD");
            delegatorTransactionResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var fee = delegatorTransactionResult.GetDefaultTransactionFee();
            Logger.Info(fee);

            var afterDelegatorBalance = _tokenContract.GetUserBalance(delegator, symbol);
            Logger.Info($"\nafter delegator balance: {afterDelegatorBalance}\n");
            foreach (var delegatee in delegateeList)
            {
                var afterDelegateeBalance = _tokenContract.GetUserBalance(delegatee, symbol);
                Logger.Info($"\nafter delegatee(CA):{delegatee}\nbalance: {afterDelegateeBalance}\n");
                afterDelegateeListBalance.Add(afterDelegateeBalance);
                var afterDelegations = _tokenContract.GetTransactionFeeDelegationsOfADelegatee(delegator, delegatee);
                Logger.Info(afterDelegations);
                afterDelegationsList.Add(afterDelegations);
            }

            if (delegatorBalance < fee)
            {
                if (!originDelegationsList.Any(l => l.Delegations[symbol] >= fee)) return;
                {
                    var useDelegations = originDelegationsList.FindIndex(l => l.Delegations[symbol] >= fee);
                    afterDelegationsList[useDelegations].Delegations[symbol]
                        .ShouldBe(originDelegationsList[useDelegations].Delegations[symbol].Sub(fee));
                    afterDelegatorBalance.ShouldBe(delegatorBalance);
                    afterDelegateeListBalance[useDelegations].ShouldBe(delegateeListBalances[useDelegations].Sub(fee));
                }
            }
            else
            {
                afterDelegationsList.ShouldBe(originDelegationsList);
                afterDelegatorBalance.ShouldBe(delegatorBalance.Sub(fee));
                afterDelegateeListBalance.ShouldBe(delegateeListBalances);
            }
        }

        [TestMethod]
        public void Transfer_Through_Same_CA_Account()
        {
            //{ "delegations": { "TEST": "10000000000", "ELF": "30000000" } }
            var delegator1 = Delegator1;
            var delegator2 = Delegator2;
            var delegatee = Delegatee1;
            var list = new List<string> { delegatee, delegator1, delegator2 };

            _tokenContract.TransferBalance(InitAccount, delegatee, 10000000000, "TEST");
            _tokenContract.TransferBalance(InitAccount, delegatee, 30000000, "ELF");
            foreach (var l in list)
            {
                _tokenContract.TransferBalance(InitAccount, l, 100000000, "DDD");

                var testBalance = _tokenContract.GetUserBalance(l, "TEST");
                var elfBalance = _tokenContract.GetUserBalance(l);
                Logger.Info($"{l} Test: {testBalance}\n Elf: {elfBalance}");
            }

            var input = new TransferInput
            {
                Amount = 1000000,
                To = TestAccount.ConvertAddress(),
                Symbol = "DDD"
            };

            _tokenContract.SetAccount(delegator1);
            var txId1 = _tokenContract.ExecuteMethodWithTxId(TokenMethod.Transfer, input);
            _tokenContract.SetAccount(delegator2);
            var txId2 = _tokenContract.ExecuteMethodWithTxId(TokenMethod.Transfer, input);
            Logger.Info(txId1);
            Logger.Info(txId2);

            var result1 = NodeManager.CheckTransactionResult(txId1);
            Logger.Info(result1.Status);
            var result2 = NodeManager.CheckTransactionResult(txId2);
            Logger.Info(result2.Status);
            //83692 //83692
        }

        [TestMethod]
        public void Transfer_Through_CA_Account_Failed()
        {
            var delegator = Delegator1;
            var delegatee = Delegatee1;
            var delegatee2 = Delegatee2;

            var symbol = "ELF";
            // _tokenContract.TransferBalance(InitAccount, delegatee2, 1000_00000000, "TEST");
            // _tokenContract.TransferBalance(InitAccount, delegator, 10_00000000, "DDD");

            var delegatorBalance = _tokenContract.GetUserBalance(delegator, symbol);
            var delegatorTestBalance = _tokenContract.GetUserBalance(delegator, Symbol);

            var delegateeBalance = _tokenContract.GetUserBalance(delegatee, symbol);
            var delegatee2Balance = _tokenContract.GetUserBalance(delegatee2, symbol);
            var delegatee2OtherBalance = _tokenContract.GetUserBalance(delegatee2, Symbol);

            Logger.Info($"\ndelegator ELF balance: {delegatorBalance}\n" +
                        $"\ndelegator TEST balance: {delegatorTestBalance}\n" +
                        $"delegatee(CA) ELF balance: {delegateeBalance}\n" +
                        $"delegatee2(CA) ELF  balance: {delegatee2Balance}\n" +
                        $"delegatee2(CA) TEST balance: {delegatee2OtherBalance}");
            var originDelegations = _tokenContract.GetTransactionFeeDelegationsOfADelegatee(delegator, delegatee);
            Logger.Info(originDelegations);
            var originDelegations2 = _tokenContract.GetTransactionFeeDelegationsOfADelegatee(delegator, delegatee2);
            Logger.Info(originDelegations2);

            _tokenContract.SetAccount(delegator);
            // var delegatorTransactionFailedResult = _tokenContract.ExecuteMethodWithResult(TokenMethod.Create,
            //     new CreateInput
            //     {
            //         Symbol = "TTT",
            //         Decimals = 8,
            //         IsBurnable = true,
            //         TotalSupply = 1000000000000000,
            //         TokenName = "Failed Token"
            //     });
            var delegatorTransactionFailedResult =
                _tokenContract.TransferBalance(delegator, TestAccount, 10000000, "DDD");
            delegatorTransactionFailedResult.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Failed);
            delegatorTransactionFailedResult.Error.ShouldContain("Transaction fee not enough.");
            var fee = delegatorTransactionFailedResult.GetTransactionFeeInfo();
            Logger.Info(fee);

            var afterDelegations = _tokenContract.GetTransactionFeeDelegationsOfADelegatee(delegator, delegatee);
            Logger.Info(afterDelegations);
            var afterDelegatorBalance = _tokenContract.GetUserBalance(delegator, symbol);
            var afterDelegateeBalance = _tokenContract.GetUserBalance(delegatee, symbol);
            Logger.Info($"\nafter delegator balance: {afterDelegatorBalance}\n" +
                        $"after delegatee(CA) balance: {afterDelegateeBalance}");
            afterDelegatorBalance.ShouldBe(0);
            // fee.ShouldBe(delegatorBalance);
            afterDelegateeBalance.ShouldBe(delegateeBalance);
            afterDelegations.Delegations[symbol].ShouldBe(originDelegations.Delegations[symbol]);
        }

        [TestMethod]
        public void Transfer_Through_CA_MultiAccount()
        {
            var delegator = Delegator1;
            var delegatee = Delegatee1;
            var delegatee2 = Delegatee2;

            var symbol = "ELF";
            var delegatorBalance = _tokenContract.GetUserBalance(delegator, symbol);
            var delegatorTestBalance = _tokenContract.GetUserBalance(delegator, Symbol);

            var delegateeBalance = _tokenContract.GetUserBalance(delegatee, symbol);
            var delegatee2Balance = _tokenContract.GetUserBalance(delegatee2, symbol);
            var delegatee2OtherBalance = _tokenContract.GetUserBalance(delegatee2, Symbol);

            Logger.Info($"\ndelegator ELF balance: {delegatorBalance}\n" +
                        $"\ndelegator TEST balance: {delegatorTestBalance}\n" +
                        $"delegatee(CA) ELF balance: {delegateeBalance}\n" +
                        $"delegatee2(CA) ELF  balance: {delegatee2Balance}\n" +
                        $"delegatee2(CA) TEST balance: {delegatee2OtherBalance}");
            var originDelegations = _tokenContract.GetTransactionFeeDelegationsOfADelegatee(delegator, delegatee);
            Logger.Info(originDelegations);
            var originDelegations2 = _tokenContract.GetTransactionFeeDelegationsOfADelegatee(delegator, delegatee2);
            Logger.Info(originDelegations2);

            _tokenContract.SetAccount(delegator);
            var delegatorTransactionFailedResult =
                _tokenContract.TransferBalance(delegator, TestAccount, 10000000, "DDD");
            delegatorTransactionFailedResult.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);
            var fee = delegatorTransactionFailedResult.GetDefaultTransactionFee();
            Logger.Info(fee);

            var afterDelegations = _tokenContract.GetTransactionFeeDelegationsOfADelegatee(delegator, delegatee);
            Logger.Info(afterDelegations);
            var afterDelegatorBalance = _tokenContract.GetUserBalance(delegator, symbol);
            var afterDelegateeBalance = _tokenContract.GetUserBalance(delegatee, symbol);
            Logger.Info($"\nafter delegator balance: {afterDelegatorBalance}\n" +
                        $"after delegatee(CA) balance: {afterDelegateeBalance}");
            afterDelegatorBalance.ShouldBe(0);
            afterDelegateeBalance.ShouldBe(delegateeBalance.Sub(fee));
            afterDelegations.Delegations[symbol].ShouldBe(originDelegations.Delegations[symbol].Sub(fee));
        }


        [TestMethod]
        public void SetTransactionFeeDelegations_Transfer()
        {
            var symbol = Symbol;
            var amount = 100_00000000;
            var testSymbol = "AAA";
            var delegator = Delegator1;
            var delegatee = Delegatee1;
            _tokenContract.TransferBalance(InitAccount, delegator, 100_00000000, testSymbol);
            var originDelegations = _tokenContract.GetTransactionFeeDelegationsOfADelegatee(delegator, delegatee);
            Logger.Info(originDelegations);
            var balance = _tokenContract.GetUserBalance(delegator, symbol);
            Logger.Info(balance);
            var delegations = new Dictionary<string, long>
            {
                [symbol] = amount
            };
            var input = new SetTransactionFeeDelegationsInput()
            {
                DelegatorAddress = delegator.ConvertAddress(),
                Delegations =
                {
                    delegations
                }
            };
            var transferInput = new TransferInput
            {
                Amount = 100000000,
                To = Address.FromBase58(TestAccount),
                Symbol = "AAA"
            };

            var set = NodeManager.GenerateRawTransaction(delegatee,
                _tokenContract.ContractAddress, TokenMethod.SetTransactionFeeDelegations.ToString(),
                input);
            var tx = NodeManager.GenerateRawTransaction(delegator,
                _tokenContract.ContractAddress, TokenMethod.Transfer.ToString(),
                transferInput);
            var rawTransactionList = new List<string?> { tx, set };
            var rawTransactions = string.Join(",", rawTransactionList);
            var transactions = NodeManager.SendTransactions(rawTransactions);
            Logger.Info(transactions);
            NodeManager.CheckTransactionListResult(transactions);


            var afterDelegations = _tokenContract.GetTransactionFeeDelegationsOfADelegatee(delegator, delegatee);
            Logger.Info(afterDelegations);
        }

        [TestMethod]
        public void RemoveTransactionFeeDelegations_Transfer()
        {
            var symbol = Symbol;
            var testSymbol = "AAA";
            var delegator = Delegator1;
            var delegatee = Delegatee1;
            _tokenContract.TransferBalance(InitAccount, delegatee, 100_00000000);
            _tokenContract.TransferBalance(InitAccount, delegator, 100_00000000, testSymbol);
            var originDelegations = _tokenContract.GetTransactionFeeDelegationsOfADelegatee(delegator, delegatee);
            Logger.Info(originDelegations);
            var balance = _tokenContract.GetUserBalance(delegator, symbol);
            Logger.Info(balance);
            var delegations = new Dictionary<string, long>
            {
                [symbol] = 0
            };
            var input = new SetTransactionFeeDelegationsInput()
            {
                DelegatorAddress = delegator.ConvertAddress(),
                Delegations =
                {
                    delegations
                }
            };
            var transferInput = new TransferInput
            {
                Amount = 100000000,
                To = Address.FromBase58(TestAccount),
                Symbol = testSymbol
            };
            
            var set = NodeManager.GenerateRawTransaction(delegatee,
                _tokenContract.ContractAddress, TokenMethod.SetTransactionFeeDelegations.ToString(),
                input);
            var tx = NodeManager.GenerateRawTransaction(delegator,
                _tokenContract.ContractAddress, TokenMethod.Transfer.ToString(),
                transferInput);
            var rawTransactionList = new List<string?> { set, tx };
            var rawTransactions = string.Join(",", rawTransactionList);
            var transactions = NodeManager.SendTransactions(rawTransactions);
            Logger.Info(transactions);
            NodeManager.CheckTransactionListResult(transactions);

            var afterDelegations = _tokenContract.GetTransactionFeeDelegationsOfADelegatee(delegator, delegatee);
            Logger.Info(afterDelegations);
            // var tx = _tokenContract.TransferBalance(delegator, TestAccount, 10000000, "ELF");
        }

        #endregion

        #region BP ClaimProfits

        [TestMethod]
        public void ClaimProfits()
        {
            var schemeId = Schemes[SchemeType.MinerBasicReward].SchemeId;
            var period = AuthorityManager.GetPeriod();
            var address = _profit.GetSchemeAddress(schemeId, period);
            var miners = AuthorityManager.GetCurrentMiners();
            var profitsInfo = new Dictionary<string, long>();
            foreach (var miner in miners)
            {
                if (miner.Equals(InitAccount)) continue;
                var amount = _profit.GetProfitAmount(miner, schemeId);
                profitsInfo.Add(miner, amount);
            }

            foreach (var miner in miners)
            {
                if (miner.Equals(InitAccount)) continue;
                var minerBalance = _tokenContract.GetUserBalance(miner);
                _profit.SetAccount(miner);
                var profitResult = _profit.ExecuteMethodWithResult(ProfitMethod.ClaimProfits, new ClaimProfitsInput
                {
                    SchemeId = schemeId
                });
                profitResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
                var claimProfitsFee = TransactionFeeCharged.Parser.ParseFrom(ByteString.FromBase64(profitResult.Logs
                    .First(l => l.Name.Equals(nameof(TransactionFeeCharged))).NonIndexed)).Amount;
                var afterBalance = _tokenContract.GetUserBalance(miner);
                afterBalance.ShouldBe(minerBalance + profitsInfo[miner] - claimProfitsFee);
            }
        }

        #endregion

        #region Check

        [TestMethod]
        public void GetBalance()
        {
            foreach (var symbol in _resourceSymbol)
            {
                var balance = _tokenContract.GetUserBalance(_acs8ContractA.ContractAddress, symbol);
                Logger.Info($"Contract A {symbol} balance : {balance}");
            }

            foreach (var symbol in _resourceSymbol)
            {
                var balance = _tokenContract.GetUserBalance(_acs8ContractB.ContractAddress, symbol);
                Logger.Info($"Contract B {symbol} balance : {balance}");
            }

            var treasuryAmount = _treasury.GetCurrentTreasuryBalance();
            Logger.Info($"treasury  balance : {treasuryAmount}");

            var treasuryBalance = _tokenContract.GetUserBalance(_treasury.ContractAddress, "NOBURN");
            Logger.Info($"treasury  balance : {treasuryBalance}");

            var miners = AuthorityManager.GetCurrentMiners();
            foreach (var miner in miners)
            {
                var balance = _tokenContract.GetUserBalance(miner, "ELF");
                Logger.Info($"{miner} balance : {balance}");

                var testBalance = _tokenContract.GetUserBalance(miner, "TEST");
                Logger.Info($"{miner} balance : {testBalance}");
            }

            var userBalance = _tokenContract.GetUserBalance(InitAccount, Symbol);
            Logger.Info($"{InitAccount} TEST balance : {userBalance}");

            var userElfBalance = _tokenContract.GetUserBalance(InitAccount, "ELF");
            Logger.Info($"{InitAccount} ELF balance : {userElfBalance}");
        }

        [TestMethod]
        public async Task CheckSideDividends()
        {
            var genesis = SideContractManager.Genesis;
            var consensusStub = genesis.GetConsensusImplStub();
            var consensus = genesis.GetConsensusContract();
            var profitStub = genesis.GetProfitStub();
            var tokenHolder = genesis.GetTokenHolderStub();
            var token = genesis.GetTokenContract();

            var check = await consensusStub.GetSymbolList.CallAsync(new Empty());
            var unAmount = await consensusStub.GetUndistributedDividends.CallAsync(new Empty());
            Logger.Info($"Symbol list : {check}\n amount:{unAmount}");

            var scheme = await tokenHolder.GetScheme.CallAsync(consensus.Contract);
            Logger.Info(scheme.SchemeId.ToHex());
            var schemeInfo = await profitStub.GetScheme.CallAsync(scheme.SchemeId);
            Logger.Info(schemeInfo);

            foreach (var symbol in check.Value)
            {
                var balance = token.GetUserBalance(consensus.ContractAddress, symbol);
                var virtualBalance = token.GetUserBalance(schemeInfo.VirtualAddress.ToBase58(), symbol);

                Logger.Info(
                    $"{consensus.ContractAddress}: {symbol}={balance}\n {schemeInfo.VirtualAddress}: {symbol}={virtualBalance} ");
            }

            foreach (var symbol in _resourceSymbol)
            {
                var balance = token.GetUserBalance(_acs8ContractA.ContractAddress, symbol);
                Logger.Info($"Contract A {symbol} balance : {balance}");
            }
        }

        [TestMethod]
        public void CheckSchemesAmount()
        {
            Logger.Info($"Treasury: ");
            foreach (var scheme in Schemes)
            {
                var period = AuthorityManager.GetPeriod();
                var schemesId = Schemes[scheme.Key].SchemeId;
                var address = _profit.GetSchemeAddress(schemesId, period - 1);
                var balance = _tokenContract.GetUserBalance(address.ToBase58());
                var testBalance = _tokenContract.GetUserBalance(address.ToBase58(), Symbol);

                var amount = _profit.GetProfitAmount(InitAccount, schemesId);
                Logger.Info(
                    $"{scheme.Key} ELF balance is :{balance} TEST balance is {testBalance}\n amount is {amount}");
            }
        }

        #endregion

        #region Side Chain Developer Fee

        [TestMethod]
        public async Task ChangeDeveloperFeeController()
        {
            var miners = SideAuthority.GetCurrentMiners();
            var defaultController =
                await ContractManager.TokenImplStub.GetDeveloperFeeController.CallAsync(new Empty());
            defaultController.RootController.ContractAddress.ShouldBe(ContractManager.Association.Contract);
            defaultController.ParliamentController.ContractAddress.ShouldBe(ContractManager.Parliament.Contract);
            defaultController.DeveloperController.ContractAddress.ShouldBe(ContractManager.Association.Contract);
            var parliamentController = defaultController.ParliamentController.OwnerAddress;
            var developerController = defaultController.DeveloperController.OwnerAddress;
            var parliamentProposer = miners.First();
            ContractManager.Association.GetOrganization(developerController).ProposerWhiteList
                .Proposers.Contains(parliamentController).ShouldBeTrue();

            var newOrganization = AuthorityManager.CreateAssociationOrganization();
            var proposer = ContractManager.Association.GetOrganization(newOrganization).ProposerWhiteList.Proposers
                .First();
            var input = new AuthorityInfo
            {
                ContractAddress = ContractManager.Association.Contract,
                OwnerAddress = newOrganization
            };
            var createNestProposalInput = new CreateProposalInput
            {
                ToAddress = ContractManager.Token.Contract,
                Params = input.ToByteString(),
                OrganizationAddress = defaultController.RootController.OwnerAddress,
                ContractMethodName = nameof(TokenContractImplContainer.TokenContractImplStub.ChangeDeveloperController),
                ExpiredTime = KernelHelper.GetUtcNow().AddHours(1)
            };

            var createProposalInput = new CreateProposalInput
            {
                ToAddress = ContractManager.Association.Contract,
                Params = createNestProposalInput.ToByteString(),
                OrganizationAddress = parliamentController,
                ContractMethodName =
                    nameof(AssociationContractImplContainer.AssociationContractImplStub.CreateProposal),
                ExpiredTime = KernelHelper.GetUtcNow().AddHours(1)
            };
            var parliamentCreateProposal =
                await ContractManager.ParliamentContractImplStub.CreateProposal.SendAsync(createProposalInput);
            parliamentCreateProposal.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var parliamentProposalId = parliamentCreateProposal.Output;
            ContractManager.Parliament.MinersApproveProposal(parliamentProposalId, miners);
            var releaseRet =
                ContractManager.Parliament.ReleaseProposal(parliamentProposalId, ContractManager.CallAddress);
            var id = ProposalCreated.Parser
                .ParseFrom(releaseRet.Logs.First(l => l.Name.Contains(nameof(ProposalCreated)))
                    .NonIndexed).ProposalId;

            //create approve parliament proposal
            var parliamentApproveProposal = ContractManager.Parliament.CreateProposal(
                ContractManager.Association.ContractAddress,
                nameof(AssociationMethod.Approve), id, parliamentController, parliamentProposer);
            ContractManager.Parliament.MinersApproveProposal(parliamentApproveProposal, miners);
            var parliamentReleaseProposal =
                ContractManager.Parliament.ReleaseProposal(parliamentApproveProposal, parliamentProposer);
            parliamentReleaseProposal.Status.ShouldBe(TransactionResultStatus.Mined);

            //create approve developer proposal
            var createDeveloperNestProposalInput = new CreateProposalInput
            {
                ToAddress = ContractManager.Association.Contract,
                Params = id.ToByteString(),
                OrganizationAddress = developerController,
                ContractMethodName = nameof(AssociationMethod.Approve),
                ExpiredTime = KernelHelper.GetUtcNow().AddHours(1)
            };

            var developerCreateProposal = ContractManager.Parliament.CreateProposal(
                ContractManager.Association.ContractAddress, nameof(ParliamentMethod.CreateProposal),
                createDeveloperNestProposalInput, parliamentController, parliamentProposer);
            ContractManager.Parliament.MinersApproveProposal(developerCreateProposal, miners);
            var developerCreateReleaseProposal =
                ContractManager.Parliament.ReleaseProposal(developerCreateProposal, parliamentProposer);
            developerCreateReleaseProposal.Status.ShouldBe(TransactionResultStatus.Mined);
            var developerCreateId = ProposalCreated.Parser
                .ParseFrom(developerCreateReleaseProposal.Logs.First(l => l.Name.Contains(nameof(ProposalCreated)))
                    .NonIndexed).ProposalId;
            //developer approve
            var developerApproveProposal = ContractManager.Parliament.CreateProposal(
                ContractManager.Association.ContractAddress, nameof(ParliamentMethod.Approve),
                developerCreateId, parliamentController, parliamentProposer);
            ContractManager.Parliament.MinersApproveProposal(developerApproveProposal, miners);
            var developerApproveReleaseProposal =
                ContractManager.Parliament.ReleaseProposal(developerApproveProposal, parliamentProposer);
            developerApproveReleaseProposal.Status.ShouldBe(TransactionResultStatus.Mined);
            //developer release
            var developerReleaseProposal = ContractManager.Parliament.CreateProposal(
                ContractManager.Association.ContractAddress, nameof(ParliamentMethod.Release),
                developerCreateId, parliamentController, parliamentProposer);
            ContractManager.Parliament.MinersApproveProposal(developerReleaseProposal, miners);
            var developerReleaseReleaseProposal =
                ContractManager.Parliament.ReleaseProposal(developerReleaseProposal, parliamentProposer);
            developerReleaseReleaseProposal.Status.ShouldBe(TransactionResultStatus.Mined);

            //release
            var releaseProposal = ContractManager.Parliament.CreateProposal(
                ContractManager.Association.ContractAddress, nameof(AssociationMethod.Release), id,
                parliamentController, parliamentProposer);
            ContractManager.Parliament.MinersApproveProposal(releaseProposal, miners);
            var release =
                ContractManager.Parliament.ReleaseProposal(releaseProposal, parliamentProposer);
            release.Status.ShouldBe(TransactionResultStatus.Mined);

            var updateController =
                await ContractManager.TokenImplStub.GetDeveloperFeeController.CallAsync(new Empty());
            updateController.RootController.ContractAddress.ShouldBe(ContractManager.Association.Contract);
            updateController.RootController.OwnerAddress.ShouldBe(newOrganization);

            //recover
            var recoverInput = new AuthorityInfo
            {
                OwnerAddress = defaultController.RootController.OwnerAddress,
                ContractAddress = defaultController.RootController.ContractAddress
            };
            var recoverProposalId = ContractManager.Association.CreateProposal(ContractManager.Token.ContractAddress,
                nameof(TokenContractImplContainer.TokenContractImplStub.ChangeDeveloperController), recoverInput,
                newOrganization, proposer.ToBase58());
            ContractManager.Association.ApproveWithAssociation(recoverProposalId, newOrganization);
            var recoverRelease =
                ContractManager.Association.ReleaseProposal(recoverProposalId, proposer.ToBase58());
            recoverRelease.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var recoverController =
                await ContractManager.TokenImplStub.GetUserFeeController.CallAsync(new Empty());
            recoverController.RootController.OwnerAddress.ShouldBe(defaultController.RootController.OwnerAddress);
        }

        #endregion

        #region Transfer

        [TestMethod]
        public void Parallel_Transfer_FreeAllowance()
        {
            CreateAndIssueToken(100000_00000000, "AAA");
            var count = 10;
            var fromAddressList = new List<string>();
            var toAddressList = new List<string>();
            var baseSymbol = Symbol;
            var sizeSymbol = "ELF";
            var testSymbol = "AAA";
            for (var i = 0; i < count; i++)
            {
                var address = NodeManager.AccountManager.NewAccount("12345678");
                if (i < count.Div(2))
                {
                    fromAddressList.Add(address);
                    _tokenContract.TransferBalance(InitAccount, address, 100_00000000, sizeSymbol);
                    _tokenContract.TransferBalance(InitAccount, address, 100_00000000, baseSymbol);
                    _tokenContract.TransferBalance(InitAccount, address, 20_00000000, testSymbol);
                }
                else
                    toAddressList.Add(address);
            }

            var rawTransactionList = new List<string>();

            for (var i = 0; i < fromAddressList.Count; i++)
            {
                var input = new TransferInput
                {
                    To = Address.FromBase58(toAddressList[i]),
                    Symbol = testSymbol,
                    Amount = 10000000 * (i + 1)
                };
                var rawTransaction = NodeManager.GenerateRawTransaction(fromAddressList[i],
                    _tokenContract.ContractAddress, TokenMethod.Transfer.ToString(),
                    input);
                rawTransactionList.Add(rawTransaction);
            }

            var rawTransactions = string.Join(",", rawTransactionList);
            var transactions = NodeManager.SendTransactions(rawTransactions);
            Logger.Info(transactions);
            NodeManager.CheckTransactionListResult(transactions);

            foreach (var from in fromAddressList)
            {
                var afterFreeAllowance = _tokenContract.GetMethodFeeFreeAllowances(from);
                Logger.Info(afterFreeAllowance);
                var afterBalance = _tokenContract.GetUserBalance(from, testSymbol);
                Logger.Info(afterBalance);
                var afterSizeFeeBalance = _tokenContract.GetUserBalance(from, sizeSymbol);
                Logger.Info(afterSizeFeeBalance);
                var afterBaseBalance = _tokenContract.GetUserBalance(from, baseSymbol);
                Logger.Info(afterBaseBalance);
            }

            foreach (var to in toAddressList)
            {
                var afterTestBalance = _tokenContract.GetUserBalance(to, testSymbol);
                Logger.Info(afterTestBalance);
            }
        }

        [TestMethod]
        public void Parallel_Transfer_FreeAllowance_DiffToken()
        {
            // CreateAndIssueToken(100000_00000000, "AAA");
            // CreateAndIssueToken(100000_00000000, "BBB");
            // CreateAndIssueToken(100000_00000000, "CCC");
            // CreateAndIssueToken(100000_00000000, "DDD");
            // CreateAndIssueToken(100000_00000000, "EEE");
            var tokenList = new List<string> { "AAA", "BBB", "CCC", "DDD", "EEE" };

            var count = 6;
            var fromAddress = "";
            var toAddressList = new List<string>();
            var baseSymbol = Symbol;
            var sizeSymbol = "ELF";
            for (var i = 0; i < count; i++)
            {
                var address = NodeManager.AccountManager.NewAccount("12345678");
                if (i == 0)
                {
                    _tokenContract.TransferBalance(InitAccount, address, 100_00000000, sizeSymbol);
                    _tokenContract.TransferBalance(InitAccount, address, 100_00000000, baseSymbol);
                    fromAddress = address;
                    foreach (var token in tokenList)
                        _tokenContract.TransferBalance(InitAccount, address, 100_00000000, token);
                }
                else
                    toAddressList.Add(address);
            }

            var rawTransactionList = new List<string>();

            for (var i = 0; i < tokenList.Count; i++)
            {
                var input = new TransferInput
                {
                    To = Address.FromBase58(toAddressList[i]),
                    Symbol = tokenList[i],
                    Amount = 10000000 * (i + 1)
                };
                var rawTransaction = NodeManager.GenerateRawTransaction(fromAddress,
                    _tokenContract.ContractAddress, TokenMethod.Transfer.ToString(),
                    input);
                rawTransactionList.Add(rawTransaction);
            }

            var rawTransactions = string.Join(",", rawTransactionList);
            var transactions = NodeManager.SendTransactions(rawTransactions);
            Logger.Info(transactions);
            NodeManager.CheckTransactionListResult(transactions);

            for (int i = 0; i < tokenList.Count; i++)
            {
                var afterBalance = _tokenContract.GetUserBalance(fromAddress, tokenList[i]);
                Logger.Info(afterBalance);
                var afterTestBalance = _tokenContract.GetUserBalance(toAddressList[i], tokenList[i]);
                Logger.Info(afterTestBalance);
            }

            var afterFreeAllowance = _tokenContract.GetMethodFeeFreeAllowances(fromAddress);
            Logger.Info(afterFreeAllowance);
            var afterSizeFeeBalance = _tokenContract.GetUserBalance(fromAddress, sizeSymbol);
            Logger.Info(afterSizeFeeBalance);
            var afterBaseBalance = _tokenContract.GetUserBalance(fromAddress, baseSymbol);
            Logger.Info(afterBaseBalance);
        }

        // A has free allowance
        // A -> B ELF
        // C -> A TokenA
        [TestMethod]
        public void NON_Parallel_Transfer_FreeAllowance()
        {
            var baseSymbol = "TEST";
            var symbol = "ELF";
            var account1 = NodeManager.NewAccount("12345678");
            var account2 = NodeManager.NewAccount("12345678");
            var account3 = NodeManager.NewAccount("12345678");

            _tokenContract.TransferBalance(InitAccount, account1, 100_00000000);
            _tokenContract.TransferBalance(InitAccount, account3, 100_00000000);
            _tokenContract.TransferBalance(InitAccount, account3, 100_00000000,baseSymbol);


            var freeAllowance = _tokenContract.GetMethodFeeFreeAllowances(account1);
            Logger.Info(freeAllowance);
            var originBalance = _tokenContract.GetUserBalance(account1, symbol);
            Logger.Info(originBalance);
            var originFeeBalance = _tokenContract.GetUserBalance(account1, baseSymbol);
            Logger.Info(originFeeBalance);


            var transferInput1 = new TransferInput
            {
                Amount = 100000000,
                Symbol = symbol,
                To = Address.FromBase58(account2)
            };

            var transferInput2 = new TransferInput
            {
                Amount = 200000000,
                Symbol = baseSymbol,
                To = Address.FromBase58(account1)
            };

            var tx1 = NodeManager.GenerateRawTransaction(account1, _tokenContract.ContractAddress, "Transfer",
                transferInput1);
            var tx2 = NodeManager.GenerateRawTransaction(account3, _tokenContract.ContractAddress, "Transfer",
                transferInput2);
            var txList = new List<string> { tx1, tx2 };

            var rawTransactions = string.Join(",", txList);
            var transactions = NodeManager.SendTransactions(rawTransactions);
            Logger.Info(transactions);
            NodeManager.CheckTransactionListResult(transactions);

            var afterFreeAllowance = _tokenContract.GetMethodFeeFreeAllowances(account1);
            Logger.Info(afterFreeAllowance);
            var afterBalance = _tokenContract.GetUserBalance(account1, symbol);
            Logger.Info(afterBalance);
            var afterFeeBalance = _tokenContract.GetUserBalance(account1, baseSymbol);
            Logger.Info(afterFeeBalance);
        }

        [TestMethod]
        public void Parallel_Transfer_Delegation()
        {
            var testSymbol = "AAA";
            var symbol = "ELF";
            var delegator1 = Delegator1;
            var delegator2 = Delegator2;
            var delegatee1 = Delegatee1;
            var delegatee2 = Delegatee2;
            var delegatee3 = Delegatee3;
            var delegatee4 = Delegatee4;
            CreateAndIssueToken(100000_00000000, testSymbol);
            _tokenContract.TransferBalance(InitAccount, delegator1, 100_00000000, testSymbol);
            _tokenContract.TransferBalance(InitAccount, delegator2, 100_00000000, testSymbol);

            var delegatorBalance = _tokenContract.GetUserBalance(delegator1, symbol);
            var delegatee1Balance = _tokenContract.GetUserBalance(delegatee1, symbol);
            var delegatee3Balance = _tokenContract.GetUserBalance(delegatee3, symbol);

            Logger.Info($"\ndelegator1 balance: {delegatorBalance}\n" +
                        $"delegatee1(CA) balance: {delegatee1Balance} {delegatee3Balance}");

            var delegator2Balance = _tokenContract.GetUserBalance(delegator2, symbol);
            var delegatee2Balance = _tokenContract.GetUserBalance(delegatee2, symbol);
            var delegatee4Balance = _tokenContract.GetUserBalance(delegatee4, symbol);

            Logger.Info($"\ndelegator2 balance: {delegator2Balance}\n" +
                        $"delegatee2(CA) balance: {delegatee2Balance} {delegatee4Balance}");

            var originDelegations1 = _tokenContract.GetTransactionFeeDelegationsOfADelegatee(delegator1, delegatee1);
            Logger.Info(originDelegations1);
            var originDelegations3 = _tokenContract.GetTransactionFeeDelegationsOfADelegatee(delegator1, delegatee3);
            Logger.Info(originDelegations3);
            var originDelegations2 = _tokenContract.GetTransactionFeeDelegationsOfADelegatee(delegator2, delegatee2);
            Logger.Info(originDelegations2);
            var originDelegations4 = _tokenContract.GetTransactionFeeDelegationsOfADelegatee(delegator2, delegatee4);
            Logger.Info(originDelegations4);

            var toAddress1 = NodeManager.NewAccount("12345678");
            var toAddress2 = NodeManager.NewAccount("12345678");
            var transferInput1 = new TransferInput
            {
                Symbol = testSymbol,
                Amount = 100000000,
                To = Address.FromBase58(toAddress1)
            };

            var transferInput2 = new TransferInput
            {
                Symbol = testSymbol,
                Amount = 200000000,
                To = Address.FromBase58(toAddress2)
            };


            _tokenContract.SetAccount(delegator1);
            var tx1 = _tokenContract.ExecuteMethodWithTxId(TokenMethod.Transfer, transferInput1);

            _tokenContract.SetAccount(delegator2);
            var tx2 = _tokenContract.ExecuteMethodWithTxId(TokenMethod.Transfer, transferInput2);

            NodeManager.CheckTransactionResult(tx1);
            NodeManager.CheckTransactionResult(tx2);


            var afterDelegations1 = _tokenContract.GetTransactionFeeDelegationsOfADelegatee(delegator1, delegatee1);
            Logger.Info(afterDelegations1);
            var afterDelegations3 = _tokenContract.GetTransactionFeeDelegationsOfADelegatee(delegator1, delegatee3);
            Logger.Info(afterDelegations3);

            var afterDelegations2 = _tokenContract.GetTransactionFeeDelegationsOfADelegatee(delegator2, delegatee2);
            Logger.Info(afterDelegations2);
            var afterDelegations4 = _tokenContract.GetTransactionFeeDelegationsOfADelegatee(delegator1, delegatee4);
            Logger.Info(afterDelegations4);

            var afterDelegatorBalance1 = _tokenContract.GetUserBalance(delegator1, symbol);
            var afterDelegateeBalance1 = _tokenContract.GetUserBalance(delegatee1, symbol);
            var afterDelegateeBalance3 = _tokenContract.GetUserBalance(delegatee3, symbol);

            var afterDelegatorBalance2 = _tokenContract.GetUserBalance(delegator2, symbol);
            var afterDelegateeBalance2 = _tokenContract.GetUserBalance(delegatee2, symbol);
            var afterDelegateeBalance4 = _tokenContract.GetUserBalance(delegatee4, symbol);


            Logger.Info($"\nafter delegator1 balance: {afterDelegatorBalance1}\n" +
                        $"after delegatee1(CA) balance: {afterDelegateeBalance1} {afterDelegateeBalance3}");
            Logger.Info($"\nafter delegator2 balance: {afterDelegatorBalance2}\n" +
                        $"after delegatee2(CA) balance: {afterDelegateeBalance2} {afterDelegateeBalance4}");
        }

        // Delegator1, Delegator2 --> Delegatee1
        // Delegator3, Delegator4 --> Delegatee2
        [TestMethod]
        public void Parallel_Transfer_Delegation_MultiAccount()
        {
            var testSymbol1 = "AAA";
            var testSymbol2 = "BBB";
            var testSymbol3 = "CCC";
            var testSymbol4 = "DDD";
            CreateAndIssueToken(100000_00000000, testSymbol1);
            CreateAndIssueToken(100000_00000000, testSymbol2);
            CreateAndIssueToken(100000_00000000, testSymbol3);
            CreateAndIssueToken(100000_00000000, testSymbol4);

            var symbol = "ELF";
            var delegator1 = Delegator1;
            var delegator2 = Delegator2;
            var delegator3 = Delegator3;
            var delegator4 = Delegator4;

            var delegatee1 = Delegatee1;
            var delegatee2 = Delegatee2;

            _tokenContract.TransferBalance(InitAccount, delegator1, 100_00000000, testSymbol1);
            _tokenContract.TransferBalance(InitAccount, delegator2, 100_00000000, testSymbol2);
            _tokenContract.TransferBalance(InitAccount, delegator3, 100_00000000, testSymbol3);
            _tokenContract.TransferBalance(InitAccount, delegator4, 100_00000000, testSymbol4);


            var delegator1Balance = _tokenContract.GetUserBalance(delegator1, symbol);
            var delegator2Balance = _tokenContract.GetUserBalance(delegator2, symbol);
            var delegatee1Balance = _tokenContract.GetUserBalance(delegatee1, symbol);

            Logger.Info($"\ndelegator1 balance: {delegator1Balance}\n" +
                        $"delegator2 balance: {delegator2Balance}\n" +
                        $"delegatee1(CA) balance: {delegatee1Balance}");

            var delegator3Balance = _tokenContract.GetUserBalance(delegator3, symbol);
            var delegator4Balance = _tokenContract.GetUserBalance(delegator4, symbol);
            var delegatee2Balance = _tokenContract.GetUserBalance(delegatee2, symbol);

            Logger.Info($"\ndelegator3 balance: {delegator3Balance}\n" +
                        $"delegator4 balance: {delegator4Balance}\n" +
                        $"delegatee2(CA) balance: {delegatee2Balance}");

            var originDelegations1 = _tokenContract.GetTransactionFeeDelegationsOfADelegatee(delegator1, delegatee1);
            Logger.Info(originDelegations1);
            var originDelegations3 = _tokenContract.GetTransactionFeeDelegationsOfADelegatee(delegator2, delegatee1);
            Logger.Info(originDelegations3);
            var originDelegations2 = _tokenContract.GetTransactionFeeDelegationsOfADelegatee(delegator3, delegatee2);
            Logger.Info(originDelegations2);
            var originDelegations4 = _tokenContract.GetTransactionFeeDelegationsOfADelegatee(delegator4, delegatee2);
            Logger.Info(originDelegations4);

            var toAddress1 = NodeManager.NewAccount("12345678");
            var toAddress2 = NodeManager.NewAccount("12345678");
            var toAddress3 = NodeManager.NewAccount("12345678");
            var toAddress4 = NodeManager.NewAccount("12345678");
            var transferInput1 = new TransferInput
            {
                Symbol = testSymbol1,
                Amount = 100000000,
                To = Address.FromBase58(toAddress1)
            };

            var transferInput2 = new TransferInput
            {
                Symbol = testSymbol2,
                Amount = 200000000,
                To = Address.FromBase58(toAddress2)
            };
            var transferInput3 = new TransferInput
            {
                Symbol = testSymbol3,
                Amount = 200000000,
                To = Address.FromBase58(toAddress3)
            };
            var transferInput4 = new TransferInput
            {
                Symbol = testSymbol4,
                Amount = 200000000,
                To = Address.FromBase58(toAddress4)
            };
            var txList = new List<string>();
            var tx1 = NodeManager.GenerateRawTransaction(delegator1, _tokenContract.ContractAddress, "Transfer",
                transferInput1);
            var tx2 = NodeManager.GenerateRawTransaction(delegator2, _tokenContract.ContractAddress, "Transfer",
                transferInput2);
            var tx3 = NodeManager.GenerateRawTransaction(delegator3, _tokenContract.ContractAddress, "Transfer",
                transferInput3);
            var tx4 = NodeManager.GenerateRawTransaction(delegator4, _tokenContract.ContractAddress, "Transfer",
                transferInput4);

            txList.Add(tx1);
            txList.Add(tx2);
            txList.Add(tx3);
            txList.Add(tx4);
            var rawTransactions = string.Join(",", txList);
            var transactions = NodeManager.SendTransactions(rawTransactions);
            Logger.Info(transactions);
            NodeManager.CheckTransactionListResult(transactions);

            var afterDelegations1 = _tokenContract.GetTransactionFeeDelegationsOfADelegatee(delegator1, delegatee1);
            Logger.Info(afterDelegations1);
            var afterDelegations3 = _tokenContract.GetTransactionFeeDelegationsOfADelegatee(delegator2, delegatee1);
            Logger.Info(afterDelegations3);

            var afterDelegations2 = _tokenContract.GetTransactionFeeDelegationsOfADelegatee(delegator3, delegatee2);
            Logger.Info(afterDelegations2);
            var afterDelegations4 = _tokenContract.GetTransactionFeeDelegationsOfADelegatee(delegator4, delegatee2);
            Logger.Info(afterDelegations4);

            var afterDelegatorBalance1 = _tokenContract.GetUserBalance(delegator1, symbol);
            var afterDelegatorBalance2 = _tokenContract.GetUserBalance(delegator2, symbol);
            var afterDelegateeBalance1 = _tokenContract.GetUserBalance(delegatee1, symbol);

            var afterDelegatorBalance3 = _tokenContract.GetUserBalance(delegator3, symbol);
            var afterDelegatorBalance4 = _tokenContract.GetUserBalance(delegator4, symbol);
            var afterDelegateeBalance2 = _tokenContract.GetUserBalance(delegatee2, symbol);


            Logger.Info($"\nafter delegator1 balance: {afterDelegatorBalance1}\n" +
                        $"after delegator2 balance: {afterDelegatorBalance2}\n" +
                        $"after delegatee1(CA) balance: {afterDelegateeBalance1}");
            Logger.Info($"\nafter delegator3 balance: {afterDelegatorBalance3}\n" +
                        $"after delegator4 balance: {afterDelegatorBalance4}\n" +
                        $"after delegatee2(CA) balance: {afterDelegateeBalance2}");
        }


        // Delegator A Delegatee C -- Delegator B Delegatee C
        [TestMethod]
        public void NON_Parallel_Transfer_Delegation_SameDelegatee()
        {
            var testSymbol = "AAA";
            var symbol = "ELF";
            var delegator1 = Delegator1;
            var delegator2 = Delegator2;
            var delegatee = Delegatee1;

            // CreateAndIssueToken(100000_00000000, testSymbol);
            _tokenContract.TransferBalance(InitAccount, delegator1, 100_00000000, testSymbol);
            _tokenContract.TransferBalance(InitAccount, delegator2, 100_00000000, testSymbol);

            var delegator1Balance = _tokenContract.GetUserBalance(delegator1, symbol);
            var delegator2Balance = _tokenContract.GetUserBalance(delegator2, symbol);
            var delegateeBalance = _tokenContract.GetUserBalance(delegatee, symbol);

            Logger.Info($"\ndelegator1 balance: {delegator1Balance}\n" +
                        $"delegator2 balance: {delegator2Balance}\n" +
                        $"delegatee(CA) balance: {delegateeBalance}");

            var originDelegations1 = _tokenContract.GetTransactionFeeDelegationsOfADelegatee(delegator1, delegatee);
            Logger.Info(originDelegations1);
            var originDelegations2 = _tokenContract.GetTransactionFeeDelegationsOfADelegatee(delegator2, delegatee);
            Logger.Info(originDelegations2);

            var toAddress1 = NodeManager.NewAccount("12345678");
            var toAddress2 = NodeManager.NewAccount("12345678");
            var transferInput1 = new TransferInput
            {
                Symbol = testSymbol,
                Amount = 100000000,
                To = Address.FromBase58(toAddress1)
            };

            var transferInput2 = new TransferInput
            {
                Symbol = testSymbol,
                Amount = 200000000,
                To = Address.FromBase58(toAddress2)
            };


            _tokenContract.SetAccount(delegator1);
            var tx1 = _tokenContract.ExecuteMethodWithTxId(TokenMethod.Transfer, transferInput1);

            _tokenContract.SetAccount(delegator2);
            var tx2 = _tokenContract.ExecuteMethodWithTxId(TokenMethod.Transfer, transferInput2);

            NodeManager.CheckTransactionResult(tx1);
            NodeManager.CheckTransactionResult(tx2);


            var afterDelegations1 = _tokenContract.GetTransactionFeeDelegationsOfADelegatee(delegator1, delegatee);
            Logger.Info(afterDelegations1);
            var afterDelegations2 = _tokenContract.GetTransactionFeeDelegationsOfADelegatee(delegator2, delegatee);
            Logger.Info(afterDelegations2);

            var afterDelegatorBalance1 = _tokenContract.GetUserBalance(delegator1, symbol);
            var afterDelegatorBalance2 = _tokenContract.GetUserBalance(delegator2, symbol);
            var afterDelegateeBalance = _tokenContract.GetUserBalance(delegatee, symbol);

            Logger.Info($"\nafter delegator1 balance: {afterDelegatorBalance1}\n" +
                        $"after delegator2 balance: {afterDelegatorBalance2}\n" +
                        $"after delegatee(CA) balance: {afterDelegateeBalance}");
        }

        // Delegator A Delegatee B 
        // Delegator B Delegatee C
        [TestMethod]
        public void NON_Parallel_Transfer_Delegation_DelegateeIsDelegator()
        {
            var testSymbol = "AAA";
            var symbol = "ELF";
            var baseFeeSymbol = "TEST";
            var delegator1 = Delegator1;
            var delegator2 = Delegatee1;
            var delegatee = Delegatee2;

            // CreateAndIssueToken(100000_00000000, testSymbol);
            _tokenContract.TransferBalance(InitAccount, delegator1, 100_00000000, testSymbol);
            _tokenContract.TransferBalance(InitAccount, delegator2, 100_00000000, testSymbol);
            _tokenContract.TransferBalance(InitAccount, delegator2, 1000_00000000, baseFeeSymbol);

            var delegator1Balance = _tokenContract.GetUserBalance(delegator1, symbol);
            var delegator2Balance = _tokenContract.GetUserBalance(delegator2, symbol);
            var delegateeBalance = _tokenContract.GetUserBalance(delegatee, symbol);

            Logger.Info($"\ndelegator1 balance: {delegator1Balance}\n" +
                        $"delegator2 balance: {delegator2Balance}\n" +
                        $"delegatee(CA) balance: {delegateeBalance}");

            var originDelegations1 = _tokenContract.GetTransactionFeeDelegationsOfADelegatee(delegator1, delegatee);
            Logger.Info(originDelegations1);
            var originDelegations2 = _tokenContract.GetTransactionFeeDelegationsOfADelegatee(delegator2, delegatee);
            Logger.Info(originDelegations2);

            var toAddress1 = NodeManager.NewAccount("12345678");
            var toAddress2 = NodeManager.NewAccount("12345678");
            var transferInput1 = new TransferInput
            {
                Symbol = testSymbol,
                Amount = 100000000,
                To = Address.FromBase58(toAddress1)
            };

            var transferInput2 = new TransferInput
            {
                Symbol = testSymbol,
                Amount = 200000000,
                To = Address.FromBase58(toAddress2)
            };


            _tokenContract.SetAccount(delegator1);
            var tx1 = _tokenContract.ExecuteMethodWithTxId(TokenMethod.Transfer, transferInput1);

            _tokenContract.SetAccount(delegator2);
            var tx2 = _tokenContract.ExecuteMethodWithTxId(TokenMethod.Transfer, transferInput2);

            NodeManager.CheckTransactionResult(tx1);
            NodeManager.CheckTransactionResult(tx2);


            var afterDelegations1 = _tokenContract.GetTransactionFeeDelegationsOfADelegatee(delegator1, delegatee);
            Logger.Info(afterDelegations1);
            var afterDelegations2 = _tokenContract.GetTransactionFeeDelegationsOfADelegatee(delegator2, delegatee);
            Logger.Info(afterDelegations2);

            var afterDelegatorBalance1 = _tokenContract.GetUserBalance(delegator1, symbol);
            var afterDelegatorBalance2 = _tokenContract.GetUserBalance(delegator2, symbol);
            var afterDelegateeBalance = _tokenContract.GetUserBalance(delegatee, symbol);

            Logger.Info($"\nafter delegator1 balance: {afterDelegatorBalance1}\n" +
                        $"after delegator2 balance: {afterDelegatorBalance2}\n" +
                        $"after delegatee(CA) balance: {afterDelegateeBalance}");
        }

        // Delegator A Delegatee B 
        // A transfer to C 
        // D transfer to B
        [TestMethod]
        [DataRow("ELF")] // transfer fee token, grouped 2 tx into 1 groups  -- non-parallet
        //[DataRow("AAA")] // transfer other token, grouped 2 tx into 2 groups -- parallet
        public void Parallel_Transfer_Delegation_TransferToDelegatee(string testSymbol)
        {
            var symbol = "ELF";
            var baseFeeSymbol = Symbol;
            var delegator = Delegator1;
            var delegatee = Delegatee1;
            var toAccount = NodeManager.NewAccount("12345678");
            var fromAccount = NodeManager.NewAccount("12345678");

            _tokenContract.TransferBalance(InitAccount, fromAccount, 1000_00000000, baseFeeSymbol);
            _tokenContract.TransferBalance(InitAccount, fromAccount, 1000_00000000, testSymbol);
            _tokenContract.TransferBalance(InitAccount, fromAccount, 100_00000000, symbol);
            _tokenContract.TransferBalance(InitAccount, delegator, 30000000, symbol);

            var delegatorBalance = _tokenContract.GetUserBalance(delegator, symbol);
            var delegatorBaseFeeBalance = _tokenContract.GetUserBalance(delegator, baseFeeSymbol);
            var delegateeBalance = _tokenContract.GetUserBalance(delegatee, symbol);
            var delegateeBaseFeeBalance = _tokenContract.GetUserBalance(delegatee, baseFeeSymbol);


            Logger.Info($"\ndelegator balance: {delegatorBalance}\n" +
                        $"delegator base fee balance: {delegatorBaseFeeBalance}\n" +
                        $"delegatee(CA) balance: {delegateeBalance}\n" +
                        $"delegatee(CA) base fee balance: {delegateeBaseFeeBalance}");

            var originDelegations = _tokenContract.GetTransactionFeeDelegationsOfADelegatee(delegator, delegatee);
            Logger.Info(originDelegations);

            var transferInput1 = new TransferInput
            {
                Symbol = testSymbol,
                Amount = 100000000,
                To = Address.FromBase58(toAccount)
            };

            var transferInput2 = new TransferInput
            {
                Symbol = testSymbol,
                Amount = 200000000,
                To = Address.FromBase58(delegatee)
            };

            var txList = new List<string>();
            var tx1 = NodeManager.GenerateRawTransaction(delegator, _tokenContract.ContractAddress, "Transfer",
                transferInput1);
            var tx2 = NodeManager.GenerateRawTransaction(fromAccount, _tokenContract.ContractAddress, "Transfer",
                transferInput2);

            txList.Add(tx1);
            txList.Add(tx2);
            var rawTransactions = string.Join(",", txList);
            var transactions = NodeManager.SendTransactions(rawTransactions);
            Logger.Info(transactions);
            NodeManager.CheckTransactionListResult(transactions);

            var afterDelegations = _tokenContract.GetTransactionFeeDelegationsOfADelegatee(delegator, delegatee);
            Logger.Info(afterDelegations);

            var afterDelegatorBalance = _tokenContract.GetUserBalance(delegator, symbol);
            var afterDelegatorBaseFeeBalance = _tokenContract.GetUserBalance(delegator, baseFeeSymbol);
            var afterDelegateeBalance = _tokenContract.GetUserBalance(delegatee, symbol);
            var afterDelegateeBaseFeeBalance = _tokenContract.GetUserBalance(delegatee, baseFeeSymbol);


            Logger.Info($"\nafter delegator balance: {afterDelegatorBalance}\n" +
                        $"after delegator base fee balance: {afterDelegatorBaseFeeBalance}\n" +
                        $"after delegatee(CA) balance: {afterDelegateeBalance}\n" +
                        $"after delegatee(CA) base fee balance: {afterDelegateeBaseFeeBalance}");
        }

        // Delegator A Delegatee B 
        // Delegator C Delegatee D
        // Delegator A add Delegatee D 
        // A transfer to E
        // C transfer to F
        // From 3 transactions, grouped 2 txs into 2 groups, left 1 as non-parallelizable transactions.
        [TestMethod]
        public void Parallel_Transfer_Delegation_AddDelegateeAndTranfser()
        {
            var testSymbol1 = "AAA";
            var testSymbol2 = "BBB";
            var symbol = "ELF";
            var amount = 100000000;
            var delegator1 = Delegator1;
            var delegatee1 = Delegatee1;
            var delegator2 = Delegator2;
            var delegatee2 = Delegatee2;

            var toAccount1 = NodeManager.NewAccount("12345678");
            var toAccount2 = NodeManager.NewAccount("12345678");

            _tokenContract.TransferBalance(InitAccount, delegator1, 1000_00000000, testSymbol1);
            _tokenContract.TransferBalance(InitAccount, delegator2, 1000_00000000, testSymbol2);


            var delegator1Balance = _tokenContract.GetUserBalance(delegator1, symbol);
            var delegator2Balance = _tokenContract.GetUserBalance(delegator2, symbol);
            var delegatee1Balance = _tokenContract.GetUserBalance(delegatee1, symbol);
            var delegatee2Balance = _tokenContract.GetUserBalance(delegatee2, symbol);


            Logger.Info($"\ndelegator1 balance: {delegator1Balance}\n" +
                        $"delegatee1(CA) balance: {delegatee1Balance}\n" +
                        $"delegator2 balance: {delegator2Balance}\n" +
                        $"delegatee2(CA) balance: {delegatee2Balance}");

            var originDelegations1 = _tokenContract.GetTransactionFeeDelegationsOfADelegatee(delegator1, delegatee1);
            Logger.Info(originDelegations1);
            var originDelegationsAdd = _tokenContract.GetTransactionFeeDelegationsOfADelegatee(delegator1, delegatee2);
            Logger.Info(originDelegationsAdd);
            var originDelegations2 = _tokenContract.GetTransactionFeeDelegationsOfADelegatee(delegator2, delegatee2);
            Logger.Info(originDelegations2);

            var delegations = new Dictionary<string, long>
            {
                [symbol] = amount
            };
            var input = new SetTransactionFeeDelegationsInput()
            {
                DelegatorAddress = delegator1.ConvertAddress(),
                Delegations =
                {
                    delegations
                }
            };

            var set = NodeManager.GenerateRawTransaction(delegatee2, _tokenContract.ContractAddress,
                nameof(TokenMethod.SetTransactionFeeDelegations), input);

            var transferInput1 = new TransferInput
            {
                Symbol = testSymbol1,
                Amount = 100000000,
                To = Address.FromBase58(toAccount1)
            };

            var transferInput2 = new TransferInput
            {
                Symbol = testSymbol1,
                Amount = 200000000,
                To = Address.FromBase58(toAccount2)
            };

            var txList = new List<string>();
            var tx1 = NodeManager.GenerateRawTransaction(delegator1, _tokenContract.ContractAddress, "Transfer",
                transferInput1);
            var tx2 = NodeManager.GenerateRawTransaction(delegator2, _tokenContract.ContractAddress, "Transfer",
                transferInput2);
            txList.Add(set);
            txList.Add(tx1);
            txList.Add(tx2);

            var rawTransactions = string.Join(",", txList);
            var transactions = NodeManager.SendTransactions(rawTransactions);
            Logger.Info(transactions);
            NodeManager.CheckTransactionListResult(transactions);

            var afterDelegator1Balance = _tokenContract.GetUserBalance(delegator1, symbol);
            var afterDelegator2Balance = _tokenContract.GetUserBalance(delegator2, symbol);
            var afterDelegatee1Balance = _tokenContract.GetUserBalance(delegatee1, symbol);
            var afterDelegatee2Balance = _tokenContract.GetUserBalance(delegatee2, symbol);

            var afterDelegations1 = _tokenContract.GetTransactionFeeDelegationsOfADelegatee(delegator1, delegatee1);
            Logger.Info(afterDelegations1);
            var afterDelegationsAdd = _tokenContract.GetTransactionFeeDelegationsOfADelegatee(delegator1, delegatee2);
            Logger.Info(afterDelegationsAdd);

            var afterDelegations2 = _tokenContract.GetTransactionFeeDelegationsOfADelegatee(delegator2, delegatee2);
            Logger.Info(afterDelegations2);

            Logger.Info($"\nafter delegator1 balance: {afterDelegator1Balance}\n" +
                        $"after delegatee1(CA) balance: {afterDelegatee1Balance}\n" +
                        $"after delegator2 balance: {afterDelegator2Balance}\n" +
                        $"after delegatee2(CA) base fee balance: {afterDelegatee2Balance}");
        }

        // Delegator A Delegatee B Delegatee D
        // Delegator C Delegatee D
        // Delegator A Remove Delegatee D 
        // A transfer to E
        // C transfer to F
        // From 3 transactions, grouped 2 txs into 1 groups, left 1 as non-parallelizable transactions.
        [TestMethod]
        public void Parallel_Transfer_Delegation_RemoveDelegateeAndTranfser()
        {
            var testSymbol1 = "AAA";
            var testSymbol2 = "BBB";
            var symbol = "ELF";
            var amount = 100000000;
            var delegator1 = Delegator1;
            var delegatee1 = Delegatee1;
            var delegator2 = Delegator2;
            var delegatee2 = Delegatee2;

            var toAccount1 = NodeManager.NewAccount("12345678");
            var toAccount2 = NodeManager.NewAccount("12345678");

            _tokenContract.TransferBalance(InitAccount, delegator1, 1000_00000000, testSymbol1);
            _tokenContract.TransferBalance(InitAccount, delegator2, 1000_00000000, testSymbol2);


            var delegator1Balance = _tokenContract.GetUserBalance(delegator1, symbol);
            var delegator2Balance = _tokenContract.GetUserBalance(delegator2, symbol);
            var delegatee1Balance = _tokenContract.GetUserBalance(delegatee1, symbol);
            var delegatee2Balance = _tokenContract.GetUserBalance(delegatee2, symbol);


            Logger.Info($"\ndelegator1 balance: {delegator1Balance}\n" +
                        $"delegatee1(CA) balance: {delegatee1Balance}\n" +
                        $"delegator2 balance: {delegator2Balance}\n" +
                        $"delegatee2(CA) balance: {delegatee2Balance}");

            var originDelegations1 = _tokenContract.GetTransactionFeeDelegationsOfADelegatee(delegator1, delegatee1);
            Logger.Info(originDelegations1);
            var originDelegationsRemove =
                _tokenContract.GetTransactionFeeDelegationsOfADelegatee(delegator1, delegatee2);
            Logger.Info(originDelegationsRemove);
            var originDelegations2 = _tokenContract.GetTransactionFeeDelegationsOfADelegatee(delegator2, delegatee2);
            Logger.Info(originDelegations2);

            var input = new RemoveTransactionFeeDelegatorInput()
            {
                DelegatorAddress = delegator1.ConvertAddress()
            };

            var remove = NodeManager.GenerateRawTransaction(delegatee2, _tokenContract.ContractAddress,
                nameof(TokenMethod.RemoveTransactionFeeDelegator), input);

            var transferInput1 = new TransferInput
            {
                Symbol = testSymbol1,
                Amount = 100000000,
                To = Address.FromBase58(toAccount1)
            };

            var transferInput2 = new TransferInput
            {
                Symbol = testSymbol1,
                Amount = 200000000,
                To = Address.FromBase58(toAccount2)
            };

            var txList = new List<string>();
            var tx1 = NodeManager.GenerateRawTransaction(delegator1, _tokenContract.ContractAddress, "Transfer",
                transferInput1);
            var tx2 = NodeManager.GenerateRawTransaction(delegator2, _tokenContract.ContractAddress, "Transfer",
                transferInput2);
            txList.Add(remove);
            txList.Add(tx1);
            txList.Add(tx2);

            var rawTransactions = string.Join(",", txList);
            var transactions = NodeManager.SendTransactions(rawTransactions);
            Logger.Info(transactions);
            NodeManager.CheckTransactionListResult(transactions);

            var afterDelegator1Balance = _tokenContract.GetUserBalance(delegator1, symbol);
            var afterDelegator2Balance = _tokenContract.GetUserBalance(delegator2, symbol);
            var afterDelegatee1Balance = _tokenContract.GetUserBalance(delegatee1, symbol);
            var afterDelegatee2Balance = _tokenContract.GetUserBalance(delegatee2, symbol);

            var afterDelegations1 = _tokenContract.GetTransactionFeeDelegationsOfADelegatee(delegator1, delegatee1);
            Logger.Info(afterDelegations1);
            var afterDelegationsRemove =
                _tokenContract.GetTransactionFeeDelegationsOfADelegatee(delegator1, delegatee2);
            Logger.Info(afterDelegationsRemove);

            var afterDelegations2 = _tokenContract.GetTransactionFeeDelegationsOfADelegatee(delegator2, delegatee2);
            Logger.Info(afterDelegations2);

            Logger.Info($"\nafter delegator1 balance: {afterDelegator1Balance}\n" +
                        $"after delegatee1(CA) balance: {afterDelegatee1Balance}\n" +
                        $"after delegator2 balance: {afterDelegator2Balance}\n" +
                        $"after delegatee2(CA) base fee balance: {afterDelegatee2Balance}");
        }

        #endregion

        [TestMethod]
        public void CheckParallel()
        {
            var count = 4;
            var fromAddressList = new List<string>();
            var toAddressList = new List<string>();
            var baseSymbol = Symbol;
            var sizeSymbol = "ELF";
            var testSymbol = "AAA";
            for (var i = 0; i < count; i++)
            {
                var address = NodeManager.AccountManager.NewAccount("12345678");
                if (i < count.Div(2))
                {
                    fromAddressList.Add(address);
                    _tokenContract.TransferBalance(InitAccount, address, 100_00000000, sizeSymbol);
                    _tokenContract.TransferBalance(InitAccount, address, 1000_00000000, baseSymbol);
                    _tokenContract.TransferBalance(InitAccount, address, 20_00000000, testSymbol);
                }
                else
                    toAddressList.Add(address);
            }

            var rawTransactionList = new List<string>();

            for (var i = 0; i < fromAddressList.Count; i++)
            {
                var input = new TransferInput
                {
                    To = Address.FromBase58(toAddressList[i]),
                    Symbol = testSymbol,
                    Amount = 10000000 * (i + 1)
                };
                var rawTransaction = NodeManager.GenerateRawTransaction(fromAddressList[i],
                    _tokenContract.ContractAddress, TokenMethod.Transfer.ToString(),
                    input);
                rawTransactionList.Add(rawTransaction);
            }

            var rawTransactions = string.Join(",", rawTransactionList);
            var transactions = NodeManager.SendTransactions(rawTransactions);
            Logger.Info(transactions);
            NodeManager.CheckTransactionListResult(transactions);

            foreach (var from in fromAddressList)
            {
                var afterFreeAllowance = _tokenContract.GetMethodFeeFreeAllowances(from);
                Logger.Info(afterFreeAllowance);
                var afterBalance = _tokenContract.GetUserBalance(from, testSymbol);
                Logger.Info(afterBalance);
                var afterSizeFeeBalance = _tokenContract.GetUserBalance(from, sizeSymbol);
                Logger.Info(afterSizeFeeBalance);
                var afterBaseBalance = _tokenContract.GetUserBalance(from, baseSymbol);
                Logger.Info(afterBaseBalance);
            }

            foreach (var to in toAddressList)
            {
                var afterTestBalance = _tokenContract.GetUserBalance(to, testSymbol);
                Logger.Info(afterTestBalance);
            }
        }

        #region private

        private async Task AdvanceResourceToken()
        {
            foreach (var symbol in _resourceSymbol)
            {
                var beforeBalance = _tokenContract.GetUserBalance(_acs8ContractA.ContractAddress, symbol);
                if (beforeBalance >= 1000_00000000) continue;
                var result = await _tokenConverterSub.Buy.SendAsync(new BuyInput
                {
                    Symbol = symbol,
                    Amount = 1000_00000000
                });
                result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

                var transferResult = await _tokenContractImpl.AdvanceResourceToken.SendAsync(
                    new AdvanceResourceTokenInput
                    {
                        ContractAddress = _acs8ContractA.Contract,
                        ResourceTokenSymbol = symbol,
                        Amount = 1000_00000000
                    });
                transferResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
                var rBalance = _tokenContract.GetUserBalance(_acs8ContractA.ContractAddress, symbol);
                rBalance.ShouldBe(beforeBalance + 1000_00000000);
            }
        }

        private async Task AdvanceResourceTokenOnSideChain()
        {
            foreach (var symbol in _resourceSymbol)
            {
                var beforeBalance = SideContractManager.Token.GetUserBalance(_acs8ContractA.ContractAddress, symbol);
                if (beforeBalance >= 2000_00000000) continue;

                var transferResult = await _sideTokenContractImpl.AdvanceResourceToken.SendAsync(
                    new AdvanceResourceTokenInput
                    {
                        ContractAddress = _acs8ContractA.Contract,
                        ResourceTokenSymbol = symbol,
                        Amount = 100_00000000
                    });
                transferResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
                var rBalance = SideContractManager.Token.GetUserBalance(_acs8ContractA.ContractAddress, symbol);
                rBalance.ShouldBe(beforeBalance + 100_00000000);
            }
        }

        private void TransferFewResource()
        {
            foreach (var symbol in _resourceSymbol)
            {
                var result = AsyncHelper.RunSync(() => _tokenConverterSub.Buy.SendAsync(new BuyInput
                {
                    Symbol = symbol,
                    Amount = 38_00024001
                }));
                result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
                _tokenContract.TransferBalance(InitAccount, _acs8ContractA.ContractAddress, 3800024001, symbol);
//                _tokenContract.TransferBalance(InitAccount, _acs8ContractB.ContractAddress, 100_00000000, symbol);
            }
        }

        private void InitializeFeesContract(TransactionFeesContract contract)
        {
            contract.ExecuteMethodWithResult(TxFeesMethod.InitializeFeesContract, contract.Contract);
        }

        private void GetMethodFee()
        {
            var fee = _tokenContract.CallViewMethod<MethodFees>(TokenMethod.GetMethodFee, new StringValue
            {
                Value = nameof(TokenMethod.Approve)
            });
            Logger.Info(JsonConvert.SerializeObject(fee));
        }

        private void CreateAndIssueToken(long amount, string symbol)
        {
            if (!_tokenContract.GetTokenInfo(symbol).Equals(new TokenInfo())) return;

            var result = _tokenContract.ExecuteMethodWithResult(TokenMethod.Create, new CreateInput
            {
                Issuer = InitAccount.ConvertAddress(),
                Symbol = symbol,
                Decimals = 8,
                IsBurnable = true,
                TokenName = $"{symbol} symbol",
                TotalSupply = 100000000_00000000
            });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            var balance = _tokenContract.GetUserBalance(InitAccount, symbol);
            var issueResult = _tokenContract.IssueBalance(InitAccount, InitAccount, amount, symbol);
            issueResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var afterBalance = _tokenContract.GetUserBalance(InitAccount, symbol);
            afterBalance.ShouldBe(balance + amount);
        }

        private void GetTestAccounts(int count)
        {
            var authority = new AuthorityManager(NodeManager);
            var miners = authority.GetCurrentMiners();
            var accounts = NodeManager.ListAccounts();
            var testUsers = accounts.FindAll(o => !miners.Contains(o));
            if (testUsers.Count >= count)
            {
                foreach (var acc in testUsers.Take(count)) _accountList.Add(acc);
            }
            else
            {
                foreach (var acc in testUsers) _accountList.Add(acc);

                var generateCount = count - testUsers.Count;
                for (var i = 0; i < generateCount; i++)
                {
                    var account = NodeManager.NewAccount();
                    _accountList.Add(account);
                }
            }
        }

        #endregion
    }
}