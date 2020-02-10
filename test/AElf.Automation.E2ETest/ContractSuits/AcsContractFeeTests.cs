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
        public async Task AdoptAcs1_TransactionSizeFee_Test()
        {
            
        }

        [TestMethod]
        public async Task AdoptAcs1_TransactionTokenList_Test()
        {
            
        }

        [TestMethod]
        public async Task AdoptResource_ReadSizeFee_Test()
        {
            
        }

        [TestMethod]
        public async Task AdoptResource_StorageFee_Test()
        {
            
        }

        [TestMethod]
        public async Task AdoptResource_WriteFee_Test()
        {
            
        }

        [TestMethod]
        public async Task AdoptResource_TrafficFee_Test()
        {
            
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