using System;
using System.Text;
using System.Threading.Tasks;
using Acs1;
using AElf.Contracts.TestContract.BasicFunction;
using AElf.Contracts.TestContract.BasicSecurity;
using AElf.Types;
using AElfChain.Common.Contracts;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using log4net;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Org.BouncyCastle.Bcpg;
using Shouldly;

namespace AElf.Automation.Contracts.ScenarioTest
{
    [TestClass]
    public class BasicUpdateTests
    {
        private static readonly ILog Logger = Log4NetHelper.GetLogger();
        private readonly string Caller = "28Y8JA1i2cN6oHvdv7EraXJr9a1gY6D1PpJXw9QtRMRwKcBQMK";
        private readonly string ContractAddress = "2LUmicHyH4RXrMjG4beDwuDsiWJESyLkgkwPdGTR8kahRzq5XS";
        private readonly string SecurityContractAddress = "DHo2K7oUXXq3kJRs1JpuwqBJP56gqoaeSKFfuvr9x8svf3vEJ";

        private INodeManager NodeManager { get; set; }
        public BasicFunctionContractContainer.BasicFunctionContractStub BasicFunctionContractStub { get; set; }
        public BasicSecurityContractContainer.BasicSecurityContractStub BasicSecurityContractStub { get; set; }
        public const int StateSizeLimit = 128 * 1024;

        [TestInitialize]
        public void TestInitialize()
        {
            Log4NetHelper.LogInit();
            NodeManager = new NodeManager("192.168.197.44:8000");
            var basicFunction = new BasicFunctionContract(NodeManager, Caller, ContractAddress);
            BasicFunctionContractStub = basicFunction.GetTestStub<BasicFunctionContractContainer.BasicFunctionContractStub>(Caller);
            var security = new BasicSecurityContract(NodeManager, Caller, SecurityContractAddress);
            BasicSecurityContractStub =
                security.GetTestStub<BasicSecurityContractContainer.BasicSecurityContractStub>(Caller);
//            BasicSecurityContractStub.InitialBasicSecurityContract.SendAsync(Address.FromBase58(ContractAddress));
        }

        [TestMethod]
        public async Task Initialize()
        {
            var result =
                await BasicFunctionContractStub.InitialBasicFunctionContract.SendAsync(new InitialBasicContractInput
                {
                    Manager = Caller.ConvertAddress(),
                    MaxValue = 1000,
                    MinValue = 1
                });
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        }

        [TestMethod]
        public async Task SetMethodFee()
        {
            var symbol = "NOBURN";
            var amount = 1_00000000000;
            var fee = await BasicFunctionContractStub.GetMethodFee.CallAsync(new StringValue
            {
                Value = nameof(BasicFunctionContractStub.UpdateBetLimit)
            });
            Logger.Info(fee);
//            if (fee.Fees.Count > 0) return;
            var input = new MethodFees
            {
                MethodName = nameof(BasicFunctionContractStub.UpdateBetLimit),
                Fees =
                {
                    new MethodFee
                    {
                        BasicFee = amount,
                        Symbol = symbol
                    }
                }
            };
            var result = await BasicFunctionContractStub.SetMethodFee.SendAsync(input);
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        }

        [TestMethod]
        public async Task CheckFee()
        {
            var genesisService = GenesisContract.GetGenesisContract(NodeManager, Caller);
            var token = genesisService.GetTokenContract();
            var balance = token.GetUserBalance(Caller, "NOBURN");
            var result = await BasicFunctionContractStub.UpdateBetLimit.SendAsync(new BetLimitInput
            {
                MaxValue = 200000,
                MinValue = 10
            });
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            
            var afterBalance = token.GetUserBalance(Caller, "NOBURN");
            Logger.Info($"{balance} {afterBalance}");

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

        [TestMethod]
        public async Task TestWhileLoop()
        {
            var result = await BasicSecurityContractStub.TestWhileInfiniteLoop.SendAsync(new Int32Input{Int32Value = 100});
            result.Output.Int32Value.ShouldBe(100);
            var result2 = await BasicSecurityContractStub.TestWhileInfiniteLoop.SendAsync(new Int32Input{Int32Value = 14999});
            result2.Output.Int32Value.ShouldBe(14999);
            var result3 = await BasicSecurityContractStub.TestWhileInfiniteLoop.SendAsync(new Int32Input{Int32Value = 15000});
            result3.TransactionResult.Status.ShouldBe(TransactionResultStatus.NodeValidationFailed);
            result3.TransactionResult.Error.ShouldContain("BranchCount");

        }

        [TestMethod]
        public async Task TestForLoop()
        {
            var result =
                await BasicSecurityContractStub.TestForInfiniteLoop.SendAsync(new Int32Input {Int32Value = 14999});
            result.Output.Int32Value.ShouldBe(14999);
            var result2 = await BasicSecurityContractStub.TestForInfiniteLoop.SendAsync(new Int32Input{Int32Value = 15000});
            result2.TransactionResult.Status.ShouldBe(TransactionResultStatus.NodeValidationFailed);
            result2.TransactionResult.Error.ShouldContain("BranchCount");
            var result3 = await BasicSecurityContractStub.TestForInfiniteLoop.SendAsync(new Int32Input{Int32Value = 15001});
            result3.TransactionResult.Status.ShouldBe(TransactionResultStatus.NodeValidationFailed);
            result3.TransactionResult.Error.ShouldContain("BranchCount");
        }

        [TestMethod]
        public async Task TestInfiniteLoopWithSend()
        {
            var result =
                await BasicSecurityContractStub.TestInfiniteLoopWithSend.SendAsync(new Int32Input {Int32Value = 100});
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var result2 =
                await BasicSecurityContractStub.TestInfiniteLoopWithSend.SendAsync(new Int32Input {Int32Value = 2000});
            result2.TransactionResult.Status.ShouldBe(TransactionResultStatus.NodeValidationFailed);
            var result3 =
                await BasicSecurityContractStub.TestInfiniteLoopWithSend.SendAsync(new Int32Input {Int32Value = 5000});
            result3.TransactionResult.Status.ShouldBe(TransactionResultStatus.NodeValidationFailed);

        }

        [TestMethod]
        public async Task TestFunctionInfiniteLoop()
        {
            var result =
                await BasicSecurityContractStub.TestFunctionInfiniteLoop.SendAsync(new Int32Input {Int32Value = 100});
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var result2 =
                await BasicSecurityContractStub.TestFunctionInfiniteLoop.SendAsync(new Int32Input {Int32Value = 10000});
            result2.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var result3 =
                await BasicSecurityContractStub.TestFunctionInfiniteLoop.SendAsync(new Int32Input {Int32Value = 15000});
            result3.TransactionResult.Status.ShouldBe(TransactionResultStatus.NodeValidationFailed);
            result3.TransactionResult.Error.ShouldContain("BranchCount");
        }

        [TestMethod]
        public async Task TestInfiniteRecursiveCall()
        {
            var result =
                await BasicSecurityContractStub.TestInfiniteRecursiveCall.SendAsync(new Int32Input {Int32Value = 100});
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var result2 =
                await BasicSecurityContractStub.TestInfiniteRecursiveCall.SendAsync(new Int32Input {Int32Value = 10000});
            result2.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var result3 =
                await BasicSecurityContractStub.TestInfiniteRecursiveCall.SendAsync(new Int32Input {Int32Value = 15000});
            result3.TransactionResult.Status.ShouldBe(TransactionResultStatus.NodeValidationFailed);
            result3.TransactionResult.Error.ShouldContain("CallCount");
        }
        
        [TestMethod]
        public async Task TestInfiniteLoop()
        {
            var result =
                await BasicSecurityContractStub.TestInfiniteLoop.SendAsync(new Int32Input {Int32Value = 100});
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var result2 =
                await BasicSecurityContractStub.TestInfiniteLoop.SendAsync(new Int32Input {Int32Value = 380});
            result2.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var result3 =
                await BasicSecurityContractStub.TestInfiniteLoop.SendAsync(new Int32Input {Int32Value = 10000});
            result3.TransactionResult.Status.ShouldBe(TransactionResultStatus.NodeValidationFailed);
            result3.TransactionResult.Error.ShouldContain("BranchCount");
        }

        [TestMethod]
        public async Task TestWhileInfiniteLoopWithState()
        {
            var result =
                await BasicSecurityContractStub.TestWhileInfiniteLoopWithState.SendAsync(new Int32Input
                    {Int32Value = 100});
            result.Output.Int32Value.ShouldBe(98);
            var result2 =
                await BasicSecurityContractStub.TestWhileInfiniteLoopWithState.SendAsync(new Int32Input
                    {Int32Value = 14999});
            result2.Output.Int32Value.ShouldBe(14994);
            var result3 =
                await BasicSecurityContractStub.TestWhileInfiniteLoopWithState.SendAsync(new Int32Input
                    {Int32Value = 15000});
            result3.TransactionResult.Status.ShouldBe(TransactionResultStatus.NodeValidationFailed);
            result3.TransactionResult.Error.ShouldContain("BranchCount");
        }

        [TestMethod]
        public async Task TestInfiniteLoopWithCall()
        {
            var result = await BasicSecurityContractStub.TestInfiniteLoopWithCall.SendAsync(new Int32Input{Int32Value = 100});
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var result1 =
                await BasicSecurityContractStub.TestInfiniteLoopWithCall.SendAsync(new Int32Input {Int32Value = 5000});
            result1.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var result2 =
                await BasicSecurityContractStub.TestInfiniteLoopWithCall.SendAsync(new Int32Input {Int32Value = 14999});
            result2.TransactionResult.Status.ShouldBe(TransactionResultStatus.NodeValidationFailed);
            result2.TransactionResult.Error.ShouldContain("CallCount");
        }
        
        [TestMethod]
        public async Task TestInfiniteLoopWhitIf()
        {
            var result = await BasicSecurityContractStub.TestInfiniteLoopWhitIf.SendAsync(new Int32Input{Int32Value = 100});
            result.Output.Int32Value.ShouldBe(100);
            var result1 =
                await BasicSecurityContractStub.TestInfiniteLoopWhitIf.SendAsync(new Int32Input {Int32Value = 14998});
            result1.Output.Int32Value.ShouldBe(14998);
            var result2 =
                await BasicSecurityContractStub.TestInfiniteLoopWhitIf.SendAsync(new Int32Input {Int32Value = 14999});
            result2.Output.Int32Value.ShouldBe(14999);
            var result3 =
                await BasicSecurityContractStub.TestInfiniteLoopWhitIf.SendAsync(new Int32Input {Int32Value = 15000});
            result3.TransactionResult.Status.ShouldBe(TransactionResultStatus.NodeValidationFailed);
            result3.TransactionResult.Error.ShouldContain("BranchCount");
        }
        
        [TestMethod]
        public async Task TestInfiniteLoopWithSendInline()
        {
            var result = await BasicSecurityContractStub.TestInfiniteLoopWithSendInline.SendAsync(new Int32Input{Int32Value = 100});
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var result1 =
                await BasicSecurityContractStub.TestInfiniteLoopWithSendInline.SendAsync(new Int32Input {Int32Value = 1000});
            result1.TransactionResult.Status.ShouldBe(TransactionResultStatus.NodeValidationFailed);
        }
        
        [TestMethod]
        public async Task TestInfiniteLoopWithSendVirtualInline()
        {
            var result = await BasicSecurityContractStub.TestInfiniteLoopWithSendVirtualInline.SendAsync(new Int32Input{Int32Value = 1000});
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var result1 =
                await BasicSecurityContractStub.TestInfiniteLoopWithSendVirtualInline.SendAsync(new Int32Input {Int32Value = 2000});
            result1.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        }
        

        [TestMethod]
        public async Task TestForeachLoop()
        {
            var result = await BasicSecurityContractStub.TestForeachInfiniteLoop.SendAsync(new StringInput{StringValue = "1234567890"});
            result.Output.Int32Value.ShouldBe(4);
            var str = GenerateCheckCode(15000);
            str.Length.ShouldBe(15000);
            var result1= await BasicSecurityContractStub.TestForeachInfiniteLoop.SendAsync(new StringInput{StringValue = str});
            result1.Output.Int32Value.ShouldBe(4);
        }

        private string GenerateCheckCode(int num)
        {
            int number;
            char code;
            string checkCode = String.Empty;
            Random random = new Random();
            for (int i = 0; i < num; i++)
            {
                number = random.Next();
                if (number % 2 == 0)
                    code = (char) ('0' + (char) (number % 10));
                else
                    code = (char) ('A' + (char) (number % 26));
                checkCode += code.ToString();
            }

            return checkCode;
        }
    }
}