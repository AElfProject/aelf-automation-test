using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using AElf.Contracts.TokenConverter;
using AElf.CSharp.Core;
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
            {"CPU", "NET", "DISK", "RAM", "READ", "WRITE", "STORAGE", "TRAFFIC","ELF","SHARE"};

        private TokenContractContainer.TokenContractStub _bpTokenSub;
        private GenesisContract _genesisContract;
        private ParliamentContract _parliamentContract;
        private TokenConverterContractContainer.TokenConverterContractStub _testTokenConverterSub;
        private TokenContractContainer.TokenContractStub _testTokenSub;

        private TokenContract _tokenContract;
        private TokenConverterContract _tokenConverterContract;
        private TokenConverterContractContainer.TokenConverterContractStub _tokenConverterSub;
        private TokenContractContainer.TokenContractStub _tokenSub;
        private  ILog Logger { get; set; }
        private INodeManager NodeManager { get; set; }
        private AuthorityManager AuthorityManager { get; set; }

        private string InitAccount { get; } = "28Y8JA1i2cN6oHvdv7EraXJr9a1gY6D1PpJXw9QtRMRwKcBQMK";
        private string BpAccount { get; } = "2ZYyxEH6j8zAyJjef6Spa99Jx2zf5GbFktyAQEBPWLCvuSAn8D";
        private string TestAccount { get; } = "YF8o6ytMB7n5VF9d1RDioDXqyQ9EQjkFK3AwLPCH2b9LxdTEq";
        private string Account { get; } = "2a6MGBRVLPsy6pu4SVMWdQqHS5wvmkZv8oas9srGWHJk7GSJPV";
        
        private static string RpcUrl { get; } = "192.168.197.47:8000";
        private string Symbol { get; } = "TEST";
        private string Symbol1 { get; } = "NOPROFIT";
        private string Symbol2 { get; } = "NOWHITE";
        

        [TestInitialize]
        public void Initialize()
        {
            Log4NetHelper.LogInit("TokenContractTest");
            Logger = Log4NetHelper.GetLogger();
            NodeInfoHelper.SetConfig("nodes-env2-main");

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
            var symbol = Symbol;
            var amount = 1000_00000000;
            var tokenInfo = _tokenContract.GetTokenInfo(symbol);
            var sub = _genesisContract.GetTokenStub(tokenInfo.Issuer.ToBase58());
            var result = await sub.ChangeTokenIssuer.SendAsync(new ChangeTokenIssuerInput
            {
                NewTokenIssuer = TestAccount.ConvertAddress(),
                Symbol = tokenInfo.Symbol
            });
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            tokenInfo = _tokenContract.GetTokenInfo(symbol);
            tokenInfo.Issuer.ShouldBe(TestAccount.ConvertAddress());
            var balance = _tokenContract.GetUserBalance(InitAccount, symbol);
            _tokenContract.SetAccount(TestAccount);
            var issue = _tokenContract.IssueBalance(TestAccount, InitAccount, amount, symbol);
            issue.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            var afterBalance = _tokenContract.GetUserBalance(InitAccount, symbol);
            afterBalance.ShouldBe(balance+amount);
        }

        [TestMethod]
        public async Task TokenCreateTest()
        {
            var symbol = Symbol;
            var amount = 5000000000_000000000;
            var issued = 4000000000_00000000;
            var burned = 1_00000000;
            await CreateToken(symbol,amount);
            var tokenInfo = _tokenContract.GetTokenInfo(symbol);
            await IssueToken(InitAccount,symbol, issued);
            await _tokenSub.Burn.SendAsync(new BurnInput{Symbol = symbol, Amount = burned});
            
            var afterTokenInfo = _tokenContract.GetTokenInfo(symbol);
            afterTokenInfo.TotalSupply.ShouldBe(amount);
            afterTokenInfo.Supply.ShouldBe(issued - burned);
            afterTokenInfo.Issued.ShouldBe(issued + tokenInfo.Issued);
            Logger.Info(afterTokenInfo);
        }

        [TestMethod]
        public async Task AddListConnector()
        {
            var list = new List<string>(){Symbol};
            foreach (var symbol in list)
            {
                await AddConnector(symbol);
            }
        }

        public async Task AddConnector(string symbol)
        {
            var amount = 80000000_0000000000;
            await CreateToken(symbol,amount);
            await IssueToken(InitAccount,symbol, amount);
            var tokenInfo = _tokenContract.GetTokenInfo(symbol);
            Logger.Info(tokenInfo);
            var input = new PairConnectorParam
            {
                NativeWeight = "0.05",
                ResourceWeight = "0.01",
                ResourceConnectorSymbol = symbol,
                NativeVirtualBalance = 100000000_00000000
            };
            var organization = _parliamentContract.GetGenesisOwnerAddress();
            var connectorController = await _tokenConverterSub.GetControllerForManageConnector.CallAsync(new Empty());
            connectorController.ContractAddress.ShouldBe(_parliamentContract.Contract);
            connectorController.OwnerAddress.ShouldBe(organization);
            
            var proposal = _parliamentContract.CreateProposal(_tokenConverterContract.ContractAddress,
                nameof(TokenConverterMethod.AddPairConnector), input, organization, InitAccount);
            var miners = AuthorityManager.GetCurrentMiners();
            _parliamentContract.MinersApproveProposal(proposal, miners);
            var result = _parliamentContract.ReleaseProposal(proposal, InitAccount);
            result.Status.ShouldBe(TransactionResultStatus.Mined);
        }

        [TestMethod]
        public void UpdateConnector()
        {
            var input = new Connector
            {
                Symbol = Symbol,
                VirtualBalance = 100000000_00000000,
                Weight = "0.1"
            };

            var organization = _parliamentContract.GetGenesisOwnerAddress();
            var proposal = _parliamentContract.CreateProposal(_tokenConverterContract.ContractAddress,
                nameof(TokenConverterMethod.UpdateConnector), input, organization, InitAccount);
            var miners = AuthorityManager.GetCurrentMiners();
            _parliamentContract.MinersApproveProposal(proposal, miners);
            var result = _parliamentContract.ReleaseProposal(proposal, InitAccount);
            result.Status.ShouldBe(TransactionResultStatus.Mined);
        }

        [TestMethod]
        public async Task EnableConnector()
        {
            var amount = 4500000000_000000000;
            var symbol = "TEST";
            await IssueToken(InitAccount,symbol, amount);
            var ELFamout = await GetNeededDeposit(amount,symbol);
            Logger.Info($"Need ELF : {ELFamout}");
            if (ELFamout > 0)
            {
                (await _tokenSub.Approve.SendAsync(new ApproveInput
                {
                    Spender = _tokenConverterContract.Contract,
                    Symbol = "ELF",
                    Amount = ELFamout
                })).TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            }
            
            (await _tokenSub.Approve.SendAsync(new ApproveInput
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
        public void BuyConnectSymbol()
        {
            var symbol = "TRAFFIC";
            var amount = 10000_00000000;
            var amountToPay = GetPayAmount(symbol, amount);
            var rate = decimal.Parse(_tokenConverterContract.GetFeeRate());
            var fee = Convert.ToInt64(amountToPay * rate);
            var donateFee = fee.Div(2);
            var burnFee = fee.Sub(donateFee); 
            
            _tokenContract.TransferBalance(InitAccount, Account, 1000_00000000, "ELF");
            var balance = _tokenContract.GetUserBalance(Account);
            var resBalance = _tokenContract.GetUserBalance(Account,symbol);

            var tokenConverterBalance = _tokenContract.GetUserBalance(_tokenConverterContract.ContractAddress);
            var treasury = _genesisContract.GetTreasuryContract();
            var dividends = treasury.GetCurrentTreasuryBalance();
            var treasuryDonate = dividends.Value["ELF"];
            var tokenInfo = _tokenContract.GetTokenInfo("ELF");
            
            Logger.Info($"amountToPay={amountToPay}, fee={fee}, donateFee={donateFee}, burnFee={burnFee}");
            Logger.Info($"tokenConverterBalance={tokenConverterBalance},user balance={balance}, treasuryDonate={treasuryDonate}");

            var result = _tokenConverterContract.Buy(Account, symbol, amount);
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var sizeFee = result.GetDefaultTransactionFee();
            var burnAmount = sizeFee.Div(10);
            var transferAmount = sizeFee.Sub(burnAmount);

            NodeManager.WaitOneBlock(result.BlockNumber);
            
            var afterBalance = _tokenContract.GetUserBalance(Account);
            var afterResBalance = _tokenContract.GetUserBalance(Account,symbol);

            var afterTokenConverterBalance = _tokenContract.GetUserBalance(_tokenConverterContract.ContractAddress);
            var afterDividends = treasury.GetCurrentTreasuryBalance();
            var afterTreasuryDonate = afterDividends.Value["ELF"];
            var afterTokenInfo = _tokenContract.GetTokenInfo("ELF");

            afterResBalance.ShouldBe(resBalance + amount);
            afterBalance.ShouldBe(balance - amountToPay - fee - sizeFee);
            afterTokenConverterBalance.ShouldBe(tokenConverterBalance  + amountToPay);
            afterTreasuryDonate.ShouldBe(treasuryDonate + transferAmount + donateFee);
            afterTokenInfo.Supply.ShouldBe(tokenInfo.Supply - burnAmount - burnFee);
            
            Logger.Info($"sizeFee={sizeFee}, burnAmount={burnAmount}, transferAmount={transferAmount}, afterTreasuryDonate={afterTreasuryDonate}");
            Logger.Info($"afterTokenConverterBalance={afterTokenConverterBalance}, balance={afterBalance}");
        }


        [TestMethod]
        public async Task GetCalculateFeeCoefficientOfDeveloper()
        {
            /*
            [pbr::OriginalName("READ")] Read = 0,
            [pbr::OriginalName("STORAGE")] Storage = 1,
            [pbr::OriginalName("WRITE")] Write = 2,
            [pbr::OriginalName("TRAFFIC")] Traffic = 3,
            [pbr::OriginalName("TX")] Tx = 4,
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
        public async Task ChangeManagerAddress()
        {
            var manager = await _tokenConverterSub.GetControllerForManageConnector.CallAsync(new Empty());
            var proposer = AuthorityManager.GetCurrentMiners().First();
            Logger.Info($"manager is {manager.OwnerAddress}");
            var association = _genesisContract.GetAssociationAuthContract();
            var newController = AuthorityManager.CreateAssociationOrganization();
            var input = new AuthorityInfo
            {
                OwnerAddress = newController,
                ContractAddress = association.Contract
            };

            var change = AuthorityManager.ExecuteTransactionWithAuthority(_tokenConverterContract.ContractAddress,
                nameof(TokenConverterMethod.ChangeConnectorController), input, proposer,
                manager.OwnerAddress);
            change.Status.ShouldBe(TransactionResultStatus.Mined);
            var newManager = await _tokenConverterSub.GetControllerForManageConnector.CallAsync(new Empty());
            newManager.ContractAddress.ShouldBe(association.Contract);
            newManager.OwnerAddress.ShouldBe(newController);
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
        public async Task GetNeededDepositTest()
        {
            var amount = 4500000000_000000000;
            var symbol = Symbol;
            var deposit = await GetNeededDeposit(amount,symbol);
            Logger.Info($"{deposit}");
        }

        [TestMethod]
        public async Task CheckPrice()
        {
            var symbol = "CPU";
            var amount = 1_00000000;

            var result = await _tokenConverterSub.GetPairConnector.CallAsync(new TokenSymbol {Symbol = symbol});
            var fromConnectorWeight = decimal.Parse(result.DepositConnector.Weight);
            var toConnectorWeight = decimal.Parse(result.ResourceConnector.Weight);
            
            var amountToPay = BancorHelper.GetAmountToPayFromReturn(
                GetSelfBalance(result.DepositConnector,result.DepositConnector.RelatedSymbol), fromConnectorWeight,
                GetSelfBalance(result.ResourceConnector,symbol), toConnectorWeight,
                amount);
            var rate = decimal.Parse(_tokenConverterContract.GetFeeRate());
            var fee = Convert.ToInt64(amountToPay * rate);
            var amountToPayPlusFee = amountToPay.Add(fee);
            
            Logger.Info($"amountToPay: {amountToPay} fee: {fee}, amountToPayPlusFee {amountToPayPlusFee}");
        }

        private long GetPayAmount(string symbol, long amount)
        {
            var result = _tokenConverterContract.GetPairConnector(symbol);
            var fromConnectorWeight = decimal.Parse(result.DepositConnector.Weight);
            var toConnectorWeight = decimal.Parse(result.ResourceConnector.Weight);
            
            var amountToPay = BancorHelper.GetAmountToPayFromReturn(
                GetSelfBalance(result.DepositConnector,symbol), fromConnectorWeight,
                GetSelfBalance(result.ResourceConnector,symbol), toConnectorWeight,
                amount);
            
            return amountToPay;
        }

        [TestMethod]
        public async Task Check()
        {
            var amount = 70000000000_00000000L;
            var symbol = "RES";
            var tokenInfo = _tokenContract.GetTokenInfo(symbol);
            var balance = _tokenContract.GetUserBalance(_tokenConverterContract.ContractAddress, symbol);
            var amountOutOfTokenConvert = tokenInfo.TotalSupply - balance - amount;

            var result = await _tokenConverterSub.GetPairConnector.CallAsync(new TokenSymbol {Symbol = symbol});
            var fb = result.DepositConnector.VirtualBalance;
            var tb = result.ResourceConnector.IsVirtualBalanceEnabled ? result.ResourceConnector.VirtualBalance.Add(tokenInfo.TotalSupply)
                : tokenInfo.TotalSupply;
            var fromConnectorWeight = decimal.Parse(result.DepositConnector.Weight);
            var toConnectorWeight = decimal.Parse(result.ResourceConnector.Weight);
            decimal bt = tb;
            decimal a = amountOutOfTokenConvert;
            decimal wf = fromConnectorWeight;
            decimal wt = toConnectorWeight;
            decimal x = bt / (bt - a);
            decimal y = wt / wf;
            
            var needDeposit =
                BancorHelper.GetAmountToPayFromReturn(fb, fromConnectorWeight,
                    tb, toConnectorWeight, amountOutOfTokenConvert);
            Logger.Info(needDeposit);
        }
        
        public long GetSelfBalance(Connector connector,string symbol)
        {
            long realBalance;
            if (connector.IsDepositAccount)
            {
                var deposit = _tokenConverterContract.GetDepositConnectorBalance(symbol);
                 var virtualBalance = connector.VirtualBalance;
                 realBalance = deposit - virtualBalance;
            }
            else
            {
                realBalance = _tokenContract.GetUserBalance(_tokenConverterContract.ContractAddress, connector.Symbol);
            }

            if (connector.IsVirtualBalanceEnabled)
            {
                return connector.VirtualBalance.Add(realBalance);
            }

            return realBalance;
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

        [TestMethod]
        public  async Task BurnToken()
        {
            var amount = 10_0000000;
            foreach (var s in ResourceSymbol)
            {
                var balance = await _tokenSub.GetBalance.CallAsync(new GetBalanceInput
                    {Owner = InitAccount.ConvertAddress(), Symbol = s});
                var result = await _tokenSub.Burn.SendAsync(new BurnInput
                {
                    Symbol = s,
                    Amount = amount
                });
                result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
                Logger.Info($"{s}: {balance}");
                var tokenInfo = await _tokenSub.GetTokenInfo.CallAsync(new GetTokenInfoInput{Symbol = s});
                Logger.Info($"{s}: {tokenInfo}");
            }
            
            foreach (var s in ResourceSymbol)
            {
                var balance = await _tokenSub.GetBalance.CallAsync(new GetBalanceInput
                    {Owner = InitAccount.ConvertAddress(), Symbol = s});
                Logger.Info($"{s}: {balance}");
            }
        }
        
        [TestMethod]
        public  async Task BurnToken_OneToken()
        {
            var s = "ELF";
            var amount = 39824688;

            var balance = await _tokenSub.GetBalance.CallAsync(new GetBalanceInput
                    {Owner = InitAccount.ConvertAddress(), Symbol = s});
                var result = await _tokenSub.Burn.SendAsync(new BurnInput
                {
                    Symbol = s,
                    Amount = amount
                });
                result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
                Logger.Info($"{s}: {balance}");
                var tokenInfo = await _tokenSub.GetTokenInfo.CallAsync(new GetTokenInfoInput{Symbol = s});
                Logger.Info($"{s}: {tokenInfo}");
                var afterBalance = await _tokenSub.GetBalance.CallAsync(new GetBalanceInput
                    {Owner = InitAccount.ConvertAddress(), Symbol = s});
                Logger.Info($"{s}: {afterBalance}");
        }

        [TestMethod]
        [DataRow("AEUSD",long.MaxValue)]
        public async Task CreateToken(string symbol,long amount)
        {
            var voteContract = _genesisContract.GetVoteContract(InitAccount);
            if (!_tokenContract.GetTokenInfo(symbol).Equals(new TokenInfo())) return;
            var result = await _tokenSub.Create.SendAsync(new CreateInput
            {
                Issuer = InitAccount.ConvertAddress(),
                Symbol = symbol,
                Decimals = 3,
                IsBurnable = true,
                TokenName = $"{symbol} symbol",
                TotalSupply = amount,
                LockWhiteList =
                {
                    voteContract.Contract
                }
            });

            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        }

        [TestMethod]
        [DataRow("28Y8JA1i2cN6oHvdv7EraXJr9a1gY6D1PpJXw9QtRMRwKcBQMK","AEUSD",1000000000_000)]
        public async Task IssueToken(string account,string symbol, long amount)
        {
            var balance = _tokenContract.GetUserBalance(account, symbol);
            var issueResult = await _tokenSub.Issue.SendAsync(new IssueInput
            {
                Amount = amount,
                Symbol = symbol,
                To =  account.ConvertAddress()
            });
            issueResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var afterBalance = _tokenContract.GetUserBalance(account, symbol);
            afterBalance.ShouldBe(amount + balance);
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