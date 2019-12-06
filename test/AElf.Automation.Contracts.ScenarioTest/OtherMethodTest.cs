using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AElfChain.Common.Contracts;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using AElfChain.Common.Utils;
using AElf.Contracts.Election;
using AElf.Contracts.MultiToken;
using AElf.Kernel;
using AElf.Sdk.CSharp;
using AElf.Types;
using AElfChain.Common;
using AElfChain.Common.ContractSerializer;
using AElfChain.SDK.Models;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;

namespace AElf.Automation.Contracts.ScenarioTest
{
    [TestClass]
    public class OtherMethodTest
    {
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
            var address = AddressUtils.Generate();
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
                EndTimestamp = TimestampHelper.GetUtcNow().AddDays(120)
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
        public async Task GetDeployedContracts()
        {
            const string endpoint = "18.162.41.20:8000";
            var nodeManager = new NodeManager(endpoint);

            var genesis = GenesisContract.GetGenesisContract(nodeManager);
            var genesisStub = genesis.GetGensisStub();

            var contractList = await genesisStub.GetDeployedContractAddressList.CallAsync(new Empty());
            Console.WriteLine($"Total deployed contracts: {contractList.Value.Count}");
            Console.WriteLine(contractList.Value);
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
                IsTransferDisabled = false,
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
            var handler = new ContractHandler();
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
            var byteInfo = nodeManager.ApiService.GetContractFileDescriptorSetAsync(token.ContractAddress).Result;
            var customContractHandler = new CustomContractHandler(byteInfo);
            customContractHandler.GetAllMethodsInfo(true);
            customContractHandler.GetParameters("Create");
        }

        [TestMethod]
        public async Task SendRawTransaction()
        {
            var nodeManager = new NodeManager("192.168.197.43:8100");
            var genesis = nodeManager.GetGenesisContract();
            var token = genesis.GetTokenContract();
            var height = await nodeManager.ApiService.GetBlockHeightAsync();
            var block = await nodeManager.ApiService.GetBlockByHeightAsync(height);
            var createRaw = await nodeManager.ApiService.CreateRawTransactionAsync(new CreateRawTransactionInput
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
                await nodeManager.ApiService.ExecuteRawTransactionAsync(new ExecuteRawTransactionDto
                {
                    RawTransaction = createRaw.RawTransaction,
                    Signature = signature
                });
            Console.WriteLine(rawTransactionResult);
        }

        [TestMethod]
        public void CreateTestAccount()
        {
            var keyPath = "/Users/ericshu/GitHub/Team/aelf-automation-test/test/AElf.Automation.ScenariosExecution/testers";
            var keyStore = AElfKeyStore.GetKeyStore(keyPath);
            var accManager = new AccountManager(keyStore);
            for (var i = 0; i < 100; i++)
            {
                var acc = accManager.NewAccount();
                accManager.UnlockAccount(acc);
            }
        }
    }
}