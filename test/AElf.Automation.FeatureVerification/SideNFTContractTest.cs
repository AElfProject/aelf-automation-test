using System.Collections.Generic;
using System.Linq;
using AElf.Contracts.MultiToken;
using AElf.Contracts.NFT;
using AElf.CSharp.Core;
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
using ApproveInput = AElf.Contracts.NFT.ApproveInput;
using Burned = AElf.Contracts.NFT.Burned;
using BurnInput = AElf.Contracts.NFT.BurnInput;
using CreateInput = AElf.Contracts.NFT.CreateInput;
using TransferFromInput = AElf.Contracts.NFT.TransferFromInput;
using TransferInput = AElf.Contracts.NFT.TransferInput;
using UnApproveInput = AElf.Contracts.NFT.UnApproveInput;

namespace AElf.Automation.Contracts.ScenarioTest
{
    [TestClass]
    public class NFTContractTest
    {
        private static readonly ILog Logger = Log4NetHelper.GetLogger();
        private TokenContract _token;
        private TokenContract _sideToken;
        private NftContract _mainNftContract;
        private NftContract _sideNftContract;
        private INodeManager NodeManager { get; set; }
        private INodeManager SideNodeManger { get; set; }
        private AuthorityManager _authorityManager;
        private AuthorityManager _sideAuthorityManager;
        private CrossChainManager CrossChainManager { get; set; }
        private string InitAccount { get; } = "nn659b9X1BLhnu5RWmEUbuuV7J9QKVVSN54j9UmeCbF3Dve5D";
        private string NewMinter { get; } = "YUW9zH5GhRboT5JK4vXp5BLAfCDv28rRmTQwo418FuaJmkSg8";
        private string Operator { get; } = "2LQe1sGesK5qFnLnGWowT5uJ1Z2g161wt1s54RY5BM157yJ7kp";
        private static string RpcUrl { get; } = "http://192.168.67.166:8000";

        private static string SideRpcUrl { get; } = "http://192.168.66.225:8000";

        //2LUmicHyH4RXrMjG4beDwuDsiWJESyLkgkwPdGTR8kahRzq5XS
        //RXcxgSXuagn8RrvhQAV81Z652EEYSwR6JLnqHYJ5UVpEptW8Y
        private string NftAddress = "2NxwCPAGJr4knVdmwhb1cK7CkZw5sMJkRDLnT7E2GoDP2dy5iZ";
        private string SideNftAddress = "2YkKkNZKCcsfUsGwCfJ6wyTx5NYLgpCg1stBuRT4z5ep3psXNG";

        //2LQe1sGesK5qFnLnGWowT5uJ1Z2g161wt1s54RY5BM157yJ7kp
        //2qkM2r145n8tQr6ADxGEJSBpFpHY1nyY6WqVnLNEpbXEgJFWvA
        //tPkiSRLTYQ8n4kqgy6nDFQb7toKH4BEpm8NtyLyQGEooZzH9y
        //2HnvUWNzKG6DbRhtrDgSwEfqA2YeHEhmLLnTAXKwzMBbJxEhUr、
        //3irnFpjyNS6B1Wxuf8V8heRQDmLUfCypRDxAWD4qKB31BjaNT

        [TestInitialize]
        public void Initialize()
        {
            #region Basic Preparation

            //Init Logger
            Log4NetHelper.LogInit("NFTTest_");
            NodeInfoHelper.SetConfig("nodes-new-env-main");

            #endregion

            NodeManager = new NodeManager(RpcUrl);
            SideNodeManger = new NodeManager(SideRpcUrl);
            _authorityManager = new AuthorityManager(NodeManager, InitAccount);
            _sideAuthorityManager = new AuthorityManager(SideNodeManger, InitAccount);
            CrossChainManager = new CrossChainManager(NodeManager, SideNodeManger, InitAccount);
            var mainService = new ContractManager(NodeManager, InitAccount);
            var sideService = new ContractManager(SideNodeManger, InitAccount);

            _token = mainService.Token;
            _sideToken = sideService.Token;

            _mainNftContract = NftAddress == ""
                ? new NftContract(NodeManager, InitAccount)
                : new NftContract(NodeManager, NftAddress, InitAccount);
            _sideNftContract = SideNftAddress == ""
                ? new NftContract(SideNodeManger, InitAccount)
                : new NftContract(SideNodeManger, SideNftAddress, InitAccount);
        }

        [TestMethod]
        public void AddAddressToCreateTokenWhiteList()
        {
            var check = _token.IsInCreateTokenWhiteList(_mainNftContract.ContractAddress);
            if (check) return;
            var result = _authorityManager.ExecuteTransactionWithAuthority(_token.ContractAddress,
                "AddAddressToCreateTokenWhiteList", _mainNftContract.Contract, InitAccount);
            result.Status.ShouldBe(TransactionResultStatus.Mined);
            check = _token.IsInCreateTokenWhiteList(_mainNftContract.ContractAddress);
            check.ShouldBeTrue();
        }
        
        [TestMethod]
        public void CreateNftToken()
        {
            var chainId = ChainHelper.ConvertBase58ToChainId(SideNodeManger.GetChainId());
            var baseUrl = "ipfs://aelf/";
            var protocolName = "VirtualWorlds";
            var totalSupply = 20000;
            var getType = _mainNftContract.GetNftTypes();
            var result = _mainNftContract.ExecuteMethodWithResult(NftContractMethod.Create, new CreateInput
            {
                NftType = NFTType.VirtualWorlds.ToString(),
                BaseUri = baseUrl,
                Creator = InitAccount.ConvertAddress(),
                IsBurnable = true,
                IssueChainId = chainId,
                ProtocolName = protocolName,
                TotalSupply = totalSupply,
                IsTokenIdReuse = false,
                Metadata = new Metadata
                {
                    Value =
                    {
                        {"Description", "VirtualWorlds test"}
                    }
                }
            });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var nftSymbol = StringValue.Parser.ParseFrom(ByteArrayHelper.HexStringToByteArray(result.ReturnValue))
                .Value;
            nftSymbol.Length.ShouldBe(11);
            Logger.Info(nftSymbol);
            var logs = result.Logs.First(l => l.Name.Equals("NFTProtocolCreated")).NonIndexed;
            var nftProtocolCreated = NFTProtocolCreated.Parser.ParseFrom(ByteString.FromBase64(logs));
            nftProtocolCreated.Creator.ShouldBe(InitAccount.ConvertAddress());
            nftProtocolCreated.Symbol.ShouldBe(nftSymbol);
            nftProtocolCreated.TotalSupply.ShouldBe(totalSupply);
            nftProtocolCreated.IssueChainId.ShouldBe(chainId);
            nftProtocolCreated.IsTokenIdReuse.ShouldBeFalse();
            nftProtocolCreated.BaseUri.ShouldBe(baseUrl);
            nftProtocolCreated.ProtocolName.ShouldBe(protocolName);

            var GetNftProtocolInfo = _mainNftContract.GetNftProtocolInfo(nftSymbol);
            GetNftProtocolInfo.Creator.ShouldBe(InitAccount.ConvertAddress());
            GetNftProtocolInfo.Symbol.ShouldBe(nftSymbol);
            GetNftProtocolInfo.TotalSupply.ShouldBe(totalSupply);
            GetNftProtocolInfo.IssueChainId.ShouldBe(chainId);
            GetNftProtocolInfo.IsTokenIdReuse.ShouldBeFalse();
            GetNftProtocolInfo.BaseUri.ShouldBe(baseUrl);
            GetNftProtocolInfo.ProtocolName.ShouldBe(protocolName);
        }

        [TestMethod]
        public void CreateOnSide()
        {
            var chainId = ChainHelper.ConvertBase58ToChainId(SideNodeManger.GetChainId());
            var baseUrl = "ipfs://aelf/";
            var protocolName = "art test";
            var totalSupply = 100000;
            var result = _sideNftContract.ExecuteMethodWithResult(NftContractMethod.Create, new CreateInput
            {
                NftType = "AR",
                BaseUri = baseUrl,
                Creator = InitAccount.ConvertAddress(),
                IsBurnable = true,
                IssueChainId = chainId,
                ProtocolName = protocolName,
                TotalSupply = totalSupply,
                IsTokenIdReuse = false,
                Metadata = new Metadata
                {
                    Value =
                    {
                        {"Description", "Test ART Type"}
                    }
                }
            });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
            result.Error.ShouldContain("NFT Protocol can only be created at aelf mainchain.");
        }

        [TestMethod]
        public void CrossChainCreate()
        {
            var symbol = "VW515164933";
            // side chain verify nft token
            var tokenInfo = _sideToken.GetTokenInfo(symbol);
            if (tokenInfo.Equals(new TokenInfo()))
            {
                var validateResult = CrossChainManager.ValidateTokenSymbol(symbol, out string raw);
                var createResult = CrossChainManager.CrossChainCreate(validateResult, raw);
                createResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            }

            var mainNftProtocolInfo = _mainNftContract.GetNftProtocolInfo(symbol);

            //side chain create nft token

            var result = _sideNftContract.ExecuteMethodWithResult(NftContractMethod.CrossChainCreate,
                new CrossChainCreateInput
                {
                    Symbol = symbol
                });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var logs = result.Logs.First(l => l.Name.Equals("NFTProtocolCreated")).NonIndexed;
            var nftProtocolCreated = NFTProtocolCreated.Parser.ParseFrom(ByteString.FromBase64(logs));
            nftProtocolCreated.Creator.ShouldBe(InitAccount.ConvertAddress());
            nftProtocolCreated.Symbol.ShouldBe(symbol);
            nftProtocolCreated.TotalSupply.ShouldBe(mainNftProtocolInfo.TotalSupply);
            nftProtocolCreated.IssueChainId.ShouldBe(mainNftProtocolInfo.IssueChainId);
            nftProtocolCreated.IsTokenIdReuse.ShouldBeFalse();
            nftProtocolCreated.BaseUri.ShouldBe(mainNftProtocolInfo.BaseUri);
            nftProtocolCreated.ProtocolName.ShouldBe(mainNftProtocolInfo.ProtocolName);

            var GetNftProtocolInfo = _sideNftContract.GetNftProtocolInfo(symbol);
            GetNftProtocolInfo.Creator.ShouldBe(InitAccount.ConvertAddress());
            GetNftProtocolInfo.Symbol.ShouldBe(symbol);
            GetNftProtocolInfo.TotalSupply.ShouldBe(mainNftProtocolInfo.TotalSupply);
            GetNftProtocolInfo.IssueChainId.ShouldBe(mainNftProtocolInfo.IssueChainId);
            GetNftProtocolInfo.IsTokenIdReuse.ShouldBeFalse();
            GetNftProtocolInfo.BaseUri.ShouldBe(mainNftProtocolInfo.BaseUri);
            GetNftProtocolInfo.ProtocolName.ShouldBe(mainNftProtocolInfo.ProtocolName);
        }

        [TestMethod]
        public void CrossChainCreate_ErrorTest()
        {
            var symbol = "MO727117725";
            var tokenInfo = _sideToken.GetTokenInfo(symbol);
            {
                //"Token info {symbol} not exists."
                //side chain create nft token before cross create token
                var result = _sideNftContract.ExecuteMethodWithResult(NftContractMethod.CrossChainCreate,
                    new CrossChainCreateInput
                    {
                        Symbol = symbol
                    });
                result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
                result.Error.ShouldContain($"Token info {symbol} not exists.");
            }
            {
                //"Full name of {nftTypeShortName} not found. Use AddNFTType to add this new pair."
                //side chain create nft token, token type didn't create on side chain/already remove
                var result = _sideNftContract.ExecuteMethodWithResult(NftContractMethod.CrossChainCreate,
                    new CrossChainCreateInput
                    {
                        Symbol = symbol
                    });
                result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
                result.Error.ShouldContain($"Full name of MO not found. Use AddNFTType to add this new pair.");
            }
        }

        [TestMethod]
        public void AddNftTypeOnMain()
        {
            var shorName = "GA";
            var fullName = "game";
            var input = new AddNFTTypeInput
            {
                FullName = fullName,
                ShortName = shorName
            };
            var types = _sideNftContract.GetNftTypes();
            Logger.Info(types.Value);
            var result =
                _authorityManager.ExecuteTransactionWithAuthority(_mainNftContract.ContractAddress, "AddNFTType", input,
                    InitAccount);
            result.Status.ShouldBe(TransactionResultStatus.Mined);
            var afterTypes = _sideNftContract.GetNftTypes();
            afterTypes.Value[shorName] = fullName;
        }

        [TestMethod]
        public void AddNftType()
        {
            var shortName = "GA";
            var fullName = "game";

            {
                var input = new AddNFTTypeInput
                {
                    FullName = fullName,
                    ShortName = shortName
                };
                //"No permission."
                var result = _sideNftContract.ExecuteMethodWithResult(NftContractMethod.AddNFTType, input);
                result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
                result.Error.ShouldContain("No permission.");
            }
            {
                //"Incorrect short name."
                var input = new AddNFTTypeInput
                {
                    FullName = NFTType.Art.ToString(),
                    ShortName = "ART"
                };
                var result =
                    _sideAuthorityManager.ExecuteTransactionWithAuthority(_sideNftContract.ContractAddress,
                        "AddNFTType",
                        input,
                        InitAccount);
                result.Status.ShouldBe(TransactionResultStatus.NodeValidationFailed);
                result.Error.ShouldContain("Incorrect short name.");
            }
            {
                //"Short name {input.ShortName}  already exists."
                var input = new AddNFTTypeInput
                {
                    FullName = NFTType.Art.ToString(),
                    ShortName = "AR"
                };
                var result =
                    _sideAuthorityManager.ExecuteTransactionWithAuthority(_sideNftContract.ContractAddress,
                        "AddNFTType",
                        input,
                        InitAccount);
                result.Status.ShouldBe(TransactionResultStatus.NodeValidationFailed);
                result.Error.ShouldContain("Short name AR already exists.");
            }
            {
                //Full name {input.FullName} already exists."
                var input = new AddNFTTypeInput
                {
                    FullName = NFTType.Art.ToString(),
                    ShortName = "AT"
                };
                var result =
                    _sideAuthorityManager.ExecuteTransactionWithAuthority(_sideNftContract.ContractAddress,
                        "AddNFTType",
                        input,
                        InitAccount);
                result.Status.ShouldBe(TransactionResultStatus.NodeValidationFailed);
                result.Error.ShouldContain($"Full name {NFTType.Art.ToString()} already exists.");
            }
            {
                var input = new AddNFTTypeInput
                {
                    FullName = fullName,
                    ShortName = shortName
                };
                var types = _sideNftContract.GetNftTypes();
                Logger.Info(types.Value);
                var result =
                    _sideAuthorityManager.ExecuteTransactionWithAuthority(_sideNftContract.ContractAddress,
                        "AddNFTType",
                        input,
                        InitAccount);
                result.Status.ShouldBe(TransactionResultStatus.Mined);
                var afterTypes = _sideNftContract.GetNftTypes();
                afterTypes.Value[shortName] = fullName;
            }
        }

        [TestMethod]
        public void RemoveNftType()
        {
            var shortName = "MO";
            var fullName = "MODEL";
            var types = _sideNftContract.GetNftTypes();
            types.Value[shortName].ShouldBe(fullName);
            {
                var input = new StringValue {Value = shortName};
                //"No permission."
                var result = _sideNftContract.ExecuteMethodWithResult(NftContractMethod.RemoveNFTType, input);
                result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
                result.Error.ShouldContain("No permission.");
            }
            {
                //"Incorrect short name."
                var input = new StringValue {Value = "ABC"};
                var result =
                    _sideAuthorityManager.ExecuteTransactionWithAuthority(_sideNftContract.ContractAddress,
                        "RemoveNFTType", input,
                        InitAccount);
                result.Status.ShouldBe(TransactionResultStatus.NodeValidationFailed);
                result.Error.ShouldContain("Incorrect short name.");
            }
            {
                //"Short name {input.ShortName}  already exists."
                var input = new StringValue {Value = "AB"};
                var result =
                    _sideAuthorityManager.ExecuteTransactionWithAuthority(_sideNftContract.ContractAddress,
                        "RemoveNFTType", input,
                        InitAccount);
                result.Status.ShouldBe(TransactionResultStatus.NodeValidationFailed);
                result.Error.ShouldContain("Short name AB does not exist.");
            }
            {
                var input = new StringValue {Value = shortName};
                var result =
                    _sideAuthorityManager.ExecuteTransactionWithAuthority(_sideNftContract.ContractAddress,
                        "RemoveNFTType", input,
                        InitAccount);
                result.Status.ShouldBe(TransactionResultStatus.Mined);
                var afterTypes = _sideNftContract.GetNftTypes();
                afterTypes.Value.Keys.ShouldNotContain(shortName);
            }
        }

        [TestMethod]
        public void MainSymbol_Mint_OnMain()
        {
            var symbol = "AR886498359";
            var protocolInfo = _mainNftContract.GetNftProtocolInfo(symbol);
            var owner = InitAccount;
            var quantity = 10;
            var tokenId = 1112;
            var alias = "AR-0";
            var tokenHash = HashHelper.ComputeFrom($"{symbol}{tokenId}");
            //on main chain
            var result = _mainNftContract.Mint(owner, quantity, alias, symbol, tokenId);
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var logs = result.Logs.First(l => l.Name.Equals("NFTMinted")).NonIndexed;
            var nftMinted = NFTMinted.Parser.ParseFrom(ByteString.FromBase64(logs));
            nftMinted.Creator.ShouldBe(protocolInfo.Creator);
            nftMinted.NftType.ShouldBe(protocolInfo.NftType);
            nftMinted.BaseUri.ShouldBe(protocolInfo.BaseUri);
            nftMinted.ProtocolName.ShouldBe(protocolInfo.ProtocolName);

            nftMinted.Minter.ShouldBe(InitAccount.ConvertAddress());
            nftMinted.Quantity.ShouldBe(quantity);
            nftMinted.Alias.ShouldBe(alias);
            nftMinted.TokenId.ShouldBe(tokenId);
            nftMinted.Symbol.ShouldBe(symbol);
            nftMinted.Uri.ShouldBe(string.Empty);
            nftMinted.Owner.ShouldBe(owner.ConvertAddress());

            var balance = _mainNftContract.GetBalance(owner, symbol, tokenId);
            balance.Balance.ShouldBe(quantity);
            balance.TokenHash.ShouldBe(tokenHash);
            balance.Owner.ShouldBe(owner.ConvertAddress());
        }
        
        [TestMethod]
        [DataRow(200)]
        //2LQe1sGesK5qFnLnGWowT5uJ1Z2g161wt1s54RY5BM157yJ7kp
        //2qkM2r145n8tQr6ADxGEJSBpFpHY1nyY6WqVnLNEpbXEgJFWvA
        //tPkiSRLTYQ8n4kqgy6nDFQb7toKH4BEpm8NtyLyQGEooZzH9y
        public void SideSymbol_Mint_OnSide(long tokenId)
        {
            var symbol = "VW515164933";
            var protocolInfo = _sideNftContract.GetNftProtocolInfo(symbol);
            var owner = "tPkiSRLTYQ8n4kqgy6nDFQb7toKH4BEpm8NtyLyQGEooZzH9y";
            var quantity = 1;
            var alias = "VW-1";
            var tokenHash = HashHelper.ComputeFrom($"{symbol}{tokenId}");
            //on main chain
            var errorResult = _mainNftContract.Mint(owner, quantity, alias, symbol, tokenId);
            errorResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
            errorResult.Error.ShouldContain("Incorrect chain.");

            //on side chain
            var beforeBalance = _sideNftContract.GetBalance(owner, symbol, tokenId);
            var result = _sideNftContract.Mint(owner, quantity, alias, symbol, tokenId);
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var returnValue = Hash.Parser.ParseFrom(ByteArrayHelper.HexStringToByteArray(result.ReturnValue));
            var nftInfo = _sideNftContract.GetNftInfoByTokenHash(returnValue);
            nftInfo.NftType.ShouldBe(protocolInfo.NftType);
            nftInfo.Minters.ShouldContain(InitAccount.ConvertAddress());
            var logs = result.Logs.First(l => l.Name.Equals("NFTMinted")).NonIndexed;
            var nftMinted = NFTMinted.Parser.ParseFrom(ByteString.FromBase64(logs));
            var afterProtocolInfo = _sideNftContract.GetNftProtocolInfo(symbol);

            nftMinted.Creator.ShouldBe(protocolInfo.Creator);
            nftMinted.NftType.ShouldBe(protocolInfo.NftType);
            nftMinted.BaseUri.ShouldBe(protocolInfo.BaseUri);
            nftMinted.ProtocolName.ShouldBe(protocolInfo.ProtocolName);

            nftMinted.Minter.ShouldBe(InitAccount.ConvertAddress());
            nftMinted.Quantity.ShouldBe(quantity);
            nftMinted.Alias.ShouldBe(alias);
            nftMinted.TokenId.ShouldBe(tokenId == 0 ? protocolInfo.Supply.Add(1) : tokenId);
            nftMinted.Symbol.ShouldBe(symbol);
            nftMinted.Uri.ShouldBe(string.Empty);
            nftMinted.Owner.ShouldBe(owner.ConvertAddress());
            nftMinted.Metadata.Value.ShouldBe(protocolInfo.Metadata.Value);

            Logger.Info(afterProtocolInfo);
            afterProtocolInfo.Supply.ShouldBe(protocolInfo.Supply.Add(quantity));
            if (tokenId == 0)
            {
                tokenHash = HashHelper.ComputeFrom($"{symbol}{nftMinted.TokenId}");
                returnValue.ShouldBe(tokenHash);
            }
            else
            {
                returnValue.ShouldBe(tokenHash);
            }

            Logger.Info($"{nftMinted.TokenId}, {returnValue.ToHex()}");
            var balance = _sideNftContract.GetBalance(owner, symbol, nftMinted.TokenId);
            balance.Balance.ShouldBe(beforeBalance.Balance.Add(quantity));
            balance.TokenHash.ShouldBe(tokenHash);
            balance.Owner.ShouldBe(owner.ConvertAddress());
        }

        [TestMethod]
        public void AddMinters()
        {
            var symbol = "AR982169896";
            var newMinter = NewMinter;
            var minterList = _sideNftContract.GetMinterList(symbol);
            if (minterList.Value.Contains(newMinter.ConvertAddress()))
            {
                Logger.Info($"{newMinter} already in minter list");
                return;
            }

            var addMinters = _sideNftContract.ExecuteMethodWithResult(NftContractMethod.AddMinters,
                new AddMintersInput
                {
                    Symbol = symbol,
                    MinterList = new MinterList
                    {
                        Value =
                        {
                            newMinter.ConvertAddress()
                        }
                    }
                });
            addMinters.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            minterList = _sideNftContract.GetMinterList(symbol);
            minterList.Value.ShouldContain(newMinter.ConvertAddress());

            _sideNftContract.SetAccount(newMinter);
            var owner = SideNodeManger.NewAccount("12345678");
            var quantity = 1;
            var alias = "";
            var tokenId = 0;
            var result = _sideNftContract.Mint(owner, quantity, alias, symbol, tokenId);
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var logs = result.Logs.First(l => l.Name.Equals("NFTMinted")).NonIndexed;
            var nftMinted = NFTMinted.Parser.ParseFrom(ByteString.FromBase64(logs));
            var returnValue = Hash.Parser.ParseFrom(ByteArrayHelper.HexStringToByteArray(result.ReturnValue));
            var nftInfo = _sideNftContract.GetNftInfoByTokenHash(returnValue);
            nftInfo.Minters.ShouldContain(newMinter.ConvertAddress());
            var balance = _sideNftContract.GetBalance(owner, symbol, nftMinted.TokenId);
            balance.Balance.ShouldBe(quantity);
        }

        [TestMethod]
        public void RemoveMinters()
        {
            var symbol = "MO727117725";
            var minterList = _sideNftContract.GetMinterList(symbol);
            var removeMinters = _sideNftContract.ExecuteMethodWithResult(NftContractMethod.RemoveMinters,
                new RemoveMintersInput
                {
                    Symbol = symbol,
                    MinterList = minterList
                });
            removeMinters.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            minterList = _sideNftContract.GetMinterList(symbol);
            minterList.ShouldBe(new MinterList());

            _sideNftContract.SetAccount(NewMinter);
            var owner = SideNodeManger.NewAccount("12345678");
            var quantity = 1;
            var alias = "";
            var tokenId = 0;
            var result = _sideNftContract.Mint(owner, quantity, alias, symbol, tokenId);
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
            result.Error.ShouldContain("No permission to mint.");

            _sideNftContract.SetAccount(InitAccount);
            removeMinters = _sideNftContract.ExecuteMethodWithResult(NftContractMethod.RemoveMinters,
                new RemoveMintersInput
                {
                    Symbol = symbol,
                    MinterList = minterList
                });
            removeMinters.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            removeMinters.Error.ShouldContain("Minter list is empty.");
        }

        [TestMethod]
        public void Transfer()
        {
            //2LQe1sGesK5qFnLnGWowT5uJ1Z2g161wt1s54RY5BM157yJ7kp
            //2qkM2r145n8tQr6ADxGEJSBpFpHY1nyY6WqVnLNEpbXEgJFWvA
            //tPkiSRLTYQ8n4kqgy6nDFQb7toKH4BEpm8NtyLyQGEooZzH9y
            //2HnvUWNzKG6DbRhtrDgSwEfqA2YeHEhmLLnTAXKwzMBbJxEhUr
            var symbol = "MO769174599";
            var tokenId = 1;
            var account = "tPkiSRLTYQ8n4kqgy6nDFQb7toKH4BEpm8NtyLyQGEooZzH9y";
            var toAccount = SideNodeManger.NewAccount("12345678");
            var nftInfo = _sideNftContract.GetNftInfo(symbol, tokenId);
            var nftProtoInfo = _sideNftContract.GetNftProtocolInfo(symbol);
            var senderBalance = _sideNftContract.GetBalance(account, symbol, tokenId);
            var amount = senderBalance.Balance.Div(2);
            var elfBalance = _sideToken.GetUserBalance(account);
            if (elfBalance < 10000000000)
                _sideToken.TransferBalance(InitAccount, account, 10000000000);
            _sideNftContract.SetAccount(account);
            var result = _sideNftContract.TransferNftToken(amount, tokenId, symbol, toAccount);
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var afterSenderBalance = _sideNftContract.GetBalance(account, symbol, tokenId);
            afterSenderBalance.Balance.ShouldBe(senderBalance.Balance.Sub(amount));
            var toBalance = _sideNftContract.GetBalance(toAccount, symbol, tokenId);
            toBalance.Balance.ShouldBe(amount);

            var afterNftInfo = _sideNftContract.GetNftInfo(symbol, tokenId);
            afterNftInfo.Quantity.ShouldBe(nftInfo.Quantity);
            var afterNftProtoInfo = _sideNftContract.GetNftProtocolInfo(symbol);
            afterNftProtoInfo.Supply.ShouldBe(nftProtoInfo.Supply);
            afterNftProtoInfo.TotalSupply.ShouldBe(nftProtoInfo.TotalSupply);
            Logger.Info($"sender balance: {afterSenderBalance.Balance}\n" +
                        $"to balance: {toBalance.Balance}");
        }

        [TestMethod]
        public void TransferFrom()
        {
            //"Not approved."
            var symbol = "MO727117725";
            var tokenId = 206;
            var account = "tPkiSRLTYQ8n4kqgy6nDFQb7toKH4BEpm8NtyLyQGEooZzH9y";
            var toAccount = SideNodeManger.NewAccount("12345678");
            var nftInfo = _sideNftContract.GetNftInfo(symbol, tokenId);
            var nftProtoInfo = _sideNftContract.GetNftProtocolInfo(symbol);
            var senderBalance = _sideNftContract.GetBalance(account, symbol, tokenId);
            var amount = senderBalance.Balance.Div(10);
            var elfBalance = _sideToken.GetUserBalance(toAccount);
            if (elfBalance < 10000000000)
                _sideToken.TransferBalance(InitAccount, toAccount, 10000000000);
            {
                _sideNftContract.SetAccount(toAccount);
                var result = _sideNftContract.ExecuteMethodWithResult(NftContractMethod.TransferFrom,
                    new TransferFromInput
                    {
                        From = account.ConvertAddress(),
                        Amount = amount,
                        Symbol = symbol,
                        TokenId = tokenId,
                        To = toAccount.ConvertAddress()
                    });
                result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
                result.Error.ShouldContain("Not approved.");
            }
            {
                _sideNftContract.SetAccount(account);
                var approveResult = _sideNftContract.ExecuteMethodWithResult(NftContractMethod.Approve,
                    new ApproveInput
                    {
                        Symbol = symbol,
                        Spender = toAccount.ConvertAddress(),
                        Amount = amount,
                        TokenId = tokenId
                    });
                approveResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
                var allowance = _sideNftContract.GetAllowance(symbol, tokenId, account, toAccount);
                allowance.Allowance.ShouldBe(amount);

                _sideNftContract.SetAccount(toAccount);
                var result = _sideNftContract.ExecuteMethodWithResult(NftContractMethod.TransferFrom,
                    new TransferFromInput
                    {
                        From = account.ConvertAddress(),
                        Amount = amount,
                        Symbol = symbol,
                        TokenId = tokenId,
                        To = toAccount.ConvertAddress()
                    });
                result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

                var afterAllowance = _sideNftContract.GetAllowance(symbol, tokenId, account, toAccount);
                afterAllowance.Allowance.ShouldBe(allowance.Allowance - amount);

                var afterSenderBalance = _sideNftContract.GetBalance(account, symbol, tokenId);
                afterSenderBalance.Balance.ShouldBe(senderBalance.Balance.Sub(amount));
                var toBalance = _sideNftContract.GetBalance(toAccount, symbol, tokenId);
                toBalance.Balance.ShouldBe(amount);

                var afterNftInfo = _sideNftContract.GetNftInfo(symbol, tokenId);
                afterNftInfo.Quantity.ShouldBe(nftInfo.Quantity);
                var afterNftProtoInfo = _sideNftContract.GetNftProtocolInfo(symbol);
                afterNftProtoInfo.Supply.ShouldBe(nftProtoInfo.Supply);
                afterNftProtoInfo.TotalSupply.ShouldBe(nftProtoInfo.TotalSupply);
                Logger.Info($"sender balance: {afterSenderBalance.Balance}\n" +
                            $"to balance: {toBalance.Balance}");
            }
        }

        [TestMethod]
        public void UnApprove()
        {
            var symbol = "MO727117725";
            var tokenId = 206;
            var account = "tPkiSRLTYQ8n4kqgy6nDFQb7toKH4BEpm8NtyLyQGEooZzH9y";
            var toAccount = SideNodeManger.NewAccount("12345678");
            var senderBalance = _sideNftContract.GetBalance(account, symbol, tokenId);
            var amount = senderBalance.Balance.Div(10);

            _sideNftContract.SetAccount(account);
            var approveResult = _sideNftContract.ExecuteMethodWithResult(NftContractMethod.Approve,
                new ApproveInput
                {
                    Symbol = symbol,
                    Spender = toAccount.ConvertAddress(),
                    Amount = amount,
                    TokenId = tokenId
                });
            approveResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var allowance = _sideNftContract.GetAllowance(symbol, tokenId, account, toAccount);
            allowance.Allowance.ShouldBe(amount);
            var unAmount = amount.Div(2);
            var unApproveResult = _sideNftContract.ExecuteMethodWithResult(NftContractMethod.UnApprove,
                new UnApproveInput
                {
                    Symbol = symbol,
                    Spender = toAccount.ConvertAddress(),
                    Amount = unAmount,
                    TokenId = tokenId
                });
            unApproveResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            var afterAllowance = _sideNftContract.GetAllowance(symbol, tokenId, account, toAccount);
            afterAllowance.Allowance.ShouldBe(allowance.Allowance - unAmount);

            unApproveResult = _sideNftContract.ExecuteMethodWithResult(NftContractMethod.UnApprove,
                new UnApproveInput
                {
                    Symbol = symbol,
                    Spender = toAccount.ConvertAddress(),
                    Amount = afterAllowance.Allowance.Add(1),
                    TokenId = tokenId
                });
            unApproveResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            afterAllowance = _sideNftContract.GetAllowance(symbol, tokenId, account, toAccount);
            afterAllowance.Allowance.ShouldBe(0);
        }

        [TestMethod]
        public void ApproveProtocol()
        {
            var symbol = "AR141829112";
            var tokenId = 100;
            var owner = "tPkiSRLTYQ8n4kqgy6nDFQb7toKH4BEpm8NtyLyQGEooZzH9y";
            var amount = 1;
            var nftProtoInfo = _sideNftContract.GetNftProtocolInfo(symbol);
            var getOperator = _sideNftContract.GetOperatorList(symbol, owner);
            if (!getOperator.Value.Contains(Operator.ConvertAddress()))
            {
                _sideNftContract.SetAccount(owner);
                var result = _sideNftContract.ApproveProtocol(Operator, symbol, true);
                result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
                getOperator = _sideNftContract.GetOperatorList(symbol, owner);
                getOperator.Value.ShouldContain(Operator.ConvertAddress());
                var elfBalance = _sideToken.GetUserBalance(Operator);
                if (elfBalance < 10000000000)
                    _sideToken.TransferBalance(InitAccount, Operator, 10000000000);
                var beforeOwnerBalance = _sideNftContract.GetBalance(owner, symbol, tokenId);
                var beforeOperatorBalance = _sideNftContract.GetBalance(Operator, symbol, tokenId);

                _sideNftContract.SetAccount(Operator);
                var transferResult = _sideNftContract.ExecuteMethodWithResult(NftContractMethod.TransferFrom,
                    new TransferFromInput
                    {
                        From = owner.ConvertAddress(),
                        Amount = amount,
                        Symbol = symbol,
                        TokenId = tokenId,
                        To = Operator.ConvertAddress()
                    });
                transferResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

                var afterOwnerBalance = _sideNftContract.GetBalance(owner, symbol, tokenId);
                var afterOperatorBalance = _sideNftContract.GetBalance(Operator, symbol, tokenId);
                afterOwnerBalance.Balance.ShouldBe(beforeOwnerBalance.Balance.Sub(amount));
                afterOperatorBalance.Balance.ShouldBe(beforeOperatorBalance.Balance.Add(amount));
            }
            else
            {
                _sideNftContract.SetAccount(owner);
                var result = _sideNftContract.ApproveProtocol(Operator, symbol, false);
                result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
                getOperator = _sideNftContract.GetOperatorList(symbol, owner);
                getOperator.Value.ShouldNotContain(Operator.ConvertAddress());
                var elfBalance = _sideToken.GetUserBalance(Operator);
                if (elfBalance < 10000000000)
                    _sideToken.TransferBalance(InitAccount, Operator, 10000000000);
                _sideNftContract.SetAccount(Operator);
                var transferResult = _sideNftContract.ExecuteMethodWithResult(NftContractMethod.TransferFrom,
                    new TransferFromInput
                    {
                        From = owner.ConvertAddress(),
                        Amount = amount,
                        Symbol = symbol,
                        TokenId = tokenId,
                        To = Operator.ConvertAddress()
                    });
                transferResult.Status.ConvertTransactionResultStatus()
                    .ShouldBe(TransactionResultStatus.NodeValidationFailed);
                transferResult.Error.ShouldContain("Not approved.");
            }
        }

        [TestMethod]
        public void Burn()
        {
            var symbol = "AR982169896";
            var tokenId = 1;
            var account = "tPkiSRLTYQ8n4kqgy6nDFQb7toKH4BEpm8NtyLyQGEooZzH9y";
            var minter = InitAccount;
            var nftProtoInfo = _sideNftContract.GetNftProtocolInfo(symbol);
            var ownerBalance = _sideNftContract.GetBalance(account, symbol, tokenId);
            var amount = ownerBalance.Balance;
            _sideNftContract.SetAccount(account);
            var transfer = _sideNftContract.ExecuteMethodWithResult(NftContractMethod.Transfer,
                new TransferInput
                {
                    Amount = amount,
                    Symbol = symbol,
                    TokenId = tokenId,
                    To = minter.ConvertAddress()
                });
            transfer.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var minterBalance = _sideNftContract.GetBalance(minter, symbol, tokenId);
            var burnAmount = minterBalance.Balance.Div(4);
            _sideNftContract.SetAccount(minter);
            var result = _sideNftContract.ExecuteMethodWithResult(NftContractMethod.Burn,
                new BurnInput
                {
                    Amount = burnAmount,
                    Symbol = symbol,
                    TokenId = tokenId,
                });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var afterMinterBalance = _sideNftContract.GetBalance(minter, symbol, tokenId);
            afterMinterBalance.Balance.ShouldBe(minterBalance.Balance - burnAmount);
            var afterNftInfo = _sideNftContract.GetNftInfo(symbol, tokenId);
            afterNftInfo.IsBurned.ShouldBeFalse();
            var afterNftProtoInfo = _sideNftContract.GetNftProtocolInfo(symbol);
            afterNftProtoInfo.Supply.ShouldBe(nftProtoInfo.Supply - burnAmount);

            var burnAllResult = _sideNftContract.ExecuteMethodWithResult(NftContractMethod.Burn,
                new BurnInput
                {
                    Amount = afterMinterBalance.Balance,
                    Symbol = symbol,
                    TokenId = tokenId,
                });
            burnAllResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            afterMinterBalance = _sideNftContract.GetBalance(minter, symbol, tokenId);
            afterMinterBalance.Balance.ShouldBe(0);
            afterNftInfo = _sideNftContract.GetNftInfo(symbol, tokenId);
            if (afterNftInfo.Quantity == 0 && !nftProtoInfo.IsTokenIdReuse)
                afterNftInfo.IsBurned.ShouldBeTrue();
            else
                afterNftInfo.IsBurned.ShouldBeFalse();
            Logger.Info($"{afterNftInfo.Quantity} ==> {afterNftInfo.IsBurned}");
        }
        
        [TestMethod]
        public void Assemble()
        {
            var symbol = "GA171851939";
            var nftSymbol1 = "BA932067411";
            var nftSymbolTokenId1 = 100;
            var nftAmount1 = 0;
            var tokenHash1 = _sideNftContract.CalculateTokenHash(nftSymbol1, nftSymbolTokenId1);
            var nftSymbol2 = "CO300641678";
            var nftSymbolTokenId2 = 100;
            var nftAmount2 = 0;
            var tokenHash2 = _sideNftContract.CalculateTokenHash(nftSymbol2, nftSymbolTokenId2);
            var ftsAmount = 0;

            var nft = new Dictionary<string, long>
                {[tokenHash1.ToHex()] = nftAmount1, [tokenHash2.ToHex()] = nftAmount2};
            var fts = new Dictionary<string, long> {["ELF"] = ftsAmount};
            var owner = "tPkiSRLTYQ8n4kqgy6nDFQb7toKH4BEpm8NtyLyQGEooZzH9y";
            var minter = InitAccount;
            var tokenId = 0;
            var alias = "";
            var description = "whatever";

            var assembleSymbolInfo = _sideNftContract.GetNftProtocolInfo(symbol);
            var beforeBalance = _sideNftContract.GetBalance(owner, symbol, tokenId);

            _sideNftContract.SetAccount(owner);
            var result1 = _sideNftContract.TransferNftToken(nftAmount1, nftSymbolTokenId1, nftSymbol1, minter);
            result1.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var result2 = _sideNftContract.TransferNftToken(nftAmount2, nftSymbolTokenId2, nftSymbol2, minter);
            result2.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            _sideNftContract.SetAccount(minter);
            _sideToken.ApproveToken(minter, _sideNftContract.ContractAddress, ftsAmount * 10, "ELF");
            var beforeMinterFtsBalance = _sideToken.GetUserBalance(minter, "ELF");
            var beforeMinterNft1Balance = _sideNftContract.GetBalance(minter, nftSymbol1, nftSymbolTokenId1);
            var beforeMinterNft2Balance = _sideNftContract.GetBalance(minter, nftSymbol2, nftSymbolTokenId2);

            var result = _sideNftContract.Assemble(owner, alias, symbol, tokenId, nft, fts, description);
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var assembleLogs = result.Logs.First(l => l.Name.Equals("Assembled")).Indexed;
            var index1 = Assembled.Parser.ParseFrom(ByteString.FromBase64(assembleLogs[0]));
            index1.Symbol.ShouldBe(symbol);
            var index2 = Assembled.Parser.ParseFrom(ByteString.FromBase64(assembleLogs[1]));
            index2.TokenId.ShouldBe(tokenId == 0 ? assembleSymbolInfo.Supply.Add(1) : tokenId);
            var index3 = Assembled.Parser.ParseFrom(ByteString.FromBase64(assembleLogs[2]));
            index3.AssembledNfts.Value.ShouldBe(nft);
            var index4 = Assembled.Parser.ParseFrom(ByteString.FromBase64(assembleLogs[3]));
            index4.AssembledFts.Value.ShouldBe(fts);

            var mintLogs = result.Logs.First(l => l.Name.Equals("NFTMinted")).NonIndexed;
            var minted = NFTMinted.Parser.ParseFrom(ByteString.FromBase64(mintLogs));
            minted.Alias.ShouldBe(alias);
            minted.Creator.ShouldBe(InitAccount.ConvertAddress());
            minted.Owner.ShouldBe(owner.ConvertAddress());
            minted.Quantity.ShouldBe(1);
            minted.TokenId.ShouldBe(tokenId == 0 ? assembleSymbolInfo.Supply.Add(1) : tokenId);
            var fee = result.GetTransactionFee().Item2;

            var afterAssembleSymbolInfo = _sideNftContract.GetNftProtocolInfo(symbol);
            var afterBalance = _sideNftContract.GetBalance(owner, symbol, minted.TokenId);
            var afterFtsBalance = _sideToken.GetUserBalance(minter, "ELF");
            var afterNft1Balance = _sideNftContract.GetBalance(minter, nftSymbol1, nftSymbolTokenId1);
            var afterNft2Balance = _sideNftContract.GetBalance(minter, nftSymbol2, nftSymbolTokenId2);
            afterBalance.Balance.ShouldBe(beforeBalance.Balance.Add(1));
            afterFtsBalance.ShouldBe(beforeMinterFtsBalance - ftsAmount - fee);
            afterNft1Balance.Balance.ShouldBe(beforeMinterNft1Balance.Balance - nftAmount1);
            afterNft2Balance.Balance.ShouldBe(beforeMinterNft2Balance.Balance - nftAmount2);
            afterAssembleSymbolInfo.Supply.ShouldBe(assembleSymbolInfo.Supply.Add(1));
        }

        [TestMethod]
        public void Disassemble()
        {
            var owner = "tPkiSRLTYQ8n4kqgy6nDFQb7toKH4BEpm8NtyLyQGEooZzH9y";
            var minter = InitAccount;
            var symbol = "GA171851939";
            var tokenId = 6;
            var amount = 1;
            var ftsAmount = 1000000000;
            var nftSymbol1 = "BA932067411";
            var nftSymbolTokenId1 = 100;
            var nftAmount1 = 100;
            var tokenHash1 = _sideNftContract.CalculateTokenHash(nftSymbol1, nftSymbolTokenId1);
            var nftSymbol2 = "CO300641678";
            var nftSymbolTokenId2 = 100;
            var nftAmount2 = 10;
            var tokenHash2 = _sideNftContract.CalculateTokenHash(nftSymbol2, nftSymbolTokenId2);
            var nft = new Dictionary<string, long>
                {[tokenHash1.ToHex()] = nftAmount1, [tokenHash2.ToHex()] = nftAmount2};
            var fts = new Dictionary<string, long> {["ELF"] = ftsAmount};
            var ownerBalance = _sideNftContract.GetBalance(owner, symbol, tokenId);
            var disassembleSymbolInfo = _sideNftContract.GetNftProtocolInfo(symbol);

            _sideNftContract.SetAccount(owner);
            var result = _sideNftContract.TransferNftToken(amount, tokenId, symbol, minter);
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            var beforeMinterFtsBalance = _sideToken.GetUserBalance(minter, "ELF");
            var beforeOwnerNft1Balance = _sideNftContract.GetBalance(owner, nftSymbol1, nftSymbolTokenId1);
            var beforeOwnerNft2Balance = _sideNftContract.GetBalance(owner, nftSymbol2, nftSymbolTokenId2);
            var beforeMinterBalance = _sideNftContract.GetBalance(minter, symbol, tokenId);

            _sideNftContract.SetAccount(InitAccount);
            var disassembleResult = _sideNftContract.Disassemble(symbol, tokenId, owner);
            disassembleResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var fee = disassembleResult.GetTransactionFee().Item2;
            var burnLogs = disassembleResult.Logs.First(l => l.Name.Equals("Burned")).Indexed;
            var burnIndex1 = Burned.Parser.ParseFrom(ByteString.FromBase64(burnLogs[0]));
            var burnIndex2 = Burned.Parser.ParseFrom(ByteString.FromBase64(burnLogs[1]));
            var burnIndex3 = Burned.Parser.ParseFrom(ByteString.FromBase64(burnLogs[2]));
            burnIndex1.Burner.ShouldBe(minter.ConvertAddress());
            burnIndex2.Symbol.ShouldBe(symbol);
            burnIndex3.TokenId.ShouldBe(tokenId);

            var disassembleLogs = disassembleResult.Logs.First(l => l.Name.Equals("Disassembled")).Indexed;
            var index1 = Disassembled.Parser.ParseFrom(ByteString.FromBase64(disassembleLogs[0]));
            index1.Symbol.ShouldBe(symbol);
            var index2 = Disassembled.Parser.ParseFrom(ByteString.FromBase64(disassembleLogs[1]));
            index2.TokenId.ShouldBe(tokenId);
            var index3 = Disassembled.Parser.ParseFrom(ByteString.FromBase64(disassembleLogs[2]));
            index3.DisassembledNfts.Value.ShouldBe(nft);
            var index4 = Disassembled.Parser.ParseFrom(ByteString.FromBase64(disassembleLogs[3]));
            index4.DisassembledFts.Value.ShouldBe(fts);

            var afterDisassembleSymbolInfo = _sideNftContract.GetNftProtocolInfo(symbol);
            var afterMinterBalance = _sideNftContract.GetBalance(minter, symbol, tokenId);
            var afterFtsBalance = _sideToken.GetUserBalance(minter, "ELF");
            var afterNft1Balance = _sideNftContract.GetBalance(owner, nftSymbol1, nftSymbolTokenId1);
            var afterNft2Balance = _sideNftContract.GetBalance(owner, nftSymbol2, nftSymbolTokenId2);
            afterMinterBalance.Balance.ShouldBe(beforeMinterBalance.Balance.Sub(amount));
            afterFtsBalance.ShouldBe(beforeMinterFtsBalance + ftsAmount - fee);
            afterNft1Balance.Balance.ShouldBe(beforeOwnerNft1Balance.Balance + nftAmount1);
            afterNft2Balance.Balance.ShouldBe(beforeOwnerNft2Balance.Balance + nftAmount2);
            afterDisassembleSymbolInfo.Supply.ShouldBe(disassembleSymbolInfo.Supply.Sub(amount));
        }

        [TestMethod]
        public void Recast()
        {
            var symbol = "VW515164933";
            var tokenId = 100;
            var alias = "xxx";
            var uri = "";
            var metadata = new Metadata
            {
                Value =
                {
                    {"Description", "Recast test"},
                    {"XXX", "xxxx"}
                }
            };
            var owner = "tPkiSRLTYQ8n4kqgy6nDFQb7toKH4BEpm8NtyLyQGEooZzH9y";
            var tokenHash = _sideNftContract.CalculateTokenHash(symbol, tokenId);
            var symbolInfo = _sideNftContract.GetNftInfoByTokenHash(tokenHash);
            var balance = _sideNftContract.GetBalance(owner, symbol, tokenId);
            var nftProtocol = _sideNftContract.GetNftProtocolInfo(symbol);
            Logger.Info(nftProtocol);
            _sideNftContract.SetAccount(owner);
            var transfer = _sideNftContract.TransferNftToken(balance.Balance, tokenId, symbol, InitAccount);
            transfer.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var minterBalance = _sideNftContract.GetBalance(InitAccount, symbol, tokenId);

            if (symbolInfo.Quantity.Equals(minterBalance.Balance))
            {
                _sideNftContract.SetAccount(InitAccount);
                var result = _sideNftContract.Recast(symbol, tokenId, alias, uri, metadata);
                result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
                var logs = result.Logs.First(l => l.Name.Equals("Recasted")).Indexed;
                var eventSymbol = Recasted.Parser.ParseFrom(ByteString.FromBase64(logs[0]));
                var eventTokenId = Recasted.Parser.ParseFrom(ByteString.FromBase64(logs[1]));
                var eventOld = Recasted.Parser.ParseFrom(ByteString.FromBase64(logs[2]));
                var eventNew = Recasted.Parser.ParseFrom(ByteString.FromBase64(logs[3]));
                var eventAlias = Recasted.Parser.ParseFrom(ByteString.FromBase64(logs[4]));
                // var eventUri =  Recasted.Parser.ParseFrom(ByteString.FromBase64(logs[5]));

                var afterSymbolInfo = _sideNftContract.GetNftInfoByTokenHash(tokenHash);
                afterSymbolInfo.Alias.ShouldBe(alias);
                Logger.Info(afterSymbolInfo.Metadata.Value);

                var afterNftProtocol = _sideNftContract.GetNftProtocolInfo(symbol);
                Logger.Info(afterNftProtocol);
                nftProtocol.Metadata.Value.ShouldBe(afterNftProtocol.Metadata.Value);
                nftProtocol.Metadata.Value.ShouldNotBe(afterSymbolInfo.Metadata.Value);
            }
            else
            {
                _sideNftContract.SetAccount(InitAccount);
                var result = _sideNftContract.Recast(symbol, tokenId, alias, uri, metadata);
                result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
                result.Error.ShouldContain("Do not support recast.");
            }

            {
                _sideNftContract.SetAccount(owner);
                var result = _sideNftContract.Recast(symbol, tokenId, alias, uri, metadata);
                result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
                result.Error.ShouldContain("No permission.");
            }
        }

        [TestMethod]
        public void CheckBalance()
        {
            var symbolList = new List<string>
            {
                "MO720142501",
                "AR141829112",
                "BA932067411", //true
                "CO300641678",
                "GA171851939"
            };
            foreach (var symbol in symbolList)
            {
                var owner = "tPkiSRLTYQ8n4kqgy6nDFQb7toKH4BEpm8NtyLyQGEooZzH9y";
                var nftBalance = _sideNftContract.GetBalance(owner, symbol, 100);
                Logger.Info($"{symbol} = {nftBalance.Balance}");
                var nftProto = _sideNftContract.GetNftProtocolInfo(symbol);
                Logger.Info(nftProto);
            }
        }
    }
}