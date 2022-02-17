using System;
using System.Linq;
using System.Threading;
using AElf.Client.Dto;
using AElf.Contracts.NFT;
using AElf.Contracts.NFTMarket;
using AElf.CSharp.Core.Extension;
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
            ContractInitialize();
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

            var listWithFixedPriceResult = ListWithFixedPrice(symbol, tokenId, sellAmount, fixedPrice, whitePrice1,
                whitePrice2,
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

            // Check event
            var logs = listWithFixedPriceResult.Logs.First(l => l.Name.Equals("FixedPriceNFTListed")).NonIndexed;
            var byteString = ByteString.FromBase64(logs);
            var fixedPriceLogs = FixedPriceNFTListed.Parser.ParseFrom(byteString);
            fixedPriceLogs.Owner.ShouldBe(InitAccount.ConvertAddress());
            fixedPriceLogs.Price.Symbol.ShouldBe(purchaseSymbol);
            fixedPriceLogs.Price.Amount.ShouldBe(fixedPrice);
            fixedPriceLogs.Quantity.ShouldBe(sellAmount);
            fixedPriceLogs.Symbol.ShouldBe(symbol);
            fixedPriceLogs.TokenId.ShouldBe(tokenId);
            fixedPriceLogs.Duration.StartTime.ShouldBe(startTime);
            fixedPriceLogs.Duration.PublicTime.ShouldBe(publicTime);
            fixedPriceLogs.Duration.DurationHours.ShouldBe(durationHours);
            fixedPriceLogs.IsMergedToPreviousListedInfo.ShouldBe(false);
            fixedPriceLogs.WhiteListAddressPriceList.Value[0].Address
                .ShouldBe(WhiteListAddress1.ConvertAddress());
            fixedPriceLogs.WhiteListAddressPriceList.Value[0].Price.Symbol.ShouldBe(purchaseSymbol);
            fixedPriceLogs.WhiteListAddressPriceList.Value[0].Price.Amount.ShouldBe(whitePrice1);
            fixedPriceLogs.WhiteListAddressPriceList.Value[1].Address
                .ShouldBe(WhiteListAddress2.ConvertAddress());
            fixedPriceLogs.WhiteListAddressPriceList.Value[1].Price.Symbol.ShouldBe(purchaseSymbol);
            fixedPriceLogs.WhiteListAddressPriceList.Value[1].Price.Amount.ShouldBe(whitePrice2);
            fixedPriceLogs.WhiteListAddressPriceList.Value[2].Address
                .ShouldBe(WhiteListAddress3.ConvertAddress());
            fixedPriceLogs.WhiteListAddressPriceList.Value[2].Price.Symbol.ShouldBe(purchaseSymbol);
            fixedPriceLogs.WhiteListAddressPriceList.Value[2].Price.Amount.ShouldBe(whitePrice3);
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
        [DataRow(1, "ELF", true, 49)]
        [DataRow(2, "ELF", true, 24)]
        [DataRow(3, "USDT", true, 24)]
        [DataRow(4, "ELF", true, 24)]
        [DataRow(5, "ELF", false, 24)]
        [DataRow(6, "ELF", false, 30)]
        public void ListWithFixedPriceAgainTest(int times, string purchaseSymbol, bool isMerge,
            int durationHours)
        {
            // NFTContract: 2qdf5ArPmD7AWTy8LsPv7giAVRrB59aLYm4adZnfMk4FHGGoko
            // NFTMarketContract: Qr6cJSLiLoTQsuVf6aPwHXJK438V99HxD8ZU9x3ZPQ14GqWF3
            var symbol = "CO913655869";
            var tokenId = 2;
            var sellAmount = 100;
            var fixedPrice = 10_00000000;
            var whitePrice1 = 9_00000000;
            var whitePrice2 = 10_00000000;
            var whitePrice3 = 11_00000000;

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

            ListWithFixedPrice(symbol, tokenId, sellAmount, fixedPrice, whitePrice1, whitePrice2,
                whitePrice3, startTime, publicTime, durationHours, purchaseSymbol, isMerge);

            var listedNftInfo =
                _nftMarketContract.GetListedNFTInfoList(symbol, tokenId, InitAccount);

            switch (times)
            {
                case 1:
                    listedNftInfo.Value[0].Quantity.ShouldBe(sellAmount * 2);
                    listedNftInfo.Value[0].Duration.StartTime.Seconds.ShouldBe(startTime.Seconds);
                    listedNftInfo.Value[0].Duration.PublicTime.Seconds.ShouldBe(publicTime.Seconds);
                    listedNftInfo.Value[0].Duration.DurationHours.ShouldBe(durationHours);
                    break;
                case 2:
                    listedNftInfo.Value[0].Quantity.ShouldBe(sellAmount * 3);
                    listedNftInfo.Value[0].Duration.StartTime.Seconds.ShouldBe(startTime.Seconds);
                    listedNftInfo.Value[0].Duration.PublicTime.Seconds.ShouldBe(publicTime.Seconds);
                    listedNftInfo.Value[0].Duration.DurationHours.ShouldBe(durationHours);
                    break;
                case 3:
                    listedNftInfo.Value[1].Quantity.ShouldBe(sellAmount);
                    listedNftInfo.Value[1].Duration.StartTime.Seconds.ShouldBe(startTime.Seconds);
                    listedNftInfo.Value[1].Duration.PublicTime.Seconds.ShouldBe(publicTime.Seconds);
                    listedNftInfo.Value[1].Duration.DurationHours.ShouldBe(durationHours);
                    break;
                case 4:
                    listedNftInfo.Value[2].Quantity.ShouldBe(sellAmount);
                    listedNftInfo.Value[2].Duration.StartTime.Seconds.ShouldBe(startTime.Seconds);
                    listedNftInfo.Value[2].Duration.PublicTime.Seconds.ShouldBe(publicTime.Seconds);
                    listedNftInfo.Value[2].Duration.DurationHours.ShouldBe(durationHours);
                    break;
                case 5:
                    listedNftInfo.Value[0].Quantity.ShouldBe(sellAmount * 4);
                    listedNftInfo.Value[0].Duration.StartTime.Seconds.ShouldBe(startTime.Seconds);
                    listedNftInfo.Value[0].Duration.PublicTime.Seconds.ShouldBe(publicTime.Seconds);
                    listedNftInfo.Value[0].Duration.DurationHours.ShouldBe(durationHours);
                    break;
                case 6:
                    listedNftInfo.Value[3].Quantity.ShouldBe(sellAmount);
                    listedNftInfo.Value[3].Duration.StartTime.Seconds.ShouldBe(startTime.Seconds);
                    listedNftInfo.Value[3].Duration.PublicTime.Seconds.ShouldBe(publicTime.Seconds);
                    listedNftInfo.Value[3].Duration.DurationHours.ShouldBe(durationHours);
                    break;
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

            // Check allowance
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
            var totalSupply = 1;
            var mintAmount = 1;
            var startingPrice = 10;
            var purchaseSymbol = "ELF";
            var startTime = DateTime.UtcNow.AddHours(12).ToTimestamp();
            var durationHours = 48;
            var earnestMoney = startingPrice;
            var symbol = CreateAndMintUnReuse(totalSupply, mintAmount, tokenId);

            ListWithEnglistAuction(symbol, tokenId, startingPrice, purchaseSymbol,
                startTime, durationHours, earnestMoney);

            CheckEnglishAuctionInfo(symbol, tokenId, new EnglishAuctionInfo
            {
                Symbol = symbol,
                TokenId = tokenId,
                StartingPrice = startingPrice,
                PurchaseSymbol = purchaseSymbol,
                Duration = new ListDuration
                {
                    StartTime = startTime,
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
            var totalSupply = 1;
            var mintAmount = 1;
            var startingPrice = 10_00000000;
            var purchaseSymbol = "ELF";
            var earnestMoney = startingPrice;
            var symbol = CreateAndMintUnReuse(totalSupply, mintAmount, tokenId);

            // Check allowance
            var listWithEnglishAuctionResult = _nftMarketContract.ListWithEnglishAuction(
                symbol,
                tokenId,
                startingPrice,
                purchaseSymbol,
                new ListDuration(),
                earnestMoney
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
                earnestMoney
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
                earnestMoney
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
                earnestMoney
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
                earnestMoney
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
                earnestMoney + 1
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
                earnestMoney
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
                earnestMoney
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
                earnestMoney
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
            var totalSupply = 1;
            var mintAmount = 1;
            var startingPrice = 10_00000000;
            var endingPrice = 1_00000000;
            var purchaseSymbol = "ELF";
            var startTime = DateTime.UtcNow.AddHours(12).ToTimestamp();
            var durationHours = 48;
            var symbol = CreateAndMintUnReuse(totalSupply, mintAmount, tokenId);

            ListWithDutchAuction(symbol, tokenId, startingPrice, endingPrice, purchaseSymbol,
                startTime, durationHours);

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
                    DurationHours = durationHours
                },
                Owner = InitAccount.ConvertAddress()
            });
        }

        [TestMethod]
        public void ListWithDutchAuctionErrorTest()
        {
            var tokenId = 1;
            var totalSupply = 1;
            var mintAmount = 1;
            var startingPrice = 10_00000000;
            var endingPrice = 1_00000000;
            var purchaseSymbol = "ELF";
            var symbol = CreateAndMintUnReuse(totalSupply, mintAmount, tokenId);

            // Check allowance
            var listWithDutchAuctionResult = _nftMarketContract.ListWithDutchAuction(
                symbol,
                tokenId,
                startingPrice,
                endingPrice,
                purchaseSymbol,
                new ListDuration()
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
                new ListDuration()
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
                new ListDuration()
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
                new ListDuration()
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
                new ListDuration()
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
                new ListDuration()
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
                new ListDuration()
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
                new ListDuration()
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
                new ListDuration()
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
                new ListDuration()
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
                new ListDuration()
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
                new ListDuration()
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
                earnestMoney
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
                new ListDuration()
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
        public void SetGlobalTokenWhiteListTest()
        {
            var totalSupply = 1000;
            var mintAmount = 1000;
            var tokenId = 1;
            var makeOffAmount = 2;
            var purchaseSymbol = "USDT";
            var purchaseAmount = 10_00000000;
            var symbol = CreateAndMint(totalSupply, mintAmount, tokenId);

            var globalTokenWhiteList = _nftMarketContract.GetGlobalTokenWhiteList();
            var tokenWhiteList = _nftMarketContract.GetTokenWhiteList(symbol);
            globalTokenWhiteList.Value.Count.ShouldBe(1);
            globalTokenWhiteList.Value.ShouldContain("ELF");
            tokenWhiteList.Value.Count.ShouldBe(1);
            tokenWhiteList.Value.ShouldContain("ELF");

            // Make offer
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
                DateTime.UtcNow.ToTimestamp().AddHours(1)
            );
            makeOffer.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            _nftMarketContract.SetAccount(InitAccount);
            var setGlobalTokenWhiteResult = _nftMarketContract.SetGlobalTokenWhiteList(new StringList
            {
                Value = {"USDT"}
            });
            setGlobalTokenWhiteResult.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);
            globalTokenWhiteList = _nftMarketContract.GetGlobalTokenWhiteList();
            tokenWhiteList = _nftMarketContract.GetTokenWhiteList(symbol);
            globalTokenWhiteList.Value.Count.ShouldBe(2);
            globalTokenWhiteList.Value.ShouldContain("ELF");
            globalTokenWhiteList.Value.ShouldContain("USDT");
            tokenWhiteList.Value.Count.ShouldBe(2);
            tokenWhiteList.Value.ShouldContain("ELF");
            tokenWhiteList.Value.ShouldContain("USDT");

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

            // Deal for the first time
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
                1
            );
            deal.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
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

            // Check tokenIdReuse
            var setCustomizeInfoResult = _nftMarketContract.SetCustomizeInfo(symbol, depositRate, new Price
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

            // Check symbol
            var handleRequestResult = _nftMarketContract.HandleRequest("CO12345678", tokenId, InitAccount, isConfirm);
            handleRequestResult.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            handleRequestResult.Error.ShouldContain("Request not exists.");

            // Check tokenId
            handleRequestResult = _nftMarketContract.HandleRequest(symbol, 100, InitAccount, isConfirm);
            handleRequestResult.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            handleRequestResult.Error.ShouldContain("Request not exists.");

            // Check Request
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

            // Approve
            var approveResult =
                _tokenContract.ApproveToken(InitAccount, _nftMarketContract.ContractAddress, 10000000000_00000000,
                    purchaseSymbol);
            approveResult.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);

            // Customize info not found
            var stakeForRequestsResult = _nftMarketContract.StakeForRequests(symbol, stakingAmount);
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
            withdrawStakingTokens.Error.ShouldContain("NFT symbol does not exist.");

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
            withdrawStakingTokens.Error.ShouldContain("NFT symbol does not exist.");
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
            var dealAmount1 = 2;
            var dealAmount2 = 8;
            var expireTime = startTime.AddHours(1);
            var serviceFeeReceiver = WhiteListAddress2;
            var royaltyFeeReceiver = WhiteListAddress3;
            var symbol = CreateAndMint(10000, 1000, tokenId);

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

            // Deal for the first time
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
                dealAmount1
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
            offerList.Value[0].Quantity.ShouldBe(makeOffAmount - dealAmount1);

            // Check service fee and royalty
            var serviceFeeReceiverBalanceAfter = _tokenContract.GetUserBalance(serviceFeeReceiver, purchaseSymbol);
            var royaltyFeeReceiverBalanceAfter = _tokenContract.GetUserBalance(royaltyFeeReceiver, purchaseSymbol);
            var expectServiceFee = 1200000;
            var expectRoyalty = 6000000;
            Logger.Info($"serviceFeeReceiverBalanceAfter is {serviceFeeReceiverBalanceAfter}");
            Logger.Info($"royaltyFeeReceiverBalanceAfter is {royaltyFeeReceiverBalanceAfter}");
            (serviceFeeReceiverBalanceAfter - serviceFeeReceiverBalanceBefore).ShouldBe(expectServiceFee);
            (royaltyFeeReceiverBalanceAfter - royaltyFeeReceiverBalanceBefore).ShouldBe(expectRoyalty);

            // Deal for the second time
            deal = _nftMarketContract.Deal(
                symbol,
                tokenId,
                BuyerAccount,
                new Price
                {
                    Symbol = purchaseSymbol,
                    Amount = purchaseAmount
                },
                dealAmount2
            );
            deal.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            offerList = _nftMarketContract.GetOfferList(symbol, tokenId, BuyerAccount);
            offerList.Value.Count.ShouldBe(0);

            // Check service fee and royalty
            var serviceFeeReceiverBalanceAfter1 = _tokenContract.GetUserBalance(serviceFeeReceiver, purchaseSymbol);
            var royaltyFeeReceiverBalanceAfter1 = _tokenContract.GetUserBalance(royaltyFeeReceiver, purchaseSymbol);
            expectServiceFee = 4800000;
            expectRoyalty = 24000000;
            Logger.Info($"serviceFeeReceiverBalanceAfter1 is {serviceFeeReceiverBalanceAfter1}");
            Logger.Info($"royaltyFeeReceiverBalanceAfter1 is {serviceFeeReceiverBalanceAfter1}");
            (serviceFeeReceiverBalanceAfter1 - serviceFeeReceiverBalanceAfter).ShouldBe(expectServiceFee);
            (royaltyFeeReceiverBalanceAfter1 - royaltyFeeReceiverBalanceAfter).ShouldBe(expectRoyalty);
        }

        [TestMethod]
        public void DealErrorTest()
        {
            // ListWithFixedPrice
            var startTime = DateTime.UtcNow.AddSeconds(10).ToTimestamp();
            var tokenId = 1;
            var makeOffAmount = 10;
            var purchaseSymbol = "USDT";
            var purchaseAmount = 600000000;
            var expireTime = startTime.AddHours(1);
            var symbol = CreateAndMint(10000, 1000, tokenId);

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

            // Check sender NFT balance
            var deal = _nftMarketContract.Deal(
                symbol,
                tokenId,
                BuyerAccount,
                new Price
                {
                    Symbol = purchaseSymbol,
                    Amount = purchaseAmount
                },
                makeOffAmount
            );
            deal.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
            deal.Error.ShouldContain("Check sender NFT balance failed.");

            _nftMarketContract.SetAccount(InitAccount);
            // Check sender NFT allowance
            deal = _nftMarketContract.Deal(
                symbol,
                tokenId,
                BuyerAccount,
                new Price
                {
                    Symbol = purchaseSymbol,
                    Amount = purchaseAmount
                },
                makeOffAmount
            );
            deal.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
            deal.Error.ShouldContain("Check sender NFT allowance failed.");

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

            // Check deal amount
            _nftMarketContract.SetAccount(InitAccount);
            deal = _nftMarketContract.Deal(
                symbol,
                tokenId,
                BuyerAccount,
                new Price
                {
                    Symbol = purchaseSymbol,
                    Amount = purchaseAmount
                },
                makeOffAmount + 1
            );
            deal.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
            deal.Error.ShouldContain("Offer quantity exceeded.");

            // Check purchase symbol
            deal = _nftMarketContract.Deal(
                symbol,
                tokenId,
                BuyerAccount,
                new Price
                {
                    Symbol = "CO12345678",
                    Amount = purchaseAmount
                },
                makeOffAmount
            );
            deal.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
            deal.Error.ShouldContain("Neither related offer nor bid are found.");

            // Check purchase amount
            deal = _nftMarketContract.Deal(
                symbol,
                tokenId,
                BuyerAccount,
                new Price
                {
                    Symbol = purchaseSymbol,
                    Amount = purchaseAmount + 1
                },
                makeOffAmount
            );
            deal.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
            deal.Error.ShouldContain("Neither related offer nor bid are found.");

            // Check symbol
            deal = _nftMarketContract.Deal(
                "CO12345678",
                tokenId,
                BuyerAccount,
                new Price
                {
                    Symbol = purchaseSymbol,
                    Amount = purchaseAmount
                },
                makeOffAmount
            );
            deal.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
            deal.Error.ShouldContain("Check sender NFT balance failed.");

            // Check offer from
            deal = _nftMarketContract.Deal(
                symbol,
                tokenId,
                OtherAccount,
                new Price
                {
                    Symbol = purchaseSymbol,
                    Amount = purchaseAmount
                },
                makeOffAmount
            );
            deal.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
            deal.Error.ShouldContain("Neither related offer nor bid are found.");

            // Make offer
            _nftMarketContract.SetAccount(BuyerAccount);
            makeOffer = _nftMarketContract.MakeOffer(
                symbol,
                tokenId,
                InitAccount,
                makeOffAmount,
                new Price
                {
                    Symbol = purchaseSymbol,
                    Amount = purchaseAmount + 1
                    // TokenId = tokenId
                },
                DateTime.UtcNow.ToTimestamp()
            );
            makeOffer.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            // Check purchase amount
            Thread.Sleep(10 * 1000);
            _nftMarketContract.SetAccount(InitAccount);
            deal = _nftMarketContract.Deal(
                symbol,
                tokenId,
                BuyerAccount,
                new Price
                {
                    Symbol = purchaseSymbol,
                    Amount = purchaseAmount + 1
                },
                makeOffAmount
            );
            deal.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
            deal.Error.ShouldContain("Neither related offer nor bid are found.");
        }

        [TestMethod]
        public void DealWithListWithFixedPriceTest()
        {
            // ListWithFixedPrice
            var startTime = DateTime.UtcNow.AddSeconds(10).ToTimestamp();
            var tokenId = 1;
            var makeOfferAmount = 8;
            var purchaseSymbol = "USDT";
            var purchaseAmount = 6_00000000;
            var dealAmount = 2;
            var expireTime = startTime.AddHours(1);
            var serviceFeeReceiver = InitAccount;
            var royaltyFeeReceiver = WhiteListAddress3;
            var symbol = CreateAndMint(10000, 10, tokenId);
            var sellAmount = 5;
            var fixedPrice = 10_00000000;
            var whitePrice1 = 9_00000000;
            var whitePrice2 = 10_00000000;
            var whitePrice3 = 11_00000000;
            var isMerge = true;

            var publicTime = DateTime.UtcNow.AddSeconds(60).ToTimestamp();
            var durationHours = 48;
            ListWithFixedPrice(symbol, tokenId, sellAmount, fixedPrice, whitePrice1, whitePrice2,
                whitePrice3, startTime, publicTime, durationHours, purchaseSymbol, isMerge);

            // Set service
            var serviceFeeResult =
                _nftMarketContract.ExecuteMethodWithResult(NFTMarketContractMethod.SetServiceFee, new SetServiceFeeInput
                {
                    ServiceFeeRate = 20
                });
            serviceFeeResult.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);
            var serviceFeeInfo = _nftMarketContract.GetServiceFeeInfo();
            serviceFeeInfo.ServiceFeeRate.ShouldBe(20);

            // Set royalty
            var setRoyaltyResult = _nftMarketContract.SetRoyalty(symbol, tokenId, 50, royaltyFeeReceiver);
            setRoyaltyResult.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);
            var royaltyInfo = _nftMarketContract.GetRoyalty(symbol, tokenId);
            royaltyInfo.Royalty.ShouldBe(50);

            // Make offer
            _nftMarketContract.SetAccount(BuyerAccount);
            var makeOffer = _nftMarketContract.MakeOffer(
                symbol,
                tokenId,
                InitAccount,
                makeOfferAmount,
                new Price
                {
                    Symbol = purchaseSymbol,
                    Amount = purchaseAmount
                    // TokenId = tokenId
                },
                expireTime
            );
            makeOffer.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

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

            // Deal failed
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
                makeOfferAmount - 1
            );
            deal.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
            deal.Error.ShouldContain("Need to delist at least 2 NFT(s) before deal.");

            var serviceFeeReceiverBalanceBefore = _tokenContract.GetUserBalance(serviceFeeReceiver, purchaseSymbol);
            var royaltyFeeReceiverBalanceBefore = _tokenContract.GetUserBalance(royaltyFeeReceiver, purchaseSymbol);
            var sellerBalanceBefore = _tokenContract.GetUserBalance(InitAccount, purchaseSymbol);

            // Deal
            deal = _nftMarketContract.Deal(
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

            var offerList = _nftMarketContract.GetOfferList(symbol, tokenId, BuyerAccount);
            offerList.Value.Count.ShouldBe(1);
            offerList.Value[0].From.ShouldBe(BuyerAccount.ConvertAddress());
            offerList.Value[0].To.ShouldBe(InitAccount.ConvertAddress());
            offerList.Value[0].Price.Symbol.ShouldBe(purchaseSymbol);
            offerList.Value[0].Price.TokenId.ShouldBe(0);
            offerList.Value[0].Price.Amount.ShouldBe(purchaseAmount);
            offerList.Value[0].ExpireTime.ShouldBe(expireTime);
            offerList.Value[0].Quantity.ShouldBe(makeOfferAmount - dealAmount);

            // Check service fee and royalty
            var serviceFeeReceiverBalanceAfter = _tokenContract.GetUserBalance(serviceFeeReceiver, purchaseSymbol);
            var royaltyFeeReceiverBalanceAfter = _tokenContract.GetUserBalance(royaltyFeeReceiver, purchaseSymbol);
            var sellerBalanceAfter = _tokenContract.GetUserBalance(InitAccount, purchaseSymbol);
            var expectServiceFee = 2400000;
            var expectRoyaltyFee = 6000000;
            var expectSellerBalance =
                purchaseAmount * dealAmount - expectServiceFee - expectRoyaltyFee + expectServiceFee;
            (royaltyFeeReceiverBalanceAfter - royaltyFeeReceiverBalanceBefore).ShouldBe(expectRoyaltyFee);
            (sellerBalanceAfter - sellerBalanceBefore).ShouldBe(expectSellerBalance);
        }

        [TestMethod]
        [DataRow("offerList", 6_00000000)]
        [DataRow("bidList", 12_00000000)]
        public void DealWithListWithEnglistAuctionTest(string listType, long purchaseAmount)
        {
            var startTime = DateTime.UtcNow.AddSeconds(10).ToTimestamp();
            var tokenId = 1;
            var makeOffAmount = 1;
            var purchaseSymbol = "USDT";
            var dealAmount = 1;
            var expireTime = startTime.AddHours(1);
            var serviceFeeReceiver = WhiteListAddress2;
            var royaltyFeeReceiver = WhiteListAddress3;
            var symbol = CreateAndMintUnReuse(1, 1, tokenId);
            var startingPrice = 10_00000000;
            var earnestMoney = 5_00000000;
            var durationHours = 48;

            ListWithEnglistAuction(symbol, tokenId, startingPrice, purchaseSymbol,
                startTime, durationHours, earnestMoney);

            // Set service
            var serviceFeeResult = _nftMarketContract.SetServiceFee(20, serviceFeeReceiver);
            serviceFeeResult.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);

            // Set royalty
            var setRoyaltyResult = _nftMarketContract.SetRoyalty(symbol, tokenId, 50, royaltyFeeReceiver);
            setRoyaltyResult.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);
            var royaltyInfo = _nftMarketContract.GetRoyalty(symbol, tokenId);
            royaltyInfo.Royalty.ShouldBe(50);

            _nftMarketContract.SetAccount(BuyerAccount);
            var approveResult =
                _tokenContract.ApproveToken(BuyerAccount, _nftMarketContract.ContractAddress, 10000000000_00000000,
                    purchaseSymbol);
            approveResult.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);

            var buyerBalance = _tokenContract.GetUserBalance(BuyerAccount, purchaseSymbol);
            // Make offer
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
            if (listType.Equals("offerList"))
            {
                var offerList = _nftMarketContract.GetOfferList(symbol, tokenId, BuyerAccount);
                offerList.Value.Count.ShouldBe(1);
            }
            else
            {
                var bidList = _nftMarketContract.GetBidList(symbol, tokenId);
                bidList.Value.Count.ShouldBe(1);
            }

            var listedNftInfo = _nftMarketContract.GetListedNFTInfoList(symbol, tokenId, InitAccount);
            listedNftInfo.Value.Count.ShouldBe(1);

            _nftMarketContract.SetAccount(InitAccount);
            // Approve
            var approve =
                _nftContract.Approve(_nftMarketContract.ContractAddress, symbol, tokenId, 1000000);
            approve.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);
            _nftMarketContract.SetAccount(BuyerAccount);
            approveResult =
                _tokenContract.ApproveToken(InitAccount, _nftMarketContract.ContractAddress, 10000000000_00000000,
                    purchaseSymbol);
            approveResult.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);

            var serviceFeeReceiverBalanceBefore = _tokenContract.GetUserBalance(serviceFeeReceiver, purchaseSymbol);
            var royaltyFeeReceiverBalanceBefore = _tokenContract.GetUserBalance(royaltyFeeReceiver, purchaseSymbol);
            var sellerBalanceBefore = _tokenContract.GetUserBalance(InitAccount, purchaseSymbol);
            var buyerBalanceBefore = _tokenContract.GetUserBalance(BuyerAccount, purchaseSymbol);
            if (listType.Equals("offerList"))
            {
                (buyerBalance - buyerBalanceBefore).ShouldBe(0);
            }
            else
            {
                (buyerBalance - buyerBalanceBefore).ShouldBe(earnestMoney);
            }

            // Deal with offerList
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

            if (listType.Equals("offerList"))
            {
                var offerListAfter = _nftMarketContract.GetOfferList(symbol, tokenId, BuyerAccount);
                offerListAfter.Value.Count.ShouldBe(0);
            }
            else
            {
                var bidListAfter = _nftMarketContract.GetBidList(symbol, tokenId);
                bidListAfter.Value.Count.ShouldBe(0);
            }

            var englishAuctionInfo = _nftMarketContract.GetEnglishAuctionInfo(symbol, tokenId);
            englishAuctionInfo.Symbol.ShouldBe(symbol);
            listedNftInfo = _nftMarketContract.GetListedNFTInfoList(symbol, tokenId, InitAccount);
            listedNftInfo.Value.Count.ShouldBe(0);

            // Check service fee and royalty
            var serviceFeeReceiverBalanceAfter = _tokenContract.GetUserBalance(serviceFeeReceiver, purchaseSymbol);
            var royaltyFeeReceiverBalanceAfter = _tokenContract.GetUserBalance(royaltyFeeReceiver, purchaseSymbol);
            var sellerBalanceAfter = _tokenContract.GetUserBalance(InitAccount, purchaseSymbol);
            var buyerBalanceAfter = _tokenContract.GetUserBalance(BuyerAccount, purchaseSymbol);
            var expectServiceFee = 0;
            var expectRoyaltyFee = 0;
            if (listType.Equals("offerList"))
            {
                expectServiceFee = 1200000;
                expectRoyaltyFee = 3000000;
                (buyerBalanceBefore - buyerBalanceAfter).ShouldBe(purchaseAmount);
            }
            else
            {
                expectServiceFee = 2400000;
                expectRoyaltyFee = 6000000;
                (buyerBalanceBefore - buyerBalanceAfter).ShouldBe(purchaseAmount - earnestMoney);
            }

            var expectSellerBalance = purchaseAmount - expectServiceFee - expectRoyaltyFee;
            (serviceFeeReceiverBalanceAfter - serviceFeeReceiverBalanceBefore).ShouldBe(expectServiceFee);
            (royaltyFeeReceiverBalanceAfter - royaltyFeeReceiverBalanceBefore).ShouldBe(expectRoyaltyFee);
            (sellerBalanceAfter - sellerBalanceBefore).ShouldBe(expectSellerBalance);
        }

        [TestMethod]
        public void ListWithEnglistAuctionAgainTest()
        {
            var startTime = DateTime.UtcNow.AddSeconds(10).ToTimestamp();
            var tokenId = 1;
            var makeOffAmount = 1;
            var purchaseSymbol = "USDT";
            var expireTime = startTime.AddHours(1);
            var symbol = CreateAndMintUnReuse(1, 1, tokenId);
            var startingPrice = 10_00000000;
            var earnestMoney = 5_00000000;
            var durationHours = 48;
            var purchaseAmount = 12_00000000;

            ListWithEnglistAuction(symbol, tokenId, startingPrice, purchaseSymbol,
                startTime, durationHours, earnestMoney);

            _nftMarketContract.SetAccount(BuyerAccount);
            var approveResult =
                _tokenContract.ApproveToken(BuyerAccount, _nftMarketContract.ContractAddress, 10000000000_00000000,
                    purchaseSymbol);
            approveResult.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);

            var buyerBalance = _tokenContract.GetUserBalance(BuyerAccount, purchaseSymbol);
            // Make offer
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

            var bidList = _nftMarketContract.GetBidList(symbol, tokenId);
            bidList.Value.Count.ShouldBe(1);
            var listedNftInfo = _nftMarketContract.GetListedNFTInfoList(symbol, tokenId, InitAccount);
            listedNftInfo.Value.Count.ShouldBe(1);

            _nftMarketContract.SetAccount(InitAccount);
            // Approve
            var approve =
                _nftContract.Approve(_nftMarketContract.ContractAddress, symbol, tokenId, 1000000);
            approve.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);
            approveResult =
                _tokenContract.ApproveToken(InitAccount, _nftMarketContract.ContractAddress, 10000000000_00000000,
                    purchaseSymbol);
            approveResult.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);

            var buyerBalanceBefore = _tokenContract.GetUserBalance(BuyerAccount, purchaseSymbol);
            (buyerBalance - buyerBalanceBefore).ShouldBe(earnestMoney);

            var listWithEnglistAuctionResult = ListWithEnglistAuction(symbol, tokenId, startingPrice, purchaseSymbol,
                startTime, durationHours, earnestMoney);
            listWithEnglistAuctionResult.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);

            bidList = _nftMarketContract.GetBidList(symbol, tokenId);
            bidList.Value.Count.ShouldBe(0);
            listedNftInfo = _nftMarketContract.GetListedNFTInfoList(symbol, tokenId, InitAccount);
            listedNftInfo.Value.Count.ShouldBe(1);

            var buyerBalanceAfter = _tokenContract.GetUserBalance(BuyerAccount, purchaseSymbol);
            (buyerBalanceAfter - buyerBalanceBefore).ShouldBe(earnestMoney);
        }

        [TestMethod]
        public void DealWithListWithDutchAuctionTest()
        {
            var tokenId = 1;
            var totalSupply = 1;
            var mintAmount = 1;
            var startingPrice = 10_00000000;
            var endingPrice = 1_00000000;
            var purchaseSymbol = "USDT";
            var startTime = DateTime.UtcNow.AddSeconds(5).ToTimestamp();
            var durationHours = 48;
            var symbol = CreateAndMintUnReuse(totalSupply, mintAmount, tokenId);
            var serviceFeeReceiver = WhiteListAddress2;
            var royaltyFeeReceiver = WhiteListAddress3;
            var makeOffAmount = 1;
            var expireTime = DateTime.UtcNow.AddHours(48).ToTimestamp();
            var purchaseAmount = 6_00000000;
            var dealAmount = 1;

            ListWithDutchAuction(symbol, tokenId, startingPrice, endingPrice, purchaseSymbol,
                startTime, durationHours);

            // Set service
            var serviceFeeResult = _nftMarketContract.SetServiceFee(20, serviceFeeReceiver);
            serviceFeeResult.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);
            var serviceFeeInfo = _nftMarketContract.GetServiceFeeInfo();
            serviceFeeInfo.ServiceFeeRate.ShouldBe(20);
            serviceFeeInfo.ServiceFeeReceiver.ShouldBe(serviceFeeReceiver.ConvertAddress());

            // Set royalty
            var setRoyaltyResult = _nftMarketContract.SetRoyalty(symbol, tokenId, 50, royaltyFeeReceiver);
            setRoyaltyResult.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);
            var royaltyFeeInfo = _nftMarketContract.GetRoyalty(symbol, tokenId);
            royaltyFeeInfo.Royalty.ShouldBe(50);
            royaltyFeeInfo.RoyaltyFeeReceiver.ShouldBe(royaltyFeeReceiver.ConvertAddress());

            _nftMarketContract.SetAccount(BuyerAccount);
            var approveResult =
                _tokenContract.ApproveToken(BuyerAccount, _nftMarketContract.ContractAddress, 10000000000_00000000,
                    purchaseSymbol);
            approveResult.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);
            // Make offer
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

            _nftMarketContract.SetAccount(InitAccount);
            // Approve
            var approve =
                _nftContract.Approve(_nftMarketContract.ContractAddress, symbol, tokenId, 1000000);
            approve.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);
            _nftMarketContract.SetAccount(BuyerAccount);
            approveResult =
                _tokenContract.ApproveToken(InitAccount, _nftMarketContract.ContractAddress, 10000000000_00000000,
                    purchaseSymbol);
            approveResult.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);

            _nftMarketContract.SetAccount(InitAccount);
            var offerListBefore = _nftMarketContract.GetOfferList(symbol, tokenId, BuyerAccount);
            offerListBefore.Value.Count.ShouldBe(1);
            var listedNftInfo = _nftMarketContract.GetListedNFTInfoList(symbol, tokenId, InitAccount);
            listedNftInfo.Value.Count.ShouldBe(1);

            var serviceFeeReceiverBalanceBefore = _tokenContract.GetUserBalance(serviceFeeReceiver, purchaseSymbol);
            var royaltyFeeReceiverBalanceBefore = _tokenContract.GetUserBalance(royaltyFeeReceiver, purchaseSymbol);
            var sellerBalanceBefore = _tokenContract.GetUserBalance(InitAccount, purchaseSymbol);
            var buyerBalanceBefore = _tokenContract.GetUserBalance(BuyerAccount, purchaseSymbol);

            // Deal with offerList
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

            var offerListAfter = _nftMarketContract.GetOfferList(symbol, tokenId, BuyerAccount);
            offerListAfter.Value.Count.ShouldBe(0);
            var dutchAuctionInfo = _nftMarketContract.GetDutchAuctionInfo(symbol, tokenId);
            dutchAuctionInfo.Symbol.ShouldBe(symbol);
            listedNftInfo = _nftMarketContract.GetListedNFTInfoList(symbol, tokenId, InitAccount);
            listedNftInfo.Value.Count.ShouldBe(0);

            // Check service fee and royalty
            var serviceFeeReceiverBalanceAfter = _tokenContract.GetUserBalance(serviceFeeReceiver, purchaseSymbol);
            var royaltyFeeReceiverBalanceAfter = _tokenContract.GetUserBalance(royaltyFeeReceiver, purchaseSymbol);
            var sellerBalanceAfter = _tokenContract.GetUserBalance(InitAccount, purchaseSymbol);
            var buyerBalanceAfter = _tokenContract.GetUserBalance(BuyerAccount, purchaseSymbol);
            var expectServiceFee = 1200000;
            var expectRoyaltyFee = 3000000;
            var expectSellerBalance = purchaseAmount - expectServiceFee - expectRoyaltyFee;
            var expectBuyerBalance = purchaseAmount;

            (serviceFeeReceiverBalanceAfter - serviceFeeReceiverBalanceBefore).ShouldBe(expectServiceFee);
            (royaltyFeeReceiverBalanceAfter - royaltyFeeReceiverBalanceBefore).ShouldBe(expectRoyaltyFee);
            (sellerBalanceAfter - sellerBalanceBefore).ShouldBe(expectSellerBalance);
            (buyerBalanceBefore - buyerBalanceAfter).ShouldBe(expectBuyerBalance);
        }

        [TestMethod]
        public void ListWithDutchAuctionTestAgain()
        {
            var tokenId = 1;
            var totalSupply = 1;
            var mintAmount = 1;
            var startingPrice = 10_00000000;
            var endingPrice = 1_00000000;
            var purchaseSymbol = "USDT";
            var startTime = DateTime.UtcNow.AddSeconds(5).ToTimestamp();
            var durationHours = 48;
            var symbol = CreateAndMintUnReuse(totalSupply, mintAmount, tokenId);
            var makeOffAmount = 1;
            var expireTime = DateTime.UtcNow.AddHours(48).ToTimestamp();
            var purchaseAmount = 6_00000000;

            ListWithDutchAuction(symbol, tokenId, startingPrice, endingPrice, purchaseSymbol,
                startTime, durationHours);

            _nftMarketContract.SetAccount(BuyerAccount);
            var approveResult =
                _tokenContract.ApproveToken(BuyerAccount, _nftMarketContract.ContractAddress, 10000000000_00000000,
                    purchaseSymbol);
            approveResult.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);
            // Make offer
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

            _nftMarketContract.SetAccount(InitAccount);
            // Approve
            var approve =
                _nftContract.Approve(_nftMarketContract.ContractAddress, symbol, tokenId, 1000000);
            approve.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);
            _nftMarketContract.SetAccount(BuyerAccount);
            approveResult =
                _tokenContract.ApproveToken(InitAccount, _nftMarketContract.ContractAddress, 10000000000_00000000,
                    purchaseSymbol);
            approveResult.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);

            _nftMarketContract.SetAccount(InitAccount);
            var offerListBefore = _nftMarketContract.GetOfferList(symbol, tokenId, BuyerAccount);
            offerListBefore.Value.Count.ShouldBe(1);
            var listedNftInfo = _nftMarketContract.GetListedNFTInfoList(symbol, tokenId, InitAccount);
            listedNftInfo.Value.Count.ShouldBe(1);
            var buyerBalanceBefore = _tokenContract.GetUserBalance(BuyerAccount, purchaseSymbol);

            var listWithDutchAuction = ListWithDutchAuction(symbol, tokenId, startingPrice, endingPrice, purchaseSymbol,
                startTime, durationHours);
            listWithDutchAuction.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            var bidList = _nftMarketContract.GetBidList(symbol, tokenId);
            bidList.Value.Count.ShouldBe(0);
            offerList = _nftMarketContract.GetOfferList(symbol, tokenId, BuyerAccount);
            offerList.Value.Count.ShouldBe(1);
            listedNftInfo = _nftMarketContract.GetListedNFTInfoList(symbol, tokenId, InitAccount);
            listedNftInfo.Value.Count.ShouldBe(1);

            var buyerBalanceAfter = _tokenContract.GetUserBalance(BuyerAccount, purchaseSymbol);
            (buyerBalanceAfter - buyerBalanceBefore).ShouldBe(0);
        }

        [TestMethod]
        public void DelistWithListWithFixedPriceTest()
        {
            // ListWithFixedPrice
            var startTime = DateTime.UtcNow.AddSeconds(10).ToTimestamp();
            var tokenId = 1;
            var makeOffAmount = 10;
            var purchaseSymbol = "USDT";
            var purchaseAmount = 6_00000000;
            var delistAmount = 1;
            var expireTime = startTime.AddHours(1);
            var serviceFeeReceiver = WhiteListAddress2;
            var symbol = CreateAndMint(10000, 1000, tokenId);
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

            // Set service
            var serviceFeeResult = _nftMarketContract.SetServiceFee(20, serviceFeeReceiver);
            serviceFeeResult.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);

            // Make offer
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

            var listedNftInfo =
                _nftMarketContract.GetListedNFTInfoList(symbol, tokenId, InitAccount).Value[0];
            listedNftInfo.Quantity.ShouldBe(sellAmount);
            var serviceFeeReceiverBalanceBefore = _tokenContract.GetUserBalance(serviceFeeReceiver, purchaseSymbol);
            var sellerBalanceBefore = _tokenContract.GetUserBalance(InitAccount, purchaseSymbol);

            // Delist
            _nftMarketContract.SetAccount(InitAccount);
            var delist = _nftMarketContract.Delist(
                symbol,
                new Price
                {
                    Symbol = purchaseSymbol,
                    Amount = fixedPrice
                    // TokenId = tokenId
                },
                tokenId,
                delistAmount
            );
            delist.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            var offerList = _nftMarketContract.GetOfferList(symbol, tokenId, BuyerAccount);
            offerList.Value.Count.ShouldBe(1);
            listedNftInfo =
                _nftMarketContract.GetListedNFTInfoList(symbol, tokenId, InitAccount).Value[0];
            listedNftInfo.Quantity.ShouldBe(sellAmount - delistAmount);

            // Check service fee
            var serviceFeeReceiverBalanceAfter = _tokenContract.GetUserBalance(serviceFeeReceiver, purchaseSymbol);
            var sellerBalanceAfter = _tokenContract.GetUserBalance(InitAccount, purchaseSymbol);
            (serviceFeeReceiverBalanceAfter - serviceFeeReceiverBalanceBefore).ShouldBe(0);
            (sellerBalanceBefore - sellerBalanceAfter).ShouldBe(0);
        }

        [TestMethod]
        [DataRow("offerList", 6_00000000)]
        [DataRow("bidList", 12_00000000)]
        public void DelistWithListWithEnglistAuctionTest(string listType, long purchaseAmount)
        {
            var startTime = DateTime.UtcNow.AddSeconds(10).ToTimestamp();
            var tokenId = 1;
            var makeOffAmount = 1;
            var purchaseSymbol = "USDT";

            var delistAmount = 1;
            var expireTime = startTime.AddHours(1);
            var serviceFeeReceiver = WhiteListAddress2;
            var royaltyFeeReceiver = WhiteListAddress3;
            var symbol = CreateAndMintUnReuse(1, 1, tokenId);
            var startingPrice = 10_00000000;
            var earnestMoney = 5_00000000;
            var durationHours = 48;

            ListWithEnglistAuction(symbol, tokenId, startingPrice, purchaseSymbol,
                startTime, durationHours, earnestMoney);

            // Set service
            var serviceFeeResult = _nftMarketContract.SetServiceFee(20, serviceFeeReceiver);
            serviceFeeResult.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);

            // Set royalty
            var setRoyaltyResult = _nftMarketContract.SetRoyalty(symbol, tokenId, 50, royaltyFeeReceiver);
            setRoyaltyResult.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);
            var royaltyInfo = _nftMarketContract.GetRoyalty(symbol, tokenId);
            royaltyInfo.Royalty.ShouldBe(50);

            _nftMarketContract.SetAccount(BuyerAccount);
            var approveResult =
                _tokenContract.ApproveToken(BuyerAccount, _nftMarketContract.ContractAddress, 10000000000_00000000,
                    purchaseSymbol);
            approveResult.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);
            var buyerBalance = _tokenContract.GetUserBalance(BuyerAccount, purchaseSymbol);

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

            if (listType.Equals("offerList"))
            {
                var offerList = _nftMarketContract.GetOfferList(symbol, tokenId, BuyerAccount);
                offerList.Value.Count.ShouldBe(1);
            }
            else
            {
                var bidList = _nftMarketContract.GetBidList(symbol, tokenId);
                bidList.Value.Count.ShouldBe(1);
            }

            var listedNftInfo =
                _nftMarketContract.GetListedNFTInfoList(symbol, tokenId, InitAccount).Value[0];
            listedNftInfo.Quantity.ShouldBe(1);

            // Approve
            approveResult =
                _tokenContract.ApproveToken(InitAccount, _nftMarketContract.ContractAddress, 10000000000_00000000,
                    purchaseSymbol);
            approveResult.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);

            var serviceFeeReceiverBalanceBefore = _tokenContract.GetUserBalance(serviceFeeReceiver, purchaseSymbol);
            var sellerBalanceBefore = _tokenContract.GetUserBalance(InitAccount, purchaseSymbol);

            var buyerBalanceBefore = _tokenContract.GetUserBalance(BuyerAccount, purchaseSymbol);
            if (listType.Equals("offerList"))
            {
                (buyerBalance - buyerBalanceBefore).ShouldBe(0);
            }
            else
            {
                (buyerBalance - buyerBalanceBefore).ShouldBe(earnestMoney);
            }

            var englishAuctionInfo1 = _nftMarketContract.GetEnglishAuctionInfo(symbol, tokenId);
            Logger.Info($"englishAuctionInfo1 is {englishAuctionInfo1}");

            // Delist
            _nftMarketContract.SetAccount(InitAccount);
            var delist = _nftMarketContract.Delist(
                symbol,
                new Price
                {
                    Symbol = purchaseSymbol,
                    Amount = startingPrice
                    // TokenId = tokenId
                },
                tokenId,
                delistAmount
            );
            delist.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            if (listType.Equals("offerList"))
            {
                var offerListAfter = _nftMarketContract.GetOfferList(symbol, tokenId, BuyerAccount);
                offerListAfter.Value.Count.ShouldBe(1);
            }
            else
            {
                var bidListAfter = _nftMarketContract.GetBidList(symbol, tokenId);
                bidListAfter.Value.Count.ShouldBe(0);
                Logger.Info($"bidListAfter is {bidListAfter}");
            }

            var listedNftInfoAfter =
                _nftMarketContract.GetListedNFTInfoList(symbol, tokenId, InitAccount);
            listedNftInfoAfter.Value.Count.ShouldBe(0);
            var englishAuctionInfo = _nftMarketContract.GetEnglishAuctionInfo(symbol, tokenId);
            englishAuctionInfo.ShouldBe(new EnglishAuctionInfo());

            var buyerBalanceAfter = _tokenContract.GetUserBalance(BuyerAccount, purchaseSymbol);

            // Check service fee
            var expectServiceFee = 0;
            if (listType.Equals("offerList"))
            {
                expectServiceFee = 0;
                (buyerBalanceAfter - buyerBalanceBefore).ShouldBe(0);
            }
            else
            {
                expectServiceFee = 2000000;
                (buyerBalanceAfter - buyerBalanceBefore).ShouldBe(earnestMoney);
            }

            var serviceFeeReceiverBalanceAfter = _tokenContract.GetUserBalance(serviceFeeReceiver, purchaseSymbol);
            var sellerBalanceAfter = _tokenContract.GetUserBalance(InitAccount, purchaseSymbol);
            (serviceFeeReceiverBalanceAfter - serviceFeeReceiverBalanceBefore).ShouldBe(expectServiceFee);
            (sellerBalanceBefore - sellerBalanceAfter).ShouldBe(expectServiceFee);
        }

        [TestMethod]
        public void DelistWithListWithDutchAuctionTest()
        {
            var tokenId = 1;
            var totalSupply = 1;
            var mintAmount = 1;
            var startingPrice = 10_00000000;
            var endingPrice = 1_00000000;
            var purchaseSymbol = "USDT";
            var startTime = DateTime.UtcNow.AddSeconds(5).ToTimestamp();

            var durationHours = 48;
            var symbol = CreateAndMintUnReuse(totalSupply, mintAmount, tokenId);
            var serviceFeeReceiver = WhiteListAddress2;
            var makeOffAmount = 1;
            var expireTime = DateTime.UtcNow.AddHours(48).ToTimestamp();
            var purchaseAmount = 6_00000000;
            var delistAmount = 1;

            ListWithDutchAuction(symbol, tokenId, startingPrice, endingPrice, purchaseSymbol,
                startTime, durationHours);

            // Set service
            var serviceFeeResult = _nftMarketContract.SetServiceFee(20, serviceFeeReceiver);
            serviceFeeResult.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);
            var serviceFeeInfo = _nftMarketContract.GetServiceFeeInfo();
            serviceFeeInfo.ServiceFeeRate.ShouldBe(20);
            serviceFeeInfo.ServiceFeeReceiver.ShouldBe(serviceFeeReceiver.ConvertAddress());

            _nftMarketContract.SetAccount(BuyerAccount);
            var approveResult =
                _tokenContract.ApproveToken(BuyerAccount, _nftMarketContract.ContractAddress, 10000000000_00000000,
                    purchaseSymbol);
            approveResult.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);
            // Make offer
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

            var listedNftInfo =
                _nftMarketContract.GetListedNFTInfoList(symbol, tokenId, InitAccount).Value[0];
            listedNftInfo.Quantity.ShouldBe(1);

            // Approve
            approveResult =
                _tokenContract.ApproveToken(InitAccount, _nftMarketContract.ContractAddress, 10000000000_00000000,
                    purchaseSymbol);
            approveResult.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);

            var serviceFeeReceiverBalanceBefore = _tokenContract.GetUserBalance(serviceFeeReceiver, purchaseSymbol);
            var sellerBalanceBefore = _tokenContract.GetUserBalance(InitAccount, purchaseSymbol);

            // Delist
            _nftMarketContract.SetAccount(InitAccount);
            var delist = _nftMarketContract.Delist(
                symbol,
                new Price
                {
                    Symbol = purchaseSymbol,
                    Amount = startingPrice
                    // TokenId = tokenId
                },
                tokenId,
                delistAmount
            );
            delist.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            var offerList = _nftMarketContract.GetOfferList(symbol, tokenId, BuyerAccount);
            offerList.Value.Count.ShouldBe(1);
            var dutchAuctionInfo = _nftMarketContract.GetDutchAuctionInfo(symbol, tokenId);
            dutchAuctionInfo.ShouldBe(new DutchAuctionInfo());
            var listedNftInfoAfter = _nftMarketContract.GetListedNFTInfoList(symbol, tokenId, InitAccount);
            listedNftInfoAfter.Value.Count.ShouldBe(0);

            // Check service fee
            var serviceFeeReceiverBalanceAfter = _tokenContract.GetUserBalance(serviceFeeReceiver, purchaseSymbol);
            var sellerBalanceAfter = _tokenContract.GetUserBalance(InitAccount, purchaseSymbol);
            (serviceFeeReceiverBalanceAfter - serviceFeeReceiverBalanceBefore).ShouldBe(2000000);
            (sellerBalanceBefore - sellerBalanceAfter).ShouldBe(2000000);
        }

        [TestMethod]
        public void DelistErrorTest()
        {
            var tokenId = 1;
            var totalSupply = 1;
            var mintAmount = 1;
            var startingPrice = 10_00000000;
            var endingPrice = 1_00000000;
            var purchaseSymbol = "USDT";
            var startTime = DateTime.UtcNow.AddSeconds(5).ToTimestamp();
            var durationHours = 48;
            var symbol = CreateAndMintUnReuse(totalSupply, mintAmount, tokenId);
            var serviceFeeReceiver = WhiteListAddress2;
            var makeOffAmount = 1;
            var expireTime = DateTime.UtcNow.AddHours(48).ToTimestamp();
            var purchaseAmount = 6_00000000;
            var delistAmount = 1;

            ListWithDutchAuction(symbol, tokenId, startingPrice, endingPrice, purchaseSymbol,
                startTime, durationHours);

            // Set service
            var serviceFeeResult = _nftMarketContract.SetServiceFee(20, serviceFeeReceiver);
            serviceFeeResult.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);
            var serviceFeeInfo = _nftMarketContract.GetServiceFeeInfo();
            serviceFeeInfo.ServiceFeeRate.ShouldBe(20);
            serviceFeeInfo.ServiceFeeReceiver.ShouldBe(serviceFeeReceiver.ConvertAddress());

            _nftMarketContract.SetAccount(BuyerAccount);
            var approveResult =
                _tokenContract.ApproveToken(BuyerAccount, _nftMarketContract.ContractAddress, 10000000000_00000000,
                    purchaseSymbol);
            approveResult.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);
            // Make offer
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

            var listedNftInfo =
                _nftMarketContract.GetListedNFTInfoList(symbol, tokenId, InitAccount).Value[0];
            listedNftInfo.Quantity.ShouldBe(1);

            // Approve
            approveResult =
                _tokenContract.ApproveToken(InitAccount, _nftMarketContract.ContractAddress, 10000000000_00000000,
                    purchaseSymbol);
            approveResult.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);

            // Check symbol
            _nftMarketContract.SetAccount(InitAccount);
            var delist = _nftMarketContract.Delist(
                "CO12345678",
                new Price
                {
                    Symbol = purchaseSymbol,
                    Amount = startingPrice
                    // TokenId = tokenId
                },
                tokenId,
                delistAmount
            );
            delist.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
            delist.Error.ShouldContain("Check sender NFT balance failed.");

            // Check tokenId
            delist = _nftMarketContract.Delist(
                symbol,
                new Price
                {
                    Symbol = purchaseSymbol,
                    Amount = startingPrice
                    // TokenId = tokenId
                },
                200,
                delistAmount
            );
            delist.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
            delist.Error.ShouldContain("Check sender NFT balance failed.");

            // Check delistAmount
            delist = _nftMarketContract.Delist(
                symbol,
                new Price
                {
                    Symbol = purchaseSymbol,
                    Amount = startingPrice
                    // TokenId = tokenId
                },
                tokenId,
                delistAmount + 1
            );
            delist.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
            delist.Error.ShouldContain("Check sender NFT balance failed.");

            // Delist successfully
            delist = _nftMarketContract.Delist(
                symbol,
                new Price
                {
                    Symbol = purchaseSymbol,
                    Amount = startingPrice
                    // TokenId = tokenId
                },
                tokenId,
                delistAmount
            );
            delist.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            Thread.Sleep(60 * 1000);
            // Delist failed
            delist = _nftMarketContract.Delist(
                symbol,
                new Price
                {
                    Symbol = purchaseSymbol,
                    Amount = startingPrice
                    // TokenId = tokenId
                },
                tokenId,
                delistAmount
            );
            delist.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
            delist.Error.ShouldContain("Listed NFT Info not exists. (Or already delisted.)");
        }

        [TestMethod]
        public void ListWithFixedPrice_CustomizedTest()
        {
            var symbol = CustomizedTest(1, 4);

            var sellAmount = 1;
            var fixedPrice = 10_00000000;
            var whitePrice = 9_00000000;
            var purchaseSymbol = "USDT";
            var tokenId = 2;
            var startTime = DateTime.UtcNow.AddSeconds(120).ToTimestamp();
            var publicTime = DateTime.UtcNow.AddHours(5).ToTimestamp();
            var durationHours = 48;
            var startingPrice = 10_00000000;
            var earnestMoney = 5_00000000;
            var endingPrice = 2_00000000;
            var withdrawAmount = 5_00000000;

            // Check white list address
            var result = ListWithFixedPrice_Request(symbol, tokenId, sellAmount, fixedPrice, whitePrice, startTime,
                publicTime, durationHours, purchaseSymbol, OtherAccount);
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
            result.Error.ShouldContain("Incorrect white list address price list.");

            // Check public time
            result = ListWithFixedPrice_Request(symbol, tokenId, sellAmount, fixedPrice, whitePrice, startTime,
                DateTime.UtcNow.AddHours(2).ToTimestamp(), durationHours, purchaseSymbol, BuyerAccount);
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
            result.Error.ShouldContain("Incorrect white list hours.");

            result = ListWithFixedPrice_Request(symbol, tokenId, sellAmount, fixedPrice, whitePrice, startTime,
                DateTime.UtcNow.AddSeconds(60).ToTimestamp(), durationHours, purchaseSymbol, BuyerAccount);
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
            result.Error.ShouldContain("Incorrect white list hours.");

            // Mint
            var mintResult =
                _nftContract.ExecuteMethodWithResult(NftContractMethod.Mint, new MintInput
                {
                    Symbol = symbol,
                    Alias = "NFT_CO_CAT1",
                    Metadata = new Metadata(),
                    Quantity = 1,
                    TokenId = tokenId
                });
            mintResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            // Approve
            var approve =
                _nftContract.Approve(_nftMarketContract.ContractAddress, symbol, tokenId, 1000000);
            approve.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            // ListWithEnglistAuction failed
            var listWithEnglistAuction = ListWithEnglistAuction(symbol, tokenId, startingPrice, purchaseSymbol,
                startTime, durationHours, earnestMoney);
            listWithEnglistAuction.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            listWithEnglistAuction.Error.ShouldContain("This NFT cannot be listed with auction for now.");

            // ListWithDutchAuction failed
            var listWithDutchAuction = ListWithDutchAuction(symbol, tokenId, startingPrice, endingPrice, purchaseSymbol,
                startTime, durationHours);
            listWithDutchAuction.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            listWithDutchAuction.Error.ShouldContain("This NFT cannot be listed with auction for now.");

            // ListWithFixedPrice successfully
            result = ListWithFixedPrice_Request(symbol, tokenId, sellAmount, fixedPrice, whitePrice, startTime,
                publicTime, durationHours, purchaseSymbol, BuyerAccount);
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var requestInfo = _nftMarketContract.GetRequestInfo(symbol, tokenId);
            Logger.Info($"requestInfo is {requestInfo}");

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

            // Delist
            _nftMarketContract.SetAccount(InitAccount);
            var delist = _nftMarketContract.Delist(
                symbol,
                new Price
                {
                    Symbol = purchaseSymbol,
                    Amount = fixedPrice
                    // TokenId = tokenId
                },
                tokenId,
                sellAmount
            );
            delist.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            // ListWithFixedPrice for the second time
            result = ListWithFixedPrice_Request(symbol, tokenId, sellAmount, fixedPrice, whitePrice, startTime,
                publicTime.AddHours(1), durationHours, purchaseSymbol, BuyerAccount);
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            requestInfo = _nftMarketContract.GetRequestInfo(symbol, tokenId);
            Logger.Info($"requestInfo is {requestInfo}");

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
                            PublicTime = publicTime.AddHours(1),
                            DurationHours = durationHours
                        }
                    }
                }
            });

            var withdrawStakingTokensResult = _nftMarketContract.WithdrawStakingTokens(symbol, withdrawAmount);
            withdrawStakingTokensResult.Error.ShouldContain(
                "Cannot withdraw staking tokens before complete all the demands.");
        }

        [TestMethod]
        public void DealWithListWithFixedPrice_CustomizedTest()
        {
            // ListWithFixedPrice
            var symbol = ListWithFixedPrice_Customized();
            var startTime = DateTime.UtcNow.AddSeconds(10).ToTimestamp();
            var tokenId = 2;
            var makeOfferAmount = 1;
            var purchaseSymbol = "USDT";
            var purchaseAmount = 12_00000000;
            var dealAmount = 1;
            var expireTime = startTime.AddHours(1);
            var delistAmount = 1;
            var startingPrice = 10_00000000;
            var endingPrice = 2_00000000;
            var earnestMoney = 1_00000000;
            var durationHours = 10;

            // Make offer
            _nftMarketContract.SetAccount(OtherAccount);
            var makeOffer = _nftMarketContract.MakeOffer(
                symbol,
                tokenId,
                InitAccount,
                makeOfferAmount,
                new Price
                {
                    Symbol = purchaseSymbol,
                    Amount = 5_00000000
                    // TokenId = tokenId
                },
                expireTime
            );
            makeOffer.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var otherOfferList = _nftMarketContract.GetOfferList(symbol, tokenId, OtherAccount);
            otherOfferList.Value.Count.ShouldBe(1);

            _nftMarketContract.SetAccount(InitAccount);
            // Approve
            var approve =
                _nftContract.Approve(_nftMarketContract.ContractAddress, symbol, tokenId, 1000000);
            approve.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);
            var approveResult =
                _tokenContract.ApproveToken(OtherAccount, _nftMarketContract.ContractAddress, 10000000000_00000000,
                    purchaseSymbol);
            approveResult.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);

            _nftMarketContract.SetAccount(InitAccount);
            // Deal with other offer
            var deal = _nftMarketContract.Deal(
                symbol,
                tokenId,
                OtherAccount,
                new Price
                {
                    Symbol = purchaseSymbol,
                    Amount = purchaseAmount
                },
                dealAmount
            );
            deal.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
            deal.Error.ShouldContain("Due time not passed.");
            otherOfferList = _nftMarketContract.GetOfferList(symbol, tokenId, OtherAccount);
            otherOfferList.Value.Count.ShouldBe(1);

            var listedNftInfoList = _nftMarketContract.GetListedNFTInfoList(symbol, tokenId, InitAccount);
            Logger.Info($"listedNftInfoList is {listedNftInfoList}");

            // ListWithEnglistAuction failed
            var listWithEnglistAuction = ListWithEnglistAuction(symbol, tokenId, startingPrice, purchaseSymbol,
                startTime, durationHours, earnestMoney);
            listWithEnglistAuction.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            listWithEnglistAuction.Error.ShouldContain("This NFT cannot be listed with auction for now.");

            // ListWithDutchAuction failed
            var listWithDutchAuction = ListWithDutchAuction(symbol, tokenId, startingPrice, endingPrice, purchaseSymbol,
                startTime, durationHours);
            listWithDutchAuction.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            listWithDutchAuction.Error.ShouldContain("This NFT cannot be listed with auction for now.");

            // Delist
            var delist = _nftMarketContract.Delist(
                symbol,
                new Price
                {
                    Symbol = purchaseSymbol,
                    Amount = purchaseAmount
                    // TokenId = tokenId
                },
                tokenId,
                delistAmount
            );
            delist.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            // Claim remain deposit
            var claimRemainDeposit = _nftMarketContract.ClaimRemainDeposit(symbol, tokenId);
            claimRemainDeposit.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            claimRemainDeposit.Error.ShouldContain("Due time not passed.");
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

        private TransactionResultDto ListWithFixedPrice(string symbol, int tokenId, long sellAmount,
            long fixedPrice,
            long whitePrice1, long whitePrice2, long whitePrice3, Timestamp startTime, Timestamp publicTime,
            int durationHours, string purchaseSymbol, bool isMerge)
        {
            // Set token white list
            var setTokenWhiteListResult = _nftMarketContract.SetTokenWhiteList(symbol, new StringList
            {
                Value = {"ELF", "USDT"}
            });
            setTokenWhiteListResult.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);

            // Approve
            var approve =
                _nftContract.Approve(_nftMarketContract.ContractAddress, symbol, tokenId, 1000000);

            approve.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            // ListWithFixedPrice
            var result = _nftMarketContract.ListWithFixedPrice(
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

            return result;
        }

        private TransactionResultDto ListWithFixedPrice_Request(string symbol, int tokenId, long sellAmount,
            long fixedPrice,
            long whitePrice1, Timestamp startTime, Timestamp publicTime, int durationHours, string purchaseSymbol,
            string buyerAccount)
        {
            var isMerge = true;

            // Set token white list
            var setTokenWhiteListResult = _nftMarketContract.SetTokenWhiteList(symbol, new StringList
            {
                Value = {"ELF", "USDT"}
            });
            setTokenWhiteListResult.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);

            // Approve
            var approve =
                _nftContract.Approve(_nftMarketContract.ContractAddress, symbol, tokenId, 1000000);

            approve.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            // ListWithFixedPrice
            var listWithFixedPriceResult = _nftMarketContract.ListWithFixedPrice(
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
                            Address = buyerAccount.ConvertAddress(),
                            Price = new Price
                            {
                                Symbol = purchaseSymbol,
                                Amount = whitePrice1
                            }
                        },
                    }
                },
                isMerge
            );

            return listWithFixedPriceResult;
        }

        private TransactionResultDto ListWithEnglistAuction(string symbol, int tokenId, long startingPrice,
            string purchaseSymbol,
            Timestamp startTime, int durationHours, long earnestMoney)
        {
            // Approve
            var approve =
                _nftContract.Approve(_nftMarketContract.ContractAddress, symbol, tokenId, 1000000);

            var setTokenWhiteListResult = _nftMarketContract.SetTokenWhiteList(symbol, new StringList
            {
                Value = {"ELF", "USDT"}
            });
            setTokenWhiteListResult.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);

            approve.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            // ListWithEnglistAuction
            var listWithEnglishAuction = _nftMarketContract.ListWithEnglishAuction(
                symbol,
                tokenId,
                startingPrice,
                purchaseSymbol,
                new ListDuration
                {
                    StartTime = startTime,
                    DurationHours = durationHours
                },
                earnestMoney
            );

            return listWithEnglishAuction;
        }

        private TransactionResultDto ListWithDutchAuction(string symbol, int tokenId, long startingPrice,
            long endingPrice,
            string purchaseSymbol,
            Timestamp startTime, int durationHours)
        {
            // Approve
            var approve =
                _nftContract.Approve(_nftMarketContract.ContractAddress, symbol, tokenId, 1000000);

            approve.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            var setTokenWhiteListResult = _nftMarketContract.SetTokenWhiteList(symbol, new StringList
            {
                Value = {"ELF", "USDT"}
            });
            setTokenWhiteListResult.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);

            // ListWithDutchAuction
            var listWithDutchAuctionResult = _nftMarketContract.ListWithDutchAuction(
                symbol,
                tokenId,
                startingPrice,
                endingPrice,
                purchaseSymbol,
                new ListDuration
                {
                    StartTime = startTime,
                    DurationHours = durationHours
                }
            );

            return listWithDutchAuctionResult;
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
            englishAuctionInfo.Duration.DurationHours.ShouldBe(expectEnglishAuctionInfo.Duration.DurationHours);
            englishAuctionInfo.Owner.ShouldBe(expectEnglishAuctionInfo.Owner);
            englishAuctionInfo.EarnestMoney.ShouldBe(expectEnglishAuctionInfo.EarnestMoney);
            englishAuctionInfo.DealPrice.ShouldBe(expectEnglishAuctionInfo.DealPrice);
            englishAuctionInfo.DealTo.ShouldBe(expectEnglishAuctionInfo.DealTo);
        }

        private void CheckDutchAuctionInfo(string symbol, int tokenId,
            DutchAuctionInfo expectDutchAuctionInfo)
        {
            var dutchAuctionInfo =
                _nftMarketContract.GetDutchAuctionInfo(symbol, tokenId);

            dutchAuctionInfo.Symbol.ShouldBe(expectDutchAuctionInfo.Symbol);
            dutchAuctionInfo.TokenId.ShouldBe(expectDutchAuctionInfo.TokenId);
            dutchAuctionInfo.StartingPrice.ShouldBe(expectDutchAuctionInfo.StartingPrice);
            dutchAuctionInfo.EndingPrice.ShouldBe(expectDutchAuctionInfo.EndingPrice);
            dutchAuctionInfo.PurchaseSymbol.ShouldBe(expectDutchAuctionInfo.PurchaseSymbol);
            dutchAuctionInfo.Duration.StartTime.ShouldBe(expectDutchAuctionInfo.Duration.StartTime);
            dutchAuctionInfo.Duration.DurationHours.ShouldBe(expectDutchAuctionInfo.Duration.DurationHours);
            dutchAuctionInfo.Owner.ShouldBe(expectDutchAuctionInfo.Owner);
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

        private string CustomizedTest(long workHours, long whiteListHours)
        {
            var totalSupply = 10000;
            var mintAmount = 1;
            var tokenId = 1;
            var requestTokenId = 2;
            var depositRate = 5000;
            var purchaseSymbol = "USDT";
            var purchaseAmount = 10_00000000;
            var makeOfferAmount1 = 3_00000000;
            // var workHours = 1;
            // var whiteListHours = 4;
            var stakingAmount = 6_00000000;
            var makeOfferAmount = 1;
            var expireTime = DateTime.UtcNow.AddHours(48).ToTimestamp();

            var symbol = CreateAndMintUnReuse(totalSupply, mintAmount, tokenId);

            // SetTokenWhiteList
            var result = _nftMarketContract.SetTokenWhiteList(symbol, new StringList
            {
                Value = {"USDT"}
            });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            // Approve from InitAccount
            var approveResult =
                _tokenContract.ApproveToken(InitAccount, _nftMarketContract.ContractAddress, 10000000000_00000000,
                    purchaseSymbol);
            approveResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            // Set customize info
            var setCustomizeInfoResult =
                _nftMarketContract.SetCustomizeInfo(symbol, depositRate, new Price
                    {
                        Symbol = purchaseSymbol,
                        Amount = purchaseAmount
                    }, workHours, whiteListHours,
                    stakingAmount);
            setCustomizeInfoResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            // Claim remain deposit
            var claimRemainDeposit = _nftMarketContract.ClaimRemainDeposit(symbol, tokenId);
            claimRemainDeposit.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            claimRemainDeposit.Error.ShouldContain("Request info does not exist.");

            // Approve from BuyerAccount
            _nftMarketContract.SetAccount(BuyerAccount);
            approveResult =
                _tokenContract.ApproveToken(BuyerAccount, _nftMarketContract.ContractAddress, 10000000000_00000000,
                    purchaseSymbol);
            approveResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            // Make offer
            var makeOffer = _nftMarketContract.MakeOffer(
                symbol,
                requestTokenId,
                InitAccount,
                makeOfferAmount,
                new Price
                {
                    Symbol = purchaseSymbol,
                    Amount = makeOfferAmount1
                    // TokenId = tokenId
                },
                expireTime
            );
            makeOffer.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            var requestInfo = _nftMarketContract.GetRequestInfo(symbol, requestTokenId);
            Logger.Info($"requestInfo is {requestInfo}");

            // HandleRequest failed
            _nftMarketContract.SetAccount(OtherAccount);
            var handleRequest = _nftMarketContract.HandleRequest(
                symbol,
                requestTokenId,
                BuyerAccount,
                true
            );
            handleRequest.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            handleRequest.Error.ShouldContain("Only NFT Protocol Creator can handle request.");

            // HandleRequest
            _nftMarketContract.SetAccount(InitAccount);
            handleRequest = _nftMarketContract.HandleRequest(
                symbol,
                requestTokenId,
                BuyerAccount,
                true
            );
            handleRequest.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            return symbol;
        }

        private string ListWithFixedPrice_Customized()
        {
            var symbol = CustomizedTest(1, 4);

            var sellAmount = 1;
            var fixedPrice = 12_00000000;
            var whitePrice = 7_00000000;
            var purchaseSymbol = "USDT";
            var tokenId = 2;
            var startTime = DateTime.UtcNow.AddSeconds(5).ToTimestamp();
            var publicTime = DateTime.UtcNow.AddHours(6).ToTimestamp();
            var durationHours = 48;

            // Mint
            var mintResult =
                _nftContract.ExecuteMethodWithResult(NftContractMethod.Mint, new MintInput
                {
                    Symbol = symbol,
                    Alias = "NFT_CO_CAT1",
                    Metadata = new Metadata(),
                    Quantity = 1,
                    TokenId = tokenId
                });
            mintResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            // Approve
            var approve =
                _nftContract.Approve(_nftMarketContract.ContractAddress, symbol, tokenId, 1000000);
            approve.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            // ListWithFixedPrice successfully
            var result = ListWithFixedPrice_Request(symbol, tokenId, sellAmount, fixedPrice, whitePrice, startTime,
                publicTime, durationHours, purchaseSymbol, BuyerAccount);
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            return symbol;
        }

        [TestMethod]
        [DataRow("ListWithFixedPrice")]
        public void CustomizedOtherCasesTest(string listType)
        {
            // [DataRow("ListWithDutchAuction")]
            // [DataRow("ListWithEnglishAuction")]
            var symbol = CustomizedTest(0, 1);
            var sellAmount = 1;
            var fixedPrice = 12_00000000;
            var whitePrice = 7_00000000;
            var purchaseSymbol = "USDT";
            var purchaseAmount = 6_00000000;
            var tokenId = 2;
            var startTime = DateTime.UtcNow.ToTimestamp();
            var publicTime = DateTime.UtcNow.ToTimestamp().AddHours(1).AddMinutes(1);
            var durationHours = 48;
            var makeOfferAmount = 1;
            var expireTime = DateTime.UtcNow.ToTimestamp().AddHours(100);
            var startingPrice = 10_00000000;
            var earnestMoney = 1_00000000;
            var endingPrice = 1_00000000;
            var delistAmount = 1;

            // Mint
            var mintResult =
                _nftContract.ExecuteMethodWithResult(NftContractMethod.Mint, new MintInput
                {
                    Symbol = symbol,
                    Alias = "NFT_CO_CAT1",
                    Metadata = new Metadata(),
                    Quantity = 1,
                    TokenId = tokenId
                });
            mintResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            // Approve
            var approve =
                _nftContract.Approve(_nftMarketContract.ContractAddress, symbol, tokenId, 1000000);
            approve.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            // ListWithFixedPrice successfully
            var result = ListWithFixedPrice_Request(symbol, tokenId, sellAmount, fixedPrice, whitePrice, startTime,
                publicTime, durationHours, purchaseSymbol, BuyerAccount);
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            // Make offer
            _nftMarketContract.SetAccount(OtherAccount);
            var makeOffer = _nftMarketContract.MakeOffer(
                symbol,
                tokenId,
                InitAccount,
                makeOfferAmount,
                new Price
                {
                    Symbol = purchaseSymbol,
                    Amount = purchaseAmount
                    // TokenId = tokenId
                },
                expireTime
            );
            makeOffer.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var otherOfferList = _nftMarketContract.GetOfferList(symbol, tokenId, OtherAccount);
            otherOfferList.Value.Count.ShouldBe(1);

            var customizeInfo = _nftMarketContract.GetCustomizeInfo(symbol);
            Logger.Info($"customizeInfo is {customizeInfo}");

            _nftMarketContract.SetAccount(InitAccount);
            var initBalanceBefore = _tokenContract.GetUserBalance(InitAccount, purchaseSymbol);
            Logger.Info($"initBalanceBefore is {initBalanceBefore}");
            var listedNftInfoList = _nftMarketContract.GetListedNFTInfoList(symbol, tokenId, InitAccount);
            Logger.Info($"listedNftInfoList is {listedNftInfoList}");
            var englishAuctionInfo = _nftMarketContract.GetEnglishAuctionInfo(symbol, tokenId);
            Logger.Info($"englishAuctionInfo is {englishAuctionInfo}");
            var dutchAuctionInfo = _nftMarketContract.GetDutchAuctionInfo(symbol, tokenId);
            Logger.Info($"dutchAuctionInfo is {dutchAuctionInfo}");

            if (listType.Equals("ListWithFixedPrice"))
            {
                // Delist
                var delist = _nftMarketContract.Delist(
                    symbol,
                    new Price
                    {
                        Symbol = purchaseSymbol,
                        Amount = fixedPrice
                        // TokenId = tokenId
                    },
                    tokenId,
                    delistAmount
                );
                delist.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

                // ListWithFixedPrice
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
                    true
                );
                listWithFixedPriceResult.Status.ConvertTransactionResultStatus()
                    .ShouldBe(TransactionResultStatus.Mined);
            }
            else if (listType.Equals("ListWithEnglishAuction"))
            {
                var listWithEnglishAuctionResult = _nftMarketContract.ListWithEnglishAuction(
                    symbol,
                    tokenId,
                    startingPrice,
                    purchaseSymbol,
                    new ListDuration(),
                    earnestMoney
                );
                listWithEnglishAuctionResult.Status.ConvertTransactionResultStatus()
                    .ShouldBe(TransactionResultStatus.Mined);
            }
            else
            {
                // ListWithDutchAuction
                var listWithDutchAuctionResult = _nftMarketContract.ListWithDutchAuction(
                    symbol,
                    tokenId,
                    startingPrice,
                    endingPrice,
                    purchaseSymbol,
                    new ListDuration()
                );
                listWithDutchAuctionResult.Status.ConvertTransactionResultStatus()
                    .ShouldBe(TransactionResultStatus.Mined);
            }

            var requestInfo = _nftMarketContract.GetRequestInfo(symbol, tokenId);
            requestInfo.ShouldBe(new RequestInfo());

            // Receive remain deposit
            var initBalanceAfter = _tokenContract.GetUserBalance(InitAccount, purchaseSymbol);
            Logger.Info($"initBalanceAfter is {initBalanceAfter}");
            initBalanceAfter.ShouldBe(initBalanceBefore + 250000000);

            // Approve
            var approveResult =
                _tokenContract.ApproveToken(OtherAccount, _nftMarketContract.ContractAddress, 10000000000_00000000,
                    purchaseSymbol);
            approveResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            // Deal offer from 
            _nftMarketContract.SetAccount(InitAccount);
            var deal = _nftMarketContract.Deal(
                symbol,
                tokenId,
                OtherAccount,
                new Price
                {
                    Symbol = purchaseSymbol,
                    Amount = purchaseAmount
                },
                1
            );
            deal.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
        }

        [TestMethod]
        public void ContractNotInitializedTest()
        {
            var symbol = CustomizedTest(1, 0);
            var sellAmount = 1;
            var fixedPrice = 12_00000000;
            var purchaseSymbol = "USDT";
            var purchaseAmount = 6_00000000;
            var tokenId = 2;
            var startingPrice = 10_00000000;
            var earnestMoney = 1_00000000;
            var endingPrice = 1_00000000;
            var isMerge = true;
            var depositRate = 5000;
            var workHours = 1;
            var whiteListHours = 1;
            var stakingAmount = 1_00000000;
            var makeOffAmount = 1;

            // ListWithFixedPrice
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

            // ListWithEnglishAuction
            var listWithEnglishAuctionResult = _nftMarketContract.ListWithEnglishAuction(
                symbol,
                tokenId,
                startingPrice,
                purchaseSymbol,
                new ListDuration(),
                earnestMoney
            );
            listWithEnglishAuctionResult.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            listWithEnglishAuctionResult.Error.ShouldContain("Contract not initialized.");

            // ListWithDutchAuction
            var listWithDutchAuctionResult = _nftMarketContract.ListWithDutchAuction(
                symbol,
                tokenId,
                startingPrice,
                endingPrice,
                purchaseSymbol,
                new ListDuration()
            );
            listWithDutchAuctionResult.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            listWithDutchAuctionResult.Error.ShouldContain("Contract not initialized.");

            // SetCustomizeInfo
            var setCustomizeInfoResult = _nftMarketContract.SetCustomizeInfo(symbol, depositRate, new Price
            {
                Symbol = purchaseSymbol,
                Amount = purchaseAmount
            }, workHours, whiteListHours, stakingAmount);
            setCustomizeInfoResult.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            setCustomizeInfoResult.Error.ShouldContain("Contract not initialized.");

            // HandleRequest
            var handleRequestResult = _nftMarketContract.HandleRequest(symbol, tokenId, InitAccount, true);
            handleRequestResult.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            handleRequestResult.Error.ShouldContain("Contract not initialized.");

            // StakeForRequests
            var stakeForRequestsResult = _nftMarketContract.StakeForRequests(symbol, stakingAmount);
            stakeForRequestsResult.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            stakeForRequestsResult.Error.ShouldContain("Contract not initialized.");

            // Deal
            var deal = _nftMarketContract.Deal(
                symbol,
                tokenId,
                BuyerAccount,
                new Price
                {
                    Symbol = purchaseSymbol,
                    Amount = purchaseAmount
                },
                makeOffAmount
            );
            deal.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
            deal.Error.ShouldContain("Contract not initialized.");
        }
    }
}