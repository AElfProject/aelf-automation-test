using System.Threading.Tasks;
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
            var managerAddress = await ContractManager.TokenconverterStub.GetManagerAddress.CallAsync(new Empty());
            var defaultOrganizationAddress =
                await ContractManager.ParliamentAuthStub.GetDefaultOrganizationAddress.CallAsync(new Empty());
            managerAddress.ShouldBe(defaultOrganizationAddress);

            var transactionResult = ContractManager.Authority.ExecuteTransactionWithAuthority(
                ContractManager.TokenConverter.ContractAddress, nameof(TokenConverterMethod.SetManagerAddress),
                ContractManager.CallAccount, ContractManager.CallAddress);
            transactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            
            var newManagerAddress = await ContractManager.TokenconverterStub.GetManagerAddress.CallAsync(new Empty());
            newManagerAddress.ShouldBe(ContractManager.CallAccount);
            
            //revert back
            var setManagerResult = await ContractManager.TokenconverterStub.SetManagerAddress.SendAsync(managerAddress);
            setManagerResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            newManagerAddress = await ContractManager.TokenconverterStub.GetManagerAddress.CallAsync(new Empty());
            newManagerAddress.ShouldBe(defaultOrganizationAddress);
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
            var connector = await ContractManager.TokenconverterStub.GetConnector.CallAsync(new TokenSymbol
            {
                Symbol = "VOTE"
            });
            if (connector.Equals(new Connector()))
            {
                //set connector test
                var transactionResult =
                    ContractManager.Authority.ExecuteTransactionWithAuthority(ContractManager.TokenConverter.ContractAddress, nameof(TokenConverterMethod.AddPairConnectors),
                        new PairConnector
                    {
                        ResourceWeight = "0.05",
                        NativeWeight = "0.05",
                        ResourceConnectorSymbol = "VOTE"
                    }, ContractManager.CallAddress);
                transactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            }
            
            connector = await ContractManager.TokenconverterStub.GetConnector.CallAsync(new TokenSymbol
            {
                Symbol = "VOTE"
            });
            connector.Symbol.ShouldBe("VOTE");
            connector.RelatedSymbol.ShouldBe("ntVOTE");
            connector.Weight.ShouldBe("0.05");
            
            //update
            connector.VirtualBalance = 1000_0000;
            connector.IsPurchaseEnabled = false;
            connector.IsVirtualBalanceEnabled = false;
            var updateResult = ContractManager.Authority.ExecuteTransactionWithAuthority(ContractManager.TokenConverter.ContractAddress, nameof(TokenConverterMethod.UpdateConnector),
                connector, ContractManager.CallAddress);
            updateResult.Status.ShouldBe(TransactionResultStatus.Mined);
            connector = await ContractManager.TokenconverterStub.GetConnector.CallAsync(new TokenSymbol
            {
                Symbol = "VOTE"
            });
            connector.IsPurchaseEnabled.ShouldBeFalse();
            connector.IsVirtualBalanceEnabled.ShouldBeFalse();
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
        public async Task GetTokenContractAddress_Test()
        {
            var address = await ContractManager.TokenconverterStub.GetTokenContractAddress.CallAsync(new Empty());
            address.ShouldBe(ContractManager.Token.Contract);
        }

        [TestMethod]
        public async Task GetConnector_Test()
        {
            var elfConnector = await ContractManager.TokenconverterStub.GetConnector.CallAsync(new TokenSymbol
            {
                Symbol = "ELF"
            });
            elfConnector.Symbol.ShouldBe("ELF");
            elfConnector.VirtualBalance.ShouldBe(10000000000000);
            elfConnector.Weight.ShouldBe("0.5");
            elfConnector.IsVirtualBalanceEnabled.ShouldBe(true);
            elfConnector.IsPurchaseEnabled.ShouldBe(true);

            var cpuConnector = await ContractManager.TokenconverterStub.GetConnector.CallAsync(new TokenSymbol
            {
                Symbol = "CPU"
            });
            cpuConnector.Symbol.ShouldBe("CPU");
            cpuConnector.VirtualBalance.ShouldBe(100000);
            cpuConnector.Weight.ShouldBe("0.005");
            cpuConnector.IsVirtualBalanceEnabled.ShouldBe(true);
            cpuConnector.IsPurchaseEnabled.ShouldBe(true);
        }
    }
}