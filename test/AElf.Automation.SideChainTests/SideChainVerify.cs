using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AElf.Automation.Common.Contracts;
using AElf.Automation.Common.Extensions;
using AElf.Automation.Common.Helpers;
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
        public static string SideARpcUrl { get; } = "http://192.168.197.70:8011/chain";
        public static string SideBRpcUrl { get; } = "http://192.168.197.70:8031/chain";
        public RpcApiHelper sideChainA { get; set; }
        public ContractTester TesterA;
        public string sideChainAccount = "2RCLmZQ2291xDwSbDEJR6nLhFJcMkyfrVTq1i1YxWC4SdY49a6";
        
        [TestInitialize]
        public void InitializeNodeTests()
        {
            base.Initialize();
        }
        
        [TestMethod]
        [DataRow("2QhTob7XyrbvByB9X1ymYKdTYM57rhHJ2w3rC3a3imWycAYBL9",1000)]
        public void TransferOnMainChain(string toAddress,long amount)
        {
            var transferResult = Tester.TransferToken(InitAccount, toAddress, amount, "ELF");
            var txIdInString = transferResult.JsonInfo["result"]["TransactionId"].ToString();
            var blockNumber = transferResult.JsonInfo["result"]["BlockNumber"].ToString();

            _logger.WriteInfo($"{txIdInString},{blockNumber}");
        }

        [TestMethod]
        [DataRow("f88c50440a441048feaee5c49a34cef5197ee15062f8d9389e0df783519e3c27","1163",2)]
        public void VerifyMainChainTransaction(string txIdInString,string blockNumber,int index)
        {
            var merklePath = GetMerklePath(blockNumber,index,CH);
            
            var verificationInput = new VerifyTransactionInput
            {
                ParentChainHeight = long.Parse(blockNumber),
                TransactionId = Hash.LoadHex(txIdInString),
                VerifiedChainId = 9992731
            };
            verificationInput.Path.AddRange(merklePath.Path);
            
            // change to side chain a to verify
            sideChainA = ChangeRpc(SideARpcUrl);
            TesterA = ChangeToSideChain(sideChainA,sideChainAccount);
            
            Thread.Sleep(4000);

            var verifyResult = TesterA.VerifyTransaction(verificationInput,sideChainAccount);
                
            verifyResult.JsonInfo["result"]["ReadableReturnValue"].ToString().ShouldBe("true");
        }
        
        [TestMethod]
        public void TransferOnsideChain(string toAddress,long amount)
        {
            // change to side chain a to verify
            sideChainA = ChangeRpc(SideARpcUrl);
            TesterA = ChangeToSideChain(sideChainA,sideChainAccount);
            
            var transferResult = Tester.TransferToken(InitAccount, toAddress, amount, "ELF");
            var txIdInString = transferResult.JsonInfo["result"]["TransactionId"].ToString();
            var blockNumber = transferResult.JsonInfo["result"]["BlockNumber"].ToString();

            _logger.WriteInfo($"{txIdInString},{blockNumber}");
        }
        
        [TestMethod]
        [DataRow("90279f487b911d48e8f39755f3fbb8d94dff58f4ae1d7996dc97e05a2182c039","201")]
        public void VerifysideACHainTransaction(string txIdInString,string blockNumber,int index)
        {
            sideChainA = ChangeRpc(SideARpcUrl);
            TesterA = ChangeToSideChain(sideChainA,sideChainAccount);
            
            var merklePath = GetMerklePath(blockNumber,index,sideChainA);
            var verificationInput = new VerifyTransactionInput
            {
                TransactionId = Hash.LoadHex(txIdInString),
                VerifiedChainId = 2750978
            };
            verificationInput.Path.AddRange(merklePath.Path);   

            // verify side chain transaction
            var transferResult1 =
                TesterA.GetBoundParentChainHeightAndMerklePathByHeight(sideChainAccount, long.Parse(blockNumber));
            var outputInBase64 = transferResult1.JsonInfo["result"]["ReturnValue"].ToString();
            var crossChainMerkleProofContext =
                CrossChainMerkleProofContext.Parser.ParseFrom(ByteString.FromBase64(outputInBase64));
            verificationInput.Path.AddRange(crossChainMerkleProofContext.MerklePathForParentChainRoot.Path);
            verificationInput.ParentChainHeight = crossChainMerkleProofContext.BoundParentChainHeight;
            
            //verify in main chain            
            var verifyResult1 =
                Tester.VerifyTransaction(verificationInput, InitAccount);
            verifyResult1.JsonInfo["result"]["ReadableReturnValue"].ToString().ShouldBe("true");

            //change to side chain B
            //changeToSideChainB();

//            var verifyResult2 =
//                sideBCrossChainContractService.ExecuteContractMethodWithResult(CrossChainContractMethod.VerifyTransaction.ToString(), verificationInput);
//            verifyResult2.JsonInfo["result"]["ReadableReturnValue"].ToString().ShouldBe("true");
        }
    }
}