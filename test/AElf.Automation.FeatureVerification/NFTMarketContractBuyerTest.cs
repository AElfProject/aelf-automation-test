using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AElf.Contracts.MultiToken;
using AElf.Contracts.NFT;
using AElf.Contracts.NFTMarket;
using AElf.Types;
using AElfChain.Common;
using AElfChain.Common.Contracts;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using Google.Protobuf.WellKnownTypes;
using log4net;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using CreateInput = AElf.Contracts.NFT.CreateInput;
using StringList = AElf.Contracts.NFTMarket.StringList;

namespace AElf.Automation.Contracts.ScenarioTest
{
    [TestClass]
    public class NFTMarketContractBuyerTest
    {
        private ILog Logger { get; set; }
        private INodeManager NodeManager { get; set; }
        private int _chainId;
        private GenesisContract _genesisContract;
        private TokenContract _tokenContract;
        private NftContract _nftContract;
        private NFTMarketContract _nftMarketContract;

        private string InitAccount{ get; } = "J6zgLjGwd1bxTBpULLXrGVeV74tnS2n74FFJJz7KNdjTYkDF6";

        // private string InitAccount { get; } = "nn659b9X1BLhnu5RWmEUbuuV7J9QKVVSN54j9UmeCbF3Dve5D";
        private string OtherAccount { get; } = "sjzNpr5bku3ZyvMqQrXeBkXGEvG2CTLA2cuNDfcDMaPTTAqEy";
        private string OtherAccount1 { get; } = "2JatA85K3mxZPs3LtFufynoUa5Wvo9QNhnofSGYisuM9P8F3Xc";
        private string OtherAccount2 { get; } = "2NKnGrarMPTXFNMRDiYH4hqfSoZw72NLxZHzgHD1Q3xmNoqdmR";
        private string OtherAccount3 { get; } = "2oKcAgFCi2FxwyQFzCVnmNYdKZzJLyA983gEwUmyuuaVUX2d1P";

        private string WhiteListAddress1 { get; } = "FHdcx45K5kovWsAKSb3rrdyNPFus8eoJ1XTQE7aXFHTgfpgzN";
        private string WhiteListAddress2 { get; } = "2bs2uYMECtHWjB57RqgqQ3X2LrxgptWHtzCqGEU11y45aWimh4";
        private string WhiteListAddress3 { get; } = "YUW9zH5GhRboT5JK4vXp5BLAfCDv28rRmTQwo418FuaJmkSg8";
        
        private string serviceAddress { get; } = "2ZYyxEH6j8zAyJjef6Spa99Jx2zf5GbFktyAQEBPWLCvuSAn8D";

        private static string RpcUrl { get; } = "192.168.66.9:8000";
        private AuthorityManager AuthorityManager { get; set; }

        private string NFT = "";
        private string NFTMarket = "";
        
        //private string NFT = "2PgMbURqeBenypU4kEcW7zckyCZXCt6qmMKfA8WRkddPVYeXb1";
        //private string NFTMarket = "2KrfXJ8WccjCr5KN5fdrm68Z1Ssq9bZLUfAsQkzafwriRg7yzh";

        
        [TestInitialize]
        public void Initialize()
        {
            Log4NetHelper.LogInit("NFTMarketContractTest");
            Logger = Log4NetHelper.GetLogger();
            NodeInfoHelper.SetConfig("nodes-env2-main");

            NodeManager = new NodeManager(RpcUrl);
            AuthorityManager = new AuthorityManager(NodeManager, InitAccount);
            _chainId = ChainHelper.ConvertBase58ToChainId(NodeManager.GetChainId());

            if (NFT.Equals(""))
                _nftContract = new NftContract(NodeManager, InitAccount);
            else
                _nftContract = new NftContract(NodeManager, InitAccount, NFT);

            if (NFTMarket.Equals(""))
                _nftMarketContract = new NFTMarketContract(NodeManager, InitAccount);
            else
                _nftMarketContract = new NFTMarketContract(NodeManager, InitAccount, NFTMarket);

            _genesisContract = GenesisContract.GetGenesisContract(NodeManager, InitAccount);
            _tokenContract = _genesisContract.GetTokenContract(InitAccount);

            AddWhiteList();
        }

        [TestMethod]
        public void ListWithFixedPriceWhiteListTest()
        {
            var totalSupply = 10000;
            var mintAmount = 1000;
            var sellAmount = 100;
            var fixedPrice = 10_00000000;
            var whitePrice1 = 9_00000000;
            var whitePrice2 = 10_00000000;
            var whitePrice3 = 11_00000000;
            var purchaseSymbol = "ELF";
            var isMerge = true;

            // StartTime = PublicTime
            var tokenId = 1;
            var symbol = CreateAndMint(totalSupply, mintAmount, tokenId);
            var startTime = DateTime.UtcNow.AddSeconds(60).ToTimestamp();
            var publicTime = DateTime.UtcNow.AddSeconds(60).ToTimestamp();
            var durationHours = 48;
            ListWithFixedPrice(symbol, tokenId, sellAmount, fixedPrice, whitePrice1, whitePrice2,
                whitePrice3, startTime, publicTime, durationHours, purchaseSymbol, isMerge);
            CheckListedNftInfo(symbol, tokenId, InitAccount, new ListedNFTInfoList
            {
                Value =
                {
                    new ListedNFTInfo
                    {
                        Symbol = symbol,
                        TokenId = tokenId,
                        Owner = InitAccount.ConvertAddress(),
                        Quantity = sellAmount,
                        ListType = ListType.FixedPrice,
                        Price = new Price
                        {
                            Symbol = purchaseSymbol,
                            Amount = fixedPrice
                        },
                        Duration = new ListDuration
                        {
                            StartTime = startTime,
                            PublicTime = publicTime,
                            DurationHours = durationHours
                        }
                    }
                }
            });

            // StartTime < PublicTime
            startTime = DateTime.UtcNow.AddHours(12).ToTimestamp();
            publicTime = DateTime.UtcNow.AddHours(24).ToTimestamp();
            durationHours = 48;
            ListWithFixedPrice(symbol, tokenId, sellAmount, fixedPrice, whitePrice1, whitePrice2,
                whitePrice3, startTime, publicTime, durationHours, purchaseSymbol, isMerge);
            CheckListedNftInfo(symbol, tokenId, InitAccount, new ListedNFTInfoList
            {
                Value =
                {
                    new ListedNFTInfo
                    {
                        Symbol = symbol,
                        TokenId = tokenId,
                        Owner = InitAccount.ConvertAddress(),
                        Quantity = sellAmount * 2,
                        ListType = ListType.FixedPrice,
                        Price = new Price
                        {
                            Symbol = purchaseSymbol,
                            Amount = fixedPrice
                        },
                        Duration = new ListDuration
                        {
                            StartTime = startTime,
                            PublicTime = publicTime,
                            DurationHours = durationHours
                        }
                    }
                }
            });

            // StartTime > PublicTime
            startTime = DateTime.UtcNow.AddHours(24).ToTimestamp();
            publicTime = DateTime.UtcNow.AddHours(12).ToTimestamp();
            durationHours = 48;
            ListWithFixedPrice(symbol, tokenId, sellAmount, fixedPrice, whitePrice1, whitePrice2,
                whitePrice3, startTime, publicTime, durationHours, purchaseSymbol, isMerge);
            CheckListedNftInfo(symbol, tokenId, InitAccount, new ListedNFTInfoList
            {
                Value =
                {
                    new ListedNFTInfo
                    {
                        Symbol = symbol,
                        TokenId = tokenId,
                        Owner = InitAccount.ConvertAddress(),
                        Quantity = sellAmount * 3,
                        ListType = ListType.FixedPrice,
                        Price = new Price
                        {
                            Symbol = purchaseSymbol,
                            Amount = fixedPrice
                        },
                        Duration = new ListDuration
                        {
                            StartTime = startTime,
                            PublicTime = startTime,
                            DurationHours = durationHours
                        }
                    }
                }
            });

            var whiteListAddressPriceList =
                _nftMarketContract.GetWhiteListAddressPriceList(symbol, tokenId, InitAccount);
            whiteListAddressPriceList.Value.Count.ShouldBe(3);
            whiteListAddressPriceList.Value[0].Address.ShouldBe(WhiteListAddress1.ConvertAddress());
            whiteListAddressPriceList.Value[0].Price.Symbol.ShouldBe("ELF");
            whiteListAddressPriceList.Value[0].Price.Amount.ShouldBe(whitePrice1);
            whiteListAddressPriceList.Value[1].Address.ShouldBe(WhiteListAddress2.ConvertAddress());
            whiteListAddressPriceList.Value[1].Price.Symbol.ShouldBe("ELF");
            whiteListAddressPriceList.Value[1].Price.Amount.ShouldBe(whitePrice2);
            whiteListAddressPriceList.Value[2].Address.ShouldBe(WhiteListAddress3.ConvertAddress());
            whiteListAddressPriceList.Value[2].Price.Symbol.ShouldBe("ELF");
            whiteListAddressPriceList.Value[2].Price.Amount.ShouldBe(whitePrice3);

            // Price.symbol is NFT
            purchaseSymbol = CreateAndMint(totalSupply, mintAmount, tokenId);
            startTime = DateTime.UtcNow.AddHours(12).ToTimestamp();
            publicTime = DateTime.UtcNow.AddHours(24).ToTimestamp();
            durationHours = 48;
            // SetTokenWhiteList
            var result = _nftMarketContract.SetTokenWhiteList(symbol, new StringList
            {
                Value = { purchaseSymbol }
            });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            ListWithFixedPrice(symbol, tokenId, sellAmount, fixedPrice, whitePrice1, whitePrice2,
                whitePrice3, startTime, publicTime, durationHours, purchaseSymbol, isMerge);
            var listedNftInfo =
                _nftMarketContract.GetListedNFTInfoList(symbol, tokenId, InitAccount).Value[1];
            listedNftInfo.Symbol.ShouldBe(symbol);
            listedNftInfo.TokenId.ShouldBe(tokenId);
            listedNftInfo.Owner.ShouldBe(InitAccount.ConvertAddress());
            listedNftInfo.Quantity.ShouldBe(sellAmount);
            listedNftInfo.ListType.ShouldBe(ListType.FixedPrice);
            listedNftInfo.Price.Symbol.ShouldBe(purchaseSymbol);
            listedNftInfo.Price.Amount.ShouldBe(fixedPrice);
            listedNftInfo.Duration.StartTime.Seconds.ShouldBe(startTime.Seconds);
            listedNftInfo.Duration.PublicTime.Seconds.ShouldBe(publicTime.Seconds);
            listedNftInfo.Duration.DurationHours.ShouldBe(durationHours);
        }

        [TestMethod]
        [DataRow(1, 1, 1, 1, 10, 9, 10, 11, "ELF", true)]
        [DataRow(2, 1000, 1000, 100, 10, 9, 10, 11, "ELF", true)]
        public void ListWithFixedPriceTest(int tokenId, long totalSupply, long mintAmount, long sellAmount,
            long fixedPrice,
            long whitePrice1, long whitePrice2, long whitePrice3, string purchaseSymbol, bool isMerge)
        {
            var startTime = DateTime.UtcNow.AddSeconds(60).ToTimestamp();
            var publicTime = DateTime.UtcNow.AddSeconds(60).ToTimestamp();
            var durationHours = 48;
            var symbol = CreateAndMint(totalSupply, mintAmount, tokenId);
            Logger.Info($"startTime is {startTime.Seconds}");
            Logger.Info($"publicTime is {publicTime.Seconds}");

            ListWithFixedPrice(symbol, tokenId, sellAmount, fixedPrice, whitePrice1, whitePrice2,
                whitePrice3, startTime, publicTime, durationHours, purchaseSymbol, isMerge);

            CheckListedNftInfo(symbol, tokenId, InitAccount, new ListedNFTInfoList
            {
                Value =
                {
                    new ListedNFTInfo
                    {
                        Symbol = symbol,
                        TokenId = tokenId,
                        Owner = InitAccount.ConvertAddress(),
                        Quantity = sellAmount,
                        ListType = ListType.FixedPrice,
                        Price = new Price
                        {
                            Symbol = purchaseSymbol,
                            Amount = fixedPrice
                        },
                        Duration = new ListDuration
                        {
                            StartTime = startTime,
                            PublicTime = publicTime,
                            DurationHours = durationHours
                        }
                    }
                }
            });
        }

        [TestMethod]
        public void ListWithFixedPriceDifferentOwnerTest()
        {
            var totalSupply = 10000;
            var mintAmount = 1000;
            var tokenId = 1;
            var sellAmount = 100;
            var fixedPrice = 10_00000000;
            var purchaseSymbol = "ELF";
            var startTime = DateTime.UtcNow.AddSeconds(60).ToTimestamp();
            var publicTime = DateTime.UtcNow.AddSeconds(60).ToTimestamp();
            var isMerge = true;
            var symbol = CreateAndMint(totalSupply, mintAmount, tokenId);
            Logger.Info($"startTime is {startTime.Seconds}");
            Logger.Info($"publicTime is {publicTime.Seconds}");

            // Initialize
            var initialize = _nftMarketContract.Initialize(
                _nftContract.ContractAddress,
                InitAccount,
                10,
                InitAccount,
                1000
            );
            initialize.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            // Approve
            var approve =
                _nftContract.Approve(_nftMarketContract.ContractAddress, symbol, tokenId, 1000000);
            approve.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            // Transfer
            var transfer = _nftContract.TransferNftToken(mintAmount / 2, tokenId, symbol, OtherAccount);
            transfer.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            // ListWithFixedPrice
            _nftMarketContract.ListWithFixedPrice(
                symbol,
                tokenId,
                new Price
                {
                    Symbol = purchaseSymbol,
                    Amount = fixedPrice
                },
                sellAmount,
                new ListDuration(),
                new WhiteListAddressPriceList(),
                isMerge
            );

            // ListWithFixedPrice
            _nftMarketContract.SetAccount(OtherAccount);
            _nftContract.SetAccount(OtherAccount);
            approve =
                _nftContract.Approve(_nftMarketContract.ContractAddress, symbol, tokenId, 1000000);
            approve.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            _nftMarketContract.ListWithFixedPrice(
                symbol,
                tokenId,
                new Price
                {
                    Symbol = purchaseSymbol,
                    Amount = fixedPrice
                },
                sellAmount + 1,
                new ListDuration(),
                new WhiteListAddressPriceList(),
                isMerge
            );
            var listedNftInfo =
                _nftMarketContract.GetListedNFTInfoList(symbol, tokenId, InitAccount);
            listedNftInfo.Value[0].Symbol.ShouldBe(symbol);
            listedNftInfo.Value[0].TokenId.ShouldBe(tokenId);
            listedNftInfo.Value[0].Owner.ShouldBe(InitAccount.ConvertAddress());
            listedNftInfo.Value[0].Quantity.ShouldBe(sellAmount);

            listedNftInfo =
                _nftMarketContract.GetListedNFTInfoList(symbol, tokenId, OtherAccount);
            listedNftInfo.Value[0].Symbol.ShouldBe(symbol);
            listedNftInfo.Value[0].TokenId.ShouldBe(tokenId);
            listedNftInfo.Value[0].Owner.ShouldBe(OtherAccount.ConvertAddress());
            listedNftInfo.Value[0].Quantity.ShouldBe(sellAmount + 1);
        }

        [TestMethod]
        [DataRow(1, 2, 1000, 1000, 100, 10_00000000, 9_00000000, 10_00000000, 11_00000000, "ELF", true, 49)]
        [DataRow(2, 2, 1000, 1000, 100, 10_00000000, 9_00000000, 10_00000000, 11_00000000, "ELF", true, 24)]
        [DataRow(3, 2, 1000, 1000, 100, 10_00000000, 9_00000000, 10_00000000, 11_00000000, "USDT", true, 24)]
        [DataRow(4, 2, 1000, 1000, 100, 20_00000000, 9_00000000, 10_00000000, 11_00000000, "ELF", true, 24)]
        [DataRow(5, 2, 1000, 1000, 100, 10_00000000, 9_00000000, 10_00000000, 11_00000000, "ELF", false, 24)]
        [DataRow(6, 2, 1000, 1000, 100, 10_00000000, 9_00000000, 10_00000000, 11_00000000, "ELF", false, 30)]
        public void ListWithFixedPriceAgainTest(int times, int tokenId, long totalSupply,
            long mintAmount,
            long sellAmount,
            long fixedPrice,
            long whitePrice1, long whitePrice2, long whitePrice3, string purchaseSymbol, bool isMerge,
            int durationHours)
        {
            // NFTContract: 2qdf5ArPmD7AWTy8LsPv7giAVRrB59aLYm4adZnfMk4FHGGoko
            // NFTMarketContract: Qr6cJSLiLoTQsuVf6aPwHXJK438V99HxD8ZU9x3ZPQ14GqWF3
            var symbol = "CO481022094";

            // SetTokenWhiteList
            var result = _nftMarketContract.SetTokenWhiteList(symbol, new StringList
            {
                Value = { "USDT" }
            });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            var startTime = new Timestamp();
            var publicTime = new Timestamp();
            if (times == 2 || times == 5)
            {
                startTime = new DateTime(2023, 1, 8, 00, 00, 00, 00, kind: DateTimeKind.Utc).ToTimestamp();
                publicTime = new DateTime(2023, 2, 8, 00, 00, 00, 00, kind: DateTimeKind.Utc).ToTimestamp();
            }
            else
            {
                startTime = DateTime.UtcNow.AddHours(1).AddHours(times).ToTimestamp();
                publicTime = DateTime.UtcNow.AddHours(1).AddHours(times).ToTimestamp();
            }

            Logger.Info($"startTime is {startTime.Seconds}");
            Logger.Info($"publicTime is {publicTime.Seconds}");

            ListWithFixedPrice(symbol, tokenId, sellAmount, fixedPrice, whitePrice1, whitePrice2,
                whitePrice3, startTime, publicTime, durationHours, purchaseSymbol, isMerge);

            if (times == 1)
            {
                var listedNftInfo =
                    _nftMarketContract.GetListedNFTInfoList(symbol, tokenId, InitAccount).Value[0];
                listedNftInfo.Symbol.ShouldBe(symbol);
                listedNftInfo.TokenId.ShouldBe(tokenId);
                listedNftInfo.Owner.ShouldBe(InitAccount.ConvertAddress());
                listedNftInfo.Quantity.ShouldBe(sellAmount * 2);
                listedNftInfo.ListType.ShouldBe(ListType.FixedPrice);
                listedNftInfo.Price.Symbol.ShouldBe(purchaseSymbol);
                listedNftInfo.Price.Amount.ShouldBe(fixedPrice);
                listedNftInfo.Duration.StartTime.Seconds.ShouldBe(startTime.Seconds);
                listedNftInfo.Duration.PublicTime.Seconds.ShouldBe(publicTime.Seconds);
                listedNftInfo.Duration.DurationHours.ShouldBe(durationHours);
            }
            else if (times == 2)
            {
                var listedNftInfo =
                    _nftMarketContract.GetListedNFTInfoList(symbol, tokenId, InitAccount).Value[0];
                listedNftInfo.Symbol.ShouldBe(symbol);
                listedNftInfo.TokenId.ShouldBe(tokenId);
                listedNftInfo.Owner.ShouldBe(InitAccount.ConvertAddress());
                listedNftInfo.Quantity.ShouldBe(sellAmount * 3);
                listedNftInfo.ListType.ShouldBe(ListType.FixedPrice);
                listedNftInfo.Price.Symbol.ShouldBe(purchaseSymbol);
                listedNftInfo.Price.Amount.ShouldBe(fixedPrice);
                listedNftInfo.Duration.StartTime.Seconds.ShouldBe(startTime.Seconds);
                listedNftInfo.Duration.PublicTime.Seconds.ShouldBe(publicTime.Seconds);
                listedNftInfo.Duration.DurationHours.ShouldBe(durationHours);
            }
            else if (times == 3)
            {
                var listedNftInfo =
                    _nftMarketContract.GetListedNFTInfoList(symbol, tokenId, InitAccount).Value[1];
                listedNftInfo.Symbol.ShouldBe(symbol);
                listedNftInfo.TokenId.ShouldBe(tokenId);
                listedNftInfo.Owner.ShouldBe(InitAccount.ConvertAddress());
                listedNftInfo.Quantity.ShouldBe(sellAmount);
                listedNftInfo.ListType.ShouldBe(ListType.FixedPrice);
                listedNftInfo.Price.Symbol.ShouldBe(purchaseSymbol);
                listedNftInfo.Price.Amount.ShouldBe(fixedPrice);
                listedNftInfo.Duration.StartTime.Seconds.ShouldBe(startTime.Seconds);
                listedNftInfo.Duration.PublicTime.Seconds.ShouldBe(publicTime.Seconds);
                listedNftInfo.Duration.DurationHours.ShouldBe(durationHours);
            }
            else if (times == 4)
            {
                var listedNftInfo =
                    _nftMarketContract.GetListedNFTInfoList(symbol, tokenId, InitAccount).Value[2];
                listedNftInfo.Symbol.ShouldBe(symbol);
                listedNftInfo.TokenId.ShouldBe(tokenId);
                listedNftInfo.Owner.ShouldBe(InitAccount.ConvertAddress());
                listedNftInfo.Quantity.ShouldBe(sellAmount);
                listedNftInfo.ListType.ShouldBe(ListType.FixedPrice);
                listedNftInfo.Price.Symbol.ShouldBe(purchaseSymbol);
                listedNftInfo.Price.Amount.ShouldBe(fixedPrice);
                listedNftInfo.Duration.StartTime.Seconds.ShouldBe(startTime.Seconds);
                listedNftInfo.Duration.PublicTime.Seconds.ShouldBe(publicTime.Seconds);
                listedNftInfo.Duration.DurationHours.ShouldBe(durationHours);
            }
            else if (times == 5)
            {
                var listedNftInfo =
                    _nftMarketContract.GetListedNFTInfoList(symbol, tokenId, InitAccount).Value[0];
                listedNftInfo.Symbol.ShouldBe(symbol);
                listedNftInfo.TokenId.ShouldBe(tokenId);
                listedNftInfo.Owner.ShouldBe(InitAccount.ConvertAddress());
                listedNftInfo.Quantity.ShouldBe(sellAmount * 4);
                listedNftInfo.ListType.ShouldBe(ListType.FixedPrice);
                listedNftInfo.Price.Symbol.ShouldBe(purchaseSymbol);
                listedNftInfo.Price.Amount.ShouldBe(fixedPrice);
                listedNftInfo.Duration.StartTime.Seconds.ShouldBe(startTime.Seconds);
                listedNftInfo.Duration.PublicTime.Seconds.ShouldBe(publicTime.Seconds);
                listedNftInfo.Duration.DurationHours.ShouldBe(durationHours);
            }
            else if (times == 6)
            {
                var listedNftInfo =
                    _nftMarketContract.GetListedNFTInfoList(symbol, tokenId, InitAccount).Value[3];
                listedNftInfo.Symbol.ShouldBe(symbol);
                listedNftInfo.TokenId.ShouldBe(tokenId);
                listedNftInfo.Owner.ShouldBe(InitAccount.ConvertAddress());
                listedNftInfo.Quantity.ShouldBe(sellAmount);
                listedNftInfo.ListType.ShouldBe(ListType.FixedPrice);
                listedNftInfo.Price.Symbol.ShouldBe(purchaseSymbol);
                listedNftInfo.Price.Amount.ShouldBe(fixedPrice);
                listedNftInfo.Duration.StartTime.Seconds.ShouldBe(startTime.Seconds);
                listedNftInfo.Duration.PublicTime.Seconds.ShouldBe(publicTime.Seconds);
                listedNftInfo.Duration.DurationHours.ShouldBe(durationHours);
            }
        }

        [TestMethod]
        public void ListWithFixedPriceErrorTest()
        {
            var totalSupply = 10000;
            var mintAmount = 1000;
            var tokenId = 1;
            var sellAmount = 100;
            var fixedPrice = 10_00000000;
            var purchaseSymbol = "ELF";
            var isMerge = true;

            var symbol = CreateAndMint(totalSupply, mintAmount, tokenId);

            // Check initialization
            var listWithFixedPriceResult = _nftMarketContract.ListWithFixedPrice(
                symbol,
                tokenId,
                new Price
                {
                    Symbol = purchaseSymbol,
                    Amount = fixedPrice
                },
                sellAmount,
                new ListDuration(),
                new WhiteListAddressPriceList(),
                isMerge
            );
            listWithFixedPriceResult.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            listWithFixedPriceResult.Error.ShouldContain("Contract not initialized.");

            // Initialize
            var initialize = _nftMarketContract.Initialize(
                _nftContract.ContractAddress,
                InitAccount,
                10,
                InitAccount,
                1000
            );
            initialize.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            // Check allowance
            listWithFixedPriceResult = _nftMarketContract.ListWithFixedPrice(
                symbol,
                tokenId,
                new Price
                {
                    Symbol = purchaseSymbol,
                    Amount = fixedPrice
                },
                sellAmount,
                new ListDuration(),
                new WhiteListAddressPriceList(),
                isMerge
            );
            listWithFixedPriceResult.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            listWithFixedPriceResult.Error.ShouldContain("Check sender NFT allowance failed.");

            // Approve
            var approve =
                _nftContract.Approve(_nftMarketContract.ContractAddress, symbol, tokenId, 1000000);
            approve.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            // Check symbol
            listWithFixedPriceResult = _nftMarketContract.ListWithFixedPrice(
                "CO12345678",
                tokenId,
                new Price
                {
                    Symbol = purchaseSymbol,
                    Amount = fixedPrice
                },
                sellAmount,
                new ListDuration(),
                new WhiteListAddressPriceList(),
                isMerge
            );
            listWithFixedPriceResult.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            listWithFixedPriceResult.Error.ShouldContain("Check sender NFT balance failed.");

            // Check tokenId
            listWithFixedPriceResult = _nftMarketContract.ListWithFixedPrice(
                symbol,
                100,
                new Price
                {
                    Symbol = purchaseSymbol,
                    Amount = fixedPrice
                },
                sellAmount,
                new ListDuration(),
                new WhiteListAddressPriceList(),
                isMerge
            );
            listWithFixedPriceResult.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            listWithFixedPriceResult.Error.ShouldContain("Check sender NFT balance failed.");

            // Check price.symbol
            listWithFixedPriceResult = _nftMarketContract.ListWithFixedPrice(
                symbol,
                tokenId,
                new Price
                {
                    Symbol = "USDT",
                    Amount = fixedPrice
                },
                sellAmount,
                new ListDuration(),
                new WhiteListAddressPriceList(),
                isMerge
            );
            listWithFixedPriceResult.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            listWithFixedPriceResult.Error.ShouldContain("USDT is not in token white list.");

            // Check price.amount
            listWithFixedPriceResult = _nftMarketContract.ListWithFixedPrice(
                symbol,
                tokenId,
                new Price
                {
                    Symbol = purchaseSymbol,
                    Amount = 0
                },
                sellAmount,
                new ListDuration(),
                new WhiteListAddressPriceList(),
                isMerge
            );
            listWithFixedPriceResult.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            listWithFixedPriceResult.Error.ShouldContain("Incorrect listing price.");

            listWithFixedPriceResult = _nftMarketContract.ListWithFixedPrice(
                symbol,
                tokenId,
                new Price
                {
                    Symbol = purchaseSymbol,
                    Amount = -1
                },
                mintAmount,
                new ListDuration(),
                new WhiteListAddressPriceList(),
                isMerge
            );
            listWithFixedPriceResult.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            listWithFixedPriceResult.Error.ShouldContain("Incorrect listing price.");

            // Check quantity
            listWithFixedPriceResult = _nftMarketContract.ListWithFixedPrice(
                symbol,
                tokenId,
                new Price
                {
                    Symbol = purchaseSymbol,
                    Amount = fixedPrice
                },
                mintAmount + 1,
                new ListDuration(),
                new WhiteListAddressPriceList(),
                isMerge
            );
            listWithFixedPriceResult.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            listWithFixedPriceResult.Error.ShouldContain("Check sender NFT balance failed.");

            listWithFixedPriceResult = _nftMarketContract.ListWithFixedPrice(
                symbol,
                tokenId,
                new Price
                {
                    Symbol = purchaseSymbol,
                    Amount = fixedPrice
                },
                0,
                new ListDuration(),
                new WhiteListAddressPriceList(),
                isMerge
            );
            listWithFixedPriceResult.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            listWithFixedPriceResult.Error.ShouldContain("Incorrect quantity.");

            listWithFixedPriceResult = _nftMarketContract.ListWithFixedPrice(
                symbol,
                tokenId,
                new Price
                {
                    Symbol = purchaseSymbol,
                    Amount = fixedPrice
                },
                -1,
                new ListDuration(),
                new WhiteListAddressPriceList(),
                isMerge
            );
            listWithFixedPriceResult.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            listWithFixedPriceResult.Error.ShouldContain("Incorrect quantity.");

            // Check default values
            listWithFixedPriceResult = _nftMarketContract.ListWithFixedPrice(
                symbol,
                tokenId,
                new Price
                {
                    Symbol = purchaseSymbol,
                    Amount = fixedPrice
                },
                sellAmount,
                new ListDuration(),
                new WhiteListAddressPriceList(),
                isMerge
            );
            listWithFixedPriceResult.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);

            var listedNftInfo =
                _nftMarketContract.GetListedNFTInfoList(symbol, tokenId, InitAccount).Value[0];
            listedNftInfo.Symbol.ShouldBe(symbol);
            listedNftInfo.TokenId.ShouldBe(tokenId);
            listedNftInfo.Owner.ShouldBe(InitAccount.ConvertAddress());
            listedNftInfo.Quantity.ShouldBe(sellAmount);
            listedNftInfo.ListType.ShouldBe(ListType.FixedPrice);
            listedNftInfo.Price.Symbol.ShouldBe(purchaseSymbol);
            listedNftInfo.Price.Amount.ShouldBe(fixedPrice);
            listedNftInfo.Duration.ShouldNotBeNull();
            listedNftInfo.Duration.DurationHours.ShouldBe(Int32.MaxValue);
            Logger.Info($"Duration.StartTime is {listedNftInfo.Duration.StartTime.Seconds}");
            Logger.Info($"Duration.PublicTime is {listedNftInfo.Duration.PublicTime.Seconds}");
            Logger.Info($"Duration.DurationHours is {listedNftInfo.Duration.DurationHours}");
        }
        private void ContractInitialize()
        {
            var serviceFeeReceiver = WhiteListAddress2;
            var serviceFeeRate = 10;
            var serviceFee = 1000_00000000;

            // Initialize
            var initialize = _nftMarketContract.Initialize(
                _nftContract.ContractAddress,
                InitAccount,
                serviceFeeRate,
                serviceFeeReceiver,
                serviceFee
            );
            initialize.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
        }
        
        
        [TestMethod]
        public void ListWithEnglishAuctionTest()
        {
            var tokenId = 1;
            var totalSupply = 1000;
            var mintAmount = 1000;
            var startingPrice = 10;
            var purchaseSymbol = "ELF";
            var startTime = DateTime.UtcNow.AddHours(12).ToTimestamp();
            var publicTime = DateTime.UtcNow.AddHours(24).ToTimestamp();
            var durationHours = 48;
            var earnestMoney = startingPrice;
            var whiteSymbol = "ELF";
            var whitePrice = 9_00000000;
            var symbol = CreateAndMint(totalSupply, mintAmount, tokenId);

            ListWithEnglistAuction(symbol, tokenId, startingPrice, purchaseSymbol,
                startTime, publicTime, durationHours, earnestMoney, whiteSymbol, whitePrice);

            GetEnglishAuctionInfo(symbol, tokenId);

            CheckEnglishAuctionInfo(symbol, tokenId, new EnglishAuctionInfo
            {
                Symbol = symbol,
                TokenId = tokenId,
                StartingPrice = startingPrice,
                PurchaseSymbol = purchaseSymbol,
                Duration = new ListDuration
                {
                    StartTime = startTime,
                    PublicTime = publicTime,
                    DurationHours = durationHours
                },
                Owner = InitAccount.ConvertAddress(),
                EarnestMoney = 0,
                DealPrice = 0,
                DealTo = null
            });
        }

        [TestMethod]
        public void ListWithEnglishAuctionErrorTest()
        {
            var tokenId = 1;
            var totalSupply = 1000;
            var mintAmount = 1000;
            var startingPrice = 10_00000000;
            var purchaseSymbol = "ELF";
            var earnestMoney = startingPrice;
            var symbol = CreateAndMint(totalSupply, mintAmount, tokenId);

            // Check initialization
            var listWithEnglishAuctionResult = _nftMarketContract.ListWithEnglishAuction(
                symbol,
                tokenId,
                startingPrice,
                purchaseSymbol,
                new ListDuration(),
                earnestMoney,
                new WhiteListAddressPriceList()
            );
            listWithEnglishAuctionResult.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            listWithEnglishAuctionResult.Error.ShouldContain("Contract not initialized.");

            // Initialize
            var initialize = _nftMarketContract.Initialize(
                _nftContract.ContractAddress,
                InitAccount,
                100,
                InitAccount,
                10_00000000
            );
            initialize.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            // Check allowance
            listWithEnglishAuctionResult = _nftMarketContract.ListWithEnglishAuction(
                symbol,
                tokenId,
                startingPrice,
                purchaseSymbol,
                new ListDuration(),
                earnestMoney,
                new WhiteListAddressPriceList()
            );
            listWithEnglishAuctionResult.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            listWithEnglishAuctionResult.Error.ShouldContain("Check sender NFT allowance failed.");

            // Approve
            var approve =
                _nftContract.Approve(_nftMarketContract.ContractAddress, symbol, tokenId, 1000000);
            approve.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            // Check symbol
            listWithEnglishAuctionResult = _nftMarketContract.ListWithEnglishAuction(
                "CO12345678",
                tokenId,
                startingPrice,
                purchaseSymbol,
                new ListDuration(),
                earnestMoney,
                new WhiteListAddressPriceList()
            );
            listWithEnglishAuctionResult.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            listWithEnglishAuctionResult.Error.ShouldContain("Check sender NFT balance failed.");

            // Check tokenId
            listWithEnglishAuctionResult = _nftMarketContract.ListWithEnglishAuction(
                symbol,
                100,
                startingPrice,
                purchaseSymbol,
                new ListDuration(),
                earnestMoney,
                new WhiteListAddressPriceList()
            );
            listWithEnglishAuctionResult.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            listWithEnglishAuctionResult.Error.ShouldContain("Check sender NFT balance failed.");

            // Check startingPrice
            listWithEnglishAuctionResult = _nftMarketContract.ListWithEnglishAuction(
                symbol,
                tokenId,
                0,
                purchaseSymbol,
                new ListDuration(),
                earnestMoney,
                new WhiteListAddressPriceList()
            );
            listWithEnglishAuctionResult.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            listWithEnglishAuctionResult.Error.ShouldContain("Incorrect listing price.");

            listWithEnglishAuctionResult = _nftMarketContract.ListWithEnglishAuction(
                symbol,
                tokenId,
                -1,
                purchaseSymbol,
                new ListDuration(),
                earnestMoney,
                new WhiteListAddressPriceList()
            );
            listWithEnglishAuctionResult.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            listWithEnglishAuctionResult.Error.ShouldContain("Incorrect listing price.");

            // Check EarnestMoney
            listWithEnglishAuctionResult = _nftMarketContract.ListWithEnglishAuction(
                symbol,
                tokenId,
                startingPrice,
                purchaseSymbol,
                new ListDuration(),
                earnestMoney + 1,
                new WhiteListAddressPriceList()
            );
            listWithEnglishAuctionResult.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            listWithEnglishAuctionResult.Error.ShouldContain("Earnest money too high.");

            // Check purchaseSymbol
            listWithEnglishAuctionResult = _nftMarketContract.ListWithEnglishAuction(
                symbol,
                tokenId,
                startingPrice,
                "USDT",
                new ListDuration(),
                earnestMoney,
                new WhiteListAddressPriceList()
            );
            listWithEnglishAuctionResult.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            listWithEnglishAuctionResult.Error.ShouldContain("USDT is not in token white list.");

            // SetTokenWhiteList
            var result = _nftMarketContract.SetTokenWhiteList(symbol, new StringList
            {
                Value = { "USDT", symbol }
            });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            listWithEnglishAuctionResult = _nftMarketContract.ListWithEnglishAuction(
                symbol,
                tokenId,
                startingPrice,
                symbol,
                new ListDuration(),
                earnestMoney,
                new WhiteListAddressPriceList()
            );
            listWithEnglishAuctionResult.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            listWithEnglishAuctionResult.Error.ShouldContain("not support purchase for auction.");

            listWithEnglishAuctionResult = _nftMarketContract.ListWithEnglishAuction(
                symbol,
                tokenId,
                startingPrice,
                purchaseSymbol,
                new ListDuration(),
                earnestMoney,
                new WhiteListAddressPriceList()
            );
            listWithEnglishAuctionResult.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);

            var englishAuctionInfo =
                _nftMarketContract.GetEnglishAuctionInfo(symbol, tokenId);
            englishAuctionInfo.Symbol.ShouldBe(symbol);
            englishAuctionInfo.TokenId.ShouldBe(tokenId);
            englishAuctionInfo.StartingPrice.ShouldBe(startingPrice);
            englishAuctionInfo.PurchaseSymbol.ShouldBe(purchaseSymbol);
            englishAuctionInfo.Duration.StartTime.ShouldNotBeNull();
            englishAuctionInfo.Duration.PublicTime.ShouldNotBeNull();
            englishAuctionInfo.Duration.DurationHours.ShouldBe(Int32.MaxValue);
            englishAuctionInfo.Owner.ShouldBe(InitAccount.ConvertAddress());
            englishAuctionInfo.EarnestMoney.ShouldBe(earnestMoney);
            englishAuctionInfo.DealPrice.ShouldBe(0);
            englishAuctionInfo.DealTo.ShouldBeNull();
            Logger.Info($"englishAuctionInfo.StartTime is {englishAuctionInfo.Duration.StartTime.Seconds}");
            Logger.Info($"englishAuctionInfo.PublicTime is {englishAuctionInfo.Duration.PublicTime.Seconds}");
            Logger.Info($"englishAuctionInfo.DurationHours is {englishAuctionInfo.Duration.DurationHours}");

            var whiteListAddressPriceList =
                _nftMarketContract.GetWhiteListAddressPriceList(symbol, tokenId, InitAccount);
            whiteListAddressPriceList.Value.Count.ShouldBe(0);
        }

        [TestMethod]
        public void ListWithDutchAuctionTest()
        {
            var tokenId = 1;
            var totalSupply = 1000;
            var mintAmount = 1000;
            var startingPrice = 10_00000000;
            var endingPrice = 1_00000000;
            var purchaseSymbol = "ELF";
            var startTime = DateTime.UtcNow.AddHours(12).ToTimestamp();
            var publicTime = DateTime.UtcNow.AddHours(24).ToTimestamp();
            var durationHours = 48;
            var whiteSymbol = "ELF";
            var whitePrice = 9_00000000;
            var symbol = CreateAndMint(totalSupply, mintAmount, tokenId);

            ListWithDutchAuction(symbol, tokenId, startingPrice, endingPrice, purchaseSymbol,
                startTime, publicTime, durationHours, whiteSymbol, whitePrice);

            GetDutchAuctionInfo(symbol, tokenId);

            CheckDutchAuctionInfo(symbol, tokenId, new DutchAuctionInfo
            {
                Symbol = symbol,
                TokenId = tokenId,
                StartingPrice = startingPrice,
                EndingPrice = endingPrice,
                PurchaseSymbol = purchaseSymbol,
                Duration = new ListDuration
                {
                    StartTime = startTime,
                    PublicTime = publicTime,
                    DurationHours = durationHours
                },
                Owner = InitAccount.ConvertAddress()
            });
        }

        [TestMethod]
        public void ListWithDutchAuctionErrorTest()
        {
            var tokenId = 1;
            var totalSupply = 1000;
            var mintAmount = 1000;
            var startingPrice = 10_00000000;
            var endingPrice = 1_00000000;
            var purchaseSymbol = "ELF";
            var symbol = CreateAndMint(totalSupply, mintAmount, tokenId);

            // Check initialization
            var listWithDutchAuctionResult = _nftMarketContract.ListWithDutchAuction(
                symbol,
                tokenId,
                startingPrice,
                endingPrice,
                purchaseSymbol,
                new ListDuration(),
                new WhiteListAddressPriceList()
            );
            listWithDutchAuctionResult.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            listWithDutchAuctionResult.Error.ShouldContain("Contract not initialized.");

            // Initialize
            var initialize = _nftMarketContract.Initialize(
                _nftContract.ContractAddress,
                InitAccount,
                100,
                InitAccount,
                10_00000000
            );
            initialize.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            // Check allowance
            listWithDutchAuctionResult = _nftMarketContract.ListWithDutchAuction(
                symbol,
                tokenId,
                startingPrice,
                endingPrice,
                purchaseSymbol,
                new ListDuration(),
                new WhiteListAddressPriceList()
            );
            listWithDutchAuctionResult.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            listWithDutchAuctionResult.Error.ShouldContain("Check sender NFT allowance failed.");

            // Approve
            var approve =
                _nftContract.Approve(_nftMarketContract.ContractAddress, symbol, tokenId, 1000000);
            approve.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            // Check symbol
            listWithDutchAuctionResult = _nftMarketContract.ListWithDutchAuction(
                "CO12345678",
                tokenId,
                startingPrice,
                endingPrice,
                purchaseSymbol,
                new ListDuration(),
                new WhiteListAddressPriceList()
            );
            listWithDutchAuctionResult.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            listWithDutchAuctionResult.Error.ShouldContain("Check sender NFT balance failed.");

            // Check tokenId
            listWithDutchAuctionResult = _nftMarketContract.ListWithDutchAuction(
                symbol,
                100,
                startingPrice,
                endingPrice,
                purchaseSymbol,
                new ListDuration(),
                new WhiteListAddressPriceList()
            );
            listWithDutchAuctionResult.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            listWithDutchAuctionResult.Error.ShouldContain("Check sender NFT balance failed.");

            // Check startingPrice
            listWithDutchAuctionResult = _nftMarketContract.ListWithDutchAuction(
                symbol,
                tokenId,
                0,
                endingPrice,
                purchaseSymbol,
                new ListDuration(),
                new WhiteListAddressPriceList()
            );
            listWithDutchAuctionResult.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            listWithDutchAuctionResult.Error.ShouldContain("Incorrect listing price.");

            listWithDutchAuctionResult = _nftMarketContract.ListWithDutchAuction(
                symbol,
                tokenId,
                -1,
                endingPrice,
                purchaseSymbol,
                new ListDuration(),
                new WhiteListAddressPriceList()
            );
            listWithDutchAuctionResult.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            listWithDutchAuctionResult.Error.ShouldContain("Incorrect listing price.");

            // Check endingPrice
            listWithDutchAuctionResult = _nftMarketContract.ListWithDutchAuction(
                symbol,
                tokenId,
                startingPrice,
                0,
                purchaseSymbol,
                new ListDuration(),
                new WhiteListAddressPriceList()
            );
            listWithDutchAuctionResult.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            listWithDutchAuctionResult.Error.ShouldContain("Incorrect listing price.");

            listWithDutchAuctionResult = _nftMarketContract.ListWithDutchAuction(
                symbol,
                tokenId,
                startingPrice,
                -1,
                purchaseSymbol,
                new ListDuration(),
                new WhiteListAddressPriceList()
            );
            listWithDutchAuctionResult.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            listWithDutchAuctionResult.Error.ShouldContain("Incorrect listing price.");

            listWithDutchAuctionResult = _nftMarketContract.ListWithDutchAuction(
                symbol,
                tokenId,
                startingPrice,
                startingPrice,
                purchaseSymbol,
                new ListDuration(),
                new WhiteListAddressPriceList()
            );
            listWithDutchAuctionResult.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            listWithDutchAuctionResult.Error.ShouldContain("Incorrect listing price.");

            listWithDutchAuctionResult = _nftMarketContract.ListWithDutchAuction(
                symbol,
                tokenId,
                startingPrice,
                startingPrice + 1,
                purchaseSymbol,
                new ListDuration(),
                new WhiteListAddressPriceList()
            );
            listWithDutchAuctionResult.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            listWithDutchAuctionResult.Error.ShouldContain("Incorrect listing price.");

            // Check purchaseSymbol
            listWithDutchAuctionResult = _nftMarketContract.ListWithDutchAuction(
                symbol,
                tokenId,
                startingPrice,
                endingPrice,
                "USDT",
                new ListDuration(),
                new WhiteListAddressPriceList()
            );
            listWithDutchAuctionResult.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            listWithDutchAuctionResult.Error.ShouldContain("USDT is not in token white list.");

            // SetTokenWhiteList
            var result = _nftMarketContract.SetTokenWhiteList(symbol, new StringList
            {
                Value = { "USDT", symbol }
            });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            listWithDutchAuctionResult = _nftMarketContract.ListWithDutchAuction(
                symbol,
                tokenId,
                startingPrice,
                endingPrice,
                symbol,
                new ListDuration(),
                new WhiteListAddressPriceList()
            );
            listWithDutchAuctionResult.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            listWithDutchAuctionResult.Error.ShouldContain("not support purchase for auction.");

            // Check default values
            listWithDutchAuctionResult = _nftMarketContract.ListWithDutchAuction(
                symbol,
                tokenId,
                startingPrice,
                endingPrice,
                purchaseSymbol,
                new ListDuration(),
                new WhiteListAddressPriceList()
            );
            listWithDutchAuctionResult.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);

            var dutchAuctionInfo =
                _nftMarketContract.GetDutchAuctionInfo(symbol, tokenId);
            dutchAuctionInfo.Symbol.ShouldBe(symbol);
            dutchAuctionInfo.TokenId.ShouldBe(tokenId);
            dutchAuctionInfo.StartingPrice.ShouldBe(startingPrice);
            dutchAuctionInfo.EndingPrice.ShouldBe(endingPrice);
            dutchAuctionInfo.PurchaseSymbol.ShouldBe(purchaseSymbol);
            dutchAuctionInfo.Duration.StartTime.ShouldNotBeNull();
            dutchAuctionInfo.Duration.PublicTime.ShouldNotBeNull();
            dutchAuctionInfo.Duration.DurationHours.ShouldBe(Int32.MaxValue);
            dutchAuctionInfo.Owner.ShouldBe(InitAccount.ConvertAddress());
            Logger.Info($"dutchAuctionInfo.StartTime is {dutchAuctionInfo.Duration.StartTime.Seconds}");
            Logger.Info($"dutchAuctionInfo.PublicTime is {dutchAuctionInfo.Duration.PublicTime.Seconds}");
            Logger.Info($"dutchAuctionInfo.DurationHours is {dutchAuctionInfo.Duration.DurationHours}");

            var whiteListAddressPriceList =
                _nftMarketContract.GetWhiteListAddressPriceList(symbol, tokenId, InitAccount);
            whiteListAddressPriceList.Value.Count.ShouldBe(0);
        }

        [TestMethod]
        public void InsufficientBalanceErrorTest()
        {
            var tokenId = 1;
            var totalSupply = 1000;
            var startingPrice = 10_00000000;
            var endingPrice = 1_00000000;
            var purchaseSymbol = "ELF";
            var earnestMoney = startingPrice;

            var createResult =
                _nftContract.ExecuteMethodWithResult(NftContractMethod.Create, new CreateInput
                {
                    NftType = NFTType.Collectables.ToString(),
                    ProtocolName = "CAT",
                    TotalSupply = totalSupply,
                    Creator = InitAccount.ConvertAddress(),
                    IsBurnable = true,
                    IssueChainId = _chainId,
                    Metadata = new Metadata(),
                    BaseUri = "aelf.com/nft/",
                    IsTokenIdReuse = true
                });
            createResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var symbol = StringValue.Parser
                .ParseFrom(ByteArrayHelper.HexStringToByteArray(createResult.ReturnValue))
                .Value;

            // Initialize
            var initialize = _nftMarketContract.Initialize(
                _nftContract.ContractAddress,
                InitAccount,
                100,
                InitAccount,
                10_00000000
            );
            initialize.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            // Approve
            var approve =
                _nftContract.Approve(_nftMarketContract.ContractAddress, symbol, tokenId, 1000000);
            approve.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            // Check balance
            var listWithEnglishAuctionResult = _nftMarketContract.ListWithEnglishAuction(
                symbol,
                tokenId,
                startingPrice,
                purchaseSymbol,
                new ListDuration(),
                earnestMoney,
                new WhiteListAddressPriceList()
            );
            listWithEnglishAuctionResult.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            listWithEnglishAuctionResult.Error.ShouldContain("Check sender NFT balance failed.");

            var listWithDutchAuctionResult = _nftMarketContract.ListWithDutchAuction(
                symbol,
                tokenId,
                startingPrice,
                endingPrice,
                purchaseSymbol,
                new ListDuration(),
                new WhiteListAddressPriceList()
            );
            listWithDutchAuctionResult.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            listWithDutchAuctionResult.Error.ShouldContain("Check sender NFT balance failed.");
        }

        [TestMethod]
        public void SetRoyaltyTest()
        {
            var totalSupply = 10000;
            var mintAmount = 1000;

            var createResult =
                _nftContract.ExecuteMethodWithResult(NftContractMethod.Create, new CreateInput
                {
                    NftType = NFTType.Collectables.ToString(),
                    ProtocolName = "CAT",
                    TotalSupply = totalSupply,
                    Creator = InitAccount.ConvertAddress(),
                    IsBurnable = true,
                    IssueChainId = _chainId,
                    Metadata = new Metadata(),
                    BaseUri = "aelf.com/nft/",
                    IsTokenIdReuse = true
                });
            createResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var symbol = StringValue.Parser
                .ParseFrom(ByteArrayHelper.HexStringToByteArray(createResult.ReturnValue))
                .Value;

            var mintResult =
                _nftContract.ExecuteMethodWithResult(NftContractMethod.Mint, new MintInput
                {
                    Symbol = symbol,
                    Alias = "NFT_CO_CAT1",
                    Metadata = new Metadata(),
                    Quantity = mintAmount,
                    TokenId = 1
                });
            mintResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            var addMintersResult =
                _nftContract.AddMinters(new MinterList { Value = { OtherAccount.ConvertAddress() } }, symbol);
            addMintersResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            _nftContract.SetAccount(OtherAccount);
            mintResult =
                _nftContract.ExecuteMethodWithResult(NftContractMethod.Mint, new MintInput
                {
                    Symbol = symbol,
                    Alias = "NFT_CO_CAT1",
                    Metadata = new Metadata(),
                    Quantity = mintAmount,
                    TokenId = 2
                });
            mintResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            // Initialize
            var initialize = _nftMarketContract.Initialize(
                _nftContract.ContractAddress,
                InitAccount,
                100,
                InitAccount,
                10_00000000
            );
            initialize.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            var royalty = 1000;
            var royaltyFeeReceiver = WhiteListAddress3;
            var setRoyaltyResult = _nftMarketContract.SetRoyalty(symbol, 0, royalty, royaltyFeeReceiver);
            setRoyaltyResult.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);
            var royaltyInfoOfTokenId1 = _nftMarketContract.GetRoyalty(symbol, 1);
            royaltyInfoOfTokenId1.Royalty.ShouldBe(royalty);
            royaltyInfoOfTokenId1.RoyaltyFeeReceiver.ShouldBe(royaltyFeeReceiver.ConvertAddress());
            var royaltyInfoOfTokenId2 = _nftMarketContract.GetRoyalty(symbol, 2);
            royaltyInfoOfTokenId2.Royalty.ShouldBe(royalty);
            royaltyInfoOfTokenId2.RoyaltyFeeReceiver.ShouldBe(royaltyFeeReceiver.ConvertAddress());

            setRoyaltyResult = _nftMarketContract.SetRoyalty(symbol, 1, royalty - 1, royaltyFeeReceiver);
            setRoyaltyResult.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);
            royaltyInfoOfTokenId1 = _nftMarketContract.GetRoyalty(symbol, 1);
            royaltyInfoOfTokenId1.Royalty.ShouldBe(royalty - 1);
            royaltyInfoOfTokenId1.RoyaltyFeeReceiver.ShouldBe(royaltyFeeReceiver.ConvertAddress());
            royaltyInfoOfTokenId2 = _nftMarketContract.GetRoyalty(symbol, 2);
            royaltyInfoOfTokenId2.Royalty.ShouldBe(royalty);
            royaltyInfoOfTokenId2.RoyaltyFeeReceiver.ShouldBe(royaltyFeeReceiver.ConvertAddress());

            setRoyaltyResult = _nftMarketContract.SetRoyalty(symbol, 1, 0, royaltyFeeReceiver);
            setRoyaltyResult.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);
            // royaltyInfoOfTokenId1 = _nftMarketContract.GetRoyalty(symbol, 1);
            // royaltyInfoOfTokenId1.Royalty.ShouldBe(0);
            // royaltyInfoOfTokenId1.RoyaltyFeeReceiver.ShouldBe(royaltyFeeReceiver.ConvertAddress());
            // royaltyInfoOfTokenId2 = _nftMarketContract.GetRoyalty(symbol, 2);
            // royaltyInfoOfTokenId2.Royalty.ShouldBe(royalty);
            // royaltyInfoOfTokenId2.RoyaltyFeeReceiver.ShouldBe(royaltyFeeReceiver.ConvertAddress());
            royaltyInfoOfTokenId1 = _nftMarketContract.GetRoyalty(symbol, 1);
            royaltyInfoOfTokenId2 = _nftMarketContract.GetRoyalty(symbol, 2);


            setRoyaltyResult = _nftMarketContract.SetRoyalty(symbol, 1, -1, royaltyFeeReceiver);
            setRoyaltyResult.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            setRoyaltyResult.Error.ShouldContain("Royalty should be between 0% to 10%.");

            setRoyaltyResult = _nftMarketContract.SetRoyalty(symbol, 1, 1001, royaltyFeeReceiver);
            setRoyaltyResult.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            setRoyaltyResult.Error.ShouldContain("Royalty should be between 0% to 10%.");

            setRoyaltyResult = _nftMarketContract.SetRoyalty("CO12345678", 1, 50, royaltyFeeReceiver);
            setRoyaltyResult.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            setRoyaltyResult.Error.ShouldContain("NFT Protocol not found.");

            _nftMarketContract.SetAccount(OtherAccount);
            setRoyaltyResult = _nftMarketContract.SetRoyalty(symbol, 0, 50, royaltyFeeReceiver);
            setRoyaltyResult.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            setRoyaltyResult.Error.ShouldContain("Only NFT Protocol Creator can set royalty for whole protocol.");

            setRoyaltyResult = _nftMarketContract.SetRoyalty(symbol, 2, 50, royaltyFeeReceiver);
            setRoyaltyResult.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);
            // royaltyInfoOfTokenId1 = _nftMarketContract.GetRoyalty(symbol, 1);
            // royaltyInfoOfTokenId1.Royalty.ShouldBe(0);
            // royaltyInfoOfTokenId1.RoyaltyFeeReceiver.ShouldBe(royaltyFeeReceiver.ConvertAddress());
            // royaltyInfoOfTokenId2 = _nftMarketContract.GetRoyalty(symbol, 2);
            // royaltyInfoOfTokenId2.Royalty.ShouldBe(50);
            // royaltyInfoOfTokenId2.RoyaltyFeeReceiver.ShouldBe(royaltyFeeReceiver.ConvertAddress());

            _nftMarketContract.SetAccount(WhiteListAddress3);
            setRoyaltyResult = _nftMarketContract.SetRoyalty(symbol, 1, 50, royaltyFeeReceiver);
            setRoyaltyResult.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            setRoyaltyResult.Error.ShouldContain("No permission.");

            setRoyaltyResult = _nftMarketContract.SetRoyalty(symbol, 2, 50, royaltyFeeReceiver);
            setRoyaltyResult.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            setRoyaltyResult.Error.ShouldContain("No permission.");
        }

        [TestMethod]
        public void SetTokenWhiteListTest()
        {
            var totalSupply = 1000;
            var mintAmount = 1000;
            var symbol = CreateAndMint(totalSupply, mintAmount, 1);

            // Initialize
            var initialize = _nftMarketContract.Initialize(
                _nftContract.ContractAddress,
                InitAccount,
                100,
                InitAccount,
                10_00000000
            );
            initialize.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            var setTokenWhiteListResult = _nftMarketContract.SetTokenWhiteList(symbol, new StringList
            {
                Value = { "ELF", "USDT" }
            });
            setTokenWhiteListResult.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);

            setTokenWhiteListResult = _nftMarketContract.SetTokenWhiteList("CO12345678", new StringList
            {
                Value = { "ELF", "USDT" }
            });
            setTokenWhiteListResult.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            // SetRoyaltyResult.Error.ShouldContain("");

            setTokenWhiteListResult = _nftMarketContract.SetTokenWhiteList(symbol, new StringList
            {
                Value = { "ELF", "USDTTEST" }
            });
            // setTokenWhiteListResult.Status.ConvertTransactionResultStatus()
            //     .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            // SetRoyaltyResult.Error.ShouldContain("");

            _nftMarketContract.SetAccount(OtherAccount);
            setTokenWhiteListResult = _nftMarketContract.SetTokenWhiteList(symbol, new StringList
            {
                Value = { "ELF", "USDT" }
            });
            setTokenWhiteListResult.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            setTokenWhiteListResult.Error.ShouldContain("Only NFT Protocol Creator can set token white list.");

            // var whiteListAddressPriceList = _nftMarketContract.GetWhiteListAddressPriceList();
        }

        [TestMethod]
        public void SetCustomizeInfoErrorTest()
        {
           
        }


        private string CreateAndMint(long totalSupply, long mintAmount, long tokenId)
        {
            var createResult =
                _nftContract.ExecuteMethodWithResult(NftContractMethod.Create, new CreateInput
                {
                    NftType = NFTType.Collectables.ToString(),
                    ProtocolName = "CAT",
                    TotalSupply = totalSupply,
                    Creator = InitAccount.ConvertAddress(),
                    IsBurnable = true,
                    IssueChainId = _chainId,
                    Metadata = new Metadata(),
                    BaseUri = "aelf.com/nft/",
                    IsTokenIdReuse = true
                });
            createResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            var symbol = StringValue.Parser
                .ParseFrom(ByteArrayHelper.HexStringToByteArray(createResult.ReturnValue))
                .Value;

            Logger.Info($"symbol is {symbol}");
            var initBalanceBefore = GetBalanceTest(InitAccount, symbol, tokenId);
            Logger.Info($"initBalanceBefore is {initBalanceBefore}");

            var mintResult =
                _nftContract.ExecuteMethodWithResult(NftContractMethod.Mint, new MintInput
                {
                    Symbol = symbol,
                    Alias = "NFT_CO_CAT1",
                    Metadata = new Metadata(),
                    Quantity = mintAmount,
                    TokenId = tokenId
                });

            mintResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var initAccountAfterBalance = GetBalanceTest(InitAccount, symbol, tokenId);
            Logger.Info($"initAccountAfterBalance is {initAccountAfterBalance}");
            initAccountAfterBalance.ShouldBe(mintAmount + initBalanceBefore);
            return symbol;
        }

        private string CreateAndMintUnReuse(long totalSupply, long mintAmount, long tokenId)
        {
            var createResult =
                _nftContract.ExecuteMethodWithResult(NftContractMethod.Create, new CreateInput
                {
                    NftType = NFTType.Collectables.ToString(),
                    ProtocolName = "CAT",
                    TotalSupply = totalSupply,
                    Creator = InitAccount.ConvertAddress(),
                    IsBurnable = true,
                    IssueChainId = _chainId,
                    Metadata = new Metadata(),
                    BaseUri = "aelf.com/nft/",
                    IsTokenIdReuse = false
                });

            createResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            var symbol = StringValue.Parser
                .ParseFrom(ByteArrayHelper.HexStringToByteArray(createResult.ReturnValue))
                .Value;

            Logger.Info($"symbol is {symbol}");
            var initBalanceBefore = GetBalanceTest(InitAccount, symbol, tokenId);
            Logger.Info($"initBalanceBefore is {initBalanceBefore}");

            var mintResult =
                _nftContract.ExecuteMethodWithResult(NftContractMethod.Mint, new MintInput
                {
                    Symbol = symbol,
                    Alias = "NFT_CO_CAT1",
                    Metadata = new Metadata(),
                    Quantity = mintAmount,
                    TokenId = tokenId
                });

            mintResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var initAccountAfterBalance = GetBalanceTest(InitAccount, symbol, tokenId);
            Logger.Info($"initAccountAfterBalance is {initAccountAfterBalance}");
            initAccountAfterBalance.ShouldBe(mintAmount + initBalanceBefore);
            return symbol;
        }
        private long GetBalanceTest(string owner, string symbol, long tokenId)
        {
            var getBalance = _nftContract.GetBalance(owner, symbol, tokenId);
            Logger.Info($"owner of {symbol} is {getBalance.Owner}");
            Logger.Info($"TokenHash of {symbol} is {getBalance.TokenHash}");
            Logger.Info($"Balance of {symbol} is {getBalance.Balance}");
            return getBalance.Balance;
        }

        private void AddWhiteList()
        {
            var check = _tokenContract.IsInCreateTokenWhiteList(_nftContract.ContractAddress);
            if (check) return;

            var result = AuthorityManager.ExecuteTransactionWithAuthority(_tokenContract.ContractAddress,
                "AddAddressToCreateTokenWhiteList", _nftContract.Contract, InitAccount);

            result.Status.ShouldBe(TransactionResultStatus.Mined);
            check = _tokenContract.IsInCreateTokenWhiteList(_nftContract.ContractAddress);
            check.ShouldBeTrue();
        }

        private void ListWithFixedPrice(string symbol, int tokenId, long sellAmount,
            long fixedPrice,
            long whitePrice1, long whitePrice2, long whitePrice3, Timestamp startTime, Timestamp publicTime,
            int durationHours, string purchaseSymbol, bool isMerge)
        {
            // Initialize
            var initialize = _nftMarketContract.Initialize(
                _nftContract.ContractAddress,
                InitAccount,
                10,
                InitAccount,
                1000
            );

            initialize.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            
            // SetTokenWhiteList
            var result = _nftMarketContract.SetTokenWhiteList(symbol, new StringList
            {
                Value = { "USDT" }
            });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            // Approve
            var approve =
                _nftContract.Approve(_nftMarketContract.ContractAddress, symbol, tokenId, 1000000);

            approve.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            // ListWithFixedPrice
            _nftMarketContract.ListWithFixedPrice(
                symbol,
                tokenId,
                new Price
                {
                    Symbol = purchaseSymbol,
                    Amount = fixedPrice
                },
                sellAmount,
                new ListDuration
                {
                    StartTime = startTime,
                    PublicTime = publicTime,
                    DurationHours = durationHours
                },
                new WhiteListAddressPriceList
                {
                    Value =
                    {
                        new WhiteListAddressPrice
                        {
                            Address = WhiteListAddress1.ConvertAddress(),
                            Price = new Price
                            {
                                Symbol = purchaseSymbol,
                                Amount = whitePrice1
                            }
                        },
                        new WhiteListAddressPrice
                        {
                            Address = WhiteListAddress2.ConvertAddress(),
                            Price = new Price
                            {
                                Symbol = purchaseSymbol,
                                Amount = whitePrice2
                            },
                        },
                        new WhiteListAddressPrice
                        {
                            Address = WhiteListAddress3.ConvertAddress(),
                            Price = new Price
                            {
                                Symbol = purchaseSymbol,
                                Amount = whitePrice3
                            },
                        }
                    }
                },
                isMerge
            );
        }

        public void ListWithEnglistAuction(string symbol, int tokenId, long startingPrice, string purchaseSymbol,
            Timestamp startTime, Timestamp publicTime, int durationHours, long earnestMoney, string whiteSymbol,
            long whitePrice)
        {
            // Initialize
            var initialize = _nftMarketContract.Initialize(
                _nftContract.ContractAddress,
                InitAccount,
                100,
                InitAccount,
                10_00000000
            );

            initialize.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            // Approve
            var approve =
                _nftContract.Approve(_nftMarketContract.ContractAddress, symbol, tokenId, 1000000);

            approve.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            // ListWithEnglistAuction
            _nftMarketContract.ListWithEnglishAuction(
                symbol,
                tokenId,
                startingPrice,
                purchaseSymbol,
                new ListDuration
                {
                    StartTime = startTime,
                    PublicTime = publicTime,
                    DurationHours = durationHours
                },
                earnestMoney,
                new WhiteListAddressPriceList
                {
                    Value =
                    {
                        new WhiteListAddressPrice
                        {
                            Address = InitAccount.ConvertAddress(),
                            Price = new Price
                            {
                                Symbol = whiteSymbol,
                                Amount = whitePrice
                            }
                        }
                    }
                }
            );
        }

        public void ListWithDutchAuction(string symbol, int tokenId, long startingPrice, long endingPrice,
            string purchaseSymbol,
            Timestamp startTime, Timestamp publicTime, int durationHours, string whiteSymbol,
            long whitePrice)
        {
            // Initialize
            var initialize = _nftMarketContract.Initialize(
                _nftContract.ContractAddress,
                InitAccount,
                100,
                InitAccount,
                10_00000000
            );

            initialize.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            // Approve
            var approve =
                _nftContract.Approve(_nftMarketContract.ContractAddress, symbol, tokenId, 1000000);

            approve.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            // ListWithDutchAuction
            _nftMarketContract.ListWithDutchAuction(
                symbol,
                tokenId,
                startingPrice,
                endingPrice,
                purchaseSymbol,
                new ListDuration
                {
                    StartTime = startTime,
                    PublicTime = publicTime,
                    DurationHours = durationHours
                },
                new WhiteListAddressPriceList
                {
                    Value =
                    {
                        new WhiteListAddressPrice
                        {
                            Address = InitAccount.ConvertAddress(),
                            Price = new Price
                            {
                                Symbol = whiteSymbol,
                                Amount = whitePrice
                            }
                        }
                    }
                }
            );
        }

        private void CheckListedNftInfo(string symbol, int tokenId, string account,
            ListedNFTInfoList expectListedNftInfoList)
        {
            var listedNftInfo =
                _nftMarketContract.GetListedNFTInfoList(symbol, tokenId, account).Value[0];

            listedNftInfo.Symbol.ShouldBe(expectListedNftInfoList.Value[0].Symbol);
            listedNftInfo.TokenId.ShouldBe(expectListedNftInfoList.Value[0].TokenId);
            listedNftInfo.Owner.ShouldBe(expectListedNftInfoList.Value[0].Owner);
            listedNftInfo.Quantity.ShouldBe(expectListedNftInfoList.Value[0].Quantity);
            listedNftInfo.ListType.ShouldBe(expectListedNftInfoList.Value[0].ListType);
            listedNftInfo.Price.Symbol.ShouldBe(expectListedNftInfoList.Value[0].Price.Symbol);
            listedNftInfo.Price.Amount.ShouldBe(expectListedNftInfoList.Value[0].Price.Amount);
            listedNftInfo.Duration.StartTime.Seconds.ShouldBe(expectListedNftInfoList.Value[0].Duration.StartTime
                .Seconds);
            listedNftInfo.Duration.PublicTime.Seconds.ShouldBe(expectListedNftInfoList.Value[0].Duration.PublicTime
                .Seconds);
            listedNftInfo.Duration.DurationHours.ShouldBe(expectListedNftInfoList.Value[0].Duration.DurationHours);
        }

        private void CheckEnglishAuctionInfo(string symbol, int tokenId,
            EnglishAuctionInfo expectEnglishAuctionInfo)
        {
            var englishAuctionInfo =
                _nftMarketContract.GetEnglishAuctionInfo(symbol, tokenId);

            englishAuctionInfo.Symbol.ShouldBe(expectEnglishAuctionInfo.Symbol);
            englishAuctionInfo.TokenId.ShouldBe(expectEnglishAuctionInfo.TokenId);
            englishAuctionInfo.StartingPrice.ShouldBe(expectEnglishAuctionInfo.StartingPrice);
            englishAuctionInfo.PurchaseSymbol.ShouldBe(expectEnglishAuctionInfo.PurchaseSymbol);
            englishAuctionInfo.Duration.StartTime.ShouldBe(expectEnglishAuctionInfo.Duration.StartTime);
            englishAuctionInfo.Duration.PublicTime.ShouldBe(expectEnglishAuctionInfo.Duration.PublicTime);
            englishAuctionInfo.Duration.DurationHours.ShouldBe(expectEnglishAuctionInfo.Duration.DurationHours);
            englishAuctionInfo.Owner.ShouldBe(expectEnglishAuctionInfo.Owner);
            englishAuctionInfo.EarnestMoney.ShouldBe(expectEnglishAuctionInfo.EarnestMoney);
            englishAuctionInfo.DealPrice.ShouldBe(expectEnglishAuctionInfo.DealPrice);
            englishAuctionInfo.DealTo.ShouldBe(expectEnglishAuctionInfo.DealTo);
        }

        private void CheckDutchAuctionInfo(string symbol, int tokenId,
            DutchAuctionInfo expectEnglishAuctionInfo)
        {
            var dutchAuctionInfo =
                _nftMarketContract.GetDutchAuctionInfo(symbol, tokenId);

            dutchAuctionInfo.Symbol.ShouldBe(expectEnglishAuctionInfo.Symbol);
            dutchAuctionInfo.TokenId.ShouldBe(expectEnglishAuctionInfo.TokenId);
            dutchAuctionInfo.StartingPrice.ShouldBe(expectEnglishAuctionInfo.StartingPrice);
            dutchAuctionInfo.EndingPrice.ShouldBe(expectEnglishAuctionInfo.EndingPrice);
            dutchAuctionInfo.PurchaseSymbol.ShouldBe(expectEnglishAuctionInfo.PurchaseSymbol);
            dutchAuctionInfo.Duration.StartTime.ShouldBe(expectEnglishAuctionInfo.Duration.StartTime);
            dutchAuctionInfo.Duration.PublicTime.ShouldBe(expectEnglishAuctionInfo.Duration.PublicTime);
            dutchAuctionInfo.Duration.DurationHours.ShouldBe(expectEnglishAuctionInfo.Duration.DurationHours);
            dutchAuctionInfo.Owner.ShouldBe(expectEnglishAuctionInfo.Owner);
        }

        private void GetListedNftInfo(string symbol, int tokenId, string account)
        {
            var listedNftInfo =
                _nftMarketContract.GetListedNFTInfoList(symbol, tokenId, account).Value[0];

            Logger.Info($"listedNftInfo.Symbol is {listedNftInfo.Symbol}");
            Logger.Info($"listedNftInfo.TokenId is {listedNftInfo.TokenId}");
            Logger.Info($"listedNftInfo.Owner is {listedNftInfo.Owner}");
            Logger.Info($"listedNftInfo.Quantity is {listedNftInfo.Quantity}");
            Logger.Info($"listedNftInfo.ListType is {listedNftInfo.ListType}");
            Logger.Info($"listedNftInfo.Price.Symbol is {listedNftInfo.Price.Symbol}");
            Logger.Info($"listedNftInfo.Price.Amount is {listedNftInfo.Price.Amount}");
            Logger.Info($"listedNftInfo.Duration.StartTime is {listedNftInfo.Duration.StartTime}");
            Logger.Info($"listedNftInfo.Duration.PublicTime is {listedNftInfo.Duration.PublicTime}");
            Logger.Info($"listedNftInfo.Duration.DurationHours is {listedNftInfo.Duration.DurationHours}");
        }

        private void GetEnglishAuctionInfo(string symbol, int tokenId)
        {
            var englishAuctionInfo =
                _nftMarketContract.GetEnglishAuctionInfo(symbol, tokenId);

            Logger.Info($"englishAuctionInfo.Symbol is {englishAuctionInfo.Symbol}");
            Logger.Info($"englishAuctionInfo.TokenId is {englishAuctionInfo.TokenId}");
            Logger.Info($"englishAuctionInfo.StartingPrice is {englishAuctionInfo.StartingPrice}");
            Logger.Info($"englishAuctionInfo.PurchaseSymbol is {englishAuctionInfo.PurchaseSymbol}");
            Logger.Info($"englishAuctionInfo.Duration.StartTime is {englishAuctionInfo.Duration.StartTime.Seconds}");
            Logger.Info($"englishAuctionInfo.Duration.PublicTime is {englishAuctionInfo.Duration.PublicTime.Seconds}");
            Logger.Info($"englishAuctionInfo.Duration.DurationHours is {englishAuctionInfo.Duration.DurationHours}");
            Logger.Info($"englishAuctionInfo.Owner is {englishAuctionInfo.Owner}");
            Logger.Info($"englishAuctionInfo.EarnestMoney is {englishAuctionInfo.EarnestMoney}");
            Logger.Info($"englishAuctionInfo.DealPrice is {englishAuctionInfo.DealPrice}");
            Logger.Info($"englishAuctionInfo.DealTo is {englishAuctionInfo.DealTo}");
        }

        private void GetDutchAuctionInfo(string symbol, int tokenId)
        {
            var dutchAuctionInfo =
                _nftMarketContract.GetDutchAuctionInfo(symbol, tokenId);

            Logger.Info($"dutchAuctionInfo.Symbol is {dutchAuctionInfo.Symbol}");
            Logger.Info($"dutchAuctionInfo.TokenId is {dutchAuctionInfo.TokenId}");
            Logger.Info($"dutchAuctionInfo.StartingPrice is {dutchAuctionInfo.StartingPrice}");
            Logger.Info($"dutchAuctionInfo.EndingPrice is {dutchAuctionInfo.EndingPrice}");
            Logger.Info($"dutchAuctionInfo.PurchaseSymbol is {dutchAuctionInfo.PurchaseSymbol}");
            Logger.Info($"dutchAuctionInfo.Duration.StartTime is {dutchAuctionInfo.Duration.StartTime.Seconds}");
            Logger.Info($"dutchAuctionInfo.Duration.PublicTime is {dutchAuctionInfo.Duration.PublicTime.Seconds}");
            Logger.Info($"dutchAuctionInfo.Duration.DurationHours is {dutchAuctionInfo.Duration.DurationHours}");
            Logger.Info($"dutchAuctionInfo.Owner is {dutchAuctionInfo.Owner}");
        }

        [TestMethod]
        public void Transfer()
        {
            _genesisContract = GenesisContract.GetGenesisContract(NodeManager, InitAccount);
            _tokenContract = _genesisContract.GetTokenContract(InitAccount);
            var account1BalanceBefore = _tokenContract.GetUserBalance(InitAccount, "ELF");
            var targetBalanceBefore =
                _tokenContract.GetUserBalance("RctxRiJUytdyzqNZWqm1PpYGw2eNR83bknn17c1p2bbmVBLQy", "ELF");
            Logger.Info($"account1BalanceBefore is {account1BalanceBefore}");
            Logger.Info($"targetBalanceBefore is {targetBalanceBefore}");

            _tokenContract.TransferBalance(InitAccount, "RctxRiJUytdyzqNZWqm1PpYGw2eNR83bknn17c1p2bbmVBLQy",
                10000_00000000, "ELF");
            var account1BalanceAfter = _tokenContract.GetUserBalance(InitAccount, "ELF");
            var targetBalanceAfter =
                _tokenContract.GetUserBalance("RctxRiJUytdyzqNZWqm1PpYGw2eNR83bknn17c1p2bbmVBLQy", "ELF");
            Logger.Info($"account1BalanceAfter is {account1BalanceAfter}");
            Logger.Info($"targetBalanceAfter is {targetBalanceAfter}");

        }

        
        
        
        
        
        
        
        
        
        [TestMethod]
        public void NotlistedTest()
        {
            var tokenId = 1;
            var tokenId2 = 2;
            var buyAmount = 10;
            var purchaseSymbol = "ELF";
            var purchaseAmount = 6_00000000;
            var dealAmount = 2;
            var expireTime = DateTime.UtcNow.AddHours(30).ToTimestamp();
            var symbol = CreateAndMint(10000, 1000, tokenId);
            //var symbol1 = CreateAndMint(10000, 1000, tokenId2);
            
            // Initialize
            ContractInitialize();

            // 1.Not listed
            _nftMarketContract.SetAccount(OtherAccount);
            var makeOffer = _nftMarketContract.MakeOffer(
                symbol,
                tokenId, 
                InitAccount, 
                buyAmount,
                new Price
                {
                    Symbol = "ELF",     //sym1
                    Amount = purchaseAmount,
                    //TokenId = 1    //1
                },
                expireTime
            );
            
            makeOffer.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            
            var offerList = _nftMarketContract.GetOfferList(symbol, tokenId, OtherAccount);
            offerList.Value.Count.ShouldBe(1);
            offerList.Value[0].From.ShouldBe(OtherAccount.ConvertAddress());
            offerList.Value[0].To.ShouldBe(InitAccount.ConvertAddress());
            offerList.Value[0].Price.Symbol.ShouldBe(purchaseSymbol);
            offerList.Value[0].Price.TokenId.ShouldBe(0);
            offerList.Value[0].Price.Amount.ShouldBe(purchaseAmount);
            offerList.Value[0].Quantity.ShouldBe(buyAmount);
            offerList.Value[0].ExpireTime.ShouldBe(expireTime);

        }
        
        
        [TestMethod]
        //ListWithFixedPrice
        //WhiteListAddress
        public void ListWithFixedPrice()
        {
            var tokenId = 1;
            var totalAmount = 1000;
            var totalSupply = 10000;
            var mintAmount = 1000;
            var sellAmount = 100;
            var fixedPrice = 12_00000000;
            var whitePrice1 = 9_00000000;
            var whitePrice2 = 10_00000000;
            var whitePrice3 = 11_00000000;
            var buyAmount = 2;
            var expireTime = DateTime.UtcNow.AddSeconds(30).ToTimestamp();

            var startTime = DateTime.UtcNow.ToTimestamp();
            var publicTime = DateTime.UtcNow.AddSeconds(10).ToTimestamp();
            var durationHours = 48;
            var symbol = CreateAndMint(totalSupply, mintAmount, tokenId);
            Logger.Info($"startTime is {startTime.Seconds}");
            Logger.Info($"publicTime is {publicTime.Seconds}");
            var purchaseSymbol = "ELF";
            var isMerge = true;

            ListWithFixedPrice(symbol, tokenId, sellAmount, fixedPrice, whitePrice1, whitePrice2,
                whitePrice3, startTime, publicTime, durationHours, purchaseSymbol, isMerge);

             
                 // Approve
                 _nftContract.SetAccount(WhiteListAddress1);
                    var approve = _tokenContract.ApproveToken(WhiteListAddress1, _nftMarketContract.ContractAddress,
                        10000000000_00000000, "ELF");
                    approve.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
                   
                   //2.start_time   <   UtcNow    <public_time
                   _nftMarketContract.SetAccount(WhiteListAddress1);
                   var getBalanceBuyerStart = _nftContract.GetBalance(WhiteListAddress1, symbol, tokenId);
                   Logger.Info($"WhiteListAddress1Balance is {getBalanceBuyerStart.Balance}");
                 
                   var getBalancesellerStart = _nftContract.GetBalance(InitAccount, symbol, tokenId);
                   Logger.Info($"InitAccountBalance is {getBalancesellerStart}");
                  
                   var balanceStart = _tokenContract.GetUserBalance(serviceAddress, "ELF");
                   Logger.Info($"balance is {balanceStart}");
                   
                   _nftMarketContract.MakeOffer(
                       symbol,
                       tokenId,
                       InitAccount,
                       buyAmount,
                       new Price
                       {
                           Symbol = "ELF",
                           Amount = whitePrice1
                       },
                       expireTime
                   );
                   var getBalanceBuyerFinish = _nftContract.GetBalance(WhiteListAddress1, symbol, tokenId);
                   Logger.Info($"WhiteListAddress1Balance is {getBalanceBuyerFinish.Balance}");
                   getBalanceBuyerFinish.Balance.ShouldBe(getBalanceBuyerStart.Balance+1);
                   
                   var getBalancesellerFinish = _nftContract.GetBalance(InitAccount, symbol, tokenId);
                   Logger.Info($"InitAccountBalance is {getBalancesellerFinish.Balance}");
                   getBalancesellerFinish.Balance.ShouldBe(getBalancesellerStart.Balance - 1);
                   
                   var balanceFinish = _tokenContract.GetUserBalance(serviceAddress, "ELF");
                   Logger.Info($"balance is {balanceFinish}");
                   balanceFinish.ShouldBe(balanceStart +900000);
  
                   /*  
                     //3.enterAmount<whitePrice
                    var tokenId1 = 1;
                    var expireTime1 = DateTime.UtcNow.AddSeconds(10).ToTimestamp();
                    
                     // Approve
         
                      _nftContract.SetAccount(WhiteListAddress1);
                      var approve1 = _tokenContract.ApproveToken(WhiteListAddress1, _nftMarketContract.ContractAddress,
                          10000000000_00000000, "ELF");
                      approve1.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
                      
                    _nftMarketContract.SetAccount(WhiteListAddress1);
                    var result = _nftMarketContract.MakeOffer(
                        symbol, 
                        tokenId1,
                      InitAccount,
                      buyAmount,
                    new Price
                    {
                        Symbol = "ELF",
                        Amount = whitePrice1-10
                    },
                     expireTime1
                );
                result.Error.ShouldContain("Cannot find correct listed nft info.");
                
           
                  
                  
                
                //4.fixedPrice   <  whitePrice< = Enter amount
                
                _nftMarketContract.SetAccount(WhiteListAddress1);
                var getBalanceBuyerStart = _nftContract.GetBalance(WhiteListAddress1, symbol, tokenId);
                Logger.Info($"WhiteListAddress1Balance is {getBalanceBuyerStart.Balance}");
                var getBalancesellerStart = _nftContract.GetBalance(InitAccount, symbol, tokenId);
                Logger.Info($"InitAccountBalance is {getBalancesellerStart}");
                var balanceStart = _tokenContract.GetUserBalance(serviceAddress, "ELF");
                Logger.Info($"balance is {balanceStart}");
                
                _nftMarketContract.SetAccount(WhiteListAddress1);
                _nftMarketContract.MakeOffer(
                    symbol,
                    tokenId,
                    InitAccount,
                    buyAmount,
                    new Price
                    {
                        Symbol = "ELF",
                        Amount = whitePrice1 + 10
                    },
                    expireTime
                );
                
                var getBalanceBuyerFinish = _nftContract.GetBalance(WhiteListAddress1, symbol, tokenId);
                Logger.Info($"getBalanceBuyerFinish is {getBalanceBuyerFinish.Balance}");
                getBalanceBuyerFinish.Balance.ShouldBe(getBalanceBuyerStart.Balance+1);
                var getBalancesellerFinish = _nftContract.GetBalance(InitAccount, symbol, tokenId);
                Logger.Info($"getBalancesellerFinish is {getBalancesellerFinish.Balance}");
                getBalancesellerFinish.Balance.ShouldBe(getBalancesellerStart.Balance - 1);
                var balanceFinish = _tokenContract.GetUserBalance(serviceAddress, "ELF");
                Logger.Info($"balanceFinish is {balanceFinish}");
                balanceFinish.ShouldBe(balanceStart + 900000);
                
              
               
                //5.fixedPrice < = Enter amount  <  whitePrice
               // var tokenId = 1;
               // var expireTime = DateTime.UtcNow.AddSeconds(10).ToTimestamp();
                
               var balanceStart = _tokenContract.GetUserBalance(serviceAddress, "ELF");
               Logger.Info($"balance is {balanceStart}");
               var balanceStart1 = _tokenContract.GetUserBalance(WhiteListAddress1, "ELF");
               Logger.Info($"balance is {balanceStart1}");
               
                _nftMarketContract.SetAccount(WhiteListAddress1);
                var result = _nftMarketContract.MakeOffer(
                    symbol,
                    tokenId,
                    InitAccount,
                    buyAmount,
                    new Price
                    {
                        Symbol = "ELF",
                        Amount = fixedPrice + 1_00000000
                    },
                    expireTime
                );
                
                var balanceFinish = _tokenContract.GetUserBalance(serviceAddress, "ELF");
                Logger.Info($"balanceFinish is {balanceFinish}");
                balanceFinish.ShouldBe(balanceStart + 900000);
                var balanceFinish1 = _tokenContract.GetUserBalance(WhiteListAddress1, "ELF");
                Logger.Info($"balanceFinish is {balanceFinish1}");
          
                
                
                result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
                result.Error.ShouldContain("Offer price too low.");
    
                
                
              
            //6.Enter amount< whitePrice,public_time< UtcNow 
    
            //var tokenId4 = 4;
           // var expireTime = DateTime.UtcNow.AddSeconds(10).ToTimestamp();
           Thread.Sleep(60 * 1000);
           var balanceStart1 = _tokenContract.GetUserBalance(WhiteListAddress1, "ELF");
           Logger.Info($"balanceStart is {balanceStart1}");
           
           _nftMarketContract.SetAccount(WhiteListAddress1);
            var result = _nftMarketContract.MakeOffer(  
                symbol,
                tokenId,
                InitAccount,
                buyAmount,
                new Price
                {
                    Symbol = "ELF",
                    Amount = whitePrice1-3_00000000
                },
                expireTime
                 ); 
            var balanceFinish1 = _tokenContract.GetUserBalance(WhiteListAddress1, "ELF");
            Logger.Info($"balanceFinish is {balanceFinish1}");
            
            
             makeOffer.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
            result.Error.ShouldContain("Cannot find correct listed nft info.");
            
            
          
            
         
             //7.whitePrice < =  Enter amount < fixedPrice  ,  public_time  < UtcNow  
             //var tokenId5 = 5;
             //var dueTime5 = DateTime.UtcNow.AddSeconds(10).ToTimestamp();
             Thread.Sleep(60 * 1000);
             var balanceStart1 = _tokenContract.GetUserBalance(WhiteListAddress1, "ELF");
             Logger.Info($"balanceStart is {balanceStart1}");
             _nftMarketContract.SetAccount(WhiteListAddress1);
             var getBalanceBuyerStart = _nftContract.GetBalance(WhiteListAddress1, symbol, tokenId);
             Logger.Info($"WhiteListAddress1Balance is {getBalanceBuyerStart.Balance}");
             var getBalancesellerStart = _nftContract.GetBalance(InitAccount, symbol, tokenId);
             Logger.Info($"InitAccountBalance is {getBalancesellerStart}");
             var balanceStart = _tokenContract.GetUserBalance(serviceAddress, "ELF");
             Logger.Info($"balance is {balanceStart}");
             
             
             _nftMarketContract.SetAccount(WhiteListAddress1);
             var result = _nftMarketContract.MakeOffer(
                 symbol,
                 tokenId,
                 InitAccount,
                 buyAmount,
                 new Price
                 {
                     Symbol = "ELF",
                     Amount = whitePrice1 + 2_00000000
                 },
                 expireTime
             );
             var balanceFinish1 = _tokenContract.GetUserBalance(WhiteListAddress1, "ELF");
             Logger.Info($"balanceFinish is {balanceFinish1}");
             
             var getBalanceBuyerFinish = _nftContract.GetBalance(WhiteListAddress1, symbol, tokenId);
             Logger.Info($"getBalanceBuyerFinish is {getBalanceBuyerFinish.Balance}");
             getBalanceBuyerFinish.Balance.ShouldBe(getBalanceBuyerStart.Balance+1);
             var getBalancesellerFinish = _nftContract.GetBalance(InitAccount, symbol, tokenId);
             Logger.Info($"getBalancesellerFinish is {getBalancesellerFinish.Balance}");
             getBalancesellerFinish.Balance.ShouldBe(getBalancesellerStart.Balance - 1);
             var balanceFinish = _tokenContract.GetUserBalance(serviceAddress, "ELF");
             Logger.Info($"balanceFinish is {balanceFinish}");
             balanceFinish.ShouldBe(balanceStart + 900000);
             
             
             
           
              
          //8.whitePrice<  fixedPrice < = Enter amount,public_time  <  UtcNow  
          
          
         // var tokenId6 = 6;
         // var buyAmount6 =1;
         // var dueTime6 = DateTime.UtcNow.AddSeconds(10).ToTimestamp();
         Thread.Sleep(60 * 1000);
         var balanceStart1 = _tokenContract.GetUserBalance(WhiteListAddress1, "ELF");
         Logger.Info($"balanceStart is {balanceStart1}");
         _nftMarketContract.SetAccount(WhiteListAddress1);
         
         var getBalanceBuyerStart = _nftContract.GetBalance(WhiteListAddress1, symbol, tokenId);
         Logger.Info($"WhiteListAddress1Balance is {getBalanceBuyerStart.Balance}");
         var getBalancesellerStart = _nftContract.GetBalance(InitAccount, symbol, tokenId);
         Logger.Info($"InitAccountBalance is {getBalancesellerStart}");
         var balanceStart = _tokenContract.GetUserBalance(serviceAddress, "ELF");
         Logger.Info($"balance is {balanceStart}");
         
          _nftMarketContract.SetAccount(WhiteListAddress1);
          _nftMarketContract.MakeOffer(
              symbol,
              tokenId,
              InitAccount,
              buyAmount,
              new Price
              {
                  Symbol = "ELF",
                  Amount = fixedPrice+5_00000000
              },
              expireTime
          );
          var balanceFinish1 = _tokenContract.GetUserBalance(WhiteListAddress1, "ELF");
          Logger.Info($"balanceFinish is {balanceFinish1}");
          _nftMarketContract.SetAccount(WhiteListAddress1);
          var getBalanceBuyerFinish = _nftContract.GetBalance(WhiteListAddress1, symbol, tokenId);
          Logger.Info($"getBalanceBuyerFinish is {getBalanceBuyerFinish.Balance}");
          getBalanceBuyerFinish.Balance.ShouldBe(getBalanceBuyerStart.Balance+1);
          var getBalancesellerFinish = _nftContract.GetBalance(InitAccount, symbol, tokenId);
          Logger.Info($"getBalancesellerFinish is {getBalancesellerFinish.Balance}");
          getBalancesellerFinish.Balance.ShouldBe(getBalancesellerStart.Balance - 1);
          var balanceFinish = _tokenContract.GetUserBalance(serviceAddress, "ELF");
          Logger.Info($"balanceFinish is {balanceFinish}");
          balanceFinish.ShouldBe(balanceStart + 900000);
          
          
          
          
        //9。DurationHours <  UtcNow  ;    fixedPrice  < = Enter amount  < whitePrice, 
        
            //var tokenId7 = 7;
             //var dueTime7 = DateTime.UtcNow.AddSeconds(10).ToTimestamp();
             Thread.Sleep(60 * 1000);
             //var OfferList = _nftMarketContract.GetOfferList(symbol, tokenId, WhiteListAddress1);
            // OfferList.ShouldBeNull();
    
            _nftMarketContract.SetAccount(WhiteListAddress1);
            var result= _nftMarketContract.MakeOffer(
                  symbol,
                  tokenId,
                  InitAccount,
                buyAmount,
                new Price
                {
                     Symbol = "ELF",
                    Amount = whitePrice1-1_00000000
                },
                  expireTime
                  );
           
             result.Error.ShouldContain("Cannot find correct listed nft info.");
    
                 
             //9.2 DurationHours <  UtcNow  ;  fixedPrice  < = Enter amount  < whitePrice
             //var tokenId8 = 8;
             //var dueTime8 = DateTime.UtcNow.AddSeconds(10).ToTimestamp();
             
             var OfferList = _nftMarketContract.GetOfferList(symbol, tokenId, WhiteListAddress1);
             OfferList.ShouldBe(new OfferList());
             _nftMarketContract.SetAccount(WhiteListAddress1);
             _nftMarketContract.MakeOffer(
                 symbol,
                 tokenId,
                 InitAccount,
                 buyAmount,
                 new Price
                 {
                     Symbol = "ELF",
                     Amount = whitePrice1+10_00000000
                 },
                 expireTime
             );
             
             OfferList = _nftMarketContract.GetOfferList(symbol, tokenId, WhiteListAddress1);
             OfferList.Value.Count.ShouldBe(1);
             OfferList.Value.First().From.ShouldBe(OtherAccount.ConvertAddress());
             OfferList.Value.First().To.ShouldBe(InitAccount.ConvertAddress());
             OfferList.Value.First().Price.Symbol.ShouldBe("ELF");
             OfferList.Value.First().Price.Amount.ShouldBe(whitePrice1);
             OfferList.Value.First().Quantity.ShouldBe(buyAmount);
             OfferList.Value.First().ExpireTime.ShouldBe(expireTime);
          
             
              
             //9.3.owner——start_time < UtcNow <public_time,fixedPrice < = whitePrice
            //var tokenId9 = 9;
           //var dueTime9 = DateTime.UtcNow.AddSeconds(10).ToTimestamp();
           
           // Approve
           _nftContract.SetAccount(InitAccount);
           var approve = _tokenContract.ApproveToken(InitAccount, _nftMarketContract.ContractAddress,
               10000000000_00000000, "ELF");
           approve.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
           
           _nftMarketContract.SetAccount(InitAccount);
           var result = _nftMarketContract.MakeOffer(
               symbol,
               tokenId,
               InitAccount,
               buyAmount,
               new Price
               {
                   Symbol = "ELF",
                   Amount = fixedPrice+10
               },
               expireTime
           );
           result.Error.ShouldContain("Origin owner cannot be sender himself.");
  
           
           */




            //users
            // Approve
            _nftContract.SetAccount(OtherAccount);
            var approve1 = _tokenContract.ApproveToken(OtherAccount, _nftMarketContract.ContractAddress,
                10000000000_00000000, "ELF");
            approve1.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            /* 
             //10.start_time < UtcNow <public_time,fixedPrice < = Enter amount
 
             //var tokenId9 = 9;
             //var dueTime9 = DateTime.UtcNow.AddSeconds(10).ToTimestamp();
             
             _nftMarketContract.SetAccount(OtherAccount);
             var result = _nftMarketContract.MakeOffer(
                 symbol,
                 tokenId,
                 InitAccount,
                 buyAmount,
                 new Price
                 {
                     Symbol = "ELF",
                     Amount = fixedPrice
                 },
                 expireTime
             );
             
             result.Error.ShouldContain("Sender is not in the white list, please need until");
         
 
 
            //11.tart_time < UtcNow <public_time, Enter amount < fixedPrice 
 
            _nftMarketContract.SetAccount(OtherAccount);
            var result = _nftMarketContract.MakeOffer(
                symbol,
                tokenId,
                InitAccount,
                buyAmount,
                new Price
                {
                    Symbol = "ELF",
                    Amount = fixedPrice - 1_00000000
                },
                expireTime
            );
 
            result.Error.ShouldContain("Cannot find valid listed nft info.");
            
 
 
            //12.whitePrice < = Enter amount < fixedPrice   ;   public_time  <  UtcNow  
            //var tokenId10 = 10;
            //var dueTime10 = DateTime.UtcNow.AddSeconds(10).ToTimestamp();
            
            var OfferList = _nftMarketContract.GetOfferList(symbol, tokenId, InitAccount);
            OfferList.ShouldBe(new OfferList());
            
            Thread.Sleep(70 * 1000);
            _nftMarketContract.SetAccount(OtherAccount);
            var result = _nftMarketContract.MakeOffer(
                symbol,
                tokenId,
                InitAccount,
                buyAmount,
                new Price
                {
                    Symbol = "ELF",
                    Amount = fixedPrice - 1_00000000
                },
                expireTime
            );
            result.Error.ShouldContain("Cannot find valid listed nft info.");           
        
            
                      
            //13.whitePrice < = Enter amount < fixedPrice   ;   public_time  <  UtcNow  
            //var tokenId11 = 11;
            //var dueTime11 = DateTime.UtcNow.AddSeconds(10).ToTimestamp();
 
            Thread.Sleep(60 * 1000);
            
            var balanceStart1 = _tokenContract.GetUserBalance(OtherAccount, "ELF");
            Logger.Info($"balanceStart is {balanceStart1}");
            _nftMarketContract.SetAccount(OtherAccount);
        
            var getBalanceBuyerStart = _nftContract.GetBalance(OtherAccount, symbol, tokenId);
            Logger.Info($"WhiteListAddress1Balance is {getBalanceBuyerStart.Balance}");
            
            var getBalancesellerStart = _nftContract.GetBalance(InitAccount, symbol, tokenId);
            Logger.Info($"InitAccountBalance is {getBalancesellerStart}");
            
            var balanceStart = _tokenContract.GetUserBalance(serviceAddress, "ELF");
            Logger.Info($"balance is {balanceStart}");
            
            _nftMarketContract.SetAccount(OtherAccount);
            _nftMarketContract.MakeOffer(
                symbol,
                tokenId,
                InitAccount,
                buyAmount,
                new Price
                {
                    Symbol = "ELF",
                    Amount = fixedPrice +10_00000000
                },
                expireTime
            );
           
            
            var balanceFinish1 = _tokenContract.GetUserBalance(OtherAccount, "ELF");
            Logger.Info($"balanceFinish is {balanceFinish1}");
            _nftMarketContract.SetAccount(OtherAccount);
            
            var getBalanceBuyerFinish = _nftContract.GetBalance(OtherAccount, symbol, tokenId);
            Logger.Info($"getBalanceBuyerFinish is {getBalanceBuyerFinish.Balance}");
            getBalanceBuyerFinish.Balance.ShouldBe(getBalanceBuyerStart.Balance+1);
            
            var getBalancesellerFinish = _nftContract.GetBalance(InitAccount, symbol, tokenId);
            Logger.Info($"getBalancesellerFinish is {getBalancesellerFinish.Balance}");
            getBalancesellerFinish.Balance.ShouldBe(getBalancesellerStart.Balance - 1);
            
            var balanceFinish = _tokenContract.GetUserBalance(serviceAddress, "ELF");
            Logger.Info($"balanceFinish is {balanceFinish}");
            balanceFinish.ShouldBe(balanceStart+ 2200000 );
            */
        
            
            /*
            //14.whitePrice < = Enter amount < fixedPrice   ;   DurationHours <  UtcNow  
            //var tokenId12 = 10;
            //var dueTime12 = DateTime.UtcNow.AddSeconds(10).ToTimestamp();

            var OfferList = _nftMarketContract.GetOfferList(symbol, tokenId, OtherAccount);
            OfferList.ShouldBe(new OfferList());
            Thread.Sleep(60 * 1000);

            _nftMarketContract.SetAccount(OtherAccount);
            _nftMarketContract.MakeOffer(
                symbol,
                tokenId,
                InitAccount,
                buyAmount,
                new Price
                {
                    Symbol = "ELF",
                    Amount = fixedPrice 
                },
                expireTime
            );                   
            OfferList = _nftMarketContract.GetOfferList(symbol, tokenId, OtherAccount);
            OfferList.Value.Count.ShouldBe(1);
            OfferList.Value.First().From.ShouldBe(OtherAccount.ConvertAddress());
            OfferList.Value.First().To.ShouldBe(InitAccount.ConvertAddress());
            OfferList.Value.First().Price.Symbol.ShouldBe("ELF");
            OfferList.Value.First().Price.Amount.ShouldBe(fixedPrice + 10_00000000);
            OfferList.Value.First().Quantity.ShouldBe(buyAmount);
            OfferList.Value.First().ExpireTime.ShouldBe(expireTime);
            */


        }
        [TestMethod]
        //ListWithEnglistAuction
        public void MakeOfferListWithEnglishTest()
        {
            var tokenId = 1;
            var totalAmount = 1000;
            var totalSupply = 10000;
            var mintAmount = 1000;
            var sellAmount = 100;
            var startingPrice = 12_00000000;
            var earnestMoney = 11_00000000;
            var whitePrice1 = 9_00000000;
            var buyAmount = 1;
            var whiteSymbol = "ELF";
            
            var expireTime = DateTime.UtcNow.AddHours(1).ToTimestamp();

            
            var startTime = DateTime.UtcNow.ToTimestamp();
            var publicTime = DateTime.UtcNow.AddSeconds(30).ToTimestamp();
            var durationHours = 1;
            var symbol = CreateAndMint(totalSupply, mintAmount, tokenId);
            //var  symbol= "CO256793574";
            Logger.Info($"startTime is {startTime.Seconds}");
            Logger.Info($"publicTime is {publicTime.Seconds}");
            var purchaseSymbol = "ELF";
            var isMerge = true;

            ListWithEnglistAuction(symbol, tokenId, startingPrice, purchaseSymbol,
                startTime, publicTime, durationHours, earnestMoney, whiteSymbol, whitePrice1);

            // Approve
            _nftContract.SetAccount(OtherAccount);
            var approve = _tokenContract.ApproveToken(OtherAccount, _nftMarketContract.ContractAddress,
                10000000000_00000000, "ELF");
            approve.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            
            _nftContract.SetAccount(OtherAccount1);
            var approve1 = _tokenContract.ApproveToken(OtherAccount1, _nftMarketContract.ContractAddress,
                10000000000_00000000, "ELF");
            approve1.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            
            _nftContract.SetAccount(OtherAccount2);
            var approve2 = _tokenContract.ApproveToken(OtherAccount2, _nftMarketContract.ContractAddress,
                10000000000_00000000, "ELF");
            approve2.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined); 
            _nftContract.SetAccount(OtherAccount3);
            var approve3 = _tokenContract.ApproveToken(OtherAccount3, _nftMarketContract.ContractAddress,
                10000000000_00000000, "ELF");
            approve3.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            
           //15. startingPrice  < =   Enter amount ,first user first purchase
           Thread.Sleep(60 * 1000);
           var BidList = _nftMarketContract.GetBidList(symbol, tokenId, OtherAccount);
           BidList.ShouldBe(new BidList());
           var offerList = _nftMarketContract.GetOfferList(symbol, tokenId, OtherAccount);
           offerList.ShouldBe(new OfferList());
           var balanceStart = _tokenContract.GetUserBalance(OtherAccount, "ELF");
           Logger.Info($"balance is {balanceStart}");


           _nftMarketContract.SetAccount(OtherAccount);
           _nftMarketContract.MakeOffer(
               symbol,
               tokenId,
               InitAccount,
               buyAmount,
               new Price
               {
                   Symbol = "ELF",
                   Amount = startingPrice 
               },
               expireTime
           );
           var balanceFinish = _tokenContract.GetUserBalance(OtherAccount, "ELF");
           Logger.Info($"balance is {balanceFinish}");
           
           BidList = _nftMarketContract.GetBidList(symbol, tokenId, OtherAccount);
           BidList.Value.Count.ShouldBe(1);
           BidList.Value.First().From.ShouldBe(OtherAccount.ConvertAddress());
           BidList.Value.First().To.ShouldBe(InitAccount.ConvertAddress());
           BidList.Value.First().Price.Symbol.ShouldBe("ELF");
           BidList.Value.First().Price.Amount.ShouldBe(startingPrice);
           
           offerList = _nftMarketContract.GetOfferList(symbol, tokenId, OtherAccount);
           offerList.Value.Count.ShouldBe(0);
          
         
           
           
           
          /*
          //17. bid  <  Enter amount ,Other user first purchase

          _nftContract.SetAccount(OtherAccount1);
          Thread.Sleep(30 * 1000);
          var BidList1 = _nftMarketContract.GetBidList(symbol, tokenId, OtherAccount1);
          var offerList1 = _nftMarketContract.GetOfferList(symbol, tokenId, OtherAccount1);

          _nftMarketContract.SetAccount(OtherAccount1);
          _nftMarketContract.MakeOffer(
              symbol,
              tokenId,
              InitAccount,
              buyAmount,
              new Price
              {
                  Symbol = "ELF",
                  Amount =  15_00000000
              },
              expireTime
          ); 
          BidList1 = _nftMarketContract.GetBidList(symbol, tokenId, OtherAccount1);
          BidList1.Value.Count.ShouldBe(1);
          BidList1.Value[1].From.ShouldBe(OtherAccount1.ConvertAddress());
          BidList1.Value[1].To.ShouldBe(InitAccount.ConvertAddress());
          BidList1.Value[1].Price.Symbol.ShouldBe("ELF");
          BidList1.Value[1].Price.Amount.ShouldBe(15_00000000);
          BidList1.Value[1].Quantity.ShouldBe(0);
           
          offerList1 = _nftMarketContract.GetOfferList(symbol, tokenId, OtherAccount1);
          offerList1.Value.Count.ShouldBe(0);
           
          
          
                     
          
            
            //18.  Enter amount<=bid   ,Other user first purchase
            Thread.Sleep(30 * 1000);
            var BidList2 = _nftMarketContract.GetBidList(symbol, tokenId, OtherAccount2);
            var offerList2 = _nftMarketContract.GetOfferList(symbol, tokenId, OtherAccount2);
            _nftMarketContract.SetAccount(OtherAccount2);
            _nftMarketContract.MakeOffer(
                symbol,
                tokenId,
                InitAccount,
                buyAmount,
                new Price
                {
                    Symbol = "ELF",
                    Amount = 14_00000000
                },
                expireTime
            );
            BidList2 = _nftMarketContract.GetBidList(symbol, tokenId, OtherAccount2);
            BidList2.Value.Count.ShouldBe(0);
            
            
            offerList2 = _nftMarketContract.GetOfferList(symbol, tokenId, OtherAccount2);
            offerList2.Value.Count.ShouldBe(1);
            offerList2.Value.First().From.ShouldBe(OtherAccount2.ConvertAddress());
            offerList2.Value.First().To.ShouldBe(InitAccount.ConvertAddress());
            offerList2.Value.First().Price.Symbol.ShouldBe("ELF");
            offerList2.Value.First().Price.Amount.ShouldBe(14_00000000);
            offerList2.Value.First().Quantity.ShouldBe(0);
            offerList2.Value.First().ExpireTime.ShouldBe(expireTime);
            
            
           //19.  Enter amount<  startingPrice ,Other user first purchase
           Thread.Sleep(60 * 1000);
           var BidList4 = _nftMarketContract.GetBidList(symbol, tokenId, OtherAccount3);
           var offerList4 = _nftMarketContract.GetOfferList(symbol, tokenId, OtherAccount3);
           _nftMarketContract.SetAccount(OtherAccount3);
           _nftMarketContract.MakeOffer(
               symbol,
               tokenId,
               InitAccount,
               buyAmount,
               new Price
               {
                   Symbol = "ELF",
                   Amount = 11_00000000
               },
               expireTime
           );
           BidList4 = _nftMarketContract.GetBidList(symbol, tokenId, OtherAccount3);
           BidList4.Value.Count.ShouldBe(0);
           
           offerList4 = _nftMarketContract.GetOfferList(symbol, tokenId, OtherAccount3);
           offerList4.Value.Count.ShouldBe(1);
           offerList4.Value.First().From.ShouldBe(OtherAccount3.ConvertAddress());
           offerList4.Value.First().To.ShouldBe(InitAccount.ConvertAddress());
           offerList4.Value.First().Price.Symbol.ShouldBe("ELF");
           offerList4.Value.First().Price.Amount.ShouldBe(11_00000000);
           offerList4.Value.First().Quantity.ShouldBe(0);
           offerList4.Value.First().ExpireTime.ShouldBe(expireTime);
          
           //20.User enters bid for the second time

           Thread.Sleep(60 * 1000);
           var BidList3 = _nftMarketContract.GetBidList(symbol, tokenId, OtherAccount);
           var offerList3 = _nftMarketContract.GetOfferList(symbol, tokenId, OtherAccount);
           var balanceStart3 = _tokenContract.GetUserBalance(OtherAccount, "ELF");
           Logger.Info($"balance is {balanceStart3}");


           _nftMarketContract.SetAccount(OtherAccount);
           _nftMarketContract.MakeOffer(
               symbol,
               tokenId,
               InitAccount,
               buyAmount,
               new Price
               {
                   Symbol = "ELF",
                   Amount = startingPrice + 20_00000000
               },
               expireTime
           );
           var balanceFinish3 = _tokenContract.GetUserBalance(OtherAccount, "ELF");
           Logger.Info($"balance is {balanceFinish3}");
          
           BidList3 = _nftMarketContract.GetBidList(symbol, tokenId, InitAccount);
           BidList3.Value.Count.ShouldBe(2);
           BidList3.Value[0].From.ShouldBe(OtherAccount.ConvertAddress());
           BidList3.Value[0].To.ShouldBe(InitAccount.ConvertAddress());
           BidList3.Value[0].Price.Symbol.ShouldBe("ELF");
           BidList3.Value[0].Price.Amount.ShouldBe(startingPrice+ 20_00000000);
           BidList3.Value.First().Quantity.ShouldBe(0);*/
           
          
        }
        
        
        [TestMethod]
        //ListWithEnglistAuction
        public void MakeOfferListWithEnglishTest1()
        {
            var tokenId = 1;
            var totalAmount = 1000;
            var totalSupply = 10000;
            var mintAmount = 1000;
            var sellAmount = 100;
            var startingPrice = 12_00000000;
            var earnestMoney = 11_00000000;
            var whitePrice1 = 9_00000000;
            var buyAmount = 1;
            var whiteSymbol = "ELF";

            var expireTime = DateTime.UtcNow.AddHours(1).ToTimestamp();


            var startTime = DateTime.UtcNow.ToTimestamp();
            var publicTime = DateTime.UtcNow.AddHours(1).ToTimestamp();
            var durationHours = 48;
            var symbol = CreateAndMint(totalSupply, mintAmount, tokenId);
            //var  symbol= "CO256793574";
            Logger.Info($"startTime is {startTime.Seconds}");
            Logger.Info($"publicTime is {publicTime.Seconds}");
            var purchaseSymbol = "ELF";
            var isMerge = true;

            ListWithEnglistAuction(symbol, tokenId, startingPrice, purchaseSymbol,
                startTime, publicTime, durationHours, earnestMoney, whiteSymbol, whitePrice1);

            // Approve
            _nftContract.SetAccount(OtherAccount);
            var approve = _tokenContract.ApproveToken(OtherAccount, _nftMarketContract.ContractAddress,
                10000000000_00000000, "ELF");
            approve.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            _nftContract.SetAccount(OtherAccount1);
            var approve1 = _tokenContract.ApproveToken(OtherAccount1, _nftMarketContract.ContractAddress,
                10000000000_00000000, "ELF");
            approve1.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            
           
           //16. startingPrice  >  enterAmount,first user first purchase
           var BidList = _nftMarketContract.GetBidList(symbol, tokenId, OtherAccount);
           BidList.ShouldBe(new BidList());
           var offerList = _nftMarketContract.GetOfferList(symbol, tokenId, OtherAccount);
           offerList.ShouldBe(new OfferList());

           Thread.Sleep(10 * 1000);
           _nftMarketContract.SetAccount(OtherAccount);
           _nftMarketContract.MakeOffer(
               symbol,
               tokenId,
               InitAccount,
               buyAmount,
               new Price
               {
                   Symbol = "ELF",
                   Amount = startingPrice-8_00000000
               },
               expireTime
           ); 
      
           Thread.Sleep(60 * 1000);
           
           BidList = _nftMarketContract.GetBidList(symbol, tokenId, OtherAccount);
           BidList.Value.Count.ShouldBe(0);

           offerList = _nftMarketContract.GetOfferList(symbol, tokenId, OtherAccount);
           offerList.Value.Count.ShouldBe(1);
           offerList.Value.First().From.ShouldBe(OtherAccount.ConvertAddress());
           offerList.Value.First().To.ShouldBe(InitAccount.ConvertAddress());
           offerList.Value.First().Price.Symbol.ShouldBe("ELF");
           offerList.Value.First().Price.Amount.ShouldBe(startingPrice-8_00000000 );
           offerList.Value[0].Quantity.ShouldBe(1);
           offerList.Value[0].ExpireTime.ShouldBe(expireTime);
          
           
           
          
            //20.1.The user's first purchase is greater than bid, the second purchase is less than bid, and the third purchase is greater than bid
            var BidList1 = _nftMarketContract.GetBidList(symbol, tokenId, OtherAccount1);
            BidList1.ShouldBe(new BidList());
            var offerList1 = _nftMarketContract.GetOfferList(symbol, tokenId, OtherAccount1);
 
            _nftMarketContract.SetAccount(OtherAccount1);
            _nftMarketContract.MakeOffer(
                symbol,
                tokenId,
                InitAccount,
                buyAmount,
                new Price
                {
                    Symbol = "ELF",
                    Amount = startingPrice 
                },
                expireTime
            ); 
            
            BidList1 = _nftMarketContract.GetBidList(symbol, tokenId, OtherAccount1);
            BidList1.Value.Count.ShouldBe(1);
            BidList1.Value[0].From.ShouldBe(OtherAccount1.ConvertAddress());
            BidList1.Value[0].To.ShouldBe(InitAccount.ConvertAddress());
            BidList1.Value[0].Price.Symbol.ShouldBe("ELF");
            BidList1.Value[0].Price.Amount.ShouldBe(startingPrice);
            
            _nftMarketContract.MakeOffer(
                symbol,
                tokenId,
                InitAccount,
                buyAmount,
                new Price
                {
                    Symbol = "ELF",
                    Amount = startingPrice-5_00000000
                },
                expireTime
            ); 
            
            offerList1 = _nftMarketContract.GetOfferList(symbol, tokenId, OtherAccount1);
            offerList1.Value.Count.ShouldBe(1);
            offerList1.Value[1].From.ShouldBe(OtherAccount1.ConvertAddress());
            offerList1.Value[1].To.ShouldBe(InitAccount.ConvertAddress());
            offerList1.Value[1].Price.Symbol.ShouldBe("ELF");
            offerList1.Value[1].Price.Amount.ShouldBe(startingPrice-5_00000000);
            offerList1.Value[1].Quantity.ShouldBe(0);
            offerList1.Value[1].ExpireTime.ShouldBe(expireTime);
 
            _nftMarketContract.MakeOffer(
                symbol,
                tokenId,
                InitAccount,
                buyAmount,
                new Price
                {
                    Symbol = "ELF",
                    Amount = startingPrice+5_00000000
                },
                expireTime
            ); 
            offerList1 = _nftMarketContract.GetOfferList(symbol, tokenId, OtherAccount1);
            offerList1.Value.Count.ShouldBe(1);
            offerList1.Value[1].From.ShouldBe(OtherAccount1.ConvertAddress());
            offerList1.Value[1].To.ShouldBe(InitAccount.ConvertAddress());
            offerList1.Value[1].Price.Symbol.ShouldBe("ELF");
            offerList1.Value[1].Price.Amount.ShouldBe(startingPrice-5_00000000);
            offerList1.Value[1].Quantity.ShouldBe(0);
            offerList.Value[1].ExpireTime.ShouldBe(expireTime);
            
            BidList1 = _nftMarketContract.GetBidList(symbol, tokenId, OtherAccount1);
            BidList1.Value.Count.ShouldBe(1);
            BidList1.Value[0].From.ShouldBe(OtherAccount1.ConvertAddress());
            BidList1.Value[0].To.ShouldBe(InitAccount.ConvertAddress());
            BidList1.Value[0].Price.Symbol.ShouldBe("ELF");
            BidList1.Value[0].Price.Amount.ShouldBe(startingPrice+5_00000000);
            
 
 
        
           //21.1.offerto address tobuy
               // Approve
               _nftContract.SetAccount(InitAccount);
               var approve3 = _tokenContract.ApproveToken(InitAccount, _nftMarketContract.ContractAddress,
                   10000000000_00000000, "ELF");
               approve3.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
               
               _nftMarketContract.SetAccount(InitAccount);
               var result = _nftMarketContract.MakeOffer(
                   symbol,
                   tokenId,
                   InitAccount,
                   buyAmount,
                   new Price
                   {
                       Symbol = "ELF",
                       Amount = startingPrice
                   },
                   expireTime
               ); 
               
               result.Error.ShouldContain("Origin owner cannot be sender himself.");
               

        }




        [TestMethod]
        //ListWithEnglistAuction
        public void MakeOfferListWithEnglishTest2()
        {
            var tokenId = 1;
            var totalAmount = 1000;
            var totalSupply = 10000;
            var mintAmount = 1000;
            var sellAmount = 100;
            var startingPrice = 12_00000000;
            var earnestMoney = 11_00000000;
            var whitePrice1 = 9_00000000;
            var buyAmount = 1;
            var whiteSymbol = "ELF";

            var expireTime = DateTime.UtcNow.AddHours(1).ToTimestamp();


            var startTime = DateTime.UtcNow.ToTimestamp();
            var publicTime = DateTime.UtcNow.AddHours(1).ToTimestamp();
            var durationHours = 2;
           //var symbol = CreateAndMint(totalSupply, mintAmount, tokenId);
            var  symbol= "CO644101375";
            Logger.Info($"startTime is {startTime.Seconds}");
            Logger.Info($"publicTime is {publicTime.Seconds}");
            var purchaseSymbol = "ELF";
            var isMerge = true;
            

            GetEnglishAuctionInfo(symbol, tokenId);
            
            // Approve
            _nftContract.SetAccount(OtherAccount);
            var approve = _tokenContract.ApproveToken(OtherAccount, _nftMarketContract.ContractAddress,
                10000000000_00000000, "ELF");
            approve.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            //
            //
            //symbol:

            var offerList = _nftMarketContract.GetBidList(symbol, tokenId, InitAccount);
            offerList.ShouldBe(new BidList());
            
            Thread.Sleep(10 * 1000);
            _nftMarketContract.SetAccount(OtherAccount);
            _nftMarketContract.MakeOffer(
                symbol,
                tokenId,
                InitAccount,
                buyAmount,
                new Price
                {
                    Symbol = "ELF",
                    Amount = startingPrice+50_00000000
                },
                expireTime
            ); 
            
            
            offerList = _nftMarketContract.GetBidList(symbol, tokenId, OtherAccount);
            offerList.Value.Count.ShouldBe(1);
            offerList.Value[0].From.ShouldBe(OtherAccount.ConvertAddress());
            offerList.Value[0].To.ShouldBe(InitAccount.ConvertAddress());
            offerList.Value[0].Price.Symbol.ShouldBe("ELF");
            offerList.Value[0].Price.Amount.ShouldBe(startingPrice+50_00000000);
            offerList.Value.First().ExpireTime.ShouldBe(expireTime);

        }


        
        [TestMethod]
        //ListWithDutchAuction
        public void MakeOfferListWithDutchAuctionTest()
        {
            var tokenId = 1;
            var totalAmount = 1000;
            var totalSupply = 10000;
            var mintAmount = 1000;
            var sellAmount = 100;
            var startingPrice = 12_00000000;
            var endingPrice = 5_00000000;
            var whitePrice1 = 9_00000000;
            var buyAmount = 1;
            var whiteSymbol = "ELF";
            
            var expireTime = DateTime.UtcNow.AddHours(1).ToTimestamp();
            
            var startTime = DateTime.UtcNow.ToTimestamp();
            var publicTime = DateTime.UtcNow.AddHours(1).ToTimestamp();
            var durationHours = 2;
            var symbol = CreateAndMint(totalSupply, mintAmount, tokenId);
            Logger.Info($"startTime is {startTime.Seconds}");
            Logger.Info($"publicTime is {publicTime.Seconds}");
            var purchaseSymbol = "ELF";
            var isMerge = true;

            ListWithDutchAuction(symbol, tokenId, startingPrice, endingPrice,purchaseSymbol,
                startTime, publicTime, durationHours,  whiteSymbol, whitePrice1);
            
            // Approve
            _nftContract.SetAccount(InitAccount);
            var approve = _tokenContract.ApproveToken(InitAccount, _nftMarketContract.ContractAddress,
                10000000000_00000000, "ELF");
            approve.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            _nftContract.SetAccount(OtherAccount);
            var approve1 = _tokenContract.ApproveToken(OtherAccount, _nftMarketContract.ContractAddress,
                10000000000_00000000, "ELF");
            approve1.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            
            
            //22.1 . offerto address tobuy
            Thread.Sleep(10 * 1000);
           _nftMarketContract.SetAccount(InitAccount);
           var result =  _nftMarketContract.MakeOffer(
               symbol,
               tokenId,
               InitAccount,
               buyAmount,
               new Price
               {
                   Symbol = "ELF",
                   Amount = startingPrice
               },
               expireTime
           ); 
           
           result.Error.ShouldContain("Origin owner cannot be sender himself.");

      
           //22.2 . endingPrice <=  startingPrice < = Enter amount
           
           var balanceStart1 = _tokenContract.GetUserBalance(OtherAccount, "ELF");
           Logger.Info($"balanceOtherAccountStart is {balanceStart1}");
           var balanceStart = _tokenContract.GetUserBalance(serviceAddress, "ELF");
           Logger.Info($"balanceserviceAddressStart is {balanceStart}");
           
           var getBalanceBuyerStart = _nftContract.GetBalance(OtherAccount, symbol, tokenId);
           Logger.Info($"WhiteListAddress1Balance is {getBalanceBuyerStart.Balance}");
           var getBalancesellerStart = _nftContract.GetBalance(InitAccount, symbol, tokenId);
           Logger.Info($"InitAccountBalance is {getBalancesellerStart}");
          
           
           Thread.Sleep(10 * 1000);
            _nftMarketContract.SetAccount(OtherAccount);
            _nftMarketContract.MakeOffer(
               symbol,
               tokenId,
               InitAccount,
               buyAmount,
               new Price
               {
                   Symbol = "ELF",
                   Amount = startingPrice+1_00000000
               },
               expireTime
           ); 
           
            var balanceFinish1 = _tokenContract.GetUserBalance(OtherAccount, "ELF");
            Logger.Info($"balanceOtherAccountFinish is {balanceFinish1}");
            var balanceFinish = _tokenContract.GetUserBalance(serviceAddress, "ELF");
            Logger.Info($"balanceserviceAddressFinish is {balanceFinish}");
            
            var getBalanceBuyerFinish = _nftContract.GetBalance(OtherAccount, symbol, tokenId);
            Logger.Info($"getBalanceBuyerFinish is {getBalanceBuyerFinish.Balance}");
            //getBalanceBuyerFinish.Balance.ShouldBe(getBalanceBuyerStart.Balance+1);
            
            var getBalancesellerFinish = _nftContract.GetBalance(InitAccount, symbol, tokenId);
            Logger.Info($"getBalancesellerFinish is {getBalancesellerFinish.Balance}");
            //getBalancesellerFinish.Balance.ShouldBe(getBalancesellerStart.Balance - 1);

            var offerList = _nftMarketContract.GetOfferList(symbol, tokenId, OtherAccount);
            Logger.Info($"offerList is {offerList}");
            var offerList1 = _nftMarketContract.GetBidList(symbol, tokenId, OtherAccount);
            Logger.Info($"offerList is {offerList1}");
            
            //22.3. nft has been purchased, another user purchased again
            Thread.Sleep(10 * 1000);
            _nftMarketContract.SetAccount(OtherAccount1);
            var result1=_nftMarketContract.MakeOffer(
                symbol,
                tokenId,
                InitAccount,
                buyAmount,
                new Price
                {
                    Symbol = "ELF",
                    Amount = startingPrice+2_00000000
                },
                expireTime
            );
            result1.Error.ShouldContain("Origin owner cannot be sender himself.");
        }

        [TestMethod]
        //ListWithDutchAuction-fail
        public void MakeOfferListWithDutchAuctionTest1()
        {
            var tokenId = 1;
            var totalAmount = 1000;
            var totalSupply = 10000;
            var mintAmount = 1000;
            var sellAmount = 100;
            var startingPrice = 12_00000000;
            var endingPrice = 5_00000000;
            var whitePrice1 = 9_00000000;
            var buyAmount = 1;
            var whiteSymbol = "ELF";

            var expireTime = DateTime.UtcNow.AddHours(1).ToTimestamp();

            var startTime = DateTime.UtcNow.ToTimestamp();
            var publicTime = DateTime.UtcNow.AddHours(12).ToTimestamp();
            var durationHours = 48;
            var symbol = CreateAndMint(totalSupply, mintAmount, tokenId);
            Logger.Info($"startTime is {startTime.Seconds}");
            Logger.Info($"publicTime is {publicTime.Seconds}");
            var purchaseSymbol = "ELF";
            var isMerge = true;

            ListWithDutchAuction(symbol, tokenId, startingPrice, endingPrice, purchaseSymbol,
                startTime, publicTime, durationHours, whiteSymbol, whitePrice1);
            
            
            // Approve
            _nftContract.SetAccount(OtherAccount);
            var approve = _tokenContract.ApproveToken(OtherAccount, _nftMarketContract.ContractAddress,
                10000000000_00000000, "ELF");
            approve.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            _nftContract.SetAccount(OtherAccount1);
            var approve1 = _tokenContract.ApproveToken(OtherAccount1, _nftMarketContract.ContractAddress,
                10000000000_00000000, "ELF");
            approve1.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            
            
            //23 . endingPrice <   Enter amount<  startingPrice
            Thread.Sleep(10 * 1000);
            var offerList = _nftMarketContract.GetOfferList(symbol, tokenId, OtherAccount);
            offerList.ShouldBe(new OfferList());
            
            _nftMarketContract.SetAccount(OtherAccount);
            _nftMarketContract.MakeOffer(
                symbol,
                tokenId,
                InitAccount,
                buyAmount,
                new Price
                {
                    Symbol = "ELF",
                    Amount = startingPrice-1_00000000
                },
                expireTime
            ); 
            
            offerList = _nftMarketContract.GetOfferList(symbol, tokenId, OtherAccount);
            offerList.Value.Count.ShouldBe(1);
            offerList.Value[0].From.ShouldBe(OtherAccount.ConvertAddress());
            offerList.Value[0].To.ShouldBe(InitAccount.ConvertAddress());
            offerList.Value[0].Price.Symbol.ShouldBe("ELF");
            offerList.Value[0].Price.Amount.ShouldBe(startingPrice-1_00000000);
            offerList.Value.First().Quantity.ShouldBe(1);
            offerList.Value.First().ExpireTime.ShouldBe(expireTime);

            
            //24 . enterAmount <endingPrice  < =  startingPrice 
            Thread.Sleep(10 * 1000);

            _nftMarketContract.SetAccount(OtherAccount1);
            _nftMarketContract.MakeOffer(
                symbol,
                tokenId,
                InitAccount,
                buyAmount,
                new Price
                {
                    Symbol = "ELF",
                    Amount = endingPrice-1_00000000
                },
                expireTime
            ); 
            var offerList1 = _nftMarketContract.GetOfferList(symbol, tokenId, OtherAccount1);
            offerList1.Value.Count.ShouldBe(1);
            offerList1.Value[0].From.ShouldBe(OtherAccount1.ConvertAddress());
            offerList1.Value[0].To.ShouldBe(InitAccount.ConvertAddress());
            offerList1.Value[0].Price.Symbol.ShouldBe("ELF");
            offerList1.Value[0].Price.Amount.ShouldBe(endingPrice-1_00000000);
            offerList1.Value.First().Quantity.ShouldBe(1);
            offerList.Value.First().ExpireTime.ShouldBe(expireTime);

        }



        [TestMethod]
        //ListWithDutchAuction-Timeout
        public void MakeOfferListWithDutchAuctionTest2()
        {
            var tokenId = 1;
            var totalAmount = 1000;
            var totalSupply = 10000;
            var mintAmount = 1000;
            var sellAmount = 100;
            var startingPrice = 12_00000000;
            var endingPrice = 5_00000000;
            var whitePrice1 = 9_00000000;
            var buyAmount = 1;
            var whiteSymbol = "ELF";

            var expireTime = DateTime.UtcNow.AddHours(1).ToTimestamp();

            var startTime = DateTime.UtcNow.ToTimestamp();
            var publicTime = DateTime.UtcNow.AddHours(12).ToTimestamp();
            var durationHours = 48;
            var symbol = "";
            Logger.Info($"startTime is {startTime.Seconds}");
            Logger.Info($"publicTime is {publicTime.Seconds}");
            var purchaseSymbol = "ELF";
            var isMerge = true;

            //approve
            _nftContract.SetAccount(OtherAccount);
            var approve1 = _tokenContract.ApproveToken(OtherAccount, _nftMarketContract.ContractAddress,
                10000000000_00000000, "ELF");
            approve1.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            
            //26.DurationHours <  UtcNow
            
            //yJuU65ntu2FGRc7U1iYQXDur6iGMv9DSofurAgDoJCdEfEM3Q
            //Bwg98qZsPZjuqUrJQnKo5ukNfuGeSgivBqfwhusUKdXvceXkn
            //CO680936746
            _nftMarketContract.SetAccount(OtherAccount);
            _nftMarketContract.MakeOffer(
                symbol,
                tokenId,
                InitAccount,
                buyAmount,
                new Price
                {
                    Symbol = "ELF",
                    Amount = startingPrice + 1_00000000
                },
                expireTime
            );
            
            var offerList = _nftMarketContract.GetOfferList(symbol, tokenId, OtherAccount);
            offerList.Value.Count.ShouldBe(1);
            offerList.Value[0].From.ShouldBe(OtherAccount.ConvertAddress());
            offerList.Value[0].To.ShouldBe(InitAccount.ConvertAddress());
            offerList.Value[0].Price.Symbol.ShouldBe("ELF");
            offerList.Value[0].Price.Amount.ShouldBe(startingPrice + 1_00000000);
            offerList.Value.First().Quantity.ShouldBe(0);
            offerList.Value.First().ExpireTime.ShouldBe(expireTime);
            
        }




        [TestMethod]
        public void CustomMadeTest()
        {
            var tokenId = 1;
            var tokenId1 = 2;
            var totalAmount = 1000;
            var totalSupply = 10000;
            var mintAmount = 1000;
            var sellAmount = 100;
            var customPrice = 1_0000;
            var endingPrice = 5_00000000;
            var whitePrice1 = 9_00000000;
            var buyAmount = 1;
            
            var expireTime = DateTime.UtcNow.AddHours(1).ToTimestamp();

            var startTime = DateTime.UtcNow.ToTimestamp();
            var publicTime = DateTime.UtcNow.AddHours(12).ToTimestamp();
            var durationHours = 48;
            var symbol = CreateAndMintUnReuse(totalSupply, mintAmount, tokenId);
            Logger.Info($"startTime is {startTime.Seconds}");
            Logger.Info($"publicTime is {publicTime.Seconds}");
            var purchaseSymbol = "USDT";
            var isMerge = true;
            var isConfirm = true;
            var isConfirm1 = false;

            
            var depositRate = 1000;
            var workHours = 1;
            var whiteListHours = 1;
            var purchaseAmount = 20_00000000;
            var stakingAmount = 10_00000000;

            // Initialize
            ContractInitialize();
            
            //approve
            _nftContract.SetAccount(OtherAccount);
            var approve1 = _tokenContract.ApproveToken(OtherAccount, _nftMarketContract.ContractAddress,
                10000000000_00000000, "USDT");
            approve1.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            
            _nftContract.SetAccount(OtherAccount1);
            var approve2 = _tokenContract.ApproveToken(OtherAccount1, _nftMarketContract.ContractAddress,
                10000000000_00000000, "USDT");
            approve2.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            
            _nftContract.SetAccount(OtherAccount2);
            var approve3 = _tokenContract.ApproveToken(OtherAccount2, _nftMarketContract.ContractAddress,
                10000000000_00000000, "USDT");
            approve3.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            

            
           //27.Creator not open SetCustomizeInfo
           _nftMarketContract.SetAccount(OtherAccount);
           var result = _nftMarketContract.MakeOffer(
               symbol,
               tokenId1,
               InitAccount,
               buyAmount,
               new Price
               {
                   Symbol = "USDT",
                   Amount = purchaseAmount,
               },
               expireTime
           );
           result.Error.ShouldContain("Cannot request new item for this protocol.");
           
           
           
           _nftMarketContract.SetAccount(InitAccount);
           var setCustomizeInfoResult = _nftMarketContract.SetCustomizeInfo(symbol, depositRate, new Price
           {
               Symbol = purchaseSymbol,
               Amount = purchaseAmount
           }, workHours, whiteListHours, 0);
           setCustomizeInfoResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
           
           
           //28.endingPrice <=   Enter amount ,Seller did not confirm
           Thread.Sleep(60 * 1000);
           var balanceOtherAccountStart = _tokenContract.GetUserBalance(OtherAccount, "USDT");
           Logger.Info($"balanceOtherStart is {balanceOtherAccountStart}");
           
           var balanceInitAccountStart = _tokenContract.GetUserBalance(InitAccount, "USDT");
           Logger.Info($"balanceInitStart is {balanceInitAccountStart}");

           _nftMarketContract.SetAccount(OtherAccount);
           var CustomMade =_nftMarketContract.MakeOffer(
               symbol,
               tokenId1,
               InitAccount,
               buyAmount,
               new Price
               {
                   Symbol = "USDT",
                   Amount = purchaseAmount,
               },
               expireTime
           );
           CustomMade.Status.ConvertTransactionResultStatus()
               .ShouldBe(TransactionResultStatus.Mined);
           
           var RequestInfo = _nftMarketContract.GetRequestInfo(symbol, tokenId1);
           Logger.Info($"RequestInfo is {RequestInfo}");
           var CustomizeInfo = _nftMarketContract.GetCustomizeInfo(symbol);
           Logger.Info($"CustomizeInfo is {CustomizeInfo}");
           
           var balanceOtherAccountFinish = _tokenContract.GetUserBalance(OtherAccount, "USDT");
           Logger.Info($"balanceOtherFinish is {balanceOtherAccountFinish}");
           var balanceInitAccountFinish = _tokenContract.GetUserBalance(InitAccount, "USDT");
           Logger.Info($"balanceInitFinish is {balanceInitAccountFinish}");
           
           
           //29.repeatedly buy
           Thread.Sleep(60 * 1000);
           _nftMarketContract.SetAccount(OtherAccount);
           var CustomMade1 =_nftMarketContract.MakeOffer(
               symbol,
               tokenId1,
               InitAccount,
               buyAmount,
               new Price
               {
                   Symbol = "USDT",
                   Amount = purchaseAmount,
               },
               expireTime
           );
           CustomMade1.Status.ConvertTransactionResultStatus()
               .ShouldBe(TransactionResultStatus.NodeValidationFailed);
           CustomMade1.Error.ShouldContain("Request already existed.");

           
           //29.1. endingPrice <=   Enter amount ,Seller confirm
           Thread.Sleep(60 * 1000);
           var balanceOtherAccount1Start1 = _tokenContract.GetUserBalance(OtherAccount1, "USDT");
           Logger.Info($"balanceOtherStart1 is {balanceOtherAccount1Start1}");
           
           var balanceInitAccountStart1 = _tokenContract.GetUserBalance(InitAccount, "USDT");
           Logger.Info($"balanceInitStart1 is {balanceInitAccountStart1}");
           
           var balanceWhiteListAddress2Start = _tokenContract.GetUserBalance(WhiteListAddress2, "USDT");
           Logger.Info($"balanceWhiteListAddress2Start is {balanceWhiteListAddress2Start}");
           
           _nftMarketContract.SetAccount(OtherAccount1);
           var CustomMade2 =_nftMarketContract.MakeOffer(
               symbol,
               3,
               InitAccount,
               buyAmount,
               new Price
               {
                   Symbol = "USDT",
                   Amount = purchaseAmount,
               },
               expireTime
           );
           CustomMade2.Status.ConvertTransactionResultStatus()
               .ShouldBe(TransactionResultStatus.Mined);
           
           
          
           _nftMarketContract.SetAccount(InitAccount);
           var handleRequestResult = _nftMarketContract.HandleRequest(symbol, tokenId1, OtherAccount1, isConfirm);
           handleRequestResult.Status.ConvertTransactionResultStatus()
               .ShouldBe(TransactionResultStatus.Mined);


           var RequestInfo1 = _nftMarketContract.GetRequestInfo(symbol, tokenId1);
           Logger.Info($"RequestInfo1 is {RequestInfo1}");
           var CustomizeInfo1 = _nftMarketContract.GetCustomizeInfo(symbol);
           Logger.Info($"CustomizeInfo1 is {CustomizeInfo1}");
           
           var balanceOtherAccountFinish1 = _tokenContract.GetUserBalance(OtherAccount1, "USDT");
           Logger.Info($"balanceOtherFinish1 is {balanceOtherAccountFinish1}");
           var balanceInitAccountFinish1 = _tokenContract.GetUserBalance(InitAccount, "USDT");
           Logger.Info($"balanceInitFinish1 is {balanceInitAccountFinish1}");
           var balanceWhiteListAddress2Finish = _tokenContract.GetUserBalance(WhiteListAddress2, "USDT");
           Logger.Info($"balanceWhiteListAddress2Start is {balanceWhiteListAddress2Finish}");
           
           
           
           //29.2. endingPrice <=   Enter amount ,Seller confirm
           Thread.Sleep(60 * 1000);
           var balanceOtherAccount2Start2 = _tokenContract.GetUserBalance(OtherAccount2, "USDT");
           Logger.Info($"balanceOtherStart2 is {balanceOtherAccount2Start2}");
           
           var balanceInitAccountStart2 = _tokenContract.GetUserBalance(InitAccount, "USDT");
           Logger.Info($"balanceInitStart2 is {balanceInitAccountStart2}");

           _nftMarketContract.SetAccount(OtherAccount2);
           var CustomMade3 =_nftMarketContract.MakeOffer(
               symbol,
               4,
               InitAccount,
               buyAmount,
               new Price
               {
                   Symbol = "USDT",
                   Amount = purchaseAmount,
               },
               expireTime
           );
           
           CustomMade3.Status.ConvertTransactionResultStatus()
               .ShouldBe(TransactionResultStatus.Mined);
           
           _nftMarketContract.SetAccount(InitAccount);
           var handleRequestResult1 = _nftMarketContract.HandleRequest(symbol, tokenId1, OtherAccount2, isConfirm1);
           handleRequestResult1.Status.ConvertTransactionResultStatus()
               .ShouldBe(TransactionResultStatus.Mined);

           
           var RequestInfo2 = _nftMarketContract.GetRequestInfo(symbol, tokenId1);
           Logger.Info($"RequestInfo2 is {RequestInfo2}");
           var CustomizeInfo2 = _nftMarketContract.GetCustomizeInfo(symbol);
           Logger.Info($"CustomizeInfo2 is {CustomizeInfo2}");
           
           var balanceOtherAccount2Finish2 = _tokenContract.GetUserBalance(OtherAccount2, "USDT");
           Logger.Info($"balanceOtherFinish2 is {balanceOtherAccount2Finish2}");
           var balanceInitAccountFinish2 = _tokenContract.GetUserBalance(InitAccount, "USDT");
           Logger.Info($"balanceInitFinish2 is {balanceInitAccountFinish2}");
           
            
           //29.3 Enter Amount< purchaseAmount* depositRate/10000
            Thread.Sleep(60 * 1000);

            _nftMarketContract.SetAccount(OtherAccount2);
            var CustomMade4 =_nftMarketContract.MakeOffer(
                symbol,
                5,
                InitAccount,
                buyAmount,
                new Price
                {
                    Symbol = "USDT",
                    Amount = customPrice,
                },
                expireTime
            );
            CustomMade4.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            CustomMade4.Error.ShouldContain(".");
            
            
            
        }

        
        
        
        
        
        
        [TestMethod]
        public void FixedMakeOffer(long priceAmount,string symbol,Timestamp expireTime)
        {
            var tokenId = 1;
            var totalAmount = 1000;
            var totalSupply = 10000;
            var mintAmount = 1000;
            var fixedPrice = 12_00000000;
            var whitePrice1 = 9_00000000;
            var whitePrice2 = 10_00000000;
            var whitePrice3 = 11_00000000;
            var buyAmount = 1;
            //var expireTime = DateTime.UtcNow.AddSeconds(30).ToTimestamp();
            
            var durationHours = 48;
            
            var CustomMade =_nftMarketContract.MakeOffer(
                symbol,
                tokenId,
                InitAccount,
                buyAmount,
                new Price
                {
                    Symbol = "USDT",
                    Amount = priceAmount
                },
                expireTime
            );
            CustomMade.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);
            
        }
        

        [TestMethod]
        public void CancelOfferTest()
        {
            var tokenId = 1;
            var totalAmount = 1000;
            var totalSupply = 10000;
            var mintAmount = 1000;
            var sellAmount = 100;
            var fixedPrice = 12_00000000;
            var whitePrice1 = 9_00000000;
            var whitePrice2 = 10_00000000;
            var whitePrice3 = 11_00000000;
            var buyAmount = 2;
            var expireTime = DateTime.UtcNow.AddHours(30).ToTimestamp();
            var expireTime1 = DateTime.UtcNow.AddSeconds(30).ToTimestamp();

            var startTime = DateTime.UtcNow.ToTimestamp();
            var publicTime = DateTime.UtcNow.AddSeconds(10).ToTimestamp();
            var durationHours = 48;
            var symbol = CreateAndMint(totalSupply, mintAmount, tokenId);
            Logger.Info($"startTime is {startTime.Seconds}");
            Logger.Info($"publicTime is {publicTime.Seconds}");
            var purchaseSymbol = "USDT";
            var isMerge = true;
            
            // Initialize
            ContractInitialize();

            //approve
            _nftContract.SetAccount(OtherAccount);
            var approve1 = _tokenContract.ApproveToken(OtherAccount, _nftMarketContract.ContractAddress,
                10000000000_00000000, "USDT");
            approve1.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            _nftContract.SetAccount(OtherAccount1);
            var approve2 = _tokenContract.ApproveToken(OtherAccount1, _nftMarketContract.ContractAddress,
                10000000000_00000000, "USDT");
            approve2.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            _nftContract.SetAccount(OtherAccount2);
            var approve3 = _tokenContract.ApproveToken(OtherAccount2, _nftMarketContract.ContractAddress,
                10000000000_00000000, "USDT");
            approve3.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            
            _nftContract.SetAccount(OtherAccount3);
            var approve4 = _tokenContract.ApproveToken(OtherAccount3, _nftMarketContract.ContractAddress,
                10000000000_00000000, "USDT");
            approve4.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            
            
            _nftContract.SetAccount(InitAccount);
            ListWithFixedPrice(symbol, tokenId, sellAmount, fixedPrice, whitePrice1, whitePrice2,
                whitePrice3, startTime, publicTime, durationHours, purchaseSymbol, isMerge);
            
            
            
            //30. OtherAccount CamcelOffer , DurationHours > UtcNow  
            _nftMarketContract.SetAccount(OtherAccount);
            FixedMakeOffer(7_00000000,symbol,expireTime);

            var OfferList = _nftMarketContract.GetOfferList(symbol, tokenId, OtherAccount);
            OfferList.Value.Count.ShouldBe(1);
            
            
            _nftMarketContract.SetAccount(OtherAccount);
            var CustomMade1 =_nftMarketContract.CancelOffer(
                symbol,
                tokenId,
                new Int32List
                {
                    Value = { 0 }
                },
                OtherAccount,
                InitAccount

            );
            CustomMade1.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);
            OfferList = _nftMarketContract.GetOfferList(symbol, tokenId, OtherAccount);
            OfferList.Value.Count.ShouldBe(0);
            
            
            //31. InitAccount CamcelOffer , DurationHours > UtcNow  
            _nftMarketContract.SetAccount(OtherAccount1);
            FixedMakeOffer(6_00000000,symbol,expireTime);

            var OfferList1 = _nftMarketContract.GetOfferList(symbol, tokenId, OtherAccount1);
            OfferList1.Value.Count.ShouldBe(1);
            
            _nftMarketContract.SetAccount(InitAccount);
            var CustomMade2 =_nftMarketContract.CancelOffer(
                symbol,
                tokenId,
                new Int32List
                {
                    Value = { 0 }
                },
                OtherAccount,
                InitAccount

            );
            CustomMade2.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            CustomMade2.Error.ShouldContain(".");
            
            
            //32. OtherAccount2 repeatedly CamcelOffer , DurationHours > UtcNow  
            _nftMarketContract.SetAccount(OtherAccount2);
            FixedMakeOffer(6_00000000,symbol,expireTime1);
            FixedMakeOffer(7_00000000,symbol,expireTime1);
            FixedMakeOffer(8_00000000,symbol,expireTime1);

            _nftMarketContract.SetAccount(OtherAccount3);
            FixedMakeOffer(9_00000000,symbol,expireTime1);

            var OfferListOtherAccount1 = _nftMarketContract.GetOfferList(symbol, tokenId, OtherAccount2);
            OfferListOtherAccount1.Value.Count.ShouldBe(3);
            var OfferListOtherAccount2 = _nftMarketContract.GetOfferList(symbol, tokenId, OtherAccount3);
            OfferListOtherAccount2.Value.Count.ShouldBe(1);
            
            _nftMarketContract.SetAccount(OtherAccount2);
            Thread.Sleep(60 * 1000);
            var CustomMade3 =_nftMarketContract.CancelOffer(
                symbol,
                tokenId,
                new Int32List
                {
                    Value = { 1,2 }
                },
                OtherAccount,
                InitAccount

            );
            CustomMade3.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);
            
            OfferListOtherAccount2 = _nftMarketContract.GetOfferList(symbol, tokenId, OtherAccount3);
            OfferListOtherAccount2.Value.Count.ShouldBe(1);
            OfferListOtherAccount2.Value[2].From.ShouldBe(OtherAccount.ConvertAddress());
            OfferListOtherAccount2.Value[2].To.ShouldBe(InitAccount.ConvertAddress());
            OfferListOtherAccount2.Value[2].Price.Symbol.ShouldBe(purchaseSymbol);
            OfferListOtherAccount2.Value[2].Price.TokenId.ShouldBe(0);
            OfferListOtherAccount2.Value[2].Price.Amount.ShouldBe(9_00000000);
            OfferListOtherAccount2.Value[2].Quantity.ShouldBe(buyAmount);
            OfferListOtherAccount2.Value[2].ExpireTime.ShouldBe(expireTime1);
            
            //33. InitAccount CamcelOffer , DurationHours < UtcNow 
            _nftMarketContract.SetAccount(InitAccount);
            Thread.Sleep(60 * 1000);
            var CustomMade4 =_nftMarketContract.CancelOffer(
                symbol,
                tokenId,
                new Int32List
                {
                    Value = { 2 }
                },
                OtherAccount,
                InitAccount

            );
            CustomMade4.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);
            OfferListOtherAccount2 = _nftMarketContract.GetOfferList(symbol, tokenId, OtherAccount3);
            OfferListOtherAccount2.Value.Count.ShouldBe(0);

        }





        
        
        



    }
    
}
