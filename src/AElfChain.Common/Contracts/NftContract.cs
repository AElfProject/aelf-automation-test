using System.Collections.Generic;
using AElf.Client.Dto;
using AElf.Contracts.NFT;
using AElf.Types;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Managers;
using Google.Protobuf.WellKnownTypes;

namespace AElfChain.Common.Contracts
{
    public enum NftContractMethod
    {
        //Action
        Create,
        CrossChainCreate,
        Mint,
        Transfer,
        TransferFrom,
        Approve,
        UnApprove,
        ApproveProtocol,
        Burn,
        Assemble,
        Disassemble,
        Recast,
        AddMinters,
        RemoveMinters,
        AddNFTType,
        RemoveNFTType,

        //View
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

    public class NftContract : BaseContract<NftContractMethod>
    {
        public NftContract(INodeManager nm, string account) :
            base(nm, ContractFileName, account)
        {
        }

        public NftContract(INodeManager nm, string callAddress, string contractAbi) :
            base(nm, contractAbi)
        {
            SetAccount(callAddress);
        }

        public static string ContractFileName => "AElf.Contracts.NFT";

        public TransactionResultDto CrossChainCreate(string symbol)
        {
            return ExecuteMethodWithResult(NftContractMethod.CrossChainCreate, new CrossChainCreateInput
            {
                Symbol = symbol
            });
        }

        public TransactionResultDto Approve(string spender, string symbol, long tokenId, long amount)
        {
            return ExecuteMethodWithResult(NftContractMethod.Approve, new ApproveInput
            {
                Spender = spender.ConvertAddress(),
                Symbol = symbol,
                TokenId = tokenId,
                Amount = amount
            });
        }

        public TransactionResultDto UnApprove(string spender, string symbol, long tokenId, long amount)
        {
            return ExecuteMethodWithResult(NftContractMethod.UnApprove, new UnApproveInput
            {
                Spender = spender.ConvertAddress(),
                Symbol = symbol,
                TokenId = tokenId,
                Amount = amount
            });
        }

        public TransactionResultDto ApproveProtocol(string operatorAddress, string symbol, bool approved)
        {
            return ExecuteMethodWithResult(NftContractMethod.ApproveProtocol, new ApproveProtocolInput
            {
                Operator = operatorAddress.ConvertAddress(),
                Symbol = symbol,
                Approved = approved
            });
        }

        public TransactionResultDto Burn(string symbol, long tokenId, long amount)
        {
            return ExecuteMethodWithResult(NftContractMethod.Burn, new BurnInput
            {
                Symbol = symbol,
                TokenId = tokenId,
                Amount = amount
            });
        }

        public TransactionResultDto Disassemble(string symbol, long tokenId, string owner = null)
        {
            if (owner == null)
                owner = CallAddress;
            var result = ExecuteMethodWithResult(NftContractMethod.Disassemble, new DisassembleInput
            {
                Symbol = symbol,
                Owner = owner.ConvertAddress(),
                TokenId = tokenId
            });
            return result;
        }

        public TransactionResultDto AddMinters(MinterList minterList, string symbol)
        {
            return ExecuteMethodWithResult(NftContractMethod.AddMinters, new AddMintersInput
            {
                MinterList = minterList,
                Symbol = symbol
            });
        }

        public TransactionResultDto RemoveMinters(MinterList minterList, string symbol)
        {
            return ExecuteMethodWithResult(NftContractMethod.RemoveMinters, new RemoveMintersInput
            {
                MinterList = minterList,
                Symbol = symbol
            });
        }

        public TransactionResultDto AddNftType(string fullName, string shortName)
        {
            return ExecuteMethodWithResult(NftContractMethod.AddNFTType, new AddNFTTypeInput
            {
                FullName = fullName,
                ShortName = shortName
            });
        }

        public TransactionResultDto RemoveNftType(string shortName)
        {
            return ExecuteMethodWithResult(NftContractMethod.RemoveNFTType, new StringValue {Value = shortName});
        }

        public NFTProtocolInfo GetNftProtocolInfo(string symbol)
        {
            return CallViewMethod<NFTProtocolInfo>(NftContractMethod.GetNFTProtocolInfo,
                new StringValue {Value = symbol});
        }

        public NFTInfo GetNftInfo(string symbol, long tokenId)
        {
            return CallViewMethod<NFTInfo>(NftContractMethod.GetNFTInfo,
                new GetNFTInfoInput
                {
                    Symbol = symbol,
                    TokenId = tokenId
                });
        }

        public NFTInfo GetNftInfoByTokenHash(Hash tokenHash)
        {
            return CallViewMethod<NFTInfo>(NftContractMethod.GetNFTInfoByTokenHash, tokenHash);
        }

        public GetBalanceOutput GetBalance(string owner, string symbol, long tokenId)
        {
            return CallViewMethod<GetBalanceOutput>(NftContractMethod.GetBalance,
                new GetBalanceInput
                {
                    Owner = owner.ConvertAddress(),
                    Symbol = symbol,
                    TokenId = tokenId
                });
        }

        public GetBalanceOutput GetBalanceByTokenHash(string owner, Hash tokenHash)
        {
            return CallViewMethod<GetBalanceOutput>(NftContractMethod.GetBalanceByTokenHash,
                new GetBalanceByTokenHashInput
                {
                    Owner = owner.ConvertAddress(),
                    TokenHash = tokenHash
                });
        }

        public GetAllowanceOutput GetAllowance(string symbol, long tokenId, string owner, string spender)
        {
            return CallViewMethod<GetAllowanceOutput>(NftContractMethod.GetAllowance,
                new GetAllowanceInput
                {
                    Symbol = symbol,
                    TokenId = tokenId,
                    Owner = owner.ConvertAddress(),
                    Spender = spender.ConvertAddress()
                });
        }

        public GetAllowanceOutput GetAllowanceByTokenHash(Hash tokenHash, string owner, string spender)
        {
            return CallViewMethod<GetAllowanceOutput>(NftContractMethod.GetAllowanceByTokenHash,
                new GetAllowanceByTokenHashInput
                {
                    TokenHash = tokenHash,
                    Owner = owner.ConvertAddress(),
                    Spender = spender.ConvertAddress()
                });
        }

        public MinterList GetMinterList(string symbol)
        {
            return CallViewMethod<MinterList>(NftContractMethod.GetMinterList, new StringValue {Value = symbol});
        }

        public Hash CalculateTokenHash(string symbol, long tokenId)
        {
            return CallViewMethod<Hash>(NftContractMethod.CalculateTokenHash,
                new CalculateTokenHashInput
                {
                    Symbol = symbol,
                    TokenId = tokenId
                });
        }

        public NFTTypes GetNftTypes()
        {
            return CallViewMethod<NFTTypes>(NftContractMethod.GetNFTTypes, new Empty());
        }

        public AddressList GetOperatorList(string symbol, string owner)
        {
            return CallViewMethod<AddressList>(NftContractMethod.GetOperatorList, new GetOperatorListInput
            {
                Symbol = symbol,
                Owner = owner.ConvertAddress()
            });
        }
        
        public TransactionResultDto Mint(string owner, long quantity, string alias, string symbol, long tokenId)
        {
            var result = ExecuteMethodWithResult(NftContractMethod.Mint, new MintInput
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
        
        public TransactionResultDto Recast(string symbol, long tokenId, string alias, string uri, Metadata metadata)
        {
            var result = ExecuteMethodWithResult(NftContractMethod.Recast,
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
        
        public TransactionResultDto Assemble(string owner, string alias, string symbol, long tokenId,
            Dictionary<string, long> nft, Dictionary<string, long> fts, string description)
        {
            var result = ExecuteMethodWithResult(NftContractMethod.Assemble, new AssembleInput
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
        
        public TransactionResultDto TransferNftToken(long amount, long tokenId, string symbol, string toAccount)
        {
            var result = ExecuteMethodWithResult(NftContractMethod.Transfer,
                new TransferInput
                {
                    Amount = amount,
                    Symbol = symbol,
                    TokenId = tokenId,
                    To = toAccount.ConvertAddress()
                });
            return result;
        }
    }
}