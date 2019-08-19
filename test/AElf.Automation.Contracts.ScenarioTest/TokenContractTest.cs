using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using AElf.Automation.Common.Contracts;
using AElf.Automation.Common.Helpers;
using AElf.Automation.Common.OptionManagers.Authority;
using AElf.Contracts.MultiToken;
using AElf.Types;
using log4net;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace AElf.Automation.Contracts.ScenarioTest
{
    [TestClass]
    public class TokenContractTest
    {
        private ILog Logger { get; set; }
        public string TokenAbi { get; set; }
        public WebApiHelper CH { get; set; }
        public List<string> UserList { get; set; }

        public string InitAccount { get; } = "e3SpqBExbGQu6AgV9jaqdbwbRVVuB521Qm8NTR4kgja9C2qZ2";
        public string TestAccount { get; } = "e3SpqBExbGQu6AgV9jaqdbwbRVVuB521Qm8NTR4kgja9C2qZ2";
        private static string RpcUrl { get; } = "127.0.0.1:8000";

        [TestInitialize]
        public void Initialize()
        {
            Log4NetHelper.LogInit("ContractTest");
            Logger = Log4NetHelper.GetLogger();
        }

        [TestMethod]
        public async Task NewStubTest_Call()
        {
            var tokenContractAddress =
                AddressHelper.Base58StringToAddress("WnV9Gv3gioSh3Vgaw8SSB96nV8fWUNxuVozCf6Y14e7RXyGaM");
            var keyPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "aelf");
            var tester = new ContractTesterFactory(RpcUrl, keyPath);
            var tokenStub = tester.Create<TokenContractContainer.TokenContractStub>(tokenContractAddress, InitAccount);
            var tokenInfo = await tokenStub.GetTokenInfo.CallAsync(new GetTokenInfoInput
            {
                Symbol = "ELF"
            });
            tokenInfo.ShouldNotBeNull();
        }

        [TestMethod]
        public async Task NewStubTest_Execution()
        {
            var tokenContractAddress =
                AddressHelper.Base58StringToAddress("WnV9Gv3gioSh3Vgaw8SSB96nV8fWUNxuVozCf6Y14e7RXyGaM");
            var keyPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "aelf");
            var tester = new ContractTesterFactory(RpcUrl, keyPath);
            var tokenStub = tester.Create<TokenContractContainer.TokenContractStub>(tokenContractAddress, InitAccount);
            var transactionResult = await tokenStub.Transfer.SendAsync(new TransferInput
            {
                Amount = 100,
                Symbol = "ELF",
                To = AddressHelper.Base58StringToAddress(TestAccount),
                Memo = "Test transfer with new sdk"
            });
            transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            //query balance
            var result = await tokenStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = AddressHelper.Base58StringToAddress(TestAccount),
                Symbol = "ELF"
            });
            result.Balance.ShouldBeGreaterThanOrEqualTo(100);
        }

        [TestMethod]
        public void DeployContractWithAuthority_Test()
        {
            var authority = new AuthorityManager(RpcUrl, TestAccount);
            var contractAddress = authority.DeployContractWithAuthority(TestAccount, "AElf.Contracts.MultiToken.dll");
            contractAddress.ShouldNotBeNull();
        }
    }
}