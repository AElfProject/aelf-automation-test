using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Acs1;
using AElf.Contracts.MultiToken;
using AElf.Contracts.TokenConverter;
using AElf.Types;
using AElfChain.Common.Contracts;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Helpers;
using Google.Protobuf.WellKnownTypes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace AElf.Automation.E2ETest.ContractSuits
{
    [TestClass]
    public class TokenTests : ContractTestBase
    {
        [TestMethod]
        public async Task TokenBasicInfo_Test()
        {
            var primaryToken = ChainManager.TokenService.GetPrimaryTokenSymbol();
            primaryToken.ShouldBe("ELF");

            var nativeTokenInfo = await ChainManager.TokenStub.GetNativeTokenInfo.CallAsync(new Empty());
            nativeTokenInfo.Symbol.ShouldBe("ELF");
            nativeTokenInfo.TokenName.ShouldBe("Native Token");
            nativeTokenInfo.Decimals.ShouldBe(8);
            nativeTokenInfo.TotalSupply.ShouldBe(10_0000_0000_00000000);
            nativeTokenInfo.IsBurnable.ShouldBe(true);
        }

        [TestMethod]
        public async Task TokenCreateAndIssue_Test()
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

            //create
            var createResult = await ChainManager.TokenStub.Create.SendAsync(new CreateInput
            {
                Decimals = 8,
                IsBurnable = true,
                TotalSupply = 80000_00000000,
                IssueChainId = ChainManager.ChainId,
                Issuer = ChainManager.CallAccount,
                Symbol = symbol,
                TokenName = $"create token {symbol}",
            });
            createResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

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

            //issue
            var issueResult = await ChainManager.TokenStub.Issue.SendAsync(new IssueInput
            {
                To = ChainManager.CallAccount,
                Amount = 40000_00000000,
                Symbol = symbol,
                Memo = "issue token"
            });
            issueResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            //check balance
            var userBalance = await ChainManager.TokenStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = ChainManager.CallAccount,
                Symbol = symbol
            });
            userBalance.Owner.ShouldBe(ChainManager.CallAccount);
            userBalance.Symbol.ShouldBe(symbol);
            userBalance.Balance.ShouldBe(40000_00000000);

            //check token total balance
            tokenInfo = await ChainManager.TokenStub.GetTokenInfo.CallAsync(new GetTokenInfoInput
            {
                Symbol = symbol
            });
            tokenInfo.Supply.ShouldBe(40000_00000000);
        }

        [TestMethod]
        public async Task TokenApproveAndUnApprove_Test()
        {
            var tester = NodeManager.NewAccount();
            var nativeSymbol = ChainManager.TokenService.GetNativeTokenSymbol();
            //Approve verify
            var approveResult = await ChainManager.TokenStub.Approve.SendAsync(new ApproveInput
            {
                Spender = tester.ConvertAddress(),
                Amount = 200_00000000L,
                Symbol = nativeSymbol
            });
            approveResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var allowance = await ChainManager.TokenStub.GetAllowance.CallAsync(new GetAllowanceInput
            {
                Owner = ChainManager.CallAccount,
                Spender = tester.ConvertAddress(),
                Symbol = nativeSymbol
            });
            allowance.Allowance.ShouldBe(200_00000000L);

            //UnApprove verify
            var unApproveResult = await ChainManager.TokenStub.UnApprove.SendAsync(new UnApproveInput
            {
                Spender = tester.ConvertAddress(),
                Amount = 100_00000000L,
                Symbol = nativeSymbol
            });
            unApproveResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            allowance = await ChainManager.TokenStub.GetAllowance.CallAsync(new GetAllowanceInput
            {
                Owner = ChainManager.CallAccount,
                Spender = tester.ConvertAddress(),
                Symbol = nativeSymbol
            });
            allowance.Allowance.ShouldBe(100_00000000L);
        }

        [TestMethod]
        public async Task BurnTokenTest()
        {
            const long burnAmount = 1_00000000;
            var tester = NodeManager.NewAccount();
            var nativeSymbol = ChainManager.TokenService.GetNativeTokenSymbol();
            ChainManager.TokenService.TransferBalance(ChainManager.CallAddress, tester, 5_00000000, nativeSymbol);

            var beforeBalance = ChainManager.TokenService.GetUserBalance(tester, nativeSymbol);
            var beforeTokenInfo = ChainManager.TokenService.GetTokenInfo(nativeSymbol);

            var tokenStub = ChainManager.GenesisService.GetTokenStub(tester);
            var burnResult = await tokenStub.Burn.SendAsync(new BurnInput
            {
                Symbol = nativeSymbol,
                Amount = burnAmount
            });
            burnResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var txFee = burnResult.TransactionResult.TransactionFee.GetDefaultTransactionFee();
            var afterBalance = ChainManager.TokenService.GetUserBalance(tester, nativeSymbol);
            var afterTokenInfo = ChainManager.TokenService.GetTokenInfo(nativeSymbol);

            beforeBalance.ShouldBe(afterBalance + burnAmount + txFee);
            afterTokenInfo.Burned.ShouldBeGreaterThanOrEqualTo(beforeTokenInfo.Burned + burnAmount);
        }

        [TestMethod]
        public async Task SetAndGetMethodFee_Test()
        {
            const string method = nameof(TokenMethod.GetBalance);
            var releaseResult = ChainManager.Authority.ExecuteTransactionWithAuthority(ChainManager.TokenService.ContractAddress,
                "SetMethodFee", new MethodFees
                {
                    MethodName = method,
                    Fees =
                    {
                        new MethodFee
                        {
                            Symbol = "CPU",
                            BasicFee = 5000_0000
                        }
                    }
                }, ChainManager.CallAddress);
            releaseResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var methodFee =
                ChainManager.TokenService.CallViewMethod<MethodFees>(TokenMethod.GetMethodFee, new StringValue
                {
                    Value = method
                });
            methodFee.MethodName.ShouldBe(method);
            methodFee.Fees.First().ShouldBe(new MethodFee
            {
                Symbol = "CPU",
                BasicFee = 5000_0000
            });
            
            //verify transaction fee
            var buyResult = await ChainManager.TokenconverterStub.Buy.SendAsync(new BuyInput
            {
                Symbol = "CPU",
                Amount = 200_00000000,
            });
            buyResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var beforeCpu = ChainManager.TokenService.GetUserBalance(ChainManager.CallAddress, "CPU");
            var getBalanceResult = await ChainManager.TokenStub.GetBalance.SendAsync(new GetBalanceInput
            {
                Owner = ChainManager.CallAccount,
                Symbol = "CPU"
            });
            getBalanceResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var afterCpu = ChainManager.TokenService.GetUserBalance(ChainManager.CallAddress, "CPU");
            beforeCpu.ShouldBe(afterCpu + 5000_0000);
        }

        [TestMethod]
        public async Task IsInWhiteList_Test()
        {
            //configuration not in token white list
            var configContract = ChainManager.GenesisService.GetContractAddressByName(NameProvider.Configuration);
            var result = await ChainManager.TokenStub.IsInWhiteList.CallAsync(new IsInWhiteListInput
            {
                Address = configContract
            });
            result.Value.ShouldBe(false);

            //token converter in token white list
            var whiteList = new List<NameProvider>
            {
                NameProvider.Vote,
                NameProvider.Profit,
                NameProvider.Election,
                NameProvider.Treasury,
                NameProvider.TokenConverter,
                NameProvider.ReferendumAuth
            };
            foreach (var provider in whiteList)
            {
                var contract = ChainManager.GenesisService.GetContractAddressByName(provider);
                result = await ChainManager.TokenStub.IsInWhiteList.CallAsync(new IsInWhiteListInput
                {
                    Address = contract,
                    Symbol = ChainManager.TokenService.GetNativeTokenSymbol()
                });
                result.Value.ShouldBe(true, $"{provider.ToString()}");
            }
        }

        [TestMethod]
        public async Task GetResourceTokenInfo_Test()
        {
            var resourceInfos = await ChainManager.TokenStub.GetResourceTokenInfo.CallAsync(new Empty());
            resourceInfos.Value.Count.ShouldBe(4);
            var resourceSymbols = resourceInfos.Value.Select(o => o.Symbol);
            resourceSymbols.ShouldBe(new[] {"WRITE", "READ", "STO", "NET"});

            resourceInfos.Value.ShouldAllBe(o => o.IsBurnable);
            resourceInfos.Value.ShouldAllBe(o => o.Supply == 5_0000_0000_00000000);
            resourceInfos.Value.ShouldAllBe(o => o.TotalSupply == 5_0000_0000_00000000);
            resourceInfos.Value.ShouldAllBe(o => o.Decimals == 8);

            var economicContract =
                await ChainManager.GenesisStub.GetContractAddressByName.CallAsync(
                    Hash.FromString("AElf.ContractNames.Economic"));
            resourceInfos.Value.ShouldAllBe(o => o.Issuer == economicContract);
        }
    }
}