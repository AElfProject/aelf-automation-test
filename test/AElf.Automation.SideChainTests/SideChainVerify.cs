using System;
using System.Collections.Generic;
using Acs3;
using Acs7;
using AElfChain.Common;
using AElfChain.Common.Contracts;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Contracts.MultiToken;
using AElf.Types;
using AElfChain.SDK.Models;
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

        #region register

        [TestMethod]
        public void RegisterTokenAddress()
        {
            ValidateChainTokenAddress(MainContracts);
            foreach (var sideTester in SideTester)
            {
                ValidateChainTokenAddress(sideTester);
            }
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

            var merklePath = GetMerklePath(blockNumber, txid, SideTester[sideNum].ContractServices);
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
            registerInput.MerklePath.MerklePathNodes.AddRange(crossChainMerkleProofContext.MerklePathForParentChainRoot
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
            var merklePath = GetMerklePath(blockNumber, txid, MainContracts.ContractServices);
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
                GetMerklePath(blockNumber, txid, SideTester[sideNum].ContractServices);
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
                .MerklePathForParentChainRoot
                .MerklePathNodes);

            Proposal(SideTester[registerSideNum].ContractServices, sideChainRegisterInputA);
            _logger.Info(
                $"Chain {SideTester[registerSideNum].ContractServices.ChainId} register chain {SideTester[sideNum].ContractServices.ChainId} token address {SideTester[sideNum].ContractServices.TokenService.ContractAddress}");
        }

        [TestMethod]
        public void MainChainCrossChainTransferSideChain()
        {
            var crossChainTransferInput = new CrossChainTransferInput()
            {
                Symbol = NodeOption.NativeTokenSymbol,
                IssueChainId = MainContracts.ContractServices.ChainId,
                Amount = 10000,
                Memo = "cross chain transfer",
                To = AddressHelper.Base58StringToAddress(InitAccount),
                ToChainId = SideTester[0].ContractServices.ChainId,
            };
            // execute cross chain transfer
            var rawTx = MainContracts.NodeManager.GenerateRawTransaction(InitAccount,
                MainContracts.ContractServices.TokenService.ContractAddress, TokenMethod.CrossChainTransfer.ToString(),
                crossChainTransferInput);
            _logger.Info($"Transaction rawTx is: {rawTx}");
            var txId = MainContracts.ExecuteMethodWithTxId(rawTx);
            var txResult = CheckTransactionResult(MainContracts.ContractServices, txId);
            // get transaction info            
            var status = txResult.Status.ConvertTransactionResultStatus();

            _logger.Info(
                $"Cross chain Transaction block: {txResult.BlockNumber}, rawTx: {rawTx}, txId:{txId} to chain {SideTester[0].ContractServices.ChainId}");
        }

//        [TestMethod]
//        [DataRow("2826", "deddcce68edf84a910fceb6acbd274f0c6a942065e23a2537ca7511343013767",
//            "0a220a20baa28ffec57c135c7015a88d4ade469ec128d5e68e9d2ddf86121f9821dc982a12220a2043a0f4a61fd597aee85d15e13bfa96e70b82a7071ca25e62c3176a80b8231ae21889162204c8768cca2a1243726f7373436861696e5472616e73666572324d0a220a20baa28ffec57c135c7015a88d4ade469ec128d5e68e9d2ddf86121f9821dc982a1203454c4618a09c01221463726f737320636861696e207472616e736665722882f4a701309bf4e10482f10441c13721edb8bd44856fe4e23e7bc82e58bc9ced26d215e0d1e9ccb269d832fd1216550b0462ba86b08facc5d0679fca4f61b6c05773c21a6f73089afa5c23bcf200")]
//        public void SideChainReceivedMainChain(string blockNumber, string txid, string rawTx)
//        {
//            var merklePath = GetMerklePath(blockNumber, txid, MainContracts);
//
//            var crossChainReceiveToken = new CrossChainReceiveTokenInput
//            {
//                FromChainId = MainContracts.ContractServices.ChainId,
//                ParentChainHeight = long.Parse(blockNumber)
//            };
//            crossChainReceiveToken.MerklePath.MerklePathNodes.Add(merklePath.MerklePathNodes);
//            crossChainReceiveToken.TransferTransactionBytes =
//                ByteString.CopyFrom(ByteArrayHelper.HexStringToByteArray(rawTx));
//            ;
//
//            var result = SideAContracts.CrossChainReceive(InitAccount, crossChainReceiveToken);
//
//            //verify
//            var balance = SideAContracts.GetBalance(InitAccount, "ELF");
//            _logger.Info($"balance: {balance}");
//        }

        
        [TestMethod]
        public void SideChainCrossChainTransferMainChain()
        {

//            var issue = sideAServices.TokenService.ExecuteMethodWithResult(TokenMethod.Issue, new IssueInput
//            {
//                Symbol = "STA",
//                Amount = 100_0000,
//                Memo = "issue side chain token on main chain",
//                To = AddressHelper.Base58StringToAddress(InitAccount)
//            });

            var crossChainTransferInput = new CrossChainTransferInput()
            {
                Symbol = "STA",
                IssueChainId = sideAServices.ChainId,
                Amount = 100_0000,
                Memo = "cross chain transfer",
                To = AddressHelper.Base58StringToAddress("oqBFyjdWqZF6QKhVfBGmxA5Xz2mVJdC6jERdyC11EELjGSp5x"),
                ToChainId = MainContracts.ContractServices.ChainId,
            };

            var sideChainTokenContracts = sideAServices.TokenService.ContractAddress;
            _logger.Info($"{sideChainTokenContracts}");
            
            // execute cross chain transfer
            var rawTx = sideAServices.NodeManager.GenerateRawTransaction(InitAccount,
                sideChainTokenContracts, nameof(TokenMethod.CrossChainTransfer),
                crossChainTransferInput);
            _logger.Info($"Transaction rawTx is: {rawTx}");
            var txId = sideAServices.NodeManager.ApiService.SendTransactionAsync(rawTx).Result.TransactionId;
            var txResult = CheckTransactionResult(sideAServices, txId);
            // get transaction info            
            var status = txResult.Status.ConvertTransactionResultStatus();

            _logger.Info(
                $"Cross chain Transaction block: {txResult.BlockNumber}, rawTx: {rawTx}, txId:{txId} to chain {MainContracts.ContractServices.ChainId}");
        }

        [TestMethod]
        [DataRow("134968", "8c00f2cb34418c882e2d774b4ae515609197779929d61b97dc2007cac0642813",
            "0a220a20cd9ba9d03d499bde0e1f75ee30eb6bdfe80065b1b37b9bfa9a47d3cb3e0ea62512220a2080ee395414cb759fc3a997f2b1a3db506aa9779bebbdef653aec7121fd681da618b19e0822042ccbdbd52a1243726f7373436861696e5472616e73666572324d0a220a206a55ae0e235e2619cc9c4af0fe4d3cc61b475a2e81aafe3f2dc00a0b9bd742a512035354411880897a221463726f737320636861696e207472616e7366657228ceae80023082f4a70182f104418b019637e5bd1fa9dfd5ab10683b4300755970b190ce984cfc9a780f50c2f6683490fad40d856f237518cb7c403f3f41f4dc651fa41cb61867f9007310374ffc01")]
        public void MainChainReceivedSideChain(string blockNumber, string txid, string rawTx)
        {
            var merklePath = GetMerklePath(blockNumber, txid, sideAServices);

            var crossChainReceiveToken = new CrossChainReceiveTokenInput
            {
                FromChainId = sideAServices.ChainId,
                MerklePath = merklePath
            };
            // verify side chain transaction
            var crossChainMerkleProofContext = GetCrossChainMerkleProofContext(sideAServices, long.Parse(blockNumber));
            crossChainReceiveToken.MerklePath.MerklePathNodes.AddRange(crossChainMerkleProofContext
                .MerklePathForParentChainRoot.MerklePathNodes);
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
            var crossChainTransferInput = new CrossChainTransferInput()
            {
                Symbol = "ELF",
                IssueChainId = MainContracts.ContractServices.ChainId,
                Amount = 1000,
                Memo = "cross chain transfer",
                To = AddressHelper.Base58StringToAddress(InitAccount),
                ToChainId = SideTester[1].ContractServices.ChainId,
            };
            // execute cross chain transfer
            var rawTx = SideTester[0].NodeManager.GenerateRawTransaction(InitAccount,
                SideTester[0].ContractServices.TokenService.ContractAddress, TokenMethod.CrossChainTransfer.ToString(),
                crossChainTransferInput);
            _logger.Info($"Transaction rawTx is: {rawTx}");
            var txId = SideTester[0].ExecuteMethodWithTxId(rawTx);
            var txResult = CheckTransactionResult(SideTester[0].ContractServices, txId);
            // get transaction info            
            var status = txResult.Status.ConvertTransactionResultStatus();

            _logger.Info(
                $"Cross chain Transaction block: {txResult.BlockNumber}, rawTx: {rawTx}, txId:{txId} to chain {SideTester[1].ContractServices.ChainId}");
        }

        #endregion

        private void ValidateChainTokenAddress(ContractTester tester)
        {
            var rawTx = tester.ValidateTokenAddress();
            var txId = tester.ExecuteMethodWithTxId(rawTx);
            var txResult = CheckTransactionResult(tester.ContractServices, txId);
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
                services.ParliamentService.CallViewMethod<Address>(ParliamentMethod.GetGenesisOwnerAddress,
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
        
        protected CrossChainMerkleProofContext GetCrossChainMerkleProofContext(ContractServices services,
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
    }
}