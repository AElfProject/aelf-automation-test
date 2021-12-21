using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AElf.Contracts.MultiToken;
using AElf.Contracts.NFT;
using AElf.Types;
using AElfChain.Common;
using AElfChain.Common.Contracts;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using log4net;
using Microsoft.VisualBasic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using CreateInput = AElf.Contracts.NFT.CreateInput;
using TransferFromInput = AElf.Contracts.NFT.TransferFromInput;
using TransferInput = AElf.Contracts.NFT.TransferInput;

namespace AElf.Automation.Contracts.ScenarioTest
{
    [TestClass]
    public class NftContractTest
    {
        private ILog Logger { get; set; }
        private INodeManager NodeManager { get; set; }
        private int _chainId;
        private GenesisContract _genesisContract;
        private TokenContract _tokenContract;
        private NftContract _nftContract;

        private string InitAccount { get; } = "J6zgLjGwd1bxTBpULLXrGVeV74tnS2n74FFJJz7KNdjTYkDF6";
        private string OtherAccount { get; } = "2bs2uYMECtHWjB57RqgqQ3X2LrxgptWHtzCqGEU11y45aWimh4";
        private string OtherAccount1 { get; } = "YUW9zH5GhRboT5JK4vXp5BLAfCDv28rRmTQwo418FuaJmkSg8";

        private static string RpcUrl { get; } = "192.168.66.9:8000";
        private AuthorityManager AuthorityManager { get; set; }

        private string Nft = "Mzq52XLHUsWtQsdCGaSMrNHDcbQhMqDYtyDvPqvztQn9ad9cX";

        [TestInitialize]
        public void Initialize()
        {
            Log4NetHelper.LogInit("NftContractTest");
            Logger = Log4NetHelper.GetLogger();
            NodeInfoHelper.SetConfig("nodes-env2-main");

            NodeManager = new NodeManager(RpcUrl);
            AuthorityManager = new AuthorityManager(NodeManager, InitAccount);
            _chainId = ChainHelper.ConvertBase58ToChainId(NodeManager.GetChainId());

            if (Nft.Equals(""))
                _nftContract = new NftContract(NodeManager, InitAccount);
            else
                _nftContract = new NftContract(NodeManager, InitAccount, Nft);

            _genesisContract = GenesisContract.GetGenesisContract(NodeManager, InitAccount);
            _tokenContract = _genesisContract.GetTokenContract(InitAccount);

            AddWhiteList();
        }

        [TestMethod]
        public void AddAndRemoveNftTypeTest()
        {
            var nftTypesInit = _nftContract.GetNftTypes();

            foreach (var pair in nftTypesInit.Value)
            {
                Logger.Info($"{pair.Key}:{pair.Value}\n");
            }

            _nftContract.SetAccount(OtherAccount);
            var addResult = _nftContract.AddNFTType("ART", "AR");
            addResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
            addResult.Error.ShouldContain("No permission.");

            _nftContract.SetAccount(InitAccount);
            addResult = AuthorityManager.ExecuteTransactionWithRelease(_nftContract.ContractAddress,
                nameof(NftContractMethod.AddNFTType), new AddNFTTypeInput
                {
                    FullName = "ART",
                    ShortName = "A"
                }, InitAccount);
            addResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
            addResult.Error.ShouldContain("Incorrect short name.");

            addResult = AuthorityManager.ExecuteTransactionWithRelease(_nftContract.ContractAddress,
                nameof(NftContractMethod.AddNFTType), new AddNFTTypeInput
                {
                    FullName = "ART",
                    ShortName = "ARR"
                }, InitAccount);
            addResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
            addResult.Error.ShouldContain("Incorrect short name.");

            addResult = AuthorityManager.ExecuteTransactionWithRelease(_nftContract.ContractAddress,
                nameof(NftContractMethod.AddNFTType), new AddNFTTypeInput
                {
                    FullName = "ART",
                    ShortName = "AR"
                }, InitAccount);
            addResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
            addResult.Error.ShouldContain("Short name AR already exists.");

            addResult = AuthorityManager.ExecuteTransactionWithRelease(_nftContract.ContractAddress,
                nameof(NftContractMethod.AddNFTType), new AddNFTTypeInput
                {
                    FullName = "Art",
                    ShortName = "WH"
                }, InitAccount);
            addResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
            addResult.Error.ShouldContain("Full name ART already exists.");

            addResult = AuthorityManager.ExecuteTransactionWithRelease(_nftContract.ContractAddress,
                nameof(NftContractMethod.AddNFTType), new AddNFTTypeInput
                {
                    FullName = "BABY",
                    ShortName = "BY"
                }, InitAccount);
            addResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var nftTypes = _nftContract.GetNftTypes();
            nftTypes.Value["BY"].ShouldBe("BABY");

            // Remove
            _nftContract.SetAccount(OtherAccount);
            var removeResult = _nftContract.RemoveNftType("AR");
            removeResult.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            removeResult.Error.ShouldContain("No permission.");

            removeResult = AuthorityManager.ExecuteTransactionWithRelease(_nftContract.ContractAddress,
                nameof(NftContractMethod.RemoveNFTType), new StringValue {Value = "A"}, InitAccount);
            removeResult.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            removeResult.Error.ShouldContain("Incorrect short name.");

            removeResult = AuthorityManager.ExecuteTransactionWithRelease(_nftContract.ContractAddress,
                nameof(NftContractMethod.RemoveNFTType), new StringValue {Value = "ARR"}, InitAccount);
            removeResult.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            removeResult.Error.ShouldContain("Incorrect short name.");

            removeResult = AuthorityManager.ExecuteTransactionWithRelease(_nftContract.ContractAddress,
                nameof(NftContractMethod.RemoveNFTType), new StringValue {Value = "AA"}, InitAccount);
            removeResult.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            removeResult.Error.ShouldContain("Short name AA does not exist.");

            _nftContract.SetAccount(InitAccount);
            removeResult = AuthorityManager.ExecuteTransactionWithRelease(_nftContract.ContractAddress,
                nameof(NftContractMethod.RemoveNFTType), new StringValue {Value = "BY"}, InitAccount);
            removeResult.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);
            nftTypes = _nftContract.GetNftTypes();
            var sum = 0;
            foreach (var pair in nftTypes.Value)
            {
                if (pair.Key == "BY")
                {
                    sum = sum + 1;
                }
            }

            sum.ShouldBe(0);
        }

        [TestMethod]
        public void CreateTest()
        {
            var createResult =
                _nftContract.ExecuteMethodWithResult(NftContractMethod.Create, new CreateInput
                {
                    NftType = NFTType.Collectables,
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

            CheckTokenNftProtocolInfo(symbol,
                new TokenInfo
                {
                    Symbol = symbol,
                    TokenName = "CAT",
                    Supply = 0,
                    TotalSupply = 10000,
                    Decimals = 0,
                    Issuer = InitAccount.ConvertAddress(),
                    IsBurnable = true,
                    IssueChainId = _chainId,
                    Issued = 0
                },
                new NFTProtocolInfo
                {
                    Symbol = symbol,
                    Supply = 0,
                    TotalSupply = 10000,
                    Creator = InitAccount.ConvertAddress(),
                    BaseUri = "aelf.com/nft/",
                    IsBurnable = true,
                    IssueChainId = _chainId,
                    NftType = NFTType.Collectables.ToString(),
                    ProtocolName = "CAT",
                    IsTokenIdReuse = true
                }
            );

            createResult =
                _nftContract.ExecuteMethodWithResult(NftContractMethod.Create, new CreateInput
                {
                    NftType = NFTType.Any,
                    ProtocolName = "CAT",
                    TotalSupply = 10000,
                    Creator = InitAccount.ConvertAddress(),
                    IsBurnable = true,
                    IssueChainId = _chainId,
                    Metadata = new Metadata
                    {
                        Value =
                        {
                            {"others", "XXX"}
                        }
                    },
                    BaseUri = "aelf.com/nft/",
                    IsTokenIdReuse = true
                });
            createResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            symbol = StringValue.Parser.ParseFrom(ByteArrayHelper.HexStringToByteArray(createResult.ReturnValue))
                .Value;
            Logger.Info($"symbol is {symbol}");
            CheckTokenNftProtocolInfo(symbol,
                new TokenInfo
                {
                    Symbol = symbol,
                    TokenName = "CAT",
                    Supply = 0,
                    TotalSupply = 10000,
                    Decimals = 0,
                    Issuer = InitAccount.ConvertAddress(),
                    IsBurnable = true,
                    IssueChainId = _chainId,
                    Issued = 0,
                },
                new NFTProtocolInfo
                {
                    Symbol = symbol,
                    Supply = 0,
                    TotalSupply = 10000,
                    Creator = InitAccount.ConvertAddress(),
                    BaseUri = "aelf.com/nft/",
                    IsBurnable = true,
                    IssueChainId = _chainId,
                    NftType = NFTType.Any.ToString(),
                    ProtocolName = "CAT",
                    IsTokenIdReuse = true,
                }
            );

            createResult =
                _nftContract.ExecuteMethodWithResult(NftContractMethod.Create, new CreateInput
                {
                    NftType = NFTType.Collectables,
                    ProtocolName = "CAT",
                    TotalSupply = 10000,
                    Creator = InitAccount.ConvertAddress(),
                    IsBurnable = true,
                    IssueChainId = _chainId,
                    Metadata = new Metadata
                    {
                        Value =
                        {
                            {"aelf_nft_type", "new type"}
                        }
                    },
                    BaseUri = "aelf.com/nft/",
                    IsTokenIdReuse = true
                });
            createResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
            // createResult.Error.ShouldContain("");

            createResult =
                _nftContract.ExecuteMethodWithResult(NftContractMethod.Create, new CreateInput
                {
                    NftType = (NFTType) 10,
                    ProtocolName = "CAT",
                    TotalSupply = 10000,
                    Creator = InitAccount.ConvertAddress(),
                    IsBurnable = true,
                    IssueChainId = _chainId,
                    Metadata = new Metadata(),
                    BaseUri = "aelf.com/nft/",
                    IsTokenIdReuse = true
                });
            createResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
            createResult.Error.ShouldContain("Short name of NFT Type 10 not found.");
        }

        [TestMethod]
        public void AddMintersAndRemoveMintersTest()
        {
            // Add minters
            _nftContract.SetAccount(OtherAccount);
            var symbol = _nftContract.GetNftProtocolInfo("CO").Symbol;

            var addMintersResult =
                _nftContract.AddMinters(new MinterList(), symbol);
            addMintersResult.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            addMintersResult.Error.ShouldContain("No permission.");

            _nftContract.SetAccount(InitAccount);
            addMintersResult =
                _nftContract.AddMinters(new MinterList(), symbol);
            addMintersResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            MinterCount(symbol).ShouldBe(1);

            // Remove minters
            _nftContract.SetAccount(OtherAccount);
            var removeResult =
                _nftContract.RemoveMiners(new MinterList(), symbol);
            removeResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
            addMintersResult.Error.ShouldContain("No permission.");

            _nftContract.SetAccount(InitAccount);
            removeResult =
                _nftContract.RemoveMiners(new MinterList(), symbol);
            removeResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            MinterCount(symbol).ShouldBe(0);

            removeResult =
                _nftContract.RemoveMiners(new MinterList(), symbol);
            removeResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
            addMintersResult.Error.ShouldContain("Minter list is empty.");
        }

        [TestMethod]
        public void MintTest()
        {
            var createResult =
                _nftContract.ExecuteMethodWithResult(NftContractMethod.Create, new CreateInput
                {
                    NftType = NFTType.Collectables,
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

            var amount = 100;
            var tokenId = 1;
            var initBalanceBefore = GetBalanceTest(InitAccount, symbol, tokenId);
            Logger.Info($"initBalanceBefore is {initBalanceBefore}");

            var mintResult =
                _nftContract.ExecuteMethodWithResult(NftContractMethod.Mint, new MintInput
                {
                    Symbol = symbol,
                    Alias = "NFT_CO_CAT1",
                    Metadata = new Metadata(),
                });
            mintResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            var initAccountAfterBalance = GetBalanceTest(InitAccount, symbol, tokenId);
            Logger.Info($"initAccountAfterBalance is {initAccountAfterBalance}");
            initAccountAfterBalance.ShouldBe(1 + initBalanceBefore);

            var nftProtocolInfo = _nftContract.GetNftProtocolInfo(symbol);
            CheckNftAndNftProtocolInfo(symbol, tokenId, new NFTInfo
                {
                    Symbol = symbol,
                    ProtocolName = nftProtocolInfo.ProtocolName,
                    TokenId = tokenId,
                    Creator = nftProtocolInfo.Creator,
                    Minters = {InitAccount.ConvertAddress()},
                    Metadata = nftProtocolInfo.Metadata,
                    Quantity = initAccountAfterBalance,
                    Uri = string.Empty,
                    BaseUri = nftProtocolInfo.BaseUri,
                    Alias = "NFT_CO_CAT1",
                    IsBurned = false,
                    NftType = nftProtocolInfo.NftType
                },
                new NFTProtocolInfo
                {
                    Symbol = symbol,
                    Supply = initAccountAfterBalance,
                    TotalSupply = 10000,
                    Creator = InitAccount.ConvertAddress(),
                    BaseUri = "aelf.com/nft/",
                    IsBurnable = true,
                    IssueChainId = _chainId,
                    NftType = NFTType.Collectables.ToString(),
                    ProtocolName = "CAT",
                    IsTokenIdReuse = true,
                });

            var otherAccountBeforeBalance = GetBalanceTest(OtherAccount, symbol, tokenId);
            Logger.Info($"otherAccountBeforeBalance is {otherAccountBeforeBalance}");
            mintResult =
                _nftContract.ExecuteMethodWithResult(NftContractMethod.Mint, new MintInput
                {
                    Symbol = symbol,
                    Owner = OtherAccount.ConvertAddress(),
                    Uri = "uri_CAT1",
                    Alias = "NFT_CO_CAT2",
                    Metadata = new Metadata
                    {
                        Value =
                        {
                            {"metadata_mint", "mint"}
                        }
                    },
                    Quantity = amount,
                    TokenId = tokenId
                });
            mintResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            var otherAccountAfterBalance = GetBalanceTest(OtherAccount, symbol, tokenId);
            Logger.Info($"otherAccountAfterBalance is {otherAccountAfterBalance}");
            otherAccountAfterBalance.ShouldBe(amount + otherAccountBeforeBalance);

            CheckNftAndNftProtocolInfo(symbol, tokenId, new NFTInfo
                {
                    Symbol = symbol,
                    ProtocolName = nftProtocolInfo.ProtocolName,
                    TokenId = tokenId,
                    Creator = nftProtocolInfo.Creator,
                    Minters = {InitAccount.ConvertAddress()},
                    Metadata = nftProtocolInfo.Metadata,
                    Quantity = initAccountAfterBalance + otherAccountAfterBalance,
                    Uri = string.Empty,
                    BaseUri = nftProtocolInfo.BaseUri,
                    Alias = "NFT_CO_CAT1",
                    IsBurned = false,
                    NftType = nftProtocolInfo.NftType
                },
                new NFTProtocolInfo
                {
                    Symbol = symbol,
                    Supply = initAccountAfterBalance + otherAccountAfterBalance,
                    TotalSupply = 10000,
                    Creator = InitAccount.ConvertAddress(),
                    BaseUri = "aelf.com/nft/",
                    IsBurnable = true,
                    IssueChainId = _chainId,
                    NftType = NFTType.Collectables.ToString(),
                    ProtocolName = "CAT",
                    IsTokenIdReuse = true,
                });

            mintResult =
                _nftContract.ExecuteMethodWithResult(NftContractMethod.Mint, new MintInput
                {
                    Symbol = symbol,
                    Owner = OtherAccount.ConvertAddress(),
                    Uri = "uri_CAT1",
                    Alias = "NFT_CO_CAT2",
                    Metadata = new Metadata
                    {
                        Value =
                        {
                            {"metadata_mint", "mint"}
                        }
                    },
                    Quantity = 10000 - amount,
                    TokenId = tokenId
                });
            mintResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
            mintResult.Error.ShouldContain("Total supply exceeded.");
        }

        [TestMethod]
        public void MintWithIsTokenIdReuseFalseTest()
        {
            var createResult =
                _nftContract.ExecuteMethodWithResult(NftContractMethod.Create, new CreateInput
                {
                    NftType = NFTType.Collectables,
                    ProtocolName = "CAT",
                    TotalSupply = 10000,
                    Creator = InitAccount.ConvertAddress(),
                    IsBurnable = true,
                    IssueChainId = _chainId,
                    Metadata = new Metadata(),
                    BaseUri = "aelf.com/nft/",
                    IsTokenIdReuse = false
                });
            createResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var symbol = StringValue.Parser.ParseFrom(ByteArrayHelper.HexStringToByteArray(createResult.ReturnValue))
                .Value;
            Logger.Info($"symbol is {symbol}");

            var amount = 100;
            var tokenId = 1;
            var initBalanceBefore = GetBalanceTest(InitAccount, symbol, tokenId);
            Logger.Info($"initBalanceBefore is {initBalanceBefore}");

            _nftContract.SetAccount(OtherAccount);
            var mintResult =
                _nftContract.ExecuteMethodWithResult(NftContractMethod.Mint, new MintInput
                {
                    Symbol = symbol,
                    Alias = "NFT_CO_CAT1",
                    Metadata = new Metadata(),
                });
            mintResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
            mintResult.Error.ShouldContain("No permission to mint.");

            _nftContract.SetAccount(InitAccount);
            mintResult =
                _nftContract.ExecuteMethodWithResult(NftContractMethod.Mint, new MintInput
                {
                    Symbol = "CO12345",
                    Alias = "NFT_CO_CAT1",
                    Metadata = new Metadata()
                });
            mintResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
            mintResult.Error.ShouldContain("Invalid NFT Token symbol: CO12345");

            mintResult =
                _nftContract.ExecuteMethodWithResult(NftContractMethod.Mint, new MintInput
                {
                    Symbol = symbol,
                    Alias = "NFT_CO_CAT1",
                    Metadata = new Metadata(),
                    Quantity = 10000 + 1,
                });
            mintResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
            mintResult.Error.ShouldContain("Total supply exceeded.");

            mintResult =
                _nftContract.ExecuteMethodWithResult(NftContractMethod.Mint, new MintInput
                {
                    Symbol = symbol,
                    Alias = "NFT_CO_CAT1",
                    Metadata = new Metadata(),
                });
            mintResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            mintResult =
                _nftContract.ExecuteMethodWithResult(NftContractMethod.Mint, new MintInput
                {
                    Symbol = symbol,
                    Owner = OtherAccount.ConvertAddress(),
                    Uri = "uri_CAT1",
                    Alias = "NFT_CO_CAT2",
                    Metadata = new Metadata
                    {
                        Value =
                        {
                            {"metadata_mint", "mint"}
                        }
                    },
                    Quantity = amount,
                    TokenId = tokenId
                });
            mintResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
            mintResult.Error.ShouldContain("Token id 1 already exists. Please assign a different token id.");
        }

        [TestMethod]
        public void BurnTest()
        {
            // contract:Mzq52XLHUsWtQsdCGaSMrNHDcbQhMqDYtyDvPqvztQn9ad9cX
            var symbol = "CO538553519";
            var tokenId = 1;
            var amount = 1;
            var nftInfoQuantity = _nftContract.GetNftInfo(symbol, tokenId).Quantity;
            Logger.Info($"nftInfoQuantity is {nftInfoQuantity}");

            _nftContract.SetAccount(OtherAccount1);
            var burnResult = _nftContract.Burn(symbol, tokenId, 1);
            burnResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
            burnResult.Error.ShouldContain("No permission.");

            _nftContract.SetAccount(OtherAccount);
            burnResult = _nftContract.Burn(symbol, tokenId, 101);
            burnResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
            burnResult.Error.ShouldContain("No permission.");

            var minterList = _nftContract.GetMinterList(symbol);
            Logger.Info($"minterList is {minterList}");
            var addMinter = _nftContract.AddMinters(new MinterList {Value = {OtherAccount.ConvertAddress()}}, symbol);
            minterList = _nftContract.GetMinterList(symbol);
            Logger.Info($"minterList is {minterList}");

            _nftContract.SetAccount(OtherAccount);
            burnResult = _nftContract.Burn(symbol, tokenId, amount);
            burnResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var nftInfoQuantityAfter = _nftContract.GetNftInfo(symbol, tokenId).Quantity;
            Logger.Info($"nftInfoQuantityAfter is {nftInfoQuantityAfter}");

            var nftProtocolInfo = _nftContract.GetNftProtocolInfo(symbol);
            CheckNftAndNftProtocolInfo(symbol, tokenId, new NFTInfo
                {
                    Symbol = symbol,
                    ProtocolName = nftProtocolInfo.ProtocolName,
                    TokenId = tokenId,
                    Creator = nftProtocolInfo.Creator,
                    Minters = {InitAccount.ConvertAddress()},
                    Metadata = new Metadata
                    {
                        Value =
                        {
                            {"aelf_nft_type", "Collectables"},
                            {"aelf_nft_base_uri", "aelf.com/nft/"},
                            {"aelf_nft_token_id_reuse", "True"}
                        }
                    },
                    Quantity = nftInfoQuantity - amount,
                    Uri = string.Empty,
                    BaseUri = nftProtocolInfo.BaseUri,
                    Alias = "NFT_CO_CAT1",
                    IsBurned = false,
                    NftType = nftProtocolInfo.NftType
                },
                new NFTProtocolInfo
                {
                    Symbol = symbol,
                    Supply = nftInfoQuantity - amount,
                    TotalSupply = 10000,
                    Creator = InitAccount.ConvertAddress(),
                    BaseUri = "aelf.com/nft/",
                    IsBurnable = true,
                    IssueChainId = _chainId,
                    NftType = NFTType.Collectables.ToString(),
                    ProtocolName = "CAT",
                    IsTokenIdReuse = true,
                });
        }

        [TestMethod]
        public void TransferTest()
        {
            var symbol = NftSymbol("CO");
            var amount = 1;
            var tokenId = 1;

            var fromBalanceBefore = GetBalanceTest(InitAccount, symbol, tokenId);
            var toBalanceBefore = GetBalanceTest(OtherAccount, symbol, tokenId);
            Logger.Info($"fromBalanceBefore is {fromBalanceBefore}");
            Logger.Info($"toBalanceBefore is {toBalanceBefore}");

            var transferResult =
                _nftContract.ExecuteMethodWithResult(NftContractMethod.Transfer, new TransferInput
                {
                    To = OtherAccount.ConvertAddress(),
                    Symbol = symbol,
                    TokenId = tokenId,
                    Memo = "transfer",
                    Amount = amount
                });
            transferResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            // Get balance
            var fromBalanceAfter = GetBalanceTest(InitAccount, symbol, tokenId);
            var toBalanceAfter = GetBalanceTest(OtherAccount, symbol, tokenId);
            Logger.Info($"fromBalanceAfter is {fromBalanceAfter}");
            Logger.Info($"toBalanceAfter is {toBalanceAfter}");

            // Get balance by tokenHash
            var tokenHash = _nftContract.CalculateTokenHash(symbol, tokenId);
            fromBalanceAfter = GetBalanceByTokenHashTest(InitAccount, tokenHash);
            toBalanceAfter = GetBalanceByTokenHashTest(OtherAccount, tokenHash);
            Logger.Info($"fromBalanceAfter is {fromBalanceAfter}");
            Logger.Info($"toBalanceAfter is {toBalanceAfter}");
        }

        [TestMethod]
        public void TransferFromTest()
        {
            var symbol = NftSymbol("CO");
            var amount = 1;
            var tokenId = 1;

            var fromBalanceBefore = GetBalanceTest(InitAccount, symbol, tokenId);
            var toBalanceBefore = GetBalanceTest(OtherAccount, symbol, tokenId);
            Logger.Info($"fromBalanceBefore is {fromBalanceBefore}");
            Logger.Info($"toBalanceBefore is {toBalanceBefore}");

            var transferResult =
                _nftContract.ExecuteMethodWithResult(NftContractMethod.TransferFrom, new TransferFromInput
                {
                    From = InitAccount.ConvertAddress(),
                    To = OtherAccount.ConvertAddress(),
                    Symbol = symbol,
                    TokenId = tokenId,
                    Memo = "transferFrom",
                    Amount = amount
                });
            transferResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            var fromBalanceAfter = GetBalanceTest(InitAccount, symbol, tokenId);
            var toBalanceAfter = GetBalanceTest(OtherAccount, symbol, tokenId);
            Logger.Info($"fromBalanceAfter is {fromBalanceAfter}");
            Logger.Info($"toBalanceAfter is {toBalanceAfter}");
        }

        [TestMethod]
        public void AddWhiteList()
        {
            var check = _tokenContract.IsInCreateTokenWhiteList(_nftContract.ContractAddress);
            if (check) return;

            var result = AuthorityManager.ExecuteTransactionWithAuthority(_tokenContract.ContractAddress,
                "AddAddressToCreateTokenWhiteList", _nftContract.Contract, InitAccount);
            result.Status.ShouldBe(TransactionResultStatus.Mined);

            check = _tokenContract.IsInCreateTokenWhiteList(_nftContract.ContractAddress);
            check.ShouldBeTrue();
        }

        private void CheckTokenNftProtocolInfo(string symbol, TokenInfo expectTokenInfo,
            NFTProtocolInfo expectNftProtocolInfo)
        {
            var tokenInfo = _tokenContract.GetTokenInfo(symbol);
            Logger.Info($"tokenInfo.Symbol is {tokenInfo.Symbol}");
            Logger.Info($"tokenInfo.TokenName is {tokenInfo.TokenName}");
            Logger.Info($"tokenInfo.Supply is {tokenInfo.Supply}");
            Logger.Info($"tokenInfo.TotalSupply is {tokenInfo.TotalSupply}");
            Logger.Info($"tokenInfo.Decimals is {tokenInfo.Decimals}");
            Logger.Info($"tokenInfo.Issuer is {tokenInfo.Issuer}");
            Logger.Info($"tokenInfo.IsBurnable is {tokenInfo.IsBurnable}");
            Logger.Info($"tokenInfo.IssueChainId is {tokenInfo.IssueChainId}");
            Logger.Info($"tokenInfo.Issued is {tokenInfo.Issued}");
            Logger.Info($"tokenInfo.ExternalInfo is {tokenInfo.ExternalInfo}");

            var nftProtocolInfo = _nftContract.GetNftProtocolInfo(symbol);
            Logger.Info($"nftProtocolInfo.Symbol is {nftProtocolInfo.Symbol}");
            Logger.Info($"nftProtocolInfo.Supply is {nftProtocolInfo.Supply}");
            Logger.Info($"nftProtocolInfo.TotalSupply is {nftProtocolInfo.TotalSupply}");
            Logger.Info($"nftProtocolInfo.Creator is {nftProtocolInfo.Creator}");
            Logger.Info($"nftProtocolInfo.IsBurnable is {nftProtocolInfo.IsBurnable}");
            Logger.Info($"nftProtocolInfo.IssueChainId is {nftProtocolInfo.IssueChainId}");
            Logger.Info($"nftProtocolInfo.Metadata is {nftProtocolInfo.Metadata}");
            Logger.Info($"nftProtocolInfo.NftType is {nftProtocolInfo.NftType}");
            Logger.Info($"nftProtocolInfo.ProtocolName is {nftProtocolInfo.ProtocolName}");
            Logger.Info($"nftProtocolInfo.IsTokenIdReuse is {nftProtocolInfo.IsTokenIdReuse}");

            tokenInfo.Symbol.ShouldBe(expectTokenInfo.Symbol);
            tokenInfo.TokenName.ShouldBe(expectTokenInfo.TokenName);
            tokenInfo.Supply.ShouldBe(expectTokenInfo.Supply);
            tokenInfo.TotalSupply.ShouldBe(expectTokenInfo.TotalSupply);
            tokenInfo.Decimals.ShouldBe(expectTokenInfo.Decimals);
            tokenInfo.Issuer.ShouldBe(expectTokenInfo.Issuer);
            tokenInfo.IsBurnable.ShouldBe(expectTokenInfo.IsBurnable);
            tokenInfo.IssueChainId.ShouldBe(expectTokenInfo.IssueChainId);
            tokenInfo.Issued.ShouldBe(expectTokenInfo.Issued);
            tokenInfo.ExternalInfo.Value["aelf_nft_type"].ShouldBe(expectNftProtocolInfo.NftType);
            tokenInfo.ExternalInfo.Value["aelf_nft_base_uri"].ShouldBe(expectNftProtocolInfo.BaseUri);
            tokenInfo.ExternalInfo.Value["aelf_nft_token_id_reuse"]
                .ShouldBe(expectNftProtocolInfo.IsTokenIdReuse.ToString());

            nftProtocolInfo.Symbol.ShouldBe(expectNftProtocolInfo.Symbol);
            nftProtocolInfo.Supply.ShouldBe(expectNftProtocolInfo.Supply);
            nftProtocolInfo.TotalSupply.ShouldBe(expectNftProtocolInfo.TotalSupply);
            nftProtocolInfo.Creator.ShouldBe(expectNftProtocolInfo.Creator);
            nftProtocolInfo.IsBurnable.ShouldBe(expectNftProtocolInfo.IsBurnable);
            nftProtocolInfo.IssueChainId.ShouldBe(expectNftProtocolInfo.IssueChainId);
            nftProtocolInfo.Metadata.Value["aelf_nft_type"].ShouldBe(expectNftProtocolInfo.NftType);
            nftProtocolInfo.Metadata.Value["aelf_nft_base_uri"].ShouldBe(expectNftProtocolInfo.BaseUri);
            nftProtocolInfo.Metadata.Value["aelf_nft_token_id_reuse"]
                .ShouldBe(expectNftProtocolInfo.IsTokenIdReuse.ToString());
            nftProtocolInfo.NftType.ShouldBe(expectNftProtocolInfo.NftType);
            nftProtocolInfo.ProtocolName.ShouldBe(expectNftProtocolInfo.ProtocolName);
            nftProtocolInfo.IsTokenIdReuse.ShouldBe(expectNftProtocolInfo.IsTokenIdReuse);
        }

        private void CheckNftAndNftProtocolInfo(string symbol, long tokenId, NFTInfo expectNftInfo,
            NFTProtocolInfo expectNftProtocolInfo
        )
        {
            var nftInfo = _nftContract.GetNftInfo(symbol, tokenId);
            Logger.Info($"nftInfo.Symbol is {nftInfo.Symbol}");
            Logger.Info($"nftInfo.ProtocolName is {nftInfo.ProtocolName}");
            Logger.Info($"nftInfo.TokenId is {nftInfo.TokenId}");
            Logger.Info($"nftInfo.Creator is {nftInfo.Creator}");
            Logger.Info($"nftInfo.Minters is {nftInfo.Minters.First().ToBase58()}");
            Logger.Info($"nftInfo.Metadata is {nftInfo.Metadata}");
            Logger.Info($"nftInfo.Quantity is {nftInfo.Quantity}");
            Logger.Info($"nftInfo.Uri is {nftInfo.Uri}");
            Logger.Info($"nftInfo.BaseUri is {nftInfo.BaseUri}");
            Logger.Info($"nftInfo.Alias is {nftInfo.Alias}");
            Logger.Info($"nftInfo.IsBurned is {nftInfo.IsBurned}");
            Logger.Info($"nftInfo.NftType is {nftInfo.NftType}");

            var nftProtocolInfo = _nftContract.GetNftProtocolInfo(symbol);
            Logger.Info($"nftProtocolInfo.Symbol is {nftProtocolInfo.Symbol}");
            Logger.Info($"nftProtocolInfo.Supply is {nftProtocolInfo.Supply}");
            Logger.Info($"nftProtocolInfo.TotalSupply is {nftProtocolInfo.TotalSupply}");
            Logger.Info($"nftProtocolInfo.Creator is {nftProtocolInfo.Creator}");
            Logger.Info($"nftProtocolInfo.IsBurnable is {nftProtocolInfo.IsBurnable}");
            Logger.Info($"nftProtocolInfo.IssueChainId is {nftProtocolInfo.IssueChainId}");
            Logger.Info($"nftProtocolInfo.Metadata is {nftProtocolInfo.Metadata}");
            Logger.Info($"nftProtocolInfo.NftType is {nftProtocolInfo.NftType}");
            Logger.Info($"nftProtocolInfo.ProtocolName is {nftProtocolInfo.ProtocolName}");
            Logger.Info($"nftProtocolInfo.IsTokenIdReuse is {nftProtocolInfo.IsTokenIdReuse}");

            nftInfo.Symbol.ShouldBe(expectNftInfo.Symbol);
            nftInfo.ProtocolName.ShouldBe(expectNftInfo.ProtocolName);
            nftInfo.TokenId.ShouldBe(expectNftInfo.TokenId);
            nftInfo.Creator.ShouldBe(expectNftInfo.Creator);
            nftInfo.Minters.ShouldBe(expectNftInfo.Minters);
            nftInfo.Metadata.ShouldBe(expectNftInfo.Metadata);
            nftInfo.Quantity.ShouldBe(expectNftInfo.Quantity);
            nftInfo.Uri.ShouldBe(expectNftInfo.Uri);
            nftInfo.BaseUri.ShouldBe(expectNftInfo.BaseUri);
            nftInfo.Alias.ShouldBe(expectNftInfo.Alias);
            nftInfo.IsBurned.ShouldBe(expectNftInfo.IsBurned);
            nftInfo.NftType.ShouldBe(expectNftInfo.NftType);

            nftProtocolInfo.Symbol.ShouldBe(expectNftProtocolInfo.Symbol);
            nftProtocolInfo.Supply.ShouldBe(expectNftProtocolInfo.Supply);
            nftProtocolInfo.TotalSupply.ShouldBe(expectNftProtocolInfo.TotalSupply);
            nftProtocolInfo.Creator.ShouldBe(expectNftProtocolInfo.Creator);
            nftProtocolInfo.IsBurnable.ShouldBe(expectNftProtocolInfo.IsBurnable);
            nftProtocolInfo.IssueChainId.ShouldBe(expectNftProtocolInfo.IssueChainId);
            nftProtocolInfo.Metadata.Value["aelf_nft_type"].ShouldBe(expectNftProtocolInfo.NftType);
            nftProtocolInfo.Metadata.Value["aelf_nft_base_uri"].ShouldBe(expectNftProtocolInfo.BaseUri);
            nftProtocolInfo.Metadata.Value["aelf_nft_token_id_reuse"]
                .ShouldBe(expectNftProtocolInfo.IsTokenIdReuse.ToString());
            nftProtocolInfo.NftType.ShouldBe(expectNftProtocolInfo.NftType);
            nftProtocolInfo.ProtocolName.ShouldBe(expectNftProtocolInfo.ProtocolName);
            nftProtocolInfo.IsTokenIdReuse.ShouldBe(expectNftProtocolInfo.IsTokenIdReuse);
        }

        public int MinterCount(string symbol)
        {
            var minterConut = 0;
            var minterList = _nftContract.GetMinterList(symbol);
            foreach (var minter in minterList.Value)
            {
                if (minterList.Value.Contains(minter))
                {
                    minterConut = minterConut + 1;
                }
            }

            return minterConut;
        }

        private string NftSymbol(string shortName)
        {
            return _nftContract.GetNftProtocolInfo(shortName).Symbol;
        }

        private long GetBalanceTest(string owner, string symbol, long tokenId)
        {
            var getBalance = _nftContract.GetBalance(owner, symbol, tokenId);

            Logger.Info($"owner of {symbol} is {getBalance.Owner}");
            Logger.Info($"TokenHash of {symbol} is {getBalance.TokenHash}");
            Logger.Info($"Balance of {symbol} is {getBalance.Balance}");

            return getBalance.Balance;
        }

        private long GetBalanceByTokenHashTest(string owner, Hash tokenHash)
        {
            var getBalance = _nftContract.GetBalanceByTokenHash(owner, tokenHash);

            Logger.Info($"owner of {tokenHash} is {getBalance.Owner}");
            Logger.Info($"TokenHash of {tokenHash} is {getBalance.TokenHash}");
            Logger.Info($"Balance of {tokenHash} is {getBalance.Balance}");

            return getBalance.Balance;
        }

        // [TestMethod]
        // public void Transfer()
        // {
        //     _genesisContract = GenesisContract.GetGenesisContract(NodeManager, InitAccount);
        //     _tokenContract = _genesisContract.GetTokenContract(InitAccount);
        //     var account1BalanceBefore = _tokenContract.GetUserBalance(InitAccount, "ELF");
        //     var targetBalanceBefore = _tokenContract.GetUserBalance(OtherAccount1, "ELF");
        //     Logger.Info($"account1BalanceBefore is {account1BalanceBefore}");
        //     Logger.Info($"targetBalanceBefore is {targetBalanceBefore}");
        //
        //     _tokenContract.TransferBalance(InitAccount, OtherAccount1, 1000_00000000, "ELF");
        //     var account1BalanceAfter = _tokenContract.GetUserBalance(InitAccount, "ELF");
        //     var targetBalanceAfter = _tokenContract.GetUserBalance(OtherAccount1, "ELF");
        //     Logger.Info($"account1BalanceAfter is {account1BalanceAfter}");
        //     Logger.Info($"targetBalanceAfter is {targetBalanceAfter}");
        // }   
    }
}