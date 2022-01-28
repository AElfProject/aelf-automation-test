/*using System;
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
    public class NFTMarketContractTest2
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
        private string OtherAccount { get; } = "YUW9zH5GhRboT5JK4vXp5BLAfCDv28rRmTQwo418FuaJmkSg8";
        private string WhiteListAddress1 { get; } = "sjzNpr5bku3ZyvMqQrXeBkXGEvG2CTLA2cuNDfcDMaPTTAqEy";
        private string WhiteListAddress2 { get; } = "2bs2uYMECtHWjB57RqgqQ3X2LrxgptWHtzCqGEU11y45aWimh4";
        //private string WhiteListAddress3 { get; } = "YUW9zH5GhRboT5JK4vXp5BLAfCDv28rRmTQwo418FuaJmkSg8";
        private string serviceAddress { get; } = "28Y8JA1i2cN6oHvdv7EraXJr9a1gY6D1PpJXw9QtRMRwKcBQMK";

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
            long whitePrice1, long whitePrice2, long whitPrice3)
        {
            var symbol = CreateAndMint(totalAmount, tokenId);
            var startTime = DateTime.UtcNow.ToTimestamp();
            var publicTime = DateTime.UtcNow.AddSeconds(20).ToTimestamp();
            var durationHours = 1;
            var protocolInfo = _nftContract.GetNftProtocolInfo(symbol);
            Logger.Info($"protocolInfo.Symbol is {protocolInfo.Symbol}");

            // Initialize
            var initialize = _nftMarketContract.Initialize(
                _nftContract.ContractAddress,
                InitAccount,
                10,
                serviceAddress,
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
                            Address =WhiteListAddress1.ConvertAddress(),
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
            var listedNftInfo =
                _nftMarketContract.GetListedNFTInfoList(symbol, tokenId, InitAccount).Value[0];
            Logger.Info($"WhiteListAddress1Balance is {listedNftInfo}");
            var whiteListAddressPriceList =
                _nftMarketContract.GetWhiteListAddressPriceList(symbol, tokenId, InitAccount);
            whiteListAddressPriceList.Value.Count.ShouldBe(1);
            whiteListAddressPriceList.Value[0].Address.ShouldBe(WhiteListAddress1.ConvertAddress());
            whiteListAddressPriceList.Value[0].Price.Symbol.ShouldBe("ELF");
            whiteListAddressPriceList.Value[0].Price.Amount.ShouldBe(9_00000000);


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
            var fixedPrice = 12_00000000;
            var whitePrice1 = 9_00000000;
            var whitePrice2 = 10_00000000;
            var whitePrice3 = 11_00000000;
            var symbol = CreateAndMint(totalAmount, tokenId);
            var startTime = DateTime.UtcNow.AddSeconds(10).ToTimestamp();
            var publicTime = DateTime.UtcNow.AddSeconds(30).ToTimestamp();
            var durationHours = 1;
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
        
         [TestMethod]
        //ListWithFixedPrice
        //WhiteListAddress
        public void MakeOfferTest()
        {
            var tokenId = 1;
            var totalAmount = 1000;
            var sellAmount = 100;
            var fixedPrice = 12_00000000;
            var whitePrice1 = 9_00000000;
            var whitePrice2 = 10_00000000;
            var whitePrice3 = 11_00000000;
            var buyAmount =1;
            var expireTime = DateTime.UtcNow.AddSeconds(30).ToTimestamp();
            
            var symbol = ListWithFixedPriceTest(tokenId,totalAmount,sellAmount,fixedPrice,whitePrice1,whitePrice2,whitePrice3);
            
            /* 
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
  
                   
                   //3.购买的价格<设置白名单的价格
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
              
         
                
                
              
              //4.一口价   <  设置白名单的价< = 格购买的价格
              
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
              
            
             
              //5.一口价 < = 格购买的价格  <  设置白名单的价
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
  
              
              
            
          //6.购买的价格< 设置白名单的价格,public_time< 当前时间 
  
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
          
          result.Error.ShouldContain("Cannot find correct listed nft info.");
          
          
        
          
       
           //7.置白名单的价格 < =  购买的价格 < 一口价设  ,  public_time  < 当前时间  
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
           
           
           
         
            
        //8.设置白名单的价格<  一口价 < = 购买的价格,public_time  <  当前时间  
        
        
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
        
        
        
        
      //9.1 DurationHours <  当前时间  ;  DurationHours <  当前时间  ;   一口价  < = 购买的价格  < 设置白名单的价格, 
      
          //var tokenId7 = 7;
           //var dueTime7 = DateTime.UtcNow.AddSeconds(10).ToTimestamp();
           Thread.Sleep(60 * 1000);
           //var OfferList = _nftMarketContract.GetOfferList(symbol, tokenId, InitAccount);
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
  
         
            
            
                    
                    
                    
           //9.2 DurationHours <  当前时间  ;DurationHours <  当前时间  ;  一口价  < = 购买的价格  < 设置白名单的价格
           //var tokenId8 = 8;
           //var dueTime8 = DateTime.UtcNow.AddSeconds(10).ToTimestamp();
           
           var OfferList = _nftMarketContract.GetOfferList(symbol, tokenId, InitAccount);
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
           
           OfferList = _nftMarketContract.GetOfferList(symbol, tokenId, InitAccount);
           OfferList.Value.Count.ShouldBe(1);
           OfferList.Value.First().From.ShouldBe(OtherAccount.ConvertAddress());
           OfferList.Value.First().To.ShouldBe(InitAccount.ConvertAddress());
           OfferList.Value.First().Price.Symbol.ShouldBe("ELF");
           OfferList.Value.First().Price.Amount.ShouldBe(whitePrice1);
           OfferList.Value.First().Quantity.ShouldBe(buyAmount);
           OfferList.Value.First().ExpireTime.ShouldBe(expireTime);
        
           
            
            //9.3.owner——start_time < 当前时间 <public_time,一口价 < = 购买的价格 

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

         
         #1#
         
         
            
            
         //users
         // Approve
         _nftContract.SetAccount(OtherAccount);
         var approve = _tokenContract.ApproveToken(OtherAccount, _nftMarketContract.ContractAddress,
             10000000000_00000000, "ELF");
         approve.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
         
           /* 
            //10.start_time < 当前时间 <public_time,一口价 < = 购买的价格 

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
        


           //11.tart_time < 当前时间 <public_time, 购买的价格 < 一口价 

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
           


           //12.设置白名单的价格 < = 购买的价格 < 一口价   ;   public_time  <  当前时间  
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
          
           
           
           
           
          
           //13.设置白名单的价格 < = 购买的价格 < 一口价   ;   public_time  <  当前时间  
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
           
           #1#
        
          
            //14.设置白名单的价格 < = 购买的价格 < 一口价   ;   DurationHours <  当前时间  
           //var tokenId12 = 10;
           //var dueTime12 = DateTime.UtcNow.AddSeconds(10).ToTimestamp();
           
           var OfferList = _nftMarketContract.GetOfferList(symbol, tokenId, InitAccount);
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
                   Amount = fixedPrice+10_00000000
               },
               expireTime
           );


           /*
           OfferList = _nftMarketContract.GetOfferList(symbol, tokenId, InitAccount);
           OfferList.Value.Count.ShouldBe(1);
           OfferList.Value.First().From.ShouldBe(OtherAccount.ConvertAddress());
           OfferList.Value.First().To.ShouldBe(InitAccount.ConvertAddress());
           OfferList.Value.First().Price.Symbol.ShouldBe("ELF");
           OfferList.Value.First().Price.Amount.ShouldBe(fixedPrice + 10_00000000);
           OfferList.Value.First().Quantity.ShouldBe(buyAmount);
           OfferList.Value.First().ExpireTime.ShouldBe(expireTime);
           #1#


        }
        
         //ListWithEnglistAuction


         public void MakeOfferListWithEnglishTest1()
         {
             var tokenId = 1;
             var totalAmount = 1000;
             var sellAmount = 100;
             var fixedPrice = 12_00000000;
             var whitePrice1 = 9_00000000;
             var whitePrice2 = 10_00000000;
             var whitePrice3 = 11_00000000;
             var buyAmount = 1;
             var expireTime = DateTime.UtcNow.AddSeconds(30).ToTimestamp();

             var symbol = ListWithEnglishAuctionTest(tokenId, totalAmount, sellAmount, fixedPrice, whitePrice1, whitePrice2,
                 whitePrice3);

         }



    }
}*/