using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Contracts.FinanceContract;
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
using InitializeInput = AElf.Contracts.FinanceContract.InitializeInput;

namespace AElf.Automation.Finance
{
    [TestClass]
    public class FinanceContractTest
    {
        private TokenContractContainer.TokenContractStub _bpTokenSub;
        private GenesisContract _genesisContract;
        private TokenContractContainer.TokenContractStub _testTokenSub;

        private TokenContract _tokenContract;
        private TokenContractContainer.TokenContractStub _tokenSub;
        
        private ILog Logger { get; set; }
        private INodeManager NodeManager { get; set; }
        private AuthorityManager AuthorityManager { get; set; }

        private string InitAccount { get; } = "28Y8JA1i2cN6oHvdv7EraXJr9a1gY6D1PpJXw9QtRMRwKcBQMK";
        private string BpAccount { get; } = "28Y8JA1i2cN6oHvdv7EraXJr9a1gY6D1PpJXw9QtRMRwKcBQMK";
        private string TestAccount { get; } = "28Y8JA1i2cN6oHvdv7EraXJr9a1gY6D1PpJXw9QtRMRwKcBQMK";
        
        private static string RpcUrl { get; } = "192.168.199.109:8007";
        private string Symbol { get; } = "TEST";

        private FinanceContract _financeContract;
        
        [TestInitialize]
        public void Initialize()
        {
            Log4NetHelper.LogInit("ContractTest");
            Logger = Log4NetHelper.GetLogger();
            NodeInfoHelper.SetConfig("nodes");

            NodeManager = new NodeManager(RpcUrl);
            AuthorityManager = new AuthorityManager(NodeManager, InitAccount);
            _genesisContract = GenesisContract.GetGenesisContract(NodeManager, InitAccount);
            _tokenContract = _genesisContract.GetTokenContract(InitAccount);

            _tokenSub = _genesisContract.GetTokenStub(InitAccount);
            _bpTokenSub = _genesisContract.GetTokenStub(BpAccount);
            _testTokenSub = _genesisContract.GetTokenStub(TestAccount);

            var financeContractAddress = "buePNjhmHckfZn9D8GTL1wq6JgA8K24SeTWnjCNcrz6Sf1FDh";
            
            if (financeContractAddress == string.Empty)
            {
                _financeContract = new FinanceContract(NodeManager,InitAccount);
            }
            else
            {
                _financeContract = new FinanceContract(NodeManager, InitAccount, financeContractAddress);
            }
        }

        [TestMethod]
        public async Task InitializeContract()
        {
            var financeStub = _financeContract.GetTestStub<FinanceContractContainer.FinanceContractStub>(InitAccount);
            await financeStub.Initialize.SendAsync(new InitializeInput
            {
                CloseFactor = "0.5",
                LiquidationIncentive = "1.1",
                MaxAssets = 8
            });
        }

        [TestMethod]
        public async Task SupportMarket()
        {
            var financeStub = _financeContract.GetTestStub<FinanceContractContainer.FinanceContractStub>(InitAccount);
            await financeStub.SupportMarket.SendAsync(new SupportMarketInput
            {
                Symbol = "ELF",
                ReserveFactor = "0.1",
                InitialExchangeRate = "0.02",
                MultiplierPerBlock = "0.00000000158549",
                BaseRatePerBlock = "0.000000000317098",
            });
        }

        public async Task SetInterestRate()
        {
            var financeStub = _financeContract.GetTestStub<FinanceContractContainer.FinanceContractStub>(InitAccount);
            await financeStub.SetInterestRate.SendAsync(new SetInterestRateInput
            {
                Symbol = "ELF",
                BaseRatePerBlock = "0.000000000317098",
                MultiplierPerBlock = "0.00000000158549"
            });
        }
        
        [TestMethod]
        public void UpdateContractWithAuthority_Test()
        {
            var authority = new AuthorityManager(NodeManager, InitAccount);
            authority.UpdateContractWithAuthority(InitAccount, _financeContract.ContractAddress,
                "AElf.Contracts.FinanceContract");
        }

        private void DeployContractWithAuthority_Test()
        {
            var authority = new AuthorityManager(NodeManager, InitAccount);
            var contractAddress = authority.DeployContractWithAuthority(TestAccount, "AElf.Contracts.FinanceContract");
            contractAddress.ShouldNotBeNull();
        }

        private async Task CreateToken(long amount)
        {
            if (!_tokenContract.GetTokenInfo(Symbol).Equals(new TokenInfo())) return;
            var result = await _bpTokenSub.Create.SendAsync(new CreateInput
            {
                Issuer = BpAccount.ConvertAddress(),
                Symbol = Symbol,
                Decimals = 8,
                IsBurnable = true,
                TokenName = "TEST symbol",
                TotalSupply = 100000000_00000000
            });

            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var issueResult = await _bpTokenSub.Issue.SendAsync(new IssueInput
            {
                Amount = amount,
                Symbol = Symbol,
                To =  BpAccount.ConvertAddress()
            });
            issueResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var balance = _tokenContract.GetUserBalance(BpAccount, Symbol);
            balance.ShouldBe(amount);
        }
    }
}