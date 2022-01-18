using System;
using System.Collections.Generic;
using System.Linq;
using AElf.Contracts.MultiToken;
using AElf.Contracts.Oracle;
using AElf.Contracts.Price;
using AElf.Contracts.TestsOracle;
using AElf.CSharp.Core;
using AElf.Types;
using AElfChain.Common;
using AElfChain.Common.Contracts;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using log4net;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using InitializeInput = AElf.Contracts.Price.InitializeInput;
using TokenPrice = AElf.Contracts.TestsOracle.TokenPrice;

namespace AElf.Automation.Contracts.ScenarioTest
{
    [TestClass]
    public class PriceContractTest
    {
        private ILog Logger { get; set; }
        private INodeManager NodeManager { get; set; }
        private AuthorityManager AuthorityManager { get; set; }

        private GenesisContract _genesisContract;
        private TokenContract _tokenContract;

        private PriceContract _priceContract;
        private OracleTestContract _oracleTest;

        private Address _oracle;
        private Address _integerAggregator;


        private string _priceAddress = "225ajURvev5rgX8HnMJ8GjbPnRxUrCHoD7HUjhWQqewEJ5GAv1";
        private string _oracleAddress = "2nyC8hqq3pGnRu8gJzCsTaxXB6snfGxmL2viimKXgEfYWGtjEh";
        private string _oracleTestAddress = "2M24EKAecggCnttZ9DUUMCXi4xC67rozA87kFgid9qEwRUMHTs";
        private string _integerAggregatorAddress = "2hqsqJndRAZGzk96fsEvyuVBTAvoBjcuwTjkuyJffBPueJFrLa";

        // private string _priceAddress = "GwsSp1MZPmkMvXdbfSCDydHhZtDpvqkFpmPvStYho288fb7QZ";
        // private string _oracleAddress = "2nyC8hqq3pGnRu8gJzCsTaxXB6snfGxmL2viimKXgEfYWGtjEh";
        // private string _integerAggregatorAddress = "2hqsqJndRAZGzk96fsEvyuVBTAvoBjcuwTjkuyJffBPueJFrLa";

        private string InitAccount { get; } = "nn659b9X1BLhnu5RWmEUbuuV7J9QKVVSN54j9UmeCbF3Dve5D";
        private string AuthorizedUser { get; } = "YUW9zH5GhRboT5JK4vXp5BLAfCDv28rRmTQwo418FuaJmkSg8";
        private string Regiment { get; } = "vcn2ypT9tqPhAb4SYurqkJtQeYfRemJZocDf6ryZft6J7zh6E";
        private static string RpcUrl { get; } = "192.168.67.166:8000";
        private const bool IsTest = true;

        private readonly List<string> _associationMember = new List<string>
        {
            "2a6MGBRVLPsy6pu4SVMWdQqHS5wvmkZv8oas9srGWHJk7GSJPV",
            "2aEZfnBtAgdSpAfYxkcbFSsEaeZSfegAY6UpPs4aCfWphSoPVg",
            "2bs2uYMECtHWjB57RqgqQ3X2LrxgptWHtzCqGEU11y45aWimh4",
            "2HnvUWNzKG6DbRhtrDgSwEfqA2YeHEhmLLnTAXKwzMBbJxEhUr",
            "2my3t9d45ytqrLii43p52CPmXYD2fGCzEJdZpuaRrASkYNVoT"
        };

        [TestInitialize]
        public void InitializeTest()
        {
            Log4NetHelper.LogInit("PriceContractTest");
            Logger = Log4NetHelper.GetLogger();
            NodeInfoHelper.SetConfig("nodes-new-env-main");

            NodeManager = new NodeManager(RpcUrl);
            AuthorityManager = new AuthorityManager(NodeManager, InitAccount);
            Logger.Info(RpcUrl);
            _genesisContract = GenesisContract.GetGenesisContract(NodeManager, InitAccount);
            _tokenContract = _genesisContract.GetTokenContract(InitAccount);

            _priceContract = _priceAddress == ""
                ? new PriceContract(NodeManager, InitAccount)
                : new PriceContract(NodeManager, _priceAddress, InitAccount);
            _oracle = _oracleAddress == ""
                ? AuthorityManager.DeployContractWithAuthority(InitAccount, "AElf.Contracts.Oracle")
                : Address.FromBase58(_oracleAddress);
            _integerAggregator = _integerAggregatorAddress == ""
                ? AuthorityManager.DeployContractWithAuthority(InitAccount, "AElf.Contracts.IntegerAggregator")
                : Address.FromBase58(_integerAggregatorAddress);
            _oracleTest = _oracleTestAddress == ""
                ? new OracleTestContract(NodeManager, InitAccount)
                : new OracleTestContract(NodeManager, _oracleTestAddress, InitAccount);
            CreateToken();
        }

        [TestMethod]
        public void InitializeContract()
        {
            var oracleAddress = IsTest ? _oracleTest.Contract : _oracle;
            var input = new InitializeInput
            {
                AuthorizedUsers = {AuthorizedUser.ConvertAddress(), InitAccount.ConvertAddress()},
                OracleAddress = oracleAddress,
                TracePathLimit = 2,
                Controller = InitAccount.ConvertAddress()
            };
            var result = _priceContract.ExecuteMethodWithResult(PriceMethod.Initialize, input);
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            var authorizeUsers = _priceContract.GetAuthorizedSwapTokenPriceQueryUsers();
            authorizeUsers.List.ShouldContain(AuthorizedUser.ConvertAddress());
            authorizeUsers.List.ShouldContain(InitAccount.ConvertAddress());
            authorizeUsers.List.Count.ShouldBe(2);
            var tracePath = _priceContract.GetTracePathLimit();
            tracePath.PathLimit.ShouldBe(2);
        }

        [TestMethod]
        public void InitializeContract_ErrorTest()
        {
            var newContract = new PriceContract(NodeManager, InitAccount);
            var controller = InitAccount.ConvertAddress();
            var input = new InitializeInput
            {
                AuthorizedUsers = { },
                OracleAddress = _oracle,
                TracePathLimit = 4,
                Controller = controller
            };
            {
                var errorResult = newContract.ExecuteMethodWithResult(PriceMethod.Initialize, input);
                errorResult.Status.ConvertTransactionResultStatus()
                    .ShouldBe(TransactionResultStatus.NodeValidationFailed);
                errorResult.Error.ShouldContain("TracePathLimit should less than 3");
            }
            {
                input.TracePathLimit = 0;
                var result = newContract.ExecuteMethodWithResult(PriceMethod.Initialize, input);
                result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
                var authorizeUsers = newContract.GetAuthorizedSwapTokenPriceQueryUsers();
                authorizeUsers.List.ShouldContain(controller);
                authorizeUsers.List.Count.ShouldBe(1);
                var tracePath = newContract.GetTracePathLimit();
                tracePath.PathLimit.ShouldBe(2);
            }
            {
                input.TracePathLimit = 1;
                var errorResult = newContract.ExecuteMethodWithResult(PriceMethod.Initialize, input);
                errorResult.Status.ConvertTransactionResultStatus()
                    .ShouldBe(TransactionResultStatus.NodeValidationFailed);
                errorResult.Error.ShouldContain("Already initialized.");
            }
        }

        [TestMethod]
        public void QuerySwapTokenPrice()
        {
            _tokenContract.IssueBalance(InitAccount, InitAccount, 10000000000, "PORT");
            _tokenContract.ApproveToken(InitAccount, _priceContract.ContractAddress, 100000000000000, "PORT");
            // _tokenContract.IssueBalance(InitAccount,_priceContract.ContractAddress, 10000000000, "PORT");

            var result =
                _priceContract.ExecuteMethodWithResult(PriceMethod.QuerySwapTokenPrice, new QueryTokenPriceInput
                {
                    AggregateThreshold = 1,
                    AggregatorContractAddress = _integerAggregator,
                    DesignatedNodes = {Regiment.ConvertAddress()},
                    TokenSymbol = "ELF",
                    TargetTokenSymbol = "ETH"
                });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            //Test Oracle Contract doesn't have event.
            if (!IsTest)
            {
                var byteString = result.Logs.First(l => l.Name.Contains(nameof(QueryCreated))).NonIndexed;
                var query = QueryCreated.Parser.ParseFrom(ByteString.FromBase64(byteString));
                query.QueryInfo.Title.ShouldBe("TokenSwapPrice");
                Logger.Info(query.QueryId.ToHex());
                Logger.Info(query.QueryInfo.Title);
                var returnValue = result.ReturnValue;
                var queryId = Hash.Parser.ParseFrom(ByteArrayHelper.HexStringToByteArray(returnValue));
                Logger.Info(queryId.ToHex());
            }

            // var check = _priceContract.CheckQueryIdIfExisted(queryId);
            // check.Value.ShouldBeTrue();
        }

        [TestMethod]
        public void QueryExchangeTokenPrice()
        {
            _tokenContract.IssueBalance(InitAccount, InitAccount, 10000000000, "PORT");
            _tokenContract.ApproveToken(InitAccount, _priceContract.ContractAddress, 100000000000000, "PORT");
            _tokenContract.IssueBalance(InitAccount, _priceContract.ContractAddress, 10000000000, "PORT");

            var result =
                _priceContract.ExecuteMethodWithResult(PriceMethod.QueryExchangeTokenPrice, new QueryTokenPriceInput
                {
                    AggregateThreshold = 1,
                    AggregatorContractAddress = _integerAggregator,
                    DesignatedNodes = {Regiment.ConvertAddress()},
                    TokenSymbol = "ELF",
                    TargetTokenSymbol = "ETH"
                });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            //Test Oracle Contract doesn't have event.
            var byteString = result.Logs.First(l => l.Name.Contains(nameof(QueryCreated))).NonIndexed;
            var query = QueryCreated.Parser.ParseFrom(ByteString.FromBase64(byteString));
            query.QueryInfo.Title.ShouldBe("ExchangeTokenPrice");
            Logger.Info(query.QueryId.ToHex());
            Logger.Info(query.QueryInfo.Title);
            var returnValue = result.ReturnValue;
            var queryId = Hash.Parser.ParseFrom(ByteArrayHelper.HexStringToByteArray(returnValue));
            Logger.Info(queryId.ToHex());

            // var check = _priceContract.CheckQueryIdIfExisted(queryId);
            // check.Value.ShouldBeTrue();
        }

        [TestMethod]
        public void QuerySwapTokenPrice_Error()
        {
            _priceContract.SetAccount(_associationMember.First());
            var result =
                _priceContract.ExecuteMethodWithResult(PriceMethod.QuerySwapTokenPrice, new QueryTokenPriceInput
                {
                    AggregateThreshold = 1,
                    AggregatorContractAddress = _integerAggregator,
                    DesignatedNodes = {Regiment.ConvertAddress()},
                    TokenSymbol = "ELF",
                    TargetTokenSymbol = "ETH"
                });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
            result.Error.ShouldContain("UnAuthorized sender");
        }

        [TestMethod]
        public void RecordSwapTokenPrice()
        {
            var tokenSymbol = "YYY";
            var targetTokenSymbol = "USDT";
            var price = "0.1";
            var queryId = "34fbf385e277c8b0a41852c0f2d19d9671d9b050cd5a213259b92a6bd64875e6";
            var now = DateTime.UtcNow;
            var timestamp = Timestamp.FromDateTime(now.AddMinutes(10));

            var result = _oracleTest.RecordTokenPrice(_priceContract.Contract, queryId, targetTokenSymbol, tokenSymbol,
                price, timestamp, Regiment.ConvertAddress());
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var logs = NewestSwapPriceUpdated.Parser.ParseFrom(
                ByteString.FromBase64(result.Logs.First(l => l.Name.Equals("NewestSwapPriceUpdated")).NonIndexed));
            logs.Price.ShouldBe(price);
            logs.Timestamp.ShouldBe(timestamp);

            var check = _priceContract.GetSwapTokenPriceInfo(targetTokenSymbol, tokenSymbol);
            check.Timestamp.ShouldBe(timestamp);
            check.Value.ShouldBe(price);
        }

        //ELF-ETH, ABC-ETH ==> ELF-ABC
        //if ELF, ETH, ABC, one of them, no pair of USDT, the price is '0'
        [TestMethod]
        public void RecordSwapTokenPrice_Path2Check()
        {
            var tokenSymbol1 = "AAA";
            var targetTokenSymbol1 = "BBB";
            var price1 = "0.000001";
            var queryId = "34fbf385e277c8b0a41852c0f2d19d9671d9b050cd5a213259b92a6bd64875e6";
            var now = DateTime.UtcNow;
            var timestamp = Timestamp.FromDateTime(now.AddMinutes(10));

            var result = _oracleTest.RecordTokenPrice(_priceContract.Contract, queryId, targetTokenSymbol1,
                tokenSymbol1,
                price1, timestamp, Regiment.ConvertAddress());
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            var check = _priceContract.GetSwapTokenPriceInfo(targetTokenSymbol1, tokenSymbol1);
            check.Timestamp.ShouldBe(timestamp);
            check.Value.ShouldBe(price1);

            var tokenSymbol2 = "CCC";
            var targetTokenSymbol2 = "BBB";
            var price2 = "0.000002";

            var secondResult = _oracleTest.RecordTokenPrice(_priceContract.Contract, queryId, targetTokenSymbol2,
                tokenSymbol2,
                price2, timestamp, Regiment.ConvertAddress());
            secondResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            var check2 = _priceContract.GetSwapTokenPriceInfo(targetTokenSymbol2, tokenSymbol2);
            check2.Timestamp.ShouldBe(timestamp);
            check2.Value.ShouldBe(price2);
            
            var check3 =  _priceContract.GetSwapTokenPriceInfo(tokenSymbol1, tokenSymbol2);
            check3.Timestamp.ShouldBeNull();
            Logger.Info(check3.Value);
        }
        
        //ELF-USDT ETH-ELF ABC-ETH ==> ABC-USDT == 0
        [TestMethod]
        public void RecordSwapTokenPrice_Path3Check()
        {
            var tokenSymbol1 = "ELF";
            var targetTokenSymbol1 = "USDT";
            var price1 = "0.4";
            var queryId = "34fbf385e277c8b0a41852c0f2d19d9671d9b050cd5a213259b92a6bd64875e6";
            var now = DateTime.UtcNow;
            var timestamp = Timestamp.FromDateTime(now.AddMinutes(10));

            var result = _oracleTest.RecordTokenPrice(_priceContract.Contract, queryId, targetTokenSymbol1,
                tokenSymbol1,
                price1, timestamp, Regiment.ConvertAddress());
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            var check = _priceContract.GetSwapTokenPriceInfo(targetTokenSymbol1, tokenSymbol1);
            check.Timestamp.ShouldBe(timestamp);
            check.Value.ShouldBe(price1);

            var tokenSymbol2 = "ETH";
            var targetTokenSymbol2 = "ELF";
            var price2 = "1000";

            var secondResult = _oracleTest.RecordTokenPrice(_priceContract.Contract, queryId, targetTokenSymbol2,
                tokenSymbol2,
                price2, timestamp, Regiment.ConvertAddress());
            secondResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            var check2 = _priceContract.GetSwapTokenPriceInfo(targetTokenSymbol2, tokenSymbol2);
            check2.Timestamp.ShouldBe(timestamp);
            check2.Value.ShouldBe(price2);
            
            var tokenSymbol3 = "ABC";
            var targetTokenSymbol3 = "ETH";
            var price3 = "0.0001";

            var result2 = _oracleTest.RecordTokenPrice(_priceContract.Contract, queryId, targetTokenSymbol3,
                tokenSymbol3,
                price3, timestamp, Regiment.ConvertAddress());
            result2.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            
            var check3 =  _priceContract.GetSwapTokenPriceInfo(targetTokenSymbol3, tokenSymbol3);
            check3.Timestamp.ShouldBe(timestamp);
            check3.Value.ShouldBe(price3);        
            
            //check ABC-USDT
            var check4 =  _priceContract.GetSwapTokenPriceInfo(targetTokenSymbol1, tokenSymbol3);
            check4.Timestamp.ShouldBeNull();
            check4.Value.ShouldBe("0.0000");        
        }
        
        //JJJ-ETH ETH-USDT JJJ-USDT
        [TestMethod]
        public void RecordSwapTokenPrice_SamePath()
        {
            var tokenSymbol1 = "JJJ";
            var targetTokenSymbol1 = "ETH";
            var stableSymbol = "USDT";
            var price1 = "0.4";
            var queryId = "34fbf385e277c8b0a41852c0f2d19d9671d9b050cd5a213259b92a6bd64875e6";
            var now = DateTime.UtcNow;
            var timestamp = Timestamp.FromDateTime(now.AddMinutes(10));
            
            var check = _priceContract.GetSwapTokenPriceInfo(stableSymbol, targetTokenSymbol1);
            Logger.Info(check.Value);

            var result = _oracleTest.RecordTokenPrice(_priceContract.Contract, queryId, targetTokenSymbol1,
                tokenSymbol1,
                price1, timestamp, Regiment.ConvertAddress());
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            
            var check2 = _priceContract.GetSwapTokenPriceInfo(targetTokenSymbol1, tokenSymbol1);
            check2.Timestamp.ShouldBe(timestamp);
            check2.Value.ShouldBe(price1);
            var check3 = _priceContract.GetSwapTokenPriceInfo(stableSymbol, tokenSymbol1);
            if (check.Timestamp == null)
            {
                check3.Value.ShouldBe("0.0");
            }
            else
            {
                check3.Value.ShouldBe(long.Parse(check.Value).Mul(long.Parse(check2.Value)).ToString());
            }
        }

        [TestMethod]
        public void RecordSwapTokenPrice_AddNewPair()
        {
            var tokenSymbol = "ABC";
            var targetTokenSymbol = "ELF";
            var info = _priceContract.GetSwapTokenPriceInfo(tokenSymbol, targetTokenSymbol);
            info.Value.ShouldBe("0");
            var price = "0.000001";
            var queryId = "34fbf385e277c8b0a41852c0f2d19d9671d9b050cd5a213259b92a6bd64875e6";
            var now = DateTime.UtcNow;
            var timestamp = Timestamp.FromDateTime(now.AddMinutes(10));

            var result = _oracleTest.RecordTokenPrice(_priceContract.Contract, queryId, targetTokenSymbol, tokenSymbol,
                price, timestamp, Regiment.ConvertAddress());
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            var check = _priceContract.GetSwapTokenPriceInfo(targetTokenSymbol, tokenSymbol);
            check.Timestamp.ShouldBe(timestamp);
            check.Value.ShouldBe(price);
        }

        [TestMethod]
        public void RecordSwapTokenPrice_SamePair()
        {
            var tokenSymbol = "ELF";
            var targetTokenSymbol = "USDT";
            var price = "0.4";
            var queryId = "34fbf385e277c8b0a41852c0f2d19d9671d9b050cd5a213259b92a6bd64875e6";
            var now = DateTime.UtcNow;
            var timestamp = Timestamp.FromDateTime(now.AddMinutes(10));

            var result = _oracleTest.RecordTokenPrice(_priceContract.Contract, queryId, targetTokenSymbol, tokenSymbol,
                price, timestamp, Regiment.ConvertAddress());
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var logs = NewestSwapPriceUpdated.Parser.ParseFrom(
                ByteString.FromBase64(result.Logs.First(l => l.Name.Equals("NewestSwapPriceUpdated")).NonIndexed));
            logs.Price.ShouldBe(price);
            logs.Timestamp.ShouldBe(timestamp);

            var check = _priceContract.GetSwapTokenPriceInfo(targetTokenSymbol, tokenSymbol);
            check.Timestamp.ShouldBe(timestamp);
            check.Value.ShouldBe(price);

            timestamp = Timestamp.FromDateTime(now.AddMinutes(11));
            price = "200000";
            var twiceResult = _oracleTest.RecordTokenPrice(_priceContract.Contract, queryId, tokenSymbol,
                targetTokenSymbol,
                price, timestamp, Regiment.ConvertAddress());
            twiceResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            check = _priceContract.GetSwapTokenPriceInfo(targetTokenSymbol, tokenSymbol);
            check.Timestamp.ShouldBe(timestamp);
            var actualPrice = decimal.Round(1 / decimal.Parse(price), 8).ToString();
            check.Value.ShouldBe(actualPrice);
            Logger.Info(check.Value);
        }

        [TestMethod]
        public void RecordSwapTokenPrice_Error()
        {
            var tokenSymbol = "ELF";
            var targetTokenSymbol = "ETH";
            var price = "0.000001";
            var queryId = "34fbf385e277c8b0a41852c0f2d19d9671d9b050cd5a213259b92a6bd64875e6";
            var now = DateTime.UtcNow;
            var timestamp = Timestamp.FromDateTime(now.AddMinutes(10));
            var input = new TokenPriceInfo
            {
                CallBackAddress = _priceContract.Contract,
                CallBackMethodName = "RecordSwapTokenPrice",
                QueryId = Hash.LoadFromHex(queryId),
                TokenPrice = new TokenPrice
                {
                    Timestamp = timestamp,
                    TargetTokenSymbol = targetTokenSymbol,
                    TokenSymbol = tokenSymbol,
                    Price = price
                },
                OracleNodes = {Regiment.ConvertAddress()}
            };
            var result = _oracleTest.ExecuteMethodWithResult(OracleTestMethod.RecordTokenPrice, input);
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            // Expired data
            {
                input.TokenPrice.Timestamp = Timestamp.FromDateTime(now.AddMinutes(-20));
                var expiredResult = _oracleTest.ExecuteMethodWithResult(OracleTestMethod.RecordTokenPrice, input);
                expiredResult.Status.ConvertTransactionResultStatus()
                    .ShouldBe(TransactionResultStatus.NodeValidationFailed);
                expiredResult.Error.ShouldContain("Expired data for pair");
            }
            {
                input.TokenPrice.Timestamp = Timestamp.FromDateTime(now.AddMinutes(10));
                input.TokenPrice.TokenSymbol = "ETH";
                var sameResult = _oracleTest.ExecuteMethodWithResult(OracleTestMethod.RecordTokenPrice, input);
                sameResult.Status.ConvertTransactionResultStatus()
                    .ShouldBe(TransactionResultStatus.NodeValidationFailed);
                sameResult.Error.ShouldContain(" are same");
            }
        }

        [TestMethod]
        public void UpdateSwapTokenTraceInfo()
        {
            var targetToken = "MMM";
            var token = "ABC";
            var price = _priceContract.GetSwapTokenPriceInfo(targetToken, token);
            price.Timestamp.ShouldBeNull();

            {
                var result = _priceContract.ExecuteMethodWithResult(PriceMethod.UpdateSwapTokenTraceInfo, new UpdateSwapTokenTraceInfoInput
                {
                    TokenSymbol = token,
                    TargetTokenSymbol = targetToken
                });
                result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
                result.Error.ShouldContain("does not exist");
            }
            //QWE-ETH QWE-ASD ASD-ZXC QWE-USDT ASD-USDT 
            //只能修改最短路径的Token QWE-USDT已经存在，所以不能修改QWE-ASD
            {
                targetToken = "ASD";
                token = "QWE";
                var result = _priceContract.ExecuteMethodWithResult(PriceMethod.UpdateSwapTokenTraceInfo, new UpdateSwapTokenTraceInfoInput
                {
                    TokenSymbol = token,
                    TargetTokenSymbol = targetToken
                });
                result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
                result.Error.ShouldContain("Invalid path set for pair "); 
            }
            // RRR-TTT-USDT ==> RRR-YYY-USDT
            {
                targetToken = "YYY";
                token = "RRR";
                price = _priceContract.GetSwapTokenPriceInfo("USDT", token);
                price.Timestamp.ShouldBeNull();
                Logger.Info(price.Value);
                
                var result = _priceContract.ExecuteMethodWithResult(PriceMethod.UpdateSwapTokenTraceInfo, new UpdateSwapTokenTraceInfoInput
                {
                    TokenSymbol = token,
                    TargetTokenSymbol = targetToken
                });
                result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
                price = _priceContract.GetSwapTokenPriceInfo("USDT", token);
                price.Timestamp.ShouldBeNull();
                Logger.Info(price.Value);
            }
        }

        [TestMethod]
        public void RecordExchangeTokenPrice()
        {
            var tokenSymbol = "EXC";
            var targetTokenSymbol = "USDT";
            var price = "0.1";
            var queryId = "34fbf385e277c8b0a41852c0f2d19d9671d9b050cd5a213259b92a6bd64875e6";
            var now = DateTime.UtcNow;
            var timestamp = Timestamp.FromDateTime(now.AddMinutes(10));
            var result = _oracleTest.RecordTokenPriceExchange(_priceContract.Contract, queryId, targetTokenSymbol, tokenSymbol,
                price, timestamp, Regiment.ConvertAddress());
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var logs = NewestExchangePriceUpdated.Parser.ParseFrom(
                ByteString.FromBase64(result.Logs.First(l => l.Name.Equals("NewestExchangePriceUpdated")).NonIndexed));
            logs.Price.ShouldBe(price);
            logs.Timestamp.ShouldBe(timestamp);
            logs.PriceSupplier.NodeList.ShouldContain(Regiment.ConvertAddress());

            var check = _priceContract.GetExchangeTokenPriceInfo(targetTokenSymbol,tokenSymbol, Regiment.ConvertAddress());
            check.Value.ShouldBe(price);
        }

        [TestMethod]
        public void CheckPrice()
        {
            var targetToken = "USDT";
            var token = "QWE";
            var price = _priceContract.GetSwapTokenPriceInfo(targetToken, token);
            Logger.Info(price.Value);
        }

        [TestMethod]
        public void UpdateAuthorizedSwapTokenPriceQueryUsers()
        {
            var newAuthor = _associationMember.First();
            var getAuthor = _priceContract.GetAuthorizedSwapTokenPriceQueryUsers();
            getAuthor.List.ShouldNotContain(newAuthor.ConvertAddress());
            var addAuthor = _priceContract.ExecuteMethodWithResult(PriceMethod.UpdateAuthorizedSwapTokenPriceQueryUsers,
                new AuthorizedSwapTokenPriceQueryUsers
                {
                    List =
                    {
                        newAuthor.ConvertAddress(), InitAccount.ConvertAddress(),
                        AuthorizedUser.ConvertAddress()
                    }
                });
            addAuthor.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            getAuthor = _priceContract.GetAuthorizedSwapTokenPriceQueryUsers();
            getAuthor.List.ShouldContain(newAuthor.ConvertAddress());
        }

        [TestMethod]
        public void ChangeTracePathLimit()
        {
            var pathLimit = _priceContract.GetTracePathLimit();
            var newPath = pathLimit.PathLimit - 1 > 0 ? pathLimit.PathLimit - 1 : pathLimit.PathLimit + 1;
            var changePath = _priceContract.ExecuteMethodWithResult(PriceMethod.ChangeTracePathLimit,
                new ChangeTracePathLimitInput
                {
                    NewPathLimit = newPath
                });
            changePath.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            pathLimit = _priceContract.GetTracePathLimit();
            pathLimit.PathLimit.ShouldBe(newPath);
        }

        [TestMethod]
        public void AuthorizedSenderCheck()
        {
            _priceContract.SetAccount(AuthorizedUser);
            {
                var newAuthor = _associationMember.First();
                var result = _priceContract.ExecuteMethodWithResult(PriceMethod.UpdateAuthorizedSwapTokenPriceQueryUsers,
                    new AuthorizedSwapTokenPriceQueryUsers
                    {
                        List =
                        {
                            newAuthor.ConvertAddress(), InitAccount.ConvertAddress(),
                            AuthorizedUser.ConvertAddress()
                        }
                    });
                result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
                result.Error.ShouldContain("Invalid sender");
            }
            {
                var result = _priceContract.ExecuteMethodWithResult(PriceMethod.ChangeOracle,
                    new ChangeOracleInput
                    {
                        Oracle = _oracleTest.Contract
                    });
                result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
                result.Error.ShouldContain("Invalid sender");
            }
            {
                var result = _priceContract.ExecuteMethodWithResult(PriceMethod.ChangeTracePathLimit,
                    new ChangeTracePathLimitInput
                    {
                        NewPathLimit = 1
                    });
                result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
                result.Error.ShouldContain("Invalid sender");
            }
            {
                var result = _priceContract.ExecuteMethodWithResult(PriceMethod.UpdateSwapTokenTraceInfo,
                    new UpdateSwapTokenTraceInfoInput()
                    {
                        TokenSymbol = "ELF",
                        TargetTokenSymbol = "ABC"
                    });
                result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
                result.Error.ShouldContain("Invalid sender");
            }
        }

        private void CreateToken()
        {
            var tokenInfo = _tokenContract.GetTokenInfo("PORT");
            if (!tokenInfo.Equals(new TokenInfo()))
                return;
            var result = _tokenContract.ExecuteMethodWithResult(TokenMethod.Create, new CreateInput
            {
                Decimals = 8,
                IsBurnable = true,
                TokenName = "PORT token",
                TotalSupply = 10000000000000000,
                Symbol = "PORT",
                Issuer = InitAccount.ConvertAddress()
            });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
        }
    }
}