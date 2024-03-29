﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AElf.Client.Dto;
using AElf.Contracts.MultiToken;
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
    public class NFTMarketContractBuyerTest
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
            SetGlobalTokenWhiteList();
        }

        private void SetGlobalTokenWhiteList()
        {
            // SetGlobalTokenWhiteList
            var result = _nftMarketContract.SetGlobalTokenWhiteList(new StringList
            {
                Value = { "USDT" }
            });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
        }

        private void ContractInitialize()
        {
            var serviceFeeReceiver = serviceAddress;
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

        private string CreateAndMint1(string creator, long totalSupply, long mintAmount, long tokenId)
        {
            _nftContract.SetAccount(creator);
            var createResult =
                _nftContract.ExecuteMethodWithResult(NftContractMethod.Create, new CreateInput
                {
                    NftType = NFTType.Collectables.ToString(),
                    ProtocolName = "CAT",
                    TotalSupply = totalSupply,
                    Creator = creator.ConvertAddress(),
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
            var initBalanceBefore = GetBalanceTest(creator, symbol, tokenId);
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
            var initAccountAfterBalance = GetBalanceTest(creator, symbol, tokenId);
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
            //initAccountAfterBalance.ShouldBe(mintAmount + initBalanceBefore);
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
            long whitePrice1, long whitePrice11, long whitePrice12, long whitePrice2, long whitePrice21,
            long whitePrice3, Timestamp startTime, Timestamp publicTime,
            int durationHours, string purchaseSymbol, bool isMerge)
        {
            // Approve
            var approve =
                _nftContract.Approve(_nftMarketContract.ContractAddress, symbol, tokenId, 1000000);

            approve.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            // ListWithFixedPrice
            var result10 = _nftMarketContract.ListWithFixedPrice(
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
                            Address = WhiteListAddress1.ConvertAddress(),
                            Price = new Price
                            {
                                Symbol = purchaseSymbol,
                                Amount = whitePrice11
                            }
                        },
                        new WhiteListAddressPrice
                        {
                            Address = WhiteListAddress1.ConvertAddress(),
                            Price = new Price
                            {
                                Symbol = purchaseSymbol,
                                Amount = whitePrice12
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
                            Address = WhiteListAddress2.ConvertAddress(),
                            Price = new Price
                            {
                                Symbol = purchaseSymbol,
                                Amount = whitePrice21
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
            result10.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
        }


        private TransactionResultDto ListWithFixedPrice_Request(string symbol, int tokenId, long sellAmount,
            long fixedPrice,
            long whitePrice1, Timestamp startTime, Timestamp publicTime, int durationHours, string purchaseSymbol,
            string buyerAccount)
        {
            var isMerge = true;

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


        public void ListWithEnglistAuction(string symbol, int tokenId, long startingPrice, string purchaseSymbol,
            Timestamp startTime, int durationHours, long earnestMoney)
        {
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
                    DurationHours = durationHours
                },
                earnestMoney
            );
        }

        public void ListWithDutchAuction(string symbol, int tokenId, long startingPrice, long endingPrice,
            string purchaseSymbol,
            Timestamp startTime, int durationHours)
        {
            // Approve
            var approve =
                _nftContract.Approve(_nftMarketContract.ContractAddress, symbol, tokenId, 1000000);

            approve.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            // ListWithDutchAuction
            var rutuen = _nftMarketContract.ListWithDutchAuction(
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
            rutuen.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
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

            // 1.Not listed
            _nftMarketContract.SetAccount(OtherAccount);
            var makeOffer = _nftMarketContract.MakeOffer(
                symbol,
                tokenId,
                InitAccount,
                buyAmount,
                new Price
                {
                    Symbol = "ELF",
                    Amount = purchaseAmount
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
        public void ListWithFixedPricepublicTime()
        {
            var tokenId = 1;
            var totalAmount = 1000;
            var totalSupply = 10000;
            var mintAmount = 1000;
            var sellAmount = 100;
            var fixedPrice = 12_00000000;
            var whitePrice1 = 9_00000000;
            var whitePrice11 = 9_10000000;
            var whitePrice12 = 9_20000000;
            var whitePrice2 = 10_00000000;
            var whitePrice21 = 10_10000000;
            var whitePrice3 = 11_00000000;
            var buyAmount = 1;
            var expireTime = DateTime.UtcNow.AddSeconds(10).ToTimestamp();

            var startTime = DateTime.UtcNow.AddHours(20).ToTimestamp();
            var publicTime = DateTime.UtcNow.AddHours(20).ToTimestamp();
            var durationHours = 1;
            var symbol = CreateAndMint(totalSupply, mintAmount, tokenId);
            Logger.Info($"startTime is {startTime.Seconds}");
            Logger.Info($"publicTime is {publicTime.Seconds}");
            var purchaseSymbol = "USDT";
            var isMerge = true;

            ListWithFixedPrice(symbol, tokenId, sellAmount, fixedPrice, whitePrice1, whitePrice11, whitePrice12,
                whitePrice2, whitePrice21,
                whitePrice3, startTime, publicTime, durationHours, purchaseSymbol, isMerge);


            // Approve
            _nftContract.SetAccount(WhiteListAddress1);
            var approve = _tokenContract.ApproveToken(WhiteListAddress1, _nftMarketContract.ContractAddress,
                10000000000_00000000, purchaseSymbol);
            approve.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            _nftContract.SetAccount(OtherAccount);
            var approve1 = _tokenContract.ApproveToken(OtherAccount, _nftMarketContract.ContractAddress,
                10000000000_00000000, purchaseSymbol);
            approve1.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            //UtcNow<start_time   
            _nftMarketContract.SetAccount(WhiteListAddress1);
            var FixedPrice = _nftMarketContract.MakeOffer(
                symbol,
                tokenId,
                InitAccount,
                1,
                new Price
                {
                    Symbol = purchaseSymbol,
                    Amount = whitePrice1
                },
                expireTime
            );
            FixedPrice.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);
            _nftMarketContract.SetAccount(WhiteListAddress1);
            var getBalanceBuyerStart = _nftContract.GetBalance(WhiteListAddress1, symbol, tokenId);
            Logger.Info($"WhiteListAddress1Balance is {getBalanceBuyerStart.Balance}");
            var OfferList = _nftMarketContract.GetOfferList(symbol, tokenId, WhiteListAddress1);
            Logger.Info($"OfferList is {OfferList}");

            _nftMarketContract.SetAccount(OtherAccount);
            var result = _nftMarketContract.MakeOffer(
                symbol,
                tokenId,
                InitAccount,
                1,
                new Price
                {
                    Symbol = purchaseSymbol,
                    Amount = fixedPrice
                },
                expireTime.AddSeconds(1)
            );
            result.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);
            var getBalanceBuyerStart1 = _nftContract.GetBalance(OtherAccount, symbol, tokenId);
            Logger.Info($"OtherAccountBalance is {getBalanceBuyerStart1.Balance}");
            var OfferList1 = _nftMarketContract.GetOfferList(symbol, tokenId, OtherAccount);
            Logger.Info($"OfferList is {OfferList1}");
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
            var whitePrice11 = 9_10000000;
            var whitePrice12 = 9_20000000;
            var whitePrice2 = 10_00000000;
            var whitePrice21 = 10_10000000;
            var whitePrice3 = 11_00000000;
            var buyAmount = 1;
            var expireTime = DateTime.UtcNow.AddSeconds(10).ToTimestamp();

            var startTime = DateTime.UtcNow.ToTimestamp();
            var publicTime = DateTime.UtcNow.AddHours(20).ToTimestamp();
            var durationHours = 1;
            var symbol = CreateAndMint(totalSupply, mintAmount, tokenId);
            Logger.Info($"startTime is {startTime.Seconds}");
            Logger.Info($"publicTime is {publicTime.Seconds}");
            var purchaseSymbol = "USDT";
            var isMerge = true;

            ListWithFixedPrice(symbol, tokenId, sellAmount, fixedPrice, whitePrice1, whitePrice11, whitePrice12,
                whitePrice2, whitePrice21,
                whitePrice3, startTime, publicTime, durationHours, purchaseSymbol, isMerge);


            // Approve
            _nftContract.SetAccount(WhiteListAddress1);
            var approve = _tokenContract.ApproveToken(WhiteListAddress1, _nftMarketContract.ContractAddress,
                10000000000_00000000, purchaseSymbol);
            approve.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            _nftContract.SetAccount(WhiteListAddress2);
            var approve1 = _tokenContract.ApproveToken(WhiteListAddress2, _nftMarketContract.ContractAddress,
                10000000000_00000000, purchaseSymbol);
            approve1.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            //2.start_time   <   UtcNow    <public_time
            _nftMarketContract.SetAccount(WhiteListAddress1);
            var getBalanceBuyerStart = _nftContract.GetBalance(WhiteListAddress1, symbol, tokenId);
            Logger.Info($"WhiteListAddress1Balance is {getBalanceBuyerStart.Balance}");

            var getBalancesellerStart = _nftContract.GetBalance(InitAccount, symbol, tokenId);
            Logger.Info($"InitAccountBalance is {getBalancesellerStart}");

            var balanceStart = _tokenContract.GetUserBalance(serviceAddress, purchaseSymbol);
            Logger.Info($"balance is {balanceStart}");


            var FixedPrice = _nftMarketContract.MakeOffer(
                symbol,
                tokenId,
                InitAccount,
                1,
                new Price
                {
                    Symbol = purchaseSymbol,
                    Amount = whitePrice1
                },
                expireTime
            );
            FixedPrice.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);

            var getBalanceBuyerFinish = _nftContract.GetBalance(WhiteListAddress1, symbol, tokenId);
            Logger.Info($"WhiteListAddress1Balance is {getBalanceBuyerFinish.Balance}");
            getBalanceBuyerFinish.Balance.ShouldBe(getBalanceBuyerStart.Balance + 1);
            var getBalancesellerFinish = _nftContract.GetBalance(InitAccount, symbol, tokenId);
            Logger.Info($"InitAccountBalance is {getBalancesellerFinish.Balance}");
            getBalancesellerFinish.Balance.ShouldBe(getBalancesellerStart.Balance - 1);
            var balanceFinish = _tokenContract.GetUserBalance(serviceAddress, purchaseSymbol);
            Logger.Info($"balance is {balanceFinish}");
            balanceFinish.ShouldBe(balanceStart + 900000);
            // Check event
            var logs = FixedPrice.Logs.First(l => l.Name.Equals("Sold")).NonIndexed;
            var byteString = ByteString.FromBase64(logs);
            var soldLogs = Sold.Parser.ParseFrom(byteString);
            soldLogs.NftFrom.ShouldBe(InitAccount.ConvertAddress());
            soldLogs.NftTo.ShouldBe(WhiteListAddress1.ConvertAddress());
            soldLogs.NftSymbol.ShouldBe(symbol);
            soldLogs.NftTokenId.ShouldBe(tokenId);
            soldLogs.NftQuantity.ShouldBe(1);
            soldLogs.PurchaseSymbol.ShouldBe(purchaseSymbol);
            soldLogs.PurchaseTokenId.ShouldBe(0);
            soldLogs.PurchaseAmount.ShouldBe(whitePrice1);

            //3.enterAmount<whitePrice
            _nftMarketContract.SetAccount(WhiteListAddress2);
            var result = _nftMarketContract.MakeOffer(
                symbol,
                tokenId,
                InitAccount,
                buyAmount,
                new Price
                {
                    Symbol = purchaseSymbol,
                    Amount = whitePrice2 - 100000000
                },
                expireTime
            );
            var OfferList = _nftMarketContract.GetOfferList(symbol, tokenId, WhiteListAddress2);
            Logger.Info($"OfferList is {OfferList}");
            OfferList.Value.Count.ShouldBe(1);
            OfferList.Value.First().From.ShouldBe(WhiteListAddress2.ConvertAddress());
            OfferList.Value.First().To.ShouldBe(InitAccount.ConvertAddress());
            OfferList.Value.First().Price.Symbol.ShouldBe(purchaseSymbol);
            OfferList.Value.First().Price.Amount.ShouldBe(whitePrice2 - 100000000);
            result.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);
            // Check event
            logs = result.Logs.First(l => l.Name.Equals("OfferAdded")).NonIndexed;
            byteString = ByteString.FromBase64(logs);
            var offerAddedLogs = OfferAdded.Parser.ParseFrom(byteString);
            offerAddedLogs.Symbol.ShouldBe(symbol);
            offerAddedLogs.TokenId.ShouldBe(tokenId);
            offerAddedLogs.OfferFrom.ShouldBe(WhiteListAddress2.ConvertAddress());
            offerAddedLogs.OfferTo.ShouldBe(InitAccount.ConvertAddress());
            offerAddedLogs.ExpireTime.ShouldBe(expireTime);
            offerAddedLogs.Price.Symbol.ShouldBe(purchaseSymbol);
            offerAddedLogs.Price.Amount.ShouldBe(whitePrice2 - 100000000);
            // Check event
            logs = result.Logs.First(l => l.Name.Equals("OfferMade")).NonIndexed;
            byteString = ByteString.FromBase64(logs);
            var offerMadeLogs = OfferMade.Parser.ParseFrom(byteString);
            offerMadeLogs.Symbol.ShouldBe(symbol);
            offerMadeLogs.TokenId.ShouldBe(tokenId);
            offerMadeLogs.OfferFrom.ShouldBe(WhiteListAddress2.ConvertAddress());
            offerMadeLogs.OfferTo.ShouldBe(InitAccount.ConvertAddress());
            offerMadeLogs.Price.Symbol.ShouldBe(purchaseSymbol);
            offerMadeLogs.Price.Amount.ShouldBe(whitePrice2 - 100000000);
            offerMadeLogs.Quantity.ShouldBe(1);

            //4.fixedPrice   <  whitePrice< = Enter amount
            _nftMarketContract.SetAccount(WhiteListAddress2);
            var getBalanceBuyerStart4 = _nftContract.GetBalance(WhiteListAddress2, symbol, tokenId);
            Logger.Info($"WhiteListAddress1Balance4 is {getBalanceBuyerStart4.Balance}");
            var getBalancesellerStart4 = _nftContract.GetBalance(InitAccount, symbol, tokenId);
            Logger.Info($"InitAccountBalance4 is {getBalancesellerStart4}");
            var balanceWhiteListAddress1Start4 = _tokenContract.GetUserBalance(WhiteListAddress2, purchaseSymbol);
            Logger.Info($"balanceWhiteListAddress1Start4 is {balanceWhiteListAddress1Start4}");
            var balanceStart4 = _tokenContract.GetUserBalance(serviceAddress, purchaseSymbol);
            Logger.Info($"balance4 is {balanceStart4}");

            _nftMarketContract.SetAccount(WhiteListAddress2);
            var result2 = _nftMarketContract.MakeOffer(
                symbol,
                tokenId,
                InitAccount,
                buyAmount,
                new Price
                {
                    Symbol = purchaseSymbol,
                    Amount = whitePrice2 + 100000000
                },
                expireTime
            );
            result2.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);

            var OfferList1 = _nftMarketContract.GetOfferList(symbol, tokenId, WhiteListAddress1);
            Logger.Info($"OfferList1 is {OfferList1}");

            var getBalanceBuyerFinish4 = _nftContract.GetBalance(WhiteListAddress2, symbol, tokenId);
            Logger.Info($"getBalanceBuyerFinish4 is {getBalanceBuyerFinish4.Balance}");
            getBalanceBuyerFinish4.Balance.ShouldBe(getBalanceBuyerStart4.Balance + 1);

            var getBalancesellerFinish4 = _nftContract.GetBalance(InitAccount, symbol, tokenId);
            Logger.Info($"getBalancesellerFinish4 is {getBalancesellerFinish4.Balance}");
            getBalancesellerFinish4.Balance.ShouldBe(getBalancesellerStart4.Balance - 1);

            var balanceWhiteListAddress1Finish4 = _tokenContract.GetUserBalance(WhiteListAddress2, purchaseSymbol);
            Logger.Info($"balanceWhiteListAddress1Finish4 is {balanceWhiteListAddress1Finish4}");
            balanceWhiteListAddress1Finish4.ShouldBe(balanceWhiteListAddress1Start4 - whitePrice2);

            var balanceFinish4 = _tokenContract.GetUserBalance(serviceAddress, purchaseSymbol);
            Logger.Info($"balanceFinish4 is {balanceFinish4}");
            balanceFinish4.ShouldBe(balanceStart4 + 1000000);

            //users
            // Approve
            _nftContract.SetAccount(OtherAccount);
            var approve2 = _tokenContract.ApproveToken(OtherAccount, _nftMarketContract.ContractAddress,
                10000000000_00000000, purchaseSymbol);
            approve2.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            //10.start_time < UtcNow <public_time,fixedPrice < = Enter amount
            _nftMarketContract.SetAccount(OtherAccount);
            var result10 = _nftMarketContract.MakeOffer(
                symbol,
                tokenId,
                InitAccount,
                buyAmount,
                new Price
                {
                    Symbol = purchaseSymbol,
                    Amount = fixedPrice
                },
                expireTime
            );
            result10.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);
            var OfferList2 = _nftMarketContract.GetOfferList(symbol, tokenId, OtherAccount);
            Logger.Info($"OfferList2 is {OfferList2}");
            // Check event
            logs = result10.Logs.First(l => l.Name.Equals("OfferAdded")).NonIndexed;
            byteString = ByteString.FromBase64(logs);
            offerAddedLogs = OfferAdded.Parser.ParseFrom(byteString);
            offerAddedLogs.Symbol.ShouldBe(symbol);
            offerAddedLogs.TokenId.ShouldBe(tokenId);
            offerAddedLogs.OfferFrom.ShouldBe(OtherAccount.ConvertAddress());
            offerAddedLogs.OfferTo.ShouldBe(InitAccount.ConvertAddress());
            offerAddedLogs.ExpireTime.ShouldBe(expireTime);
            offerAddedLogs.Price.Symbol.ShouldBe(purchaseSymbol);
            offerAddedLogs.Price.Amount.ShouldBe(fixedPrice);
            // Check event
            logs = result10.Logs.First(l => l.Name.Equals("OfferMade")).NonIndexed;
            byteString = ByteString.FromBase64(logs);
            offerMadeLogs = OfferMade.Parser.ParseFrom(byteString);
            offerMadeLogs.Symbol.ShouldBe(symbol);
            offerMadeLogs.TokenId.ShouldBe(tokenId);
            offerMadeLogs.OfferFrom.ShouldBe(OtherAccount.ConvertAddress());
            offerMadeLogs.OfferTo.ShouldBe(InitAccount.ConvertAddress());
            offerMadeLogs.Price.Symbol.ShouldBe(purchaseSymbol);
            offerMadeLogs.Price.Amount.ShouldBe(fixedPrice);
            offerMadeLogs.Quantity.ShouldBe(1);

            //11.tart_time < UtcNow <public_time, Enter amount < fixedPrice 
            _nftMarketContract.SetAccount(OtherAccount1);
            var result11 = _nftMarketContract.MakeOffer(
                symbol,
                tokenId,
                InitAccount,
                buyAmount,
                new Price
                {
                    Symbol = purchaseSymbol,
                    Amount = fixedPrice - 1_00000000
                },
                expireTime
            );
            var OfferList3 = _nftMarketContract.GetOfferList(symbol, tokenId, OtherAccount1);
            Logger.Info($"OfferList2 is {OfferList3}");
            result11.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);
            // Check event
            logs = result11.Logs.First(l => l.Name.Equals("OfferAdded")).NonIndexed;
            byteString = ByteString.FromBase64(logs);
            offerAddedLogs = OfferAdded.Parser.ParseFrom(byteString);
            offerAddedLogs.Symbol.ShouldBe(symbol);
            offerAddedLogs.TokenId.ShouldBe(tokenId);
            offerAddedLogs.OfferFrom.ShouldBe(OtherAccount1.ConvertAddress());
            offerAddedLogs.OfferTo.ShouldBe(InitAccount.ConvertAddress());
            offerAddedLogs.ExpireTime.ShouldBe(expireTime);
            offerAddedLogs.Price.Symbol.ShouldBe(purchaseSymbol);
            offerAddedLogs.Price.Amount.ShouldBe(fixedPrice - 1_00000000);
            // Check event
            logs = result11.Logs.First(l => l.Name.Equals("OfferMade")).NonIndexed;
            byteString = ByteString.FromBase64(logs);
            offerMadeLogs = OfferMade.Parser.ParseFrom(byteString);
            offerMadeLogs.Symbol.ShouldBe(symbol);
            offerMadeLogs.TokenId.ShouldBe(tokenId);
            offerMadeLogs.OfferFrom.ShouldBe(OtherAccount1.ConvertAddress());
            offerMadeLogs.OfferTo.ShouldBe(InitAccount.ConvertAddress());
            offerMadeLogs.Price.Symbol.ShouldBe(purchaseSymbol);
            offerMadeLogs.Price.Amount.ShouldBe(fixedPrice - 1_00000000);
            offerMadeLogs.Quantity.ShouldBe(1);

            //11.2.second Offer
            _nftMarketContract.SetAccount(OtherAccount);
            var resultsecond = _nftMarketContract.MakeOffer(
                symbol,
                tokenId,
                InitAccount,
                buyAmount,
                new Price
                {
                    Symbol = purchaseSymbol,
                    Amount = fixedPrice + 1_00000000
                },
                expireTime
            );
            resultsecond.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);
            var OfferList4 = _nftMarketContract.GetOfferList(symbol, tokenId, OtherAccount);
            Logger.Info($"OfferList4 is {OfferList4}");
            // Check event
            logs = resultsecond.Logs.First(l => l.Name.Equals("OfferChanged")).NonIndexed;
            byteString = ByteString.FromBase64(logs);
            var offerChanged = OfferChanged.Parser.ParseFrom(byteString);
            offerChanged.Symbol.ShouldBe(symbol);
            offerChanged.TokenId.ShouldBe(tokenId);
            offerChanged.OfferFrom.ShouldBe(OtherAccount.ConvertAddress());
            offerChanged.OfferTo.ShouldBe(InitAccount.ConvertAddress());
            offerChanged.ExpireTime.ShouldBe(expireTime);
            offerChanged.Price.Symbol.ShouldBe(purchaseSymbol);
            offerChanged.Price.Amount.ShouldBe(fixedPrice);
            // Check event
            logs = resultsecond.Logs.First(l => l.Name.Equals("OfferMade")).NonIndexed;
            byteString = ByteString.FromBase64(logs);
            offerMadeLogs = OfferMade.Parser.ParseFrom(byteString);
            offerMadeLogs.Symbol.ShouldBe(symbol);
            offerMadeLogs.TokenId.ShouldBe(tokenId);
            offerMadeLogs.OfferFrom.ShouldBe(OtherAccount.ConvertAddress());
            offerMadeLogs.OfferTo.ShouldBe(InitAccount.ConvertAddress());
            offerMadeLogs.Price.Symbol.ShouldBe(purchaseSymbol);
            offerMadeLogs.Price.Amount.ShouldBe(fixedPrice);
            offerMadeLogs.Quantity.ShouldBe(1);
        }


        [TestMethod]
        public void ListWithFixedPriceblack()
        {
            var tokenId = 1;
            var totalAmount = 1000;
            var totalSupply = 10000;
            var mintAmount = 1000;
            var sellAmount = 100;
            var fixedPrice = 6_00000000;
            var whitePrice1 = 9_00000000;
            var whitePrice11 = 9_10000000;
            var whitePrice12 = 9_20000000;
            var whitePrice2 = 10_00000000;
            var whitePrice21 = 10_10000000;
            var whitePrice3 = 11_00000000;
            var buyAmount = 1;
            var expireTime = DateTime.UtcNow.AddSeconds(10).ToTimestamp();

            var startTime = DateTime.UtcNow.ToTimestamp();
            var publicTime = DateTime.UtcNow.AddSeconds(20).ToTimestamp();
            var durationHours = 24;
            var symbol = CreateAndMint(totalSupply, mintAmount, tokenId);
            Logger.Info($"startTime is {startTime.Seconds}");
            Logger.Info($"publicTime is {publicTime.Seconds}");
            var purchaseSymbol = "USDT";
            var isMerge = true;

            _nftContract.SetAccount(InitAccount);
            ListWithFixedPrice(symbol, tokenId, sellAmount, fixedPrice, whitePrice1, whitePrice11, whitePrice12,
                whitePrice2, whitePrice21,
                whitePrice3, startTime, publicTime, durationHours, purchaseSymbol, isMerge);


            // Approve
            _nftContract.SetAccount(WhiteListAddress1);
            var approve = _tokenContract.ApproveToken(WhiteListAddress1, _nftMarketContract.ContractAddress,
                10000000000_00000000, purchaseSymbol);
            approve.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            _nftContract.SetAccount(OtherAccount);
            var approve1 = _tokenContract.ApproveToken(OtherAccount, _nftMarketContract.ContractAddress,
                10000000000_00000000, purchaseSymbol);
            approve1.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            _nftContract.SetAccount(OtherAccount1);
            var approve2 = _tokenContract.ApproveToken(OtherAccount1, _nftMarketContract.ContractAddress,
                10000000000_00000000, purchaseSymbol);
            approve2.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            //5.start_time  <public_time  <   UtcNow.
            var getBalanceBuyerStart = _nftContract.GetBalance(WhiteListAddress1, symbol, tokenId);
            Logger.Info($"WhiteListAddress1Balance is {getBalanceBuyerStart.Balance}");
            var getBalancesellerStart = _nftContract.GetBalance(InitAccount, symbol, tokenId);
            Logger.Info($"InitAccountBalance is {getBalancesellerStart}");
            var balanceStart = _tokenContract.GetUserBalance(serviceAddress, purchaseSymbol);
            Logger.Info($"balance is {balanceStart}");
            _nftMarketContract.SetAccount(WhiteListAddress1);
            var FixedPrice = _nftMarketContract.MakeOffer(
                symbol,
                tokenId,
                InitAccount,
                1,
                new Price
                {
                    Symbol = purchaseSymbol,
                    Amount = whitePrice1
                },
                expireTime
            );
            FixedPrice.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);

            var getBalanceBuyerFinish = _nftContract.GetBalance(WhiteListAddress1, symbol, tokenId);
            Logger.Info($"WhiteListAddress1Balance is {getBalanceBuyerFinish.Balance}");
            getBalanceBuyerFinish.Balance.ShouldBe(getBalanceBuyerStart.Balance + 1);
            var getBalancesellerFinish = _nftContract.GetBalance(InitAccount, symbol, tokenId);
            Logger.Info($"InitAccountBalance is {getBalancesellerFinish.Balance}");
            getBalancesellerFinish.Balance.ShouldBe(getBalancesellerStart.Balance - 1);
            var balanceFinish = _tokenContract.GetUserBalance(serviceAddress, purchaseSymbol);
            Logger.Info($"balance is {balanceFinish}");
            balanceFinish.ShouldBe(balanceStart + 900000);

            //OtherAccount
            var getBalanceOtherAccountStart = _nftContract.GetBalance(OtherAccount, symbol, tokenId);
            Logger.Info($"getBalanceOtherAccountStart is {getBalanceOtherAccountStart.Balance}");
            var getBalanceInitAccountStart = _nftContract.GetBalance(InitAccount, symbol, tokenId);
            Logger.Info($"getBalanceInitAccountStart is {getBalanceInitAccountStart}");
            var balanceserviceAddressStart1 = _tokenContract.GetUserBalance(serviceAddress, purchaseSymbol);
            Logger.Info($"balance is {balanceserviceAddressStart1}");
            _nftMarketContract.SetAccount(OtherAccount);
            var result = _nftMarketContract.MakeOffer(
                symbol,
                tokenId,
                InitAccount,
                1,
                new Price
                {
                    Symbol = purchaseSymbol,
                    Amount = fixedPrice
                },
                expireTime
            );
            result.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);
            var offerList = _nftMarketContract.GetOfferList(symbol, tokenId, OtherAccount);
            Logger.Info($"offerList is {offerList}");

            var getBalanceWhiteListAddress1Finish = _nftContract.GetBalance(OtherAccount, symbol, tokenId);
            Logger.Info($"getBalanceWhiteListAddress1Finish is {getBalanceWhiteListAddress1Finish.Balance}");
            getBalanceWhiteListAddress1Finish.Balance.ShouldBe(getBalanceOtherAccountStart.Balance + 1);
            var getBalanceInitAccountFinish = _nftContract.GetBalance(InitAccount, symbol, tokenId);
            Logger.Info($"InitAccountBalance is {getBalanceInitAccountFinish.Balance}");
            getBalanceInitAccountFinish.Balance.ShouldBe(getBalanceInitAccountStart.Balance - 1);
            var balanceserviceAddressFinish = _tokenContract.GetUserBalance(serviceAddress, purchaseSymbol);
            Logger.Info($"balanceserviceAddressFinish is {balanceserviceAddressFinish}");
            balanceserviceAddressFinish.ShouldBe(balanceserviceAddressStart1 + 600000);

            _nftMarketContract.SetAccount(OtherAccount1);
            var result1 = _nftMarketContract.MakeOffer(
                symbol,
                tokenId,
                InitAccount,
                100,
                new Price
                {
                    Symbol = purchaseSymbol,
                    Amount = fixedPrice
                },
                expireTime.AddSeconds(1)
            );
            result1.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);
            var NFT = _nftContract.GetBalance(OtherAccount1, symbol, tokenId);
            Logger.Info($"NFT is {NFT.Balance}");
            var offerList1 = _nftMarketContract.GetOfferList(symbol, tokenId, OtherAccount1);
            Logger.Info($"offerList1 is {offerList1}");
        }

        [TestMethod]
        public void ListWithFixedPrice1()
        {
            var tokenId = 1;
            var totalAmount = 1000;
            var totalSupply = 10000;
            var mintAmount = 1000;
            var sellAmount = 100;
            var fixedPrice = 12_00000000;
            var whitePrice1 = 9_00000000;
            var whitePrice11 = 9_10000000;
            var whitePrice12 = 9_20000000;
            var whitePrice2 = 10_00000000;
            var whitePrice21 = 10_10000000;
            var whitePrice3 = 11_00000000;
            var buyAmount = 1;
            var expireTime = DateTime.UtcNow.AddSeconds(40).ToTimestamp();
            var startTime = DateTime.UtcNow.ToTimestamp();
            var publicTime = DateTime.UtcNow.AddSeconds(20).ToTimestamp();
            var durationHours = 1111;
            var symbol = CreateAndMint(totalSupply, mintAmount, tokenId);
            Logger.Info($"startTime is {startTime.Seconds}");
            Logger.Info($"publicTime is {publicTime.Seconds}");
            var purchaseSymbol = "USDT";
            var isMerge = true;

            ListWithFixedPrice(symbol, tokenId, sellAmount, fixedPrice, whitePrice1, whitePrice11, whitePrice12,
                whitePrice2, whitePrice21,
                whitePrice3, startTime, publicTime, durationHours, purchaseSymbol, isMerge);

            // Approve
            _nftContract.SetAccount(WhiteListAddress1);
            var approve = _tokenContract.ApproveToken(WhiteListAddress1, _nftMarketContract.ContractAddress,
                10000000000_00000000, purchaseSymbol);
            approve.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            _nftContract.SetAccount(OtherAccount);
            var approve1 = _tokenContract.ApproveToken(OtherAccount, _nftMarketContract.ContractAddress,
                10000000000_00000000, purchaseSymbol);
            approve1.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            _nftContract.SetAccount(OtherAccount1);
            var approve2 = _tokenContract.ApproveToken(OtherAccount1, _nftMarketContract.ContractAddress,
                10000000000_00000000, purchaseSymbol);
            approve2.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            //6.Enter amount< whitePrice,public_time< UtcNow 
            _nftMarketContract.SetAccount(WhiteListAddress1);
            var result6 = _nftMarketContract.MakeOffer(
                symbol,
                tokenId,
                InitAccount,
                buyAmount,
                new Price
                {
                    Symbol = purchaseSymbol,
                    Amount = whitePrice1 - 3_00000000
                },
                expireTime
            );
            var offerList = _nftMarketContract.GetOfferList(symbol, tokenId, WhiteListAddress1);
            offerList.Value.Count.ShouldBe(1);
            offerList.Value.First().From.ShouldBe(WhiteListAddress1.ConvertAddress());
            offerList.Value.First().To.ShouldBe(InitAccount.ConvertAddress());
            offerList.Value.First().Price.Symbol.ShouldBe(purchaseSymbol);
            offerList.Value.First().Price.Amount.ShouldBe(6_00000000);
            result6.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            //7.whitePrice <=Enter amount < fixedPrice  ,  public_time  < UtcNow  
            var balanceStart1 = _tokenContract.GetUserBalance(WhiteListAddress1, purchaseSymbol);
            Logger.Info($"balanceStart is {balanceStart1}");
            _nftMarketContract.SetAccount(WhiteListAddress1);
            var getBalanceBuyerStart = _nftContract.GetBalance(WhiteListAddress1, symbol, tokenId);
            Logger.Info($"WhiteListAddress1Balance is {getBalanceBuyerStart.Balance}");
            var getBalancesellerStart = _nftContract.GetBalance(InitAccount, symbol, tokenId);
            Logger.Info($"InitAccountBalance is {getBalancesellerStart}");
            var balanceStart = _tokenContract.GetUserBalance(serviceAddress, purchaseSymbol);
            Logger.Info($"balance is {balanceStart}");

            _nftMarketContract.SetAccount(WhiteListAddress1);
            var result7 = _nftMarketContract.MakeOffer(
                symbol,
                tokenId,
                InitAccount,
                buyAmount,
                new Price
                {
                    Symbol = purchaseSymbol,
                    Amount = whitePrice1 + 2_00000000
                },
                expireTime
            );
            result7.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var OfferList7 = _nftMarketContract.GetOfferList(symbol, tokenId, WhiteListAddress1);
            Logger.Info($"OfferList1 is {OfferList7}");
            var balanceFinish1 = _tokenContract.GetUserBalance(WhiteListAddress1, purchaseSymbol);
            Logger.Info($"balanceFinish is {balanceFinish1}");
            var getBalanceBuyerFinish = _nftContract.GetBalance(WhiteListAddress1, symbol, tokenId);
            Logger.Info($"getBalanceBuyerFinish is {getBalanceBuyerFinish.Balance}");
            getBalanceBuyerFinish.Balance.ShouldBe(getBalanceBuyerStart.Balance + 1);
            var getBalancesellerFinish = _nftContract.GetBalance(InitAccount, symbol, tokenId);
            Logger.Info($"getBalancesellerFinish is {getBalancesellerFinish.Balance}");
            getBalancesellerFinish.Balance.ShouldBe(getBalancesellerStart.Balance - 1);
            var balanceFinish = _tokenContract.GetUserBalance(serviceAddress, purchaseSymbol);
            Logger.Info($"balanceFinish is {balanceFinish}");
            balanceFinish.ShouldBe(balanceStart + 900000);


            //12.whitePrice < = Enter amount < fixedPrice   ;   public_time  <  UtcNow  
            var OfferList = _nftMarketContract.GetOfferList(symbol, tokenId, InitAccount);
            OfferList.ShouldBe(new OfferList());

            _nftMarketContract.SetAccount(OtherAccount);
            var result12 = _nftMarketContract.MakeOffer(
                symbol,
                tokenId,
                InitAccount,
                buyAmount,
                new Price
                {
                    Symbol = purchaseSymbol,
                    Amount = fixedPrice - 1_00000000
                },
                expireTime
            );
            result12.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var offerList1 = _nftMarketContract.GetOfferList(symbol, tokenId, OtherAccount);
            Logger.Info($"offerList1 is {offerList1}");
            offerList1.Value.Count.ShouldBe(1);
            offerList1.Value[0].From.ShouldBe(OtherAccount.ConvertAddress());
            offerList1.Value.First().To.ShouldBe(InitAccount.ConvertAddress());
            offerList1.Value.First().Price.Symbol.ShouldBe(purchaseSymbol);
            offerList1.Value.First().Price.Amount.ShouldBe(11_00000000);

            //13.whitePrice < fixedPrice < = Enter amount   ;   public_time  <  UtcNow  
            var balanceStart13 = _tokenContract.GetUserBalance(OtherAccount1, purchaseSymbol);
            Logger.Info($"balanceStart13 is {balanceStart13}");

            var getBalanceOtherAccountStart = _nftContract.GetBalance(OtherAccount1, symbol, tokenId);
            Logger.Info($"getBalanceOtherAccountStart is {getBalanceOtherAccountStart.Balance}");
            var getBalanceInitAccountStart = _nftContract.GetBalance(InitAccount, symbol, tokenId);
            Logger.Info($"getBalanceInitAccountStart is {getBalanceInitAccountStart.Balance}");

            var balanceserviceAddressStart = _tokenContract.GetUserBalance(serviceAddress, purchaseSymbol);
            Logger.Info($"balanceserviceAddressStart is {balanceserviceAddressStart}");


            _nftMarketContract.SetAccount(OtherAccount1);
            _nftMarketContract.MakeOffer(
                symbol,
                tokenId,
                InitAccount,
                buyAmount,
                new Price
                {
                    Symbol = purchaseSymbol,
                    Amount = 22_00000000
                },
                expireTime
            );
            var offerList2 = _nftMarketContract.GetOfferList(symbol, tokenId, OtherAccount1);
            Logger.Info($"offerList2 is {offerList2}");


            var balanceFinish13 = _tokenContract.GetUserBalance(OtherAccount1, purchaseSymbol);
            Logger.Info($"balanceFinish13 is {balanceFinish13}");

            var getBalanceOtherAccountFinish = _nftContract.GetBalance(OtherAccount1, symbol, tokenId);
            Logger.Info($"getBalanceOtherAccountFinish is {getBalanceOtherAccountFinish.Balance}");
            getBalanceOtherAccountFinish.Balance.ShouldBe(getBalanceOtherAccountStart.Balance + 1);

            var getBalanceInitAccountFinish = _nftContract.GetBalance(InitAccount, symbol, tokenId);
            Logger.Info($"getBalanceInitAccountFinish is {getBalanceInitAccountFinish.Balance}");
            getBalanceInitAccountFinish.Balance.ShouldBe(getBalanceInitAccountStart.Balance - 1);

            var balanceserviceAddressFinish = _tokenContract.GetUserBalance(serviceAddress, purchaseSymbol);
            Logger.Info($"balanceFinish is {balanceserviceAddressFinish}");
            balanceserviceAddressFinish.ShouldBe(balanceserviceAddressStart + 1200000);
        }


        [TestMethod]
        //ListWithFixedPrice
        public void ListWithFixedPrice2()
        {
            var tokenId = 1;
            var totalAmount = 1000;
            var totalSupply = 10000;
            var mintAmount = 1000;
            var sellAmount = 100;
            var fixedPrice = 12_00000000;
            var whitePrice1 = 9_00000000;
            var whitePrice11 = 9_10000000;
            var whitePrice12 = 9_20000000;
            var whitePrice2 = 10_00000000;
            var whitePrice21 = 10_10000000;
            var whitePrice3 = 11_00000000;
            var buyAmount = 1;
            var expireTime = DateTime.UtcNow.AddSeconds(10).ToTimestamp();

            var startTime = DateTime.UtcNow.ToTimestamp();
            var publicTime = DateTime.UtcNow.AddSeconds(20).ToTimestamp();
            var durationHours = 1;
            //var symbol = CreateAndMint(totalSupply, mintAmount, tokenId);
            var symbol = "CO446965316";
            Logger.Info($"startTime is {startTime.Seconds}");
            Logger.Info($"publicTime is {publicTime.Seconds}");
            var purchaseSymbol = "USDT";
            var isMerge = true;

            //Symbol:CO446965316
            //NFT:tUFuzEQAtG5rvY2LyVahGZazksnnyfBjGVpwdF5LH3QoteLVV
            //NFTMarket：orFEjYJsZ6T9bpguLmuVL6w34aC5EwD26S5e64u4FrrZqRuV2
            /*
            _nftContract.SetAccount(InitAccount);
            ListWithFixedPrice(symbol, tokenId, sellAmount, fixedPrice, whitePrice1, whitePrice11, whitePrice12,
                whitePrice2, whitePrice21,
                whitePrice3, startTime, publicTime, durationHours, purchaseSymbol, isMerge);
            */

            // Approve
            _nftContract.SetAccount(WhiteListAddress1);
            var approve = _tokenContract.ApproveToken(WhiteListAddress1, _nftMarketContract.ContractAddress,
                10000000000_00000000, purchaseSymbol);
            approve.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            _nftContract.SetAccount(OtherAccount);
            var approve2 = _tokenContract.ApproveToken(OtherAccount, _nftMarketContract.ContractAddress,
                10000000000_00000000, purchaseSymbol);
            approve2.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);


            //9.1DurationHours <  UtcNow  ;      whitePrice< fixedPrice<Enter amount   , 
            _nftMarketContract.SetAccount(WhiteListAddress2);
            var result10 = _nftMarketContract.MakeOffer(
                symbol,
                tokenId,
                InitAccount,
                buyAmount,
                new Price
                {
                    Symbol = purchaseSymbol,
                    Amount = whitePrice2 + 10_00000000
                },
                expireTime
            );

            result10.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var offerList1 = _nftMarketContract.GetOfferList(symbol, tokenId, WhiteListAddress2);
            Logger.Info($"offerList1 is {offerList1}");
            offerList1.Value.Count.ShouldBe(1);
            offerList1.Value[0].From.ShouldBe(WhiteListAddress2.ConvertAddress());
            offerList1.Value.First().To.ShouldBe(InitAccount.ConvertAddress());
            offerList1.Value.First().Price.Symbol.ShouldBe(purchaseSymbol);
            offerList1.Value.First().Price.Amount.ShouldBe(20_00000000);

            //9.DurationHours <  UtcNow  ;    Enter amount  < whitePrice< fixedPrice  , 
            _nftMarketContract.SetAccount(WhiteListAddress1);
            var result9 = _nftMarketContract.MakeOffer(
                symbol,
                tokenId,
                InitAccount,
                buyAmount,
                new Price
                {
                    Symbol = purchaseSymbol,
                    Amount = whitePrice1 - 1_00000000
                },
                expireTime
            );
            result9.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var offerList = _nftMarketContract.GetOfferList(symbol, tokenId, WhiteListAddress1);
            Logger.Info($"offerList is {offerList}");
            offerList.Value.Count.ShouldBe(1);
            offerList.Value.First().From.ShouldBe(WhiteListAddress1.ConvertAddress());
            offerList.Value.First().To.ShouldBe(InitAccount.ConvertAddress());
            offerList.Value.First().Price.Symbol.ShouldBe(purchaseSymbol);
            offerList.Value.First().Price.Amount.ShouldBe(8_00000000);

            //9.3.owner——start_time <public_time< UtcNow,fixedPrice < = whitePrice
            // Approve
            _nftContract.SetAccount(InitAccount);
            var approve1 = _tokenContract.ApproveToken(InitAccount, _nftMarketContract.ContractAddress,
                10000000000_00000000, "USDT");
            approve1.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            _nftMarketContract.SetAccount(InitAccount);
            var result = _nftMarketContract.MakeOffer(
                symbol,
                tokenId,
                InitAccount,
                buyAmount,
                new Price
                {
                    Symbol = purchaseSymbol,
                    Amount = fixedPrice + 10
                },
                expireTime
            );
            result.Error.ShouldContain("Origin owner cannot be sender himself.");

            //14.whitePrice < = Enter amount < fixedPrice   ;   DurationHours <  UtcNow  

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
                    Symbol = purchaseSymbol,
                    Amount = fixedPrice
                },
                expireTime
            );
            OfferList = _nftMarketContract.GetOfferList(symbol, tokenId, OtherAccount);
            OfferList.Value.Count.ShouldBe(1);
            OfferList.Value.First().From.ShouldBe(OtherAccount.ConvertAddress());
            OfferList.Value.First().To.ShouldBe(InitAccount.ConvertAddress());
            OfferList.Value.First().Price.Symbol.ShouldBe(purchaseSymbol);
            OfferList.Value.First().Price.Amount.ShouldBe(fixedPrice);
            OfferList.Value.First().Quantity.ShouldBe(buyAmount);
            OfferList.Value.First().ExpireTime.ShouldBe(expireTime);
        }


        [TestMethod]
        //ListWithFixedPrice
        public void ListWithFixedPrice3()
        {
            var tokenId = 1;
            var totalAmount = 1000;
            var totalSupply = 10000;
            var mintAmount = 1000;
            var sellAmount = 100;
            var fixedPrice = 12_00000000;
            var whitePrice1 = 9_00000000;
            var whitePrice11 = 9_10000000;
            var whitePrice12 = 9_20000000;
            var whitePrice2 = 10_00000000;
            var whitePrice21 = 10_10000000;
            var whitePrice3 = 11_00000000;
            var buyAmount = 1;
            var expireTime = DateTime.UtcNow.AddSeconds(10).ToTimestamp();

            var startTime = DateTime.UtcNow.ToTimestamp();
            var publicTime = DateTime.UtcNow.AddDays(20).ToTimestamp();
            var durationHours = 1;
            //var symbol = CreateAndMint(totalSupply, mintAmount, tokenId);
            var symbol = "CO688173734";
            Logger.Info($"startTime is {startTime.Seconds}");
            Logger.Info($"publicTime is {publicTime.Seconds}");
            var purchaseSymbol = "USDT";
            var isMerge = true;

            //symbol:CO688173734
            //NFT:UfWgFq7heWJj1QokH4E3wczg9ckmRgUJMyvhrtHqZfzt9Yyds
            //NFTMarket：KLTr4ZSEt5F5AALHnCc9Nq76FSkBA8cBbf79Dxikpx9W5En7W

            /*
            _nftContract.SetAccount(InitAccount);
             ListWithFixedPrice(symbol, tokenId, sellAmount, fixedPrice, whitePrice1, whitePrice11, whitePrice12,
                whitePrice2, whitePrice21,
                whitePrice3, startTime, publicTime, durationHours, purchaseSymbol, isMerge);
              */

            // Approve
            _nftContract.SetAccount(WhiteListAddress1);
            var approve = _tokenContract.ApproveToken(WhiteListAddress1, _nftMarketContract.ContractAddress,
                10000000000_00000000, purchaseSymbol);
            approve.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            _nftContract.SetAccount(OtherAccount);
            var approve2 = _tokenContract.ApproveToken(OtherAccount, _nftMarketContract.ContractAddress,
                10000000000_00000000, purchaseSymbol);
            approve2.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            //9.2
            _nftMarketContract.SetAccount(OtherAccount);
            var result1 = _nftMarketContract.MakeOffer(
                symbol,
                tokenId,
                InitAccount,
                buyAmount,
                new Price
                {
                    Symbol = purchaseSymbol,
                    Amount = fixedPrice
                },
                expireTime
            );
            result1.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var offerList = _nftMarketContract.GetOfferList(symbol, tokenId, OtherAccount);
            Logger.Info($"offerList is {offerList}");
            offerList.Value.Count.ShouldBe(1);
            offerList.Value[0].From.ShouldBe(OtherAccount.ConvertAddress());
            offerList.Value[0].To.ShouldBe(InitAccount.ConvertAddress());
            offerList.Value[0].Price.Symbol.ShouldBe(purchaseSymbol);
            offerList.Value[0].Price.Amount.ShouldBe(fixedPrice);

            _nftMarketContract.SetAccount(WhiteListAddress1);
            var result2 = _nftMarketContract.MakeOffer(
                symbol,
                tokenId,
                InitAccount,
                buyAmount,
                new Price
                {
                    Symbol = purchaseSymbol,
                    Amount = fixedPrice
                },
                expireTime
            );
            result2.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var getBalanceIWhiteListAddressFinish = _nftContract.GetBalance(WhiteListAddress1, symbol, tokenId);
            Logger.Info($"getBalanceIWhiteListAddressFinish is {getBalanceIWhiteListAddressFinish.Balance}");

            var offerList1 = _nftMarketContract.GetOfferList(symbol, tokenId, WhiteListAddress1);
            Logger.Info($"offerList1 is {offerList1}");
            offerList1.Value.Count.ShouldBe(1);
            offerList1.Value[0].From.ShouldBe(WhiteListAddress1.ConvertAddress());
            offerList1.Value[0].To.ShouldBe(InitAccount.ConvertAddress());
            offerList1.Value[0].Price.Symbol.ShouldBe(purchaseSymbol);
            offerList1.Value[0].Price.Amount.ShouldBe(fixedPrice);
        }


        [TestMethod]
        //ListWithFixedPrice
        public void ListWithFixedPriceWhiteList()
        {
            var tokenId = 1;
            var totalAmount = 1000;
            var totalSupply = 10000;
            var mintAmount = 1000;
            var sellAmount = 100;
            var fixedPrice = 12_00000000;
            var whitePrice1 = 9_00000000;
            var whitePrice11 = 9_10000000;
            var whitePrice12 = 9_20000000;
            var whitePrice2 = 10_00000000;
            var whitePrice21 = 10_10000000;
            var whitePrice3 = 11_00000000;
            var buyAmount = 1;
            var expireTime = DateTime.UtcNow.AddSeconds(10).ToTimestamp();

            var startTime = DateTime.UtcNow.ToTimestamp();
            var publicTime = DateTime.UtcNow.AddDays(20).ToTimestamp();
            var durationHours = 1;
            var symbol = CreateAndMint(totalSupply, mintAmount, tokenId);
            Logger.Info($"startTime is {startTime.Seconds}");
            Logger.Info($"publicTime is {publicTime.Seconds}");
            var purchaseSymbol = "USDT";
            var isMerge = true;

            ListWithFixedPrice(symbol, tokenId, sellAmount, fixedPrice, whitePrice1, whitePrice11, whitePrice12,
                whitePrice2, whitePrice21,
                whitePrice3, startTime, publicTime, durationHours, purchaseSymbol, isMerge);
            // Approve
            _nftContract.SetAccount(WhiteListAddress1);
            var approve = _tokenContract.ApproveToken(WhiteListAddress1, _nftMarketContract.ContractAddress,
                10000000000_00000000, purchaseSymbol);
            approve.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            // Approve
            _nftContract.SetAccount(WhiteListAddress2);
            var approve2 = _tokenContract.ApproveToken(WhiteListAddress2, _nftMarketContract.ContractAddress,
                10000000000_00000000, purchaseSymbol);
            approve2.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            var getBalanceIWhiteListAddressStart = _nftContract.GetBalance(WhiteListAddress1, symbol, tokenId);
            Logger.Info($"getBalanceIWhiteListAddressStart is {getBalanceIWhiteListAddressStart.Balance}");
            //First
            _nftMarketContract.SetAccount(WhiteListAddress1);
            var result10 = _nftMarketContract.MakeOffer(
                symbol,
                tokenId,
                InitAccount,
                2,
                new Price
                {
                    Symbol = purchaseSymbol,
                    Amount = whitePrice12
                },
                expireTime
            );
            var getBalanceIWhiteListAddressFinish = _nftContract.GetBalance(WhiteListAddress1, symbol, tokenId);
            Logger.Info($"getBalanceIWhiteListAddressFinish is {getBalanceIWhiteListAddressFinish.Balance}");
            getBalanceIWhiteListAddressFinish.Balance.ShouldBe(getBalanceIWhiteListAddressStart.Balance + 1);

            result10.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var offerList = _nftMarketContract.GetOfferList(symbol, tokenId, WhiteListAddress1);
            Logger.Info($"offerList1 is {offerList}");
            offerList.Value.Count.ShouldBe(1);
            offerList.Value[0].From.ShouldBe(WhiteListAddress1.ConvertAddress());
            offerList.Value.First().To.ShouldBe(InitAccount.ConvertAddress());
            offerList.Value.First().Price.Symbol.ShouldBe(purchaseSymbol);
            offerList.Value.First().Price.Amount.ShouldBe(whitePrice12);

            //second
            var balanceStart1 = _tokenContract.GetUserBalance(WhiteListAddress1, purchaseSymbol);
            Logger.Info($"balanceStart is {balanceStart1}");
            _nftMarketContract.SetAccount(WhiteListAddress1);
            var getBalanceBuyerStart = _nftContract.GetBalance(WhiteListAddress1, symbol, tokenId);
            Logger.Info($"WhiteListAddress1Balance is {getBalanceBuyerStart.Balance}");
            var getBalancesellerStart = _nftContract.GetBalance(InitAccount, symbol, tokenId);
            Logger.Info($"InitAccountBalance is {getBalancesellerStart.Balance}");
            var balanceStart = _tokenContract.GetUserBalance(serviceAddress, purchaseSymbol);
            Logger.Info($"balance is {balanceStart}");

            _nftMarketContract.SetAccount(WhiteListAddress1);
            var result1 = _nftMarketContract.MakeOffer(
                symbol,
                tokenId,
                InitAccount,
                1,
                new Price
                {
                    Symbol = purchaseSymbol,
                    Amount = whitePrice12
                },
                expireTime
            );
            result1.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            var balanceFinish1 = _tokenContract.GetUserBalance(WhiteListAddress1, purchaseSymbol);
            Logger.Info($"balanceFinish is {balanceFinish1}");
            var getBalanceBuyerFinish = _nftContract.GetBalance(WhiteListAddress1, symbol, tokenId);
            Logger.Info($"getBalanceBuyerFinish is {getBalanceBuyerFinish.Balance}");
            getBalanceBuyerFinish.Balance.ShouldBe(getBalanceBuyerStart.Balance + 1);
            var getBalancesellerFinish = _nftContract.GetBalance(InitAccount, symbol, tokenId);
            Logger.Info($"getBalancesellerFinish is {getBalancesellerFinish.Balance}");
            getBalancesellerFinish.Balance.ShouldBe(getBalancesellerStart.Balance - 1);
            var balanceFinish = _tokenContract.GetUserBalance(serviceAddress, purchaseSymbol);
            Logger.Info($"balanceFinish is {balanceFinish}");
            balanceFinish.ShouldBe(balanceStart + 910000);

            //repeatedly
            var balanceStart2 = _tokenContract.GetUserBalance(WhiteListAddress2, purchaseSymbol);
            Logger.Info($"balanceStart2 is {balanceStart2}");
            _nftMarketContract.SetAccount(WhiteListAddress2);
            var getBalanceBuyerStart2 = _nftContract.GetBalance(WhiteListAddress2, symbol, tokenId);
            Logger.Info($"getBalanceBuyerStart2 is {getBalanceBuyerStart2.Balance}");
            var getBalancesellerStart2 = _nftContract.GetBalance(InitAccount, symbol, tokenId);
            Logger.Info($"getBalancesellerStart2 is {getBalancesellerStart2.Balance}");
            var balanceserviceAddressStart2 = _tokenContract.GetUserBalance(serviceAddress, purchaseSymbol);
            Logger.Info($"balanceserviceAddressStart2 is {balanceserviceAddressStart2}");

            _nftMarketContract.SetAccount(WhiteListAddress2);
            var result2 = _nftMarketContract.MakeOffer(
                symbol,
                tokenId,
                InitAccount,
                2,
                new Price
                {
                    Symbol = purchaseSymbol,
                    Amount = whitePrice2 + 10_00000000
                },
                expireTime.AddSeconds(10)
            );
            result2.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var offerList2 = _nftMarketContract.GetOfferList(symbol, tokenId, WhiteListAddress2);
            Logger.Info($"offerList2 is {offerList2}");

            var balanceFinish2 = _tokenContract.GetUserBalance(WhiteListAddress2, purchaseSymbol);
            Logger.Info($"balanceFinish2 is {balanceFinish2}");
            var getBalanceBuyerFinish2 = _nftContract.GetBalance(WhiteListAddress2, symbol, tokenId);
            Logger.Info($"getBalanceBuyerFinish2 is {getBalanceBuyerFinish2.Balance}");
            getBalanceBuyerFinish2.Balance.ShouldBe(getBalanceBuyerStart2.Balance + 1);
            var getBalancesellerFinish2 = _nftContract.GetBalance(InitAccount, symbol, tokenId);
            Logger.Info($"getBalancesellerFinish2 is {getBalancesellerFinish2.Balance}");
            getBalancesellerFinish2.Balance.ShouldBe(getBalancesellerStart2.Balance - 1);
            var balanceserviceAddressFinish2 = _tokenContract.GetUserBalance(serviceAddress, purchaseSymbol);
            Logger.Info($"balanceFinish is {balanceserviceAddressFinish2}");
            balanceserviceAddressFinish2.ShouldBe(balanceserviceAddressStart2 + 1000000);
        }


        [TestMethod]
        public void ListWithFixedPriceNFT()
        {
            var tokenId = 1;
            var totalAmount = 1000;
            var totalSupply = 10000;
            var mintAmount = 1000;
            var sellAmount = 100;
            var fixedPrice = 12;
            var whitePrice1 = 9;
            var whitePrice11 = 9;
            var whitePrice12 = 9;
            var whitePrice2 = 10;
            var whitePrice21 = 10;
            var whitePrice3 = 11;
            var buyAmount = 1;
            var expireTime = DateTime.UtcNow.AddSeconds(10).ToTimestamp();

            var startTime = DateTime.UtcNow.ToTimestamp();
            var publicTime = DateTime.UtcNow.AddSeconds(20).ToTimestamp();
            var durationHours = 24;
            var symbol = CreateAndMint(totalSupply, mintAmount, tokenId);
            Logger.Info($"startTime is {startTime.Seconds}");
            Logger.Info($"publicTime is {publicTime.Seconds}");

            var purchaseSymbol = CreateAndMint1(WhiteListAddress1, totalSupply, mintAmount, tokenId);
            var isMerge = true;

            _nftContract.SetAccount(InitAccount);
            ListWithFixedPrice(symbol, tokenId, sellAmount, fixedPrice, whitePrice1, whitePrice11, whitePrice12,
                whitePrice2, whitePrice21,
                whitePrice3, startTime, publicTime, durationHours, purchaseSymbol, isMerge);

            // Approve
            _nftContract.SetAccount(WhiteListAddress2);
            var approve =
                _nftContract.Approve(_nftMarketContract.ContractAddress, purchaseSymbol, tokenId, 10000);
            approve.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            var allowance = _nftContract.GetAllowance(purchaseSymbol, tokenId, WhiteListAddress2,
                _nftMarketContract.ContractAddress);
            Logger.Info($"allowance is {allowance}");

            var balance = _nftContract.GetBalance(WhiteListAddress2, purchaseSymbol, tokenId);
            Logger.Info($"balance is {balance}");
            //
            _nftMarketContract.SetAccount(WhiteListAddress2);
            var FixedPrice = _nftMarketContract.MakeOffer(
                symbol,
                tokenId,
                InitAccount,
                1,
                new Price
                {
                    Symbol = purchaseSymbol,
                    Amount = whitePrice1
                },
                expireTime
            );
            FixedPrice.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);
        }


        [TestMethod]
        //ListWithEnglistAuction
        public void MakeOfferListWithEnglishStartTimeTest()
        {
            var tokenId = 1;
            var totalSupply = 10000;
            var mintAmount = 1;
            var startingPrice = 12_00000000;
            var earnestMoney = 10_00000000;
            var buyAmount = 1;
            var expireTime = DateTime.UtcNow.AddHours(1).ToTimestamp();
            var startTime = DateTime.UtcNow.AddDays(1).ToTimestamp();
            var durationHours = 1;
            var symbol = CreateAndMintUnReuse(totalSupply, mintAmount, tokenId);
            Logger.Info($"startTime is {startTime.Seconds}");
            var purchaseSymbol = "USDT";
            var isMerge = true;

            //ListWithEnglistAuction
            ListWithEnglistAuction(symbol, tokenId, startingPrice, purchaseSymbol,
                startTime, durationHours, earnestMoney);
            // Approve
            _nftContract.SetAccount(OtherAccount);
            var approve = _tokenContract.ApproveToken(OtherAccount, _nftMarketContract.ContractAddress,
                10000000000_00000000, purchaseSymbol);
            approve.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            _nftContract.SetAccount(OtherAccount1);
            var approve1 = _tokenContract.ApproveToken(OtherAccount1, _nftMarketContract.ContractAddress,
                10000000000_00000000, purchaseSymbol);
            approve1.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            //startTime before buying
            _nftMarketContract.SetAccount(OtherAccount);
            var result = _nftMarketContract.MakeOffer(
                symbol,
                tokenId,
                InitAccount,
                buyAmount,
                new Price
                {
                    Symbol = purchaseSymbol,
                    Amount = startingPrice
                },
                expireTime
            );
            result.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);

            var offerList = _nftMarketContract.GetOfferList(symbol, tokenId, OtherAccount);
            Logger.Info($"offerList1 is {offerList}");
            offerList.Value.Count.ShouldBe(1);
            offerList.Value.First().From.ShouldBe(OtherAccount.ConvertAddress());
            offerList.Value.First().To.ShouldBe(InitAccount.ConvertAddress());
            offerList.Value.First().Price.Symbol.ShouldBe(purchaseSymbol);
            offerList.Value.First().Price.Amount.ShouldBe(startingPrice);
            var BidList = _nftMarketContract.GetBidList(symbol, tokenId);
            Logger.Info($"BidList1 is {BidList}");

            //startTime before buying
            _nftMarketContract.SetAccount(OtherAccount1);
            var result1 = _nftMarketContract.MakeOffer(
                symbol,
                tokenId,
                InitAccount,
                buyAmount,
                new Price
                {
                    Symbol = purchaseSymbol,
                    Amount = startingPrice - 1_00000000
                },
                expireTime
            );
            result1.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);

            var offerList1 = _nftMarketContract.GetOfferList(symbol, tokenId, OtherAccount1);
            Logger.Info($"offerList1 is {offerList1}");
            offerList1.Value.Count.ShouldBe(1);
            offerList1.Value.First().From.ShouldBe(OtherAccount1.ConvertAddress());
            offerList1.Value.First().To.ShouldBe(InitAccount.ConvertAddress());
            offerList1.Value.First().Price.Symbol.ShouldBe(purchaseSymbol);
            offerList1.Value.First().Price.Amount.ShouldBe(startingPrice - 1_00000000);
            var BidList1 = _nftMarketContract.GetBidList(symbol, tokenId);
            Logger.Info($"BidList1 is {BidList1}");
        }

        [TestMethod]
        //ListWithEnglistAuction
        public void MakeOfferListWithEnglishTest()
        {
            var tokenId = 1;
            var totalSupply = 10000;
            var mintAmount = 1;
            var startingPrice = 12_00000000;
            var earnestMoney = 10_00000000;
            var buyAmount = 1;
            var expireTime = DateTime.UtcNow.AddHours(1).ToTimestamp();
            var startTime = DateTime.UtcNow.ToTimestamp();
            var durationHours = 1;
            var symbol = CreateAndMintUnReuse(totalSupply, mintAmount, tokenId);
            Logger.Info($"startTime is {startTime.Seconds}");
            var purchaseSymbol = "USDT";
            var isMerge = true;

            //ListWithEnglistAuction
            ListWithEnglistAuction(symbol, tokenId, startingPrice, purchaseSymbol,
                startTime, durationHours, earnestMoney);
            // Approve
            _nftContract.SetAccount(OtherAccount);
            var approve = _tokenContract.ApproveToken(OtherAccount, _nftMarketContract.ContractAddress,
                10000000000_00000000, purchaseSymbol);
            approve.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            _nftContract.SetAccount(OtherAccount1);
            var approve1 = _tokenContract.ApproveToken(OtherAccount1, _nftMarketContract.ContractAddress,
                10000000000_00000000, purchaseSymbol);
            approve1.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            _nftContract.SetAccount(OtherAccount2);
            var approve2 = _tokenContract.ApproveToken(OtherAccount2, _nftMarketContract.ContractAddress,
                10000000000_00000000, purchaseSymbol);
            approve2.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            _nftContract.SetAccount(OtherAccount3);
            var approve3 = _tokenContract.ApproveToken(OtherAccount3, _nftMarketContract.ContractAddress,
                10000000000_00000000, purchaseSymbol);
            approve3.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            _nftContract.SetAccount(WhiteListAddress1);
            var approve4 = _tokenContract.ApproveToken(WhiteListAddress1, _nftMarketContract.ContractAddress,
                10000000000_00000000, purchaseSymbol);
            approve4.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            //16. startingPrice  >  enterAmount,first user first purchase
            _nftMarketContract.SetAccount(WhiteListAddress1);
            var result = _nftMarketContract.MakeOffer(
                symbol,
                tokenId,
                InitAccount,
                buyAmount,
                new Price
                {
                    Symbol = purchaseSymbol,
                    Amount = startingPrice - 8_00000000
                },
                expireTime
            );
            result.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);

            var offerList = _nftMarketContract.GetOfferList(symbol, tokenId, WhiteListAddress1);
            offerList.Value.Count.ShouldBe(1);
            offerList.Value.First().From.ShouldBe(WhiteListAddress1.ConvertAddress());
            offerList.Value.First().To.ShouldBe(InitAccount.ConvertAddress());
            offerList.Value.First().Price.Symbol.ShouldBe(purchaseSymbol);
            offerList.Value.First().Price.Amount.ShouldBe(startingPrice - 8_00000000);
            offerList.Value[0].Quantity.ShouldBe(1);
            offerList.Value[0].ExpireTime.ShouldBe(expireTime);


            //15. startingPrice  < =   Enter amount ,first user first purchase
            var BidList = _nftMarketContract.GetBidList(symbol, tokenId);
            BidList.ShouldBe(new BidList());
            offerList = _nftMarketContract.GetOfferList(symbol, tokenId, OtherAccount);
            offerList.ShouldBe(new OfferList());
            var balanceStart = _tokenContract.GetUserBalance(OtherAccount, purchaseSymbol);
            Logger.Info($"balanceStart is {balanceStart}");

            _nftMarketContract.SetAccount(OtherAccount);
            var result1 = _nftMarketContract.MakeOffer(
                symbol,
                tokenId,
                InitAccount,
                buyAmount,
                new Price
                {
                    Symbol = purchaseSymbol,
                    Amount = startingPrice
                },
                expireTime
            );
            result1.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);
            BidList = _nftMarketContract.GetBidList(symbol, tokenId);
            BidList.Value.Count.ShouldBe(1);
            BidList.Value.First().From.ShouldBe(OtherAccount.ConvertAddress());
            BidList.Value.First().To.ShouldBe(InitAccount.ConvertAddress());
            BidList.Value.First().Price.Symbol.ShouldBe(purchaseSymbol);
            BidList.Value.First().Price.Amount.ShouldBe(startingPrice);
            offerList = _nftMarketContract.GetOfferList(symbol, tokenId, OtherAccount);
            offerList.Value.Count.ShouldBe(0);
            var balanceFinish = _tokenContract.GetUserBalance(OtherAccount, purchaseSymbol);
            Logger.Info($"balanceFinish is {balanceFinish}");
            balanceFinish.ShouldBe(balanceStart - earnestMoney);
            // Check event
            var logs = result1.Logs.First(l => l.Name.Equals("BidPlaced")).NonIndexed;
            var byteString = ByteString.FromBase64(logs);
            var bidPlacedLogs = BidPlaced.Parser.ParseFrom(byteString);
            bidPlacedLogs.Symbol.ShouldBe(symbol);
            bidPlacedLogs.TokenId.ShouldBe(tokenId);
            bidPlacedLogs.Price.Symbol.ShouldBe(purchaseSymbol);
            bidPlacedLogs.Price.Amount.ShouldBe(startingPrice);
            bidPlacedLogs.OfferFrom.ShouldBe(OtherAccount.ConvertAddress());
            bidPlacedLogs.OfferTo.ShouldBe(InitAccount.ConvertAddress());

            //17. bid  <  Enter amount ,Other user first purchase
            _nftContract.SetAccount(OtherAccount1);

            _nftMarketContract.SetAccount(OtherAccount1);
            var result2 = _nftMarketContract.MakeOffer(
                symbol,
                tokenId,
                InitAccount,
                buyAmount,
                new Price
                {
                    Symbol = purchaseSymbol,
                    Amount = 15_00000000
                },
                expireTime
            );
            result2.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);
            BidList = _nftMarketContract.GetBidList(symbol, tokenId);
            BidList.Value.Count.ShouldBe(2);
            BidList.Value[1].From.ShouldBe(OtherAccount1.ConvertAddress());
            BidList.Value[1].To.ShouldBe(InitAccount.ConvertAddress());
            BidList.Value[1].Price.Symbol.ShouldBe(purchaseSymbol);
            BidList.Value[1].Price.Amount.ShouldBe(15_00000000);

            offerList = _nftMarketContract.GetOfferList(symbol, tokenId, OtherAccount1);
            offerList.Value.Count.ShouldBe(0);

            //18.  Enter amount<=bid   ,Other user first purchase
            _nftMarketContract.SetAccount(OtherAccount2);
            var result3 = _nftMarketContract.MakeOffer(
                symbol,
                tokenId,
                InitAccount,
                buyAmount,
                new Price
                {
                    Symbol = purchaseSymbol,
                    Amount = 14_00000000
                },
                expireTime
            );
            result3.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);
            BidList = _nftMarketContract.GetBidList(symbol, tokenId);
            BidList.Value.Count.ShouldBe(2);

            offerList = _nftMarketContract.GetOfferList(symbol, tokenId, OtherAccount2);
            offerList.Value.Count.ShouldBe(1);
            offerList.Value.First().From.ShouldBe(OtherAccount2.ConvertAddress());
            offerList.Value.First().To.ShouldBe(InitAccount.ConvertAddress());
            offerList.Value.First().Price.Symbol.ShouldBe(purchaseSymbol);
            offerList.Value.First().Price.Amount.ShouldBe(14_00000000);
            offerList.Value.First().Quantity.ShouldBe(buyAmount);
            offerList.Value.First().ExpireTime.ShouldBe(expireTime);

            //19.  Enter amount<  startingPrice ,Other user first purchase
            _nftMarketContract.SetAccount(OtherAccount3);
            var result4 = _nftMarketContract.MakeOffer(
                symbol,
                tokenId,
                InitAccount,
                buyAmount,
                new Price
                {
                    Symbol = purchaseSymbol,
                    Amount = 11_00000000
                },
                expireTime
            );
            result4.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);
            BidList = _nftMarketContract.GetBidList(symbol, tokenId);
            BidList.Value.Count.ShouldBe(2);

            offerList = _nftMarketContract.GetOfferList(symbol, tokenId, OtherAccount3);
            offerList.Value.Count.ShouldBe(1);
            offerList.Value.First().From.ShouldBe(OtherAccount3.ConvertAddress());
            offerList.Value.First().To.ShouldBe(InitAccount.ConvertAddress());
            offerList.Value.First().Price.Symbol.ShouldBe(purchaseSymbol);
            offerList.Value.First().Price.Amount.ShouldBe(11_00000000);
            offerList.Value.First().Quantity.ShouldBe(1);
            offerList.Value.First().ExpireTime.ShouldBe(expireTime);

            //20.User enters bid for the second time
            var balanceStart3 = _tokenContract.GetUserBalance(OtherAccount, purchaseSymbol);
            Logger.Info($"balance is {balanceStart3}");

            _nftMarketContract.SetAccount(OtherAccount);
            var result5 = _nftMarketContract.MakeOffer(
                symbol,
                tokenId,
                InitAccount,
                buyAmount,
                new Price
                {
                    Symbol = purchaseSymbol,
                    Amount = 32_00000000
                },
                expireTime
            );
            result5.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);
            var balanceFinish3 = _tokenContract.GetUserBalance(OtherAccount, purchaseSymbol);
            Logger.Info($"balance is {balanceFinish3}");
            balanceFinish3.ShouldBe(balanceStart3);

            BidList = _nftMarketContract.GetBidList(symbol, tokenId);
            BidList.Value.Count.ShouldBe(2);
            BidList.Value[0].From.ShouldBe(OtherAccount.ConvertAddress());
            BidList.Value[0].To.ShouldBe(InitAccount.ConvertAddress());
            BidList.Value[0].Price.Symbol.ShouldBe(purchaseSymbol);
            BidList.Value[0].Price.Amount.ShouldBe(32_00000000);
        }


        [TestMethod]
        //ListWithEnglistAuction
        public void MakeOfferListWithEnglishTest1()
        {
            var tokenId = 1;
            var totalSupply = 10000;
            var mintAmount = 1;
            var startingPrice = 12_00000000;
            var earnestMoney = 10_00000000;
            var buyAmount = 1;
            var expireTime = DateTime.UtcNow.AddHours(1).ToTimestamp();
            var startTime = DateTime.UtcNow.ToTimestamp();
            var durationHours = 1;
            var symbol = CreateAndMintUnReuse(totalSupply, mintAmount, tokenId);
            Logger.Info($"startTime is {startTime.Seconds}");
            var purchaseSymbol = "USDT";
            var isMerge = true;

            ListWithEnglistAuction(symbol, tokenId, startingPrice, purchaseSymbol,
                startTime, durationHours, earnestMoney);

            // Approve
            _nftContract.SetAccount(OtherAccount);
            var approve = _tokenContract.ApproveToken(OtherAccount, _nftMarketContract.ContractAddress,
                10000000000_00000000, purchaseSymbol);
            approve.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            _nftContract.SetAccount(OtherAccount1);
            var approve1 = _tokenContract.ApproveToken(OtherAccount1, _nftMarketContract.ContractAddress,
                10000000000_00000000, purchaseSymbol);
            approve1.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            //20.1.The user's first purchase is greater than bid, the second purchase is less than bid, and the third purchase is greater than bid
            var balance = _tokenContract.GetUserBalance(OtherAccount1, purchaseSymbol);
            Logger.Info($"balance is {balance}");
            _nftMarketContract.SetAccount(OtherAccount1);
            var result = _nftMarketContract.MakeOffer(
                symbol,
                tokenId,
                InitAccount,
                buyAmount,
                new Price
                {
                    Symbol = purchaseSymbol,
                    Amount = startingPrice
                },
                expireTime
            );
            result.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);
            var BidList = _nftMarketContract.GetBidList(symbol, tokenId);
            BidList.Value.Count.ShouldBe(1);
            BidList.Value[0].From.ShouldBe(OtherAccount1.ConvertAddress());
            BidList.Value[0].To.ShouldBe(InitAccount.ConvertAddress());
            BidList.Value[0].Price.Symbol.ShouldBe(purchaseSymbol);
            BidList.Value[0].Price.Amount.ShouldBe(startingPrice);
            var balance1 = _tokenContract.GetUserBalance(OtherAccount1, purchaseSymbol);
            Logger.Info($"balance1 is {balance1}");
            balance1.ShouldBe(balance - earnestMoney);

            var result1 = _nftMarketContract.MakeOffer(
                symbol,
                tokenId,
                InitAccount,
                buyAmount,
                new Price
                {
                    Symbol = purchaseSymbol,
                    Amount = startingPrice - 5_00000000
                },
                expireTime
            );
            result1.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);
            var offerList = _nftMarketContract.GetOfferList(symbol, tokenId, OtherAccount1);
            offerList.Value.Count.ShouldBe(1);
            offerList.Value[0].From.ShouldBe(OtherAccount1.ConvertAddress());
            offerList.Value[0].To.ShouldBe(InitAccount.ConvertAddress());
            offerList.Value[0].Price.Symbol.ShouldBe(purchaseSymbol);
            offerList.Value[0].Price.Amount.ShouldBe(startingPrice - 5_00000000);
            offerList.Value[0].Quantity.ShouldBe(1);
            offerList.Value[0].ExpireTime.ShouldBe(expireTime);
            var balance2 = _tokenContract.GetUserBalance(OtherAccount1, purchaseSymbol);
            Logger.Info($"balance2 is {balance2}");
            balance2.ShouldBe(balance1);

            var result2 = _nftMarketContract.MakeOffer(
                symbol,
                tokenId,
                InitAccount,
                buyAmount,
                new Price
                {
                    Symbol = purchaseSymbol,
                    Amount = startingPrice + 5_00000000
                },
                expireTime
            );
            result2.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);
            var balance3 = _tokenContract.GetUserBalance(OtherAccount1, purchaseSymbol);
            Logger.Info($"balance3 is {balance3}");
            balance3.ShouldBe(balance1);

            offerList = _nftMarketContract.GetOfferList(symbol, tokenId, OtherAccount1);
            offerList.Value.Count.ShouldBe(1);
            offerList.Value[0].From.ShouldBe(OtherAccount1.ConvertAddress());
            offerList.Value[0].To.ShouldBe(InitAccount.ConvertAddress());
            offerList.Value[0].Price.Symbol.ShouldBe(purchaseSymbol);
            offerList.Value[0].Price.Amount.ShouldBe(startingPrice - 5_00000000);
            offerList.Value[0].Quantity.ShouldBe(1);
            offerList.Value[0].ExpireTime.ShouldBe(expireTime);

            BidList = _nftMarketContract.GetBidList(symbol, tokenId);
            BidList.Value.Count.ShouldBe(1);
            BidList.Value[0].From.ShouldBe(OtherAccount1.ConvertAddress());
            BidList.Value[0].To.ShouldBe(InitAccount.ConvertAddress());
            BidList.Value[0].Price.Symbol.ShouldBe(purchaseSymbol);
            BidList.Value[0].Price.Amount.ShouldBe(startingPrice + 5_00000000);


            //21.1.offerto address tobuy
            // Approve
            _nftContract.SetAccount(InitAccount);
            var approve3 = _tokenContract.ApproveToken(InitAccount, _nftMarketContract.ContractAddress,
                10000000000_00000000, purchaseSymbol);
            approve3.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            _nftMarketContract.SetAccount(InitAccount);
            var result3 = _nftMarketContract.MakeOffer(
                symbol,
                tokenId,
                InitAccount,
                buyAmount,
                new Price
                {
                    Symbol = purchaseSymbol,
                    Amount = startingPrice
                },
                expireTime
            );
            result3.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            result3.Error.ShouldContain("Origin owner cannot be sender himself.");
        }


        [TestMethod]
        //ListWithEnglistAuction
        public void MakeOfferListWithEnglishTest2()
        {
            var tokenId = 1;
            var totalSupply = 10000;
            var mintAmount = 1;
            var buyAmount = 1;
            var expireTime = DateTime.UtcNow.AddHours(1).ToTimestamp();
            var startTime = DateTime.UtcNow.ToTimestamp();
            //var symbol = CreateAndMintUnReuse(totalSupply, mintAmount, tokenId);
            var symbol = "CO575585112";
            Logger.Info($"startTime is {startTime.Seconds}");
            var purchaseSymbol = "USDT";
            var isMerge = true;

            /*
             ListWithEnglistAuction(symbol, tokenId, startingPrice, purchaseSymbol,
                startTime, durationHours, earnestMoney);
                */
            //symbol:CO575585112
            //NFT:2JFJPnDRggKwMnuzijA5AJJin2AyeXDZBx5wU122Kf88KfdnRM
            //NFTMarket：FptR35pMNQmmSP3hRSFASG2qaHmLGjYDp4UpPu3Cvf8dNXwxo

            // Approve
            _nftContract.SetAccount(OtherAccount);
            var approve = _tokenContract.ApproveToken(OtherAccount, _nftMarketContract.ContractAddress,
                10000000000_00000000, purchaseSymbol);
            approve.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            //21.DurationHours <  UtcNow  
            _nftMarketContract.SetAccount(OtherAccount);
            var result = _nftMarketContract.MakeOffer(
                symbol,
                tokenId,
                InitAccount,
                buyAmount,
                new Price
                {
                    Symbol = purchaseSymbol,
                    Amount = 50_00000000
                },
                expireTime
            );
            result.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);
            var offerList = _nftMarketContract.GetBidList(symbol, tokenId);
            offerList.Value.Count.ShouldBe(1);
            offerList.Value[0].From.ShouldBe(OtherAccount.ConvertAddress());
            offerList.Value[0].To.ShouldBe(InitAccount.ConvertAddress());
            offerList.Value[0].Price.Symbol.ShouldBe(purchaseSymbol);
            offerList.Value[0].Price.Amount.ShouldBe(50_00000000);
            offerList.Value.First().ExpireTime.ShouldBe(expireTime);
        }


        [TestMethod]
        //ListWithDutchAuction
        public void MakeOfferListWithDutchAuctionStartTimeTest()
        {
            var tokenId = 1;
            var totalSupply = 10000;
            var mintAmount = 1;
            var startingPrice = 12_00000000;
            var endingPrice = 5_00000000;
            var buyAmount = 1;
            var expireTime = DateTime.UtcNow.AddHours(1).ToTimestamp();
            var startTime = DateTime.UtcNow.AddHours(1).ToTimestamp();
            var durationHours = 100;
            var symbol = CreateAndMintUnReuse(totalSupply, mintAmount, tokenId);
            Logger.Info($"startTime is {startTime.Seconds}");
            var purchaseSymbol = "USDT";
            var isMerge = true;

            ListWithDutchAuction(symbol, tokenId, startingPrice, endingPrice, purchaseSymbol,
                startTime, durationHours);

            _nftContract.SetAccount(OtherAccount);
            var approve1 = _tokenContract.ApproveToken(OtherAccount, _nftMarketContract.ContractAddress,
                10000000000_00000000, purchaseSymbol);
            approve1.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            //startTime before buying
            var balanceStart = _tokenContract.GetUserBalance(OtherAccount, purchaseSymbol);
            Logger.Info($"balanceStart is {balanceStart}");
            _nftMarketContract.SetAccount(OtherAccount);
            var result = _nftMarketContract.MakeOffer(
                symbol,
                tokenId,
                InitAccount,
                buyAmount,
                new Price
                {
                    Symbol = purchaseSymbol,
                    Amount = startingPrice
                },
                expireTime
            );
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            var balanceFinish = _tokenContract.GetUserBalance(OtherAccount, purchaseSymbol);
            Logger.Info($"balanceFinish is {balanceFinish}");
            var offerList = _nftMarketContract.GetOfferList(symbol, tokenId, OtherAccount);
            Logger.Info($"offerList is {offerList}");
            offerList.Value.Count.ShouldBe(1);
            offerList.Value.First().From.ShouldBe(OtherAccount.ConvertAddress());
            offerList.Value.First().To.ShouldBe(InitAccount.ConvertAddress());
            offerList.Value.First().Price.Symbol.ShouldBe(purchaseSymbol);
            offerList.Value.First().Price.Amount.ShouldBe(startingPrice);
            var BidList = _nftMarketContract.GetBidList(symbol, tokenId);
            Logger.Info($"BidList is {BidList}");
        }

        [TestMethod]
        //ListWithDutchAuction
        public void MakeOfferListWithDutchAuctionTest()
        {
            var tokenId = 1;
            var totalSupply = 10000;
            var mintAmount = 1;
            var startingPrice = 12_00000000;
            var endingPrice = 5_00000000;
            var buyAmount = 1;
            var expireTime = DateTime.UtcNow.AddHours(1).ToTimestamp();
            var startTime = DateTime.UtcNow.ToTimestamp();
            var durationHours = 100;
            var symbol = CreateAndMintUnReuse(totalSupply, mintAmount, tokenId);
            Logger.Info($"startTime is {startTime.Seconds}");
            var purchaseSymbol = "USDT";
            var isMerge = true;

            ListWithDutchAuction(symbol, tokenId, startingPrice, endingPrice, purchaseSymbol,
                startTime, durationHours);

            var GetListedNFTInfoList = _nftMarketContract.GetListedNFTInfoList(symbol, tokenId, InitAccount);
            Logger.Info($"GetListedNFTInfoList is {GetListedNFTInfoList}");

            // Approve
            _nftContract.SetAccount(InitAccount);
            var approve = _tokenContract.ApproveToken(InitAccount, _nftMarketContract.ContractAddress,
                10000000000_00000000, purchaseSymbol);
            approve.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            _nftContract.SetAccount(OtherAccount);
            var approve1 = _tokenContract.ApproveToken(OtherAccount, _nftMarketContract.ContractAddress,
                10000000000_00000000, purchaseSymbol);
            approve1.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            _nftContract.SetAccount(OtherAccount1);
            var approve2 = _tokenContract.ApproveToken(OtherAccount1, _nftMarketContract.ContractAddress,
                10000000000_00000000, purchaseSymbol);
            approve2.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);


            //22.1 . offerto address tobuy
            _nftMarketContract.SetAccount(InitAccount);
            var result = _nftMarketContract.MakeOffer(
                symbol,
                tokenId,
                InitAccount,
                buyAmount,
                new Price
                {
                    Symbol = purchaseSymbol,
                    Amount = startingPrice
                },
                expireTime
            );
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
            result.Error.ShouldContain("Origin owner cannot be sender himself.");


            //22.2 . endingPrice <=  startingPrice < = Enter amount
            var balanceStart1 = _tokenContract.GetUserBalance(OtherAccount, purchaseSymbol);
            Logger.Info($"balanceOtherAccountStart is {balanceStart1}");
            var balanceStart = _tokenContract.GetUserBalance(serviceAddress, purchaseSymbol);
            Logger.Info($"balanceserviceAddressStart is {balanceStart}");

            var getBalanceBuyerStart = _nftContract.GetBalance(OtherAccount, symbol, tokenId);
            Logger.Info($"OtherAccountBalance is {getBalanceBuyerStart.Balance}");
            var getBalancesellerStart = _nftContract.GetBalance(InitAccount, symbol, tokenId);
            Logger.Info($"InitAccountBalance is {getBalancesellerStart.Balance}");

            _nftMarketContract.SetAccount(OtherAccount);
            var result1 = _nftMarketContract.MakeOffer(
                symbol,
                tokenId,
                InitAccount,
                buyAmount,
                new Price
                {
                    Symbol = purchaseSymbol,
                    Amount = 13_00000000
                },
                expireTime.AddDays(1)
            );
            result1.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var GetOfferList = _nftMarketContract.GetOfferList(symbol, tokenId, OtherAccount);
            Logger.Info($"GetOfferList22.2 is {GetOfferList}");

            var balanceFinish1 = _tokenContract.GetUserBalance(OtherAccount, purchaseSymbol);
            Logger.Info($"balanceOtherAccountFinish is {balanceFinish1}");
            var balanceFinish = _tokenContract.GetUserBalance(serviceAddress, purchaseSymbol);
            Logger.Info($"balanceserviceAddressFinish is {balanceFinish}");
            var getBalanceBuyerFinish = _nftContract.GetBalance(OtherAccount, symbol, tokenId);
            Logger.Info($"getBalanceBuyerFinish is {getBalanceBuyerFinish.Balance}");
            getBalanceBuyerFinish.Balance.ShouldBe(getBalanceBuyerStart.Balance + 1);

            var getBalancesellerFinish = _nftContract.GetBalance(InitAccount, symbol, tokenId);
            Logger.Info($"getBalancesellerFinish is {getBalancesellerFinish.Balance}");
            getBalancesellerFinish.Balance.ShouldBe(getBalancesellerStart.Balance - 1);


            //22.3. nft has been purchased, another user purchased again

            _nftMarketContract.SetAccount(OtherAccount1);
            var result2 = _nftMarketContract.MakeOffer(
                symbol,
                tokenId,
                InitAccount,
                buyAmount,
                new Price
                {
                    Symbol = purchaseSymbol,
                    Amount = startingPrice + 2_00000000
                },
                expireTime
            );
            result2.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            GetOfferList = _nftMarketContract.GetOfferList(symbol, tokenId, OtherAccount1);
            Logger.Info($"GetOfferList22.3 is {GetOfferList}");
            var GetBidList = _nftMarketContract.GetBidList(symbol, tokenId);
            Logger.Info($"GetBidList22.3 is {GetBidList}");
        }

        [TestMethod]
        //ListWithDutchAuction-offer
        public void MakeOfferListWithDutchAuctionTest1()
        {
            var tokenId = 1;
            var totalSupply = 10000;
            var mintAmount = 1;
            var startingPrice = 12_00000000;
            var endingPrice = 5_00000000;
            var buyAmount = 1;
            var expireTime = DateTime.UtcNow.AddHours(1).ToTimestamp();

            var startTime = DateTime.UtcNow.ToTimestamp();
            var durationHours = 48;
            var symbol = CreateAndMintUnReuse(totalSupply, mintAmount, tokenId);
            Logger.Info($"startTime is {startTime.Seconds}");
            var purchaseSymbol = "USDT";

            ListWithDutchAuction(symbol, tokenId, startingPrice, endingPrice, purchaseSymbol,
                startTime, durationHours);


            // Approve
            _nftContract.SetAccount(OtherAccount);
            var approve = _tokenContract.ApproveToken(OtherAccount, _nftMarketContract.ContractAddress,
                10000000000_00000000, purchaseSymbol);
            approve.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            _nftContract.SetAccount(OtherAccount1);
            var approve1 = _tokenContract.ApproveToken(OtherAccount1, _nftMarketContract.ContractAddress,
                10000000000_00000000, purchaseSymbol);
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
                    Symbol = purchaseSymbol,
                    Amount = startingPrice - 1_00000000
                },
                expireTime
            );

            offerList = _nftMarketContract.GetOfferList(symbol, tokenId, OtherAccount);
            offerList.Value.Count.ShouldBe(1);
            offerList.Value[0].From.ShouldBe(OtherAccount.ConvertAddress());
            offerList.Value[0].To.ShouldBe(InitAccount.ConvertAddress());
            offerList.Value[0].Price.Symbol.ShouldBe(purchaseSymbol);
            offerList.Value[0].Price.Amount.ShouldBe(startingPrice - 1_00000000);
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
                    Symbol = purchaseSymbol,
                    Amount = endingPrice - 1_00000000
                },
                expireTime
            );
            var offerList1 = _nftMarketContract.GetOfferList(symbol, tokenId, OtherAccount1);
            offerList1.Value.Count.ShouldBe(1);
            offerList1.Value[0].From.ShouldBe(OtherAccount1.ConvertAddress());
            offerList1.Value[0].To.ShouldBe(InitAccount.ConvertAddress());
            offerList1.Value[0].Price.Symbol.ShouldBe(purchaseSymbol);
            offerList1.Value[0].Price.Amount.ShouldBe(endingPrice - 1_00000000);
            offerList1.Value.First().Quantity.ShouldBe(1);
            offerList.Value.First().ExpireTime.ShouldBe(expireTime);
        }


        [TestMethod]
        //ListWithDutchAuction-Timeout
        public void MakeOfferListWithDutchAuctionTest2()
        {
            var tokenId = 1;
            var totalSupply = 10000;
            var mintAmount = 1;
            var startingPrice = 12_00000000;
            var buyAmount = 1;
            var expireTime = DateTime.UtcNow.AddHours(1).ToTimestamp();
            var startTime = DateTime.UtcNow.ToTimestamp();
            //var symbol = CreateAndMintUnReuse(totalSupply, mintAmount, tokenId);
            var symbol = "CO501253588";
            Logger.Info($"startTime is {startTime.Seconds}");
            var purchaseSymbol = "USDT";

            /*
            ListWithDutchAuction(symbol, tokenId, startingPrice, endingPrice, purchaseSymbol,
                startTime,  durationHours);
            */
            //NFT:DiFQRf3q8ogxKe2js6gFzyUsASs8KiMPNaUp2LtvRLYZbGxiv
            //NFTMarket:2MoHEtvng4vdrN8iJwcj6fsrf2icA5toqned4mKuLgsUE1zYHm
            //CO501253588

            //approve
            _nftContract.SetAccount(OtherAccount);
            var approve1 = _tokenContract.ApproveToken(OtherAccount, _nftMarketContract.ContractAddress,
                10000000000_00000000, purchaseSymbol);
            approve1.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            //26.DurationHours <  UtcNow
            _nftMarketContract.SetAccount(OtherAccount);
            _nftMarketContract.MakeOffer(
                symbol,
                tokenId,
                InitAccount,
                buyAmount,
                new Price
                {
                    Symbol = purchaseSymbol,
                    Amount = startingPrice + 1_00000000
                },
                expireTime
            );

            var offerList = _nftMarketContract.GetOfferList(symbol, tokenId, OtherAccount);
            offerList.Value.Count.ShouldBe(1);
            offerList.Value[0].From.ShouldBe(OtherAccount.ConvertAddress());
            offerList.Value[0].To.ShouldBe(InitAccount.ConvertAddress());
            offerList.Value[0].Price.Symbol.ShouldBe(purchaseSymbol);
            offerList.Value[0].Price.Amount.ShouldBe(startingPrice + 1_00000000);
            offerList.Value.First().Quantity.ShouldBe(0);
            offerList.Value.First().ExpireTime.ShouldBe(expireTime);
        }


        [TestMethod]
        public void CustomMadeTest()
        {
            var tokenId = 1;
            var tokenId1 = 2;
            var totalSupply = 10000;
            var mintAmount = 1000;
            var buyAmount = 1;

            var expireTime = DateTime.UtcNow.AddHours(1).ToTimestamp();

            var startTime = DateTime.UtcNow.ToTimestamp();
            var publicTime = DateTime.UtcNow.AddHours(12).ToTimestamp();
            var durationHours = 48;
            var symbol = CreateAndMintUnReuse(totalSupply, mintAmount, tokenId);
            Logger.Info($"startTime is {startTime.Seconds}");
            Logger.Info($"publicTime is {publicTime.Seconds}");
            var purchaseSymbol = "USDT";
            var isConfirm = true;
            var isConfirm1 = false;


            var depositRate = 1000;
            var workHours = 1;
            var whiteListHours = 1;
            var purchaseAmount = 20_00000000;
            var stakingAmount = 10_00000000;

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
            var balanceOtherAccountStart = _tokenContract.GetUserBalance(OtherAccount, "USDT");
            Logger.Info($"balanceOtherStart is {balanceOtherAccountStart}");

            var balanceInitAccountStart = _tokenContract.GetUserBalance(InitAccount, "USDT");
            Logger.Info($"balanceInitStart is {balanceInitAccountStart}");

            _nftMarketContract.SetAccount(OtherAccount);
            var CustomMade = _nftMarketContract.MakeOffer(
                symbol,
                tokenId1,
                InitAccount,
                buyAmount,
                new Price
                {
                    Symbol = "USDT",
                    Amount = purchaseAmount,
                },
                expireTime.AddSeconds(1)
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
            // Check event
            var logs = CustomMade.Logs.First(l => l.Name.Equals("NewNFTRequested")).NonIndexed;
            var byteString = ByteString.FromBase64(logs);
            var newNFTRequestedLogs = NewNFTRequested.Parser.ParseFrom(byteString);
            newNFTRequestedLogs.Symbol.ShouldBe(symbol);
            newNFTRequestedLogs.Requester.ShouldBe(OtherAccount.ConvertAddress());
            newNFTRequestedLogs.TokenId.ShouldBe(tokenId1);
            

            //29.repeatedly buy
            _nftMarketContract.SetAccount(OtherAccount);
            var CustomMade1 = _nftMarketContract.MakeOffer(
                symbol,
                tokenId1,
                InitAccount,
                buyAmount,
                new Price
                {
                    Symbol = "USDT",
                    Amount = purchaseAmount,
                },
                expireTime.AddSeconds(2)
            );
            CustomMade1.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            CustomMade1.Error.ShouldContain("Request already existed.");


            //29.1. endingPrice <=   Enter amount ,Seller confirm
            var balanceOtherAccount1Start1 = _tokenContract.GetUserBalance(OtherAccount1, "USDT");
            Logger.Info($"balanceOtherStart1 is {balanceOtherAccount1Start1}");

            var balanceInitAccountStart1 = _tokenContract.GetUserBalance(InitAccount, "USDT");
            Logger.Info($"balanceInitStart1 is {balanceInitAccountStart1}");


            _nftMarketContract.SetAccount(OtherAccount1);
            var CustomMade2 = _nftMarketContract.MakeOffer(
                symbol,
                3,
                InitAccount,
                buyAmount,
                new Price
                {
                    Symbol = "USDT",
                    Amount = purchaseAmount,
                },
                expireTime.AddSeconds(3)
            );
            CustomMade2.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);
            var RequestInfo1 = _nftMarketContract.GetRequestInfo(symbol, 3);
            Logger.Info($"RequestInfo1 is {RequestInfo1}");

            //HandleRequest
            var balanceOtherAccount1Handle = _tokenContract.GetUserBalance(OtherAccount1, "USDT");
            Logger.Info($"balanceOtherAccount1Handle is {balanceOtherAccount1Handle}");
            var balanceInitAccountHandle = _tokenContract.GetUserBalance(InitAccount, "USDT");
            Logger.Info($"balanceInitAccountHandle is {balanceInitAccountHandle}");
            var balanceserviceAddressHandle = _tokenContract.GetUserBalance(serviceAddress, "USDT");
            Logger.Info($"balanceInitAccountHandle is {balanceserviceAddressHandle}");

            _nftMarketContract.SetAccount(InitAccount);
            var handleRequestResult = _nftMarketContract.HandleRequest(symbol, 3, OtherAccount1, isConfirm);
            handleRequestResult.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);

            var balanceOtherAccountFinish1 = _tokenContract.GetUserBalance(OtherAccount1, "USDT");
            Logger.Info($"balanceOtherFinish1 is {balanceOtherAccountFinish1}");
            balanceOtherAccountFinish1.ShouldBe(balanceOtherAccount1Handle);
            var balanceInitAccountFinish1 = _tokenContract.GetUserBalance(InitAccount, "USDT");
            Logger.Info($"balanceInitFinish1 is {balanceInitAccountFinish1}");
            balanceInitAccountFinish1.ShouldBe(balanceInitAccountHandle + 99900000);
            var balanceserviceAddressFinish1 = _tokenContract.GetUserBalance(serviceAddress, "USDT");
            Logger.Info($"balanceserviceAddressFinish1 is {balanceserviceAddressFinish1}");
            balanceserviceAddressFinish1.ShouldBe(balanceserviceAddressHandle + 100000);


            //29.2. endingPrice <=   Enter amount ,Seller confirm
            var balanceOtherAccount2Start2 = _tokenContract.GetUserBalance(OtherAccount2, "USDT");
            Logger.Info($"balanceOtherStart2 is {balanceOtherAccount2Start2}");
            var balanceInitAccountStart2 = _tokenContract.GetUserBalance(InitAccount, "USDT");
            Logger.Info($"balanceInitStart2 is {balanceInitAccountStart2}");
            var balanceserviceAddressStart2 = _tokenContract.GetUserBalance(serviceAddress, "USDT");
            Logger.Info($"balanceserviceAddressStart2 is {balanceserviceAddressStart2}");
            _nftMarketContract.SetAccount(OtherAccount2);
            var CustomMade3 = _nftMarketContract.MakeOffer(
                symbol,
                4,
                InitAccount,
                buyAmount,
                new Price
                {
                    Symbol = "USDT",
                    Amount = purchaseAmount,
                },
                expireTime.AddSeconds(10)
            );

            CustomMade3.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);
            var RequestInfo2 = _nftMarketContract.GetRequestInfo(symbol, 4);
            Logger.Info($"RequestInfo2 is {RequestInfo2}");

            //HandleRequest
            var balanceOtherAccount2Handle2 = _tokenContract.GetUserBalance(OtherAccount2, "USDT");
            Logger.Info($"balanceOtherAccount2Handle2 is {balanceOtherAccount2Handle2}");
            balanceOtherAccount2Handle2.ShouldBe(balanceOtherAccount2Start2 - 2_00000000);
            var balanceInitAccountHandle2 = _tokenContract.GetUserBalance(InitAccount, "USDT");
            Logger.Info($"balanceInitAccountHandle2 is {balanceInitAccountHandle2}");
            var balanceserviceAddressHandle2 = _tokenContract.GetUserBalance(serviceAddress, "USDT");
            Logger.Info($"balanceserviceAddressHandle2 is {balanceserviceAddressHandle2}");

            _nftMarketContract.SetAccount(InitAccount);
            var handleRequestResult1 = _nftMarketContract.HandleRequest(symbol, 4, OtherAccount2, isConfirm1);
            handleRequestResult1.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);

            var balanceOtherAccount2Finish2 = _tokenContract.GetUserBalance(OtherAccount2, "USDT");
            Logger.Info($"balanceOtherFinish2 is {balanceOtherAccount2Finish2}");
            balanceOtherAccount2Finish2.ShouldBe(balanceOtherAccount2Start2);
            var balanceInitAccountFinish2 = _tokenContract.GetUserBalance(InitAccount, "USDT");
            Logger.Info($"balanceInitFinish2 is {balanceInitAccountFinish2}");
            balanceInitAccountFinish2.ShouldBe(balanceInitAccountStart2);
            var balanceserviceAddressFinish2 = _tokenContract.GetUserBalance(serviceAddress, "USDT");
            Logger.Info($"balanceserviceAddressFinish2 is {balanceserviceAddressFinish2}");
            balanceserviceAddressFinish2.ShouldBe(balanceserviceAddressStart2);

            //29.3 Enter Amount< purchaseAmount* depositRate/10000
            _nftMarketContract.SetAccount(OtherAccount2);
            var CustomMade4 = _nftMarketContract.MakeOffer(
                symbol,
                5,
                InitAccount,
                buyAmount,
                new Price
                {
                    Symbol = "USDT",
                    Amount = 1,
                },
                expireTime.AddSeconds(20)
            );
            CustomMade4.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);

            var RequestInfo4 = _nftMarketContract.GetRequestInfo(symbol, 5);
            Logger.Info($"RequestInfo4 is {RequestInfo4}");
        }


        [TestMethod]
        public void MakeOffer(long priceAmount, string symbol, Timestamp expireTime)
        {
            var tokenId = 1;
            var buyAmount = 1;
            var CustomMade = _nftMarketContract.MakeOffer(
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
        public void CancelOfferTest1()
        {
            var tokenId = 1;
            var totalSupply = 10000;
            var mintAmount = 1000;
            var sellAmount = 100;
            var fixedPrice = 12_00000000;
            var whitePrice1 = 9_00000000;
            var whitePrice11 = 9_10000000;
            var whitePrice12 = 9_20000000;
            var whitePrice2 = 10_00000000;
            var whitePrice21 = 10_10000000;
            var whitePrice3 = 11_00000000;
            var buyAmount = 1;
            var expireTime = DateTime.UtcNow.AddHours(30).ToTimestamp();
            var expireTime1 = DateTime.UtcNow.AddSeconds(30).ToTimestamp();

            var startTime = DateTime.UtcNow.ToTimestamp();
            var publicTime = DateTime.UtcNow.AddSeconds(10).ToTimestamp();
            var durationHours = 1;
            var symbol = CreateAndMint(totalSupply, mintAmount, tokenId);
            Logger.Info($"startTime is {startTime.Seconds}");
            Logger.Info($"publicTime is {publicTime.Seconds}");
            var purchaseSymbol = "USDT";
            var isMerge = true;

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
            ListWithFixedPrice(symbol, tokenId, sellAmount, fixedPrice, whitePrice1, whitePrice11, whitePrice12,
                whitePrice2, whitePrice21,
                whitePrice3, startTime, publicTime, durationHours, purchaseSymbol, isMerge);


            //30. OtherAccount CamcelOffer , expireTime > UtcNow  
            _nftMarketContract.SetAccount(OtherAccount);
            MakeOffer(7_00000000, symbol, expireTime);

            var OfferList = _nftMarketContract.GetOfferList(symbol, tokenId, OtherAccount);
            OfferList.Value.Count.ShouldBe(1);

            _nftMarketContract.SetAccount(OtherAccount);
            var CustomMade1 = _nftMarketContract.CancelOffer(
                symbol,
                tokenId,
                new Int32List
                {
                    Value = { 0 }
                },
                OtherAccount,
                true
            );
            CustomMade1.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);
            OfferList = _nftMarketContract.GetOfferList(symbol, tokenId, OtherAccount);
            OfferList.Value.Count.ShouldBe(0);
            // Check event
            var logs = CustomMade1.Logs.First(l => l.Name.Equals("OfferCanceled")).NonIndexed;
            var byteString = ByteString.FromBase64(logs);
            var offerCanceledLogs = OfferCanceled.Parser.ParseFrom(byteString);
            offerCanceledLogs.Symbol.ShouldBe(symbol);
            offerCanceledLogs.TokenId.ShouldBe(tokenId);
            offerCanceledLogs.OfferFrom.ShouldBe(OtherAccount.ConvertAddress());
            offerCanceledLogs.IndexList.Value[0].ShouldBe(0);
            // Check event
            logs = CustomMade1.Logs.First(l => l.Name.Equals("OfferRemoved")).NonIndexed;
            byteString = ByteString.FromBase64(logs);
            var offerRemovedLogs = OfferRemoved.Parser.ParseFrom(byteString);
            offerRemovedLogs.Symbol.ShouldBe(symbol);
            offerRemovedLogs.TokenId.ShouldBe(tokenId);
            offerRemovedLogs.OfferFrom.ShouldBe(OtherAccount.ConvertAddress());
            offerRemovedLogs.OfferTo.ShouldBe(InitAccount.ConvertAddress());
            offerRemovedLogs.ExpireTime.ShouldBe(expireTime);

            //31. InitAccount CamcelOffer , expireTime > UtcNow  
            _nftMarketContract.SetAccount(OtherAccount1);
            MakeOffer(6_00000000, symbol, expireTime);
            var OfferList1 = _nftMarketContract.GetOfferList(symbol, tokenId, OtherAccount1);
            OfferList1.Value.Count.ShouldBe(1);

            _nftMarketContract.SetAccount(InitAccount);
            var CustomMade2 = _nftMarketContract.CancelOffer(
                symbol,
                tokenId,
                new Int32List
                {
                    Value = { 0 }
                },
                OtherAccount1,
                false
            );
            CustomMade2.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);
            OfferList1.Value.Count.ShouldBe(1);

            //32. OtherAccount2 repSetAccounteatedly CamcelOffer , expireTime > UtcNow  
            _nftMarketContract.SetAccount(OtherAccount2);
            MakeOffer(6_00000000, symbol, expireTime1);
            MakeOffer(7_00000000, symbol, expireTime1);
            MakeOffer(8_00000000, symbol, expireTime1);
            _nftMarketContract.SetAccount(OtherAccount3);
            MakeOffer(9_00000000, symbol, expireTime1);
            MakeOffer(10_00000000, symbol, expireTime);

            var OfferListOtherAccount2 = _nftMarketContract.GetOfferList(symbol, tokenId, OtherAccount2);
            OfferListOtherAccount2.Value.Count.ShouldBe(3);
            var OfferListOtherAccount3 = _nftMarketContract.GetOfferList(symbol, tokenId, OtherAccount3);
            OfferListOtherAccount3.Value.Count.ShouldBe(2);

            _nftMarketContract.SetAccount(OtherAccount2);
            Thread.Sleep(90 * 1000);
            var CustomMade3 = _nftMarketContract.CancelOffer(
                symbol,
                tokenId,
                new Int32List
                {
                    Value = { 0, 2 }
                },
                OtherAccount2,
                true
            );
            CustomMade3.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);

            OfferListOtherAccount2 = _nftMarketContract.GetOfferList(symbol, tokenId, OtherAccount2);
            OfferListOtherAccount2.Value.Count.ShouldBe(1);
            OfferListOtherAccount2.Value[0].From.ShouldBe(OtherAccount2.ConvertAddress());
            OfferListOtherAccount2.Value[0].To.ShouldBe(InitAccount.ConvertAddress());
            OfferListOtherAccount2.Value[0].Price.Symbol.ShouldBe(purchaseSymbol);
            OfferListOtherAccount2.Value[0].Price.TokenId.ShouldBe(0);
            OfferListOtherAccount2.Value[0].Price.Amount.ShouldBe(7_00000000);
            OfferListOtherAccount2.Value[0].Quantity.ShouldBe(buyAmount);
            OfferListOtherAccount2.Value[0].ExpireTime.ShouldBe(expireTime1);
            // Check event
            logs = CustomMade3.Logs.First(l => l.Name.Equals("OfferCanceled")).NonIndexed;
            byteString = ByteString.FromBase64(logs);
            offerCanceledLogs = OfferCanceled.Parser.ParseFrom(byteString);
            offerCanceledLogs.Symbol.ShouldBe(symbol);
            offerCanceledLogs.TokenId.ShouldBe(tokenId);
            offerCanceledLogs.OfferFrom.ShouldBe(OtherAccount2.ConvertAddress());
            offerCanceledLogs.IndexList.Value[0].ShouldBe(0);
            offerCanceledLogs.IndexList.Value[1].ShouldBe(2);
            // Check event
            logs = CustomMade3.Logs.First(l => l.Name.Equals("OfferRemoved")).NonIndexed;
            byteString = ByteString.FromBase64(logs);
            offerRemovedLogs = OfferRemoved.Parser.ParseFrom(byteString);
            offerRemovedLogs.Symbol.ShouldBe(symbol);
            offerRemovedLogs.TokenId.ShouldBe(tokenId);
            offerRemovedLogs.OfferFrom.ShouldBe(OtherAccount2.ConvertAddress());
            offerRemovedLogs.OfferTo.ShouldBe(InitAccount.ConvertAddress());
            offerRemovedLogs.ExpireTime.ShouldBe(expireTime1);


            //33. InitAccount CamcelOffer , expireTime < UtcNow 
            Thread.Sleep(90 * 1000);
            _nftMarketContract.SetAccount(InitAccount);

            var CustomMade4 =
                _nftMarketContract.ExecuteMethodWithResult(NFTMarketContractMethod.CancelOffer, new CancelOfferInput
                {
                    Symbol = symbol,
                    TokenId = tokenId,
                    OfferFrom = OtherAccount3.ConvertAddress(),
                    IsCancelBid = false
                });

            CustomMade4.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);
            OfferList1 = _nftMarketContract.GetOfferList(symbol, tokenId, OtherAccount1);
            OfferList1.Value.Count.ShouldBe(1);
            OfferListOtherAccount2 = _nftMarketContract.GetOfferList(symbol, tokenId, OtherAccount2);
            OfferListOtherAccount2.Value.Count.ShouldBe(1);
            OfferListOtherAccount2.Value[0].Price.Amount.ShouldBe(7_00000000);
            OfferListOtherAccount3 = _nftMarketContract.GetOfferList(symbol, tokenId, OtherAccount3);
            OfferListOtherAccount3.Value.Count.ShouldBe(1);
            OfferListOtherAccount3.Value[0].Price.Amount.ShouldBe(10_00000000);
        }

        [TestMethod]
        public void CancelOfferTest2()
        {
            var tokenId = 1;
            var totalSupply = 10000;
            var mintAmount = 1;
            var startingPrice = 12_00000000;
            var earnestMoney = 11_00000000;
            var buyAmount = 1;
            var expireTime = DateTime.UtcNow.AddHours(24).ToTimestamp();
            var expireTime1 = DateTime.UtcNow.AddSeconds(30).ToTimestamp();
            var startTime = DateTime.UtcNow.ToTimestamp();
            var durationHours = 1;
            var symbol = CreateAndMintUnReuse(totalSupply, mintAmount, tokenId);

            Logger.Info($"startTime is {startTime.Seconds}");
            var purchaseSymbol = "USDT";
            var isMerge = true;

            //ListWithEnglistAuction
            ListWithEnglistAuction(symbol, tokenId, startingPrice, purchaseSymbol,
                startTime, durationHours, earnestMoney);

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


            //34.buyer CamcelOffer , expireTime > UtcNow  
            _nftMarketContract.SetAccount(OtherAccount);
            MakeOffer(6_00000000, symbol, expireTime);

            var OfferListOtherAccount = _nftMarketContract.GetOfferList(symbol, tokenId, OtherAccount);
            OfferListOtherAccount.Value.Count.ShouldBe(1);

            _nftMarketContract.SetAccount(OtherAccount);
            var CustomMade3 = _nftMarketContract.CancelOffer(
                symbol,
                tokenId,
                new Int32List
                {
                    Value = { 0 }
                },
                OtherAccount,
                false
            );
            CustomMade3.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);
            OfferListOtherAccount = _nftMarketContract.GetOfferList(symbol, tokenId, OtherAccount);
            OfferListOtherAccount.Value.Count.ShouldBe(0);
            // Check event
            var logs = CustomMade3.Logs.First(l => l.Name.Equals("OfferCanceled")).NonIndexed;
            var byteString = ByteString.FromBase64(logs);
            var offerCanceledLogs = OfferCanceled.Parser.ParseFrom(byteString);
            offerCanceledLogs.Symbol.ShouldBe(symbol);
            offerCanceledLogs.TokenId.ShouldBe(tokenId);
            offerCanceledLogs.OfferFrom.ShouldBe(OtherAccount.ConvertAddress());
            offerCanceledLogs.IndexList.Value[0].ShouldBe(0);
            // Check event
            logs = CustomMade3.Logs.First(l => l.Name.Equals("OfferRemoved")).NonIndexed;
            byteString = ByteString.FromBase64(logs);
            var offerRemovedLogs = OfferRemoved.Parser.ParseFrom(byteString);
            offerRemovedLogs.Symbol.ShouldBe(symbol);
            offerRemovedLogs.TokenId.ShouldBe(tokenId);
            offerRemovedLogs.OfferFrom.ShouldBe(OtherAccount.ConvertAddress());
            offerRemovedLogs.OfferTo.ShouldBe(InitAccount.ConvertAddress());
            offerRemovedLogs.ExpireTime.ShouldBe(expireTime);

            //34.1.buyer CamcelBid , expireTime > UtcNow  
            var balanceOtherAccount = _tokenContract.GetUserBalance(OtherAccount, "USDT");
            Logger.Info($"balanceOtherAccount is {balanceOtherAccount}");

            _nftMarketContract.SetAccount(OtherAccount);
            MakeOffer(13_00000000, symbol, expireTime);

            var balanceOtherAccount1 = _tokenContract.GetUserBalance(OtherAccount, "USDT");
            Logger.Info($"balanceOtherAccount1 is {balanceOtherAccount1}");


            var BidListOtherAccount = _nftMarketContract.GetBidList(symbol, tokenId);
            BidListOtherAccount.Value.Count.ShouldBe(1);

            _nftMarketContract.SetAccount(OtherAccount);
            var CustomMade4 = _nftMarketContract.CancelOffer(
                symbol,
                tokenId,
                new Int32List
                {
                    Value = { 0 }
                },
                OtherAccount,
                true
            );
            CustomMade4.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);
            var BidList1OtherAccount = _nftMarketContract.GetBidList(symbol, tokenId);
            BidList1OtherAccount.Value.Count.ShouldBe(0);
            var balance3OtherAccount = _tokenContract.GetUserBalance(OtherAccount, "USDT");
            Logger.Info($"balance3OtherAccount is {balance3OtherAccount}");
            // Check event
            logs = CustomMade4.Logs.First(l => l.Name.Equals("BidCanceled")).NonIndexed;
            byteString = ByteString.FromBase64(logs);
            var bidCanceledLogs = BidCanceled.Parser.ParseFrom(byteString);
            bidCanceledLogs.Symbol.ShouldBe(symbol);
            bidCanceledLogs.TokenId.ShouldBe(tokenId);
            bidCanceledLogs.BidFrom.ShouldBe(OtherAccount.ConvertAddress());
            bidCanceledLogs.BidTo.ShouldBe(InitAccount.ConvertAddress());


            //34.2.buyer CamcelBid , expireTime > UtcNow  ，nft is defined
            var BalanceOtherAccount = _tokenContract.GetUserBalance(OtherAccount, "USDT");
            Logger.Info($"BalanceOtherAccount is {BalanceOtherAccount}");

            //Makeoffer
            _nftMarketContract.SetAccount(OtherAccount);
            MakeOffer(14_00000000, symbol, expireTime);
            _nftMarketContract.SetAccount(OtherAccount1);
            MakeOffer(15_00000000, symbol, expireTime);
            var Balance4OtherAccount = _tokenContract.GetUserBalance(OtherAccount, "USDT");
            Logger.Info($"Balance4OtherAccount is {Balance4OtherAccount}");
            BidListOtherAccount = _nftMarketContract.GetBidList(symbol, tokenId);
            BidListOtherAccount.Value.Count.ShouldBe(2);

            // Deal
            _nftMarketContract.SetAccount(InitAccount);
            var deal = _nftMarketContract.Deal(
                symbol,
                tokenId,
                OtherAccount1,
                new Price
                {
                    Symbol = purchaseSymbol,
                    Amount = 15_00000000
                },
                buyAmount
            );
            deal.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);

            _nftMarketContract.SetAccount(OtherAccount);
            var CustomMade5 = _nftMarketContract.CancelOffer(
                symbol,
                tokenId,
                new Int32List
                {
                    Value = { 0 }
                },
                OtherAccount,
                true
            );
            CustomMade5.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);
            BidListOtherAccount = _nftMarketContract.GetBidList(symbol, tokenId);
            BidListOtherAccount.Value.Count.ShouldBe(0);
            Logger.Info($"GetBidList is {BidListOtherAccount}");

            var Balance3OtherAccount = _tokenContract.GetUserBalance(OtherAccount, "USDT");
            Logger.Info($"Balance3OtherAccount is {Balance3OtherAccount}");
            Balance3OtherAccount.ShouldBe(Balance4OtherAccount + 11_00000000);


            //34.3.buyer CamcelBid , expireTime < UtcNow 
            // Approve
            _nftContract.SetAccount(OtherAccount1);
            var approve =
                _nftContract.Approve(_nftMarketContract.ContractAddress, symbol, tokenId, 1000000);
            approve.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            //ListWithEnglistAuction
            _nftMarketContract.SetAccount(OtherAccount1);
            ListWithEnglistAuction(symbol, tokenId, startingPrice, purchaseSymbol,
                startTime, durationHours, earnestMoney);

            var balance1OtherAccount1 = _tokenContract.GetUserBalance(OtherAccount, "USDT");
            Logger.Info($"balance1OtherAccount1 is {balance1OtherAccount1}");
            _nftMarketContract.SetAccount(OtherAccount);
            var CustomMade = _nftMarketContract.MakeOffer(
                symbol,
                tokenId,
                OtherAccount1,
                buyAmount,
                new Price
                {
                    Symbol = "USDT",
                    Amount = 20_00000000,
                },
                expireTime1
            );
            CustomMade.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);

            var balance2OtherAccount1 = _tokenContract.GetUserBalance(OtherAccount, "USDT");
            Logger.Info($"balance2OtherAccount1 is {balance2OtherAccount1}");
            BidListOtherAccount = _nftMarketContract.GetBidList(symbol, tokenId);
            BidListOtherAccount.Value.Count.ShouldBe(1);


            _nftMarketContract.SetAccount(OtherAccount);
            Thread.Sleep(60 * 1000);
            var CustomMade6 = _nftMarketContract.CancelOffer(
                symbol,
                tokenId,
                new Int32List
                {
                    Value = { 0 }
                },
                OtherAccount,
                true
            );
            CustomMade6.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);
            BidListOtherAccount = _nftMarketContract.GetBidList(symbol, tokenId);
            BidListOtherAccount.Value.Count.ShouldBe(0);
            var balanceOther1Account1 = _tokenContract.GetUserBalance(OtherAccount, "USDT");
            Logger.Info($"balance3OtherAccount is {balanceOther1Account1}");
            balanceOther1Account1.ShouldBe(balance2OtherAccount1 + 11_00000000);


            //35.1.Admin CamcelBid , expireTime < UtcNow  
            var balanceOtherAccount3 = _tokenContract.GetUserBalance(OtherAccount3, "USDT");
            Logger.Info($"balance1OtherAccount1 is {balanceOtherAccount3}");
            _nftMarketContract.SetAccount(OtherAccount3);
            var CustomMade1 = _nftMarketContract.MakeOffer(
                symbol,
                tokenId,
                OtherAccount1,
                buyAmount,
                new Price
                {
                    Symbol = "USDT",
                    Amount = 21_00000000,
                },
                expireTime1
            );
            CustomMade1.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);

            var balance1OtherAccount3 = _tokenContract.GetUserBalance(OtherAccount3, "USDT");
            Logger.Info($"balance1OtherAccount3 is {balance1OtherAccount3}");

            BidListOtherAccount = _nftMarketContract.GetBidList(symbol, tokenId);
            BidListOtherAccount.Value.Count.ShouldBe(1);

            _nftMarketContract.SetAccount(InitAccount);
            Thread.Sleep(60 * 1000);
            var CustomMade8 =
                _nftMarketContract.ExecuteMethodWithResult(NFTMarketContractMethod.CancelOffer, new CancelOfferInput
                {
                    Symbol = symbol,
                    TokenId = tokenId,
                    OfferFrom = OtherAccount3.ConvertAddress(),
                    IsCancelBid = true
                });
            CustomMade8.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);
            var balance3OtherAccount3 = _tokenContract.GetUserBalance(OtherAccount3, "USDT");
            Logger.Info($"balanceOtherAccount3 is {balance3OtherAccount3}");
            balance3OtherAccount3.ShouldBe(balance1OtherAccount3 + 11_00000000);
            BidListOtherAccount = _nftMarketContract.GetBidList(symbol, tokenId);
            BidListOtherAccount.Value.Count.ShouldBe(0);

            //35.Admin CamcelBid , expireTime > UtcNow  ，nft is sell
            var BalanceOtherAccount2 = _tokenContract.GetUserBalance(OtherAccount2, "USDT");
            Logger.Info($"BalanceOtherAccount2 is {BalanceOtherAccount2}");

            //Makeoffer
            _nftMarketContract.SetAccount(OtherAccount2);
            var CustomMade2 = _nftMarketContract.MakeOffer(
                symbol,
                tokenId,
                OtherAccount1,
                buyAmount,
                new Price
                {
                    Symbol = "USDT",
                    Amount = 25_00000000,
                },
                expireTime
            );
            CustomMade2.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);

            _nftMarketContract.SetAccount(OtherAccount3);
            var Custom = _nftMarketContract.MakeOffer(
                symbol,
                tokenId,
                OtherAccount1,
                buyAmount,
                new Price
                {
                    Symbol = "USDT",
                    Amount = 26_00000000,
                },
                expireTime
            );
            Custom.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);

            var Balance1OtherAccount2 = _tokenContract.GetUserBalance(OtherAccount2, "USDT");
            Logger.Info($"Balance1OtherAccount2 is {Balance1OtherAccount2}");

            BidListOtherAccount = _nftMarketContract.GetBidList(symbol, tokenId);
            BidListOtherAccount.Value.Count.ShouldBe(2);
            Logger.Info($"GetBidList3 is {BidListOtherAccount}");

            // Deal
            _nftMarketContract.SetAccount(OtherAccount1);
            var deal1 = _nftMarketContract.Deal(
                symbol,
                tokenId,
                OtherAccount3,
                new Price
                {
                    Symbol = purchaseSymbol,
                    Amount = 26_00000000
                },
                buyAmount
            );
            deal1.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);
            BidListOtherAccount = _nftMarketContract.GetBidList(symbol, tokenId);
            BidListOtherAccount.Value.Count.ShouldBe(0);
            Logger.Info($"GetBidList3 is {BidListOtherAccount}");

            _nftMarketContract.SetAccount(InitAccount);
            var CustomMade7 =
                _nftMarketContract.ExecuteMethodWithResult(NFTMarketContractMethod.CancelOffer, new CancelOfferInput
                    {
                        Symbol = symbol,
                        TokenId = tokenId,
                        OfferFrom = OtherAccount2.ConvertAddress(),
                        IsCancelBid = true
                    }
                );
            CustomMade7.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);
            BidListOtherAccount = _nftMarketContract.GetBidList(symbol, tokenId);
            BidListOtherAccount.Value.Count.ShouldBe(0);
            Logger.Info($"GetBidList3 is {BidListOtherAccount}");
            var Balance2OtherAccount2 = _tokenContract.GetUserBalance(OtherAccount2, "USDT");
            Logger.Info($"Balance2OtherAccount2 is {Balance2OtherAccount2}");
            Balance2OtherAccount2.ShouldBe(Balance1OtherAccount2 + 11_00000000);

            //admin CancelOffer
            _nftMarketContract.SetAccount(OtherAccount3);
            var Custom1 = _nftMarketContract.MakeOffer(
                symbol,
                tokenId,
                OtherAccount1,
                buyAmount,
                new Price
                {
                    Symbol = "USDT",
                    Amount = 26_00000000,
                },
                expireTime1
            );
            Custom1.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);

            _nftMarketContract.SetAccount(InitAccount);
            var CustomMade9 =
                _nftMarketContract.ExecuteMethodWithResult(NFTMarketContractMethod.CancelOffer, new CancelOfferInput
                    {
                        Symbol = symbol,
                        TokenId = tokenId,
                        OfferFrom = OtherAccount3.ConvertAddress(),
                        IsCancelBid = true
                    }
                );
            CustomMade9.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);
            // Check event
            logs = CustomMade9.Logs.First(l => l.Name.Equals("BidCanceled")).NonIndexed;
            byteString = ByteString.FromBase64(logs);
            bidCanceledLogs = BidCanceled.Parser.ParseFrom(byteString);
            bidCanceledLogs.Symbol.ShouldBe(symbol);
            bidCanceledLogs.TokenId.ShouldBe(tokenId);
            bidCanceledLogs.BidFrom.ShouldBe(OtherAccount3.ConvertAddress());
            bidCanceledLogs.BidTo.ShouldBe(OtherAccount1.ConvertAddress());
        }


        public void MakeOffer1(long tokenId, long purchaseAmount, string symbol, Timestamp expireTime)
        {
            var buyAmount = 1;
            var CustomMade = _nftMarketContract.MakeOffer(
                symbol,
                tokenId,
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
        }

        [TestMethod]
        public void CancelOfferTest3()
        {
            var tokenId = 1;
            var totalSupply = 10000;
            var mintAmount = 1;
            var sellAmount = 1;
            var fixedPrice = 25_00000000;
            var whitePrice1 = 18_00000000;
            var durationHours = 24;
            var expireTime = DateTime.UtcNow.AddHours(24).ToTimestamp();
            var startTime = DateTime.UtcNow.ToTimestamp();
            var publicTime = DateTime.UtcNow.AddHours(30).ToTimestamp();
            var symbol = CreateAndMintUnReuse(totalSupply, mintAmount, tokenId);
            Logger.Info($"startTime is {startTime.Seconds}");
            Logger.Info($"publicTime is {publicTime.Seconds}");
            var purchaseSymbol = "USDT";
            var depositRate = 1000;
            var workHours = 1;
            var whiteListHours = 1;
            var purchaseAmount = 20_00000000;
            var isConfirm = true;
            var isConfirm1 = false;

            //approve
            _nftContract.SetAccount(OtherAccount);
            var approve1 = _tokenContract.ApproveToken(OtherAccount, _nftMarketContract.ContractAddress,
                10000000000_00000000, "USDT");
            approve1.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            _nftContract.SetAccount(OtherAccount1);
            var approve2 = _tokenContract.ApproveToken(OtherAccount1, _nftMarketContract.ContractAddress,
                10000000000_00000000, "USDT");
            approve2.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);


            _nftMarketContract.SetAccount(InitAccount);
            var setCustomizeInfoResult = _nftMarketContract.SetCustomizeInfo(symbol, depositRate, new Price
            {
                Symbol = purchaseSymbol,
                Amount = purchaseAmount
            }, workHours, whiteListHours, 0);
            setCustomizeInfoResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);


            //36.buyer CancelOffer，InitAccount no confirmation
            //Makeoffer
            _nftMarketContract.SetAccount(OtherAccount);
            MakeOffer1(2, 20_00000000, symbol, expireTime);
            var RequestInfo = _nftMarketContract.GetRequestInfo(symbol, 2);
            Logger.Info($"GetRequestInfo is {RequestInfo}");

            var balanceOtherAccount = _tokenContract.GetUserBalance(OtherAccount, "USDT");
            Logger.Info($"balanceOtherAccount is {balanceOtherAccount}");

            _nftMarketContract.SetAccount(OtherAccount);
            var CustomMade = _nftMarketContract.CancelOffer(
                symbol,
                2,
                new Int32List
                {
                    Value = { 0 }
                },
                OtherAccount,
                false
            );
            CustomMade.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);
            var balanceOtherAccountAfter = _tokenContract.GetUserBalance(OtherAccount, "USDT");
            Logger.Info($"balanceOtherAccountAfter is {balanceOtherAccountAfter}");
            balanceOtherAccountAfter.ShouldBe(balanceOtherAccount + 2_00000000);


            //37-38
            //Makeoffer
            _nftMarketContract.SetAccount(OtherAccount);
            Thread.Sleep(60 * 1000);
            MakeOffer1(2, 20_00000000, symbol, expireTime);
            var RequestInfo1 = _nftMarketContract.GetRequestInfo(symbol, 2);
            Logger.Info($"GetRequestInfo is {RequestInfo1}");

            var balance1OtherAccount = _tokenContract.GetUserBalance(OtherAccount, "USDT");
            Logger.Info($"balance1OtherAccount is {balance1OtherAccount}");
            var balanceInitAccount = _tokenContract.GetUserBalance(InitAccount, "USDT");
            Logger.Info($"balanceInitAccount is {balanceInitAccount}");

            //HandleRequest
            _nftMarketContract.SetAccount(InitAccount);
            var handleRequestResult = _nftMarketContract.HandleRequest(symbol, 2, OtherAccount, isConfirm);
            handleRequestResult.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);

            //CancelOffer_37.InitAccount CancelOffer，InitAccount no confirmation
            _nftMarketContract.SetAccount(InitAccount);
            Thread.Sleep(60 * 1000);
            var CustomMade1 = _nftMarketContract.CancelOffer(
                symbol,
                2,
                new Int32List
                {
                    Value = { 0 }
                },
                OtherAccount,
                false
            );
            CustomMade1.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);
            RequestInfo1 = _nftMarketContract.GetRequestInfo(symbol, 2);
            Logger.Info($"GetRequestInfo is {RequestInfo1}");


            //CancelOffer_38.buyer CancelOffer，InitAccount  confirmation，HandleRequest+work_hours > UtcNow
            Thread.Sleep(60 * 1000);
            _nftMarketContract.SetAccount(OtherAccount);
            var CustomMade2 = _nftMarketContract.CancelOffer(
                symbol,
                2,
                new Int32List
                {
                    Value = { 0 }
                },
                OtherAccount,
                false
            );
            CustomMade2.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);
            RequestInfo1 = _nftMarketContract.GetRequestInfo(symbol, 2);
            Logger.Info($"GetRequestInfo is {RequestInfo1}");
            var balanceOtherAccountAfter1 = _tokenContract.GetUserBalance(OtherAccount, "USDT");
            Logger.Info($"balanceOtherAccountAfter1 is {balanceOtherAccountAfter1}");
            balanceOtherAccountAfter1.ShouldBe(balance1OtherAccount);
            var balanceInitAccountAfter1 = _tokenContract.GetUserBalance(InitAccount, "USDT");
            Logger.Info($"balanceInitAccountAfter1 is {balanceInitAccountAfter1}");

            //38.1.successful purchase
            //Makeoffer
            _nftMarketContract.SetAccount(OtherAccount);
            MakeOffer1(3, purchaseAmount, symbol, expireTime);

            //HandleRequest
            _nftMarketContract.SetAccount(InitAccount);
            var handleRequestResult2 = _nftMarketContract.HandleRequest(symbol, 3, OtherAccount, isConfirm);
            handleRequestResult2.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);
            //Mint TOKEN
            _nftContract.SetAccount(InitAccount);
            var mintResult =
                _nftContract.ExecuteMethodWithResult(NftContractMethod.Mint, new MintInput
                {
                    Symbol = symbol,
                    Alias = "NFT_CO_CAT1",
                    Metadata = new Metadata(),
                    Quantity = mintAmount,
                    TokenId = 3
                });
            mintResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            _nftMarketContract.SetAccount(InitAccount);
            //ListWithFixedPrice
            ListWithFixedPrice_Request(symbol, 3, sellAmount, fixedPrice, whitePrice1,
                startTime, publicTime, durationHours, purchaseSymbol, OtherAccount);

            //MakeOffer
            var tailbalanceOtherAccount = _tokenContract.GetUserBalance(OtherAccount, "USDT");
            Logger.Info($"tailbalanceOtherAccount is {tailbalanceOtherAccount}");
            var tailbalanceInitAccount = _tokenContract.GetUserBalance(InitAccount, "USDT");
            Logger.Info($"tailbalanceInitAccount is {tailbalanceInitAccount}");

            _nftMarketContract.SetAccount(OtherAccount);
            MakeOffer1(3, purchaseAmount, symbol, expireTime);

            balanceOtherAccount = _tokenContract.GetUserBalance(OtherAccount, "USDT");
            Logger.Info($"tailbalance1OtherAccount is {balanceOtherAccount}");
            balanceInitAccount = _tokenContract.GetUserBalance(InitAccount, "USDT");
            Logger.Info($"tailbalance1InitAccount is {balanceInitAccount}");
            var offerList = _nftMarketContract.GetOfferList(symbol, 3, OtherAccount);
            Logger.Info($"offerList is {offerList}");
            var nftBalance = _nftContract.GetBalance(OtherAccount, symbol, 3);
            Logger.Info($"nftBalance is {nftBalance}");
        }


        [TestMethod]
        public void CancelOfferListWithDutchAuction()
        {
            var tokenId = 1;
            var totalSupply = 10000;
            var mintAmount = 1;
            var startingPrice = 12_00000000;
            var endingPrice = 11_00000000;
            var expireTime = DateTime.UtcNow.AddHours(24).ToTimestamp();
            var expireTime1 = DateTime.UtcNow.AddSeconds(30).ToTimestamp();
            var startTime = DateTime.UtcNow.ToTimestamp();
            var durationHours = 1;
            var symbol = CreateAndMintUnReuse(totalSupply, mintAmount, tokenId);
            Logger.Info($"startTime is {startTime.Seconds}");
            var purchaseSymbol = "USDT";
            var isMerge = true;

            //ListWithDutchAuction
            ListWithDutchAuction(symbol, tokenId, startingPrice, endingPrice, purchaseSymbol,
                startTime, durationHours);

            //approve
            _nftContract.SetAccount(OtherAccount);
            var approve1 = _tokenContract.ApproveToken(OtherAccount, _nftMarketContract.ContractAddress,
                10000000000_00000000, "USDT");
            approve1.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            _nftContract.SetAccount(OtherAccount1);
            var approve2 = _tokenContract.ApproveToken(OtherAccount1, _nftMarketContract.ContractAddress,
                10000000000_00000000, "USDT");
            approve2.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            //44.expireTime>UtcNow
            _nftMarketContract.SetAccount(OtherAccount);
            MakeOffer(6_00000000, symbol, expireTime);
            var OfferListOtherAccount = _nftMarketContract.GetOfferList(symbol, tokenId, OtherAccount);
            OfferListOtherAccount.Value.Count.ShouldBe(1);
            Logger.Info($"OfferListOtherAccount is {OfferListOtherAccount}");

            //CancelOffer
            _nftMarketContract.SetAccount(OtherAccount);
            var CustomMade = _nftMarketContract.CancelOffer(
                symbol,
                tokenId,
                new Int32List
                {
                    Value = { 0 }
                },
                OtherAccount,
                false
            );
            CustomMade.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);
            OfferListOtherAccount = _nftMarketContract.GetOfferList(symbol, tokenId, OtherAccount);
            OfferListOtherAccount.Value.Count.ShouldBe(0);

            //45.expireTime<UtcNow
            _nftMarketContract.SetAccount(OtherAccount1);
            MakeOffer(6_00000000, symbol, expireTime1);
            var OfferListOtherAccount1 = _nftMarketContract.GetOfferList(symbol, tokenId, OtherAccount1);
            OfferListOtherAccount1.Value.Count.ShouldBe(1);
            Logger.Info($"OfferListOtherAccount1 is {OfferListOtherAccount1}");

            //CancelOffer
            _nftMarketContract.SetAccount(OtherAccount1);
            var CustomMade1 = _nftMarketContract.CancelOffer(
                symbol,
                tokenId,
                new Int32List
                {
                    Value = { 0 }
                },
                OtherAccount1,
                true
            );
            CustomMade1.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);
            OfferListOtherAccount1 = _nftMarketContract.GetOfferList(symbol, tokenId, OtherAccount1);
            OfferListOtherAccount1.Value.Count.ShouldBe(0);
        }

        [TestMethod]
        public void CancelOfferTest41111()
        {
            //nft:KxY8eAgKjofYqvBaqavujXHyBrekXEpb32wc8QN65Cd8HTRQj
            //nftmarket:2oMp9P7cHVHd37jPLwPjSYP6NGPu83LV9gVb6zFA4rcsxxzpqo
        }


        //39——43
        [TestMethod]
        [DataRow(0, 2)]
        [DataRow(1_00000000, 3)]
        [DataRow(1_0000000, 4)]
        [DataRow(1_00000000, 5)]
        [DataRow(1_00000000, 6)]
        public void CancelOfferTest4(long stakingAmount, int tokenId)
        {
            //var tokenId = 1;
            var totalSupply = 10000;
            var mintAmount = 1;
            var expireTime = DateTime.UtcNow.AddHours(24).ToTimestamp();
            var expireTime1 = DateTime.UtcNow.AddSeconds(1).ToTimestamp();
            var symbol = CreateAndMintUnReuse(totalSupply, mintAmount, 1);
            var purchaseSymbol = "USDT";
            var depositRate = 1000;
            var workHours = 0;
            var whiteListHours = 1;
            var purchaseAmount = 20_00000000;
            var isConfirm = true;


            //approve
            _nftContract.SetAccount(OtherAccount);
            var approve1 = _tokenContract.ApproveToken(OtherAccount, _nftMarketContract.ContractAddress,
                10000000000_00000000, "USDT");
            approve1.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            _nftContract.SetAccount(InitAccount);
            var approve2 = _tokenContract.ApproveToken(InitAccount, _nftMarketContract.ContractAddress,
                10000000000_00000000, "USDT");
            approve2.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            var balanceInitAccount = _tokenContract.GetUserBalance(InitAccount, "USDT");
            Logger.Info($"balanceInitAccount is {balanceInitAccount}");
            //SetCustomizeInfo
            _nftMarketContract.SetAccount(InitAccount);
            var setCustomizeInfoResult = _nftMarketContract.SetCustomizeInfo(symbol, depositRate, new Price
            {
                Symbol = purchaseSymbol,
                Amount = purchaseAmount
            }, workHours, whiteListHours, stakingAmount);
            setCustomizeInfoResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            var balanceOtherAccount = _tokenContract.GetUserBalance(OtherAccount, "USDT");
            Logger.Info($"balance1OtherAccount is {balanceOtherAccount}");
            balanceInitAccount = _tokenContract.GetUserBalance(InitAccount, "USDT");
            Logger.Info($"balance1InitAccount is {balanceInitAccount}");
            //Makeoffer
            _nftMarketContract.SetAccount(OtherAccount);
            if (tokenId == 2 || tokenId == 3 || tokenId == 4 || tokenId == 6)
            {
                MakeOffer1(tokenId, purchaseAmount, symbol, expireTime);
            }
            else
            {
                MakeOffer1(tokenId, purchaseAmount, symbol, expireTime1);
            }

            var requestInfo = _nftMarketContract.GetRequestInfo(symbol, tokenId);
            Logger.Info($"requestInfo is {requestInfo}");
            balanceOtherAccount = _tokenContract.GetUserBalance(OtherAccount, "USDT");
            Logger.Info($"balance2OtherAccount is {balanceOtherAccount}");
            balanceInitAccount = _tokenContract.GetUserBalance(InitAccount, "USDT");
            Logger.Info($"balance2InitAccount is {balanceInitAccount}");

            //HandleRequest
            _nftMarketContract.SetAccount(InitAccount);
            var handleRequestResult2 = _nftMarketContract.HandleRequest(symbol, tokenId, OtherAccount, isConfirm);
            handleRequestResult2.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);

            balanceOtherAccount = _tokenContract.GetUserBalance(OtherAccount, "USDT");
            Logger.Info($"balance3OtherAccount is {balanceOtherAccount}");
            balanceInitAccount = _tokenContract.GetUserBalance(InitAccount, "USDT");
            Logger.Info($"balance3InitAccount is {balanceInitAccount}");

            if (tokenId == 6)
            {
                var sellAmount = 1;
                var fixedPrice = 25_00000000;
                var whitePrice1 = 18_00000000;
                var startTime = DateTime.UtcNow.ToTimestamp();
                var publicTime = DateTime.UtcNow.AddHours(20).ToTimestamp();
                var durationHours = 24;
                //Mint TOKEN
                _nftContract.SetAccount(InitAccount);
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
                _nftMarketContract.SetAccount(InitAccount);
                //ListWithFixedPrice
                var listWithFixedPrice = ListWithFixedPrice_Request(symbol, tokenId, sellAmount, fixedPrice,
                    whitePrice1,
                    startTime, publicTime, durationHours, purchaseSymbol, OtherAccount);
                listWithFixedPrice.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
                var GetListedNFTInfoList = _nftMarketContract.GetListedNFTInfoList(symbol, tokenId, InitAccount);
                Logger.Info($"GetListedNFTInfoList is {GetListedNFTInfoList}");

                //MakeOffer
                var tailbalanceOtherAccount = _tokenContract.GetUserBalance(OtherAccount, "USDT");
                Logger.Info($"tailbalanceOtherAccount is {tailbalanceOtherAccount}");
                var tailbalanceInitAccount = _tokenContract.GetUserBalance(InitAccount, "USDT");
                Logger.Info($"tailbalanceInitAccount is {tailbalanceInitAccount}");
                var WhiteListAddress = _nftMarketContract.GetWhiteListAddressPriceList(symbol, tokenId, OtherAccount);
                Logger.Info($"WhiteListAddress is {WhiteListAddress}");

                _nftMarketContract.SetAccount(OtherAccount);
                MakeOffer1(tokenId, purchaseAmount, symbol, expireTime);

                balanceOtherAccount = _tokenContract.GetUserBalance(OtherAccount, "USDT");
                Logger.Info($"tailbalance1OtherAccount is {balanceOtherAccount}");
                balanceInitAccount = _tokenContract.GetUserBalance(InitAccount, "USDT");
                Logger.Info($"tailbalance1InitAccount is {balanceInitAccount}");
                var offerList = _nftMarketContract.GetOfferList(symbol, tokenId, OtherAccount);
                Logger.Info($"offerList is {offerList}");
                var nftBalance = _nftContract.GetBalance(OtherAccount, symbol, tokenId);
                Logger.Info($"nftBalance is {nftBalance}");
                var nftBalanceInitAccount = _nftContract.GetBalance(InitAccount, symbol, tokenId);
                Logger.Info($"nftBalanceInitAccount is {nftBalanceInitAccount}");
                var GetRequestInfo = _nftMarketContract.GetRequestInfo(symbol, tokenId);
                Logger.Info($"GetRequestInfo is {GetRequestInfo}");
            }
            else
            {
                if (tokenId == 2 || tokenId == 3 || tokenId == 4)
                {
                    _nftMarketContract.SetAccount(OtherAccount);
                }
                else if (tokenId == 5)
                {
                    _nftMarketContract.SetAccount(InitAccount);
                }
                var CustomMade = _nftMarketContract.CancelOffer(
                    symbol,
                    tokenId,
                    new Int32List
                    {
                        Value = { 0 }
                    },
                    OtherAccount,
                    false
                );
                CustomMade.Status.ConvertTransactionResultStatus()
                    .ShouldBe(TransactionResultStatus.Mined);
                var GetRequestInfo = _nftMarketContract.GetRequestInfo(symbol, tokenId);
                Logger.Info($"GetRequestInfo is {GetRequestInfo}");
                balanceOtherAccount = _tokenContract.GetUserBalance(OtherAccount, "USDT");
                Logger.Info($"balance4OtherAccount is {balanceOtherAccount}");
                balanceInitAccount = _tokenContract.GetUserBalance(InitAccount, "USDT");
                Logger.Info($"balance4InitAccount is {balanceInitAccount}");

                // Check event
                var logs = CustomMade.Logs.First(l => l.Name.Equals("NFTRequestCancelled")).NonIndexed;
                var byteString = ByteString.FromBase64(logs);
                var nftRequestCancelledLogs = NFTRequestCancelled.Parser.ParseFrom(byteString);
                nftRequestCancelledLogs.Symbol.ShouldBe(symbol);
                nftRequestCancelledLogs.TokenId.ShouldBe(tokenId);
                nftRequestCancelledLogs.Requester.ShouldBe(OtherAccount.ConvertAddress());
                // Check event
                logs = CustomMade.Logs.First(l => l.Name.Equals("OfferRemoved")).NonIndexed;
                byteString = ByteString.FromBase64(logs);
                var offerRemovedLogs = OfferRemoved.Parser.ParseFrom(byteString);
                offerRemovedLogs.Symbol.ShouldBe(symbol);
                offerRemovedLogs.TokenId.ShouldBe(tokenId);
                offerRemovedLogs.OfferFrom.ShouldBe(OtherAccount.ConvertAddress());
                offerRemovedLogs.OfferTo.ShouldBe(InitAccount.ConvertAddress());
                offerRemovedLogs.ExpireTime.ShouldBe(expireTime);
                
            }
        }
    }
}
