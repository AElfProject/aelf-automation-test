using System;
using System.Linq;
using AElf.Contracts.MultiToken;
using AElf.Contracts.NFT;
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

        private string Nft = "";

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
            var result = _nftContract.AddNftType("Art", "AR");
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
            result.Error.ShouldContain("No permission.");

            _nftContract.SetAccount(InitAccount);
            var addResult = AuthorityManager.ExecuteTransactionWithAuthority(_nftContract.ContractAddress,
                nameof(NftContractMethod.AddNFTType), new AddNFTTypeInput
                {
                    FullName = "Art",
                    ShortName = "A"
                }, InitAccount);
            addResult.Status.ShouldBe(TransactionResultStatus.NodeValidationFailed);
            addResult.Error.ShouldContain("Incorrect short name.");

            addResult = AuthorityManager.ExecuteTransactionWithAuthority(_nftContract.ContractAddress,
                nameof(NftContractMethod.AddNFTType), new AddNFTTypeInput
                {
                    FullName = "Art",
                    ShortName = "ARR"
                }, InitAccount);
            addResult.Status.ShouldBe(TransactionResultStatus.NodeValidationFailed);
            addResult.Error.ShouldContain("Incorrect short name.");

            addResult = AuthorityManager.ExecuteTransactionWithAuthority(_nftContract.ContractAddress,
                nameof(NftContractMethod.AddNFTType), new AddNFTTypeInput
                {
                    FullName = "Art",
                    ShortName = "AR"
                }, InitAccount);
            addResult.Status.ShouldBe(TransactionResultStatus.NodeValidationFailed);
            addResult.Error.ShouldContain("Short name AR already exists.");

            addResult = AuthorityManager.ExecuteTransactionWithAuthority(_nftContract.ContractAddress,
                nameof(NftContractMethod.AddNFTType), new AddNFTTypeInput
                {
                    FullName = "Art",
                    ShortName = "WH"
                }, InitAccount);
            addResult.Status.ShouldBe(TransactionResultStatus.NodeValidationFailed);
            addResult.Error.ShouldContain("Full name Art already exists.");

            addResult = AuthorityManager.ExecuteTransactionWithAuthority(_nftContract.ContractAddress,
                nameof(NftContractMethod.AddNFTType), new AddNFTTypeInput
                {
                    FullName = "Baby",
                    ShortName = "BY"
                }, InitAccount);
            addResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var nftTypes = _nftContract.GetNftTypes();
            nftTypes.Value["BY"].ShouldBe("Baby");
            foreach (var pair in nftTypes.Value)
            {
                Logger.Info($"{pair.Key}:{pair.Value}\n");
            }

            // Remove
            _nftContract.SetAccount(OtherAccount);
            result = _nftContract.RemoveNftType("AR");
            result.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            result.Error.ShouldContain("No permission.");

            var removeResult = AuthorityManager.ExecuteTransactionWithAuthority(_nftContract.ContractAddress,
                nameof(NftContractMethod.RemoveNFTType), new StringValue {Value = "A"}, InitAccount);
            removeResult.Status.ShouldBe(TransactionResultStatus.NodeValidationFailed);
            removeResult.Error.ShouldContain("Incorrect short name.");

            removeResult = AuthorityManager.ExecuteTransactionWithAuthority(_nftContract.ContractAddress,
                nameof(NftContractMethod.RemoveNFTType), new StringValue {Value = "ARR"}, InitAccount);
            removeResult.Status.ShouldBe(TransactionResultStatus.NodeValidationFailed);
            removeResult.Error.ShouldContain("Incorrect short name.");

            removeResult = AuthorityManager.ExecuteTransactionWithAuthority(_nftContract.ContractAddress,
                nameof(NftContractMethod.RemoveNFTType), new StringValue {Value = "AA"}, InitAccount);
            removeResult.Status.ShouldBe(TransactionResultStatus.NodeValidationFailed);
            removeResult.Error.ShouldContain("Short name AA does not exist.");

            _nftContract.SetAccount(InitAccount);
            removeResult = AuthorityManager.ExecuteTransactionWithAuthority(_nftContract.ContractAddress,
                nameof(NftContractMethod.RemoveNFTType), new StringValue {Value = "BY"}, InitAccount);
            removeResult.Status.ShouldBe(TransactionResultStatus.Mined);
            nftTypes = _nftContract.GetNftTypes();
            var sum = 0;
            foreach (var pair in nftTypes.Value)
            {
                Logger.Info($"{pair.Key}:{pair.Value}\n");
                if (pair.Key == "BY")
                {
                    sum = sum + 1;
                }
            }

            sum.ShouldBe(0);
        }

        [TestMethod]
        public void CreateAfterAddNftTypeTest()
        {
            var addResult = AuthorityManager.ExecuteTransactionWithAuthority(_nftContract.ContractAddress,
                nameof(NftContractMethod.AddNFTType), new AddNFTTypeInput
                {
                    FullName = "Baby",
                    ShortName = "BY"
                }, InitAccount);
            addResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var nftTypesInit = _nftContract.GetNftTypes();
            foreach (var pair in nftTypesInit.Value)
            {
                Logger.Info($"{pair.Key}:{pair.Value}\n");
            }

            var createResult =
                _nftContract.ExecuteMethodWithResult(NftContractMethod.Create, new CreateInput
                {
                    NftType = "Baby",
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
                    NftType = "Baby",
                    ProtocolName = "CAT",
                    IsTokenIdReuse = true
                }
            );
        }

        [TestMethod]
        public void CrossChainCreateTest()
        {
            var addResult = AuthorityManager.ExecuteTransactionWithAuthority(_nftContract.ContractAddress,
                nameof(NftContractMethod.AddNFTType), new AddNFTTypeInput
                {
                    FullName = "Baby",
                    ShortName = "BY"
                }, InitAccount);
            addResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var nftTypesInit = _nftContract.GetNftTypes();
            foreach (var pair in nftTypesInit.Value)
            {
                Logger.Info($"{pair.Key}:{pair.Value}\n");
            }

            var createResult =
                _nftContract.ExecuteMethodWithResult(NftContractMethod.Create, new CreateInput
                {
                    NftType = "Baby",
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

            var crossChainCreateResult = _nftContract.CrossChainCreate(symbol);
            crossChainCreateResult.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            crossChainCreateResult.Error.ShouldContain("already created.");
        }

        [TestMethod]
        public void CreateTest()
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
                    NftType = NFTType.Any.ToString(),
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
                    NftType = NFTType.Collectables.ToString(),
                    ProtocolName = "CAT",
                    TotalSupply = 10000,
                    Creator = InitAccount.ConvertAddress(),
                    IsBurnable = true,
                    IssueChainId = _chainId,
                    Metadata = new Metadata
                    {
                        Value =
                        {
                            {"aelf_nft_type", "Collectables_metadata"},
                            {"aelf_nft_base_uri", "aelf.com/nft/_metadata"},
                            {"aelf_nft_token_id_reuse", "false"}
                        }
                    },
                    BaseUri = "aelf.com/nft/",
                });
            createResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            symbol = StringValue.Parser.ParseFrom(ByteArrayHelper.HexStringToByteArray(createResult.ReturnValue))
                .Value;
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
                    ExternalInfo = new ExternalInfo
                    {
                        Value =
                        {
                            {"aelf_nft_type", "Collectables"},
                            {"aelf_nft_base_uri", "aelf.com/nft/"},
                            {"aelf_nft_token_id_reuse", "False"}
                        }
                    },
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
                    Metadata = new Metadata
                    {
                        Value =
                        {
                            {"aelf_nft_type", "Collectables"},
                            {"aelf_nft_base_uri", "aelf.com/nft/"},
                            {"aelf_nft_token_id_reuse", "False"}
                        }
                    },
                    NftType = NFTType.Collectables.ToString(),
                    ProtocolName = "CAT",
                    IsTokenIdReuse = false,
                }
            );

            createResult =
                _nftContract.ExecuteMethodWithResult(NftContractMethod.Create, new CreateInput
                {
                    NftType = "10",
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
            // Contract:2cJKsFvfb1PTt6XTP6we4BG2axYUzYeP3Y8XBeFRXJaUtM2pgm
            var symbol = "CO439834521";

            var minterList = _nftContract.GetMinterList(symbol);
            Logger.Info($"minterList is {minterList}");
            minterList.Value.Count.ShouldBe(0);
            // Add minters
            _nftContract.SetAccount(OtherAccount);
            var addMintersResult =
                _nftContract.AddMinters(
                    new MinterList {Value = {InitAccount.ConvertAddress(), OtherAccount.ConvertAddress()}}, symbol);
            addMintersResult.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            addMintersResult.Error.ShouldContain("No permission.");

            _nftContract.SetAccount(InitAccount);
            addMintersResult =
                _nftContract.AddMinters(new MinterList {Value = {InitAccount.ConvertAddress()}}, symbol);
            addMintersResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            minterList = _nftContract.GetMinterList(symbol);
            Logger.Info($"minterList is {minterList}");
            minterList.Value.Count.ShouldBe(1);
            minterList.Value[0].ShouldBe(InitAccount.ConvertAddress());

            addMintersResult =
                _nftContract.AddMinters(
                    new MinterList {Value = {OtherAccount.ConvertAddress()}}, symbol);
            addMintersResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            minterList = _nftContract.GetMinterList(symbol);
            Logger.Info($"minterList is {minterList}");
            minterList.Value.Count.ShouldBe(2);
            minterList.Value[0].ShouldBe(InitAccount.ConvertAddress());
            minterList.Value[1].ShouldBe(OtherAccount.ConvertAddress());

            addMintersResult =
                _nftContract.AddMinters(
                    new MinterList {Value = {InitAccount.ConvertAddress(), OtherAccount.ConvertAddress()}}, symbol);
            addMintersResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            minterList = _nftContract.GetMinterList(symbol);
            Logger.Info($"minterList is {minterList}");
            minterList.Value.Count.ShouldBe(2);
            minterList.Value[0].ShouldBe(InitAccount.ConvertAddress());
            minterList.Value[1].ShouldBe(OtherAccount.ConvertAddress());

            // Remove minters
            _nftContract.SetAccount(OtherAccount);
            var removeResult =
                _nftContract.RemoveMinters(
                    new MinterList {Value = {InitAccount.ConvertAddress(), OtherAccount.ConvertAddress()}}, symbol);
            removeResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
            removeResult.Error.ShouldContain("No permission.");

            _nftContract.SetAccount(InitAccount);
            removeResult =
                _nftContract.RemoveMinters(
                    new MinterList {Value = {InitAccount.ConvertAddress()}}, symbol);
            removeResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            minterList = _nftContract.GetMinterList(symbol);
            Logger.Info($"minterList is {minterList}");
            minterList.Value.Count.ShouldBe(1);
            minterList.Value[0].ShouldBe(OtherAccount.ConvertAddress());

            removeResult =
                _nftContract.RemoveMinters(
                    new MinterList {Value = {InitAccount.ConvertAddress()}}, symbol);
            removeResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            minterList = _nftContract.GetMinterList(symbol);
            Logger.Info($"minterList is {minterList}");
            minterList.Value.Count.ShouldBe(1);
            minterList.Value[0].ShouldBe(OtherAccount.ConvertAddress());

            removeResult =
                _nftContract.RemoveMinters(
                    new MinterList {Value = {OtherAccount.ConvertAddress()}}, symbol);
            removeResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            minterList = _nftContract.GetMinterList(symbol);
            Logger.Info($"minterList is {minterList}");
            minterList.Value.Count.ShouldBe(0);

            removeResult =
                _nftContract.RemoveMinters(
                    new MinterList {Value = {OtherAccount.ConvertAddress()}}, symbol);
            removeResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            minterList = _nftContract.GetMinterList(symbol);
            Logger.Info($"minterList is {minterList}");
            minterList.Value.Count.ShouldBe(0);
        }

        [TestMethod]
        public void MintTest()
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
        public void MintMetaDataTest()
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

            var amount = 100;
            var tokenId = 1;

            var nftProtocolInfo = _nftContract.GetNftProtocolInfo(symbol);
            Logger.Info($"nftProtocolInfo.Metadata is {nftProtocolInfo.Metadata}");

            var mintResult =
                _nftContract.ExecuteMethodWithResult(NftContractMethod.Mint, new MintInput
                {
                    Symbol = symbol,
                    Owner = InitAccount.ConvertAddress(),
                    Uri = "uri_CAT1",
                    Alias = "NFT_CO_CAT1",
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
                            {"aelf_nft_token_id_reuse", "True"},
                            {"metadata_mint", "mint"}
                        }
                    },
                    Quantity = amount,
                    Uri = "uri_CAT1",
                    BaseUri = nftProtocolInfo.BaseUri,
                    Alias = "NFT_CO_CAT1",
                    IsBurned = false,
                    NftType = nftProtocolInfo.NftType
                },
                new NFTProtocolInfo
                {
                    Symbol = symbol,
                    Supply = amount,
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
        public void MintWithFalseTest()
        {
            var createResult =
                _nftContract.ExecuteMethodWithResult(NftContractMethod.Create, new CreateInput
                {
                    NftType = NFTType.Collectables.ToString(),
                    ProtocolName = "CAT",
                    TotalSupply = 10000,
                    Creator = InitAccount.ConvertAddress(),
                    IsBurnable = false,
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

            var addMinter =
                _nftContract.AddMinters(
                    new MinterList {Value = {InitAccount.ConvertAddress(), OtherAccount.ConvertAddress()}}, symbol);
            addMinter.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var minterList = _nftContract.GetMinterList(symbol);
            Logger.Info($"minterList is {minterList}");

            var burnResult = _nftContract.Burn(symbol, tokenId, 1);
            burnResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
            burnResult.Error.ShouldContain(" is not burnable.");
        }

        [TestMethod]
        public void BurnTest()
        {
            // Contract:uBvnFUUKG43qfnjPqoXB8S4nHkHaPXYgjMDn5B2CRPigUeM7B

            var symbol = "CO999123111";
            var tokenId = 1;
            var nftInfoQuantity = _nftContract.GetNftInfo(symbol, tokenId).Quantity;
            Logger.Info($"nftInfoQuantity is {nftInfoQuantity}");
            var initAccountBalance = GetBalanceTest(InitAccount, symbol, tokenId);
            var otherAccountBalance = GetBalanceTest(OtherAccount, symbol, tokenId);
            initAccountBalance.ShouldBe(1);
            otherAccountBalance.ShouldBe(100);

            var minterList = _nftContract.GetMinterList(symbol);
            Logger.Info($"minterList is {minterList}");

            _nftContract.SetAccount(OtherAccount);
            var burnResult = _nftContract.Burn(symbol, tokenId, 1);
            burnResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
            burnResult.Error.ShouldContain("No permission.");

            _nftContract.SetAccount(InitAccount);
            var addMinter =
                _nftContract.AddMinters(
                    new MinterList {Value = {InitAccount.ConvertAddress(), OtherAccount.ConvertAddress()}}, symbol);
            addMinter.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            minterList = _nftContract.GetMinterList(symbol);
            Logger.Info($"minterList is {minterList}");

            burnResult = _nftContract.Burn(symbol, tokenId, 2);
            burnResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
            burnResult.Error.ShouldContain("No permission.");

            burnResult = _nftContract.Burn(symbol, tokenId, 1);
            burnResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            initAccountBalance = GetBalanceTest(InitAccount, symbol, tokenId);
            otherAccountBalance = GetBalanceTest(OtherAccount, symbol, tokenId);
            initAccountBalance.ShouldBe(0);
            otherAccountBalance.ShouldBe(100);

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
                    Quantity = nftInfoQuantity - 1,
                    Uri = string.Empty,
                    BaseUri = nftProtocolInfo.BaseUri,
                    Alias = "NFT_CO_CAT1",
                    IsBurned = false,
                    NftType = nftProtocolInfo.NftType
                },
                new NFTProtocolInfo
                {
                    Symbol = symbol,
                    Supply = nftInfoQuantity - 1,
                    TotalSupply = 10000,
                    Creator = InitAccount.ConvertAddress(),
                    BaseUri = "aelf.com/nft/",
                    IsBurnable = true,
                    IssueChainId = _chainId,
                    NftType = NFTType.Collectables.ToString(),
                    ProtocolName = "CAT",
                    IsTokenIdReuse = true,
                });

            _nftContract.SetAccount(OtherAccount);
            burnResult = _nftContract.Burn(symbol, tokenId, 101);
            burnResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
            burnResult.Error.ShouldContain("No permission.");

            burnResult = _nftContract.Burn(symbol, tokenId, 100);
            burnResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            nftInfoQuantityAfter = _nftContract.GetNftInfo(symbol, tokenId).Quantity;
            Logger.Info($"nftInfoQuantityAfter is {nftInfoQuantityAfter}");
            initAccountBalance = GetBalanceTest(InitAccount, symbol, tokenId);
            otherAccountBalance = GetBalanceTest(OtherAccount, symbol, tokenId);
            initAccountBalance.ShouldBe(0);
            otherAccountBalance.ShouldBe(0);

            nftProtocolInfo = _nftContract.GetNftProtocolInfo(symbol);
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
                    Quantity = 0,
                    Uri = string.Empty,
                    BaseUri = nftProtocolInfo.BaseUri,
                    Alias = "NFT_CO_CAT1",
                    IsBurned = true,
                    NftType = nftProtocolInfo.NftType
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
                    IsTokenIdReuse = true,
                });

            _nftContract.SetAccount(OtherAccount);
            burnResult = _nftContract.Burn(symbol, tokenId, 1);
            burnResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
            burnResult.Error.ShouldContain("No permission.");
        }

        [TestMethod]
        public void BurnAfterTransferTest()
        {
            var amount = 100;
            var tokenId = 1;
            var symbol = MintInit(amount, tokenId);

            var initAccountBalance = GetBalanceTest(InitAccount, symbol, tokenId);
            var otherAccountBalance = GetBalanceTest(OtherAccount, symbol, tokenId);
            initAccountBalance.ShouldBe(100);
            otherAccountBalance.ShouldBe(0);

            var transferResult =
                _nftContract.ExecuteMethodWithResult(NftContractMethod.Transfer, new TransferInput
                {
                    To = OtherAccount.ConvertAddress(),
                    Symbol = symbol,
                    TokenId = tokenId,
                    Memo = "transfer",
                    Amount = 10
                });
            transferResult.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);
            initAccountBalance = GetBalanceTest(InitAccount, symbol, tokenId);
            otherAccountBalance = GetBalanceTest(OtherAccount, symbol, tokenId);
            initAccountBalance.ShouldBe(90);
            otherAccountBalance.ShouldBe(10);

            var addMinterResult =
                _nftContract.AddMinters(
                    new MinterList {Value = {InitAccount.ConvertAddress(), OtherAccount.ConvertAddress()}}, symbol);
            addMinterResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            var burnResult = _nftContract.Burn(symbol, tokenId, 90);
            burnResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            initAccountBalance = GetBalanceTest(InitAccount, symbol, tokenId);
            otherAccountBalance = GetBalanceTest(OtherAccount, symbol, tokenId);
            initAccountBalance.ShouldBe(0);
            otherAccountBalance.ShouldBe(10);
            CheckNftAndNftProtocolInfo(symbol, tokenId, new NFTInfo
                {
                    Symbol = symbol,
                    ProtocolName = "CAT",
                    TokenId = tokenId,
                    Creator = InitAccount.ConvertAddress(),
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
                    Quantity = 10,
                    Uri = string.Empty,
                    BaseUri = "aelf.com/nft/",
                    Alias = "NFT_CO_CAT1",
                    IsBurned = false,
                    NftType = NFTType.Collectables.ToString()
                },
                new NFTProtocolInfo
                {
                    Symbol = symbol,
                    Supply = 10,
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
            var amount = 100;
            var tokenId = 1;
            var symbol = MintInit(amount, tokenId);

            var initAccountBalance = GetBalanceTest(InitAccount, symbol, tokenId);
            var otherAccountBalance = GetBalanceTest(OtherAccount, symbol, tokenId);
            initAccountBalance.ShouldBe(100);
            otherAccountBalance.ShouldBe(0);

            var transferResult =
                _nftContract.ExecuteMethodWithResult(NftContractMethod.Transfer, new TransferInput
                {
                    To = OtherAccount.ConvertAddress(),
                    Symbol = symbol,
                    TokenId = tokenId,
                    Memo = "transfer",
                    Amount = initAccountBalance + 1
                });
            transferResult.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            transferResult.Error.ShouldContain("Insufficient balance.");
            initAccountBalance = GetBalanceTest(InitAccount, symbol, tokenId);
            otherAccountBalance = GetBalanceTest(OtherAccount, symbol, tokenId);
            initAccountBalance.ShouldBe(100);
            otherAccountBalance.ShouldBe(0);

            transferResult =
                _nftContract.ExecuteMethodWithResult(NftContractMethod.Transfer, new TransferInput
                {
                    To = OtherAccount.ConvertAddress(),
                    Symbol = symbol,
                    TokenId = tokenId,
                    Memo = "transfer",
                    Amount = -10
                });
            transferResult.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            transferResult.Error.ShouldContain("Invalid transfer amount.");
            initAccountBalance = GetBalanceTest(InitAccount, symbol, tokenId);
            otherAccountBalance = GetBalanceTest(OtherAccount, symbol, tokenId);
            initAccountBalance.ShouldBe(100);
            otherAccountBalance.ShouldBe(0);

            transferResult =
                _nftContract.ExecuteMethodWithResult(NftContractMethod.Transfer, new TransferInput
                {
                    To = OtherAccount.ConvertAddress(),
                    Symbol = symbol,
                    TokenId = tokenId,
                    Memo = "transfer",
                    Amount = 0
                });
            transferResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            initAccountBalance = GetBalanceTest(InitAccount, symbol, tokenId);
            otherAccountBalance = GetBalanceTest(OtherAccount, symbol, tokenId);
            initAccountBalance.ShouldBe(100);
            otherAccountBalance.ShouldBe(0);

            transferResult =
                _nftContract.ExecuteMethodWithResult(NftContractMethod.Transfer, new TransferInput
                {
                    To = OtherAccount.ConvertAddress(),
                    Symbol = symbol,
                    TokenId = tokenId,
                    Memo = "transfer",
                    Amount = 1
                });
            transferResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            transferResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            initAccountBalance = GetBalanceTest(InitAccount, symbol, tokenId);
            otherAccountBalance = GetBalanceTest(OtherAccount, symbol, tokenId);
            initAccountBalance.ShouldBe(99);
            otherAccountBalance.ShouldBe(1);

            transferResult =
                _nftContract.ExecuteMethodWithResult(NftContractMethod.Transfer, new TransferInput
                {
                    To = OtherAccount.ConvertAddress(),
                    Symbol = symbol,
                    TokenId = tokenId,
                    Memo = "transfer",
                    Amount = 99
                });
            transferResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            // Get balance
            initAccountBalance = GetBalanceTest(InitAccount, symbol, tokenId);
            otherAccountBalance = GetBalanceTest(OtherAccount, symbol, tokenId);
            initAccountBalance.ShouldBe(0);
            otherAccountBalance.ShouldBe(100);

            // Get balance by tokenHash
            var tokenHash = _nftContract.CalculateTokenHash(symbol, tokenId);
            Logger.Info($"tokenHash is {tokenHash}");
            initAccountBalance = GetBalanceByTokenHashTest(InitAccount, tokenHash);
            otherAccountBalance = GetBalanceByTokenHashTest(OtherAccount, tokenHash);
            initAccountBalance.ShouldBe(0);
            otherAccountBalance.ShouldBe(100);

            transferResult =
                _nftContract.ExecuteMethodWithResult(NftContractMethod.Transfer, new TransferInput
                {
                    To = OtherAccount.ConvertAddress(),
                    Symbol = symbol,
                    TokenId = tokenId,
                    Memo = "transfer",
                    Amount = 99
                });
            transferResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            initAccountBalance = GetBalanceTest(InitAccount, symbol, tokenId);
            otherAccountBalance = GetBalanceTest(OtherAccount, symbol, tokenId);
            initAccountBalance.ShouldBe(0);
            otherAccountBalance.ShouldBe(100);
        }

        [TestMethod]
        public void TransferFromTest()
        {
            var amount = 100;
            var tokenId = 1;
            var symbol = MintInit(amount, tokenId);

            var tokenHash = _nftContract.CalculateTokenHash(symbol, tokenId);
            Logger.Info($"tokenHash is {tokenHash}");
            var ownerBalance = GetBalanceByTokenHashTest(InitAccount, tokenHash);
            var spenderBalance = GetBalanceByTokenHashTest(OtherAccount, tokenHash);
            var toBalance = GetBalanceByTokenHashTest(OtherAccount1, tokenHash);
            ownerBalance.ShouldBe(100);
            spenderBalance.ShouldBe(0);
            toBalance.ShouldBe(0);

            _nftContract.SetAccount(OtherAccount);
            var transferResult =
                _nftContract.ExecuteMethodWithResult(NftContractMethod.TransferFrom, new TransferFromInput
                {
                    From = InitAccount.ConvertAddress(),
                    To = OtherAccount1.ConvertAddress(),
                    Symbol = symbol,
                    TokenId = tokenId,
                    Memo = "transferFrom",
                    Amount = -10
                });
            transferResult.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            transferResult.Error.ShouldContain("Invalid transfer amount.");

            transferResult =
                _nftContract.ExecuteMethodWithResult(NftContractMethod.TransferFrom, new TransferFromInput
                {
                    From = InitAccount.ConvertAddress(),
                    To = OtherAccount1.ConvertAddress(),
                    Symbol = symbol,
                    TokenId = tokenId,
                    Memo = "transferFrom",
                    Amount = 0
                });
            transferResult.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);
            ownerBalance = GetBalanceByTokenHashTest(InitAccount, tokenHash);
            spenderBalance = GetBalanceByTokenHashTest(OtherAccount, tokenHash);
            toBalance = GetBalanceByTokenHashTest(OtherAccount1, tokenHash);
            ownerBalance.ShouldBe(100);
            spenderBalance.ShouldBe(0);
            toBalance.ShouldBe(0);

            _nftContract.SetAccount(InitAccount);
            var approveResult = _nftContract.Approve(OtherAccount, symbol, tokenId, 50);
            approveResult.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);
            var allowance = _nftContract.GetAllowance(symbol, tokenId, InitAccount, OtherAccount).Allowance;
            Logger.Info($"allowance is {allowance}");
            allowance.ShouldBe(50);

            _nftContract.SetAccount(OtherAccount);
            transferResult =
                _nftContract.ExecuteMethodWithResult(NftContractMethod.TransferFrom, new TransferFromInput
                {
                    From = InitAccount.ConvertAddress(),
                    To = OtherAccount1.ConvertAddress(),
                    Symbol = symbol,
                    TokenId = tokenId,
                    Memo = "transferFrom",
                    Amount = allowance + 1
                });
            transferResult.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            transferResult.Error.ShouldContain("Not approved.");

            transferResult =
                _nftContract.ExecuteMethodWithResult(NftContractMethod.TransferFrom, new TransferFromInput
                {
                    From = InitAccount.ConvertAddress(),
                    To = OtherAccount1.ConvertAddress(),
                    Symbol = symbol,
                    TokenId = tokenId,
                    Memo = "transferFrom",
                    Amount = -10
                });
            transferResult.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            transferResult.Error.ShouldContain("Invalid transfer amount.");

            transferResult =
                _nftContract.ExecuteMethodWithResult(NftContractMethod.TransferFrom, new TransferFromInput
                {
                    From = InitAccount.ConvertAddress(),
                    To = OtherAccount1.ConvertAddress(),
                    Symbol = symbol,
                    TokenId = tokenId,
                    Memo = "transferFrom",
                    Amount = 0
                });
            transferResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            ownerBalance = GetBalanceByTokenHashTest(InitAccount, tokenHash);
            spenderBalance = GetBalanceByTokenHashTest(OtherAccount, tokenHash);
            toBalance = GetBalanceByTokenHashTest(OtherAccount1, tokenHash);
            ownerBalance.ShouldBe(100);
            spenderBalance.ShouldBe(0);
            toBalance.ShouldBe(0);

            allowance = _nftContract.GetAllowance(symbol, tokenId, InitAccount, OtherAccount).Allowance;
            allowance.ShouldBe(50);

            transferResult =
                _nftContract.ExecuteMethodWithResult(NftContractMethod.TransferFrom, new TransferFromInput
                {
                    From = InitAccount.ConvertAddress(),
                    To = OtherAccount1.ConvertAddress(),
                    Symbol = symbol,
                    TokenId = tokenId,
                    Memo = "transferFrom",
                    Amount = 1
                });
            transferResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            ownerBalance = GetBalanceByTokenHashTest(InitAccount, tokenHash);
            spenderBalance = GetBalanceByTokenHashTest(OtherAccount, tokenHash);
            toBalance = GetBalanceByTokenHashTest(OtherAccount1, tokenHash);
            ownerBalance.ShouldBe(99);
            spenderBalance.ShouldBe(0);
            toBalance.ShouldBe(1);

            transferResult =
                _nftContract.ExecuteMethodWithResult(NftContractMethod.TransferFrom, new TransferFromInput
                {
                    From = InitAccount.ConvertAddress(),
                    To = OtherAccount1.ConvertAddress(),
                    Symbol = symbol,
                    TokenId = tokenId,
                    Memo = "transferFrom",
                    Amount = 2
                });
            transferResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            ownerBalance = GetBalanceByTokenHashTest(InitAccount, tokenHash);
            spenderBalance = GetBalanceByTokenHashTest(OtherAccount, tokenHash);
            toBalance = GetBalanceByTokenHashTest(OtherAccount1, tokenHash);
            ownerBalance.ShouldBe(97);
            spenderBalance.ShouldBe(0);
            toBalance.ShouldBe(3);

            _nftContract.SetAccount(InitAccount);
            approveResult = _nftContract.Approve(OtherAccount, symbol, tokenId, 200);
            approveResult.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);
            allowance = _nftContract.GetAllowance(symbol, tokenId, InitAccount, OtherAccount).Allowance;
            allowance.ShouldBe(200);

            _nftContract.SetAccount(OtherAccount);
            transferResult =
                _nftContract.ExecuteMethodWithResult(NftContractMethod.TransferFrom, new TransferFromInput
                {
                    From = InitAccount.ConvertAddress(),
                    To = OtherAccount1.ConvertAddress(),
                    Symbol = symbol,
                    TokenId = tokenId,
                    Memo = "transferFrom",
                    Amount = ownerBalance + 1
                });
            transferResult.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            transferResult.Error.ShouldContain("Insufficient balance.");
            ownerBalance = GetBalanceByTokenHashTest(InitAccount, tokenHash);
            spenderBalance = GetBalanceByTokenHashTest(OtherAccount, tokenHash);
            toBalance = GetBalanceByTokenHashTest(OtherAccount1, tokenHash);
            ownerBalance.ShouldBe(97);
            spenderBalance.ShouldBe(0);
            toBalance.ShouldBe(3);

            transferResult =
                _nftContract.ExecuteMethodWithResult(NftContractMethod.TransferFrom, new TransferFromInput
                {
                    From = InitAccount.ConvertAddress(),
                    To = OtherAccount1.ConvertAddress(),
                    Symbol = symbol,
                    TokenId = tokenId,
                    Memo = "transferFrom",
                    Amount = 97
                });
            transferResult.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);
            ownerBalance = GetBalanceByTokenHashTest(InitAccount, tokenHash);
            spenderBalance = GetBalanceByTokenHashTest(OtherAccount, tokenHash);
            toBalance = GetBalanceByTokenHashTest(OtherAccount1, tokenHash);
            ownerBalance.ShouldBe(0);
            spenderBalance.ShouldBe(0);
            toBalance.ShouldBe(100);
        }

        [TestMethod]
        public void ApproveUnApproveTest()
        {
            var amount = 100;
            var tokenId = 1;
            var symbol = MintInit(amount, tokenId);

            var allowance = _nftContract.GetAllowance(symbol, tokenId, InitAccount, OtherAccount).Allowance;
            allowance.ShouldBe(0);
            var balance = GetBalanceTest(InitAccount, symbol, tokenId);
            balance.ShouldBe(100);

            var approveResult = _nftContract.Approve(OtherAccount, symbol, tokenId, 200);
            approveResult.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);
            allowance = _nftContract.GetAllowance(symbol, tokenId, InitAccount, OtherAccount).Allowance;
            allowance.ShouldBe(200);

            approveResult = _nftContract.Approve(OtherAccount, symbol, tokenId, 90);
            approveResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            allowance = _nftContract.GetAllowance(symbol, tokenId, InitAccount, OtherAccount).Allowance;
            allowance.ShouldBe(90);

            var unApproveResult = _nftContract.UnApprove(OtherAccount, symbol, tokenId, 100);
            unApproveResult.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);
            allowance = _nftContract.GetAllowance(symbol, tokenId, InitAccount, OtherAccount).Allowance;
            allowance.ShouldBe(0);

            approveResult = _nftContract.Approve(OtherAccount, symbol, tokenId, 10);
            approveResult.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);
            allowance = _nftContract.GetAllowance(symbol, tokenId, InitAccount, OtherAccount).Allowance;
            allowance.ShouldBe(10);

            unApproveResult = _nftContract.UnApprove(OtherAccount, symbol, tokenId, 10);
            unApproveResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            allowance = _nftContract.GetAllowance(symbol, tokenId, InitAccount, OtherAccount).Allowance;
            allowance.ShouldBe(0);

            unApproveResult = _nftContract.UnApprove(OtherAccount, symbol, tokenId, 0);
            unApproveResult.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);
            allowance = _nftContract.GetAllowance(symbol, tokenId, InitAccount, OtherAccount).Allowance;
            allowance.ShouldBe(0);

            unApproveResult = _nftContract.UnApprove(OtherAccount, symbol, tokenId, 1);
            unApproveResult.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);
            allowance = _nftContract.GetAllowance(symbol, tokenId, InitAccount, OtherAccount).Allowance;
            allowance.ShouldBe(0);
        }

        [TestMethod]
        public void AssembleDisassembleTest()
        {
            // Contract:28wZQtdGMjPmUNvVusr7VqV2XjavTiL5HVqHayrp9q3GL1Qhpb
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
                    IsTokenIdReuse = false
                });
            createResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var symbol = StringValue.Parser.ParseFrom(ByteArrayHelper.HexStringToByteArray(createResult.ReturnValue))
                .Value;

            var tokenSymbol1 = MintInit(100, 1);
            var tokenSymbol2 = MintInit(100, 2);
            var tokenHash1 = _nftContract.CalculateTokenHash(tokenSymbol1, 1);
            var tokenHash2 = _nftContract.CalculateTokenHash(tokenSymbol2, 2);
            var tokenHash = _nftContract.CalculateTokenHash(symbol, 3);
            Logger.Info($"tokenHash1 is {tokenHash1}");
            Logger.Info($"tokenHash2 is {tokenHash2}");
            Logger.Info($"tokenHash is {tokenHash}");

            var initBalanceBeforeSymbol1 = GetBalanceByTokenHashTest(InitAccount, tokenHash1);
            var initBalanceBeforeSymbol2 = GetBalanceByTokenHashTest(InitAccount, tokenHash2);
            var otherBalanceBeforeSymbol = GetBalanceByTokenHashTest(OtherAccount, tokenHash);
            var initBalanceBeforeElf = GetTokenBalance(InitAccount, "ELF");

            var result =
                _nftContract.ExecuteMethodWithResult(NftContractMethod.Assemble, new AssembleInput
                {
                    Symbol = symbol,
                    Owner = OtherAccount.ConvertAddress(),
                    Uri = "uri_assemble",
                    Alias = "alias_assemble",
                    Metadata = new Metadata
                    {
                        Value =
                        {
                            {"assemble", "metadata_assemble"}
                        }
                    },
                    AssembledNfts = new AssembledNfts
                    {
                        Value =
                        {
                            {tokenHash1.ToHex(), 1},
                            {tokenHash2.ToHex(), 2},
                        }
                    },
                    AssembledFts = new AssembledFts
                    {
                        Value =
                        {
                            {"ELF", 100}
                        }
                    },
                    TokenId = 1
                });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
            result.Error.ShouldContain("Insufficient allowance of ELF");

            // Approve
            var approve =
                _tokenContract.ApproveToken(InitAccount, _nftContract.ContractAddress, 10000_00000000, "ELF");
            approve.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            // AssembledFts amount is more than token balance
            result =
                _nftContract.ExecuteMethodWithResult(NftContractMethod.Assemble, new AssembleInput
                {
                    Symbol = symbol,
                    Owner = OtherAccount.ConvertAddress(),
                    Uri = "uri_assemble",
                    Alias = "alias_assemble",
                    Metadata = new Metadata
                    {
                        Value =
                        {
                            {"assemble", "metadata_assemble"}
                        }
                    },
                    AssembledNfts = new AssembledNfts
                    {
                        Value =
                        {
                            {tokenHash1.ToHex(), 200},
                            {tokenHash2.ToHex(), 2},
                        }
                    },
                    AssembledFts = new AssembledFts
                    {
                        Value =
                        {
                            {"ELF", 100}
                        }
                    },
                    TokenId = 1
                });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
            result.Error.ShouldContain("Insufficient balance of " + $"{tokenSymbol1}1");

            // AssembledFts amount is more than token balance
            result =
                _nftContract.ExecuteMethodWithResult(NftContractMethod.Assemble, new AssembleInput
                {
                    Symbol = symbol,
                    Owner = OtherAccount.ConvertAddress(),
                    Uri = "uri_assemble",
                    Alias = "alias_assemble",
                    Metadata = new Metadata
                    {
                        Value =
                        {
                            {"assemble", "metadata_assemble"}
                        }
                    },
                    AssembledNfts = new AssembledNfts
                    {
                        Value =
                        {
                            {tokenHash1.ToHex(), 1},
                            {tokenHash2.ToHex(), 2},
                        }
                    },
                    AssembledFts = new AssembledFts
                    {
                        Value =
                        {
                            {"ELF", initBalanceBeforeElf + 1}
                        }
                    },
                    TokenId = 1
                });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
            result.Error.ShouldContain("Insufficient balance of ELF");

            result =
                _nftContract.ExecuteMethodWithResult(NftContractMethod.Assemble, new AssembleInput
                {
                    Symbol = symbol,
                    Owner = OtherAccount.ConvertAddress(),
                    Uri = "uri_assemble",
                    Alias = "alias_assemble",
                    Metadata = new Metadata
                    {
                        Value =
                        {
                            {"assemble", "metadata_assemble"},
                            {"aelf_nft_type", "Collectables"}
                        }
                    },
                    AssembledNfts = new AssembledNfts
                    {
                        Value =
                        {
                            {tokenHash1.ToHex(), 1},
                            {tokenHash2.ToHex(), 2},
                        }
                    },
                    AssembledFts = new AssembledFts
                    {
                        Value =
                        {
                            {"ELF", initBalanceBeforeElf + 1}
                        }
                    },
                    TokenId = 1
                });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
            result.Error.ShouldContain("Metadata key aelf_nft_type is reserved.");

            // Assemble After approve
            result =
                _nftContract.ExecuteMethodWithResult(NftContractMethod.Assemble, new AssembleInput
                {
                    Symbol = symbol,
                    Owner = OtherAccount.ConvertAddress(),
                    Uri = "uri_assemble",
                    Alias = "alias_assemble",
                    Metadata = new Metadata
                    {
                        Value =
                        {
                            {"assemble", "metadata_assemble"}
                        }
                    },
                    AssembledNfts = new AssembledNfts
                    {
                        Value =
                        {
                            {tokenHash1.ToHex(), 1},
                            {tokenHash2.ToHex(), 2},
                        }
                    },
                    AssembledFts = new AssembledFts
                    {
                        Value =
                        {
                            {"ELF", 100}
                        }
                    },
                    TokenId = 3
                });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            var initBalanceAfterSymbol1 = GetBalanceByTokenHashTest(InitAccount, tokenHash1);
            var initBalanceAfterSymbol2 = GetBalanceByTokenHashTest(InitAccount, tokenHash2);
            var otherBalanceAfterSymbol = GetBalanceByTokenHashTest(OtherAccount, tokenHash);
            (initBalanceBeforeSymbol1 - initBalanceAfterSymbol1).ShouldBe(1);
            (initBalanceBeforeSymbol2 - initBalanceAfterSymbol2).ShouldBe(2);
            (otherBalanceAfterSymbol - otherBalanceBeforeSymbol).ShouldBe(1);

            CheckNftInfo(tokenHash, new NFTInfo
            {
                Symbol = symbol,
                ProtocolName = "CAT",
                TokenId = 3,
                Creator = InitAccount.ConvertAddress(),
                Minters = {InitAccount.ConvertAddress()},
                Quantity = 1,
                BaseUri = "aelf.com/nft/",
                Uri = "uri_assemble",
                Alias = "alias_assemble",
                IsBurned = false,
                NftType = NFTType.Collectables.ToString()
            });

            result =
                _nftContract.ExecuteMethodWithResult(NftContractMethod.Assemble, new AssembleInput
                {
                    Symbol = tokenSymbol1,
                    Owner = OtherAccount.ConvertAddress(),
                    Uri = "uri_assemble",
                    Alias = "alias_assemble",
                    Metadata = new Metadata
                    {
                        Value =
                        {
                            {"assemble", "metadata_assemble"}
                        }
                    },
                    AssembledNfts = new AssembledNfts
                    {
                        Value =
                        {
                            {tokenHash1.ToHex(), 1},
                            {tokenHash2.ToHex(), 2},
                        }
                    },
                    AssembledFts = new AssembledFts
                    {
                        Value =
                        {
                            {"ELF", 100}
                        }
                    },
                    TokenId = 1
                });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
            result.Error.ShouldContain("Token id 1 already exists. Please assign a different token id.");

            var addMinter =
                _nftContract.AddMinters(
                    new MinterList {Value = {OtherAccount.ConvertAddress()}}, symbol);
            addMinter.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var minterList = _nftContract.GetMinterList(symbol);
            Logger.Info($"minterList is {minterList}");
            var initBalanceAfterElf = GetTokenBalance(InitAccount, "ELF");
            Logger.Info($"initBalanceAfterElf is {initBalanceAfterElf}");

            _nftContract.SetAccount(OtherAccount);
            var disassembleResult =
                _nftContract.ExecuteMethodWithResult(NftContractMethod.Disassemble, new DisassembleInput
                {
                    Symbol = symbol,
                    TokenId = 3,
                    Owner = InitAccount.ConvertAddress()
                });
            disassembleResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            var initBalanceAfterDisassembleSymbol1 = GetBalanceByTokenHashTest(InitAccount, tokenHash1);
            var initBalanceAfterDisassembleSymbol2 = GetBalanceByTokenHashTest(InitAccount, tokenHash2);
            var otherBalanceAfterDisassembleSymbol = GetBalanceByTokenHashTest(OtherAccount, tokenHash);
            var initBalanceAfterDisassembleElf = GetTokenBalance(InitAccount, "ELF");
            Logger.Info($"initBalanceAfterDisassembleSymbol1 is {initBalanceAfterDisassembleSymbol1}");
            Logger.Info($"initBalanceAfterDisassembleSymbol2 is {initBalanceAfterDisassembleSymbol2}");
            Logger.Info($"otherBalanceAfterDisassembleSymbol is {otherBalanceAfterDisassembleSymbol}");
            Logger.Info($"initBalanceAfterDisassembleElf is {initBalanceAfterDisassembleElf}");
            initBalanceAfterDisassembleSymbol1.ShouldBe(initBalanceBeforeSymbol1);
            initBalanceAfterDisassembleSymbol2.ShouldBe(initBalanceBeforeSymbol2);
            otherBalanceAfterDisassembleSymbol.ShouldBe(0);
            initBalanceAfterDisassembleElf.ShouldBe(initBalanceAfterElf + 100);

            CheckNftInfo(tokenHash, new NFTInfo
            {
                Symbol = symbol,
                ProtocolName = "CAT",
                TokenId = 3,
                Creator = InitAccount.ConvertAddress(),
                Minters = {InitAccount.ConvertAddress()},
                Quantity = 0,
                BaseUri = "aelf.com/nft/",
                Uri = "uri_assemble",
                Alias = "alias_assemble",
                IsBurned = true,
                NftType = NFTType.Collectables.ToString()
            });

            disassembleResult =
                _nftContract.ExecuteMethodWithResult(NftContractMethod.Disassemble, new DisassembleInput
                {
                    Symbol = symbol,
                    TokenId = 3
                });
            disassembleResult.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            disassembleResult.Error.ShouldContain("No permission.");
        }

        [TestMethod]
        public void RecastTest()
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

            var amount = 1;
            var tokenId = 1;
            var mintResult =
                _nftContract.ExecuteMethodWithResult(NftContractMethod.Mint, new MintInput
                {
                    Symbol = symbol,
                    Alias = "NFT_CO_CAT1",
                    Metadata = new Metadata
                    {
                        Value =
                        {
                            {"other1", "other"}
                        }
                    },
                    Quantity = amount
                });
            mintResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            var recastResult =
                _nftContract.ExecuteMethodWithResult(NftContractMethod.Recast, new RecastInput
                {
                    Symbol = symbol,
                    TokenId = tokenId,
                    Uri = "uri_recast",
                    Alias = "alias_recast",
                    Metadata = new Metadata
                    {
                        Value =
                        {
                            {"other1", "metadata_recast1"},
                            {"other2", "metadata_recast2"},
                        }
                    },
                });
            recastResult.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);
            CheckNftAndNftProtocolInfo(symbol, tokenId, new NFTInfo
                {
                    Symbol = symbol,
                    ProtocolName = "CAT",
                    TokenId = tokenId,
                    Creator = InitAccount.ConvertAddress(),
                    Minters = {InitAccount.ConvertAddress()},
                    Metadata = new Metadata
                    {
                        Value =
                        {
                            {"aelf_nft_type", "Collectables"},
                            {"aelf_nft_base_uri", "aelf.com/nft/"},
                            {"aelf_nft_token_id_reuse", "True"},
                            {"other1", "metadata_recast1"},
                            {"other2", "metadata_recast2"},
                        }
                    },
                    Quantity = amount,
                    Uri = "uri_recast",
                    BaseUri = "aelf.com/nft/",
                    Alias = "alias_recast",
                    IsBurned = false,
                    NftType = NFTType.Collectables.ToString()
                },
                new NFTProtocolInfo
                {
                    Symbol = symbol,
                    Supply = amount,
                    TotalSupply = 10000,
                    Creator = InitAccount.ConvertAddress(),
                    BaseUri = "aelf.com/nft/",
                    IsBurnable = true,
                    IssueChainId = _chainId,
                    NftType = NFTType.Collectables.ToString(),
                    ProtocolName = "CAT",
                    IsTokenIdReuse = true
                });

            recastResult =
                _nftContract.ExecuteMethodWithResult(NftContractMethod.Recast, new RecastInput
                {
                    Symbol = symbol,
                    TokenId = tokenId,
                    Uri = "uri_recast",
                    Alias = "alias_recast",
                    Metadata = new Metadata
                    {
                        Value =
                        {
                            {"other1", "metadata_recast1"},
                            {"other2", "metadata_recast2"},
                        }
                    },
                });
            recastResult.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);

            amount = 3;
            tokenId = 2;
            mintResult =
                _nftContract.ExecuteMethodWithResult(NftContractMethod.Mint, new MintInput
                {
                    Symbol = symbol,
                    Uri = "uri_CAT1",
                    Alias = "NFT_CO_CAT1",
                    Metadata = new Metadata
                    {
                        Value =
                        {
                            {"other", "metadata_recast"}
                        }
                    },
                    Quantity = amount,
                    TokenId = tokenId
                });
            mintResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            var transferResult =
                _nftContract.ExecuteMethodWithResult(NftContractMethod.Transfer, new TransferInput
                {
                    To = OtherAccount.ConvertAddress(),
                    Symbol = symbol,
                    TokenId = tokenId,
                    Memo = "transfer",
                    Amount = 1
                });
            transferResult.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);
            var initBalance = GetBalanceTest(InitAccount, symbol, tokenId);
            var otherBalance = GetBalanceTest(OtherAccount, symbol, tokenId);
            initBalance.ShouldBe(2);
            otherBalance.ShouldBe(1);

            recastResult =
                _nftContract.ExecuteMethodWithResult(NftContractMethod.Recast, new RecastInput
                {
                    Symbol = symbol,
                    TokenId = tokenId,
                    Metadata = new Metadata
                    {
                        Value =
                        {
                            {"other", "metadata_recast"}
                        }
                    }
                });
            recastResult.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            recastResult.Error.ShouldContain("Do not support recast.");

            _nftContract.SetAccount(OtherAccount);
            transferResult =
                _nftContract.ExecuteMethodWithResult(NftContractMethod.Transfer, new TransferInput
                {
                    To = InitAccount.ConvertAddress(),
                    Symbol = symbol,
                    TokenId = tokenId,
                    Memo = "transfer",
                    Amount = 1
                });
            transferResult.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);
            initBalance = GetBalanceTest(InitAccount, symbol, tokenId);
            otherBalance = GetBalanceTest(OtherAccount, symbol, tokenId);
            initBalance.ShouldBe(3);
            otherBalance.ShouldBe(0);

            _nftContract.SetAccount(InitAccount);
            recastResult =
                _nftContract.ExecuteMethodWithResult(NftContractMethod.Recast, new RecastInput
                {
                    Symbol = symbol,
                    TokenId = tokenId,
                    Metadata = new Metadata()
                });
            recastResult.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);
            CheckNftAndNftProtocolInfo(symbol, tokenId, new NFTInfo
                {
                    Symbol = symbol,
                    ProtocolName = "CAT",
                    TokenId = tokenId,
                    Creator = InitAccount.ConvertAddress(),
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
                    Quantity = amount,
                    BaseUri = "aelf.com/nft/",
                    Uri = string.Empty,
                    Alias = String.Empty,
                    IsBurned = false,
                    NftType = NFTType.Collectables.ToString()
                },
                new NFTProtocolInfo
                {
                    Symbol = symbol,
                    Supply = 4,
                    TotalSupply = 10000,
                    Creator = InitAccount.ConvertAddress(),
                    BaseUri = "aelf.com/nft/",
                    IsBurnable = true,
                    IssueChainId = _chainId,
                    NftType = NFTType.Collectables.ToString(),
                    ProtocolName = "CAT",
                    IsTokenIdReuse = true
                });

            amount = 1;
            tokenId = 3;
            mintResult =
                _nftContract.ExecuteMethodWithResult(NftContractMethod.Mint, new MintInput
                {
                    Symbol = symbol,
                    Alias = "NFT_CO_CAT1",
                    Metadata = new Metadata(),
                    Quantity = amount,
                    TokenId = tokenId
                });
            mintResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            _nftContract.SetAccount(OtherAccount);
            recastResult =
                _nftContract.ExecuteMethodWithResult(NftContractMethod.Recast, new RecastInput
                {
                    Symbol = symbol,
                    TokenId = tokenId,
                    Uri = "uri_recast",
                    Alias = "alias_recast",
                    Metadata = new Metadata
                    {
                        Value =
                        {
                            {"other", "metadata_recast"},
                        }
                    }
                });
            recastResult.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            recastResult.Error.ShouldContain("No permission.");
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

        private void CheckNftInfo(Hash tokenHash, NFTInfo expectNftInfo)
        {
            var nftInfo = _nftContract.GetNftInfoByTokenHash(tokenHash);
            Logger.Info($"nftInfo.Symbol is {nftInfo.Symbol}");
            Logger.Info($"nftInfo.ProtocolName is {nftInfo.ProtocolName}");
            Logger.Info($"nftInfo.TokenId is {nftInfo.TokenId}");
            Logger.Info($"nftInfo.Creator is {nftInfo.Creator}");
            foreach (var minters in nftInfo.Minters)
            {
                Logger.Info($"nftInfo.Minters is {minters.ToBase58()}");
            }

            Logger.Info($"nftInfo.Metadata is {nftInfo.Metadata}");
            Logger.Info($"nftInfo.Quantity is {nftInfo.Quantity}");
            Logger.Info($"nftInfo.Uri is {nftInfo.Uri}");
            Logger.Info($"nftInfo.BaseUri is {nftInfo.BaseUri}");
            Logger.Info($"nftInfo.Alias is {nftInfo.Alias}");
            Logger.Info($"nftInfo.IsBurned is {nftInfo.IsBurned}");
            Logger.Info($"nftInfo.NftType is {nftInfo.NftType}");

            nftInfo.Symbol.ShouldBe(expectNftInfo.Symbol);
            nftInfo.ProtocolName.ShouldBe(expectNftInfo.ProtocolName);
            nftInfo.TokenId.ShouldBe(expectNftInfo.TokenId);
            nftInfo.Creator.ShouldBe(expectNftInfo.Creator);
            nftInfo.Minters.ShouldBe(expectNftInfo.Minters);
            nftInfo.Quantity.ShouldBe(expectNftInfo.Quantity);
            nftInfo.Uri.ShouldBe(expectNftInfo.Uri);
            nftInfo.BaseUri.ShouldBe(expectNftInfo.BaseUri);
            nftInfo.Alias.ShouldBe(expectNftInfo.Alias);
            nftInfo.IsBurned.ShouldBe(expectNftInfo.IsBurned);
            nftInfo.NftType.ShouldBe(expectNftInfo.NftType);
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

        private long GetTokenBalance(string account, string symbol)
        {
            _genesisContract = GenesisContract.GetGenesisContract(NodeManager, account);
            _tokenContract = _genesisContract.GetTokenContract(account);
            var balance = _tokenContract.GetUserBalance(account, symbol);
            Logger.Info($"balance of {symbol} is {balance}");
            return balance;
        }

        private string MintInit(long amount, long tokenId)
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
    }
}