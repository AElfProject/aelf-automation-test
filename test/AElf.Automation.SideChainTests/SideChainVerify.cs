using System;
using System.Collections.Generic;
using Acs3;
using AElf.Automation.Common.Contracts;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Contracts.MultiToken;
using AElf.Types;
using AElfChain.SDK.Models;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AElf.Automation.SideChainTests
{
    [TestClass]
    public class SideChainVerify : SideChainTestBase
    {
        public static string SideARpcUrl { get; } = "http://127.0.0.1:9001";
        public static string SideBRpcUrl { get; } = "http://127.0.0.1:9002";

        public static List<ContractTester> SideTester;

        [TestInitialize]
        public void InitializeNodeTests()
        {
            SideTester = new List<ContractTester>();
            Initialize();
            var testerA = GetSideChain(SideARpcUrl, InitAccount, "2112");
            var testerB = GetSideChain(SideBRpcUrl, InitAccount, "2113");
            SideTester.Add(testerA);
            SideTester.Add(testerB);
        }

        #region register

        [TestMethod]
        public void RegisterTokenAddress()
        {
            ValidateChainTokenAddress(Tester);
            foreach (var sideTester in SideTester)
            {
                ValidateChainTokenAddress(sideTester);
            }
        }

        [TestMethod]
        [DataRow(1,"834","2d855e9ca868166fac39a74492971ab8b62d82c799ddbb7032aa981827b9ba4b","0a220a20baa28ffec57c135c7015a88d4ade469ec128d5e68e9d2ddf86121f9821dc982a12220a20eac92c9fdb86bf19b1933b2f884b69ddebc376be13357f7afe7cc4eccf7b22f118c1062204451a03ec2a1d56616c696461746553797374656d436f6e74726163744164647265737332480a220a20a2a00f8583c08daa00b80b0bbac4684396fe966b683ea956a63bd8845eee6ae712220a20778e3006a12cc609d78bad825f6bc18ff1e354ec7fdaaa02de71c0983abbf70582f10441a96a75bd8f65c979a2e0e20f43e8a0b6c4193833f39e1a0da5348ed3998e838b0c12445a2add09efd5254251a033236560ce5fef9a588b77cd56bf0eed6940a200")]
//        [DataRow(0,"888","dfabdd6355ff5c2429c55bbca7c91b73349a92c1eaa14f5e56ac2430a80e4975","0a220a20baa28ffec57c135c7015a88d4ade469ec128d5e68e9d2ddf86121f9821dc982a12220a209aba3474df140748412c938a2798b24461da0e95d1139bcbe2d74e3c4695526f18f6062204274daa842a1d56616c696461746553797374656d436f6e74726163744164647265737332480a220a20a2a00f8583c08daa00b80b0bbac4684396fe966b683ea956a63bd8845eee6ae712220a2080ee395414cb759fc3a997f2b1a3db506aa9779bebbdef653aec7121fd681da682f10441545beb266f309fd2d094cdbce0e0853f9e3005d617a2ae3968057992652fa4983127b7ba9875352c764f171d76f55eadb04873e164eb77bedf556ddea09aab7d01")]
        public void MainChainRegisterSideChain(int sideNum, string blockNumber, string txid, string rawTx)
        {

                var crossChainMerkleProofContext =
                    SideTester[sideNum].GetBoundParentChainHeightAndMerklePathByHeight(InitAccount, long.Parse(blockNumber));

                var merklePath = GetMerklePath(blockNumber, txid,  SideTester[sideNum]);
                if (merklePath == null)
                    Assert.IsTrue(false, "Can't get the merkle path.");

                var registerInput = new RegisterCrossChainTokenContractAddressInput
                {
                    FromChainId =  SideTester[sideNum].ContractServices.ChainId,
                    ParentChainHeight = crossChainMerkleProofContext.BoundParentChainHeight,
                    TokenContractAddress =
                        AddressHelper.Base58StringToAddress( SideTester[sideNum].ContractServices.TokenService.ContractAddress),
                    TransactionBytes = ByteString.CopyFrom(ByteArrayHelper.HexStringToByteArray(rawTx))
                };
                registerInput.MerklePath.MerklePathNodes.AddRange(merklePath.MerklePathNodes);
                registerInput.MerklePath.MerklePathNodes.AddRange(crossChainMerkleProofContext.MerklePathForParentChainRoot.MerklePathNodes);
                Proposal(Tester.ContractServices, registerInput);
                _logger.Info(
                    $"Main chain register chain { SideTester[sideNum].ContractServices.ChainId} token address { SideTester[sideNum].ContractServices.TokenService.ContractAddress}");
        }

        [TestMethod]
        [DataRow("1138","4f110113873cf2f13766cf00da27d1bb221fed1d75dc8612d352410a84dcd42e","0a220a20baa28ffec57c135c7015a88d4ade469ec128d5e68e9d2ddf86121f9821dc982a12220a20dd8eea50c31966e06e4a2662bebef7ed81d09a47b2eb1eb3729f2f0cc78129ae18f1082204095dfde42a1d56616c696461746553797374656d436f6e74726163744164647265737332480a220a20a2a00f8583c08daa00b80b0bbac4684396fe966b683ea956a63bd8845eee6ae712220a2043a0f4a61fd597aee85d15e13bfa96e70b82a7071ca25e62c3176a80b8231ae282f1044171c30ca8dc89e8cbb1bcc3f70b1026c9c6294c168c41f46e5b684861615f2c4a46d83ce294f66c5712b71e35f149f615d10b4fee2524b9f42d8913901b15558900")]
        public void SideChainRegisterMainChain(string blockNumber, string txid, string rawTx)
        {
            //register main chain token address
            var merklePath = GetMerklePath(blockNumber, txid, Tester);
            if (merklePath == null)
                Assert.IsTrue(false, "Can't get the merkle path.");
            var registerInput = new RegisterCrossChainTokenContractAddressInput
            {
                FromChainId = Tester.ContractServices.ChainId,
                ParentChainHeight = long.Parse(blockNumber),
                TokenContractAddress =
                    AddressHelper.Base58StringToAddress(Tester.ContractServices.TokenService.ContractAddress),
                TransactionBytes = ByteString.CopyFrom(ByteArrayHelper.HexStringToByteArray(rawTx))
            };
            registerInput.MerklePath.MerklePathNodes.AddRange(merklePath.MerklePathNodes);

            foreach (var tester in SideTester)
            {
                Proposal(tester.ContractServices, registerInput);
                _logger.Info(
                    $"Chain {tester.ContractServices.ChainId} register Main chain token address {Tester.ContractServices.TokenService.ContractAddress}");
            }
        }

        [TestMethod]
        [DataRow(0,1,"888","dfabdd6355ff5c2429c55bbca7c91b73349a92c1eaa14f5e56ac2430a80e4975","0a220a20baa28ffec57c135c7015a88d4ade469ec128d5e68e9d2ddf86121f9821dc982a12220a209aba3474df140748412c938a2798b24461da0e95d1139bcbe2d74e3c4695526f18f6062204274daa842a1d56616c696461746553797374656d436f6e74726163744164647265737332480a220a20a2a00f8583c08daa00b80b0bbac4684396fe966b683ea956a63bd8845eee6ae712220a2080ee395414cb759fc3a997f2b1a3db506aa9779bebbdef653aec7121fd681da682f10441545beb266f309fd2d094cdbce0e0853f9e3005d617a2ae3968057992652fa4983127b7ba9875352c764f171d76f55eadb04873e164eb77bedf556ddea09aab7d01")]
        [DataRow(1,0,"834","2d855e9ca868166fac39a74492971ab8b62d82c799ddbb7032aa981827b9ba4b","0a220a20baa28ffec57c135c7015a88d4ade469ec128d5e68e9d2ddf86121f9821dc982a12220a20eac92c9fdb86bf19b1933b2f884b69ddebc376be13357f7afe7cc4eccf7b22f118c1062204451a03ec2a1d56616c696461746553797374656d436f6e74726163744164647265737332480a220a20a2a00f8583c08daa00b80b0bbac4684396fe966b683ea956a63bd8845eee6ae712220a20778e3006a12cc609d78bad825f6bc18ff1e354ec7fdaaa02de71c0983abbf70582f10441a96a75bd8f65c979a2e0e20f43e8a0b6c4193833f39e1a0da5348ed3998e838b0c12445a2add09efd5254251a033236560ce5fef9a588b77cd56bf0eed6940a200")]
        public void SideChainRegisterSideChain(int sideNum, int registerSideNum,string blockNumber, string txid, string rawTx)
        {
            var crossChainMerkleProofContextA =
                SideTester[sideNum].GetBoundParentChainHeightAndMerklePathByHeight(InitAccount, long.Parse(blockNumber));
            var sideChainMerklePathA =
                GetMerklePath(blockNumber, txid, SideTester[sideNum]);
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
            sideChainRegisterInputA.MerklePath.MerklePathNodes.AddRange(crossChainMerkleProofContextA.MerklePathForParentChainRoot
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
                Symbol = "ELF",
                IssueChainId = Tester.ContractServices.ChainId,
                Amount = 10000,
                Memo = "cross chain transfer",
                To = AddressHelper.Base58StringToAddress(InitAccount),
                ToChainId = SideTester[0].ContractServices.ChainId,
            };
            // execute cross chain transfer
            var rawTx = Tester.ApiHelper.GenerateTransactionRawTx(InitAccount,
                Tester.ContractServices.TokenService.ContractAddress, TokenMethod.CrossChainTransfer.ToString(),
                crossChainTransferInput);
            _logger.Info($"Transaction rawTx is: {rawTx}");
            var txId = Tester.ExecuteMethodWithTxId(rawTx);
            var result =CheckTransactionResult(Tester.ContractServices,txId);
            // get transaction info            
            var txResult = result.InfoMsg as TransactionResultDto;
            var status = txResult.Status.ConvertTransactionResultStatus();
            
            _logger.Info($"Cross chain Transaction block: {txResult.BlockNumber}, rawTx: {rawTx}, txId:{txId} to chain {SideTester[0].ContractServices.ChainId}");
        }

        [TestMethod]
        [DataRow("2826","deddcce68edf84a910fceb6acbd274f0c6a942065e23a2537ca7511343013767","0a220a20baa28ffec57c135c7015a88d4ade469ec128d5e68e9d2ddf86121f9821dc982a12220a2043a0f4a61fd597aee85d15e13bfa96e70b82a7071ca25e62c3176a80b8231ae21889162204c8768cca2a1243726f7373436861696e5472616e73666572324d0a220a20baa28ffec57c135c7015a88d4ade469ec128d5e68e9d2ddf86121f9821dc982a1203454c4618a09c01221463726f737320636861696e207472616e736665722882f4a701309bf4e10482f10441c13721edb8bd44856fe4e23e7bc82e58bc9ced26d215e0d1e9ccb269d832fd1216550b0462ba86b08facc5d0679fca4f61b6c05773c21a6f73089afa5c23bcf200")]
        public void SideChainReceivedMainChain(string blockNumber, string txid,string rawTx)
        {
            var merklePath = GetMerklePath(blockNumber, txid, Tester);

            var crossChainReceiveToken = new CrossChainReceiveTokenInput
            {
                FromChainId = Tester.ContractServices.ChainId,
                ParentChainHeight = long.Parse(blockNumber)
            };
            crossChainReceiveToken.MerklePath.MerklePathNodes.Add(merklePath.MerklePathNodes);
            crossChainReceiveToken.TransferTransactionBytes =
                ByteString.CopyFrom(ByteArrayHelper.HexStringToByteArray(rawTx)); ;
            
           var result = SideTester[0].CrossChainReceive(InitAccount, crossChainReceiveToken);
            
            //verify
            var balance = SideTester[0].GetBalance(InitAccount, "ELF");
            _logger.Info($"balance: {balance}");
        }

        [TestMethod]
        public void SideChainACrossChainTransferSideChainB()
        {
            //Transfer
            var crossChainTransferInput = new CrossChainTransferInput()
            {
                Symbol = "ELF",
                IssueChainId = Tester.ContractServices.ChainId,
                Amount = 1000,
                Memo = "cross chain transfer",
                To = AddressHelper.Base58StringToAddress(InitAccount),
                ToChainId = SideTester[1].ContractServices.ChainId,
            };
            // execute cross chain transfer
            var rawTx = SideTester[0].ApiHelper.GenerateTransactionRawTx(InitAccount,
                SideTester[0].ContractServices.TokenService.ContractAddress, TokenMethod.CrossChainTransfer.ToString(),
                crossChainTransferInput);
            _logger.Info($"Transaction rawTx is: {rawTx}");
            var txId = SideTester[0].ExecuteMethodWithTxId(rawTx);
            var result =CheckTransactionResult(SideTester[0].ContractServices,txId);
            // get transaction info            
            var txResult = result.InfoMsg as TransactionResultDto;
            var status = txResult.Status.ConvertTransactionResultStatus();
            
            _logger.Info($"Cross chain Transaction block: {txResult.BlockNumber}, rawTx: {rawTx}, txId:{txId} to chain {SideTester[1].ContractServices.ChainId}");
        }

        #endregion


//        #region cross chain transfer
//
//        [TestMethod]
//        [DataRow("2A1RKFfxeh2n7nZpcci6t8CcgbJMGz9a7WGpC94THpiTK3U7nG", 2750978)]
//        public void MainChainTransferSideChainA(string accountA, int toChainId)
//        {
//            //get token info
//            var tokenInfo = Tester.GetTokenInfo("ELF");
//            //Transfer
//            var result = Tester.CrossChainTransfer(InitAccount, accountA, tokenInfo, toChainId, 1000);
//            var resultReturn = result.InfoMsg as TransactionResultDto;
//            var blockNumber = resultReturn.BlockNumber;
//            _logger.Info($"Block Number: {blockNumber}");
//        }
//
//        [TestMethod]
//        [DataRow("2A1RKFfxeh2n7nZpcci6t8CcgbJMGz9a7WGpC94THpiTK3U7nG","138809","bc58078bdd74e9d2e76250a416a6e67858eb2522f6635018d86537e705fe3aeb","0a220a200e0859791a72bc512b4b91edfd12116daddf7c9f8915d608e2313f4772e761df12220a2043a0f4a61fd597aee85d15e13bfa96e70b82a7071ca25e62c3176a80b8231ae218a9bc082204e7db11582a1243726f7373436861696e5472616e73666572328a010a220a209825ea6ae8e17764b3d6561088d21134d8683419ef386257ae7e9404f47fd34212440a03454c461209656c6620746f6b656e1880a8d6b9072080a8d6b907280432220a20dd8eea50c31966e06e4a2662bebef7ed81d09a47b2eb1eb3729f2f0cc78129ae380118d00f22167472616e7366657220746f207369646520636861696e2882f4a70182f1044106db22096561cfc70b0faaa57c5bd521d59d359db341e05754c9a65ff1ec0192050a11375c02076b3dc4e03f5ca8b8fa5c78ed9636af47c79b9c4556a922f03801")]
//        public void SideChainAReceive(string accountA,string blockNumber, string txid,string rawTx)
//        {
//            var merklePath = GetMerklePath(blockNumber, txid, Tester);
//
//            var crossChainReceiveToken = new CrossChainReceiveTokenInput
//            {
//                FromChainId = 9992731,
//                ParentChainHeight = long.Parse(blockNumber)
//            };
//            crossChainReceiveToken.MerklePath.AddRange(merklePath.Path);
//            crossChainReceiveToken.TransferTransactionBytes =
//                ByteString.CopyFrom(ByteArrayHelper.HexStringToByteArray(rawTx));
//
//            SideChainA = ChangeRpc(SideARpcUrl);
//            TesterA = ChangeToSideChain(SideChainA,sideChainAccount);
//            TesterA.CrossChainReceive(InitAccount, crossChainReceiveToken);
//            
//            //verify
//            var balance = TesterA.GetBalance(accountA, "ELF");
//            _logger.Info($"balance: {balance}");
//
//            var tokenInfo = TesterA.GetTokenInfo("ELF");
//            _logger.Info($"Token: {tokenInfo}");
//        }
//
//        [TestMethod]
//        [DataRow("2A1RKFfxeh2n7nZpcci6t8CcgbJMGz9a7WGpC94THpiTK3U7nG", "2A1RKFfxeh2n7nZpcci6t8CcgbJMGz9a7WGpC94THpiTK3U7nG",2113)]
//        public void SideChainATransferSideChainB(string accountA, string accountB, int toChainId)
//        {
//            SideChainA = ChangeRpc(SideARpcUrl);
//            TesterA = ChangeToSideChain(SideChainA, sideChainAccount);
//            //get tokenInfo
//            var tokenInfo = TesterA.GetTokenInfo("ELF");
//            //Transfer
//            var result = Tester.CrossChainTransfer(accountA, accountB, tokenInfo, toChainId, 1000);
//            var resultReturn = result.InfoMsg as TransactionResultDto;
//            var blockNumber = resultReturn.BlockNumber;
//            _logger.Info($"Block Number: {blockNumber}");
//        }
//
//        [TestMethod]
//        [DataRow("", "", "")]
//        public void SideChainBReceive(string accountB, string blockNumber, string txid, string rawTx)
//        {
//            SideChainA = ChangeRpc(SideARpcUrl);
//            TesterA = ChangeToSideChain(SideChainA, sideChainAccount);
//
//            var merklePath = GetMerklePath(blockNumber, txid, TesterA);
//
//            var crossChainReceiveToken = new CrossChainReceiveTokenInput
//            {
//                FromChainId = 2750978,
//            };
//            crossChainReceiveToken.MerklePath.AddRange(merklePath.Path);
//
//            // verify side chain transaction
//            var crossChainMerkleProofContext =
//                TesterA.GetBoundParentChainHeightAndMerklePathByHeight(sideChainAccount, long.Parse(blockNumber));
//            crossChainReceiveToken.MerklePath.AddRange(crossChainMerkleProofContext.MerklePathForParentChainRoot.Path);
//            crossChainReceiveToken.ParentChainHeight = crossChainMerkleProofContext.BoundParentChainHeight;
//            crossChainReceiveToken.TransferTransactionBytes =
//                ByteString.CopyFrom(ByteArrayHelper.HexStringToByteArray(rawTx));
//
//            //receive in side chain B
//            SideChainB = ChangeRpc(SideBRpcUrl);
//            TesterB = ChangeToSideChain(SideChainB, sideChainBccount);
//            TesterB.CrossChainReceive(accountB, crossChainReceiveToken);
//
//            //verify
//            var balance = TesterA.GetBalance(accountB, "ELF");
//            _logger.Info($"balance: {balance}");
//
//            var tokenInfo = TesterA.GetTokenInfo("ELF");
//            _logger.Info($"Token: {tokenInfo}");
//        }
//
//        [TestMethod]
//        [DataRow("", "")]
//        public void SideChainTransferMainChain(string accountB, string accountM, int toChainId)
//        {
//            SideChainA = ChangeRpc(SideARpcUrl);
//            TesterA = ChangeToSideChain(SideChainA,sideChainAccount);
//            //get ELF token info
//            var tokenInfo = TesterB.GetTokenInfo("ELF");
//            //Transfer
//            var result = TesterB.CrossChainTransfer(accountB, accountM, tokenInfo, toChainId, 1000);
//            var resultReturn = result.InfoMsg as TransactionResultDto;
//            var blockNumber = resultReturn.BlockNumber;
//            _logger.Info($"Block Number: {blockNumber}");
//        }
//
//        [TestMethod]
//        [DataRow("2iimYTf2mn134pAsRqRT2a1kEadNVGkBZdNgE8na9y4RnwiPRU","2460",1,"0a220a20e26d40e0021a6dd128cda7d682d8cd9dba10feda55d8c689d1563fb0b313c8e812220a2080ee395414cb759fc3a997f2b1a3db506aa9779bebbdef653aec7121fd681da61893132204dfcc9d022a1243726f7373436861696e5472616e736665723288010a220a200640bf6b4c86d22ebf32cd8f96135c421990326febdc94256d3b0f5f1ac89ff712440a03454c461209656c6620746f6b656e18fca7d6b9072080a8d6b907280432220a20dd8eea50c31966e06e4a2662bebef7ed81d09a47b2eb1eb3729f2f0cc78129ae380118c801221463726f737320636861696e207472616e73666572289bf4e10482f104412bd970d3de35175f33e74e0563a82b1e87792096ce5c72859f29d23a425f905512bc028dbd27ecc68995f2934405d66ee55a6bc06d47cfdc0a639aa620e4f93a01")]
//        public void MainChainReceive(string accountM,string blockNumber,string txid,string rawTx)
//        {
//            SideChainB = ChangeRpc(SideBRpcUrl);
//            TesterB = ChangeToSideChain(SideChainB, sideChainBccount);
//
//            var merklePath = GetMerklePath(blockNumber, txid, TesterB);
//
//            var crossChainReceiveToken = new CrossChainReceiveTokenInput
//            {
//                FromChainId = 2750978,
//            };
//            crossChainReceiveToken.MerklePath.AddRange(merklePath.Path);
//
//            // verify side chain transaction
//            var crossChainMerkleProofContext =
//                TesterB.GetBoundParentChainHeightAndMerklePathByHeight(sideChainAccount, long.Parse(blockNumber));
//            crossChainReceiveToken.MerklePath.AddRange(crossChainMerkleProofContext.MerklePathForParentChainRoot.Path);
//            crossChainReceiveToken.ParentChainHeight = crossChainMerkleProofContext.BoundParentChainHeight;
//            crossChainReceiveToken.TransferTransactionBytes =
//                ByteString.CopyFrom(ByteArrayHelper.HexStringToByteArray(rawTx));
//
//            //receive in main chain
//            Tester.CrossChainReceive(accountM, crossChainReceiveToken);
//
//            //verify
//            var balance = TesterA.GetBalance(accountM, "ELF");
//            _logger.Info($"balance: {balance}");
//
//            var tokenInfo = TesterA.GetTokenInfo("ELF");
//            _logger.Info($"Token: {tokenInfo}");
//        }
//
//        #endregion

        private void ValidateChainTokenAddress(ContractTester tester)
        {
            var rawTx = tester.ValidateTokenAddress();
            var txId = tester.ExecuteMethodWithTxId(rawTx);
            var result = CheckTransactionResult(tester.ContractServices, txId);
            if (!(result.InfoMsg is TransactionResultDto txResult)) return;
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
            if (!(createProposalResult.InfoMsg is TransactionResultDto txResult)) return;
            if (txResult.Status.ConvertTransactionResultStatus() != TransactionResultStatus.Mined) return;
            var proposalId = txResult.ReadableReturnValue.Replace("\"", "");

            //approve
            var miners = GetMiners(services);
            foreach (var miner in miners)
            {
                services.ParliamentService.SetAccount(miner.GetFormatted());
                var approveResult = services.ParliamentService.ExecuteMethodWithResult(ParliamentMethod.Approve,
                    new Acs3.ApproveInput
                    {
                        ProposalId = HashHelper.HexStringToHash(proposalId)
                    });
                if (!(approveResult.InfoMsg is TransactionResultDto approveTxResult)) return;
                if (approveTxResult.Status.ConvertTransactionResultStatus() != TransactionResultStatus.Mined) return;
            }

            services.ParliamentService.SetAccount(InitAccount);
            var releaseResult
                = services.ParliamentService.ExecuteMethodWithResult(ParliamentMethod.Release,
                    HashHelper.HexStringToHash(proposalId));
            if (!(releaseResult.InfoMsg is TransactionResultDto releaseTxResult)) return;
            if (releaseTxResult.Status.ConvertTransactionResultStatus() != TransactionResultStatus.Mined)
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
    }
}