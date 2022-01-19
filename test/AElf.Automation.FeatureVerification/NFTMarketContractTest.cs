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
    public class NFTMarketContractTest
    {
        private ILog Logger { get; set; }
        private INodeManager NodeManager { get; set; }
        private int _chainId;
        private GenesisContract _genesisContract;
        private TokenContract _tokenContract;
        private NftContract _nftContract;
        private NFTMarketContract _nftMarketContract;

        private string InitAccount { get; } = "J6zgLjGwd1bxTBpULLXrGVeV74tnS2n74FFJJz7KNdjTYkDF6";

        // private string InitAccount { get; } = "nn659b9X1BLhnu5RWmEUbuuV7J9QKVVSN54j9UmeCbF3Dve5D";
        private string OtherAccount { get; } = "sjzNpr5bku3ZyvMqQrXeBkXGEvG2CTLA2cuNDfcDMaPTTAqEy";
        private string WhiteListAddress1 { get; } = "sjzNpr5bku3ZyvMqQrXeBkXGEvG2CTLA2cuNDfcDMaPTTAqEy";
        private string WhiteListAddress2 { get; } = "2bs2uYMECtHWjB57RqgqQ3X2LrxgptWHtzCqGEU11y45aWimh4";
        private string WhiteListAddress3 { get; } = "YUW9zH5GhRboT5JK4vXp5BLAfCDv28rRmTQwo418FuaJmkSg8";

        private static string RpcUrl { get; } = "192.168.66.9:8000";
        private AuthorityManager AuthorityManager { get; set; }

        private string NFT = "";
        private string NFTMarket = "";

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
        [DataRow(10000, 1000, 100, 10, 9, 10, 11, "ELF", true)]
        public void ListWithFixedPriceWhiteListTest(long totalSupply, long mintAmount, long sellAmount, long fixedPrice,
            long whitePrice1, long whitePrice2, long whitePrice3, string purchaseSymbol, bool isMerge)
        {
            // StartTime = PublicTime
            var tokenId = 1;
            var symbol = CreateAndMint(totalSupply, mintAmount, tokenId);
            var startTime = DateTime.UtcNow.AddSeconds(60).ToTimestamp();
            var publicTime = DateTime.UtcNow.AddSeconds(60).ToTimestamp();
            var durationHours = 48;
            ListWithFixedPrice(symbol, tokenId, mintAmount, sellAmount, fixedPrice, whitePrice1, whitePrice2,
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
            tokenId = 2;
            symbol = CreateAndMint(totalSupply, mintAmount, tokenId);
            startTime = DateTime.UtcNow.AddHours(12).ToTimestamp();
            publicTime = DateTime.UtcNow.AddHours(24).ToTimestamp();
            durationHours = 48;
            ListWithFixedPrice(symbol, tokenId, mintAmount, sellAmount, fixedPrice, whitePrice1, whitePrice2,
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

            // StartTime > PublicTime
            tokenId = 3;
            symbol = CreateAndMint(totalSupply, mintAmount, tokenId);
            startTime = DateTime.UtcNow.AddHours(24).ToTimestamp();
            publicTime = DateTime.UtcNow.AddHours(12).ToTimestamp();
            durationHours = 48;
            ListWithFixedPrice(symbol, tokenId, mintAmount, sellAmount, fixedPrice, whitePrice1, whitePrice2,
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
            whiteListAddressPriceList.Value[0].Price.Amount.ShouldBe(9);
            whiteListAddressPriceList.Value[1].Address.ShouldBe(WhiteListAddress2.ConvertAddress());
            whiteListAddressPriceList.Value[1].Price.Symbol.ShouldBe("ELF");
            whiteListAddressPriceList.Value[1].Price.Amount.ShouldBe(10);
            whiteListAddressPriceList.Value[2].Address.ShouldBe(WhiteListAddress3.ConvertAddress());
            whiteListAddressPriceList.Value[2].Price.Symbol.ShouldBe("ELF");
            whiteListAddressPriceList.Value[2].Price.Amount.ShouldBe(11);
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

            ListWithFixedPrice(symbol, tokenId, mintAmount, sellAmount, fixedPrice, whitePrice1, whitePrice2,
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
        [DataRow(1, 2, 1000, 1000, 100, 10, 9, 10, 11, "ELF", true, 49)]
        [DataRow(2, 2, 1000, 1000, 100, 10, 9, 10, 11, "ELF", true, 24)]
        [DataRow(3, 2, 1000, 1000, 100, 10, 9, 10, 11, "USDT", true, 24)]
        [DataRow(4, 2, 1000, 1000, 100, 20, 9, 10, 11, "ELF", true, 24)]
        [DataRow(5, 2, 1000, 1000, 100, 10, 9, 10, 11, "ELF", false, 24)]
        [DataRow(6, 2, 1000, 1000, 100, 10, 9, 10, 11, "ELF", false, 30)]
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
                Value = {"USDT"}
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

            ListWithFixedPrice(symbol, tokenId, mintAmount, sellAmount, fixedPrice, whitePrice1, whitePrice2,
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
            var startTime = DateTime.UtcNow.AddHours(1).ToTimestamp();
            var publicTime = DateTime.UtcNow.AddHours(2).ToTimestamp();
            var durationHours = 48;
            var totalSupply = 10000;
            var mintAmount = 1000;
            var tokenId = 1;
            var sellAmount = 100;
            var fixedPrice = 10_00000000;
            var whitePrice1 = 9_00000000;
            var whitePrice2 = 10_00000000;
            var whitePrice3 = 11_00000000;
            var purchaseSymbol = "ELF";
            var isMerge = true;

            var symbol = CreateAndMint(totalSupply, mintAmount, tokenId);

            var protocolInfo = _nftContract.GetNftProtocolInfo(symbol);
            Logger.Info($"protocolInfo.Symbol is {protocolInfo.Symbol}");

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

        [TestMethod]
        // [DataRow("","")]
        // [DataRow("","")]
        // [DataRow("","")]
        public void ListWithEnglishAuctionTest()
        {
            var tokenId = 1;
            var totalSupply = 1000;
            var mintAmount = 1000;
            var sellAmount = 100;
            var fixedPrice = 10;
            var whitePrice1 = 9;
            var whitePrice2 = 10;
            var whitePrice3 = 11;
            var symbol = CreateAndMint(totalSupply, mintAmount, tokenId);
            var startTime = DateTime.UtcNow.AddSeconds(10).ToTimestamp();
            var publicTime = DateTime.UtcNow.AddHours(24).ToTimestamp();
            var durationHours = 48;
            var protocolInfo = _nftContract.GetNftProtocolInfo(symbol);
            Logger.Info($"protocolInfo.Symbol is {protocolInfo.Symbol}");

            _nftMarketContract.Initialize(
                _nftContract.CallAddress,
                InitAccount,
                10,
                InitAccount,
                1000
            );

            var listedNFTInfo = _nftMarketContract.GetListedNFTInfoList(symbol, tokenId, InitAccount).Value;
            listedNFTInfo.First().ShouldBe(new ListedNFTInfo());

            _nftMarketContract.ListWithEnglishAuction(
                symbol,
                tokenId,
                1,
                "ELF",
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
                                Symbol = "ELF",
                                Amount = whitePrice1
                            }
                        }
                    }
                }
            );

            var listedNFTInfoFirst =
                _nftMarketContract.GetListedNFTInfoList(symbol, tokenId, InitAccount).Value.First();
            listedNFTInfoFirst.Symbol.ShouldBe(symbol);
            listedNFTInfoFirst.TokenId.ShouldBe(tokenId);
            listedNFTInfoFirst.Owner.ShouldBe(InitAccount.ConvertAddress());
            listedNFTInfoFirst.Quantity.ShouldBe(sellAmount);
            listedNFTInfoFirst.ListType.ShouldBe(ListType.FixedPrice);
            listedNFTInfoFirst.Price.Symbol.ShouldBe("ELF");
            listedNFTInfoFirst.Price.Amount.ShouldBe(fixedPrice);
            listedNFTInfoFirst.Duration.StartTime.ShouldBe(startTime);
            listedNFTInfoFirst.Duration.PublicTime.ShouldBe(publicTime);
            listedNFTInfoFirst.Duration.DurationHours.ShouldBe(durationHours);
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

        public void ListWithFixedPrice(string symbol, int tokenId, long totalAmount, long sellAmount,
            long fixedPrice,
            long whitePrice1, long whitePrice2, long whitePrice3, Timestamp startTime, Timestamp publicTime,
            int durationHours, string purchaseSymbol, bool isMerge)
        {
            var protocolInfo = _nftContract.GetNftProtocolInfo(symbol);
            Logger.Info($"protocolInfo.Symbol is {protocolInfo.Symbol}");

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
                                Symbol = "ELF",
                                Amount = whitePrice1
                            }
                        },
                        new WhiteListAddressPrice
                        {
                            Address = WhiteListAddress2.ConvertAddress(),
                            Price = new Price
                            {
                                Symbol = "ELF",
                                Amount = whitePrice2
                            },
                        },
                        new WhiteListAddressPrice
                        {
                            Address = WhiteListAddress3.ConvertAddress(),
                            Price = new Price
                            {
                                Symbol = "ELF",
                                Amount = whitePrice3
                            },
                        }
                    }
                },
                isMerge
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
    }
}