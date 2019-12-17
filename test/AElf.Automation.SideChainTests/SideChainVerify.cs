using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Acs3;
using Acs7;
using AElf.Client.Dto;
using AElfChain.Common;
using AElfChain.Common.Contracts;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Contracts.MultiToken;
using AElf.Kernel;
using AElf.Types;
using AElfChain.Common.DtoExtension;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ApproveInput = Acs3.ApproveInput;

namespace AElf.Automation.SideChainTests
{
    [TestClass]
    public class SideChainVerify : SideChainTestBase
    {
        public static List<ContractTester> SideTester;

        [TestInitialize]
        public void InitializeNodeTests()
        {
            Initialize();
        }

        private void ValidateChainTokenAddress(ContractTester tester)
        {
            var rawTx = tester.ValidateTokenAddress();
            var txId = tester.ExecuteMethodWithTxId(rawTx);
            var txResult = tester.NodeManager.CheckTransactionResult(txId);
            if (txResult.Status.ConvertTransactionResultStatus() != TransactionResultStatus.Mined)
                Assert.IsTrue(false, $"Validate chain {tester.ContractServices.ChainId} token contract failed");
            _logger.Info($"Validate Transaction block: {txResult.BlockNumber}, rawTx: {rawTx}, txId:{txId}");
            _logger.Info(
                $"Validate chain {tester.ContractServices.ChainId} token address {tester.TokenService.ContractAddress}");
        }

        private void Proposal(ContractServices services, IMessage input)
        {
            //get default organization
            var organizationAddress =
                services.ParliamentService.CallViewMethod<Address>(ParliamentMethod.GetDefaultOrganizationAddress,
                    new Empty());
            //create proposal
            var createProposalInput = new CreateProposalInput
            {
                OrganizationAddress = organizationAddress,
                ToAddress = AddressHelper.Base58StringToAddress(services.TokenService.ContractAddress),
                ContractMethodName = TokenMethod.RegisterCrossChainTokenContractAddress.ToString(),
                ExpiredTime = DateTime.UtcNow.AddDays(1).ToTimestamp(),
                Params = input.ToByteString()
            };
            var createProposalResult =
                services.ParliamentService.ExecuteMethodWithResult(ParliamentMethod.CreateProposal,
                    createProposalInput);
            if (createProposalResult.Status.ConvertTransactionResultStatus() != TransactionResultStatus.Mined) return;
            var proposalId = createProposalResult.ReadableReturnValue.Replace("\"", "");

            //approve
            var miners = GetMiners(services);
            foreach (var miner in miners)
            {
                services.ParliamentService.SetAccount(miner.GetFormatted());
                var approveResult = services.ParliamentService.ExecuteMethodWithResult(ParliamentMethod.Approve,
                    new ApproveInput
                    {
                        ProposalId = HashHelper.HexStringToHash(proposalId)
                    });
                if (approveResult.Status.ConvertTransactionResultStatus() != TransactionResultStatus.Mined) return;
            }

            services.ParliamentService.SetAccount(InitAccount);
            var releaseResult
                = services.ParliamentService.ExecuteMethodWithResult(ParliamentMethod.Release,
                    HashHelper.HexStringToHash(proposalId));
            if (releaseResult.Status.ConvertTransactionResultStatus() != TransactionResultStatus.Mined)
                Assert.IsTrue(false,
                    $"Release proposal failed, token address can't register on chain {services.ChainId}");
        }

        // get miners
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

        private CrossChainMerkleProofContext GetCrossChainMerkleProofContext(ContractServices services,
            long blockHeight)
        {
            var crossChainMerkleProofContext =
                services.CrossChainService.CallViewMethod<CrossChainMerkleProofContext>(
                    CrossChainContractMethod.GetBoundParentChainHeightAndMerklePathByHeight, new SInt64Value
                    {
                        Value = blockHeight
                    });
            _logger.Info("Get CrossChain Merkle Proof");
            return crossChainMerkleProofContext;
        }

        #region register

        [TestMethod]
        public void RegisterTokenAddress()
        {
            ValidateChainTokenAddress(MainContracts);
            foreach (var sideTester in SideTester) ValidateChainTokenAddress(sideTester);
        }

        [TestMethod]
        [DataRow(1, "834", "2d855e9ca868166fac39a74492971ab8b62d82c799ddbb7032aa981827b9ba4b",
            "0a220a20baa28ffec57c135c7015a88d4ade469ec128d5e68e9d2ddf86121f9821dc982a12220a20eac92c9fdb86bf19b1933b2f884b69ddebc376be13357f7afe7cc4eccf7b22f118c1062204451a03ec2a1d56616c696461746553797374656d436f6e74726163744164647265737332480a220a20a2a00f8583c08daa00b80b0bbac4684396fe966b683ea956a63bd8845eee6ae712220a20778e3006a12cc609d78bad825f6bc18ff1e354ec7fdaaa02de71c0983abbf70582f10441a96a75bd8f65c979a2e0e20f43e8a0b6c4193833f39e1a0da5348ed3998e838b0c12445a2add09efd5254251a033236560ce5fef9a588b77cd56bf0eed6940a200")]
//        [DataRow(0,"888","dfabdd6355ff5c2429c55bbca7c91b73349a92c1eaa14f5e56ac2430a80e4975","0a220a20baa28ffec57c135c7015a88d4ade469ec128d5e68e9d2ddf86121f9821dc982a12220a209aba3474df140748412c938a2798b24461da0e95d1139bcbe2d74e3c4695526f18f6062204274daa842a1d56616c696461746553797374656d436f6e74726163744164647265737332480a220a20a2a00f8583c08daa00b80b0bbac4684396fe966b683ea956a63bd8845eee6ae712220a2080ee395414cb759fc3a997f2b1a3db506aa9779bebbdef653aec7121fd681da682f10441545beb266f309fd2d094cdbce0e0853f9e3005d617a2ae3968057992652fa4983127b7ba9875352c764f171d76f55eadb04873e164eb77bedf556ddea09aab7d01")]
        public void MainChainRegisterSideChain(int sideNum, string blockNumber, string txid, string rawTx)
        {
            var crossChainMerkleProofContext =
                SideTester[sideNum]
                    .GetBoundParentChainHeightAndMerklePathByHeight(InitAccount, long.Parse(blockNumber));

            var merklePath = GetMerklePath(blockNumber, txid, SideTester[sideNum].ContractServices, out var root);
            if (merklePath == null)
                Assert.IsTrue(false, "Can't get the merkle path.");

            var registerInput = new RegisterCrossChainTokenContractAddressInput
            {
                FromChainId = SideTester[sideNum].ContractServices.ChainId,
                ParentChainHeight = crossChainMerkleProofContext.BoundParentChainHeight,
                TokenContractAddress =
                    AddressHelper.Base58StringToAddress(SideTester[sideNum].ContractServices.TokenService
                        .ContractAddress),
                TransactionBytes = ByteString.CopyFrom(ByteArrayHelper.HexStringToByteArray(rawTx))
            };
            registerInput.MerklePath.MerklePathNodes.AddRange(merklePath.MerklePathNodes);
            registerInput.MerklePath.MerklePathNodes.AddRange(crossChainMerkleProofContext.MerklePathFromParentChain
                .MerklePathNodes);
            Proposal(MainContracts.ContractServices, registerInput);
            _logger.Info(
                $"Main chain register chain {SideTester[sideNum].ContractServices.ChainId} token address {SideTester[sideNum].ContractServices.TokenService.ContractAddress}");
        }

        [TestMethod]
        [DataRow("1138", "4f110113873cf2f13766cf00da27d1bb221fed1d75dc8612d352410a84dcd42e",
            "0a220a20baa28ffec57c135c7015a88d4ade469ec128d5e68e9d2ddf86121f9821dc982a12220a20dd8eea50c31966e06e4a2662bebef7ed81d09a47b2eb1eb3729f2f0cc78129ae18f1082204095dfde42a1d56616c696461746553797374656d436f6e74726163744164647265737332480a220a20a2a00f8583c08daa00b80b0bbac4684396fe966b683ea956a63bd8845eee6ae712220a2043a0f4a61fd597aee85d15e13bfa96e70b82a7071ca25e62c3176a80b8231ae282f1044171c30ca8dc89e8cbb1bcc3f70b1026c9c6294c168c41f46e5b684861615f2c4a46d83ce294f66c5712b71e35f149f615d10b4fee2524b9f42d8913901b15558900")]
        public void SideChainRegisterMainChain(string blockNumber, string txid, string rawTx)
        {
            //register main chain token address
            var merklePath = GetMerklePath(blockNumber, txid, MainContracts.ContractServices,out var root);
            if (merklePath == null)
                Assert.IsTrue(false, "Can't get the merkle path.");
            var registerInput = new RegisterCrossChainTokenContractAddressInput
            {
                FromChainId = MainContracts.ContractServices.ChainId,
                ParentChainHeight = long.Parse(blockNumber),
                TokenContractAddress =
                    AddressHelper.Base58StringToAddress(MainContracts.ContractServices.TokenService.ContractAddress),
                TransactionBytes = ByteString.CopyFrom(ByteArrayHelper.HexStringToByteArray(rawTx))
            };
            registerInput.MerklePath.MerklePathNodes.AddRange(merklePath.MerklePathNodes);

            foreach (var tester in SideTester)
            {
                Proposal(tester.ContractServices, registerInput);
                _logger.Info(
                    $"Chain {tester.ContractServices.ChainId} register Main chain token address {MainContracts.ContractServices.TokenService.ContractAddress}");
            }
        }

        [TestMethod]
        [DataRow(0, 1, "888", "dfabdd6355ff5c2429c55bbca7c91b73349a92c1eaa14f5e56ac2430a80e4975",
            "0a220a20baa28ffec57c135c7015a88d4ade469ec128d5e68e9d2ddf86121f9821dc982a12220a209aba3474df140748412c938a2798b24461da0e95d1139bcbe2d74e3c4695526f18f6062204274daa842a1d56616c696461746553797374656d436f6e74726163744164647265737332480a220a20a2a00f8583c08daa00b80b0bbac4684396fe966b683ea956a63bd8845eee6ae712220a2080ee395414cb759fc3a997f2b1a3db506aa9779bebbdef653aec7121fd681da682f10441545beb266f309fd2d094cdbce0e0853f9e3005d617a2ae3968057992652fa4983127b7ba9875352c764f171d76f55eadb04873e164eb77bedf556ddea09aab7d01")]
        [DataRow(1, 0, "834", "2d855e9ca868166fac39a74492971ab8b62d82c799ddbb7032aa981827b9ba4b",
            "0a220a20baa28ffec57c135c7015a88d4ade469ec128d5e68e9d2ddf86121f9821dc982a12220a20eac92c9fdb86bf19b1933b2f884b69ddebc376be13357f7afe7cc4eccf7b22f118c1062204451a03ec2a1d56616c696461746553797374656d436f6e74726163744164647265737332480a220a20a2a00f8583c08daa00b80b0bbac4684396fe966b683ea956a63bd8845eee6ae712220a20778e3006a12cc609d78bad825f6bc18ff1e354ec7fdaaa02de71c0983abbf70582f10441a96a75bd8f65c979a2e0e20f43e8a0b6c4193833f39e1a0da5348ed3998e838b0c12445a2add09efd5254251a033236560ce5fef9a588b77cd56bf0eed6940a200")]
        public void SideChainRegisterSideChain(int sideNum, int registerSideNum, string blockNumber, string txid,
            string rawTx)
        {
            var crossChainMerkleProofContextA =
                SideTester[sideNum]
                    .GetBoundParentChainHeightAndMerklePathByHeight(InitAccount, long.Parse(blockNumber));
            var sideChainMerklePathA =
                GetMerklePath(blockNumber, txid, SideTester[sideNum].ContractServices,out var root);
            if (sideChainMerklePathA == null)
                Assert.IsTrue(false, "Can't get the merkle path.");
            var sideChainRegisterInputA = new RegisterCrossChainTokenContractAddressInput
            {
                FromChainId = SideTester[sideNum].ContractServices.ChainId,
                ParentChainHeight = crossChainMerkleProofContextA.BoundParentChainHeight,
                TokenContractAddress =
                    AddressHelper.Base58StringToAddress(SideTester[0].ContractServices.TokenService.ContractAddress),
                TransactionBytes = ByteString.CopyFrom(ByteArrayHelper.HexStringToByteArray(rawTx))
            };
            sideChainRegisterInputA.MerklePath.MerklePathNodes.AddRange(sideChainMerklePathA.MerklePathNodes);
            sideChainRegisterInputA.MerklePath.MerklePathNodes.AddRange(crossChainMerkleProofContextA
                .MerklePathFromParentChain
                .MerklePathNodes);

            Proposal(SideTester[registerSideNum].ContractServices, sideChainRegisterInputA);
            _logger.Info(
                $"Chain {SideTester[registerSideNum].ContractServices.ChainId} register chain {SideTester[sideNum].ContractServices.ChainId} token address {SideTester[sideNum].ContractServices.TokenService.ContractAddress}");
        }

        [TestMethod]
        public void MainChainCrossChainTransferSideChain()
        {
            var crossChainTransferInput = new CrossChainTransferInput
            {
                Symbol = NodeOption.NativeTokenSymbol,
                IssueChainId = MainContracts.ContractServices.ChainId,
                Amount = 1_00000000,
                Memo = "cross chain transfer",
                To = AddressHelper.Base58StringToAddress(InitAccount),
                ToChainId = SideContractTester3.ContractServices.ChainId,
            };
            // execute cross chain transfer
            var rawTx = MainContracts.NodeManager.GenerateRawTransaction(InitAccount,
                MainContracts.ContractServices.TokenService.ContractAddress, TokenMethod.CrossChainTransfer.ToString(),
                crossChainTransferInput);
            _logger.Info($"Transaction rawTx is: {rawTx}");
            var txId = MainContracts.ExecuteMethodWithTxId(rawTx);
            var txResult = MainContracts.NodeManager.CheckTransactionResult(txId);
            // get transaction info            
            var status = txResult.Status.ConvertTransactionResultStatus();

            _logger.Info(
                $"Cross chain Transaction block: {txResult.BlockNumber}, rawTx: {rawTx}, txId:{txId} to chain {SideContractTester1.ContractServices.ChainId}");
        }

        [TestMethod]
        [DataRow("5510273", "2dd1258a7ac475bc81652ee073fc4be4b8005cd5e9ce312811150aeb728a72ae",
            "0a220a20baa28ffec57c135c7015a88d4ade469ec128d5e68e9d2ddf86121f9821dc982a12220a208d3c0f7c83c8fd069f648afbe7e443f5fcf2c5fd7dc3ae63984a51698af0f0e718fea8d0022204a082e6fb2a1243726f7373436861696e5472616e73666572324d0a220a20baa28ffec57c135c7015a88d4ade469ec128d5e68e9d2ddf86121f9821dc982a1203454c46188084af5f221463726f737320636861696e207472616e736665722898f579309bf4e10482f10441ac0551d088dd1eefbcc2ad941c72f611a70fa3962ffc86e5f0bb7d8de993e0e43fc3d5fdf63964475d227b31fe998b4333c330e8ae5024c1cc86ee89f08aaf2d01")]
        public void SideChainReceivedMainChain(string blockNumber, string txid, string rawTx)
        {
            var merklePath = GetMerklePath(blockNumber, txid, MainContracts.ContractServices,out var root);

            var crossChainReceiveToken = new CrossChainReceiveTokenInput
            {
                FromChainId = MainContracts.ContractServices.ChainId,
                ParentChainHeight = long.Parse(blockNumber),
                MerklePath = merklePath
            };
            crossChainReceiveToken.TransferTransactionBytes =
                ByteString.CopyFrom(ByteArrayHelper.HexStringToByteArray(rawTx));


            var result = SideContractTester3.CrossChainReceive(InitAccount, crossChainReceiveToken);

            //verify
            var balance = SideContractTester1.GetBalance(InitAccount, "ELF");
            _logger.Info($"balance: {balance}");
        }
        
        #endregion

        
        [TestMethod]
        public void MainChainToDvvCrossChainTransferSideChain()
        {
            var crossChainTransferInput = new CrossChainTransferInput()
            {
                Symbol = "STA",
                IssueChainId = SideContractTester1.ContractServices.ChainId,
                Amount = 100000_00000000,
                Memo = "cross chain transfer",
                To = AddressHelper.Base58StringToAddress(InitAccount),
                ToChainId = SideContractTester11.ContractServices.ChainId,
            };
            // execute cross chain transfer
            var rawTx = SideContractTester1.NodeManager.GenerateRawTransaction(InitAccount,
                SideContractTester1.ContractServices.TokenService.ContractAddress, TokenMethod.CrossChainTransfer.ToString(),
                crossChainTransferInput);
            _logger.Info($"Transaction rawTx is: {rawTx}");
            var txId = SideContractTester1.ExecuteMethodWithTxId(rawTx);
            var txResult = SideContractTester1.NodeManager.CheckTransactionResult(txId);
            // get transaction info            
            var status = txResult.Status.ConvertTransactionResultStatus();

            _logger.Info(
                $"Cross chain Transaction block: {txResult.BlockNumber}, rawTx: {rawTx}, txId:{txId} to chain {SideContractTester1.ContractServices.ChainId}");
        }

        [TestMethod]
        [DataRow("1906", "8e9eaeaddde844e3d7a0b41ead22853da16d1ad62bf7bf47b68d6da96d0af547",
            "0a220a20baa28ffec57c135c7015a88d4ade469ec128d5e68e9d2ddf86121f9821dc982a12220a208d3c0f7c83c8fd069f648afbe7e443f5fcf2c5fd7dc3ae63984a51698af0f0e718f00e2204049052f02a1243726f7373436861696e5472616e7366657232500a220a20baa28ffec57c135c7015a88d4ade469ec128d5e68e9d2ddf86121f9821dc982a1203454c4618808095e789c604221463726f737320636861696e207472616e736665722898f571309bf4e10482f104417106decded4092633824de2875b8fb8d3b378967f3c906f1724e0decc508ab7b546cbad07e81a8ca6e207fb2df49ca7c166ff3d415ccfa847457aef098af7a4c00")]
        public void SideChainAZcPReceivedMainChain(string blockNumber, string txid, string rawTx)
        {
            var merklePath = GetMerklePath(blockNumber, txid, MainContracts.ContractServices,out var root);

            var crossChainReceiveToken = new CrossChainReceiveTokenInput
            {
                FromChainId = MainContracts.ContractServices.ChainId,
                ParentChainHeight = long.Parse(blockNumber),
                MerklePath = merklePath
            };
            crossChainReceiveToken.TransferTransactionBytes =
                ByteString.CopyFrom(ByteArrayHelper.HexStringToByteArray(rawTx));


            var result = SideContractTester3.CrossChainReceive(InitAccount, crossChainReceiveToken);

            //verify
            var balance = SideContractTester1.GetBalance(InitAccount, "ELF");
            _logger.Info($"balance: {balance}");
        }
        

        [TestMethod]
        public void SideChainCrossChainTransferMainChain()
        {
            var issue = sideAServices.TokenService.ExecuteMethodWithResult(TokenMethod.Issue, new IssueInput
            {
                Symbol = "STA",
                Amount = 25000_0000_00000000,
                Memo = "issue side chain token on main chain",
                To = AddressHelper.Base58StringToAddress(InitAccount)
            });

            var crossChainTransferInput = new CrossChainTransferInput
            {
                Symbol = "STA",
                IssueChainId = sideAServices.ChainId,
                Amount = 25000_0000_00000000,
                Memo = "cross chain transfer",
                To = AddressHelper.Base58StringToAddress(InitAccount),
                ToChainId = MainContracts.ContractServices.ChainId
            };

            var sideChainTokenContracts = sideAServices.TokenService.ContractAddress;
            _logger.Info($"{sideChainTokenContracts}");

            // execute cross chain transfer
            var rawTx = sideAServices.NodeManager.GenerateRawTransaction(InitAccount,
                sideChainTokenContracts, nameof(TokenMethod.CrossChainTransfer),
                crossChainTransferInput);
            _logger.Info($"Transaction rawTx is: {rawTx}");
            var txId = sideAServices.NodeManager.SendTransaction(rawTx);
            var txResult = sideAServices.NodeManager.CheckTransactionResult(txId);
            // get transaction info            
            var status = txResult.Status.ConvertTransactionResultStatus();

            _logger.Info(
                $"Cross chain Transaction block: {txResult.BlockNumber}, rawTx: {rawTx}, txId:{txId} to chain {MainContracts.ContractServices.ChainId}");
        }

        [TestMethod]
        [DataRow("19452", "208c854757299753f30dbee37869b511773f3f6eb04db92bdfd7e6945d270123",
            "0a220a20baa28ffec57c135c7015a88d4ade469ec128d5e68e9d2ddf86121f9821dc982a12220a203025dca79298bfc07abe7cfc0c9d51c0306462f073768f7c549a53e108b19cd918f8970122043c08dfb82a1243726f7373436861696e5472616e7366657232510a220a20baa28ffec57c135c7015a88d4ade469ec128d5e68e9d2ddf86121f9821dc982a120353544118808094f6c2d7e858221463726f737320636861696e207472616e73666572289bf4e1043098f57182f104417c4a80452427f7ab042e4fcc62db6bff5cca1d9daa823e4c5b49f59eff130a6a4d9617cb86d7429cffcc210137cb098a79ea5ba132c902be54b47d1165f6d24d01")]
        public void MainChainReceivedSideChain(string blockNumber, string txid, string rawTx)
        {
            var merklePath = GetMerklePath(blockNumber, txid, sideAServices,out var root);

            var crossChainReceiveToken = new CrossChainReceiveTokenInput
            {
                FromChainId = sideAServices.ChainId,
                MerklePath = merklePath
            };
            // verify side chain transaction
            var crossChainMerkleProofContext = GetCrossChainMerkleProofContext(sideAServices, long.Parse(blockNumber));
            crossChainReceiveToken.MerklePath.MerklePathNodes.AddRange(crossChainMerkleProofContext
                .MerklePathFromParentChain.MerklePathNodes);
            crossChainReceiveToken.ParentChainHeight = crossChainMerkleProofContext.BoundParentChainHeight;
            crossChainReceiveToken.TransferTransactionBytes =
                ByteString.CopyFrom(ByteArrayHelper.HexStringToByteArray(rawTx));


            var result = MainContracts.CrossChainReceive(InitAccount, crossChainReceiveToken);

            //verify
            var balance = MainContracts.GetBalance(InitAccount, "STA");
            _logger.Info($"balance: {balance}");
        }


        [TestMethod]
        public void SideChainACrossChainTransferSideChainB()
        {
            //Transfer
            var crossChainTransferInput = new CrossChainTransferInput
            {
                Symbol = "ELF",
                IssueChainId = MainContracts.ContractServices.ChainId,
                Amount = 1000,
                Memo = "cross chain transfer",
                To = AddressHelper.Base58StringToAddress(InitAccount),
                ToChainId = SideContractTester2.ContractServices.ChainId
            };
            // execute cross chain transfer
            var rawTx = SideContractTester1.NodeManager.GenerateRawTransaction(InitAccount,
                SideContractTester1.ContractServices.TokenService.ContractAddress, TokenMethod.CrossChainTransfer.ToString(),
                crossChainTransferInput);
            _logger.Info($"Transaction rawTx is: {rawTx}");
            var txId = SideContractTester1.ExecuteMethodWithTxId(rawTx);
            var txResult = SideContractTester1.NodeManager.CheckTransactionResult(txId);
            // get transaction info            
            var status = txResult.Status.ConvertTransactionResultStatus();

            _logger.Info(
                $"Cross chain Transaction block: {txResult.BlockNumber}, rawTx: {rawTx}, txId:{txId} to chain {SideTester[1].ContractServices.ChainId}");
        }
        
        [TestMethod]
        public async Task CrossChainData()
        {
            var balance = MainContracts.TokenService.GetUserBalance(InitAccount);
            _logger.Info($"{balance}");
            var blocks = new List<BlockDto>();
            for (int i = 511; i < 521; i++)
            {
                var block = await SideContractTester1.NodeManager.ApiClient.GetBlockByHeightAsync(i,true);
                blocks.Add(block);
            }
            
            var crossChainData = new CrossChainBlockData();

            for (int i = 1; i < blocks.Count; i++)
            {
                var blockHeader = new BlockHeader(HashHelper.HexStringToHash(blocks[i-1].BlockHash));
                var height = blocks[i].Header.Height;
                var txId = blocks[i].Body.Transactions.First();
                GetMerklePath(height.ToString(), txId,
                    SideContractTester1.ContractServices,out var root);
                var sideChainBlockDate = new SideChainBlockData
                {
                    ChainId = ChainHelper.ConvertBase58ToChainId("tDVV"),
                    BlockHeaderHash = Hash.FromMessage(blockHeader),
                    Height = height,
                    TransactionStatusMerkleTreeRoot = root
                };
                crossChainData.SideChainBlockData.Add(sideChainBlockDate);
            }

            crossChainData.PreviousBlockHeight = 3900;
           
            var result =
                MainContracts.CrossChainService.ExecuteMethodWithResult(CrossChainContractMethod.RecordCrossChainData,crossChainData);
            
            var afterbalance = MainContracts.TokenService.GetUserBalance(InitAccount);
            _logger.Info($"{afterbalance}");
        }

        [TestMethod]
        public void CheckBalance()
        {
            var balance = MainContracts.GetBalance(MainContracts.CrossChainService.ContractAddress,
                MainContracts.TokenService.GetPrimaryTokenSymbol());
            _logger.Info($"{balance}");
        }

        [TestMethod]

        public void GetIndexParentHeight()
        {
            var height1 =  sideAServices.CrossChainService.CallViewMethod<SInt64Value>(
                CrossChainContractMethod.GetParentChainHeight, new Empty()).Value;
            _logger.Info($"{height1}");
            
            var height2 =  sideBServices.CrossChainService.CallViewMethod<SInt64Value>(
                CrossChainContractMethod.GetParentChainHeight, new Empty()).Value;
            _logger.Info($"{height2}");

        }
        
        [TestMethod]
        public void GetIndexSideHeight()
        {
            var height1 =  MainContracts.CrossChainService.CallViewMethod<SInt64Value>(
                CrossChainContractMethod.GetSideChainHeight, new SInt32Value {Value = sideAServices.ChainId}).Value;
            _logger.Info($"{height1}");
            
            var height2 =  MainContracts.CrossChainService.CallViewMethod<SInt64Value>(
                CrossChainContractMethod.GetSideChainHeight, new SInt32Value {Value = sideBServices.ChainId}).Value;
            _logger.Info($"{height2}");
        }
    }
}