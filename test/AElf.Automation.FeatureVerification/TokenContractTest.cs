using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Client.Dto;
using AElf.Contracts.Association;
using AElf.Contracts.MultiToken;
using AElf.Contracts.TestContract.BasicFunction;
using AElf.Contracts.TokenConverter;
using AElf.CSharp.Core;
using AElf.Standards.ACS10;
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
using ExternalInfo = AElf.Contracts.TestContract.BasicFunction.ExternalInfo;

namespace AElf.Automation.Contracts.ScenarioTest
{
    [TestClass]
    public class TokenContractTest
    {
        private readonly List<string> ResourceSymbol = new List<string>
            { "CPU", "NET", "DISK", "RAM", "READ", "WRITE", "STORAGE", "TRAFFIC", "ELF", "SHARE" };

        private TokenContractContainer.TokenContractStub _bpTokenSub;
        private GenesisContract _genesisContract;
        private ParliamentContract _parliamentContract;
        private TokenConverterContractContainer.TokenConverterContractStub _testTokenConverterStub;
        private TokenContractContainer.TokenContractStub _testTokenSub;

        private TokenContract _tokenContract;
        private TokenConverterContract _tokenConverterContract;
        private TokenConverterContractContainer.TokenConverterContractStub _tokenConverterStub;
        private TokenContractImplContainer.TokenContractImplStub _tokenStub;
        private AssociationContract _association;
        private AssociationContractContainer.AssociationContractStub _associationStub;
        private ILog Logger { get; set; }
        private INodeManager NodeManager { get; set; }
        private AuthorityManager AuthorityManager { get; set; }

        private string InitAccount { get; } = "28Y8JA1i2cN6oHvdv7EraXJr9a1gY6D1PpJXw9QtRMRwKcBQMK";
        private string BpAccount { get; } = "2ZYyxEH6j8zAyJjef6Spa99Jx2zf5GbFktyAQEBPWLCvuSAn8D";
        private string TestAccount { get; } = "YF8o6ytMB7n5VF9d1RDioDXqyQ9EQjkFK3AwLPCH2b9LxdTEq";
        private string Account { get; } = "2a6MGBRVLPsy6pu4SVMWdQqHS5wvmkZv8oas9srGWHJk7GSJPV";

        // private static string RpcUrl { get; } = "192.168.66.163:8000";
        private static string RpcUrl { get; } = "127.0.0.1:8000";

        private string Symbol { get; } = "TEST";
        private string Symbol1 { get; } = "NOPROFIT";
        private string Symbol2 { get; } = "NOWHITE";

        private string _basicFunctionAddress = "";
        private BasicFunctionContract _basicFunctionContract;
        private BasicFunctionContractContainer.BasicFunctionContractStub _basicFunctionStub;

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
            _association = _genesisContract.GetAssociationAuthContract(InitAccount);
            _tokenConverterContract = _genesisContract.GetTokenConverterContract(InitAccount);

            _tokenStub = _genesisContract.GetTokenImplStub(InitAccount);
            _bpTokenSub = _genesisContract.GetTokenStub(BpAccount);
            _testTokenSub = _genesisContract.GetTokenStub(TestAccount);
            _associationStub = _genesisContract.GetAssociationAuthStub(InitAccount);
            _tokenConverterStub = _genesisContract.GetTokenConverterStub(InitAccount);
            _testTokenConverterStub = _genesisContract.GetTokenConverterStub(TestAccount);
            //
            _basicFunctionContract = _basicFunctionAddress == ""
            ? new BasicFunctionContract(NodeManager, InitAccount)
            : new BasicFunctionContract(NodeManager, InitAccount, _basicFunctionAddress);
            _basicFunctionStub = _basicFunctionContract
            .GetTestStub<BasicFunctionContractContainer.BasicFunctionContractStub>(InitAccount);
        }

        #region Connector

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
            await CreateToken(Symbol, long.MaxValue, 3);
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
            afterBalance.ShouldBe(balance + amount);
        }

        [TestMethod]
        public async Task TokenCreateTest()
        {
            var symbol = Symbol;
            var amount = 5000000000_000000000;
            var issued = 4000000000_00000000;
            var burned = 1_00000000;
            await CreateToken(symbol, amount, 3);
            var tokenInfo = _tokenContract.GetTokenInfo(symbol);
            await IssueToken(InitAccount, symbol, issued);
            await _tokenStub.Burn.SendAsync(new BurnInput { Symbol = symbol, Amount = burned });

            var afterTokenInfo = _tokenContract.GetTokenInfo(symbol);
            afterTokenInfo.TotalSupply.ShouldBe(amount);
            afterTokenInfo.Supply.ShouldBe(issued - burned);
            afterTokenInfo.Issued.ShouldBe(issued + tokenInfo.Issued);
            Logger.Info(afterTokenInfo);
        }

        [TestMethod]
        public async Task AddListConnector()
        {
            var list = new List<string>() { Symbol };
            foreach (var symbol in list)
            {
                await AddConnector(symbol);
            }
        }

        public async Task AddConnector(string symbol)
        {
            var amount = 80000000_0000000000;
            await CreateToken(symbol, amount, 3);
            await IssueToken(InitAccount, symbol, amount);
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
            var connectorController = await _tokenConverterStub.GetControllerForManageConnector.CallAsync(new Empty());
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
            await IssueToken(InitAccount, symbol, amount);
            var ELFAmount = await GetNeededDeposit(amount, symbol);
            Logger.Info($"Need ELF : {ELFAmount}");
            if (ELFAmount > 0)
            {
                (await _tokenStub.Approve.SendAsync(new ApproveInput
                {
                    Spender = _tokenConverterContract.Contract,
                    Symbol = "ELF",
                    Amount = ELFAmount
                })).TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            }

            (await _tokenStub.Approve.SendAsync(new ApproveInput
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

            var enableConnector = await _tokenConverterStub.EnableConnector.SendAsync(buildInput);
            enableConnector.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var tokenConverterBalance = await _tokenStub.GetBalance.CallAsync(new GetBalanceInput
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
            var resBalance = _tokenContract.GetUserBalance(Account, symbol);

            var tokenConverterBalance = _tokenContract.GetUserBalance(_tokenConverterContract.ContractAddress);
            var treasury = _genesisContract.GetTreasuryContract();
            var dividends = treasury.GetCurrentTreasuryBalance();
            var treasuryDonate = dividends.Value["ELF"];
            var tokenInfo = _tokenContract.GetTokenInfo("ELF");

            Logger.Info($"amountToPay={amountToPay}, fee={fee}, donateFee={donateFee}, burnFee={burnFee}");
            Logger.Info(
                $"tokenConverterBalance={tokenConverterBalance},user balance={balance}, treasuryDonate={treasuryDonate}");

            var result = _tokenConverterContract.Buy(Account, symbol, amount);
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var sizeFee = result.GetDefaultTransactionFee();
            var burnAmount = sizeFee.Div(10);
            var transferAmount = sizeFee.Sub(burnAmount);

            NodeManager.WaitOneBlock(result.BlockNumber);

            var afterBalance = _tokenContract.GetUserBalance(Account);
            var afterResBalance = _tokenContract.GetUserBalance(Account, symbol);

            var afterTokenConverterBalance = _tokenContract.GetUserBalance(_tokenConverterContract.ContractAddress);
            var afterDividends = treasury.GetCurrentTreasuryBalance();
            var afterTreasuryDonate = afterDividends.Value["ELF"];
            var afterTokenInfo = _tokenContract.GetTokenInfo("ELF");

            afterResBalance.ShouldBe(resBalance + amount);
            afterBalance.ShouldBe(balance - amountToPay - fee - sizeFee);
            afterTokenConverterBalance.ShouldBe(tokenConverterBalance + amountToPay);
            afterTreasuryDonate.ShouldBe(treasuryDonate + transferAmount + donateFee);
            afterTokenInfo.Supply.ShouldBe(tokenInfo.Supply - burnAmount - burnFee);

            Logger.Info(
                $"sizeFee={sizeFee}, burnAmount={burnAmount}, transferAmount={transferAmount}, afterTreasuryDonate={afterTreasuryDonate}");
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
            var result =
                await _tokenStub.GetCalculateFeeCoefficientsForContract.CallAsync(new Int32Value { Value = 0 });
            Logger.Info($"{result}");

            var result1 =
                await _tokenStub.GetCalculateFeeCoefficientsForContract.CallAsync(new Int32Value { Value = 1 });
            Logger.Info($"{result1}");

            var result2 =
                await _tokenStub.GetCalculateFeeCoefficientsForContract.CallAsync(new Int32Value { Value = 2 });
            Logger.Info($"{result2}");

            var result3 =
                await _tokenStub.GetCalculateFeeCoefficientsForContract.CallAsync(new Int32Value { Value = 3 });
            Logger.Info($"{result3}");
        }

        [TestMethod]
        public async Task GetCalculateFeeCoefficientOfUser()
        {
            var result = await _tokenStub.GetCalculateFeeCoefficientsForSender.CallAsync(new Empty());
            Logger.Info($"{result}");
        }

        [TestMethod]
        public async Task GetBasicToken()
        {
            var result = await _tokenConverterStub.GetBaseTokenSymbol.CallAsync(new Empty());
            Logger.Info($"{result.Symbol}");
        }

        [TestMethod]
        public async Task ChangeManagerAddress()
        {
            var manager = await _tokenConverterStub.GetControllerForManageConnector.CallAsync(new Empty());
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
            var newManager = await _tokenConverterStub.GetControllerForManageConnector.CallAsync(new Empty());
            newManager.ContractAddress.ShouldBe(association.Contract);
            newManager.OwnerAddress.ShouldBe(newController);
        }

        [TestMethod]
        public async Task GetManagerAddress()
        {
            var manager = await _tokenConverterStub.GetControllerForManageConnector.CallAsync(new Empty());
            Logger.Info($"manager is {manager.OwnerAddress}");
            var organization = _parliamentContract.GetGenesisOwnerAddress();
            Logger.Info($"organization is {organization}");
        }

        [TestMethod]
        public async Task GetConnector()
        {
            var result = await _tokenConverterStub.GetPairConnector.CallAsync(new TokenSymbol { Symbol = "STA" });
            Logger.Info($"{result}");
        }

        [TestMethod]
        public async Task GetNeededDepositTest()
        {
            var amount = 4500000000_000000000;
            var symbol = Symbol;
            var deposit = await GetNeededDeposit(amount, symbol);
            Logger.Info($"{deposit}");
        }

        [TestMethod]
        public async Task CheckPrice()
        {
            var symbol = "CPU";
            var amount = 1_00000000;

            var result = await _tokenConverterStub.GetPairConnector.CallAsync(new TokenSymbol { Symbol = symbol });
            var fromConnectorWeight = decimal.Parse(result.DepositConnector.Weight);
            var toConnectorWeight = decimal.Parse(result.ResourceConnector.Weight);

            var amountToPay = BancorHelper.GetAmountToPayFromReturn(
                GetSelfBalance(result.DepositConnector, result.DepositConnector.RelatedSymbol), fromConnectorWeight,
                GetSelfBalance(result.ResourceConnector, symbol), toConnectorWeight,
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
                GetSelfBalance(result.DepositConnector, symbol), fromConnectorWeight,
                GetSelfBalance(result.ResourceConnector, symbol), toConnectorWeight,
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

            var result = await _tokenConverterStub.GetPairConnector.CallAsync(new TokenSymbol { Symbol = symbol });
            var fb = result.DepositConnector.VirtualBalance;
            var tb = result.ResourceConnector.IsVirtualBalanceEnabled
                ? result.ResourceConnector.VirtualBalance.Add(tokenInfo.TotalSupply)
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

        public long GetSelfBalance(Connector connector, string symbol)
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

        #endregion


        [TestMethod]
        [DataRow("2WHXRoLRjbUTDQsuqR5CntygVfnDb125qdJkudev4kVNbLhTdG")]
        public async Task Acs8ContractTest(string acs8Contract)
        {
            foreach (var s in ResourceSymbol)
            {
                var balance = await _tokenStub.GetBalance.CallAsync(new GetBalanceInput
                    { Owner = acs8Contract.ConvertAddress(), Symbol = s });
                Logger.Info($"{s} balance is {balance.Balance}");
            }
        }

        [TestMethod]
        public async Task BurnToken()
        {
            var amount = 10_0000000;
            foreach (var s in ResourceSymbol)
            {
                var balance = await _tokenStub.GetBalance.CallAsync(new GetBalanceInput
                    { Owner = InitAccount.ConvertAddress(), Symbol = s });
                var result = await _tokenStub.Burn.SendAsync(new BurnInput
                {
                    Symbol = s,
                    Amount = amount
                });
                result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
                Logger.Info($"{s}: {balance}");
                var tokenInfo = await _tokenStub.GetTokenInfo.CallAsync(new GetTokenInfoInput { Symbol = s });
                Logger.Info($"{s}: {tokenInfo}");
            }

            foreach (var s in ResourceSymbol)
            {
                var balance = await _tokenStub.GetBalance.CallAsync(new GetBalanceInput
                    { Owner = InitAccount.ConvertAddress(), Symbol = s });
                Logger.Info($"{s}: {balance}");
            }
        }

        [TestMethod]
        public async Task BurnToken_OneToken()
        {
            var s = "ELF";
            var amount = 39824688;

            var balance = await _tokenStub.GetBalance.CallAsync(new GetBalanceInput
                { Owner = InitAccount.ConvertAddress(), Symbol = s });
            var result = await _tokenStub.Burn.SendAsync(new BurnInput
            {
                Symbol = s,
                Amount = amount
            });
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            Logger.Info($"{s}: {balance}");
            var tokenInfo = await _tokenStub.GetTokenInfo.CallAsync(new GetTokenInfoInput { Symbol = s });
            Logger.Info($"{s}: {tokenInfo}");
            var afterBalance = await _tokenStub.GetBalance.CallAsync(new GetBalanceInput
                { Owner = InitAccount.ConvertAddress(), Symbol = s });
            Logger.Info($"{s}: {afterBalance}");
        }

        [TestMethod]
        [DataRow("CROSS", 1000000000000000000, 10)]
        public async Task CreateToken(string symbol, long amount, int d)
        {
            if (!_tokenContract.GetTokenInfo(symbol).Equals(new TokenInfo())) return;
            var result = await _tokenStub.Create.SendAsync(new CreateInput
            {
                Issuer = InitAccount.ConvertAddress(),
                Symbol = symbol,
                Decimals = d,
                IsBurnable = true,
                TokenName = $"{symbol} symbol",
                TotalSupply = amount,
                ExternalInfo = new AElf.Contracts.MultiToken.ExternalInfo()
                {
                    Value =
                    {
                        {
                            "__nft_image_url",
                            ""
                        }
                    }
                }
            });
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var tokenInfo = _tokenContract.GetTokenInfo(symbol);
            tokenInfo.Symbol.ShouldBe(symbol);
            Logger.Info(tokenInfo);
        }

        [TestMethod]
        [DataRow("TESTNFTCOLLECTION-0", 100, 0, "")]
        public void CreateTokenThroughOrganization(string symbol, long amount, int d, string organization)
        {
            var organizationInfo = _association.GetOrganization(Address.FromBase58(organization));
            var input = new CreateInput
            {
                Issuer = InitAccount.ConvertAddress(),
                Symbol = symbol,
                Decimals = d,
                IsBurnable = true,
                IssueChainId = ChainHelper.ConvertBase58ToChainId("AELF"),
                TokenName = $"{symbol} symbol",
                TotalSupply = amount,
                ExternalInfo = new AElf.Contracts.MultiToken.ExternalInfo()
                {
                    Value =
                    {
                        {
                            "__nft_image_url",
                            ""
                        },
                        { "__nft_type", "ART" }
                    }
                }
            };

            var approveInput = new ApproveInput
            {
                Spender = _tokenContract.Contract,
                Amount = 10000_00000000,
                Symbol = "ELF"
            };

            var approveProposal = _association.CreateProposal(_tokenContract.ContractAddress, "Approve", approveInput,
                Address.FromBase58(organization), InitAccount);
            foreach (var member in organizationInfo.OrganizationMemberList.OrganizationMembers)
            {
                _association.ApproveProposal(approveProposal, member.ToBase58());
            }
            var releaseProposal = _association.ReleaseProposal(approveProposal, InitAccount);
            releaseProposal.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            
            _tokenContract.TransferBalance(InitAccount, organization, 10000_00000000);

            var creatProposal = _association.CreateProposal(_tokenContract.ContractAddress, "Create", input,
                Address.FromBase58(organization), InitAccount);
            foreach (var member in organizationInfo.OrganizationMemberList.OrganizationMembers)
            {
                _association.ApproveProposal(creatProposal, member.ToBase58());
            }
            var release = _association.ReleaseProposal(creatProposal, InitAccount);
            release.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            
            //Check TokenInfo
            var tokenInfo = _tokenContract.GetTokenInfo(symbol);
            tokenInfo.ShouldNotBe(new TokenInfo());
            Logger.Info(tokenInfo);
        }

        [TestMethod]
        [DataRow("", "", 0)]
        public async Task IssueToken(string account, string symbol, long amount)
        {
            var balance = _tokenContract.GetUserBalance(account, symbol);
            var issueResult = await _tokenStub.Issue.SendAsync(new IssueInput
            {
                Amount = amount,
                Symbol = symbol,
                To = account.ConvertAddress()
            });
            issueResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var afterBalance = _tokenContract.GetUserBalance(account, symbol);
            afterBalance.ShouldBe(amount + balance);
            var tokenInfo = _tokenContract.GetTokenInfo(symbol);
            tokenInfo.Symbol.ShouldBe(symbol);
            Logger.Info(tokenInfo);
        }
        
        [TestMethod]
        [DataRow("", "","", 1)]
        public async Task TransferToken(string account, string toAccount, string symbol, long amount)
        {
            var balance = _tokenContract.GetUserBalance(account, symbol);
            var toBalance = _tokenContract.GetUserBalance(toAccount, symbol);

            var issueResult = await _tokenStub.Transfer.SendAsync(new TransferInput
            {
                Amount = amount,
                Symbol = symbol,
                To = toAccount.ConvertAddress()
            });
            issueResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            
            var afterBalance = _tokenContract.GetUserBalance(account, symbol);
            var afterToBalance = _tokenContract.GetUserBalance(toAccount, symbol);

            afterBalance.ShouldBe(balance - amount);
            afterToBalance.ShouldBe(toBalance + amount);
        }

        private async Task<long> GetNeededDeposit(long amount, string symbol)
        {
            var result = await _tokenConverterStub.GetNeededDeposit.CallAsync(new ToBeConnectedTokenInfo
            {
                TokenSymbol = symbol,
                AmountToTokenConvert = amount
            });
            return result.NeedAmount;
        }

        #region Cross contract create token

        [TestMethod]
        public async Task TestCrossContractCreateToken()
        {
            var fee = await _tokenStub.GetMethodFee.CallAsync(new StringValue { Value = "Create" });
            _tokenContract.TransferBalance(InitAccount, _basicFunctionAddress, 100_0000000, "TEST");
            var createTokenInput = new CreateTokenThroughMultiTokenInput
            {
                Symbol = "ABCDEFG-0",
                Decimals = 0,
                TokenName = "ABCDEFG nfts token",
                Issuer = Address.FromBase58(InitAccount),
                IsBurnable = true,
                TotalSupply = 1,
                ExternalInfo = new ExternalInfo()
            };
            var result =
                await _basicFunctionStub.CreateTokenThroughMultiToken.SendAsync(
                    createTokenInput);
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var logs = result.TransactionResult.Logs.Where(l => l.Name.Equals("TransactionFeeCharged")).ToList();
            foreach (var log in logs)
            {
                Logger.Info(log.Address);
                var feeCharged = TransactionFeeCharged.Parser.ParseFrom(log.NonIndexed);
                Logger.Info(feeCharged.Amount);
                Logger.Info(feeCharged.Symbol);
                var feeChargedSender = TransactionFeeCharged.Parser.ParseFrom(log.Indexed.First());
                Logger.Info(feeChargedSender.ChargingAddress);
            }

            var blockHeight = result.TransactionResult.BlockNumber;
            Logger.Info(blockHeight);

            var checkBlock =
                AsyncHelper.RunSync(() => NodeManager.ApiClient.GetBlockByHeightAsync(blockHeight + 1, true));
            var transactionList =
                AsyncHelper.RunSync(() => NodeManager.ApiClient.GetTransactionResultsAsync(checkBlock.BlockHash));
            var transaction = transactionList.Find(t => t.Transaction.MethodName.Equals("ClaimTransactionFees"));
            CheckLogFee(transaction);
        }

        private void CheckLogFee(TransactionResultDto txResult)
        {
            Logger.Info(" ==== Check Log Fee ====");
            var logs = txResult.Logs;
            foreach (var log in logs)
            {
                var name = log.Name;
                switch (name)
                {
                    case "Burned":
                        Logger.Info("Burned");
                        var burnedNoIndexed = Burned.Parser.ParseFrom(ByteString.FromBase64(log.NonIndexed));
                        foreach (var indexed in log.Indexed)
                        {
                            var burnedIndexed = Burned.Parser.ParseFrom(ByteString.FromBase64(indexed));
                            Logger.Info(burnedIndexed.Symbol.Equals("")
                                ? $"Burner: {burnedIndexed.Burner}"
                                : $"Symbol: {burnedIndexed.Symbol}");
                        }

                        Logger.Info($"Amount: {burnedNoIndexed.Amount}");
                        // burnedNoIndexed.Amount.ShouldBe(feeAmount.Div(10));
                        break;
                    case "DonationReceived":
                        Logger.Info("DonationReceived");
                        var donationReceivedNoIndexed =
                            DonationReceived.Parser.ParseFrom(ByteString.FromBase64(log.NonIndexed));
                        Logger.Info($"From: {donationReceivedNoIndexed.From}");
                        Logger.Info($"Amount: {donationReceivedNoIndexed.Amount}");
                        Logger.Info($"Symbol: {donationReceivedNoIndexed.Symbol}");
                        Logger.Info($"PoolContract: {donationReceivedNoIndexed.PoolContract}");
                        // donationReceivedNoIndexed.Amount.ShouldBe(feeAmount.Div(90));
                        break;
                    case "Transferred":
                        Logger.Info("Transferred");
                        var transferredNoIndexed = Transferred.Parser.ParseFrom(ByteString.FromBase64(log.NonIndexed));
                        foreach (var indexed in log.Indexed)
                        {
                            var transferredIndexed = Transferred.Parser.ParseFrom(ByteString.FromBase64(indexed));
                            if (transferredIndexed.Symbol.Equals(""))
                            {
                                Logger.Info(transferredIndexed.From == null
                                    ? $"To: {transferredIndexed.To}"
                                    : $"From: {transferredIndexed.From}");
                            }
                            else
                                Logger.Info($"Symbol: {transferredIndexed.Symbol}");
                        }

                        Logger.Info($"Amount: {transferredNoIndexed.Amount}");
                        // transferredNoIndexed.Amount.ShouldBe(feeAmount.Div(90));

                        break;
                    case "Approved":
                        Logger.Info("Approved");
                        var approvedNoIndexed = Approved.Parser.ParseFrom(ByteString.FromBase64(log.NonIndexed));
                        foreach (var indexed in log.Indexed)
                        {
                            var approvedIndexed = Approved.Parser.ParseFrom(ByteString.FromBase64(indexed));
                            if (approvedIndexed.Symbol.Equals(""))
                            {
                                Logger.Info(approvedIndexed.Owner == null
                                    ? $"To: {approvedIndexed.Spender}"
                                    : $"From: {approvedIndexed.Owner}");
                            }
                            else
                                Logger.Info($"Symbol: {approvedIndexed.Symbol}");
                        }

                        Logger.Info($"Amount: {approvedNoIndexed.Amount}");
                        // approvedNoIndexed.Amount.ShouldBe(feeAmount.Div(90));

                        break;
                }
            }
        }
        #endregion
    }
}