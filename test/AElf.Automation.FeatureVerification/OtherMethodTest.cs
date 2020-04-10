using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AElf.Client.Dto;
using AElf.Contracts.Configuration;
using AElf.Contracts.Election;
using AElf.Contracts.MultiToken;
using AElf.Contracts.TestContract.BasicUpdate;
using AElf.CSharp.Core.Extension;
using AElf.Types;
using AElfChain.Common;
using AElfChain.Common.Contracts;
using AElfChain.Common.Contracts.Serializer;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using AElfChain.Contract;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using log4net;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using Shouldly;
using Xunit;

namespace AElf.Automation.Contracts.ScenarioTest
{
    [TestClass]
    public class OtherMethodTest
    {
        private static readonly ILog Logger = Log4NetHelper.GetLogger();

        [TestInitialize]
        public void TestInitialize()
        {
            Log4NetHelper.LogInit();
        }

        [TestMethod]
        public void ConvertFromHex()
        {
            var message = DataHelper.ConvertHexToString(
                "454c465f32476b44317137344877427246734875666d6e434b484a7661475642596b6d59636447337565624573415753737058");
            Assert.IsTrue(message == "ELF_2GkD1q74HwBrFsHufmnCKHJvaGVBYkmYcdG3uebEsAWSspX");
        }

        [TestMethod]
        public void ConvertTest()
        {
            var rpcMessage =
                "{\"result\":\"Mined\", \"message\":\"Test successful.\", \"return_code\":\"90000\", \"detail\":{\"info\":\"successful\"}}";
            var result1 = DataHelper.TryGetValueFromJson(out var message1, rpcMessage, "return_code");
            var result2 = DataHelper.TryGetValueFromJson(out var message2, rpcMessage, "detail", "info");
        }

        [TestMethod]
        public void ProtoMessageRead_Test()
        {
            var address = AddressExtension.Generate();
            var stream = new MemoryStream();
            address.WriteTo(stream);

            var value = address.GetFormatted();

            var hash = HashHelper.HexStringToHash("a6d05b63cb36687116e8d2ed791e9806652c370d40184f43a7e4fda08f5e29b1");
            var jsonInfo = JsonFormatter.Default.Format(hash);

            var convertHash = JsonParser.Default.Parse(jsonInfo, Hash.Descriptor);

            var voteInput = new VoteMinerInput
            {
                CandidatePubkey =
                    "04b6c07711bc30cdf98c9f081e70591f98f2ba7ff971e5a146d47009a754dacceb46813f92bc82c700971aa93945f726a96864a2aa36da4030f097f806b5abeca4",
                Amount = 100_00000000,
                EndTimestamp = KernelHelper.GetUtcNow().AddDays(120)
            };
            var voteOutput = JsonFormatter.Default.Format(voteInput);
        }

        [TestMethod]
        public void AccountCreate()
        {
            var keyStore = AElfKeyStore.GetKeyStore();
            var accountManager = new AccountManager(keyStore);
            for (var i = 0; i < 10; i++)
            {
                var accountInfo = accountManager.NewAccount(NodeOption.DefaultPassword);
                Console.WriteLine($"Account: {accountInfo}");

                var publicKey = accountManager.GetPublicKey(accountInfo, NodeOption.DefaultPassword);
                Console.WriteLine($"Public Key: {publicKey}");

                Console.WriteLine();
            }
        }

        [TestMethod]
        public void StringValueParse()
        {
            var info = new StringValue
            {
                Value = "test info"
            };
            var jsonInfo = JsonFormatter.Default.Format(info);

            var msg = JsonParser.Default.Parse(jsonInfo, StringValue.Descriptor);
        }

        [TestMethod]
        public void ProtoTypeConvertTest()
        {
            var create = new CreateInput
            {
                Symbol = "TELF",
                TokenName = "token test name",
                TotalSupply = 8000_00000000,
                Decimals = 8,
                Issuer = "28Y8JA1i2cN6oHvdv7EraXJr9a1gY6D1PpJXw9QtRMRwKcBQMK".ConvertAddress(),
                IsBurnable = true,
                IssueChainId = 9992731
            };
            var output = JsonFormatter.Default.Format(create);
            Console.WriteLine(output);
            var output1 = JsonFormatter.ToDiagnosticString(create);
            Console.WriteLine(output1);
        }

        [TestMethod]
        public void ConvertProtoMethod()
        {
            var handler = new ContractSerializer();
            var tokenInfo = handler.GetContractInfo(NameProvider.Token);
            var createMethod = tokenInfo.GetContractMethod("Create");
            var result = bool.Parse("true");
            var result1 = bool.Parse("false");
        }

        [TestMethod]
        public void ContractMethodSerialize()
        {
            var nodeManager = new NodeManager("192.168.197.14:8000");
            var genesis = nodeManager.GetGenesisContract();
            var token = genesis.GetTokenContract();
            var byteInfo = nodeManager.ApiClient.GetContractFileDescriptorSetAsync(token.ContractAddress).Result;
            var customContractHandler = new CustomContractSerializer(byteInfo);
            customContractHandler.GetAllMethodsInfo(true);
            customContractHandler.GetParameters("Create");
        }

        [TestMethod]
        public async Task SendRawTransaction()
        {
            var nodeManager = new NodeManager("192.168.197.43:8100");
            var genesis = nodeManager.GetGenesisContract();
            var token = genesis.GetTokenContract();
            var height = await nodeManager.ApiClient.GetBlockHeightAsync();
            var block = await nodeManager.ApiClient.GetBlockByHeightAsync(height);
            var createRaw = await nodeManager.ApiClient.CreateRawTransactionAsync(new CreateRawTransactionInput
            {
                From = token.CallAddress,
                To = token.ContractAddress,
                MethodName = "GetBalance",
                Params = new JObject
                {
                    ["symbol"] = "ELF",
                    ["owner"] = new JObject
                    {
                        ["value"] = "28Y8JA1i2cN6oHvdv7EraXJr9a1gY6D1PpJXw9QtRMRwKcBQMK".ConvertAddress().Value
                            .ToBase64()
                    }
                }.ToString(),
                RefBlockNumber = height,
                RefBlockHash = block.BlockHash
            });
            var transactionId =
                Hash.FromRawBytes(ByteArrayHelper.HexStringToByteArray(createRaw.RawTransaction));
            var signature = nodeManager.TransactionManager.Sign(token.CallAddress, transactionId.ToByteArray())
                .ToByteArray().ToHex();
            var rawTransactionResult =
                await nodeManager.ApiClient.ExecuteRawTransactionAsync(new ExecuteRawTransactionDto
                {
                    RawTransaction = createRaw.RawTransaction,
                    Signature = signature
                });
            Console.WriteLine(rawTransactionResult);
        }

        [TestMethod]
        public void CreateTestAccount()
        {
            var keyPath =
                "/Users/ericshu/GitHub/Team/aelf-automation-test/test/AElf.Automation.ScenariosExecution/testers";
            var keyStore = AElfKeyStore.GetKeyStore(keyPath);
            var accManager = new AccountManager(keyStore);
            for (var i = 0; i < 100; i++)
            {
                var acc = accManager.NewAccount();
                accManager.UnlockAccount(acc);
            }
        }

        [SkippableFact]
        public void TestIgnoreTest()
        {
            Skip.If(true, "local environment");
        }

        [TestMethod]
        public async Task SaveTokenContractFile()
        {
            var nodeManager = new NodeManager("192.168.197.40:8000");
            NodeInfoHelper.SetConfig("nodes-env1-main.json");
            var contractManager = new ContractManager(nodeManager, nodeManager.GetRandomAccount());
            var tokenAddress = contractManager.Token.Contract;
            var contractResult =
                await contractManager.GenesisStub.GetSmartContractRegistrationByAddress.CallAsync(tokenAddress);
            var binaryWriter = new BinaryWriter(new FileStream("TokenOrg.dll", FileMode.OpenOrCreate));
            binaryWriter.Write(contractResult.Code.ToByteArray());
        }

        [TestMethod]
        public void GetHashCodeTest()
        {
            //byte string
            var byteString = ByteString.CopyFromUtf8("test info");
            Logger.Info($"ByteString => {byteString.GetHashCode()}");
            //string value
            var message = "proto buf string test info";
            var stringInfo = new StringValue
            {
                Value = message
            };
            Logger.Info($"StringValue => {stringInfo.GetHashCode()}/{message.GetHashCode()}");

            //int3 value
            var value = int.MaxValue;
            var int32Info = new Int32Value
            {
                Value = value
            };
            Logger.Info($"Int32Value => {int32Info.GetHashCode()}/{value.GetHashCode()}");

            //int64 value
            var data = long.MaxValue;
            var int64Info = new Int64Value
            {
                Value = data
            };
            Logger.Info($"Int64Value => {int64Info.GetHashCode()}/{data.GetHashCode()}");

            //enum
            var enumData = Color.Blue;
            var enumInfo = new EnumInput
            {
                Info = enumData
            };
            Logger.Info($"EnumInput => {enumInfo.GetHashCode()}/{enumData.GetHashCode()}");

            //map
            var map1 = new MapStringInput
            {
                Info =
                {
                    {"key1", "test1"},
                    {"key2", "test1"},
                    {"key3", "test1"}
                }
            };
            var map2 = new MapStringInput
            {
                Info =
                {
                    {"key1", "test1"},
                    {"key2", "test2"}
                }
            };
            Logger.Info($"MapStringInput => {map1.GetHashCode()}/{map2.GetHashCode()}");
        }

        [TestMethod]
        public void UpdateCodeHash_Test()
        {
            const string tokenPath = "/Users/ericshu/.local/share/aelf/contracts/AElf.Contracts.MultiToken.dll.patched";
            var contractFileCode = File.ReadAllBytes(tokenPath);
            var newCode = CodeInjectHelper.ChangeContractCodeHash(contractFileCode);
            var binaryWriter = new BinaryWriter(new FileStream("TokenCodeChange.dll", FileMode.OpenOrCreate));
            binaryWriter.Write(newCode);
        }

        [TestMethod]
        [DataRow(50)]
        public async Task GetTransactionLimit(int limit)
        {
            NodeInfoHelper.SetConfig("nodes-env205-main");
            var node = NodeInfoHelper.Config.Nodes.First();
            var nodeManager = new NodeManager(node.Endpoint);
            var contractManager = new ContractManager(nodeManager, node.Account);
            var configurationContract = contractManager.Genesis.GetConfigurationContract();
            var configurationStub = contractManager.Genesis.GetConfigurationStub();

            var genesisOwner = contractManager.Authority.GetGenesisOwnerAddress();
            var miners = contractManager.Authority.GetCurrentMiners();
            var input = new SetConfigurationInput
            {
                Key = nameof(ConfigurationNameProvider.BlockTransactionLimit),
                Value = new SInt32Value {Value = limit}.ToByteString()
            };
            var transactionResult = contractManager.Authority.ExecuteTransactionWithAuthority(
                configurationContract.ContractAddress,
                nameof(ConfigurationMethod.SetConfiguration), input,
                genesisOwner, miners, configurationContract.CallAddress);
            transactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var limitResult =
                await configurationStub.GetConfiguration.CallAsync(new StringValue
                    {Value = nameof(ConfigurationNameProvider.BlockTransactionLimit)});
            var value = SInt32Value.Parser.ParseFrom(limitResult.Value).Value;
            Logger.Info($"Block transaction limit: {value}");
        }
    }
}