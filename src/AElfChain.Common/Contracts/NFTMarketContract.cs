using AElf.Client.Dto;
using AElf.Client.Proto;
using AElf.Contracts.MultiToken;
using AElf.Contracts.NFTMarket;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Managers;
using Google.Protobuf.WellKnownTypes;
using AddressList = AElf.Contracts.NFTMarket.AddressList;
using StringList = AElf.Contracts.NFTMarket.StringList;

namespace AElfChain.Common.Contracts
{
    public enum NFTMarketContractMethod
    {
        Initialize,

        // For Sellers
        ListWithFixedPrice,
        ListWithEnglishAuction,
        ListWithDutchAuction,
        Deal,
        Delist,

        // For Buyers
        MakeOffer,
        CancelOffer,

        // For Creators
        SetRoyalty,
        SetTokenWhiteList,
        SetCustomizeInfo,
        StakeForRequests,
        WithdrawStakingTokens,
        HandleRequest,

        // For Admin
        SetServiceFee,
        SetGlobalTokenWhiteList,

        //View
        GetListedNFTInfoList,
        GetWhiteListAddressPriceList,
        GetOfferAddressList,
        GetOfferList,
        GetBidAddressList,
        GetBidList,
        GetCustomizeInfo,
        GetRequestInfo,
        GetEnglishAuctionInfo,
        GetDutchAuctionInfo,
        GetTokenWhiteList,
        GetGlobalTokenWhiteList,
        GetStakingTokens,
        GetRoyalty
    }

    public class NFTMarketContract : BaseContract<NFTMarketContractMethod>
    {
        public NFTMarketContract(INodeManager nm, string account) :
            base(nm, ContractFileName, account)
        {
        }

        public NFTMarketContract(INodeManager nm, string callAddress, string contractAbi) :
            base(nm, contractAbi)
        {
            SetAccount(callAddress);
        }

        public static string ContractFileName => "AElf.Contracts.NFTMarket";

        public TransactionResultDto Initialize(string nftContractAddress, string adminAdress, int SetServiceFeeRate,
            string serviceFeeReceiver, long serviceFee)
        {
            return ExecuteMethodWithResult(NFTMarketContractMethod.Initialize, new InitializeInput
            {
                NftContractAddress = nftContractAddress.ConvertAddress(),
                AdminAddress = adminAdress.ConvertAddress(),
                ServiceFeeRate = SetServiceFeeRate,
                ServiceFeeReceiver = serviceFeeReceiver.ConvertAddress(),
                ServiceFee = serviceFee
            });
        }

        public TransactionResultDto ListWithFixedPrice(string symbol, long tokenId, Price price, long quantity,
            ListDuration duration, WhiteListAddressPriceList whiteListAddressPriceList,
            bool isMergeToPreviousListedInfo)
        {
            return ExecuteMethodWithResult(NFTMarketContractMethod.ListWithFixedPrice, new ListWithFixedPriceInput
            {
                Symbol = symbol,
                TokenId = tokenId,
                Price = price,
                Quantity = quantity,
                Duration = duration,
                WhiteListAddressPriceList = whiteListAddressPriceList,
                IsMergeToPreviousListedInfo = isMergeToPreviousListedInfo
            });
        }

        public TransactionResultDto ListWithEnglishAuction(string symbol, long tokenId, long startingPrice,
            string purchaseSymbol,
            ListDuration duration, long earnestMoney, WhiteListAddressPriceList whiteListAddressPriceList)
        {
            return ExecuteMethodWithResult(NFTMarketContractMethod.ListWithEnglishAuction,
                new ListWithEnglishAuctionInput
                {
                    Symbol = symbol,
                    TokenId = tokenId,
                    StartingPrice = startingPrice,
                    PurchaseSymbol = purchaseSymbol,
                    Duration = duration,
                    EarnestMoney = earnestMoney,
                    WhiteListAddressPriceList = whiteListAddressPriceList
                });
        }

        public TransactionResultDto ListWithDutchAuction(string symbol, long tokenId, long startingPrice,
            long endingPrice, string purchaseSymbol,
            ListDuration duration, WhiteListAddressPriceList whiteListAddressPriceList)
        {
            return ExecuteMethodWithResult(NFTMarketContractMethod.ListWithDutchAuction, new ListWithDutchAuctionInput
            {
                Symbol = symbol,
                TokenId = tokenId,
                StartingPrice = startingPrice,
                EndingPrice = endingPrice,
                PurchaseSymbol = purchaseSymbol,
                Duration = duration,
                WhiteListAddressPriceList = whiteListAddressPriceList
            });
        }

        public TransactionResultDto Deal(string symbol, long tokenId, string offerFrom, Price price, long quantity)
        {
            return ExecuteMethodWithResult(NFTMarketContractMethod.Deal, new DealInput
            {
                Symbol = symbol,
                TokenId = tokenId,
                OfferFrom = offerFrom.ConvertAddress(),
                Price = price,
                Quantity = quantity
            });
        }

        public TransactionResultDto Delist(string symbol, long tokenId, long quantity)
        {
            return ExecuteMethodWithResult(NFTMarketContractMethod.Delist, new DelistInput
            {
                Symbol = symbol,
                TokenId = tokenId,
                Quantity = quantity
            });
        }

        public TransactionResultDto MakeOffer(string symbol, long tokenId, string offerTo, long quantity, Price price,
            Timestamp expireTime)
        {
            return ExecuteMethodWithResult(NFTMarketContractMethod.MakeOffer, new MakeOfferInput
            {
                Symbol = symbol,
                TokenId = tokenId,
                OfferTo = offerTo.ConvertAddress(),
                Quantity = quantity,
                Price = price,
                ExpireTime = expireTime,
            });
        }

        public TransactionResultDto CancelOffer(string symbol, long tokenId, Int32List indexList, string offerFrom,
            string offerTo)
        {
            return ExecuteMethodWithResult(NFTMarketContractMethod.CancelOffer, new CancelOfferInput
            {
                Symbol = symbol,
                TokenId = tokenId,
                IndexList = indexList,
                OfferFrom = offerFrom.ConvertAddress(),
                OfferTo = offerTo.ConvertAddress()
            });
        }

        public TransactionResultDto SetRoyalty(string symbol, long tokenId, int royalty, string royaltyFeeReceiver)
        {
            return ExecuteMethodWithResult(NFTMarketContractMethod.SetRoyalty, new SetRoyaltyInput
            {
                Symbol = symbol,
                TokenId = tokenId,
                Royalty = royalty,
                RoyaltyFeeReceiver = royaltyFeeReceiver.ConvertAddress()
            });
        }

        public TransactionResultDto SetTokenWhiteList(string symbol, StringList tokenWhiteList)
        {
            return ExecuteMethodWithResult(NFTMarketContractMethod.SetTokenWhiteList, new SetTokenWhiteListInput
            {
                Symbol = symbol,
                TokenWhiteList = tokenWhiteList
            });
        }

        public TransactionResultDto SetCustomizeInfo(string symbol, int depositRate, Price price, long workHours,
            long whiteListHours, long stakingAmount)
        {
            return ExecuteMethodWithResult(NFTMarketContractMethod.SetCustomizeInfo, new CustomizeInfo
            {
                Symbol = symbol,
                DepositRate = depositRate,
                Price = price,
                WorkHours = workHours,
                WhiteListHours = whiteListHours,
                StakingAmount = stakingAmount
            });
        }

        public TransactionResultDto StakeForRequests(string symbol, long stakingAmount)
        {
            return ExecuteMethodWithResult(NFTMarketContractMethod.StakeForRequests, new StakeForRequestsInput
            {
                Symbol = symbol,
                StakingAmount = stakingAmount
            });
        }

        public TransactionResultDto WithdrawStakingTokens(string symbol, long withdrawAmount)
        {
            return ExecuteMethodWithResult(NFTMarketContractMethod.WithdrawStakingTokens, new WithdrawStakingTokensInput
            {
                Symbol = symbol,
                WithdrawAmount = withdrawAmount
            });
        }

        public TransactionResultDto HandleRequest(string symbol, long tokenId, string requester, bool isConfirm)
        {
            return ExecuteMethodWithResult(NFTMarketContractMethod.HandleRequest, new HandleRequestInput
            {
                Symbol = symbol,
                TokenId = tokenId,
                Requester = requester.ConvertAddress(),
                IsConfirm = isConfirm
            });
        }

        public TransactionResultDto SetServiceFee(int serviceFeeRate, string serviceFeeReceiver)
        {
            return ExecuteMethodWithResult(NFTMarketContractMethod.SetServiceFee, new SetServiceFeeInput
            {
                ServiceFeeRate = serviceFeeRate,
                ServiceFeeReceiver = serviceFeeReceiver.ConvertAddress()
            });
        }

        public TransactionResultDto SetGlobalTokenWhiteList(StringList globalTokenWhiteList)
        {
            return ExecuteMethodWithResult(NFTMarketContractMethod.SetGlobalTokenWhiteList, globalTokenWhiteList);
        }

        public ListedNFTInfoList GetListedNFTInfoList(string symbol, long tokenId, string owner)
        {
            return CallViewMethod<ListedNFTInfoList>(NFTMarketContractMethod.GetListedNFTInfoList,
                new GetListedNFTInfoListInput
                {
                    Symbol = symbol,
                    TokenId = tokenId,
                    Owner = owner.ConvertAddress()
                });
        }

        public WhiteListAddressPriceList GetWhiteListAddressPriceList(string symbol, long tokenId, string owner)
        {
            return CallViewMethod<WhiteListAddressPriceList>(NFTMarketContractMethod.GetWhiteListAddressPriceList,
                new GetWhiteListAddressPriceListInput
                {
                    Symbol = symbol,
                    TokenId = tokenId,
                    Owner = owner.ConvertAddress()
                });
        }

        public AddressList GetOfferAddressList(string symbol, long tokenId)
        {
            return CallViewMethod<AddressList>(NFTMarketContractMethod.GetOfferAddressList,
                new GetAddressListInput
                {
                    Symbol = symbol,
                    TokenId = tokenId
                });
        }

        public OfferList GetOfferList(string symbol, long tokenId, string address)
        {
            return CallViewMethod<OfferList>(NFTMarketContractMethod.GetOfferList, new GetOfferListInput
            {
                Symbol = symbol,
                TokenId = tokenId,
                Address = address.ConvertAddress()
            });
        }

        public AddressList GetBidAddressList(string symbol, long tokenId)
        {
            return CallViewMethod<AddressList>(NFTMarketContractMethod.GetBidAddressList,
                new GetAddressListInput
                {
                    Symbol = symbol,
                    TokenId = tokenId
                });
        }

        public OfferList GetBidList(string symbol, long tokenId, string address)
        {
            return CallViewMethod<OfferList>(NFTMarketContractMethod.GetBidList, new GetOfferListInput
            {
                Symbol = symbol,
                TokenId = tokenId,
                Address = address.ConvertAddress()
            });
        }

        public CustomizeInfo GetCustomizeInfo(string symbol)
        {
            return CallViewMethod<CustomizeInfo>(NFTMarketContractMethod.GetCustomizeInfo,
                new StringValue {Value = symbol});
        }

        public RequestInfo GetRequestInfo(string symbol, long tokenId)
        {
            return CallViewMethod<RequestInfo>(NFTMarketContractMethod.GetRequestInfo, new GetRequestInfoInput
            {
                Symbol = symbol,
                TokenId = tokenId
            });
        }

        public EnglishAuctionInfo GetEnglishAuctionInfo(string symbol, long tokenId)
        {
            return CallViewMethod<EnglishAuctionInfo>(NFTMarketContractMethod.GetEnglishAuctionInfo,
                new GetEnglishAuctionInfoInput
                {
                    Symbol = symbol,
                    TokenId = tokenId
                });
        }

        public DutchAuctionInfo GetDutchAuctionInfo(string symbol, long tokenId)
        {
            return CallViewMethod<DutchAuctionInfo>(NFTMarketContractMethod.GetDutchAuctionInfo,
                new GetDutchAuctionInfoInput
                {
                    Symbol = symbol,
                    TokenId = tokenId
                });
        }

        public StringList GetTokenWhiteList(string symbol)
        {
            return CallViewMethod<StringList>(NFTMarketContractMethod.GetTokenWhiteList,
                new StringValue {Value = symbol});
        }

        public StringList GetGlobalTokenWhiteList()
        {
            return CallViewMethod<StringList>(NFTMarketContractMethod.GetGlobalTokenWhiteList, new Empty());
        }

        public Price GetStakingTokens(string symbol)
        {
            return CallViewMethod<Price>(NFTMarketContractMethod.GetStakingTokens, new StringValue {Value = symbol});
        }

        public RoyaltyInfo GetRoyalty(string symbol, long tokenId)
        {
            return CallViewMethod<RoyaltyInfo>(NFTMarketContractMethod.GetRoyalty,
                new GetRoyaltyInput
                {
                    Symbol = symbol,
                    TokenId = tokenId
                });
        }
    }
}