using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Acs3;
using AElf.Standards.ACS7;
using AElf.Contracts.Parliament;
using AElf.Types;
using AElfChain.Common.Contracts;
using AElfChain.Common.DtoExtension;
using Google.Protobuf;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Volo.Abp.Threading;

namespace AElf.Automation.SideChainTests
{
    [TestClass]
    public class SideChainIndexTest : SideChainTestBase
    {
        [TestInitialize]
        public void InitializeNodeTests()
        {
            Initialize();
        }

        [TestMethod]
        public void ProposeCrossChainIndexing()
        {
            string txId = "";
            int times = 10;
            while (txId.Equals("") && times > 0)
            {
                foreach (var miner in Miners)
                {
                    MainServices.CrossChainService.SetAccount(miner);
                    var height = AsyncHelper.RunSync(() => MainServices.NodeManager.ApiClient.GetBlockHeightAsync());
                    var indexHeight = MainServices.CrossChainService.GetSideChainHeight(SideBServices.ChainId);
                    var sideChainBlockDataList = new List<SideChainBlockData>();
                    var sideChainHeight =
                        AsyncHelper.RunSync(() => SideBServices.NodeManager.ApiClient.GetBlockHeightAsync());
                    var checkHeight = sideChainHeight > indexHeight + 10 ? indexHeight + 10 : sideChainHeight;
                    for (var h = indexHeight + 10; h <= checkHeight; h++)
                    {
                        var blockHash = AsyncHelper.RunSync(() =>
                            SideBServices.NodeManager.ApiClient.GetBlockByHeightAsync(h, false));
                        var sideChainBlockData = new SideChainBlockData
                        {
                            BlockHeaderHash = Hash.LoadFromHex(blockHash.BlockHash),
                            Height = h,
                            ChainId = SideBServices.ChainId,
                            TransactionStatusMerkleTreeRoot = HashHelper.ComputeFrom(h)
                        };
                        sideChainBlockDataList.Add(sideChainBlockData);
                    }

                    var input = new CrossChainBlockData
                    {
                        ParentChainBlockDataList = { },
                        SideChainBlockDataList = {sideChainBlockDataList},
                        PreviousBlockHeight = height
                    };
                    var result =
                        MainServices.CrossChainService.ExecuteMethodWithResult(
                            CrossChainContractMethod.ProposeCrossChainIndexing, input);
                    times--;
                    if (!result.Status.ConvertTransactionResultStatus().Equals(TransactionResultStatus.Mined)) continue;
                    txId = result.TransactionId;
                    break;
                }
            }

            Approve(txId);
        }
        
        [TestMethod]
        public void  ReleaseCrossChainIndexingProposal()
        {
            var miner = Miners.Take(1).ToList().First();
            for (int i = 0; i < 10; i++)
            {
                MainServices.CrossChainService.SetAccount(miner);
                var input = new ReleaseCrossChainIndexingProposalInput
                {
                    ChainIdList = {SideBServices.ChainId}
                };
                var result =
                    MainServices.CrossChainService.ExecuteMethodWithResult(
                        CrossChainContractMethod.ReleaseCrossChainIndexingProposal, input);
                if(result.Status.ConvertTransactionResultStatus().Equals(TransactionResultStatus.Mined))
                    break;
            }
        }
        
        [TestMethod]
        public void  Approve(string txId)
        {
            var resultDto = AsyncHelper.RunSync(() => MainServices.NodeManager.ApiClient.GetTransactionResultAsync(txId));
   
            var proposalCreated =
                ProposalCreated.Parser.ParseFrom(
                    ByteString.FromBase64(resultDto.Logs.Last(l =>l.Name.Contains("ProposalCreated")).NonIndexed));

                foreach (var miner in Miners)
                {
                    MainServices.ParliamentService.SetAccount(miner);
                    var result =
                        MainServices.ParliamentService.ExecuteMethodWithTxId(
                            ParliamentMethod.Approve, proposalCreated.ProposalId);
                }
                Thread.Sleep(500);

            var proposalInfo1 = MainServices.ParliamentService.CheckProposal(proposalCreated.ProposalId);
            Logger.Info(proposalInfo1.ToBeReleased);
        }
    }
}