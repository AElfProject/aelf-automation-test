using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Acs1;
using AElf.Contracts.MultiToken;
using AElf.Contracts.Profit;
using AElf.Contracts.TestContract.TransactionFees;
using AElf.Contracts.TokenConverter;
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
using Shouldly;
using Volo.Abp.Threading;
using TokenInfo = AElf.Contracts.MultiToken.TokenInfo;

namespace AElf.Automation.Contracts.ScenarioTest
{
    [TestClass]
    public class TransactionFeeTest
    {
        private ILog Logger { get; set; }
        private INodeManager NodeManager { get; set; }
        private AuthorityManager AuthorityManager { get; set; }

        private TokenContract _tokenContract;
        private TokenContract _tokenTestContract;
        private GenesisContract _genesisContract;
        private TreasuryContract _treasury;
        private ProfitContract _profit;

        private TransactionFeesContract _acs8ContractA;
        private TransactionFeesContract _acs8ContractB;
        private TransactionFeesContractContainer.TransactionFeesContractStub _acs8SubA;
        private TransactionFeesContractContainer.TransactionFeesContractStub _acs8SubB;
        private TokenConverterContractContainer.TokenConverterContractStub _tokenConverterSub;
        private TokenContractContainer.TokenContractStub _tokenTestSub;
        private TokenContractImplContainer.TokenContractImplStub _tokenContractImpl;
        private Dictionary<SchemeType, Scheme> Schemes { get; set; }
        private string InitAccount { get; } = "28Y8JA1i2cN6oHvdv7EraXJr9a1gY6D1PpJXw9QtRMRwKcBQMK";
        private string TestAccount { get; } = "2oSMWm1tjRqVdfmrdL8dgrRvhWu1FP8wcZidjS6wPbuoVtxhEz";
        private static string RpcUrl { get; } = "192.168.197.14:8000";
        private string Symbol { get; } = "TEST";
        private long SymbolFee = 100000000;

        private List<string> _resourceSymbol = new List<string>
            {"READ", "WRITE", "STORAGE", "TRAFFIC"};

        [TestInitialize]
        public void Initialize()
        {
            Log4NetHelper.LogInit("ResourceTest");
            Logger = Log4NetHelper.GetLogger();
            NodeInfoHelper.SetConfig("nodes-env1-main");

            NodeManager = new NodeManager(RpcUrl);
            AuthorityManager = new AuthorityManager(NodeManager, InitAccount);
            _genesisContract = GenesisContract.GetGenesisContract(NodeManager, InitAccount);
            _tokenContract = _genesisContract.GetTokenContract(InitAccount);
            _tokenTestContract = new TokenContract(NodeManager, InitAccount,
                "uSXxaGWKDBPV6Z8EG8Et9sjaXhH1uMWEpVvmo2KzKEaueWzSe");
            _tokenTestSub = _tokenTestContract.GetTestStub<TokenContractContainer.TokenContractStub>(InitAccount);
            CreateAndIssueToken(1000_00000000);
            SetTokenContractMethodFee();
            _treasury = _genesisContract.GetTreasuryContract(InitAccount);
            _profit = _genesisContract.GetProfitContract(InitAccount);
            _profit.GetTreasurySchemes(_treasury.ContractAddress);
            Schemes = ProfitContract.Schemes;

                _tokenConverterSub = _genesisContract.GetTokenConverterStub(InitAccount);
            _acs8ContractA = new TransactionFeesContract(NodeManager, InitAccount,
                "q6B5hzdSMaXZqjrYHVakngmL1xfUoWyfQnDttf4ktxoRSTUC7");
            _acs8ContractB = new TransactionFeesContract(NodeManager, InitAccount,
                "Xg6cJsRnCuznxHC1JAyB8XSmxfDnCKTQeJN9fP4ca938MBYgU");
//           TransferFewResource();
//            InitializeFeesContract(_acs8ContractA);
//           InitializeFeesContract(_acs8ContractB);
            _acs8SubA =
                _acs8ContractA.GetTestStub<TransactionFeesContractContainer.TransactionFeesContractStub>(InitAccount);
            _acs8SubB =
                _acs8ContractB.GetTestStub<TransactionFeesContractContainer.TransactionFeesContractStub>(InitAccount);
        }

        #region ACS8

        [TestMethod]
        public async Task Acs8ContractTest()
        {
            var fees = new Dictionary<string, long>();

            await AdvanceResourceToken();
            var treasuryAmount = _treasury.GetCurrentTreasuryBalance();
            Logger.Info($"treasury  balance : {treasuryAmount}");
            var cpuResult = await _acs8SubA.ReadCpuCountTest.SendAsync(new Int32Value {Value = 20});
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
        }

        [TestMethod]
        public async Task Acs8ContractTest_Owned()
        {
//            TransferFewResource();
            var fees = new Dictionary<string, long>();
            var treasuryAmount = _treasury.GetCurrentTreasuryBalance();
            Logger.Info($"treasury  balance : {treasuryAmount}");
            var cpuResult = await _acs8SubB.ReadCpuCountTest.SendAsync(new Int32Value {Value = 20});

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
                var balance = _tokenContract.GetUserBalance(_acs8ContractB.ContractAddress, symbol);
                // balance.ShouldBe(0);
                Logger.Info($"Contract {symbol} balance : {balance}");
            }
        }

        #endregion

        #region ACS1

        [TestMethod]
        public async Task Acs1ContractTest()
        {
            var balance = _tokenContract.GetUserBalance(InitAccount, Symbol);
            var result = await _tokenTestSub.Approve.SendAsync(new ApproveInput
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
            afterBalance.ShouldBe(balance-SymbolFee);
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
                if(miner.Equals(InitAccount)) continue;
                var amount = _profit.GetProfitAmount(miner, schemeId);
                profitsInfo.Add(miner, amount);
            }

            foreach (var miner in miners)
            {
                if(miner.Equals(InitAccount)) continue;
                var minerBalance = _tokenContract.GetUserBalance(miner);
                _profit.SetAccount(miner);
                var profitResult = _profit.ExecuteMethodWithResult(ProfitMethod.ClaimProfits, new ClaimProfitsInput
                {
                    SchemeId = schemeId,
                    Symbol = NodeOption.NativeTokenSymbol
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

            var miners = AuthorityManager.GetCurrentMiners();
            foreach (var miner in miners)
            {
                var balance = _tokenContract.GetUserBalance(miner, "ELF");
                Logger.Info($"{miner} balance : {balance}");
            }

            var userBalance = _tokenContract.GetUserBalance(InitAccount, Symbol);
            Logger.Info($"{InitAccount} TEST balance : {userBalance}");

            var userElfBalance = _tokenContract.GetUserBalance(InitAccount, "ELF");
            Logger.Info($"{InitAccount} ELF balance : {userElfBalance}");
        }

        [TestMethod]
        public void CheckSchemesAmount()
        {
            Logger.Info($"Treasury: ");
            foreach (var scheme in Schemes)
            {
                var period = AuthorityManager.GetPeriod();
                var schemesId = Schemes[scheme.Key].SchemeId;
                var address = _profit.GetSchemeAddress(schemesId, period-1);
                var balance = _tokenContract.GetUserBalance(address.GetFormatted());
                var testBalance = _tokenContract.GetUserBalance(address.GetFormatted(),Symbol);

                var amount = _profit.GetProfitAmount(InitAccount, schemesId);
                Logger.Info($"{scheme.Key} ELF balance is :{balance} TEST balance is {testBalance}\n amount is {amount}");
            }
        }

        #endregion

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
                _tokenContract.TransferBalance(InitAccount, _acs8ContractA.ContractAddress, 1000_00000000, symbol);
                var rBalance = _tokenContract.GetUserBalance(_acs8ContractA.ContractAddress, symbol);
                rBalance.ShouldBe(beforeBalance + 1000_00000000);
            }
        }

        private void TransferFewResource()
        {
            foreach (var symbol in _resourceSymbol)
            {
                var result = AsyncHelper.RunSync(() => _tokenConverterSub.Buy.SendAsync(new BuyInput
                {
                    Symbol = symbol,
                    Amount = 200_00000000
                }));
                result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
                _tokenContract.TransferBalance(InitAccount, _acs8ContractA.ContractAddress, 50_00000000, symbol);
                _tokenContract.TransferBalance(InitAccount, _acs8ContractB.ContractAddress, 50_00000000, symbol);
            }
        }

        private void InitializeFeesContract(TransactionFeesContract contract)
        {
            contract.ExecuteMethodWithResult(TxFeesMethod.InitializeFeesContract, contract.Contract);
        }

        private void SetTokenContractMethodFee()
        {
            var fee = _tokenTestContract.CallViewMethod<MethodFees>(TokenMethod.GetMethodFee, new StringValue
            {
                Value = nameof(TokenMethod.Approve)
            });
            if (fee.Fees.Count > 0) return;
            var organization =
                _tokenTestContract.CallViewMethod<AuthorityInfo>(TokenMethod.GetMethodFeeController, new Empty())
                    .OwnerAddress;
            var input = new MethodFees
            {
                MethodName = nameof(TokenMethod.Approve),
                Fees =
                {
                    new MethodFee
                    {
                        BasicFee = SymbolFee,
                        Symbol = "TEST"
                    }
                }
            };
            var result = AuthorityManager.ExecuteTransactionWithAuthority(_tokenTestContract.ContractAddress,
                "SetMethodFee", input,
                InitAccount, organization);
            result.Status.ShouldBe(TransactionResultStatus.Mined);
        }

        private void CreateAndIssueToken(long amount)
        {
            if (!_tokenContract.GetTokenInfo(Symbol).Equals(new TokenInfo())) return;

            var result = _tokenContract.ExecuteMethodWithResult(TokenMethod.Create, new CreateInput
            {
                Issuer = AddressHelper.Base58StringToAddress(InitAccount),
                Symbol = Symbol,
                Decimals = 8,
                IsBurnable = true,
                TokenName = "TEST symbol",
                TotalSupply = 100000000_00000000,
                IsProfitable = true
            });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var testResult = _tokenTestContract.ExecuteMethodWithResult(TokenMethod.Create, new CreateInput
            {
                Issuer = AddressHelper.Base58StringToAddress(InitAccount),
                Symbol = Symbol,
                Decimals = 8,
                IsBurnable = true,
                TokenName = "TEST symbol",
                TotalSupply = 100000000_00000000,
                IsProfitable = true
            });

            var balance = _tokenContract.GetUserBalance(InitAccount, Symbol);
            var issueResult = _tokenContract.IssueBalance(InitAccount, InitAccount, amount, Symbol);
            issueResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var afterBalance = _tokenContract.GetUserBalance(InitAccount, Symbol);
            afterBalance.ShouldBe(balance + amount);
        }

        #endregion
    }
}