using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AElf.Automation.Common.Contracts;
using AElf.Automation.Common.Extensions;
using AElf.Automation.Common.Helpers;
using AElf.Automation.Common.WebApi;
using AElf.Automation.Common.WebApi.Dto;
using AElf.Contracts.CrossChain;
using AElf.Contracts.MultiToken.Messages;
using AElf.CrossChain;
using AElf.CSharp.Core.Utils;
using AElf.Kernel;
using Google.Protobuf;
using Shouldly;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AElf.Automation.SideChainTests
{
    [TestClass]
    public class SideChainVerify : SideChainTestBase
    {
        public static string SideARpcUrl { get; } = "http://192.168.197.70:8011";
        public static string SideBRpcUrl { get; } = "http://192.168.197.56:8011";
        public IApiHelper SideChainA { get; set; }
        public IApiHelper SideChainB { get; set; }
        public IApiService ISA { get; set; }
        public IApiService ISB { get; set; }
        public ContractTester TesterA;
        public ContractTester TesterB;
        public string sideChainAccount = "2RCLmZQ2291xDwSbDEJR6nLhFJcMkyfrVTq1i1YxWC4SdY49a6";
        public string sideChainBccount = "2RCLmZQ2291xDwSbDEJR6nLhFJcMkyfrVTq1i1YxWC4SdY49a6";
        
        [TestInitialize]
        public void InitializeNodeTests()
        {
            base.Initialize();
        }

        #region cross chain verify 
        
        [TestMethod]
        [DataRow("2QhTob7XyrbvByB9X1ymYKdTYM57rhHJ2w3rC3a3imWycAYBL9",1000)]
        public void TransferOnMainChain(string toAddress,long amount)
        {
            var result = Tester.TransferToken(InitAccount, toAddress, amount, "ELF");
            var transferResult = result.InfoMsg as TransactionResultDto;
            var txIdInString = transferResult.TransactionId;
            var blockNumber = transferResult.BlockNumber;

            _logger.WriteInfo($"{txIdInString},{blockNumber}");
        }

        [TestMethod]
        [DataRow("f88c50440a441048feaee5c49a34cef5197ee15062f8d9389e0df783519e3c27","1163",2)]
        public void VerifyMainChainTransaction(string txIdInString,string blockNumber,int index)
        {
            var merklePath = GetMerklePath(blockNumber,index,IS);
            
            var verificationInput = new VerifyTransactionInput
            {
                ParentChainHeight = long.Parse(blockNumber),
                TransactionId = Hash.LoadHex(txIdInString),
                VerifiedChainId = 9992731
            };
            verificationInput.Path.AddRange(merklePath.Path);
            
            // change to side chain a to verify
            SideChainA = ChangeRpc(SideARpcUrl);
            TesterA = ChangeToSideChain(SideChainA,sideChainAccount);
            
            Thread.Sleep(4000);

            var result = TesterA.VerifyTransaction(verificationInput,sideChainAccount);
            var verifyResult = result.InfoMsg as TransactionResultDto;
            verifyResult.ReadableReturnValue.ShouldBe("true");
        }
        
        [TestMethod]
        public void TransferOnsideChain(string toAddress,long amount)
        {
            // change to side chain a to verify
            SideChainA = ChangeRpc(SideARpcUrl);
            TesterA = ChangeToSideChain(SideChainA,sideChainAccount);
            
            var result = Tester.TransferToken(InitAccount, toAddress, amount, "ELF");
            var transferResult = result.InfoMsg as TransactionResultDto;
            var txIdInString = transferResult.TransactionId;
            var blockNumber = transferResult.BlockNumber;

            _logger.WriteInfo($"{txIdInString},{blockNumber}");
        }
        
        [TestMethod]
        [DataRow("6bbc4ee5f62f4a8e5a5ccbfa1f6f33caff09f9ad41248db3925618b8962e25c2","3605",2)]
        public void VerifysideACHainTransaction(string txIdInString,string blockNumber,int index)
        {
            SideChainA = ChangeRpc(SideARpcUrl);
            TesterA = ChangeToSideChain(SideChainA,sideChainAccount);
            ISA = new WebApiService(SideARpcUrl);
            
            var merklePath = GetMerklePath(blockNumber,index,ISA);
            var verificationInput = new VerifyTransactionInput
            {
                TransactionId = Hash.LoadHex(txIdInString),
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

            //change to side chain B
            SideChainB = ChangeRpc(SideBRpcUrl);
            TesterB = ChangeToSideChain(SideChainB,sideChainBccount);

            var result2 =
                Tester.VerifyTransaction(verificationInput, sideChainBccount);
            var verifyResult2 = result2.InfoMsg as TransactionResultDto;
            verifyResult2.ReadableReturnValue.ShouldBe("true");
        }
        #endregion
                
        #region cross chain transfer

        [TestMethod]
        [DataRow("2A1RKFfxeh2n7nZpcci6t8CcgbJMGz9a7WGpC94THpiTK3U7nG",2750978)]
        public void MainChainTransferSideChainA(string accountA,int toChainId)
        {
            //get token info
            var tokenInfo = Tester.GetTokenInfo("ELF");
            //Transfer
            var result = Tester.CrossChainTransfer(InitAccount, accountA, tokenInfo, toChainId, 1000);
            var resultReturn = result.InfoMsg as TransactionResultDto;
            var blockNumber = resultReturn.BlockNumber;
            _logger.WriteInfo($"Block Number: {blockNumber}");
        }
                
        [TestMethod]
        [DataRow("2A1RKFfxeh2n7nZpcci6t8CcgbJMGz9a7WGpC94THpiTK3U7nG","3150",2,"0a220a20baa28ffec57c135c7015a88d4ade469ec128d5e68e9d2ddf86121f9821dc982a12220a20aaa58b6cf58d4ef337f6dc55b701fd57d622015a3548a91a4e40892aa355d70e18cd182204042a9e332a1243726f7373436861696e5472616e73666572328a010a220a209825ea6ae8e17764b3d6561088d21134d8683419ef386257ae7e9404f47fd34212440a03454c461209656c6620746f6b656e1880a8d6b9072080a8d6b907280432220a20dd8eea50c31966e06e4a2662bebef7ed81d09a47b2eb1eb3729f2f0cc78129ae380118d00f22167472616e7366657220746f207369646520636861696e2882f4a70182f104418c4193447b0c20b1c9948b82b45c0e6ed169af3dd324cde12f5dfbe80d76aac2488e5edf804df98ca71e356c716c97a17644f3040cb0a27abd54376e718bad9200")]
        public void SideChainAReceive(string accountA,string blockNumber,int index,string rawTx)
        {
            var merklePath = GetMerklePath(blockNumber,index,IS);
                      
            var crossChainReceiveToken = new CrossChainReceiveTokenInput
            {
                FromChainId = 9992731,
                ParentChainHeight = long.Parse(blockNumber)
            };
            crossChainReceiveToken.MerklePath.AddRange(merklePath.Path);
            crossChainReceiveToken.TransferTransactionBytes = ByteString.CopyFrom(ByteArrayHelpers.FromHexString(rawTx));
                
            SideChainA = ChangeRpc(SideARpcUrl);
            TesterA = ChangeToSideChain(SideChainA,sideChainAccount);
            TesterA.CrossChainReceive(accountA, crossChainReceiveToken);
            
            //verify
            var balance = TesterA.GetBalance(accountA, "ELF");
            _logger.WriteInfo($"balance: {balance}");
            
            var tokenInfo = TesterA.GetTokenInfo("ELF");
            _logger.WriteInfo($"Token: {tokenInfo}");
        }
        
        [TestMethod]
        [DataRow("","")]
        public void SideChainATransferSideChainB(string accountA, string accountB, int toChainId)
        {
            SideChainA = ChangeRpc(SideARpcUrl);
            TesterA = ChangeToSideChain(SideChainA,sideChainAccount);
            ISA = new WebApiService(SideARpcUrl);
            //get tokenInfo
            var tokenInfo = TesterA.GetTokenInfo("ELF");
            //Transfer
            var result = Tester.CrossChainTransfer(accountA, accountB, tokenInfo, toChainId, 1000);
            var resultReturn = result.InfoMsg as TransactionResultDto;
            var blockNumber = resultReturn.BlockNumber;
            _logger.WriteInfo($"Block Number: {blockNumber}");
        }
        
        [TestMethod]
        [DataRow("","","")]
        public void SideChainBReceive(string accountB,string blockNumber,int index, string rawTx)
        {
            SideChainA = ChangeRpc(SideARpcUrl);
            TesterA = ChangeToSideChain(SideChainA,sideChainAccount);
            ISA = new WebApiService(SideARpcUrl);

            var merklePath = GetMerklePath(blockNumber, index, ISA);
       
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
            crossChainReceiveToken.TransferTransactionBytes = ByteString.CopyFrom(ByteArrayHelpers.FromHexString(rawTx));
            
            //receive in side chain B
            SideChainB = ChangeRpc(SideBRpcUrl);
            TesterB = ChangeToSideChain(SideChainB,sideChainBccount);
            ISB = new WebApiService(SideBRpcUrl);
            TesterB.CrossChainReceive(accountB,crossChainReceiveToken);
            
            //verify
            var balance = TesterA.GetBalance(accountB, "ELF");
            _logger.WriteInfo($"balance: {balance}");
            
            var tokenInfo = TesterA.GetTokenInfo("ELF");
            _logger.WriteInfo($"Token: {tokenInfo}");
        }
        
        [TestMethod]
        [DataRow("","")]
        public void SideChainTransferMainChain(string accountB, string accountM, int toChainId)
        {
            SideChainB = ChangeRpc(SideBRpcUrl);
            TesterB = ChangeToSideChain(SideChainB,sideChainBccount);
            ISB = new WebApiService(SideBRpcUrl);
            
            //get ELF token info
            var tokenInfo = TesterB.GetTokenInfo("ELF");            
            //Transfer
            var result = TesterB.CrossChainTransfer(accountB, accountM, tokenInfo, toChainId, 1000);
            var resultReturn = result.InfoMsg as TransactionResultDto;
            var blockNumber = resultReturn.BlockNumber;
            _logger.WriteInfo($"Block Number: {blockNumber}");
        }
        
        [TestMethod]
        [DataRow("","","")]
        public void MainChainReceive(string accountM,string blockNumber,int index,string rawTx)
        {
            SideChainB = ChangeRpc(SideBRpcUrl);
            TesterB = ChangeToSideChain(SideChainB,sideChainBccount);
            ISB = new WebApiService(SideBRpcUrl);

            var merklePath = GetMerklePath(blockNumber,index,ISB);
                                      
            var crossChainReceiveToken = new CrossChainReceiveTokenInput
            {
                FromChainId = 2816514,
            };
            crossChainReceiveToken.MerklePath.AddRange(merklePath.Path);
                              
            // verify side chain transaction
            var crossChainMerkleProofContext =
                TesterB.GetBoundParentChainHeightAndMerklePathByHeight(sideChainAccount, long.Parse(blockNumber));
            crossChainReceiveToken.MerklePath.AddRange(crossChainMerkleProofContext.MerklePathForParentChainRoot.Path);
            crossChainReceiveToken.ParentChainHeight = crossChainMerkleProofContext.BoundParentChainHeight;
            crossChainReceiveToken.TransferTransactionBytes = ByteString.CopyFrom(ByteArrayHelpers.FromHexString(rawTx));
            
            //receive in main chain
            Tester.CrossChainReceive(accountM, crossChainReceiveToken);
            
            //verify
            var balance = TesterA.GetBalance(accountM, "ELF");
            _logger.WriteInfo($"balance: {balance}");
            
            var tokenInfo = TesterA.GetTokenInfo("ELF");
            _logger.WriteInfo($"Token: {tokenInfo}");
        }
        #endregion
    }
}