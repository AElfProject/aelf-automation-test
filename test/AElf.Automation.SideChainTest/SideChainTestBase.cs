using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Acs0;
using Acs3;
using Acs7;
using AElf.Client.Dto;
using AElf.Contracts.Association;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Contracts.MultiToken;
using AElf.Types;
using AElfChain.Common;
using AElfChain.Common.Contracts;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using log4net;
using Shouldly;
using Volo.Abp.Threading;

namespace AElf.Automation.SideChainTests
{
    public class SideChainTestBase
    {
        protected static readonly ILog _logger = Log4NetHelper.GetLogger();
        public string InitAccount;
        public ContractServices MainServices;
        public string MemberAccount = "2frDVeV6VxUozNqcFbgoxruyqCRAuSyXyfCaov6bYWc7Gkxkh2";

        public List<string> Miners;
        public string OtherAccount = "h6CRCFAhyozJPwdFRd7i8A5zVAqy171AVty3uMQUQp1MB9AKa";
        public TokenContractImplContainer.TokenContractImplStub side1TokenContractStub;
        public TokenContractImplContainer.TokenContractImplStub side2TokenContractStub;
        public ContractServices SideAServices;
        public ContractServices SideBServices;
        public List<ContractServices> SideServices;
        public TokenContractImplContainer.TokenContractImplStub TokenContractImplStub;

        public TokenContractContainer.TokenContractStub TokenContractStub;

        protected void Initialize()
        {
            //Init Logger
            Log4NetHelper.LogInit();
            NodeInfoHelper.SetConfig("nodes-env1-main");
            InitAccount = ConfigInfoHelper.Config.MainChainInfos.Account;
            var mainUrl = ConfigInfoHelper.Config.MainChainInfos.MainChainUrl;
            var password = ConfigInfoHelper.Config.MainChainInfos.Password;
            var sideUrls = ConfigInfoHelper.Config.SideChainInfos.Select(l => l.SideChainUrl).ToList();
            _logger.Info($"url :{mainUrl}");

            MainServices = new ContractServices(mainUrl, InitAccount, password);
            SideServices = new List<ContractServices>();
            foreach (var side in sideUrls)
            {
                var sideServices = new ContractServices(side, InitAccount, password);
                SideServices.Add(sideServices);
            }

            SideAServices = SideServices.First();
            SideBServices = new ContractServices(sideUrls[1], InitAccount, NodeOption.DefaultPassword);

            TokenContractStub = MainServices.TokenContractStub;
            TokenContractImplStub = MainServices.TokenImplContractStub;
            side1TokenContractStub = SideAServices.TokenImplContractStub;
            side2TokenContractStub = SideBServices.TokenImplContractStub;
            Miners = new List<string>();
            Miners = new AuthorityManager(MainServices.NodeManager, InitAccount).GetCurrentMiners();

//            foreach (var miner in Miners)
//            {
//                foreach (var service in SideServices)
//                {
//                    service.TokenService.IssueBalance(InitAccount, miner, 10000_00000000,
//                        service.TokenService.GetPrimaryTokenSymbol());
//                }
//            }
        }

        #region Other Method

        protected string ExecuteMethodWithTxId(ContractServices services, string rawTx)
        {
            var transactionId =
                services.NodeManager.SendTransaction(rawTx);

            return transactionId;
        }

        #endregion

        #region cross chain verify 

        protected CrossChainMerkleProofContext GetBoundParentChainHeightAndMerklePathByHeight(ContractServices services,
            string account,
            long blockNumber)
        {
            services.CrossChainService.SetAccount(account);
            var result = services.CrossChainService.CallViewMethod<CrossChainMerkleProofContext>(
                CrossChainContractMethod.GetBoundParentChainHeightAndMerklePathByHeight, new Int64Value
                {
                    Value = blockNumber
                });
            return result;
        }

        #endregion

        private IEnumerable<Address> GetMiners(ContractServices services)
        {
            var minerList = new List<Address>();
            var miners =
                services.ConsensusService.CallViewMethod<MinerList>(ConsensusMethod.GetCurrentMinerList, new Empty());
            foreach (var publicKey in miners.Pubkeys)
            {
                var address = Address.FromPublicKey(publicKey.ToByteArray());
                minerList.Add(address);
            }

            return minerList;
        }

        #region cross chain transfer

        protected MerklePath GetMerklePath(long blockNumber, string txId, ContractServices services, out Hash root)
        {
            var index = 0;
            var blockInfoResult =
                AsyncHelper.RunSync(() => services.NodeManager.ApiClient.GetBlockByHeightAsync(blockNumber, true));
            var transactionIds = blockInfoResult.Body.Transactions;
            var transactionStatus = new List<string>();

            foreach (var transactionId in transactionIds)
            {
                var txResult = AsyncHelper.RunSync(() =>
                    services.NodeManager.ApiClient.GetTransactionResultAsync(transactionId));
                var resultStatus = txResult.Status.ConvertTransactionResultStatus();
                transactionStatus.Add(resultStatus.ToString());
            }

            var txIdsWithStatus = new List<Hash>();
            for (var num = 0; num < transactionIds.Count; num++)
            {
                var transactionId = HashHelper.HexStringToHash(transactionIds[num]);
                var txRes = transactionStatus[num];
                var rawBytes = transactionId.ToByteArray().Concat(Encoding.UTF8.GetBytes(txRes))
                    .ToArray();
                var txIdWithStatus = Hash.FromRawBytes(rawBytes);
                txIdsWithStatus.Add(txIdWithStatus);
                if (!transactionIds[num].Equals(txId)) continue;
                index = num;
                _logger.Info($"The transaction index is {index}");
            }

            var bmt = BinaryMerkleTree.FromLeafNodes(txIdsWithStatus);
            root = bmt.Root;
            var merklePath = new MerklePath();
            merklePath.MerklePathNodes.AddRange(bmt.GenerateMerklePath(index).MerklePathNodes);
            return merklePath;
        }

        protected string ValidateTokenAddress(ContractServices services)
        {
            var validateTransaction = services.NodeManager.GenerateRawTransaction(
                services.CallAddress, services.GenesisService.ContractAddress,
                GenesisMethod.ValidateSystemContractAddress.ToString(), new ValidateSystemContractAddressInput
                {
                    Address = AddressHelper.Base58StringToAddress(services.TokenService.ContractAddress),
                    SystemContractHashName = Hash.FromString("AElf.ContractNames.Token")
                });
            return validateTransaction;
        }

        public async Task<string> ValidateTokenSymbol(ContractServices services, string symbol)
        {
            var tokenInfo = await TokenContractStub.GetTokenInfo.CallAsync(new GetTokenInfoInput {Symbol = symbol});
            var validateTransaction = services.NodeManager.GenerateRawTransaction(
                services.CallAddress, services.TokenService.ContractAddress,
                TokenMethod.ValidateTokenInfoExists.ToString(), new ValidateTokenInfoExistsInput
                {
                    IsBurnable = tokenInfo.IsBurnable,
                    Issuer = tokenInfo.Issuer,
                    IssueChainId = tokenInfo.IssueChainId,
                    Decimals = tokenInfo.Decimals,
                    Symbol = tokenInfo.Symbol,
                    TokenName = tokenInfo.TokenName,
                    TotalSupply = tokenInfo.TotalSupply
                });
            return validateTransaction;
        }

        #endregion

        #region side chain create method

        protected Hash RequestSideChainCreation(ContractServices services, string creator, string password,
            long indexingPrice, long lockedTokenAmount, bool isPrivilegePreserved,
            SideChainTokenInfo tokenInfo)
        {
            services.CrossChainService.SetAccount(creator, password);
            var issue = new SideChainTokenInitialIssue
            {
                Address = AddressHelper.Base58StringToAddress(creator),
                Amount = 10_0000_0000_0000
            };
            var result =
                services.CrossChainService.ExecuteMethodWithResult(CrossChainContractMethod.RequestSideChainCreation,
                    new SideChainCreationRequest
                    {
                        IndexingPrice = indexingPrice,
                        LockedTokenAmount = lockedTokenAmount,
                        IsPrivilegePreserved = isPrivilegePreserved,
                        SideChainTokenDecimals = tokenInfo.Decimals,
                        SideChainTokenName = tokenInfo.TokenName,
                        SideChainTokenSymbol = tokenInfo.Symbol,
                        SideChainTokenTotalSupply = tokenInfo.TotalSupply,
                        IsSideChainTokenBurnable = tokenInfo.IsBurnable,
                        InitialResourceAmount = {{"CPU", 2}, {"RAM", 4}, {"DISK", 512}, {"NET", 1024}},
                        SideChainTokenInitialIssueList = {issue}
                    });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var byteString = result.Logs.First(l => l.Name.Contains(nameof(ProposalCreated))).NonIndexed;
            var proposalId = ProposalCreated.Parser
                .ParseFrom(ByteString.FromBase64(byteString))
                .ProposalId;
            ;
            return proposalId;
        }

        protected TransactionResultDto Recharge(ContractServices services, string account, int chainId, long amount)
        {
            services.CrossChainService.SetAccount(account);
            var result =
                services.CrossChainService.ExecuteMethodWithResult(CrossChainContractMethod.Recharge, new RechargeInput
                {
                    ChainId = chainId,
                    Amount = amount
                });
            return result;
        }

        public ProposalOutput GetProposal(ContractServices services, string proposalId)
        {
            var result =
                services.ParliamentService.CallViewMethod<ProposalOutput>(ParliamentMethod.GetProposal,
                    HashHelper.HexStringToHash(proposalId));
            return result;
        }

        public void ApproveProposal(ContractServices services, Hash proposalId)
        {
            var miners = GetMiners(services);
            foreach (var miner in miners)
            {
                services.ParliamentService.SetAccount(miner.GetFormatted(), "123");
                var result = services.ParliamentService.ExecuteMethodWithResult(ParliamentMethod.Approve, proposalId);
            }
        }

        protected Address CreateAssociationOrganization(ContractServices services)
        {
            var address = services.AssociationService.ExecuteMethodWithResult(AssociationMethod.CreateOrganization,
                new CreateOrganizationInput
                {
                    ProposalReleaseThreshold = new ProposalReleaseThreshold
                    {
                        MaximalAbstentionThreshold = 1,
                        MaximalRejectionThreshold = 1,
                        MinimalApprovalThreshold = 2,
                        MinimalVoteThreshold = 2
                    },
                    OrganizationMemberList = new OrganizationMemberList
                    {
                        OrganizationMembers =
                        {
                            AddressHelper.Base58StringToAddress(OtherAccount),
                            AddressHelper.Base58StringToAddress(InitAccount),
                            AddressHelper.Base58StringToAddress(MemberAccount)
                        }
                    },
                    ProposerWhiteList = new ProposerWhiteList
                    {
                        Proposers = {AddressHelper.Base58StringToAddress(OtherAccount)}
                    }
                });
            var organizationAddress =
                Address.Parser.ParseFrom(ByteArrayHelper.HexStringToByteArray(address.ReturnValue));
            _logger.Info($"Association organization is: {organizationAddress}");
            return organizationAddress;
        }

        protected Address CreateParliamentOrganization(ContractServices services)
        {
            var miners = GetMiners(services);
            services.ParliamentService.SetAccount(miners.First().GetFormatted());
            var address = services.ParliamentService.ExecuteMethodWithResult(ParliamentMethod.CreateOrganization,
                new Contracts.Parliament.CreateOrganizationInput
                {
                    ProposalReleaseThreshold = new ProposalReleaseThreshold
                    {
                        MaximalAbstentionThreshold = 1000,
                        MaximalRejectionThreshold = 1000,
                        MinimalApprovalThreshold = 3000,
                        MinimalVoteThreshold = 3000
                    },
                    ProposerAuthorityRequired = false,
                    ParliamentMemberProposingAllowed = true
                });
            var organizationAddress =
                Address.Parser.ParseFrom(ByteArrayHelper.HexStringToByteArray(address.ReturnValue));
            return organizationAddress;
        }

        protected void ApproveWithAssociation(ContractServices tester, Hash proposalId, Address association)
        {
            var organization =
                tester.AssociationService.CallViewMethod<Organization>(AssociationMethod.GetOrganization,
                    association);
            var members = organization.OrganizationMemberList.OrganizationMembers.ToList();
            foreach (var member in members)
            {
                tester.AssociationService.SetAccount(member.GetFormatted());
                var approve =
                    tester.AssociationService.ExecuteMethodWithResult(AssociationMethod.Approve, proposalId);
                if (tester.AssociationService.CheckProposal(proposalId).ToBeReleased) return;
            }
        }

        protected TransactionResultDto ReleaseWithAssociation(ContractServices tester, Hash proposalId, string account)
        {
            tester.AssociationService.SetAccount(account);
            var result =
                tester.AssociationService.ExecuteMethodWithResult(AssociationMethod.Release,
                    proposalId);
            return result;
        }

        protected void ApproveAndTransferOrganizationBalanceAsync(ContractServices tester, Address organizationAddress,
            long amount, string proposer)
        {
            var approveInput = new ApproveInput
            {
                Spender = AddressHelper.Base58StringToAddress(tester.CrossChainService.ContractAddress),
                Symbol = "ELF",
                Amount = amount
            };
            var proposal = tester.AssociationService.CreateProposal(tester.TokenService.ContractAddress,
                nameof(TokenMethod.Approve), approveInput, organizationAddress, proposer);
            ApproveWithAssociation(tester, proposal, organizationAddress);
            ReleaseWithAssociation(tester, proposal, proposer);
        }

        protected Address CreateAssociationController(string creator, CreateOrganizationInput input)
        {
            var address = MainServices.AssociationService.CreateOrganization(input);
            return address;
        }

        #endregion

        #region ParliamentAuth Method

        protected TransactionResultDto Approve(ContractServices services, string account, string proposalId)
        {
            services.ParliamentService.SetAccount(account);
            var result = services.ParliamentService.ExecuteMethodWithResult(ParliamentMethod.Approve,
                HashHelper.HexStringToHash(proposalId));

            return result;
        }

        protected TransactionResultDto ReleaseSideChainCreation(ContractServices services, string account,
            Hash proposalId)
        {
            services.CrossChainService.SetAccount(account);
            var transactionResult =
                services.CrossChainService.ExecuteMethodWithResult(CrossChainContractMethod.ReleaseSideChainCreation,
                    new ReleaseSideChainCreationInput {ProposalId = proposalId});
            return transactionResult;
        }

        #endregion

        #region Token Method

        //action
        protected TransactionResultDto TransferToken(ContractServices services, string owner, string spender,
            long amount, string symbol)
        {
            var transfer = services.TokenService.ExecuteMethodWithResult(TokenMethod.Transfer, new TransferInput
            {
                Symbol = symbol,
                To = AddressHelper.Base58StringToAddress(spender),
                Amount = amount,
                Memo = "Transfer Token"
            });
            return transfer;
        }

        protected TransactionResultDto TokenApprove(ContractServices services, string owner, long amount)
        {
            services.TokenService.SetAccount(owner);

            var result = services.TokenService.ExecuteMethodWithResult(TokenMethod.Approve,
                new ApproveInput
                {
                    Symbol = NodeOption.NativeTokenSymbol,
                    Spender = AddressHelper.Base58StringToAddress(services.CrossChainService.ContractAddress),
                    Amount = amount
                });

            return result;
        }

        protected TransactionResultDto CrossChainReceive(ContractServices services, string account,
            CrossChainReceiveTokenInput input)
        {
            services.TokenService.SetAccount(account);
            var result = services.TokenService.ExecuteMethodWithResult(TokenMethod.CrossChainReceiveToken, input);
            return result;
        }

        //view
        protected GetBalanceOutput GetBalance(ContractServices services, string account, string symbol)
        {
            var balance = services.TokenService.CallViewMethod<GetBalanceOutput>(TokenMethod.GetBalance,
                new GetBalanceInput
                {
                    Owner = AddressHelper.Base58StringToAddress(account),
                    Symbol = symbol
                });
            return balance;
        }

        protected string GetPrimaryTokenSymbol(ContractServices services)
        {
            var symbol = services.TokenService.GetPrimaryTokenSymbol();
            return symbol;
        }

        #endregion
    }
}