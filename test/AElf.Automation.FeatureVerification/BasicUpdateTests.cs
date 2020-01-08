using System;
using System.Threading.Tasks;
using AElf.Contracts.TestContract.BasicUpdate;
using AElf.Types;
using AElfChain.Common.Contracts;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using log4net;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace AElf.Automation.Contracts.ScenarioTest
{
    [TestClass]
    public class BasicUpdateTests
    {
        private static readonly ILog Logger = Log4NetHelper.GetLogger();
        
        private INodeManager NodeManager { get; set; }
        private string ContractAddress = "uSXxaGWKDBPV6Z8EG8Et9sjaXhH1uMWEpVvmo2KzKEaueWzSe";
        private string Caller = "28Y8JA1i2cN6oHvdv7EraXJr9a1gY6D1PpJXw9QtRMRwKcBQMK";
        public BasicUpdateContractContainer.BasicUpdateContractStub BasicUpdateStub { get; set; }
        
        [TestInitialize]
        public void TestInitialize()
        {
            Log4NetHelper.LogInit();  
            NodeManager = new NodeManager("192.168.197.40:8000");
            var basicUpdate = new BasicUpdateContract(NodeManager, Caller, ContractAddress);
            BasicUpdateStub = basicUpdate.GetTestStub<BasicUpdateContractContainer.BasicUpdateContractStub>(Caller);
        }

        [TestMethod]
        public async Task GetHashCodeBytesValue_Test()
        {
            var result = await BasicUpdateStub.GetHashCodeBytesValue.SendAsync(new BytesValue
            {
                Value = ByteString.CopyFromUtf8("test info")
            });
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            Logger.Info($"GetHashCodeBytesValue =>{result.Output.Value}");
        }

        [TestMethod]
        public async Task GetHashCodeInt32Value_Test()
        {
            var result = await BasicUpdateStub.GetHashCodeInt32Value.SendAsync(new Int32Value
            {
                Value = Int32.MaxValue
            });
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            Logger.Info($"GetHashCodeInt32Value =>{result.Output.Value}");
        }

        [TestMethod]
        public async Task GetHashCodeInt64Value_Test()
        {
            var result = await BasicUpdateStub.GetHashCodeInt64Value.SendAsync(new Int64Value
            {
                Value = Int64.MaxValue
            });
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            Logger.Info($"GetHashCodeInt64Value =>{result.Output.Value}");
        }

        [TestMethod]
        public async Task GetHashCodeStringValue_Test()
        {
            var result = await BasicUpdateStub.GetHashCodeStringValue.SendAsync(new StringValue
            {
                Value = "proto buf string test info"
            });
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            Logger.Info($"GetHashCodeStringValue =>{result.Output.Value}");
        }

        [TestMethod]
        public async Task GetHashCodeEnumValue_Test()
        {
            var result = await BasicUpdateStub.GetHashCodeEnumValue.SendAsync(new EnumInput
            {
                Info = Color.Black
            });
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            Logger.Info($"GetHashCodeEnumValue =>{result.Output.Value}");
        }

        [TestMethod]
        public async Task GetHashCodeComplexValue_Test()
        {
            var result = await BasicUpdateStub.GetHashCodeComplexValue.SendAsync(new ComplexInput
            {
                StringValue = "just string info",
                IntValue = 249,
                LongValue = 4679L,
                EnumValue = Color.White
            });
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            Logger.Info($"GetHashCodeComplexValue =>{result.Output.Value}");
        }

        [TestMethod]
        public async Task GetHashCodeMapStringValue_Test()
        {
            var result = await BasicUpdateStub.GetHashCodeMapStringValue.SendAsync(new MapStringInput
            {
                Info =
                {
                    {"key1", "test1"},
                    {"key2", "test1"},
                    {"key3", "test1"}
                }
            });
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            Logger.Info($"GetHashCodeComplexValue =>{result.Output.Value}");
        }

        [TestMethod]
        public async Task GetHashCodeMapEnumValue_Test()
        {
            var result = await BasicUpdateStub.GetHashCodeMapEnumValue.SendAsync(new MapEnumInput
            {
                Info =
                {
                    {"key1", Color.Black},
                    {"key2", Color.Blue},
                    {"key3", Color.White}
                }
            });
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            Logger.Info($"GetHashCodeComplexValue =>{result.Output.Value}");
        }
    }
}