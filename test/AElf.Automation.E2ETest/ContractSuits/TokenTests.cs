using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using AElf.Types;
using AElfChain.Common.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace AElf.Automation.E2ETest.ContractSuits
{
    [TestClass]
    public class TokenTests : ContractTestBase
    {
        [TestMethod]
        public async Task TokenCreate_Test()
        {
            string symbol;
            while (true)
            {
                symbol = CommonHelper.RandomString(6, false);
                var token = await ChainManager.TokenStub.GetTokenInfo.CallAsync(new GetTokenInfoInput
                {
                    Symbol = symbol
                });
                if (token.Equals(new TokenInfo()))
                    break;
            }

            var transactionResult = await ChainManager.TokenStub.Create.SendAsync(new CreateInput
            {
                Decimals = 8,
                IsBurnable = true,
                TotalSupply = 80000_00000000,
                IssueChainId = ChainManager.ChainId,
                Issuer = ChainManager.CallAccount,
                Symbol = symbol,
                TokenName = $"create token {symbol}",
            });
            transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var tokenInfo = await ChainManager.TokenStub.GetTokenInfo.CallAsync(new GetTokenInfoInput
            {
                Symbol = symbol
            });
            tokenInfo.ShouldNotBe(new TokenInfo());
            tokenInfo.Symbol.ShouldBe(symbol);
            tokenInfo.TokenName.ShouldBe($"create token {symbol}");
            tokenInfo.Decimals.ShouldBe(8);
            tokenInfo.IsBurnable.ShouldBe(true);
            tokenInfo.TotalSupply.ShouldBe(80000_00000000);
            tokenInfo.Issuer.ShouldBe(ChainManager.CallAccount);
        }
    }
}