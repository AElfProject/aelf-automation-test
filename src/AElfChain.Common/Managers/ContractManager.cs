using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AElf;
using AElf.Contracts.Association;
using AElf.Contracts.Configuration;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Contracts.CrossChain;
using AElf.Contracts.Election;
using AElf.Contracts.Genesis;
using AElf.Contracts.MultiToken;
using AElf.Contracts.Parliament;
using AElf.Contracts.Profit;
using AElf.Contracts.Referendum;
using AElf.Contracts.TokenConverter;
using AElf.Contracts.TokenHolder;
using AElf.Contracts.Treasury;
using AElf.Contracts.Vote;
using AElf.Types;
using AElfChain.Common.Contracts;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Helpers;
using log4net;
using TokenContract = AElfChain.Common.Contracts.TokenContract;

namespace AElfChain.Common.Managers
{
    public class ContractManager
    {
        private AuthorityManager _authorityManager;
        private Dictionary<string, string> _systemContracts;

        public ILog Logger = Log4NetHelper.GetLogger();

        public ContractManager(INodeManager nodeManager, string callAddress)
        {
            NodeManager = nodeManager;
            CallAddress = callAddress;
            CallAccount = callAddress.ConvertAddress();
            ChainIdName = NodeManager.GetChainId();
            ChainId = ChainHelper.ConvertBase58ToChainId(ChainIdName);
            Genesis = GenesisContract.GetGenesisContract(NodeManager, CallAddress);
            GenesisStub = Genesis.GetGensisStub(CallAddress);
        }

        public ContractManager(string endpoint, string callAddress)
        {
            NodeManager = new NodeManager(endpoint);
            CallAddress = callAddress;
            CallAccount = callAddress.ConvertAddress();
            ChainIdName = NodeManager.GetChainId();
            ChainId = ChainHelper.ConvertBase58ToChainId(ChainIdName);
            Genesis = GenesisContract.GetGenesisContract(NodeManager, CallAddress);
            GenesisStub = Genesis.GetGensisStub(CallAddress);
        }

        public int ChainId { get; set; }
        public string ChainIdName { get; set; }
        public INodeManager NodeManager { get; set; }
        public string CallAddress { get; set; }
        public Address CallAccount { get; set; }
        public AuthorityManager Authority => GetAuthority();
        public Dictionary<string, string> SystemContracts => GetSystemContracts();

        public async Task<long> CheckSideChainBlockIndex(long txHeight, int sideChainId)
        {
            var stopwatch = Stopwatch.StartNew();
            while (true)
            {
                var indexSideHeight = CrossChain.GetSideChainHeight(sideChainId);
                if (indexSideHeight < txHeight)
                {
                    Console.Write(
                        $"\r[Main->Side]Current index height: {indexSideHeight}, target index height: {txHeight}. Time using: {CommonHelper.ConvertMileSeconds(stopwatch.ElapsedMilliseconds)}");
                    await Task.Delay(2000);
                    continue;
                }

                Console.Write(
                    $"\r[Main->Side]Current index height: {indexSideHeight}, target index height: {txHeight}. Time using: {CommonHelper.ConvertMileSeconds(stopwatch.ElapsedMilliseconds)}");
                Console.WriteLine();
                stopwatch.Stop();
                var mainHeight = await NodeManager.ApiClient.GetBlockHeightAsync();
                return mainHeight;
            }
        }

        public async Task CheckParentChainBlockIndex(long blockHeight)
        {
            var stopwatch = Stopwatch.StartNew();
            while (true)
            {
                var indexHeight = CrossChain.GetParentChainHeight();
                if (blockHeight > indexHeight)
                {
                    Console.Write(
                        $"\r[Side->Main]Current index height: {indexHeight}, target index height: {blockHeight}. Time using: {CommonHelper.ConvertMileSeconds(stopwatch.ElapsedMilliseconds)}");
                    await Task.Delay(2000);
                    continue;
                }

                Console.Write(
                    $"\r[Side->Main]Current index height: {indexHeight}, target index height: {blockHeight}. Time using: {CommonHelper.ConvertMileSeconds(stopwatch.ElapsedMilliseconds)}");
                Console.WriteLine();
                stopwatch.Stop();
                break;
            }
        }

        public async Task<MerklePath> GetMerklePath(long blockNumber, string txId)
        {
            var index = 0;
            var blockInfoResult =
                await NodeManager.ApiClient.GetBlockByHeightAsync(blockNumber, true);
            var transactionIds = blockInfoResult.Body.Transactions;
            var transactionStatus = new List<string>();

            foreach (var transactionId in transactionIds)
            {
                var txResult = await NodeManager.ApiClient.GetTransactionResultAsync(transactionId);
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
            }

            var bmt = BinaryMerkleTree.FromLeafNodes(txIdsWithStatus);
            var merklePath = new MerklePath();
            merklePath.MerklePathNodes.AddRange(bmt.GenerateMerklePath(index).MerklePathNodes);

            return merklePath;
        }

        public string GetContractAddress(string name)
        {
            if (SystemContracts.ContainsKey(name))
                return SystemContracts[name];

            return null;
        }

        private AuthorityManager GetAuthority()
        {
            if (_authorityManager == null)
                _authorityManager = new AuthorityManager(NodeManager, Genesis.CallAddress);

            return _authorityManager;
        }

        private Dictionary<string, string> GetSystemContracts()
        {
            if (_systemContracts == null)
            {
                var contracts = Genesis.GetAllSystemContracts();
                _systemContracts = new Dictionary<string, string>();
                foreach (var key in contracts.Keys)
                {
                    if (contracts[key].Equals(new Address())) continue;
                    _systemContracts.Add(key.ToString(), contracts[key].ToBase58());
                }
            }

            return _systemContracts;
        }

        #region Contracts services and stub

        public GenesisContract Genesis { get; set; }
        public BasicContractZeroContainer.BasicContractZeroStub GenesisStub { get; set; }
        public BasicContractZeroImplContainer.BasicContractZeroImplStub GenesisImplStub => Genesis.GetGenesisImplStub();
        public TokenContract Token => Genesis.GetTokenContract();
        public TokenContractContainer.TokenContractStub TokenStub => Genesis.GetTokenStub();
        public TokenContractImplContainer.TokenContractImplStub TokenImplStub => Genesis.GetTokenImplStub();
        public TokenHolderContract TokenHolder => Genesis.GetTokenHolderContract();
        public TokenHolderContractContainer.TokenHolderContractStub TokenHolderStub => Genesis.GetTokenHolderStub();
        public TokenHolderContractImplContainer.TokenHolderContractImplStub TokenHolderImplStub => Genesis.GetTokenHolderImplStub();
        public TokenConverterContract TokenConverter => Genesis.GetTokenConverterContract();
        public TokenConverterContractContainer.TokenConverterContractStub TokenconverterStub =>
            Genesis.GetTokenConverterStub();
        public TokenConverterContractImplContainer.TokenConverterContractImplStub TokenconverterImplStub =>
            Genesis.GetTokenConverterImplStub();
        public ConfigurationContract Configuration => Genesis.GetConfigurationContract();
        public ConfigurationContainer.ConfigurationStub ConfigurationStub => Genesis.GetConfigurationStub();
        public ConfigurationImplContainer.ConfigurationImplStub ConfigurationImplStub => Genesis.GetConfigurationImplStub();
        public ConsensusContract Consensus => Genesis.GetConsensusContract();
        public AEDPoSContractContainer.AEDPoSContractStub ConsensusStub => Genesis.GetConsensusStub();
        public AEDPoSContractImplContainer.AEDPoSContractImplStub ConsensusImplStub => Genesis.GetConsensusImplStub();
        public CrossChainContract CrossChain => Genesis.GetCrossChainContract();
        public CrossChainContractContainer.CrossChainContractStub CrossChainStub => Genesis.GetCrossChainStub();
        public CrossChainContractImplContainer.CrossChainContractImplStub CrossChainImplStub => Genesis.GetCrossChainImplStub();
        public ParliamentContract Parliament => Genesis.GetParliamentContract();
        public ParliamentContractContainer.ParliamentContractStub ParliamentAuthStub =>
            Genesis.GetParliamentAuthStub();
        public ParliamentContractImplContainer.ParliamentContractImplStub ParliamentContractImplStub =>
            Genesis.GetParliamentAuthImplStub();
        public AssociationContract Association => Genesis.GetAssociationAuthContract();
        public AssociationContractContainer.AssociationContractStub AssociationStub => Genesis.GetAssociationAuthStub();
        public AssociationContractImplContainer.AssociationContractImplStub AssociationImplStub => Genesis.GetAssociationAuthImplStub();
        public ReferendumContract Referendum => Genesis.GetReferendumAuthContract();
        public ReferendumContractContainer.ReferendumContractStub ReferendumStub =>
            Genesis.GetReferendumAuthStub();
        public ReferendumContractImplContainer.ReferendumContractImplStub ReferendumImplStub =>
            Genesis.GetReferendumAuthImplStub();
        public ElectionContract Election => Genesis.GetElectionContract();
        public ElectionContractContainer.ElectionContractStub ElectionStub => Genesis.GetElectionStub();
        public ElectionContractImplContainer.ElectionContractImplStub ElectionContractImplStub => Genesis.GetElectionImplStub();
        public VoteContract Vote => Genesis.GetVoteContract();
        public VoteContractContainer.VoteContractStub VoteStub => Genesis.GetVoteStub();
        public VoteContractImplContainer.VoteContractImplStub VoteImplStub => Genesis.GetVoteImplStub();
        public ProfitContract Profit => Genesis.GetProfitContract();
        public ProfitContractContainer.ProfitContractStub ProfitStub => Genesis.GetProfitStub();
        public ProfitContractImplContainer.ProfitContractImplStub ProfitImplStub => Genesis.GetProfitImplStub();
        public TreasuryContract Treasury => Genesis.GetTreasuryContract();
        public TreasuryContractContainer.TreasuryContractStub TreasuryStub => Genesis.GetTreasuryStub();
        public TreasuryContractImplContainer.TreasuryContractImplStub TreasuryImplStub => Genesis.GetTreasuryImplStub();

        #endregion
    }
}