using System.Threading;
using AElf.Automation.Common.Helpers;
using AElf.Automation.Common.WebApi;
using AElf.Automation.Common.WebApi.Dto;
using AElf.Contracts.CrossChain;
using AElf.Contracts.MultiToken.Messages;
using AElf.Types;
using Google.Protobuf;
using Shouldly;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AElf.Automation.SideChainTests
{
    [TestClass]
    public class SideChainVerify : SideChainTestBase
    {
        public static string SideARpcUrl { get; } = "http://192.168.197.16:8011";
        public static string SideBRpcUrl { get; } = "http://192.168.197.26:8021";
        public IApiHelper SideChainA { get; set; }
        public IApiHelper SideChainB { get; set; }
        public ContractTester TesterA;
        public ContractTester TesterB;
        public string sideChainAccount = "28Y8JA1i2cN6oHvdv7EraXJr9a1gY6D1PpJXw9QtRMRwKcBQMK";
        public string sideChainBccount = "28Y8JA1i2cN6oHvdv7EraXJr9a1gY6D1PpJXw9QtRMRwKcBQMK";

        [TestInitialize]
        public void InitializeNodeTests()
        {
            Initialize();
        }

        #region cross chain verify 

        [TestMethod]
        [DataRow("2QhTob7XyrbvByB9X1ymYKdTYM57rhHJ2w3rC3a3imWycAYBL9", 1000)]
        public void TransferOnMainChain(string toAddress, long amount)
        {
            var result = Tester.TransferToken(InitAccount, toAddress, amount, "ELF");
            var transferResult = result.InfoMsg as TransactionResultDto;
            var txIdInString = transferResult.TransactionId;
            var blockNumber = transferResult.BlockNumber;

            _logger.Info($"{txIdInString},{blockNumber}");
        }

        [TestMethod]
        [DataRow("bf1fab93f707f7d85dcbeeb11bfa40142bccef5118a579531e5e3339cf8d8488","138675")]
        public void VerifyMainChainTransaction(string txIdInString,string blockNumber,string txid)
        {
            var merklePath = GetMerklePath(blockNumber, txid, Tester);

            var verificationInput = new VerifyTransactionInput
            {
                ParentChainHeight = long.Parse(blockNumber),
                TransactionId = HashHelper.HexStringToHash(txIdInString),
                VerifiedChainId = 9992731
            };
            verificationInput.Path.AddRange(merklePath.Path);

            // change to side chain a to verify
            SideChainA = ChangeRpc(SideARpcUrl);
            TesterA = ChangeToSideChain(SideChainA, sideChainAccount);

            Thread.Sleep(4000);

            var result = TesterA.VerifyTransaction(verificationInput, sideChainAccount);
            var verifyResult = result.InfoMsg as TransactionResultDto;
            verifyResult.ReadableReturnValue.ShouldBe("true");
        }

        [TestMethod]
        [DataRow("2QhTob7XyrbvByB9X1ymYKdTYM57rhHJ2w3rC3a3imWycAYBL9",1000)]
        public void TransferOnsideChain(string toAddress,long amount)
        {
            // change to side chain a to verify
            SideChainA = ChangeRpc(SideARpcUrl);
            TesterA = ChangeToSideChain(SideChainA,sideChainAccount);
            
            var result = TesterA.TransferToken(InitAccount, toAddress, amount, "ELF");
            var transferResult = result.InfoMsg as TransactionResultDto;
            var txIdInString = transferResult.TransactionId;
            var blockNumber = transferResult.BlockNumber;

            _logger.Info($"{txIdInString},{blockNumber}");
        }

        [TestMethod]
        [DataRow("8e8c7176bf37ef1ce8f8414fdff54a68d67e9b9da9124c659f46910593c292d1","12793",1)]
        public void VerifysideACHainTransaction(string txIdInString,string blockNumber,string txid)
        {
            SideChainA = ChangeRpc(SideARpcUrl);
            TesterA = ChangeToSideChain(SideChainA, sideChainAccount);

            var merklePath = GetMerklePath(blockNumber, txid, TesterA);
            var verificationInput = new VerifyTransactionInput
            {
                TransactionId = HashHelper.HexStringToHash(txIdInString),
                VerifiedChainId = 2750978
            };
            verificationInput.Path.AddRange(merklePath.Path);

            // verify side chain transaction
            var crossChainMerkleProofContext =
                TesterA.GetBoundParentChainHeightAndMerklePathByHeight(sideChainAccount, long.Parse(blockNumber));
            verificationInput.Path.AddRange(crossChainMerkleProofContext.MerklePathForParentChainRoot.Path);
            verificationInput.ParentChainHeight = crossChainMerkleProofContext.BoundParentChainHeight;

            //verify in main chain            
            var result =
                Tester.VerifyTransaction(verificationInput, InitAccount);
            var verifyResult = result.InfoMsg as TransactionResultDto;
            verifyResult.ReadableReturnValue.ShouldBe("true");

//            //change to side chain B
//            SideChainB = ChangeRpc(SideBRpcUrl);
//            TesterB = ChangeToSideChain(SideChainB,sideChainBccount);
//
//            var result2 =
//                TesterB.VerifyTransaction(verificationInput, sideChainBccount);
//            var verifyResult2 = result2.InfoMsg as TransactionResultDto;
//            verifyResult2.ReadableReturnValue.ShouldBe("true");
        }

        #endregion

        #region cross chain transfer

        [TestMethod]
        [DataRow("2A1RKFfxeh2n7nZpcci6t8CcgbJMGz9a7WGpC94THpiTK3U7nG", 2750978)]
        public void MainChainTransferSideChainA(string accountA, int toChainId)
        {
            //get token info
            var tokenInfo = Tester.GetTokenInfo("ELF");
            //Transfer
            var result = Tester.CrossChainTransfer(InitAccount, accountA, tokenInfo, toChainId, 1000);
            var resultReturn = result.InfoMsg as TransactionResultDto;
            var blockNumber = resultReturn.BlockNumber;
            _logger.Info($"Block Number: {blockNumber}");
        }

        [TestMethod]
        [DataRow("2A1RKFfxeh2n7nZpcci6t8CcgbJMGz9a7WGpC94THpiTK3U7nG","138809","bc58078bdd74e9d2e76250a416a6e67858eb2522f6635018d86537e705fe3aeb","0a220a200e0859791a72bc512b4b91edfd12116daddf7c9f8915d608e2313f4772e761df12220a2043a0f4a61fd597aee85d15e13bfa96e70b82a7071ca25e62c3176a80b8231ae218a9bc082204e7db11582a1243726f7373436861696e5472616e73666572328a010a220a209825ea6ae8e17764b3d6561088d21134d8683419ef386257ae7e9404f47fd34212440a03454c461209656c6620746f6b656e1880a8d6b9072080a8d6b907280432220a20dd8eea50c31966e06e4a2662bebef7ed81d09a47b2eb1eb3729f2f0cc78129ae380118d00f22167472616e7366657220746f207369646520636861696e2882f4a70182f1044106db22096561cfc70b0faaa57c5bd521d59d359db341e05754c9a65ff1ec0192050a11375c02076b3dc4e03f5ca8b8fa5c78ed9636af47c79b9c4556a922f03801")]
        public void SideChainAReceive(string accountA,string blockNumber, string txid,string rawTx)
        {
            var merklePath = GetMerklePath(blockNumber, txid, Tester);

            var crossChainReceiveToken = new CrossChainReceiveTokenInput
            {
                FromChainId = 9992731,
                ParentChainHeight = long.Parse(blockNumber)
            };
            crossChainReceiveToken.MerklePath.AddRange(merklePath.Path);
            crossChainReceiveToken.TransferTransactionBytes =
                ByteString.CopyFrom(ByteArrayHelper.HexStringToByteArray(rawTx));

            SideChainA = ChangeRpc(SideARpcUrl);
            TesterA = ChangeToSideChain(SideChainA,sideChainAccount);
            TesterA.CrossChainReceive(InitAccount, crossChainReceiveToken);
            
            //verify
            var balance = TesterA.GetBalance(accountA, "ELF");
            _logger.Info($"balance: {balance}");

            var tokenInfo = TesterA.GetTokenInfo("ELF");
            _logger.Info($"Token: {tokenInfo}");
        }

        [TestMethod]
        [DataRow("2A1RKFfxeh2n7nZpcci6t8CcgbJMGz9a7WGpC94THpiTK3U7nG", "2A1RKFfxeh2n7nZpcci6t8CcgbJMGz9a7WGpC94THpiTK3U7nG",2113)]
        public void SideChainATransferSideChainB(string accountA, string accountB, int toChainId)
        {
            SideChainA = ChangeRpc(SideARpcUrl);
            TesterA = ChangeToSideChain(SideChainA, sideChainAccount);
            //get tokenInfo
            var tokenInfo = TesterA.GetTokenInfo("ELF");
            //Transfer
            var result = Tester.CrossChainTransfer(accountA, accountB, tokenInfo, toChainId, 1000);
            var resultReturn = result.InfoMsg as TransactionResultDto;
            var blockNumber = resultReturn.BlockNumber;
            _logger.Info($"Block Number: {blockNumber}");
        }

        [TestMethod]
        [DataRow("", "", "")]
        public void SideChainBReceive(string accountB, string blockNumber, string txid, string rawTx)
        {
            SideChainA = ChangeRpc(SideARpcUrl);
            TesterA = ChangeToSideChain(SideChainA, sideChainAccount);

            var merklePath = GetMerklePath(blockNumber, txid, TesterA);

            var crossChainReceiveToken = new CrossChainReceiveTokenInput
            {
                FromChainId = 2750978,
            };
            crossChainReceiveToken.MerklePath.AddRange(merklePath.Path);

            // verify side chain transaction
            var crossChainMerkleProofContext =
                TesterA.GetBoundParentChainHeightAndMerklePathByHeight(sideChainAccount, long.Parse(blockNumber));
            crossChainReceiveToken.MerklePath.AddRange(crossChainMerkleProofContext.MerklePathForParentChainRoot.Path);
            crossChainReceiveToken.ParentChainHeight = crossChainMerkleProofContext.BoundParentChainHeight;
            crossChainReceiveToken.TransferTransactionBytes =
                ByteString.CopyFrom(ByteArrayHelper.HexStringToByteArray(rawTx));

            //receive in side chain B
            SideChainB = ChangeRpc(SideBRpcUrl);
            TesterB = ChangeToSideChain(SideChainB, sideChainBccount);
            TesterB.CrossChainReceive(accountB, crossChainReceiveToken);

            //verify
            var balance = TesterA.GetBalance(accountB, "ELF");
            _logger.Info($"balance: {balance}");

            var tokenInfo = TesterA.GetTokenInfo("ELF");
            _logger.Info($"Token: {tokenInfo}");
        }

        [TestMethod]
        [DataRow("", "")]
        public void SideChainTransferMainChain(string accountB, string accountM, int toChainId)
        {
            SideChainA = ChangeRpc(SideARpcUrl);
            TesterA = ChangeToSideChain(SideChainA,sideChainAccount);
            //get ELF token info
            var tokenInfo = TesterB.GetTokenInfo("ELF");
            //Transfer
            var result = TesterB.CrossChainTransfer(accountB, accountM, tokenInfo, toChainId, 1000);
            var resultReturn = result.InfoMsg as TransactionResultDto;
            var blockNumber = resultReturn.BlockNumber;
            _logger.Info($"Block Number: {blockNumber}");
        }

        [TestMethod]
        [DataRow("2iimYTf2mn134pAsRqRT2a1kEadNVGkBZdNgE8na9y4RnwiPRU","2460",1,"0a220a20e26d40e0021a6dd128cda7d682d8cd9dba10feda55d8c689d1563fb0b313c8e812220a2080ee395414cb759fc3a997f2b1a3db506aa9779bebbdef653aec7121fd681da61893132204dfcc9d022a1243726f7373436861696e5472616e736665723288010a220a200640bf6b4c86d22ebf32cd8f96135c421990326febdc94256d3b0f5f1ac89ff712440a03454c461209656c6620746f6b656e18fca7d6b9072080a8d6b907280432220a20dd8eea50c31966e06e4a2662bebef7ed81d09a47b2eb1eb3729f2f0cc78129ae380118c801221463726f737320636861696e207472616e73666572289bf4e10482f104412bd970d3de35175f33e74e0563a82b1e87792096ce5c72859f29d23a425f905512bc028dbd27ecc68995f2934405d66ee55a6bc06d47cfdc0a639aa620e4f93a01")]
        public void MainChainReceive(string accountM,string blockNumber,string txid,string rawTx)
        {
            SideChainB = ChangeRpc(SideBRpcUrl);
            TesterB = ChangeToSideChain(SideChainB, sideChainBccount);

            var merklePath = GetMerklePath(blockNumber, txid, TesterB);

            var crossChainReceiveToken = new CrossChainReceiveTokenInput
            {
                FromChainId = 2750978,
            };
            crossChainReceiveToken.MerklePath.AddRange(merklePath.Path);

            // verify side chain transaction
            var crossChainMerkleProofContext =
                TesterB.GetBoundParentChainHeightAndMerklePathByHeight(sideChainAccount, long.Parse(blockNumber));
            crossChainReceiveToken.MerklePath.AddRange(crossChainMerkleProofContext.MerklePathForParentChainRoot.Path);
            crossChainReceiveToken.ParentChainHeight = crossChainMerkleProofContext.BoundParentChainHeight;
            crossChainReceiveToken.TransferTransactionBytes =
                ByteString.CopyFrom(ByteArrayHelper.HexStringToByteArray(rawTx));

            //receive in main chain
            Tester.CrossChainReceive(accountM, crossChainReceiveToken);

            //verify
            var balance = TesterA.GetBalance(accountM, "ELF");
            _logger.Info($"balance: {balance}");

            var tokenInfo = TesterA.GetTokenInfo("ELF");
            _logger.Info($"Token: {tokenInfo}");
        }

        #endregion
    }
}