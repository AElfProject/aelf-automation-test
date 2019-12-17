using System.Collections.Generic;
using System.Security.Cryptography;
using System.Threading.Tasks;
using AElf.Client.TokenConverter;
using AElfChain.Common;
using AElfChain.Common.Contracts;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using AElf.Contracts.MultiToken;
using AElf.Contracts.TokenConverter;
using AElf.Kernel;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using log4net;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using Connector = AElf.Contracts.TokenConverter.Connector;
using TokenSymbol = AElf.Contracts.TokenConverter.TokenSymbol;

namespace AElf.Automation.Contracts.ScenarioTest
{
    [TestClass]
    public class TokenContractTest
    {
        private ILog Logger { get; set; }
        private INodeManager NodeManager { get; set; }
        private AuthorityManager AuthorityManager { get; set; }

        private TokenContract _tokenContract;
        private GenesisContract _genesisContract;
        private TokenConverterContract _tokenConverterContract;
        private ParliamentAuthContract _parliamentAuthContract;
        private TokenContractContainer.TokenContractStub _tokenSub;
        private TokenContractContainer.TokenContractStub _bpTokenSub;
        private TokenContractContainer.TokenContractStub _testTokenSub;

        private TokenConverterContractContainer.TokenConverterContractStub _tokenConverterSub;
        private TokenConverterContractContainer.TokenConverterContractStub _testTokenConverterSub;

        private string InitAccount { get; } = "h6CRCFAhyozJPwdFRd7i8A5zVAqy171AVty3uMQUQp1MB9AKa";
        private string BpAccount { get; } = "2RCLmZQ2291xDwSbDEJR6nLhFJcMkyfrVTq1i1YxWC4SdY49a6";
        private string TestAccount { get; } = "W4xEKTZcvPKXRAmdu9xEpM69ArF7gUxDh9MDgtsKnu7JfePXo";
        private static string RpcUrl { get; } = "192.168.197.51:8000";
        private string Symbol { get; } = "STA";

        [TestInitialize]
        public void Initialize()
        {
            Log4NetHelper.LogInit("ContractTest");
            Logger = Log4NetHelper.GetLogger();

            NodeManager = new NodeManager(RpcUrl);
            AuthorityManager = new AuthorityManager(NodeManager, InitAccount);
            _genesisContract = GenesisContract.GetGenesisContract(NodeManager, InitAccount);
            _tokenContract = _genesisContract.GetTokenContract(InitAccount);
            _parliamentAuthContract = _genesisContract.GetParliamentAuthContract(InitAccount);
            _tokenConverterContract = _genesisContract.GetTokenConverterContract(InitAccount);

            var tester = new ContractTesterFactory(NodeManager);
            _tokenSub = tester.Create<TokenContractContainer.TokenContractStub>(_tokenContract.Contract, InitAccount);
            _bpTokenSub = tester.Create<TokenContractContainer.TokenContractStub>(_tokenContract.Contract, BpAccount);
            _testTokenSub = tester.Create<TokenContractContainer.TokenContractStub>(_tokenContract.Contract, TestAccount);

            _tokenConverterSub =
                tester.Create<TokenConverterContractContainer.TokenConverterContractStub>(
                    _tokenConverterContract.Contract, BpAccount);
            _testTokenConverterSub =
                tester.Create<TokenConverterContractContainer.TokenConverterContractStub>(
                    _tokenConverterContract.Contract, TestAccount);
        }

        [TestMethod]
        public async Task NewStubTest_Call()
        {
            var tokenContractAddress =
                AddressHelper.Base58StringToAddress("WnV9Gv3gioSh3Vgaw8SSB96nV8fWUNxuVozCf6Y14e7RXyGaM");
            var tester = new ContractTesterFactory(NodeManager);
            var tokenStub = tester.Create<TokenContractContainer.TokenContractStub>(tokenContractAddress, InitAccount);
            var tokenInfo = await tokenStub.GetTokenInfo.CallAsync(new GetTokenInfoInput
            {
                Symbol = NodeOption.NativeTokenSymbol
            });
            tokenInfo.ShouldNotBeNull();
        }

        [TestMethod]
        public async Task NewStubTest_Execution()
        {
            var tokenContractAddress =
                AddressHelper.Base58StringToAddress("WnV9Gv3gioSh3Vgaw8SSB96nV8fWUNxuVozCf6Y14e7RXyGaM");
            var tester = new ContractTesterFactory(NodeManager);
            var tokenStub = tester.Create<TokenContractContainer.TokenContractStub>(tokenContractAddress, InitAccount);
            var transactionResult = await tokenStub.Transfer.SendAsync(new TransferInput
            {
                Amount = 100,
                Symbol = NodeOption.NativeTokenSymbol,
                To = AddressHelper.Base58StringToAddress(TestAccount),
                Memo = "Test transfer with new sdk"
            });
            transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            //query balance
            var result = await tokenStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = AddressHelper.Base58StringToAddress(TestAccount),
                Symbol = NodeOption.NativeTokenSymbol
            });
            result.Balance.ShouldBeGreaterThanOrEqualTo(100);
        }

        [TestMethod]
        public void DeployContractWithAuthority_Test()
        {
            var authority = new AuthorityManager(NodeManager, TestAccount);
            var contractAddress = authority.DeployContractWithAuthority(TestAccount, "AElf.Contracts.MultiToken.dll");
            contractAddress.ShouldNotBeNull();
        }

        [TestMethod]
        public async Task SendTransaction()
        {
            var result = await _bpTokenSub.Transfer.SendAsync(new TransferInput
            {
                To = AddressHelper.Base58StringToAddress(TestAccount),
                Amount = 1000_00000000,
                Memo = "Transfer to test account",
                Symbol = "ELF"
            });
            var size = result.Transaction.Size();
            Logger.Info($"transfer size is: {size}");
        }

        [TestMethod]
        public async Task Approve()
        {
            var result = await _bpTokenSub.Approve.SendAsync(new ApproveInput
            {
                Spender = _tokenConverterContract.Contract,
                Symbol = Symbol,
                Amount = 10_0000_0000_00000000
            });
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        }

        [TestMethod]
        public async Task BuyResource()
        {
            var balance = await _testTokenSub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = AddressHelper.Base58StringToAddress(TestAccount),
                Symbol = NodeManager.GetNativeTokenSymbol()
            });
            var otherTokenBalance = await _testTokenSub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = AddressHelper.Base58StringToAddress(TestAccount),
                Symbol = Symbol
            });

            Logger.Info($"user ELF balance is {balance} user {Symbol} balance is {otherTokenBalance}");

            var result = await _testTokenConverterSub.Buy.SendAsync(new BuyInput
            {
                Amount = 1999_00000000,
                Symbol = Symbol
            });
            var size = result.Transaction.Size();
            Logger.Info($"transfer size is: {size}");

            var afterbalance = await _testTokenSub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = AddressHelper.Base58StringToAddress(TestAccount),
                Symbol = NodeManager.GetNativeTokenSymbol()
            });

            var afterotherTokenBalance = await _testTokenSub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = AddressHelper.Base58StringToAddress(TestAccount),
                Symbol = Symbol
            });

            Logger.Info(
                $"After buy token, user ELF balance is {afterbalance} user {Symbol} balance is {afterotherTokenBalance}");
        }

        [TestMethod]
        public async Task SellResource()
        {
            var balance = await _tokenSub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = AddressHelper.Base58StringToAddress(TestAccount),
                Symbol = NodeManager.GetNativeTokenSymbol()
            });
            var otherTokenBalance = await _tokenSub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = AddressHelper.Base58StringToAddress(TestAccount),
                Symbol = Symbol
            });

            Logger.Info($"user ELF balance is {balance} user {Symbol} balance is {otherTokenBalance}");

            var result = await _testTokenConverterSub.Sell.SendAsync(new SellInput()
            {
                Amount = 1_00000000,
                Symbol = Symbol
            });
            var size = result.Transaction.Size();
            Logger.Info($"transfer size is: {size}");

            var afterBalance = await _tokenSub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = AddressHelper.Base58StringToAddress(TestAccount),
                Symbol = NodeManager.GetNativeTokenSymbol()
            });

            var afterOtherTokenBalance = await _tokenSub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = AddressHelper.Base58StringToAddress(TestAccount),
                Symbol = Symbol
            });

            Logger.Info(
                $"After sell token, user ELF balance is {afterBalance} user {Symbol} balance is {afterOtherTokenBalance}");
        }

        [TestMethod]
        public async Task CreateToken()
        {
            var result = await _bpTokenSub.Create.SendAsync(new CreateInput
            {
                Issuer = AddressHelper.Base58StringToAddress(BpAccount),
                Symbol = Symbol,
                Decimals = 8,
                IsBurnable = true,
                TokenName = "TEST symbol",
                TotalSupply = 1_0000_00000000
            });

            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var issueResult = await _bpTokenSub.Issue.SendAsync(new IssueInput
            {
                Amount = 1000_00000000,
                Symbol = Symbol,
                To = AddressHelper.Base58StringToAddress(BpAccount)
            });
            issueResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var balance = _tokenContract.GetUserBalance(BpAccount, Symbol);
            Logger.Info($"token {Symbol} balance is {balance}");

            var tokenInfo = await _bpTokenSub.GetTokenInfo.CallAsync(new GetTokenInfoInput {Symbol = Symbol});
            Logger.Info($"Supply is {tokenInfo.Supply}, Total supply is {tokenInfo.TotalSupply}");
        }

        [TestMethod]
        public async Task IssueTokenToTokenConverter()
        {
            var tokenInfo = await _bpTokenSub.GetTokenInfo.CallAsync(new GetTokenInfoInput {Symbol = Symbol});
            Logger.Info($"Supply is {tokenInfo.Supply}, Total supply is {tokenInfo.TotalSupply}");

            var issueTokenConverter = await _bpTokenSub.Issue.SendAsync(new IssueInput
            {
                Amount = tokenInfo.TotalSupply - tokenInfo.Supply,
                Symbol = Symbol,
                To = AddressHelper.Base58StringToAddress(_tokenConverterContract.ContractAddress)
            });
            issueTokenConverter.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var converterBalance = _tokenContract.GetUserBalance(_tokenConverterContract.ContractAddress, Symbol);
            converterBalance.ShouldBe(tokenInfo.TotalSupply - tokenInfo.Supply);
            Logger.Info($"token {Symbol} balance is {converterBalance}");
        }

        [TestMethod]
        public void AddConnector()
        {
            var input = new PairConnector
            {
                IsNativeVirtualBalanceEnabled = true,
                IsResourceVirtualBalanceEnabled = false,
                NativeWeight = "0.05",
                ResourceWeight = "0.05",
                ResourceConnectorSymbol = Symbol,
                NativeVirtualBalance = 10_00000000_00000000,
                ResourceVirtualBalance = 0
            };

            var organization = _parliamentAuthContract.GetGenesisOwnerAddress();
            var proposal = _parliamentAuthContract.CreateProposal(_tokenConverterContract.ContractAddress,
                nameof(TokenConverterMethod.AddPairConnectors), input, organization, BpAccount);
            var miners = AuthorityManager.GetCurrentMiners();
            _parliamentAuthContract.MinersApproveProposal(proposal, miners);
            var result = _parliamentAuthContract.ReleaseProposal(proposal, BpAccount);
            result.Status.ShouldBe(TransactionResultStatus.Mined);
        }

        [TestMethod]
        public void UpdateConnector()
        {
            var input = new Connector
            {
                Symbol = "STA",
                Weight = "0.05"
            };

            var organization = _parliamentAuthContract.GetGenesisOwnerAddress();
            var proposal = _parliamentAuthContract.CreateProposal(_tokenConverterContract.ContractAddress,
                nameof(TokenConverterMethod.UpdateConnector), input, organization, BpAccount);
            var miners = AuthorityManager.GetCurrentMiners();
            _parliamentAuthContract.MinersApproveProposal(proposal, miners);
            var result = _parliamentAuthContract.ReleaseProposal(proposal, BpAccount);
            result.Status.ShouldBe(TransactionResultStatus.Mined);
        }

        [TestMethod]
        public async Task BuildConnector()
        {
            var buildInput = new ToBeConnectedTokenInfo()
            {
                TokenSymbol = Symbol,
                AmountToTokenConvert = 1000_00000000
            };

            var enableConnector = await _tokenConverterSub.EnableConnector.SendAsync(buildInput);
            enableConnector.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        }

        /*
        UpdateCoefficientFormContract,
        UpdateCoefficientFormSender,
        UpdateLinerAlgorithm,
        UpdatePowerAlgorithm,
        ChangeFeePieceKey,
        */

        [TestMethod]
        public void UpdateCoefficientFormContract()
        {
            var input = new CoefficientFromContract
            {
                FeeType = 0,
                Coefficient = new CoefficientFromSender
                {
                    PieceKey = 50,
                    IsChangePieceKey = true,
                    NewPieceKeyCoefficient = new NewPieceKeyCoefficient {NewPieceKey = 200},
                    IsLiner = false,
                    LinerCoefficient = new LinerCoefficient
                    {
                        ConstantValue = 1,
                        Denominator = 1,
                        Numerator = 1
                    }
                }
            };
            var organization = _parliamentAuthContract.GetGenesisOwnerAddress();
            var proposal = _parliamentAuthContract.CreateProposal(_tokenContract.ContractAddress,
                nameof(TokenMethod.UpdateCoefficientFromContract), input, organization, InitAccount);
            var miners = AuthorityManager.GetCurrentMiners();
            _parliamentAuthContract.MinersApproveProposal(proposal, miners);
            var result = _parliamentAuthContract.ReleaseProposal(proposal, InitAccount);
            result.Status.ShouldBe(TransactionResultStatus.Mined);
        }

//
        [TestMethod]
        public void UpdateCoefficientFromSender()
        {
            var input = new CoefficientFromSender
            {
                PieceKey = 1000000,
                IsChangePieceKey = false,
                IsLiner = true,
                LinerCoefficient = new LinerCoefficient
                {
                    ConstantValue = 10000,
                    Numerator = 1,
                    Denominator = 400
                }
            };
            var organization = _parliamentAuthContract.GetGenesisOwnerAddress();
            var proposal = _parliamentAuthContract.CreateProposal(_tokenContract.ContractAddress,
                nameof(TokenMethod.UpdateCoefficientFromSender), input, organization, InitAccount);
            var miners = AuthorityManager.GetCurrentMiners();
            _parliamentAuthContract.MinersApproveProposal(proposal, miners);
            var result = _parliamentAuthContract.ReleaseProposal(proposal, InitAccount);
            result.Status.ShouldBe(TransactionResultStatus.Mined);
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
            var result = await _tokenSub.GetCalculateFeeCoefficientOfContract.CallAsync(new SInt32Value {Value = 0});
            var cpu = result.Coefficients;
            Logger.Info($"{cpu}");

            var result1 = await _tokenSub.GetCalculateFeeCoefficientOfContract.CallAsync(new SInt32Value {Value = 1});
            var sto = result1.Coefficients;
            Logger.Info($"{sto}");

            var result2 = await _tokenSub.GetCalculateFeeCoefficientOfContract.CallAsync(new SInt32Value {Value = 2});
            var ram = result2.Coefficients;
            Logger.Info($"{ram}");

            var result3 = await _tokenSub.GetCalculateFeeCoefficientOfContract.CallAsync(new SInt32Value {Value = 3});
            var net = result3.Coefficients;
            Logger.Info($"{net}");
        }

        [TestMethod]
        public async Task GetCalculateFeeCoefficientOfUser()
        {
            var result = await _tokenSub.GetCalculateFeeCoefficientOfSender.CallAsync(new Empty());
            Logger.Info($"{result.Coefficients}");
        }

        [TestMethod]
        public async Task GetNeededDeposit()
        {
            var result = await _tokenConverterSub.GetNeededDeposit.CallAsync(new ToBeConnectedTokenInfo
            {
                TokenSymbol = "TESTELF",
                AmountToTokenConvert = 1900_00000000
            });
            Logger.Info($"{result.NeedAmount},{result.AmountOutOfTokenConvert}");
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
            var manager = await _tokenConverterSub.GetManagerAddress.CallAsync(new Empty());
            Logger.Info($"manager is {manager}");
            var organization = _parliamentAuthContract.GetGenesisOwnerAddress();
            Logger.Info($"organization is {organization}");
        }

        [TestMethod]
        public async Task GetBalance()
        {
            var result = await _tokenSub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = AddressHelper.Base58StringToAddress(_tokenConverterContract.ContractAddress),
                Symbol = _tokenContract.GetPrimaryTokenSymbol()
            });

            var result1 = await _tokenSub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = AddressHelper.Base58StringToAddress(_tokenConverterContract.ContractAddress),
                Symbol = Symbol
            });

            Logger.Info($"{result.Symbol},{result.Balance}");
            Logger.Info($"{result1.Symbol},{result1.Balance}");
        }

        [TestMethod]
        public async Task GetTokenInfo()
        {
            var result = await _tokenSub.GetTokenInfo.CallAsync(new GetTokenInfoInput {Symbol = "ELF"});
            var result1 = await _tokenSub.GetTokenInfo.CallAsync(new GetTokenInfoInput {Symbol = "STA"});

            Logger.Info($"{result.Supply},{result.TotalSupply},\n {result1.Supply},{result1.TotalSupply}");
        }

        [TestMethod]
        public async Task GetConnector()
        {
            var result = await _tokenConverterSub.GetConnector.CallAsync(new TokenSymbol {Symbol = "STA"});
            Logger.Info($"{result}");
        }
    }
}