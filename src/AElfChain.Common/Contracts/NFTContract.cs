using System.Collections.Generic;
using AElf.Client.Dto;
using AElf.Contracts.NFT;
using AElf.Standards.ACS10;
using AElf.Types;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Managers;
using Google.Protobuf.WellKnownTypes;
using log4net.Config;

namespace AElfChain.Common.Contracts
{
    public enum NFTMethod
    {
        Create,
        CrossChainCreate,
        Mint,
        Transfer,
        TransferFrom,
        Approve,
        UnApprove,
        Burn,
        ApproveProtocol,

        //Lock several nfts and fts to mint one nft.
        Assemble,
        Disassemble,
        Recast,

        AddMinters,
        RemoveMiners,
        AddNFTType,
        RemoveNFTType,

        GetNFTProtocolInfo,
        GetNFTInfo,
        GetNFTInfoByTokenHash,
        GetBalance,
        GetBalanceByTokenHash,
        GetAllowance,
        GetAllowanceByTokenHash,
        GetMinterList,
        CalculateTokenHash,
        GetNFTTypes,
        GetOperatorList
    }

    public class NFTContract : BaseContract<NFTMethod>
    {
        public NFTContract(INodeManager nodeManager, string callAddress) : base(nodeManager,
            "AElf.Contracts.NFT", callAddress)
        {
        }

        public NFTContract(INodeManager nodeManager, string contractAddress, string callAddress) : base(nodeManager,
            contractAddress)
        {
            SetAccount(callAddress);
        }

        public Hash CalculateTokenHash(string symbol, long tokenId)
        {
            return CallViewMethod<Hash>(NFTMethod.CalculateTokenHash,
                new CalculateTokenHashInput
                {
                    Symbol = symbol,
                    TokenId = tokenId
                });
        }

        public NFTProtocolInfo GetNFTProtocolInfo(string symbol)
        {
            return CallViewMethod<NFTProtocolInfo>(NFTMethod.GetNFTProtocolInfo, new StringValue {Value = symbol});
        }

        public NFTInfo GetNFTInfoByTokenHash(Hash input)
        {
            return CallViewMethod<NFTInfo>(NFTMethod.GetNFTInfoByTokenHash, input);
        }

        public MinterList GetMinterList(string symbol)
        {
            return CallViewMethod<MinterList>(NFTMethod.GetMinterList, new StringValue {Value = symbol});
        }

        public GetBalanceOutput GetBalance(string symbol, long tokenId, string owner)
        {
            return CallViewMethod<GetBalanceOutput>(NFTMethod.GetBalance, new GetBalanceInput
            {
                Owner = owner.ConvertAddress(),
                Symbol = symbol,
                TokenId = tokenId
            });
        }

        public GetAllowanceOutput GetAllowance(string symbol, long tokenId, string owner, string spender)
        {
            return CallViewMethod<GetAllowanceOutput>(NFTMethod.GetAllowance, new GetAllowanceInput
            {
                Owner = owner.ConvertAddress(),
                Symbol = symbol,
                Spender = spender.ConvertAddress(),
                TokenId = tokenId
            });
        }

        public AddressList GetOperatorList(string symbol, string owner)
        {
            return CallViewMethod<AddressList>(NFTMethod.GetOperatorList, new GetOperatorListInput
            {
                Owner = owner.ConvertAddress(),
                Symbol = symbol
            });
        }

        public NFTInfo GetNFTInfo(string symbol, long tokenId)
        {
            return CallViewMethod<NFTInfo>(NFTMethod.GetNFTInfo, new GetNFTInfoInput
            {
                Symbol = symbol,
                TokenId = tokenId
            });
        }

        public NFTTypes GetNFTTypes()
        {
            return CallViewMethod<NFTTypes>(NFTMethod.GetNFTTypes, new Empty());
        }

        public TransactionResultDto Mint(string owner, long quantity, string alias, string symbol, long tokenId)
        {
            var result = ExecuteMethodWithResult(NFTMethod.Mint, new MintInput
            {
                Owner = owner.ConvertAddress(),
                Quantity = quantity,
                Alias = alias,
                Metadata = new Metadata
                {
                    Value =
                    {
                        {"Description", ""}
                    }
                },
                Symbol = symbol,
                TokenId = tokenId,
                Uri = ""
            });
            return result;
        }

        public TransactionResultDto Assemble(string owner, string alias, string symbol, long tokenId,
            Dictionary<string, long> nft, Dictionary<string, long> fts, string description)
        {
            var result = ExecuteMethodWithResult(NFTMethod.Assemble, new AssembleInput
            {
                Owner = owner.ConvertAddress(),
                Alias = alias,
                Metadata = new Metadata
                {
                    Value =
                    {
                        {"Description", description}
                    }
                },
                Symbol = symbol,
                TokenId = tokenId,
                Uri = "",
                AssembledFts = new AssembledFts
                {
                    Value = {fts}
                },
                AssembledNfts = new AssembledNfts
                {
                    Value = {nft}
                }
            });
            return result;
        }

        public TransactionResultDto Disassemble(string symbol, long tokenId, string owner)
        {
            var result = ExecuteMethodWithResult(NFTMethod.Disassemble, new DisassembleInput
            {
                Symbol = symbol,
                Owner = owner.ConvertAddress(),
                TokenId = tokenId
            });
            return result;
        }

        public TransactionResultDto TransferNftToken(long amount, long tokenId, string symbol, string toAccount)
        {
            var result = ExecuteMethodWithResult(NFTMethod.Transfer,
                new TransferInput
                {
                    Amount = amount,
                    Symbol = symbol,
                    TokenId = tokenId,
                    To = toAccount.ConvertAddress()
                });
            return result;
        }

        public TransactionResultDto ApproveProtocol(string symbol, Address operatorAddress, bool approved)
        {
            var result = ExecuteMethodWithResult(NFTMethod.ApproveProtocol,
                new ApproveProtocolInput
                {
                    Operator = operatorAddress,
                    Symbol = symbol,
                    Approved = approved
                });
            return result;
        }

        public TransactionResultDto Recast(string symbol, long tokenId, string alias, string uri, Metadata metadata)
        {
            var result = ExecuteMethodWithResult(NFTMethod.Recast,
                new RecastInput
                {
                    Symbol = symbol,
                    TokenId = tokenId,
                    Alias = alias,
                    Uri = uri,
                    Metadata = metadata
                });
            return result;
        }
    }
}