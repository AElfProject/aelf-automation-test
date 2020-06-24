using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using AElf.Contracts.TokenConverter;
using AElf.Types;
using AElfChain.Common;
using AElfChain.Common.Contracts;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using Google.Protobuf.WellKnownTypes;
using log4net;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace AElf.Automation.Contracts.ScenarioTest
{
    [TestClass]
    public class TokenContractTest
    {
        private readonly List<string> ResourceSymbol = new List<string>
            {"CPU", "NET", "DISK", "RAM", "READ", "WRITE", "STORAGE", "TRAFFIC","ELF"};

        private TokenContractContainer.TokenContractStub _bpTokenSub;
        private GenesisContract _genesisContract;
        private ParliamentContract _parliamentContract;
        private TokenConverterContractContainer.TokenConverterContractStub _testTokenConverterSub;
        private TokenContractContainer.TokenContractStub _testTokenSub;

        private TokenContract _tokenContract;
        private TokenConverterContract _tokenConverterContract;
        private TokenConverterContractContainer.TokenConverterContractStub _tokenConverterSub;
        private TokenContractContainer.TokenContractStub _tokenSub;
        private ILog Logger { get; set; }
        private INodeManager NodeManager { get; set; }
        private AuthorityManager AuthorityManager { get; set; }

        private string InitAccount { get; } = "28Y8JA1i2cN6oHvdv7EraXJr9a1gY6D1PpJXw9QtRMRwKcBQMK";
        private string BpAccount { get; } = "2ZYyxEH6j8zAyJjef6Spa99Jx2zf5GbFktyAQEBPWLCvuSAn8D";
        private string TestAccount { get; } = "2oSMWm1tjRqVdfmrdL8dgrRvhWu1FP8wcZidjS6wPbuoVtxhEz";
        
        private static string RpcUrl { get; } = "192.168.197.42:8000";
        private string Symbol { get; } = "TEST";
        private string Symbol1 { get; } = "NOPROFIT";
        private string Symbol2 { get; } = "NOWHITE";
        
        [TestInitialize]
        public void Initialize()
        {
            Log4NetHelper.LogInit("TokenContractTest");
            Logger = Log4NetHelper.GetLogger();
            NodeInfoHelper.SetConfig("nodes-env1-main");

            NodeManager = new NodeManager(RpcUrl);
            AuthorityManager = new AuthorityManager(NodeManager, InitAccount);
            _genesisContract = GenesisContract.GetGenesisContract(NodeManager, InitAccount);
            _tokenContract = _genesisContract.GetTokenContract(InitAccount);
            _parliamentContract = _genesisContract.GetParliamentContract(InitAccount);
            _tokenConverterContract = _genesisContract.GetTokenConverterContract(InitAccount);

            _tokenSub = _genesisContract.GetTokenStub(InitAccount);
            _bpTokenSub = _genesisContract.GetTokenStub(BpAccount);
            _testTokenSub = _genesisContract.GetTokenStub(TestAccount);

            _tokenConverterSub = _genesisContract.GetTokenConverterStub(InitAccount);
            _testTokenConverterSub = _genesisContract.GetTokenConverterStub(TestAccount);
        }

        [TestMethod]
        public async Task NewStubTest_Call()
        {
            var tokenContractAddress =
                ("WnV9Gv3gioSh3Vgaw8SSB96nV8fWUNxuVozCf6Y14e7RXyGaM").ConvertAddress();
            var tester = new ContractTesterFactory(NodeManager);
            var tokenStub = tester.Create<TokenContractContainer.TokenContractStub>(tokenContractAddress, InitAccount);
            var tokenInfo = await tokenStub.GetTokenInfo.CallAsync(new GetTokenInfoInput
            {
                Symbol = NodeOption.NativeTokenSymbol
            });
            tokenInfo.ShouldNotBeNull();
        }

        [TestMethod]
        public async Task ChangeIssuer()
        {
            await CreateToken(Symbol, long.MaxValue);
            var tokenInfo = _tokenContract.GetTokenInfo(Symbol);
            var sub = _genesisContract.GetTokenStub(tokenInfo.Issuer.ToBase58());
            var result = await sub.ChangeTokenIssuer.SendAsync(new ChangeTokenIssuerInput
            {
                NewTokenIssuer = TestAccount.ConvertAddress(),
                Symbol = tokenInfo.Symbol
            });
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            tokenInfo = _tokenContract.GetTokenInfo(Symbol);
            tokenInfo.Issuer.ShouldBe(TestAccount.ConvertAddress());
            _tokenContract.SetAccount(TestAccount);
            var issue = _tokenContract.IssueBalance(TestAccount, InitAccount, 1000, Symbol);
            issue.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            var balance = _tokenContract.GetUserBalance(InitAccount, Symbol);
            balance.ShouldBe(1000+1000000_0000);
        }

        [TestMethod]
        public async Task BuySideChainToken()
        {
            var balance = await _testTokenSub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = TestAccount.ConvertAddress(),
                Symbol = NodeManager.GetNativeTokenSymbol()
            });
            var otherTokenBalance = await _testTokenSub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = TestAccount.ConvertAddress(),
                Symbol = Symbol
            });

            Logger.Info($"user ELF balance is {balance} user EPC balance is {otherTokenBalance}");

            var result = await _testTokenConverterSub.Buy.SendAsync(new BuyInput
            {
                Amount = 1_00000000,
                Symbol = Symbol
            });
            var size = result.Transaction.CalculateSize();
            Logger.Info($"transfer size is: {size}");

            var afterBalance = await _testTokenSub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = TestAccount.ConvertAddress(),
                Symbol = NodeManager.GetNativeTokenSymbol()
            });

            var afterOtherTokenBalance = await _testTokenSub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = InitAccount.ConvertAddress(),
                Symbol = Symbol
            });

            Logger.Info(
                $"After buy token, user ELF balance is {afterBalance} user {Symbol} balance is {afterOtherTokenBalance}");
        }

        [TestMethod]
        public async Task AddListConnector()
        {
            var list = new List<string>(){Symbol,Symbol1,Symbol2};
            foreach (var symbol in list)
            {
                await AddConnector(symbol);
            }
        }

        [TestMethod]
        public async Task AddConnector(string symbol)
        {
            var amount = 80000000_0000000000;
            await CreateToken(symbol,amount);
            await IssueToken(symbol, amount);
            var input = new PairConnectorParam
            {
                NativeWeight = "0.05",
                ResourceWeight = "0.05",
                ResourceConnectorSymbol = symbol,
                NativeVirtualBalance = 100000000_00000000,
            };
            var organization = _parliamentContract.GetGenesisOwnerAddress();
            var connectorController = await _tokenConverterSub.GetControllerForManageConnector.CallAsync(new Empty());
            connectorController.ContractAddress.ShouldBe(_parliamentContract.Contract);
            connectorController.OwnerAddress.ShouldBe(organization);
            
            var proposal = _parliamentContract.CreateProposal(_tokenConverterContract.ContractAddress,
                nameof(TokenConverterMethod.AddPairConnector), input, organization, BpAccount);
            var miners = AuthorityManager.GetCurrentMiners();
            _parliamentContract.MinersApproveProposal(proposal, miners);
            var result = _parliamentContract.ReleaseProposal(proposal, BpAccount);
            result.Status.ShouldBe(TransactionResultStatus.Mined);

            var ELFamout = await GetNeededDeposit(amount,symbol);
            Logger.Info($"Need ELF : {ELFamout}");
            (await _bpTokenSub.Approve.SendAsync(new ApproveInput
            {
                Spender = _tokenConverterContract.Contract,
                Symbol = "ELF",
                Amount = ELFamout
            })).TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            (await _bpTokenSub.Approve.SendAsync(new ApproveInput
            {
                Spender = _tokenConverterContract.Contract,
                Symbol = symbol,
                Amount = amount
            })).TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var buildInput = new ToBeConnectedTokenInfo
            {
                TokenSymbol = symbol,
                AmountToTokenConvert = amount
            };

            var enableConnector = await _tokenConverterSub.EnableConnector.SendAsync(buildInput);
            enableConnector.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var tokenConverterBalance = await _tokenSub.GetBalance.CallAsync(new GetBalanceInput
            {
                Symbol = symbol,
                Owner = _tokenConverterContract.Contract
            });
            tokenConverterBalance.Balance.ShouldBe(amount);
        }

        [TestMethod]
        public void UpdateConnector()
        {
            var input = new Connector
            {
                Symbol = Symbol,
                VirtualBalance = 100_0000_00000000,
            };

            var organization = _parliamentContract.GetGenesisOwnerAddress();
            var proposal = _parliamentContract.CreateProposal(_tokenConverterContract.ContractAddress,
                nameof(TokenConverterMethod.UpdateConnector), input, organization, BpAccount);
            var miners = AuthorityManager.GetCurrentMiners();
            _parliamentContract.MinersApproveProposal(proposal, miners);
            var result = _parliamentContract.ReleaseProposal(proposal, BpAccount);
            result.Status.ShouldBe(TransactionResultStatus.Mined);
        }

        [TestMethod]
        public async Task EnableConnector()
        {
            var buildInput = new ToBeConnectedTokenInfo
            {
                TokenSymbol = Symbol,
                AmountToTokenConvert = 0
            };

            var enableConnector = await _tokenConverterSub.EnableConnector.SendAsync(buildInput);
            enableConnector.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        }

        [TestMethod]
        public async Task GetCalculateFeeCoefficientOfDeveloper()
        {
            /*
            [pbr::OriginalName("Cpu")] Cpu = 0,
            [pbr::OriginalName("Sto")] Sto = 1,
            [pbr::OriginalName("Ram")] Ram = 2,
            [pbr::OriginalName("Net")] Net = 3,
             */
            var result = await _tokenSub.GetCalculateFeeCoefficientsForContract.CallAsync(new Int32Value {Value = 0});
            Logger.Info($"{result}");

            var result1 = await _tokenSub.GetCalculateFeeCoefficientsForContract.CallAsync(new Int32Value {Value = 1});
            Logger.Info($"{result1}");

            var result2 = await _tokenSub.GetCalculateFeeCoefficientsForContract.CallAsync(new Int32Value {Value = 2});
            Logger.Info($"{result2}");

            var result3 = await _tokenSub.GetCalculateFeeCoefficientsForContract.CallAsync(new Int32Value {Value = 3});
            Logger.Info($"{result3}");
        }

        [TestMethod]
        public async Task GetCalculateFeeCoefficientOfUser()
        {
            var result = await _tokenSub.GetCalculateFeeCoefficientsForSender.CallAsync(new Empty());
            Logger.Info($"{result}");
        }

        [TestMethod]
        public async Task GetBasicToken()
        {
            var result = await _tokenConverterSub.GetBaseTokenSymbol.CallAsync(new Empty());
            Logger.Info($"{result.Symbol}");
        }

        [TestMethod]
        public async Task GetManagerAddress()
        {
            var manager = await _tokenConverterSub.GetControllerForManageConnector.CallAsync(new Empty());
            Logger.Info($"manager is {manager.OwnerAddress}");
            var organization = _parliamentContract.GetGenesisOwnerAddress();
            Logger.Info($"organization is {organization}");
        }

        [TestMethod]
        public async Task GetConnector()
        {
            var result = await _tokenConverterSub.GetPairConnector.CallAsync(new TokenSymbol {Symbol = "STA"});
            Logger.Info($"{result}");
        }

        [TestMethod]
        [DataRow("2WHXRoLRjbUTDQsuqR5CntygVfnDb125qdJkudev4kVNbLhTdG")]
        public async Task Acs8ContractTest(string acs8Contract)
        {
            foreach (var s in ResourceSymbol)
            {
                var balance = await _tokenSub.GetBalance.CallAsync(new GetBalanceInput
                    {Owner = acs8Contract.ConvertAddress(), Symbol = s});
                Logger.Info($"{s} balance is {balance.Balance}");
            }
        }

        private async Task CreateToken(string symbol,long amount)
        {
            if (!_tokenContract.GetTokenInfo(symbol).Equals(new TokenInfo())) return;
            var result = await _tokenSub.Create.SendAsync(new CreateInput
            {
                Issuer = InitAccount.ConvertAddress(),
                Symbol = symbol,
                Decimals = 8,
                IsBurnable = true,
                TokenName = "TEST symbol",
                TotalSupply = amount
            });

            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        }

        private async Task IssueToken(string symbol, long amount)
        {
            var issueResult = await _tokenSub.Issue.SendAsync(new IssueInput
            {
                Amount = amount,
                Symbol = symbol,
                To =  BpAccount.ConvertAddress()
            });
            issueResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var balance = _tokenContract.GetUserBalance(BpAccount, Symbol);
            balance.ShouldBe(amount);
        }

        private async Task<long> GetNeededDeposit(long amount,string symbol)
        {
            var result = await _tokenConverterSub.GetNeededDeposit.CallAsync(new ToBeConnectedTokenInfo
            {
                TokenSymbol = symbol,
                AmountToTokenConvert = amount
            });
            return result.NeedAmount;
        }
    }
}