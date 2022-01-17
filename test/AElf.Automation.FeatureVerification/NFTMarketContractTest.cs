using System;
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
        public string ListWithFixedPriceTest(int tokenId, long totalAmount, long sellAmount, long fixedPrice,
            long whitePrice1, long whitePrice2, long whitePrice3)
        {
            var symbol = CreateAndMint(totalAmount, tokenId);
            var startTime = DateTime.UtcNow.AddSeconds(10).ToTimestamp();
            var publicTime = DateTime.UtcNow.AddHours(24).ToTimestamp();
            var durationHours = 48;
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
                    Symbol = "ELF",
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
                            Address = InitAccount.ConvertAddress(),
                            Price = new Price
                            {
                                Symbol = "ELF",
                                Amount = whitePrice1
                            }
                        }
                    }
                },
                true
            );

            return symbol;
        }

        [TestMethod]
        // [DataRow("","")]
        // [DataRow("","")]
        // [DataRow("","")]
        public void ListWithEnglishAuctionTest()
        {
            var tokenId = 1;
            var totalAmount = 1000;
            var sellAmount = 100;
            var fixedPrice = 10;
            var whitePrice1 = 9;
            var whitePrice2 = 10;
            var whitePrice3 = 11;
            var symbol = CreateAndMint(totalAmount, tokenId);
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

        private string CreateAndMint(long amount, long tokenId)
        {
            var createResult =
                _nftContract.ExecuteMethodWithResult(NftContractMethod.Create, new CreateInput
                {
                    NftType = NFTType.Collectables.ToString(),
                    ProtocolName = "CAT",
                    TotalSupply = 10000,
                    Creator = InitAccount.ConvertAddress(),
                    IsBurnable = true,
                    IssueChainId = _chainId,
                    Metadata = new Metadata(),
                    BaseUri = "aelf.com/nft/",
                    IsTokenIdReuse = true
                });
            createResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var symbol = StringValue.Parser.ParseFrom(ByteArrayHelper.HexStringToByteArray(createResult.ReturnValue))
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
                    Quantity = amount,
                    TokenId = tokenId
                });
            mintResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            var initAccountAfterBalance = GetBalanceTest(InitAccount, symbol, tokenId);
            Logger.Info($"initAccountAfterBalance is {initAccountAfterBalance}");
            initAccountAfterBalance.ShouldBe(amount + initBalanceBefore);

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
    }
}