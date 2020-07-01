using System.Text;
using System.Threading.Tasks;
using AElf.Contracts.TestContract.BasicFunction;
using AElf.Contracts.TestContract.BasicSecurity;
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
        private readonly string Caller = "28Y8JA1i2cN6oHvdv7EraXJr9a1gY6D1PpJXw9QtRMRwKcBQMK";
        private readonly string ContractAddress = "2WHXRoLRjbUTDQsuqR5CntygVfnDb125qdJkudev4kVNbLhTdG";
        private readonly string SecurityContractAddress = "2LUmicHyH4RXrMjG4beDwuDsiWJESyLkgkwPdGTR8kahRzq5XS";

        private INodeManager NodeManager { get; set; }
        public BasicFunctionContractContainer.BasicFunctionContractStub BasicFunctionContractStub { get; set; }
        public BasicSecurityContractContainer.BasicSecurityContractStub BasicSecurityContractStub { get; set; }
        public const int StateSizeLimit = 128 * 1024;

        [TestInitialize]
        public void TestInitialize()
        {
            Log4NetHelper.LogInit();
            NodeManager = new NodeManager("192.168.197.21:8000");
            var basicFunction = new BasicFunctionContract(NodeManager, Caller, ContractAddress);
            BasicFunctionContractStub = basicFunction.GetTestStub<BasicFunctionContractContainer.BasicFunctionContractStub>(Caller);
            var security = new BasicSecurityContract(NodeManager, Caller, SecurityContractAddress);
            BasicSecurityContractStub =
                security.GetTestStub<BasicSecurityContractContainer.BasicSecurityContractStub>(Caller);
//            BasicSecurityContractStub.InitialBasicSecurityContract.SendAsync(Address.FromBase58(ContractAddress));
        }

        [TestMethod]
        public async Task StringStateTest()
        {
            var str1 = Encoding.UTF8.GetString(new byte[StateSizeLimit + 1]);
            var str2 = Encoding.UTF8.GetString(new byte[StateSizeLimit]);

            var errorResult = await BasicSecurityContractStub.TestStringState.SendAsync(new StringInput
            {
                StringValue = str1
            });
            errorResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.NodeValidationFailed);
            
            var result1 = await BasicSecurityContractStub.TestStringState.SendAsync(new StringInput
            {
                StringValue = str2
            });
            result1.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            
            str2 = Encoding.UTF8.GetString(new byte[StateSizeLimit -1]);
            var result2 = await BasicSecurityContractStub.TestStringState.SendAsync(new StringInput
            {
                StringValue = str2
            });
            result2.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var getResult = await BasicSecurityContractStub.QueryStringState.CallAsync(new Empty());
            getResult.StringValue.ShouldBe(str2);
        }
        
        [TestMethod]
        public async Task ProtoStateTest()
        {
            var str1 = Encoding.UTF8.GetString(new byte[StateSizeLimit + 1]);
            var str2 = Encoding.UTF8.GetString(new byte[StateSizeLimit - 100]);

            var errorResult = await BasicSecurityContractStub.TestProtobufState.SendAsync(new ProtobufInput
            {
                ProtobufValue = new ProtobufMessage
                {
                    StringValue = str1
                }
            });
            errorResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.NodeValidationFailed);
            
//            var getResult = await BasicSecurityContractStub.QueryProtobufState.CallAsync(new Empty());
//            getResult.ProtobufValue.ShouldBeNull();
            
            var result = await BasicSecurityContractStub.TestProtobufState.SendAsync(new ProtobufInput
            {
                ProtobufValue = new ProtobufMessage
                {
                    StringValue = str2,
                    BoolValue = true,
                    Int64Value = 1000_00000000
                }
            });
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var size = result.Transaction.CalculateSize();
            Logger.Info($"{size}"); 
            var  getResult = await BasicSecurityContractStub.QueryProtobufState.CallAsync(new Empty());
            getResult.ProtobufValue.StringValue.ShouldBe(str2);
            getResult.ProtobufValue.BoolValue.ShouldBeTrue();
            getResult.ProtobufValue.Int64Value.ShouldBe(1000_00000000);
        }

        [TestMethod]
        public async Task TestMappedState()
        {
            var str1 = Encoding.UTF8.GetString(new byte[StateSizeLimit]);

            var errorResult = await BasicSecurityContractStub.TestMappedState.SendAsync(new ProtobufInput
            {
                ProtobufValue = new ProtobufMessage
                {
                    Int64Value = 10,
                    StringValue = str1
                }
            });
            errorResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.NodeValidationFailed);

            var getResult = await BasicSecurityContractStub.QueryMappedState.CallAsync(new ProtobufInput
            {
                ProtobufValue = new ProtobufMessage
                {
                    Int64Value = 10
                }
            });
            getResult.ShouldBe(new ProtobufMessage());
            
            var result1 = await BasicSecurityContractStub.TestMappedState.SendAsync(new ProtobufInput
            {
                ProtobufValue = new ProtobufMessage
                {
                    Int64Value = 10,
                    StringValue = "TEST"
                }
            });
            result1.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            getResult = await BasicSecurityContractStub.QueryMappedState.CallAsync(new ProtobufInput
            {
                ProtobufValue = new ProtobufMessage()
                {
                    Int64Value = 10
                }
            });
            getResult.StringValue.ShouldBe("TEST");
            getResult.BoolValue.ShouldBeTrue();
        }
        
        [TestMethod]
        public  async Task ThroughParliament()
        {
            var result1 = await BasicSecurityContractStub.TestBytesState.SendAsync(new BytesInput
            {
                BytesValue = ByteString.CopyFrom(new byte[StateSizeLimit - 3])
            });
            result1.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            
            var input = new BytesInput
            {
                BytesValue = ByteString.CopyFrom(new byte[StateSizeLimit - 3])
            };
            
            var authority = new AuthorityManager(NodeManager,Caller);
            var result = authority.ExecuteTransactionWithAuthority(SecurityContractAddress,
                nameof(SecurityMethod.TestBytesState), input,Caller);
            result.Status.ShouldBe(TransactionResultStatus.Mined);
        }
        
    }
}