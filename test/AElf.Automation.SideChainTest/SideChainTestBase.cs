using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Acs0;
using Acs3;
using AElf.Standards.ACS7;
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
        protected static readonly ILog Logger = Log4NetHelper.GetLogger();
        public AuthorityManager AuthorityManager;
        public AuthorityManager SideAuthorityManager;
        public ContractServices MainServices;
        public ContractServices SideAServices;
        public ContractServices SideBServices;
        public List<ContractServices> SideServices;
        public List<string> Miners;
        public static string InitAccount;
        public static string TestAccount = "2oSMWm1tjRqVdfmrdL8dgrRvhWu1FP8wcZidjS6wPbuoVtxhEz";
        public static string OtherAccount = "h6CRCFAhyozJPwdFRd7i8A5zVAqy171AVty3uMQUQp1MB9AKa";
        public static string MemberAccount = "2frDVeV6VxUozNqcFbgoxruyqCRAuSyXyfCaov6bYWc7Gkxkh2";
        public List<string> Members;
        public TokenContractContainer.TokenContractStub TokenContractStub;

        protected void Initialize()
        {
            //Init Logger
            Log4NetHelper.LogInit();
            NodeInfoHelper.SetConfig("nodes-env2-main");
            InitAccount = ConfigInfoHelper.Config.MainChainInfos.Account;
            var mainUrl = ConfigInfoHelper.Config.MainChainInfos.MainChainUrl;
            var password = ConfigInfoHelper.Config.MainChainInfos.Password;
            var sideUrls = ConfigInfoHelper.Config.SideChainInfos.Select(l => l.SideChainUrl).ToList();
            Members = new List<string> {InitAccount, TestAccount, OtherAccount, MemberAccount};
            Logger.Info($"url :{mainUrl}");

            MainServices = new ContractServices(mainUrl, InitAccount, password);
            SideServices = new List<ContractServices>();
            foreach (var side in sideUrls)
            {
                var sideServices = new ContractServices(side, InitAccount, password);
                SideServices.Add(sideServices);
            }

            SideAServices = SideServices.First();
            SideBServices = new ContractServices(sideUrls[1], InitAccount, NodeOption.DefaultPassword);
            
            AuthorityManager = new AuthorityManager(MainServices.NodeManager,InitAccount);
            SideAuthorityManager = new AuthorityManager(SideAServices.NodeManager,InitAccount);
            SideAuthorityManager = new AuthorityManager(SideBServices.NodeManager,InitAccount);

            TokenContractStub = MainServices.TokenContractStub;
            Miners = new List<string>();
            Miners = AuthorityManager.GetCurrentMiners();
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
                var transactionId = Hash.LoadFromHex(transactionIds[num]);
                var txRes = transactionStatus[num];
                var rawBytes = transactionId.ToByteArray().Concat(Encoding.UTF8.GetBytes(txRes))
                    .ToArray();
                var txIdWithStatus = HashHelper.ComputeFrom(rawBytes);
                txIdsWithStatus.Add(txIdWithStatus);
                if (!transactionIds[num].Equals(txId)) continue;
                index = num;
                Logger.Info($"The transaction index is {index}");
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
                    Address = services.TokenService.Contract,
                    SystemContractHashName = HashHelper.ComputeFrom("AElf.ContractNames.Token")
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
                Address = creator.ConvertAddress(),
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
                    Hash.LoadFromHex(proposalId));
            return result;
        }

        public void ApproveProposal(ContractServices services, Hash proposalId)
        {
            var miners = GetMiners(services);
            foreach (var miner in miners)
            {
                services.ParliamentService.SetAccount(miner.ToBase58(), "123");
                var balance = services.TokenService.GetUserBalance(miner.ToBase58());
                if (balance < 1000_00000000 && !miner.ToBase58().Equals(InitAccount))
                {
                    services.TokenService.TransferBalance(InitAccount, miner.ToBase58(), 1000_00000000);
                }

                var result = services.ParliamentService.ExecuteMethodWithResult(ParliamentMethod.Approve, proposalId);
                result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            }
        }

        protected void ApproveWithAssociation(ContractServices tester, Hash proposalId, Address association)
        {
            var organization =
                tester.AssociationService.CallViewMethod<Organization>(AssociationMethod.GetOrganization,
                    association);
            var members = organization.OrganizationMemberList.OrganizationMembers.ToList();
            foreach (var member in members)
            {
                tester.AssociationService.SetAccount(member.ToBase58());
                var approve =
                    tester.AssociationService.ExecuteMethodWithResult(AssociationMethod.Approve, proposalId);
                approve.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
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
                Spender = tester.CrossChainService.Contract,
                Symbol = "ELF",
                Amount = amount
            };
            var proposal = tester.AssociationService.CreateProposal(tester.TokenService.ContractAddress,
                nameof(TokenMethod.Approve), approveInput, organizationAddress, proposer);
            ApproveWithAssociation(tester, proposal, organizationAddress);
            ReleaseWithAssociation(tester, proposal, proposer);
        }
        #endregion

        #region Parliament Method

        protected TransactionResultDto Approve(ContractServices services, string account, string proposalId)
        {
            services.ParliamentService.SetAccount(account);
            var result = services.ParliamentService.ExecuteMethodWithResult(ParliamentMethod.Approve,
                Hash.LoadFromHex(proposalId));

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
                To = spender.ConvertAddress(),
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
                    Spender = services.CrossChainService.Contract,
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
                    Owner = account.ConvertAddress(),
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