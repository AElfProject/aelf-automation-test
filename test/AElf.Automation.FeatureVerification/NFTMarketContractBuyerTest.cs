using System;
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

        private string CreateAndMint1(string creator,long totalSupply, long mintAmount, long tokenId)
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

            // SetTokenWhiteList
            var result = _nftMarketContract.SetTokenWhiteList(symbol, new StringList
            {
                Value = { "USDT",purchaseSymbol }
            });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            // Approve
            var approve =
                _nftContract.Approve(_nftMarketContract.ContractAddress, symbol, tokenId, 1000000);

            approve.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            // ListWithFixedPrice
            var result10 =_nftMarketContract.ListWithFixedPrice(
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


            // Set token white list
            var setTokenWhiteListResult = _nftMarketContract.SetTokenWhiteList(symbol, new StringList
            {
                Value = { "ELF", "USDT" }
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


        public void ListWithEnglistAuction(string symbol, int tokenId, long startingPrice, string purchaseSymbol,
            Timestamp startTime, Timestamp publicTime, int durationHours, long earnestMoney, string whiteSymbol,
            long whitePrice)
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
                    Symbol = "ELF", //sym1
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
                10000000000_00000000, "ELF");
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
            // Approve
            _nftContract.SetAccount(OtherAccount);
            var approve1 = _tokenContract.ApproveToken(OtherAccount, _nftMarketContract.ContractAddress,
                10000000000_00000000, purchaseSymbol);
            approve1.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            // Approve
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


           //7.whitePrice < =  Enter amount < fixedPrice  ,  public_time  < UtcNow  
           Thread.Sleep(20 * 1000);
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
           //var tokenId10 = 10;
           //var dueTime10 = DateTime.UtcNow.AddSeconds(10).ToTimestamp();

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
            var publicTime = DateTime.UtcNow.AddHours(20).ToTimestamp();
            var durationHours = 1;
            var symbol = CreateAndMint(totalSupply, mintAmount, tokenId);
            Logger.Info($"startTime is {startTime.Seconds}");
            Logger.Info($"publicTime is {publicTime.Seconds}");
            var purchaseSymbol = "USDT";
            var isMerge = true;

            //NFT:2mM8SPmCHsuoeDEqt4K5td6iBGefqWdrk5BUmLKpGcxur9sA7G
            //NFTMarket：F1fwY8DFFtAvSvgy8CBg24hKGVDat5daYM3v6Y9PxvUtiXhU3

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
                10000000000_00000000, "ELF");
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
            var symbol = CreateAndMint(totalSupply, mintAmount, tokenId);
            Logger.Info($"startTime is {startTime.Seconds}");
            Logger.Info($"publicTime is {publicTime.Seconds}");
            var purchaseSymbol = "USDT";
            var isMerge = true;

            //NFT:24FMdYa98dmcjGhT3igGx5THyVVuH4tF5dC62z7Ebaedgz179h
            //NFTMarket：SC3qiGDHA1wW4o1QkYTJNbHhr1nfR48Na4vTzsbDXbxH4k4k8

            
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
            
            var getBalanceIWhiteListAddressStart = _nftContract.GetBalance(WhiteListAddress1, symbol, tokenId);
            Logger.Info($"getBalanceIWhiteListAddressStart is {getBalanceIWhiteListAddressStart.Balance}");

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
            
            var purchaseSymbol = CreateAndMint1(WhiteListAddress1,totalSupply, mintAmount, tokenId);
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
            
            var allowance = _nftContract.GetAllowance(purchaseSymbol, tokenId,WhiteListAddress2,_nftMarketContract.ContractAddress);
            Logger.Info($"allowance is {allowance}");
            
            var balance = _nftContract.GetBalance(WhiteListAddress2, purchaseSymbol,tokenId);
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
            Logger.Info($"startTime is {startTime.Seconds}");
            Logger.Info($"publicTime is {publicTime.Seconds}");
            var purchaseSymbol = "ELF";
            var isMerge = true;

            // Initialize
            ContractInitialize();
            // SetTokenWhiteList
            var result = _nftMarketContract.SetTokenWhiteList(symbol, new StringList
            {
                Value = { "USDT" }
            });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

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
            var BidList = _nftMarketContract.GetBidList(symbol, tokenId);
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

            BidList = _nftMarketContract.GetBidList(symbol, tokenId);
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

            // Initialize
            ContractInitialize();
            // SetTokenWhiteList
            var result = _nftMarketContract.SetTokenWhiteList(symbol, new StringList
            {
                Value = { "USDT" }
            });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

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
            var BidList = _nftMarketContract.GetBidList(symbol, tokenId);
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
                    Amount = startingPrice - 8_00000000
                },
                expireTime
            );

            Thread.Sleep(60 * 1000);

            BidList = _nftMarketContract.GetBidList(symbol, tokenId);
            BidList.Value.Count.ShouldBe(0);

            offerList = _nftMarketContract.GetOfferList(symbol, tokenId, OtherAccount);
            offerList.Value.Count.ShouldBe(1);
            offerList.Value.First().From.ShouldBe(OtherAccount.ConvertAddress());
            offerList.Value.First().To.ShouldBe(InitAccount.ConvertAddress());
            offerList.Value.First().Price.Symbol.ShouldBe("ELF");
            offerList.Value.First().Price.Amount.ShouldBe(startingPrice - 8_00000000);
            offerList.Value[0].Quantity.ShouldBe(1);
            offerList.Value[0].ExpireTime.ShouldBe(expireTime);


            //20.1.The user's first purchase is greater than bid, the second purchase is less than bid, and the third purchase is greater than bid
            var BidList1 = _nftMarketContract.GetBidList(symbol, tokenId);
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

            BidList1 = _nftMarketContract.GetBidList(symbol, tokenId);
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
                    Amount = startingPrice - 5_00000000
                },
                expireTime
            );

            offerList1 = _nftMarketContract.GetOfferList(symbol, tokenId, OtherAccount1);
            offerList1.Value.Count.ShouldBe(1);
            offerList1.Value[1].From.ShouldBe(OtherAccount1.ConvertAddress());
            offerList1.Value[1].To.ShouldBe(InitAccount.ConvertAddress());
            offerList1.Value[1].Price.Symbol.ShouldBe("ELF");
            offerList1.Value[1].Price.Amount.ShouldBe(startingPrice - 5_00000000);
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
                    Amount = startingPrice + 5_00000000
                },
                expireTime
            );
            offerList1 = _nftMarketContract.GetOfferList(symbol, tokenId, OtherAccount1);
            offerList1.Value.Count.ShouldBe(1);
            offerList1.Value[1].From.ShouldBe(OtherAccount1.ConvertAddress());
            offerList1.Value[1].To.ShouldBe(InitAccount.ConvertAddress());
            offerList1.Value[1].Price.Symbol.ShouldBe("ELF");
            offerList1.Value[1].Price.Amount.ShouldBe(startingPrice - 5_00000000);
            offerList1.Value[1].Quantity.ShouldBe(0);
            offerList.Value[1].ExpireTime.ShouldBe(expireTime);

            BidList1 = _nftMarketContract.GetBidList(symbol, tokenId);
            BidList1.Value.Count.ShouldBe(1);
            BidList1.Value[0].From.ShouldBe(OtherAccount1.ConvertAddress());
            BidList1.Value[0].To.ShouldBe(InitAccount.ConvertAddress());
            BidList1.Value[0].Price.Symbol.ShouldBe("ELF");
            BidList1.Value[0].Price.Amount.ShouldBe(startingPrice + 5_00000000);


            //21.1.offerto address tobuy
            // Approve
            _nftContract.SetAccount(InitAccount);
            var approve3 = _tokenContract.ApproveToken(InitAccount, _nftMarketContract.ContractAddress,
                10000000000_00000000, "ELF");
            approve3.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            _nftMarketContract.SetAccount(InitAccount);
            var result1 = _nftMarketContract.MakeOffer(
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

            result1.Error.ShouldContain("Origin owner cannot be sender himself.");
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
            var symbol = "CO644101375";
            Logger.Info($"startTime is {startTime.Seconds}");
            Logger.Info($"publicTime is {publicTime.Seconds}");
            var purchaseSymbol = "ELF";
            var isMerge = true;

            // Approve
            _nftContract.SetAccount(OtherAccount);
            var approve = _tokenContract.ApproveToken(OtherAccount, _nftMarketContract.ContractAddress,
                10000000000_00000000, "ELF");
            approve.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            //
            //
            //symbol:

            var offerList = _nftMarketContract.GetBidList(symbol, tokenId);
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
                    Amount = startingPrice + 50_00000000
                },
                expireTime
            );


            offerList = _nftMarketContract.GetBidList(symbol, tokenId);
            offerList.Value.Count.ShouldBe(1);
            offerList.Value[0].From.ShouldBe(OtherAccount.ConvertAddress());
            offerList.Value[0].To.ShouldBe(InitAccount.ConvertAddress());
            offerList.Value[0].Price.Symbol.ShouldBe("ELF");
            offerList.Value[0].Price.Amount.ShouldBe(startingPrice + 50_00000000);
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

            ListWithDutchAuction(symbol, tokenId, startingPrice, endingPrice, purchaseSymbol,
                startTime, publicTime, durationHours, whiteSymbol, whitePrice1);

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
                    Amount = startingPrice + 1_00000000
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
            var offerList1 = _nftMarketContract.GetBidList(symbol, tokenId);
            Logger.Info($"offerList is {offerList1}");

            //22.3. nft has been purchased, another user purchased again
            Thread.Sleep(10 * 1000);
            _nftMarketContract.SetAccount(OtherAccount1);
            var result1 = _nftMarketContract.MakeOffer(
                symbol,
                tokenId,
                InitAccount,
                buyAmount,
                new Price
                {
                    Symbol = "ELF",
                    Amount = startingPrice + 2_00000000
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
                    Amount = startingPrice - 1_00000000
                },
                expireTime
            );

            offerList = _nftMarketContract.GetOfferList(symbol, tokenId, OtherAccount);
            offerList.Value.Count.ShouldBe(1);
            offerList.Value[0].From.ShouldBe(OtherAccount.ConvertAddress());
            offerList.Value[0].To.ShouldBe(InitAccount.ConvertAddress());
            offerList.Value[0].Price.Symbol.ShouldBe("ELF");
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
                    Symbol = "ELF",
                    Amount = endingPrice - 1_00000000
                },
                expireTime
            );
            var offerList1 = _nftMarketContract.GetOfferList(symbol, tokenId, OtherAccount1);
            offerList1.Value.Count.ShouldBe(1);
            offerList1.Value[0].From.ShouldBe(OtherAccount1.ConvertAddress());
            offerList1.Value[0].To.ShouldBe(InitAccount.ConvertAddress());
            offerList1.Value[0].Price.Symbol.ShouldBe("ELF");
            offerList1.Value[0].Price.Amount.ShouldBe(endingPrice - 1_00000000);
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
                expireTime
            );
            CustomMade1.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            CustomMade1.Error.ShouldContain("Request already existed.");


            //29.1. endingPrice <=   Enter amount ,Seller confirm
            var balanceOtherAccount1Start1 = _tokenContract.GetUserBalance(OtherAccount1, "USDT");
            Logger.Info($"balanceOtherStart1 is {balanceOtherAccount1Start1}");

            var balanceInitAccountStart1 = _tokenContract.GetUserBalance(InitAccount, "USDT");
            Logger.Info($"balanceInitStart1 is {balanceInitAccountStart1}");

            var balanceWhiteListAddress2Start = _tokenContract.GetUserBalance(WhiteListAddress2, "USDT");
            Logger.Info($"balanceWhiteListAddress2Start is {balanceWhiteListAddress2Start}");

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
                expireTime
            );
            CustomMade2.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);

            _nftMarketContract.SetAccount(InitAccount);
            var handleRequestResult = _nftMarketContract.HandleRequest(symbol, 3, OtherAccount1, isConfirm);
            handleRequestResult.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);


            var RequestInfo1 = _nftMarketContract.GetRequestInfo(symbol, 3);
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
            var balanceOtherAccount2Start2 = _tokenContract.GetUserBalance(OtherAccount2, "USDT");
            Logger.Info($"balanceOtherStart2 is {balanceOtherAccount2Start2}");

            var balanceInitAccountStart2 = _tokenContract.GetUserBalance(InitAccount, "USDT");
            Logger.Info($"balanceInitStart2 is {balanceInitAccountStart2}");

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
                expireTime
            );

            CustomMade3.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);

            _nftMarketContract.SetAccount(InitAccount);
            var handleRequestResult1 = _nftMarketContract.HandleRequest(symbol, 4, OtherAccount2, isConfirm1);
            handleRequestResult1.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);


            var RequestInfo2 = _nftMarketContract.GetRequestInfo(symbol, 4);
            Logger.Info($"RequestInfo2 is {RequestInfo2}");
            var CustomizeInfo2 = _nftMarketContract.GetCustomizeInfo(symbol);
            Logger.Info($"CustomizeInfo2 is {CustomizeInfo2}");

            var balanceOtherAccount2Finish2 = _tokenContract.GetUserBalance(OtherAccount2, "USDT");
            Logger.Info($"balanceOtherFinish2 is {balanceOtherAccount2Finish2}");
            var balanceInitAccountFinish2 = _tokenContract.GetUserBalance(InitAccount, "USDT");
            Logger.Info($"balanceInitFinish2 is {balanceInitAccountFinish2}");
            


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
                expireTime.AddSeconds(10)
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
            var totalAmount = 1000;
            var totalSupply = 10000;
            var mintAmount = 1;
            var sellAmount = 1;
            var startingPrice = 12_00000000;
            var earnestMoney = 11_00000000;
            var whitePrice1 = 9_00000000;
            var buyAmount = 1;
            var whiteSymbol = "USDT";

            var expireTime = DateTime.UtcNow.AddHours(24).ToTimestamp();
            var expireTime1 = DateTime.UtcNow.AddSeconds(30).ToTimestamp();


            var startTime = DateTime.UtcNow.ToTimestamp();
            var publicTime = DateTime.UtcNow.AddHours(30).ToTimestamp();
            var durationHours = 1;
            var symbol = CreateAndMintUnReuse(totalSupply, mintAmount, tokenId);

            Logger.Info($"startTime is {startTime.Seconds}");
            Logger.Info($"publicTime is {publicTime.Seconds}");
            var purchaseSymbol = "USDT";
            var isMerge = true;


            // Initialize
            ContractInitialize();
            // SetTokenWhiteList
            var result = _nftMarketContract.SetTokenWhiteList(symbol, new StringList
            {
                Value = { "USDT" }
            });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            //ListWithEnglistAuction
            ListWithEnglistAuction(symbol, tokenId, startingPrice, purchaseSymbol,
                startTime, publicTime, durationHours, earnestMoney, whiteSymbol, whitePrice1);

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
            Thread.Sleep(60 * 1000);
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
            Thread.Sleep(60 * 1000);
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


            //34.2.buyer CamcelBid , expireTime > UtcNow  ，nft is defined
            var BalanceOtherAccount = _tokenContract.GetUserBalance(OtherAccount, "USDT");
            Logger.Info($"BalanceOtherAccount is {BalanceOtherAccount}");

            //Makeoffer
            _nftMarketContract.SetAccount(OtherAccount);
            MakeOffer(13_00000000, symbol, expireTime);
            _nftMarketContract.SetAccount(OtherAccount1);
            MakeOffer(14_00000000, symbol, expireTime);

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
                    Amount = 14_00000000
                },
                buyAmount
            );
            deal.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);


            _nftMarketContract.SetAccount(OtherAccount);
            Thread.Sleep(60 * 1000);
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


            //34.3.buyer CamcelBid , expireTime < UtcNow 
            // Approve
            _nftContract.SetAccount(OtherAccount1);
            var approve =
                _nftContract.Approve(_nftMarketContract.ContractAddress, symbol, tokenId, 1000000);
            approve.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            //ListWithEnglistAuction
            _nftMarketContract.SetAccount(OtherAccount1);
            ListWithEnglistAuction(symbol, tokenId, startingPrice, purchaseSymbol,
                startTime, publicTime, durationHours, earnestMoney, whiteSymbol, whitePrice1);

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


            //35.1.Admin CamcelBid , expireTime < UtcNow  
            var balanceOtherAccount3 = _tokenContract.GetUserBalance(OtherAccount3, "USDT");
            Logger.Info($"balance1OtherAccount1 is {balanceOtherAccount3}");
            Thread.Sleep(60 * 1000);
            _nftMarketContract.SetAccount(OtherAccount3);
            var CustomMade1 = _nftMarketContract.MakeOffer(
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

            BidListOtherAccount = _nftMarketContract.GetBidList(symbol, tokenId);
            BidListOtherAccount.Value.Count.ShouldBe(0);
            //35.Admin CamcelBid , expireTime > UtcNow  ，nft is defined
            var BalanceOtherAccount2 = _tokenContract.GetUserBalance(OtherAccount2, "USDT");
            Logger.Info($"BalanceOtherAccount is {BalanceOtherAccount}");

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
                    Amount = 20_00000000,
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
                    Amount = 21_00000000,
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
                    Amount = 21_00000000
                },
                buyAmount
            );
            deal1.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);


            _nftMarketContract.SetAccount(InitAccount);
            Thread.Sleep(60 * 1000);
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
        }


        [TestMethod]
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
            var totalAmount = 1000;
            var totalSupply = 10000;
            var mintAmount = 1;
            var sellAmount = 1;
            var startingPrice = 25_00000000;
            var earnestMoney = 11_00000000;
            var whitePrice1 = 9_00000000;
            var buyAmount = 1;
            var whiteSymbol = "USDT";


            var fixedPrice = 25_00000000;
            var whitePrice2 = 10_00000000;
            var whitePrice3 = 11_00000000;

            var expireTime = DateTime.UtcNow.AddHours(24).ToTimestamp();
            var expireTime1 = DateTime.UtcNow.AddSeconds(30).ToTimestamp();

            var startTime = DateTime.UtcNow.ToTimestamp();
            var publicTime = DateTime.UtcNow.AddHours(30).ToTimestamp();
            var durationHours = 1;
            var symbol = CreateAndMintUnReuse(totalSupply, mintAmount, tokenId);

            Logger.Info($"startTime is {startTime.Seconds}");
            Logger.Info($"publicTime is {publicTime.Seconds}");
            var purchaseSymbol = "USDT";

            var depositRate = 1000;
            var workHours = 1;
            var whiteListHours = 1;
            var purchaseAmount = 20_00000000;
            var isMerge = true;
            var isConfirm = true;
            var isConfirm1 = false;

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

            Thread.Sleep(60 * 1000);
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
            Thread.Sleep(60 * 1000);
            _nftMarketContract.SetAccount(InitAccount);
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


            //38.1.buyer CancelOffer，InitAccount  confirmation，HandleRequest+work_hours > UtcNow
            //Makeoffer
            _nftMarketContract.SetAccount(OtherAccount);
            MakeOffer1(2, 20_00000000, symbol, expireTime);
            var RequestInfo2 = _nftMarketContract.GetRequestInfo(symbol, 2);
            Logger.Info($"GetRequestInfo is {RequestInfo2}");

            var balance2OtherAccount = _tokenContract.GetUserBalance(OtherAccount, "USDT");
            Logger.Info($"balanceOtherAccount is {balance2OtherAccount}");
            var balance1InitAccount = _tokenContract.GetUserBalance(InitAccount, "USDT");
            Logger.Info($"balanceInitAccount! is {balance1InitAccount}");

            //HandleRequest
            _nftMarketContract.SetAccount(InitAccount);
            var handleRequestResult1 = _nftMarketContract.HandleRequest(symbol, 2, OtherAccount, isConfirm);
            handleRequestResult1.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);

            //CancelOffer
            Thread.Sleep(60 * 1000);
            _nftMarketContract.SetAccount(OtherAccount);
            var CustomMade3 = _nftMarketContract.CancelOffer(
                symbol,
                2,
                new Int32List
                {
                    Value = { 0 }
                },
                OtherAccount,
                false
            );
            CustomMade3.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);

            RequestInfo2 = _nftMarketContract.GetRequestInfo(symbol, 2);
            Logger.Info($"GetRequestInfo1 is {RequestInfo2}");

            var balanceOtherAccountAfter2 = _tokenContract.GetUserBalance(OtherAccount, "USDT");
            Logger.Info($"balanceOtherAccountAfter2 is {balanceOtherAccountAfter2}");
            balanceOtherAccountAfter1.ShouldBe(balance1OtherAccount);
            var balanceInitAccountAfter2 = _tokenContract.GetUserBalance(InitAccount, "USDT");
            Logger.Info($"balanceInitAccountAfter2 is {balanceInitAccountAfter2}");

            //Makeoffer
            _nftMarketContract.SetAccount(OtherAccount1);
            MakeOffer1(2, 20_00000000, symbol, expireTime);
            var RequestInfo3 = _nftMarketContract.GetRequestInfo(symbol, 2);
            Logger.Info($"GetRequestInfo3 is {RequestInfo3}");

            var balanceOtherAccount1 = _tokenContract.GetUserBalance(OtherAccount1, "USDT");
            Logger.Info($"balanceOtherAccount is {balanceOtherAccount1}");
            var balance2InitAccount = _tokenContract.GetUserBalance(InitAccount, "USDT");
            Logger.Info($"balance2InitAccount is {balance2InitAccount}");

            //HandleRequest
            _nftMarketContract.SetAccount(InitAccount);
            var handleRequestResult2 = _nftMarketContract.HandleRequest(symbol, 2, OtherAccount, isConfirm);
            handleRequestResult2.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);

            var minterList = _nftContract.GetMinterList(symbol);
            Logger.Info($"minterList is {minterList}");
            Logger.Info($"symbol is {symbol}");

            //Mint
            _nftContract.SetAccount(InitAccount);
            var mintResult =
                _nftContract.ExecuteMethodWithResult(NftContractMethod.Mint, new MintInput
                {
                    Symbol = symbol,
                    Alias = "NFT_CO_CAT1",
                    Metadata = new Metadata(),
                    Quantity = mintAmount,
                    TokenId = 2
                });
            mintResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            //ListWithFixedPrice
            var result = ListWithFixedPrice_Request(symbol, 2, 1, fixedPrice,
                18_00000000, startTime, publicTime, durationHours, purchaseSymbol,
                OtherAccount1);
            result.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);

            var balance333OtherAccount1 = _tokenContract.GetUserBalance(InitAccount, "USDT");
            Logger.Info($"balance333OtherAccount1 is {balance333OtherAccount1}");

            //Makeoffer
            var balance33333InitAccount = _tokenContract.GetUserBalance(OtherAccount1, "USDT");
            Logger.Info($"balance22222222InitAccount is {balance33333InitAccount}");

            _nftMarketContract.SetAccount(OtherAccount1);
            MakeOffer1(2, 18_00000000, symbol, expireTime);
            var balance3InitAccount = _tokenContract.GetUserBalance(OtherAccount1, "USDT");
            Logger.Info($"balance2InitAccount is {balance3InitAccount}");

            var balance444OtherAccount1 = _tokenContract.GetUserBalance(InitAccount, "USDT");
            Logger.Info($"balance444OtherAccount1 is {balance444OtherAccount1}");
            //balance444OtherAccount1.ShouldBe(balance3InitAccount +1_00000000 +);
        }

        //39——43
        [TestMethod]
        public RequestInfo CancelOfferTest4(long stakingAmount, int tokenId)
        {
            //var tokenId = 1;
            var totalAmount = 1000;
            var totalSupply = 10000;
            var mintAmount = 1;
            var sellAmount = 1;
            var startingPrice = 12_00000000;
            var earnestMoney = 11_00000000;
            var whitePrice1 = 9_00000000;
            var buyAmount = 1;
            var whiteSymbol = "USDT";


            var fixedPrice = 12_00000000;
            var whitePrice2 = 10_00000000;
            var whitePrice3 = 11_00000000;

            var expireTime = DateTime.UtcNow.AddHours(24).ToTimestamp();
            var expireTime1 = DateTime.UtcNow.AddSeconds(30).ToTimestamp();

            var startTime = DateTime.UtcNow.ToTimestamp();
            var publicTime = DateTime.UtcNow.AddHours(30).ToTimestamp();
            var durationHours = 1;
            var symbol = CreateAndMintUnReuse(totalSupply, mintAmount, 1);

            Logger.Info($"startTime is {startTime.Seconds}");
            Logger.Info($"publicTime is {publicTime.Seconds}");
            var purchaseSymbol = "USDT";

            var depositRate = 1000;
            var workHours = 1;
            var whiteListHours = 1;
            var purchaseAmount = 20_00000000;
            var isMerge = true;
            var isConfirm = true;
            var isConfirm1 = false;

            /*// Initialize
            ContractInitialize();*/

            //approve
            _nftContract.SetAccount(OtherAccount);
            var approve1 = _tokenContract.ApproveToken(OtherAccount, _nftMarketContract.ContractAddress,
                10000000000_00000000, "USDT");
            approve1.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);


            _nftContract.SetAccount(InitAccount);
            var approve3 = _tokenContract.ApproveToken(InitAccount, _nftMarketContract.ContractAddress,
                10000000000_00000000, "USDT");
            approve3.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);


            _nftMarketContract.SetAccount(InitAccount);
            var setCustomizeInfoResult = _nftMarketContract.SetCustomizeInfo(symbol, depositRate, new Price
            {
                Symbol = purchaseSymbol,
                Amount = purchaseAmount
            }, workHours, whiteListHours, stakingAmount);
            setCustomizeInfoResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);


            //Makeoffer
            _nftMarketContract.SetAccount(OtherAccount);
            MakeOffer1(tokenId, 20_00000000, symbol, expireTime);

            var requestInfo = _nftMarketContract.GetRequestInfo(symbol, tokenId);
            Logger.Info($"requestInfo is {requestInfo}");

            var balanceOtherAccount = _tokenContract.GetUserBalance(OtherAccount, "USDT");
            Logger.Info($"balanceOtherAccount is {balanceOtherAccount}");

            //HandleRequest
            _nftMarketContract.SetAccount(InitAccount);
            var handleRequestResult2 = _nftMarketContract.HandleRequest(symbol, tokenId, OtherAccount, isConfirm);
            handleRequestResult2.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);

            var balanceInitAccount = _tokenContract.GetUserBalance(InitAccount, "USDT");
            Logger.Info($"balanceInitAccount is {balanceInitAccount}");

            return requestInfo;
        }

        [TestMethod]
        [DataRow(0, 10)]
        [DataRow(1_00000000, 11)]
        [DataRow(5_00000000, 12)]
        [DataRow(1_00000000, 13)]
        [DataRow(1_00000000, 14)]
        //nft：2SJGKt7rhRv7aXzkhNbXx5f6LRoCxuTt77odtwNkg1d7yhBQpn
        //nftMarket:2pPYvikKEbnTcuSDh89hTc9zZsu2nEP4aFh1yKT8tL7UNoTvvv
        //symbol:CO833629449
        public void CancelOfferTest5(long stakingAmount, int tokenId)
        {
            var totalAmount = 1000;

            var sellAmount = 1;
            var startingPrice = 12_00000000;
            var earnestMoney = 11_00000000;
            var whitePrice1 = 9_00000000;
            var buyAmount = 1;
            var whiteSymbol = "USDT";


            var fixedPrice = 12_00000000;
            var whitePrice2 = 10_00000000;
            var whitePrice3 = 11_00000000;
            var purchaseSymbol = "USDT";
            var expireTime = DateTime.UtcNow.AddHours(24).ToTimestamp();
            var expireTime1 = DateTime.UtcNow.AddSeconds(30).ToTimestamp();
            var startTime = DateTime.UtcNow.ToTimestamp();
            var publicTime = DateTime.UtcNow.AddHours(30).ToTimestamp();
            var durationHours = 1;
            var totalSupply = 10000;
            var mintAmount = 1;
            var requestInfo = CancelOfferTest4(stakingAmount, tokenId);
            var symbol = requestInfo.Symbol;

            //39.CancelOffer
            var balance0OtherAccount = _tokenContract.GetUserBalance(OtherAccount, "USDT");
            Logger.Info($"balance0OtherAccount is {balance0OtherAccount}");
            var balance0InitAccount = _tokenContract.GetUserBalance(InitAccount, "USDT");
            Logger.Info($"balance0InitAccount is {balance0InitAccount}");

            if (tokenId == 2 || tokenId == 3)
            {
                _nftMarketContract.SetAccount(OtherAccount);
            }
            else
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


            var balanceOtherAccount = _tokenContract.GetUserBalance(OtherAccount, "USDT");
            Logger.Info($"balanceOtherAccount is {balanceOtherAccount}");
            var balanceInitAccount = _tokenContract.GetUserBalance(InitAccount, "USDT");
            Logger.Info($"balanceInitAccount is {balanceInitAccount}");


           
        }
    }
}
