using System.Linq;
using System.Threading.Tasks;
using Acs1;
using Acs3;
using AElf.Contracts.MultiToken;
using AElf.Contracts.Parliament;
using AElf.Contracts.TokenConverter;
using AElf.Types;
using AElfChain.Common.Contracts;
using Google.Protobuf.WellKnownTypes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace AElf.Automation.E2ETest.ContractSuits
{
    [TestClass]
    public class TokenConverterTests : ContractTestBase
    {
        [TestMethod]
        public async Task GetAndSetManager_Test()
        {
            var managerController =
                await ContractManager.TokenconverterStub.GetControllerForManageConnector.CallAsync(new Empty());
            var defaultOrganizationAddress =
                await ContractManager.ParliamentAuthStub.GetDefaultOrganizationAddress.CallAsync(new Empty());
            managerController.OwnerAddress.ShouldBe(defaultOrganizationAddress);

            var miners = ContractManager.Authority.GetCurrentMiners();
            var parliamentStub =
                ContractManager.ParliamentAuth.GetTestStub<ParliamentContractContainer.ParliamentContractStub>(
                    miners.First());
            var createManagerController =
                await parliamentStub.CreateOrganization.SendAsync(new CreateOrganizationInput
                {
                    ProposalReleaseThreshold = new ProposalReleaseThreshold
                    {
                        MaximalAbstentionThreshold = 1000,
                        MaximalRejectionThreshold = 1000,
                        MinimalApprovalThreshold = 2000,
                        MinimalVoteThreshold = 2000
                    }
                });
            var newControllerManager = createManagerController.Output;
            var transactionResult = ContractManager.Authority.ExecuteTransactionWithAuthority(
                ContractManager.TokenConverter.ContractAddress,
                nameof(TokenConverterMethod.ChangeConnectorController),
                new AuthorityInfo
                {
                    ContractAddress = managerController.ContractAddress,
                    OwnerAddress = newControllerManager
                }, ContractManager.CallAddress);
            transactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var newManagerAddress =
                await ContractManager.TokenconverterStub.GetControllerForManageConnector.CallAsync(new Empty());
            newManagerAddress.OwnerAddress.ShouldBe(newControllerManager);

            //revert back
            var setManagerResult = ContractManager.Authority.ExecuteTransactionWithAuthority(
                ContractManager.TokenConverter.ContractAddress,
                nameof(TokenConverterMethod.ChangeConnectorController),
                new AuthorityInfo
                {
                    ContractAddress = managerController.ContractAddress,
                    OwnerAddress = defaultOrganizationAddress
                }, ContractManager.CallAddress, newControllerManager);
            setManagerResult.Status.ShouldBe(TransactionResultStatus.Mined);
            newManagerAddress =
                await ContractManager.TokenconverterStub.GetControllerForManageConnector.CallAsync(new Empty());
            newManagerAddress.OwnerAddress.ShouldBe(defaultOrganizationAddress);
        }

        [TestMethod]
        public async Task SetFeeRate_Test()
        {
            var transactionResult = ContractManager.Authority.ExecuteTransactionWithAuthority(
                ContractManager.TokenConverter.ContractAddress, nameof(TokenConverterMethod.SetFeeRate),
                new StringValue
                {
                    Value = "0.006"
                }, ContractManager.CallAddress);
            transactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var feeRate = await ContractManager.TokenconverterStub.GetFeeRate.CallAsync(new Empty());
            feeRate.Value.ShouldBe("0.006");

            //recover back
            transactionResult = ContractManager.Authority.ExecuteTransactionWithAuthority(
                ContractManager.TokenConverter.ContractAddress, nameof(TokenConverterMethod.SetFeeRate),
                new StringValue
                {
                    Value = "0.005"
                }, ContractManager.CallAddress);
            transactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            feeRate = await ContractManager.TokenconverterStub.GetFeeRate.CallAsync(new Empty());
            feeRate.Value.ShouldBe("0.005");
        }

        [TestMethod]
        public async Task Connector_Test()
        {
            var connector = await ContractManager.TokenconverterStub.GetPairConnector.CallAsync(new TokenSymbol
            {
                Symbol = "VOTE"
            });
            if (connector.Equals(new PairConnector()))
            {
                //set connector test
                var transactionResult =
                    ContractManager.Authority.ExecuteTransactionWithAuthority(
                        ContractManager.TokenConverter.ContractAddress,
                        nameof(TokenConverterMethod.AddPairConnector),
                        new PairConnectorParam
                        {
                            ResourceWeight = "0.05",
                            NativeWeight = "0.05",
                            ResourceConnectorSymbol = "VOTE",
                            NativeVirtualBalance = 100000000_00000000
                        }, ContractManager.CallAddress);
                transactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            }

            connector = await ContractManager.TokenconverterStub.GetPairConnector.CallAsync(new TokenSymbol
            {
                Symbol = "VOTE"
            });
            connector.ResourceConnector.Symbol.ShouldBe("VOTE");
            connector.ResourceConnector.RelatedSymbol.ShouldBe("ntVOTE");
            connector.ResourceConnector.Weight.ShouldBe("0.05");

            //update
            var updateResult = ContractManager.Authority.ExecuteTransactionWithAuthority(
                ContractManager.TokenConverter.ContractAddress, nameof(TokenConverterMethod.UpdateConnector),
                new Connector
                {
                    Symbol = "VOTE",
                    RelatedSymbol = "ntVOTE",
                    IsDepositAccount = true,
                    Weight = "0.05",
                    IsPurchaseEnabled = false,
                    IsVirtualBalanceEnabled = false,
                    VirtualBalance = 1000_0000
                }, ContractManager.CallAddress);
            updateResult.Status.ShouldBe(TransactionResultStatus.Mined);
            connector = await ContractManager.TokenconverterStub.GetPairConnector.CallAsync(new TokenSymbol
            {
                Symbol = "VOTE"
            });
            connector.ResourceConnector.IsPurchaseEnabled.ShouldBeFalse();
            connector.ResourceConnector.IsVirtualBalanceEnabled.ShouldBeFalse();
        }

        [TestMethod]
        public async Task GetBaseTokenSymbol_Test()
        {
            var baseTokenSymbol = await ContractManager.TokenconverterStub.GetBaseTokenSymbol.CallAsync(new Empty());
            baseTokenSymbol.Symbol.ShouldBe("ELF");
        }

        [TestMethod]
        public async Task GetFeeReceiverAddress_Test()
        {
            var feeReceiverAddress =
                await ContractManager.TokenconverterStub.GetFeeReceiverAddress.CallAsync(new Empty());
            feeReceiverAddress.ShouldBe(ContractManager.Treasury.Contract);
        }

        [TestMethod]
        public async Task GetConnector_Test()
        {
            var elfConnector = await ContractManager.TokenconverterStub.GetPairConnector.CallAsync(new TokenSymbol
            {
                Symbol = "ELF"
            });
            elfConnector.ResourceConnector.Symbol.ShouldBe("ELF");
            elfConnector.ResourceConnector.VirtualBalance.ShouldBe(10000000000000);
            elfConnector.ResourceConnector.Weight.ShouldBe("0.5");
            elfConnector.ResourceConnector.IsVirtualBalanceEnabled.ShouldBe(true);
            elfConnector.ResourceConnector.IsPurchaseEnabled.ShouldBe(true);

            var cpuConnector = await ContractManager.TokenconverterStub.GetPairConnector.CallAsync(new TokenSymbol
            {
                Symbol = "CPU"
            });
            cpuConnector.ResourceConnector.Symbol.ShouldBe("CPU");
            cpuConnector.ResourceConnector.VirtualBalance.ShouldBe(100000);
            cpuConnector.ResourceConnector.Weight.ShouldBe("0.005");
            cpuConnector.ResourceConnector.IsVirtualBalanceEnabled.ShouldBe(true);
            cpuConnector.ResourceConnector.IsPurchaseEnabled.ShouldBe(true);
        }

        [TestMethod]
        public async Task EnableConnector_Test()
        {
            var tokenContract = new TokenTests();
            var symbol = await tokenContract.TokenCreateAndIssue_Test();

            //set connector test
            var transactionResult =
                ContractManager.Authority.ExecuteTransactionWithAuthority(
                    ContractManager.TokenConverter.ContractAddress,
                    nameof(TokenConverterMethod.AddPairConnector),
                    new PairConnectorParam
                    {
                        ResourceWeight = "0.05",
                        NativeWeight = "0.05",
                        NativeVirtualBalance = 20000_00000000,
                        ResourceConnectorSymbol = symbol
                    }, ContractManager.CallAddress);
            transactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var connector = await ContractManager.TokenconverterStub.GetPairConnector.CallAsync(new TokenSymbol
            {
                Symbol = symbol
            });
            connector.ResourceConnector.VirtualBalance.ShouldBe(0);

            //set allowance
            var allowanceResult = await ContractManager.TokenStub.Approve.SendAsync(new ApproveInput
            {
                Amount = 20000_00000000,
                Spender = ContractManager.TokenConverter.Contract,
                Symbol = symbol
            });
            allowanceResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var enableResult =
                await ContractManager.TokenconverterStub.EnableConnector.SendAsync(new ToBeConnectedTokenInfo
                {
                    TokenSymbol = symbol,
                    AmountToTokenConvert = 20000_00000000
                });
            enableResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            connector = await ContractManager.TokenconverterStub.GetPairConnector.CallAsync(new TokenSymbol
            {
                Symbol = symbol
            });
            connector.ResourceConnector.IsPurchaseEnabled.ShouldBeTrue();
        }
    }
}