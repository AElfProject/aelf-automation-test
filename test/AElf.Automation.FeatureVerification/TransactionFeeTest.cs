using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Acs1;
using AElf.Standards.ACS3;
using AElf.Contracts.Association;
using AElf.Contracts.MultiToken;
using AElf.Contracts.Profit;
using AElf.Contracts.TestContract.TransactionFees;
using AElf.Contracts.TokenConverter;
using AElf.CSharp.Core.Extension;
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

        private Dictionary<SchemeType, Scheme> Schemes { get; set; }
        private string InitAccount { get; } = "28Y8JA1i2cN6oHvdv7EraXJr9a1gY6D1PpJXw9QtRMRwKcBQMK";
        private string TestAccount { get; } = "Nhmm7kab6j6voDfzmeVN6BCo5Te8oTi5PW9qNeyT4ZnxnGJCa";
        private static string RpcUrl { get; } = "192.168.197.44:8000";
        private static string SideRpcUrl { get; } = "192.168.197.44:8002";

        private string Symbol { get; } = "TEST";
        private long SymbolFee = 1_00000000;

        private List<string> _resourceSymbol = new List<string>
            {"READ", "WRITE", "STORAGE", "TRAFFIC"};

        [TestInitialize]
        public void Initialize()
        {
            Log4NetHelper.LogInit("TransactionFeeTest");
            Logger = Log4NetHelper.GetLogger();
            NodeInfoHelper.SetConfig("nodes-env2-main");

            NodeManager = new NodeManager(RpcUrl);
            SideNodeManager = new NodeManager(SideRpcUrl);
            ContractManager = new ContractManager(SideNodeManager, InitAccount);
            SideContractManager = new ContractManager(SideNodeManager, InitAccount);
            AuthorityManager = new AuthorityManager(NodeManager, InitAccount);
            SideAuthority = new AuthorityManager(SideNodeManager, InitAccount);

            _genesisContract = GenesisContract.GetGenesisContract(NodeManager, InitAccount);
            _tokenContract = _genesisContract.GetTokenContract(InitAccount);
            _tokenContractImpl = _genesisContract.GetTokenImplStub();

            _treasury = _genesisContract.GetTreasuryContract(InitAccount);
            _profit = _genesisContract.GetProfitContract(InitAccount);
            _profit.GetTreasurySchemes(_treasury.ContractAddress);
            Schemes = ProfitContract.Schemes;

            _tokenConverterSub = _genesisContract.GetTokenConverterStub(InitAccount);
            _acs8ContractA = new TransactionFeesContract(SideNodeManager, InitAccount);
            _sideTokenContractImpl = SideContractManager.TokenImplStub;
//            _acs8ContractB = new TransactionFeesContract(NodeManager, InitAccount,
//                "Xg6cJsRnCuznxHC1JAyB8XSmxfDnCKTQeJN9fP4ca938MBYgU");
//           TransferFewResource();
            InitializeFeesContract(_acs8ContractA);
//           InitializeFeesContract(_acs8ContractB);
            _acs8SubA =
                _acs8ContractA.GetTestStub<TransactionFeesContractContainer.TransactionFeesContractStub>(InitAccount);
//            _acs8SubB =
//                _acs8ContractB.GetTestStub<TransactionFeesContractContainer.TransactionFeesContractStub>(InitAccount);
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

            var cpuResult = await _acs8SubA.ReadCpuCountTest.SendAsync(new Int32Value {Value = 19});
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
                        new Int32Value {Value = (i + 20)});
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
        public void SetTokenContractMethodFee()
        {
            var symbol = "ELF";
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
            var result = _tokenContract.TransferBalance(InitAccount, TestAccount, 100, "TEST");
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
                ContractMethodName = nameof(AssociationContractContainer.AssociationContractStub.CreateProposal),
                ExpiredTime = KernelHelper.GetUtcNow().AddHours(1)
            };
            var parliamentCreateProposal =
                await ContractManager.ParliamentAuthStub.CreateProposal.SendAsync(createProposalInput);
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
                    Amount = 3800024001
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

        #endregion
    }
}