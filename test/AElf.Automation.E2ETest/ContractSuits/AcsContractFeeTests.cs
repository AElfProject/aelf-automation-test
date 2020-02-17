using System.Linq;
using System.Threading.Tasks;
using Acs1;
using AElf.Contracts.MultiToken;
using AElf.Types;
using AElfChain.Common.Managers;
using Google.Protobuf.WellKnownTypes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace AElf.Automation.E2ETest.ContractSuits
{
    [TestClass]
    public class AcsContractFeeTests : ContractTestBase
    {
        public AuthorityManager Authority => ContractManager.Authority;
        
        [TestMethod]
        public async Task AdoptAcs1_TransactionTokenList_Test()
        {
            var availableTokenInfo = new SymbolListToPayTXSizeFee
            {
                SymbolsToPayTxSizeFee =
                {
                    new SymbolToPayTXSizeFee
                    {
                        TokenSymbol = "ELF",
                        AddedTokenWeight = 1,
                        BaseTokenWeight = 1
                    },
                    new SymbolToPayTXSizeFee
                    {
                        TokenSymbol = "CPU",
                        AddedTokenWeight = 50,
                        BaseTokenWeight = 1
                    },
                    new SymbolToPayTXSizeFee
                    {
                        TokenSymbol = "RAM",
                        AddedTokenWeight = 50,
                        BaseTokenWeight = 1
                    },
                    new SymbolToPayTXSizeFee
                    {
                        TokenSymbol = "NET",
                        AddedTokenWeight = 50,
                        BaseTokenWeight = 1
                    }
                }
            };

            var transactionResult = ContractManager.Authority.ExecuteTransactionWithAuthority(
                ContractManager.Token.ContractAddress, nameof(ContractManager.TokenStub.SetSymbolsToPayTXSizeFee),
                availableTokenInfo, ContractManager.CallAddress);
            transactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var tokenInfos = await ContractManager.TokenStub.GetSymbolsToPayTXSizeFee.CallAsync(new Empty());
            tokenInfos.SymbolsToPayTxSizeFee.ShouldBe(availableTokenInfo.SymbolsToPayTxSizeFee);
        }

        [TestMethod]
        [DataRow("Transfer", 1000_0000L)]
        public void AdoptContract_TokenTransactionMethodFee_Test(string method, long methodFee)
        {
            var primaryToken = NodeManager.GetPrimaryTokenSymbol();
            var transactionResult = Authority.ExecuteTransactionWithAuthority(
                ContractManager.Token.ContractAddress,
                nameof(TokenContractImplContainer.TokenContractImplStub.SetMethodFee),
                new MethodFees
                {
                    MethodName = method,
                    Fees =
                    {
                        new MethodFee
                        {
                            Symbol = primaryToken,
                            BasicFee = methodFee
                        }
                    }
                },
                ContractManager.CallAddress
            );
            transactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            
            //Query transaction fee
            var transactionFee = ContractManager.Token.CallViewMethod<MethodFees>(
                nameof(TokenContractImplContainer.TokenContractImplStub.GetMethodFee),
                new StringValue
                {
                    Value = method
                });
            var basicFee = transactionFee.Fees.First();
            basicFee.Symbol.ShouldBe(primaryToken);
            basicFee.BasicFee.ShouldBe(methodFee);
        }
    }
}