using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Standards.ACS1;
using AElf.Contracts.MultiToken;
using AElf.Contracts.TokenConverter;
using AElf.Types;
using AElfChain.Common.Contracts;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
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
            var primaryToken = ContractManager.Token.GetPrimaryTokenSymbol();
            primaryToken.ShouldBe("ELF");

            var nativeTokenInfo = await ContractManager.TokenStub.GetNativeTokenInfo.CallAsync(new Empty());
            nativeTokenInfo.Symbol.ShouldBe("ELF");
            nativeTokenInfo.TokenName.ShouldBe("Native Token");
            nativeTokenInfo.Decimals.ShouldBe(8);
            nativeTokenInfo.TotalSupply.ShouldBe(10_0000_0000_00000000);
            nativeTokenInfo.IsBurnable.ShouldBe(true);
        }

        [TestMethod]
        public async Task<string> TokenCreateAndIssue_Test()
        {
            string symbol;
            while (true)
            {
                symbol = CommonHelper.RandomString(6, false);
                var token = await ContractManager.TokenStub.GetTokenInfo.CallAsync(new GetTokenInfoInput
                {
                    Symbol = symbol
                });
                if (token.Equals(new TokenInfo()))
                    break;
            }

            //create
            var createResult = await ContractManager.TokenStub.Create.SendAsync(new CreateInput
            {
                Decimals = 8,
                IsBurnable = true,
                TotalSupply = 80000_00000000,
                IssueChainId = ContractManager.ChainId,
                Issuer = ContractManager.CallAccount,
                Symbol = symbol,
                TokenName = $"create token {symbol}"
            });
            createResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var tokenInfo = await ContractManager.TokenStub.GetTokenInfo.CallAsync(new GetTokenInfoInput
            {
                Symbol = symbol
            });
            tokenInfo.ShouldNotBe(new TokenInfo());
            tokenInfo.Symbol.ShouldBe(symbol);
            tokenInfo.TokenName.ShouldBe($"create token {symbol}");
            tokenInfo.Decimals.ShouldBe(8);
            tokenInfo.IsBurnable.ShouldBe(true);
            tokenInfo.TotalSupply.ShouldBe(80000_00000000);
            tokenInfo.Issuer.ShouldBe(ContractManager.CallAccount);

            //issue
            var issueResult = await ContractManager.TokenStub.Issue.SendAsync(new IssueInput
            {
                To = ContractManager.CallAccount,
                Amount = 40000_00000000,
                Symbol = symbol,
                Memo = "issue token"
            });
            issueResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            //check balance
            var userBalance = await ContractManager.TokenStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = ContractManager.CallAccount,
                Symbol = symbol
            });
            userBalance.Owner.ShouldBe(ContractManager.CallAccount);
            userBalance.Symbol.ShouldBe(symbol);
            userBalance.Balance.ShouldBe(40000_00000000);

            //check token total balance
            tokenInfo = await ContractManager.TokenStub.GetTokenInfo.CallAsync(new GetTokenInfoInput
            {
                Symbol = symbol
            });
            tokenInfo.Supply.ShouldBe(40000_00000000);

            return symbol;
        }

        [TestMethod]
        public async Task ChangeIssuerTest()
        {
            var testAccount = ConfigNodes.Last().Account;
            var symbol = await TokenCreateAndIssue_Test();
            var tokenInfo = ContractManager.Token.GetTokenInfo(symbol);
            var sub = ContractManager.Genesis.GetTokenStub(tokenInfo.Issuer.ToBase58());
            var result = await sub.ChangeTokenIssuer.SendAsync(new ChangeTokenIssuerInput
            {
                NewTokenIssuer = testAccount.ConvertAddress(),
                Symbol = tokenInfo.Symbol
            });
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            tokenInfo = ContractManager.Token.GetTokenInfo(symbol);
            tokenInfo.Issuer.ShouldBe(testAccount.ConvertAddress());
            ContractManager.Token.SetAccount(testAccount);
            var beforeBalance = ContractManager.Token.GetUserBalance(ContractManager.CallAddress, symbol);
            var issue = ContractManager.Token.IssueBalance(testAccount, ContractManager.CallAddress, 1000, symbol);
            issue.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            var balance = ContractManager.Token.GetUserBalance(ContractManager.CallAddress, symbol);
            balance.ShouldBe(beforeBalance + 1000);
        }

        [TestMethod]
        public async Task TokenApproveAndUnApprove_Test()
        {
            var tester = NodeManager.NewAccount();
            var nativeSymbol = ContractManager.Token.GetNativeTokenSymbol();
            //Approve verify
            var approveResult = await ContractManager.TokenStub.Approve.SendAsync(new ApproveInput
            {
                Spender = tester.ConvertAddress(),
                Amount = 200_00000000L,
                Symbol = nativeSymbol
            });
            approveResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var allowance = await ContractManager.TokenStub.GetAllowance.CallAsync(new GetAllowanceInput
            {
                Owner = ContractManager.CallAccount,
                Spender = tester.ConvertAddress(),
                Symbol = nativeSymbol
            });
            allowance.Allowance.ShouldBe(200_00000000L);

            //UnApprove verify
            var unApproveResult = await ContractManager.TokenStub.UnApprove.SendAsync(new UnApproveInput
            {
                Spender = tester.ConvertAddress(),
                Amount = 100_00000000L,
                Symbol = nativeSymbol
            });
            unApproveResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            allowance = await ContractManager.TokenStub.GetAllowance.CallAsync(new GetAllowanceInput
            {
                Owner = ContractManager.CallAccount,
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
            var nativeSymbol = ContractManager.Token.GetNativeTokenSymbol();
            ContractManager.Token.TransferBalance(ContractManager.CallAddress, tester, 5_00000000, nativeSymbol);

            var beforeBalance = ContractManager.Token.GetUserBalance(tester, nativeSymbol);
            var beforeTokenInfo = ContractManager.Token.GetTokenInfo(nativeSymbol);

            var tokenStub = ContractManager.Genesis.GetTokenStub(tester);
            var burnResult = await tokenStub.Burn.SendAsync(new BurnInput
            {
                Symbol = nativeSymbol,
                Amount = burnAmount
            });
            burnResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var txFee = burnResult.TransactionResult.GetDefaultTransactionFee();
            var afterBalance = ContractManager.Token.GetUserBalance(tester, nativeSymbol);
            var afterTokenInfo = ContractManager.Token.GetTokenInfo(nativeSymbol);

            beforeBalance.ShouldBe(afterBalance + burnAmount + txFee);
            afterTokenInfo.Issued.ShouldBe(beforeTokenInfo.Issued);
            afterTokenInfo.Supply.ShouldBeLessThan(beforeTokenInfo.Supply - burnAmount);
        }

        [TestMethod]
        public async Task SetAndGetMethodFee_Test()
        {
            const string method = nameof(TokenMethod.GetBalance);
            var releaseResult = ContractManager.Authority.ExecuteTransactionWithAuthority(
                ContractManager.Token.ContractAddress,
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
                }, ContractManager.CallAddress);
            releaseResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var methodFee =
                ContractManager.Token.CallViewMethod<MethodFees>(TokenMethod.GetMethodFee, new StringValue
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
            var buyResult = await ContractManager.TokenconverterStub.Buy.SendAsync(new BuyInput
            {
                Symbol = "CPU",
                Amount = 200_00000000
            });
            buyResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var beforeCpu = ContractManager.Token.GetUserBalance(ContractManager.CallAddress, "CPU");
            var getBalanceResult = await ContractManager.TokenStub.GetBalance.SendAsync(new GetBalanceInput
            {
                Owner = ContractManager.CallAccount,
                Symbol = "CPU"
            });
            getBalanceResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var afterCpu = ContractManager.Token.GetUserBalance(ContractManager.CallAddress, "CPU");
            beforeCpu.ShouldBe(afterCpu + 5000_0000);
        }

        [TestMethod]
        public async Task ChangeMethodFeeController()
        {
            const string method = nameof(TokenMethod.GetBalance);
            var referendum = ContractManager.Referendum;
            var authorityManager = new AuthorityManager(NodeManager);
            var defaultController =
                await ContractManager.ParliamentContractImplStub.GetMethodFeeController.CallAsync(new Empty());
            defaultController.ContractAddress.ShouldBe(ContractManager.Parliament.Contract);
            var newOrganization = ReferendumOrganization;
            var proposer = referendum.GetOrganization(newOrganization).ProposerWhiteList.Proposers.First();
            ContractManager.Token.ApproveToken(proposer.ToBase58(), referendum.ContractAddress,
                2000, "ELF");
            var input = new AuthorityInfo
            {
                OwnerAddress = newOrganization,
                ContractAddress = ContractManager.Referendum.Contract
            };
            var changeResult = authorityManager.ExecuteTransactionWithAuthority(ContractManager.Token.ContractAddress,
                nameof(ContractManager.TokenImplStub.ChangeMethodFeeController), input, proposer.ToBase58(),
                defaultController.OwnerAddress);
            changeResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var changeInput = new MethodFees
            {
                MethodName = method,
                Fees =
                {
                    new MethodFee
                    {
                        Symbol = "NET",
                        BasicFee = 10000000
                    }
                }
            };

            var changeProposalId = referendum.CreateProposal(ContractManager.Token.ContractAddress,
                nameof(ContractManager.TokenImplStub.SetMethodFee), changeInput,
                newOrganization, proposer.ToBase58());
            var virtualAddress = referendum.GetProposalVirtualAddress(changeProposalId);
            ContractManager.Token.ApproveToken(proposer.ToBase58(), virtualAddress.ToBase58(),
                2000_00000000, "ELF");

            referendum.SetAccount(proposer.ToBase58());
            var changeApproveResult = referendum.ExecuteMethodWithResult(ReferendumMethod.Approve, changeProposalId);
            changeApproveResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var changeReleaseResult = referendum.ReleaseProposal(changeProposalId, proposer.ToBase58());
            changeReleaseResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var methodFee =
                ContractManager.Token.CallViewMethod<MethodFees>(TokenMethod.GetMethodFee, new StringValue
                {
                    Value = method
                });
            methodFee.MethodName.ShouldBe(method);
            methodFee.Fees.Contains(new MethodFee
            {
                Symbol = "NET",
                BasicFee = 10000000
            }).ShouldBeTrue();
//            recover
            var recoverInput = new AuthorityInfo
            {
                ContractAddress = defaultController.ContractAddress,
                OwnerAddress = defaultController.OwnerAddress
            };
            var recoverProposalId = referendum.CreateProposal(ContractManager.Token.ContractAddress,
                nameof(ContractManager.TokenImplStub.ChangeMethodFeeController), recoverInput,
                newOrganization, proposer.ToBase58());
            var recoverVirtualAddress = referendum.GetProposalVirtualAddress(recoverProposalId);
            ContractManager.Token.ApproveToken(proposer.ToBase58(), recoverVirtualAddress.ToBase58(),
                2000_00000000, "ELF");

            referendum.SetAccount(proposer.ToBase58());
            var recoverApproveResult = referendum.ExecuteMethodWithResult(ReferendumMethod.Approve, recoverProposalId);
            recoverApproveResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var recoverReleaseResult = referendum.ReleaseProposal(recoverProposalId, proposer.ToBase58());
            recoverReleaseResult.Status.ShouldBe(TransactionResultStatus.Mined);
        }

        [TestMethod]
        public async Task IsInWhiteList_Test()
        {
            //configuration not in token white list
            var configContract = ContractManager.Genesis.GetContractAddressByName(NameProvider.Configuration);
            var result = await ContractManager.TokenStub.IsInWhiteList.CallAsync(new IsInWhiteListInput
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
                NameProvider.TokenConverter
            };
            foreach (var provider in whiteList)
            {
                var contract = ContractManager.Genesis.GetContractAddressByName(provider);
                result = await ContractManager.TokenStub.IsInWhiteList.CallAsync(new IsInWhiteListInput
                {
                    Address = contract,
                    Symbol = ContractManager.Token.GetNativeTokenSymbol()
                });
                result.Value.ShouldBe(true, $"{provider.ToString()}");
            }
        }

        [TestMethod]
        public async Task GetResourceTokenInfo_Test()
        {
            var resourceInfos = await ContractManager.TokenStub.GetResourceTokenInfo.CallAsync(new Empty());
            resourceInfos.Value.Count.ShouldBe(8);
            var resourceSymbols = resourceInfos.Value.Select(o => o.Symbol);
            resourceSymbols.ShouldBe(new[] {"WRITE", "READ", "STORAGE", "TRAFFIC", "CPU", "RAM", "DISK", "NET"});

            resourceInfos.Value.ShouldAllBe(o => o.IsBurnable);
            resourceInfos.Value.ShouldAllBe(o => o.TotalSupply == 5_0000_0000_00000000);
            resourceInfos.Value.ShouldAllBe(o => o.Decimals == 8);

            var economicContract =
                await ContractManager.GenesisImplStub.GetContractAddressByName.CallAsync(
                    HashHelper.ComputeFrom("AElf.ContractNames.Economic"));
            resourceInfos.Value.ShouldAllBe(o => o.Issuer == economicContract);
        }

        [TestMethod]
        public async Task CheckAllResourcesToken_Test()
        {
            var chainId = NodeManager.GetChainId();
            var hash = HashHelper.ComputeFrom("AElf.ContractNames.Economic");
            var economicContract = await ContractManager.GenesisImplStub.GetContractAddressByName.CallAsync(hash);
            var resourceSymbols = new[] {"CPU", "RAM", "DISK", "NET", "WRITE", "READ", "STORAGE", "TRAFFIC"};
            foreach (var symbol in resourceSymbols)
            {
                var tokenInfo = ContractManager.Token.GetTokenInfo(symbol);
                tokenInfo.Symbol.ShouldBe(symbol);
                tokenInfo.TotalSupply.ShouldBe(5_0000_0000_00000000L);
                tokenInfo.Decimals.ShouldBe(8);
                tokenInfo.Issuer.ShouldBe(economicContract);
                tokenInfo.IsBurnable.ShouldBeTrue();
                tokenInfo.IssueChainId.ShouldBe(ChainHelper.ConvertBase58ToChainId(chainId));
            }
        }
    }
}