using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AElf.Contracts.MultiToken;
using AElf.Contracts.NFT;
using AElf.Contracts.NFTMarket;
using AElf.CSharp.Core;
using AElf.CSharp.Core.Extension;
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
        private string OtherAccount { get; } = "sjzNpr5bku3ZyvMqQrXeBkXGEvG2CTLA2cuNDfcDMaPTTAqEy";

        private string BuyerAccount { get; } = "2RehEQSpXeZ5DUzkjTyhAkr9csu7fWgE5DAuB2RaKQCpdhB8zC";
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
                Value = {purchaseSymbol}
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
                Value = {"USDT", symbol}
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
                Value = {"USDT", symbol}
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
                _nftContract.AddMinters(new MinterList {Value = {OtherAccount.ConvertAddress()}}, symbol);
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
            var royaltyInfoOfTokenId0 = _nftMarketContract.GetRoyalty(symbol, 0);
            royaltyInfoOfTokenId0.Royalty.ShouldBe(royalty);
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
            royaltyInfoOfTokenId1 = _nftMarketContract.GetRoyalty(symbol, 1);
            royaltyInfoOfTokenId1.Royalty.ShouldBe(0);
            royaltyInfoOfTokenId1.RoyaltyFeeReceiver.ShouldBe(royaltyFeeReceiver.ConvertAddress());
            royaltyInfoOfTokenId2 = _nftMarketContract.GetRoyalty(symbol, 2);
            royaltyInfoOfTokenId2.Royalty.ShouldBe(royalty);
            royaltyInfoOfTokenId2.RoyaltyFeeReceiver.ShouldBe(royaltyFeeReceiver.ConvertAddress());

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
            royaltyInfoOfTokenId1 = _nftMarketContract.GetRoyalty(symbol, 1);
            royaltyInfoOfTokenId1.Royalty.ShouldBe(0);
            royaltyInfoOfTokenId1.RoyaltyFeeReceiver.ShouldBe(royaltyFeeReceiver.ConvertAddress());
            royaltyInfoOfTokenId2 = _nftMarketContract.GetRoyalty(symbol, 2);
            royaltyInfoOfTokenId2.Royalty.ShouldBe(50);
            royaltyInfoOfTokenId2.RoyaltyFeeReceiver.ShouldBe(royaltyFeeReceiver.ConvertAddress());

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
        public void SetTokenWhiteListAndGlobalTokenWhiteListTest()
        {
            var totalSupply = 1000;
            var mintAmount = 1000;
            var tokenId = 1;
            var symbol = CreateAndMint(totalSupply, mintAmount, tokenId);

            // Initialize
            var initialize = _nftMarketContract.Initialize(
                _nftContract.ContractAddress,
                InitAccount,
                100,
                InitAccount,
                10_00000000
            );
            initialize.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            var globalTokenWhiteList = _nftMarketContract.GetGlobalTokenWhiteList();
            var tokenWhiteList = _nftMarketContract.GetTokenWhiteList(symbol);
            globalTokenWhiteList.Value.Count.ShouldBe(1);
            globalTokenWhiteList.Value.ShouldContain("ELF");
            tokenWhiteList.Value.Count.ShouldBe(1);
            tokenWhiteList.Value.ShouldContain("ELF");

            var setGlobalTokenWhiteResult = _nftMarketContract.SetGlobalTokenWhiteList(new StringList
            {
                Value = {"ELF", "USDT", "USDT"}
            });
            setGlobalTokenWhiteResult.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);
            globalTokenWhiteList = _nftMarketContract.GetGlobalTokenWhiteList();
            tokenWhiteList = _nftMarketContract.GetTokenWhiteList(symbol);
            globalTokenWhiteList.Value.Count.ShouldBe(3);
            globalTokenWhiteList.Value.ShouldContain("ELF");
            globalTokenWhiteList.Value.ShouldContain("USDT");
            tokenWhiteList.Value.Count.ShouldBe(3);
            tokenWhiteList.Value.ShouldContain("ELF");
            tokenWhiteList.Value.ShouldContain("USDT");
            Logger.Info($"globalTokenWhiteList {globalTokenWhiteList}");
            Logger.Info($"tokenWhiteList {tokenWhiteList}");

            var setTokenWhiteListResult = _nftMarketContract.SetTokenWhiteList(symbol, new StringList
            {
                Value = {"Token1"}
            });
            setTokenWhiteListResult.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);
            globalTokenWhiteList = _nftMarketContract.GetGlobalTokenWhiteList();
            tokenWhiteList = _nftMarketContract.GetTokenWhiteList(symbol);
            globalTokenWhiteList.Value.Count.ShouldBe(3);
            globalTokenWhiteList.Value.ShouldContain("ELF");
            globalTokenWhiteList.Value.ShouldContain("USDT");
            tokenWhiteList.Value.Count.ShouldBe(3);
            tokenWhiteList.Value.ShouldContain("ELF");
            tokenWhiteList.Value.ShouldContain("USDT");
            tokenWhiteList.Value.ShouldContain("Token1");
            Logger.Info($"globalTokenWhiteList {globalTokenWhiteList}");
            Logger.Info($"tokenWhiteList {tokenWhiteList}");

            setGlobalTokenWhiteResult = _nftMarketContract.SetGlobalTokenWhiteList(new StringList
            {
                Value = {"ELF"}
            });
            setGlobalTokenWhiteResult.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);
            globalTokenWhiteList = _nftMarketContract.GetGlobalTokenWhiteList();
            tokenWhiteList = _nftMarketContract.GetTokenWhiteList(symbol);
            globalTokenWhiteList.Value.Count.ShouldBe(1);
            globalTokenWhiteList.Value.ShouldContain("ELF");
            tokenWhiteList.Value.Count.ShouldBe(2);
            tokenWhiteList.Value.ShouldContain("ELF");
            tokenWhiteList.Value.ShouldContain("Token1");
            Logger.Info($"globalTokenWhiteList {globalTokenWhiteList}");
            Logger.Info($"tokenWhiteList {tokenWhiteList}");

            setTokenWhiteListResult = _nftMarketContract.SetTokenWhiteList(symbol, new StringList
            {
                Value = {"Token2", "Token2", "Token3"}
            });
            setTokenWhiteListResult.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);
            globalTokenWhiteList = _nftMarketContract.GetGlobalTokenWhiteList();
            tokenWhiteList = _nftMarketContract.GetTokenWhiteList(symbol);
            globalTokenWhiteList.Value.Count.ShouldBe(1);
            globalTokenWhiteList.Value.ShouldContain("ELF");
            tokenWhiteList.Value.Count.ShouldBe(4);
            tokenWhiteList.Value.ShouldContain("ELF");
            tokenWhiteList.Value.ShouldContain("Token2");
            tokenWhiteList.Value.ShouldContain("Token3");
            Logger.Info($"globalTokenWhiteList {globalTokenWhiteList}");
            Logger.Info($"tokenWhiteList {tokenWhiteList}");

            setGlobalTokenWhiteResult = _nftMarketContract.SetGlobalTokenWhiteList(new StringList
            {
                Value = { }
            });
            setGlobalTokenWhiteResult.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);
            globalTokenWhiteList = _nftMarketContract.GetGlobalTokenWhiteList();
            tokenWhiteList = _nftMarketContract.GetTokenWhiteList(symbol);
            globalTokenWhiteList.Value.Count.ShouldBe(1);
            globalTokenWhiteList.Value.ShouldContain("ELF");
            tokenWhiteList.Value.Count.ShouldBe(4);
            tokenWhiteList.Value.ShouldContain("ELF");
            tokenWhiteList.Value.ShouldContain("Token2");
            tokenWhiteList.Value.ShouldContain("Token3");
            Logger.Info($"globalTokenWhiteList {globalTokenWhiteList}");
            Logger.Info($"tokenWhiteList {tokenWhiteList}");

            setTokenWhiteListResult = _nftMarketContract.SetTokenWhiteList("CO12345678", new StringList
            {
                Value = {"ELF", "USDT"}
            });
            setTokenWhiteListResult.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            setTokenWhiteListResult.Error.ShouldContain("NFT Protocol not exists.");

            _nftMarketContract.SetAccount(OtherAccount);
            setTokenWhiteListResult = _nftMarketContract.SetTokenWhiteList(symbol, new StringList
            {
                Value = {"ELF", "USDT"}
            });
            setTokenWhiteListResult.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            setTokenWhiteListResult.Error.ShouldContain("Only NFT Protocol Creator can set token white list.");
        }

        [TestMethod]
        public void SetCustomizeInfoErrorTest()
        {
            var totalSupply = 1000;
            var mintAmount = 1000;
            var tokenId = 1;
            var depositRate = 1000;
            var workHours = 1;
            var whiteListHours = 1;
            var stakingAmount = 10_00000000;
            var purchaseAmount = 20_00000000;
            var symbol = CreateAndMint(totalSupply, mintAmount, tokenId);
            var purchaseSymbol = "ELF";

            // Check initialization
            var setCustomizeInfoResult = _nftMarketContract.SetCustomizeInfo(symbol, depositRate, new Price
            {
                Symbol = purchaseSymbol,
                Amount = purchaseAmount
            }, workHours, whiteListHours, stakingAmount);
            setCustomizeInfoResult.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            setCustomizeInfoResult.Error.ShouldContain("Contract not initialized.");

            // Initialize
            var initialize = _nftMarketContract.Initialize(
                _nftContract.ContractAddress,
                InitAccount,
                100,
                InitAccount,
                10_00000000
            );
            initialize.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            // Check tokenIdReuse
            setCustomizeInfoResult = _nftMarketContract.SetCustomizeInfo(symbol, depositRate, new Price
            {
                Symbol = purchaseSymbol,
                Amount = purchaseAmount
            }, workHours, whiteListHours, stakingAmount);
            setCustomizeInfoResult.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            setCustomizeInfoResult.Error.ShouldContain("Not support customize.");

            symbol = CreateAndMintUnReuse(totalSupply, mintAmount, tokenId);

            // Check staking amount
            setCustomizeInfoResult = _nftMarketContract.SetCustomizeInfo(symbol, depositRate, new Price
            {
                Symbol = purchaseSymbol,
                Amount = purchaseAmount
            }, workHours, whiteListHours, -1);
            setCustomizeInfoResult.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            setCustomizeInfoResult.Error.ShouldContain("Invalid staking amount.");

            // Approve
            var approveResult =
                _tokenContract.ApproveToken(InitAccount, _nftMarketContract.ContractAddress, 10000000000_00000000,
                    "ELF");
            approveResult.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);
            var balance = _tokenContract.GetUserBalance(InitAccount, purchaseSymbol);
            setCustomizeInfoResult = _nftMarketContract.SetCustomizeInfo(symbol, depositRate, new Price
            {
                Symbol = purchaseSymbol,
                Amount = purchaseAmount
            }, workHours, whiteListHours, balance + 1);
            setCustomizeInfoResult.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            setCustomizeInfoResult.Error.ShouldContain("Insufficient balance of");

            setCustomizeInfoResult = _nftMarketContract.SetCustomizeInfo(symbol, depositRate, new Price
            {
                Symbol = purchaseSymbol,
                Amount = purchaseAmount
            }, workHours, whiteListHours, 0);
            setCustomizeInfoResult.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);
            var customizeInfo = _nftMarketContract.GetCustomizeInfo(symbol);
            customizeInfo.Symbol.ShouldBe(symbol);
            customizeInfo.DepositRate.ShouldBe(depositRate);
            customizeInfo.Price.Symbol.ShouldBe(purchaseSymbol);
            customizeInfo.Price.Amount.ShouldBe(purchaseAmount);
            customizeInfo.WorkHours.ShouldBe(workHours);
            customizeInfo.WhiteListHours.ShouldBe(whiteListHours);
            customizeInfo.StakingAmount.ShouldBe(0);
            customizeInfo.ReservedTokenIds.Count.ShouldBe(0);

            // Check depositRate
            setCustomizeInfoResult = _nftMarketContract.SetCustomizeInfo(symbol, -1, new Price
            {
                Symbol = purchaseSymbol,
                Amount = purchaseAmount
            }, workHours, whiteListHours, stakingAmount);
            setCustomizeInfoResult.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            setCustomizeInfoResult.Error.ShouldContain("Invalid deposit rate.");

            setCustomizeInfoResult = _nftMarketContract.SetCustomizeInfo(symbol, 0, new Price
            {
                Symbol = purchaseSymbol,
                Amount = purchaseAmount
            }, workHours, whiteListHours, stakingAmount);
            setCustomizeInfoResult.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);

            // Check symbol
            setCustomizeInfoResult = _nftMarketContract.SetCustomizeInfo("CO12345678", 0, new Price
            {
                Symbol = purchaseSymbol,
                Amount = purchaseAmount
            }, workHours, whiteListHours, stakingAmount);
            setCustomizeInfoResult.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            setCustomizeInfoResult.Error.ShouldContain("NFT Protocol not found.");

            // Check purchase symbol
            setCustomizeInfoResult = _nftMarketContract.SetCustomizeInfo(symbol, 0, new Price
            {
                Symbol = "TEST",
                Amount = stakingAmount
            }, workHours, whiteListHours, stakingAmount);
            setCustomizeInfoResult.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            setCustomizeInfoResult.Error.ShouldContain("Invalid staking token symbol.");

            // Check price
            setCustomizeInfoResult =
                _nftMarketContract.SetCustomizeInfo(symbol, depositRate, null, workHours, whiteListHours,
                    stakingAmount);
            setCustomizeInfoResult.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);
            customizeInfo = _nftMarketContract.GetCustomizeInfo(symbol);
            customizeInfo.Symbol.ShouldBe(symbol);
            customizeInfo.DepositRate.ShouldBe(0);
            customizeInfo.Price.Symbol.ShouldBe(purchaseSymbol);
            customizeInfo.Price.Amount.ShouldBe(0);
            customizeInfo.WorkHours.ShouldBe(workHours);
            customizeInfo.WhiteListHours.ShouldBe(whiteListHours);
            customizeInfo.StakingAmount.ShouldBe(stakingAmount);
            customizeInfo.ReservedTokenIds.Count.ShouldBe(0);

            setCustomizeInfoResult =
                _nftMarketContract.SetCustomizeInfo(symbol, depositRate, new Price
                    {
                        Symbol = purchaseSymbol,
                        Amount = purchaseAmount
                    }, workHours, whiteListHours,
                    stakingAmount);
            setCustomizeInfoResult.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);
            customizeInfo = _nftMarketContract.GetCustomizeInfo(symbol);
            customizeInfo.Symbol.ShouldBe(symbol);
            customizeInfo.DepositRate.ShouldBe(depositRate);
            customizeInfo.Price.Symbol.ShouldBe(purchaseSymbol);
            customizeInfo.Price.Amount.ShouldBe(purchaseAmount);
            customizeInfo.WorkHours.ShouldBe(workHours);
            customizeInfo.WhiteListHours.ShouldBe(whiteListHours);
            customizeInfo.StakingAmount.ShouldBe(stakingAmount);
            customizeInfo.ReservedTokenIds.Count.ShouldBe(0);

            var stakingToken = _nftMarketContract.GetStakingTokens(symbol);
            Logger.Info($"stakingToken is {stakingToken}");
            stakingToken.Symbol.ShouldBe(purchaseSymbol);
            stakingToken.Amount.ShouldBe(stakingAmount);
        }

        [TestMethod]
        public void HandleRequestErrorTest()
        {
            var totalSupply = 1000;
            var mintAmount = 1000;
            var tokenId = 1;
            var isConfirm = true;
            var symbol = CreateAndMint(totalSupply, mintAmount, tokenId);

            // Check initialization
            var handleRequestResult = _nftMarketContract.HandleRequest(symbol, tokenId, InitAccount, isConfirm);
            handleRequestResult.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            handleRequestResult.Error.ShouldContain("Contract not initialized.");

            // Initialize
            var initialize = _nftMarketContract.Initialize(
                _nftContract.ContractAddress,
                InitAccount,
                100,
                InitAccount,
                10_00000000
            );
            initialize.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            // Check symbol
            handleRequestResult = _nftMarketContract.HandleRequest("CO12345678", tokenId, InitAccount, isConfirm);
            handleRequestResult.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            handleRequestResult.Error.ShouldContain("Request not exists.");

            // Check tokenId
            handleRequestResult = _nftMarketContract.HandleRequest(symbol, 100, InitAccount, isConfirm);
            handleRequestResult.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            handleRequestResult.Error.ShouldContain("Request not exists.");

            // Request not exists
            _nftMarketContract.SetAccount(OtherAccount);
            handleRequestResult = _nftMarketContract.HandleRequest(symbol, tokenId, InitAccount, isConfirm);
            handleRequestResult.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            handleRequestResult.Error.ShouldContain("Request not exists.");
        }

        [TestMethod]
        public void StakeAndWithdrawTest()
        {
            var totalSupply = 1000;
            var mintAmount = 1000;
            var tokenId = 1;
            var stakingAmount = 100_00000000;
            var symbol = CreateAndMintUnReuse(totalSupply, mintAmount, tokenId);
            var purchaseSymbol = "USDT";
            var purchaseAmount = 10_00000000;
            var workHours = 1;
            var whiteListHours = 1;

            // Check initialization
            var stakeForRequestsResult = _nftMarketContract.StakeForRequests(symbol, stakingAmount);
            stakeForRequestsResult.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            stakeForRequestsResult.Error.ShouldContain("Contract not initialized.");

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
            var approveResult =
                _tokenContract.ApproveToken(InitAccount, _nftMarketContract.ContractAddress, 10000000000_00000000,
                    purchaseSymbol);
            approveResult.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);

            // Customize info not found
            stakeForRequestsResult = _nftMarketContract.StakeForRequests(symbol, stakingAmount);
            stakeForRequestsResult.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            stakeForRequestsResult.Error.ShouldContain("Customize info not found.");

            // Set customize info
            var setCustomizeInfoResult = _nftMarketContract.SetCustomizeInfo(symbol, 1000, new Price
            {
                Symbol = purchaseSymbol,
                Amount = purchaseAmount
            }, workHours, whiteListHours, 0);
            setCustomizeInfoResult.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);

            // Check staking amount
            stakeForRequestsResult = _nftMarketContract.StakeForRequests(symbol, -1);
            stakeForRequestsResult.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            stakeForRequestsResult.Error.ShouldContain("Invalid staking amount.");

            stakeForRequestsResult = _nftMarketContract.StakeForRequests(symbol, 0);
            stakeForRequestsResult.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            stakeForRequestsResult.Error.ShouldContain("Invalid staking amount.");

            var balance = _tokenContract.GetUserBalance(InitAccount, purchaseSymbol);
            stakeForRequestsResult = _nftMarketContract.StakeForRequests(symbol, balance + 1);
            stakeForRequestsResult.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            stakeForRequestsResult.Error.ShouldContain("Insufficient balance of");

            stakeForRequestsResult = _nftMarketContract.StakeForRequests(symbol, stakingAmount);
            stakeForRequestsResult.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);
            var stakeTokens = _nftMarketContract.GetStakingTokens(symbol);
            stakeTokens.Amount.ShouldBe(stakingAmount);

            // Check symbol
            stakeForRequestsResult = _nftMarketContract.StakeForRequests("CO12345678", stakingAmount);
            stakeForRequestsResult.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            stakeForRequestsResult.Error.ShouldContain("NFT Protocol not found.");

            // Withdraw staking tokens
            var withdrawStakingTokens = _nftMarketContract.WithdrawStakingTokens(symbol, -1);
            withdrawStakingTokens.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            withdrawStakingTokens.Error.ShouldContain("Invalid withdraw amount.");

            withdrawStakingTokens = _nftMarketContract.WithdrawStakingTokens(symbol, 0);
            withdrawStakingTokens.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            withdrawStakingTokens.Error.ShouldContain("Invalid withdraw amount.");

            withdrawStakingTokens = _nftMarketContract.WithdrawStakingTokens(symbol, stakingAmount + 1);
            withdrawStakingTokens.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            withdrawStakingTokens.Error.ShouldContain("Insufficient staking amount.");

            withdrawStakingTokens = _nftMarketContract.WithdrawStakingTokens("CO12345678", stakingAmount + 1);
            withdrawStakingTokens.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            // withdrawStakingTokens.Error.ShouldContain("");

            stakeTokens = _nftMarketContract.GetStakingTokens(symbol);
            stakeTokens.Amount.ShouldBe(stakingAmount);
            Logger.Info($"stakeTokens is {stakeTokens}");

            var balanceBefore = _tokenContract.GetUserBalance(InitAccount, purchaseSymbol);
            withdrawStakingTokens = _nftMarketContract.WithdrawStakingTokens(symbol, stakingAmount);
            withdrawStakingTokens.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);
            stakeTokens = _nftMarketContract.GetStakingTokens(symbol);
            Logger.Info($"stakeTokens is {stakeTokens}");
            var balanceAfter = _tokenContract.GetUserBalance(InitAccount, purchaseSymbol);
            (balanceAfter - balanceBefore).ShouldBe(stakingAmount);
            stakeTokens.Amount.ShouldBe(0);

            // Check creator
            _nftMarketContract.SetAccount(OtherAccount);
            stakeForRequestsResult = _nftMarketContract.StakeForRequests(symbol, stakingAmount);
            stakeForRequestsResult.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            stakeForRequestsResult.Error.ShouldContain("Only NFT Protocol Creator can stake for requests.");

            withdrawStakingTokens = _nftMarketContract.WithdrawStakingTokens(symbol, stakingAmount);
            withdrawStakingTokens.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            withdrawStakingTokens.Error.ShouldContain("Only NFT Protocol Creator can withdraw.");
        }

        [TestMethod]
        public void DealWithNotListedTest()
        {
            // ListWithFixedPrice
            var startTime = DateTime.UtcNow.AddSeconds(10).ToTimestamp();
            var tokenId = 1;
            var makeOffAmount = 10;
            var purchaseSymbol = "USDT";
            var purchaseAmount = 600000000;
            var dealAmount = 2;
            var expireTime = startTime.AddHours(1);
            var serviceFeeReceiver = WhiteListAddress2;
            var royaltyFeeReceiver = WhiteListAddress3;
            var symbol = CreateAndMint(10000, 1000, tokenId);
            // Initialize
            ContractInitialize();

            // Set royalty
            var setRoyaltyResult = _nftMarketContract.SetRoyalty(symbol, tokenId, 50, royaltyFeeReceiver);
            setRoyaltyResult.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);
            var royaltyInfo = _nftMarketContract.GetRoyalty(symbol, tokenId);
            royaltyInfo.Royalty.ShouldBe(50);

            // Not listed
            _nftMarketContract.SetAccount(BuyerAccount);
            var makeOffer = _nftMarketContract.MakeOffer(
                symbol,
                tokenId,
                InitAccount,
                makeOffAmount,
                new Price
                {
                    Symbol = purchaseSymbol,
                    Amount = purchaseAmount
                    // TokenId = tokenId
                },
                expireTime
            );
            makeOffer.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            var offerList = _nftMarketContract.GetOfferList(symbol, tokenId, BuyerAccount);
            offerList.Value.Count.ShouldBe(1);
            offerList.Value[0].From.ShouldBe(BuyerAccount.ConvertAddress());
            offerList.Value[0].To.ShouldBe(InitAccount.ConvertAddress());
            offerList.Value[0].Price.Symbol.ShouldBe(purchaseSymbol);
            offerList.Value[0].Price.TokenId.ShouldBe(0);
            offerList.Value[0].Price.Amount.ShouldBe(purchaseAmount);
            offerList.Value[0].ExpireTime.ShouldBe(expireTime);
            offerList.Value[0].Quantity.ShouldBe(makeOffAmount);

            _nftMarketContract.SetAccount(InitAccount);
            // Approve
            var approve =
                _nftContract.Approve(_nftMarketContract.ContractAddress, symbol, tokenId, 1000000);
            approve.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);
            _nftMarketContract.SetAccount(BuyerAccount);
            var approveResult =
                _tokenContract.ApproveToken(BuyerAccount, _nftMarketContract.ContractAddress, 10000000000_00000000,
                    purchaseSymbol);
            approveResult.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);

            var serviceFeeReceiverBalanceBefore = _tokenContract.GetUserBalance(serviceFeeReceiver, purchaseSymbol);
            var royaltyFeeReceiverBalanceBefore = _tokenContract.GetUserBalance(royaltyFeeReceiver, purchaseSymbol);
            Logger.Info($"serviceFeeReceiverBalanceBefore is {serviceFeeReceiverBalanceBefore}");
            Logger.Info($"royaltyFeeReceiverBalanceBefore is {royaltyFeeReceiverBalanceBefore}");

            _nftMarketContract.SetAccount(InitAccount);
            var deal = _nftMarketContract.Deal(
                symbol,
                tokenId,
                BuyerAccount,
                new Price
                {
                    Symbol = purchaseSymbol,
                    Amount = purchaseAmount
                },
                dealAmount
            );
            deal.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            offerList = _nftMarketContract.GetOfferList(symbol, tokenId, BuyerAccount);
            offerList.Value.Count.ShouldBe(1);
            offerList.Value[0].From.ShouldBe(BuyerAccount.ConvertAddress());
            offerList.Value[0].To.ShouldBe(InitAccount.ConvertAddress());
            offerList.Value[0].Price.Symbol.ShouldBe(purchaseSymbol);
            offerList.Value[0].Price.TokenId.ShouldBe(0);
            offerList.Value[0].Price.Amount.ShouldBe(purchaseAmount);
            offerList.Value[0].ExpireTime.ShouldBe(expireTime);
            offerList.Value[0].Quantity.ShouldBe(makeOffAmount);

            // Check service fee and royalty
            var serviceFeeReceiverBalanceAfter = _tokenContract.GetUserBalance(serviceFeeReceiver, purchaseSymbol);
            var royaltyFeeReceiverBalanceAfter = _tokenContract.GetUserBalance(royaltyFeeReceiver, purchaseSymbol);
            var expectServiceFee = 1200000;
            var expectRoyalty = 6000000;
            Logger.Info($"serviceFeeReceiverBalanceAfter is {serviceFeeReceiverBalanceAfter}");
            Logger.Info($"royaltyFeeReceiverBalanceAfter is {royaltyFeeReceiverBalanceAfter}");
            (serviceFeeReceiverBalanceAfter - serviceFeeReceiverBalanceBefore).ShouldBe(expectServiceFee);
            (royaltyFeeReceiverBalanceAfter - royaltyFeeReceiverBalanceBefore).ShouldBe(expectRoyalty);
        }

        [TestMethod]
        public void DealWithListWithFixedPriceTest()
        {
            var startTime = DateTime.UtcNow.AddSeconds(10).ToTimestamp();
            var tokenId = 1;
            var makeOffAmount = 10;
            var purchaseSymbol = "USDT";
            var purchaseAmount = 6_00000000;
            var dealAmount = 5;
            var expireTime = startTime.AddHours(1);
            var serviceFeeReceiver = WhiteListAddress2;
            var royaltyFeeReceiver = WhiteListAddress3;
            var symbol = CreateAndMint(10000, 1000, tokenId);
            // Initialize
            ContractInitialize();

            // Set royalty
            var setRoyaltyResult = _nftMarketContract.SetRoyalty(symbol, tokenId, 50, royaltyFeeReceiver);
            setRoyaltyResult.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);
            var royaltyInfo = _nftMarketContract.GetRoyalty(symbol, tokenId);
            royaltyInfo.Royalty.ShouldBe(50);

            // List with fixed price
            var sellAmount = 100;
            var fixedPrice = 10_00000000;
            var whitePrice1 = 9_00000000;
            var whitePrice2 = 10_00000000;
            var whitePrice3 = 11_00000000;
            var isMerge = true;
            var publicTime = DateTime.UtcNow.AddSeconds(60).ToTimestamp();
            var durationHours = 48;
            ListWithFixedPrice(symbol, tokenId, sellAmount, fixedPrice, whitePrice1, whitePrice2,
                whitePrice3, startTime, publicTime, durationHours, purchaseSymbol, isMerge);

            _nftMarketContract.SetAccount(BuyerAccount);
            var makeOffer = _nftMarketContract.MakeOffer(
                symbol,
                tokenId,
                InitAccount,
                makeOffAmount,
                new Price
                {
                    Symbol = purchaseSymbol,
                    Amount = fixedPrice - 1_00000000
                },
                expireTime
            );
            makeOffer.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            var offerList = _nftMarketContract.GetOfferList(symbol, tokenId, BuyerAccount);
            offerList.Value.Count.ShouldBe(1);
            offerList.Value[0].From.ShouldBe(BuyerAccount.ConvertAddress());
            offerList.Value[0].To.ShouldBe(InitAccount.ConvertAddress());
            offerList.Value[0].Price.Symbol.ShouldBe(purchaseSymbol);
            offerList.Value[0].Price.TokenId.ShouldBe(0);
            offerList.Value[0].Price.Amount.ShouldBe(purchaseAmount);
            offerList.Value[0].ExpireTime.ShouldBe(expireTime);
            offerList.Value[0].Quantity.ShouldBe(makeOffAmount);

            _nftMarketContract.SetAccount(InitAccount);
            // Approve
            var approve =
                _nftContract.Approve(_nftMarketContract.ContractAddress, symbol, tokenId, 1000000);
            approve.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);
            _nftMarketContract.SetAccount(BuyerAccount);
            var approveResult =
                _tokenContract.ApproveToken(BuyerAccount, _nftMarketContract.ContractAddress, 10000000000_00000000,
                    purchaseSymbol);
            approveResult.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);

            var serviceFeeReceiverBalanceBefore = _tokenContract.GetUserBalance(serviceFeeReceiver, purchaseSymbol);
            var royaltyFeeReceiverBalanceBefore = _tokenContract.GetUserBalance(royaltyFeeReceiver, purchaseSymbol);
            Logger.Info($"royaltyFeeReceiverBalanceBefore is {royaltyFeeReceiverBalanceBefore}");

            _nftMarketContract.SetAccount(InitAccount);
            var deal = _nftMarketContract.Deal(
                symbol,
                tokenId,
                BuyerAccount,
                new Price
                {
                    Symbol = purchaseSymbol,
                    Amount = purchaseAmount
                },
                dealAmount
            );
            deal.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            offerList = _nftMarketContract.GetOfferList(symbol, tokenId, InitAccount);
            offerList.Value.Count.ShouldBe(0);

            // Check service fee and royalty
            var serviceFeeReceiverBalanceAfter = _tokenContract.GetUserBalance(InitAccount, purchaseSymbol);
            var royaltyFeeReceiverBalanceAfter = _tokenContract.GetUserBalance(royaltyFeeReceiver, purchaseSymbol);
            var expectServiceFee = purchaseAmount.Mul(10).Div(10000);
            var expectRoyalty = purchaseAmount.Mul(royaltyInfo.Royalty).Div(10000);
            Logger.Info($"royaltyFeeReceiverBalanceAfter is {royaltyFeeReceiverBalanceAfter}");
            Logger.Info($"expectServiceFee is {expectServiceFee}");
            Logger.Info($"expectRoyalty is {expectRoyalty}");
            (royaltyFeeReceiverBalanceAfter - royaltyFeeReceiverBalanceBefore).ShouldBe(expectRoyalty);
            (serviceFeeReceiverBalanceAfter - serviceFeeReceiverBalanceBefore).ShouldBe(expectServiceFee);
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

        public void ContractInitialize()
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
    }
}