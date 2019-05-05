//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Linq;
//using System.Threading;
//using AElf.Automation.Common.Contracts;
//using AElf.Automation.Common.Extensions;
//using AElf.Automation.Common.Helpers;
//using AElf.Contracts.CrossChain;
//using AElf.Contracts.MultiToken.Messages;
//using AElf.CrossChain;
//using AElf.Kernel;
//using Google.Protobuf;
//using Microsoft.VisualStudio.TestTools.UnitTesting;
//using Newtonsoft.Json.Linq;
//using Shouldly;
//using AElf.Common;
//using AElf.CSharp.Core;
//using AElf.CSharp.Core.Utils;
//
//
//namespace AElf.Automation.Contracts.ScenarioTest
//{
//    [TestClass]
//    public class CrossChainContractTest
//    {
//        private string NativeSymbol = "ELF";
//        
//        private readonly ILogHelper _logger = LogHelper.GetLogHelper();
//        public Address TokenContractAddress { get; set; }
//        public Address CrossChainContractAddress { get; set; }
//
//        public  JToken[] TxIds { get; set; }
//        public string _symbol = "ELF";
//
//        public string InitAccount { get; } = "2kVKi7EMChW4wLi3AZkT5moQHkDXSfSTJiS6K8pAwHm7nZjABK";
//        public static string SideAChainAccount { get; } = "";
//        public static string SideBChainAccount { get; } = "";
//
//        public static string MainChainAccount { get; } = "3ntF4gPT3r6FRuEL3SvJWzAGmoScYn4zBwYwp3gr7ueSXen";
//        
//        public RpcApiHelper CH { get; set; }
//        public RpcApiHelper sideACH { get; set; }
//        public RpcApiHelper sideBCH { get; set; }
//        public List<string> UserList { get; set; }
//
//        private GenesisContract GenesisService { get; set; }
//        private TokenContract TokenService { get; set; }
//        private CrossChainContract CrossChainService { get; set; }
//
//        //side chain contract
//        private TokenContract sideTokenService { get; set; }
//        private CrossChainContract sideCrossChainContractService { get; set; }
//        private GenesisContract sideBasicContractZeroService { get; set; }
//        private TokenContract sideBTokenService { get; set; }
//        private CrossChainContract sideBCrossChainContractService { get; set; }
//
////       public string RpcUrl { get; } ="http://192.168.197.27:8010/chain";
//        public string RpcUrl { get; } = "http://192.168.197.70:8001/chain";
////        public string RpcUrl { get; } = "http://192.168.197.70:8010/chain";
////        public string SideARpcUrl { get; }="http://192.168.197.27:8020/chain";
////        public string SideBRpcUrl { get; }="http://192.168.197.27:8030/chain";
//        public string SideARpcUrl { get; }="http://192.168.197.70:8011/chain";
//        public string SideBRpcUrl { get; }="http://192.168.197.70:8031/chain";
////        public string SideARpcUrl { get; }="http://192.168.197.70:8020/chain";
////        public string SideBRpcUrl { get; }="http://192.168.197.70:8030/chain";
//        
//        [TestInitialize]
//        public void Initialize()
//        {
//            #region Basic Preparation
//            //Init Logger
//            string logName = "ContractTest_" + DateTime.Now.ToString("MMddHHmmss") + ".log";
//            string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", logName);
//            _logger.InitLogHelper(dir);
//
//            //Main Chain
//            CH = new RpcApiHelper(RpcUrl, AccountManager.GetDefaultDataDir());
//            //Connect Chain
//            var ci = new CommandInfo("GetChainInformation");
//            CH.RpcGetChainInformation(ci);
//            Assert.IsTrue(ci.Result, "Connect chain got exception.");
//           
//            //Init contract service
//            GenesisService = GenesisContract.GetGenesisContract(CH, InitAccount);
//            //Get contract
//            TokenContractAddress = GenesisService.GetContractAddressByName(NameProvider.TokenName);
//            CrossChainContractAddress = GenesisService.GetContractAddressByName(NameProvider.CrossChainName);
//            
//            //Init other contract service
//            TokenService = new TokenContract(CH,InitAccount,TokenContractAddress.GetFormatted());
//            CrossChainService = new CrossChainContract(CH,InitAccount,CrossChainContractAddress.GetFormatted());
//            
//            #endregion
//        }
//
//        [TestMethod]
//        [DataRow("")]
//        [DataRow("")]
////        [DataRow("")]
////        [DataRow("")]
////        [DataRow("")]
//        public void TransferForCreateCrossChain(string account )
//        {
////            TokenService.SetAccount(MainChainAccount);
//            TokenService.SetAccount(InitAccount);
//            TokenService.ExecuteMethodWithResult(TokenMethod.Transfer,new TransferInput
//            {
//                Symbol = _symbol,
//                Amount = 300_000,
//                Memo = "Transfer to cross chain owner",
//                To = Address.Parse(account)
//            });
//
//            TokenService.SetAccount(account);
//            TokenService.ExecuteMethodWithResult(TokenMethod.Approve, new ApproveInput
//            {
//                Symbol = _symbol,
//                Amount = 300_000,
//                Spender = Address.Parse("2ErQEpj6v63LRSBwijZkHQVkRt1JH6P5Tuz6a6Jzg5DDiDF")
//            });
//            
//            var userResult = TokenService.ExecuteMethodWithResult(TokenMethod.GetBalance, new GetBalanceInput
//            {
//                Symbol = _symbol,
//                Owner = Address.Parse(account)
//            });
//                
//            var userResultReturn=  userResult.JsonInfo["result"]["ReadableReturnValue"].ToString().Replace("\"","");
//            _logger.WriteInfo($"user balance: {userResultReturn}");
//
//            var userResult1 = TokenService.ExecuteMethodWithResult(TokenMethod.GetAllowance, new GetAllowanceInput
//            {
//                Symbol = _symbol,
//                Spender =  Address.Parse("2ErQEpj6v63LRSBwijZkHQVkRt1JH6P5Tuz6a6Jzg5DDiDF"),
//                Owner = Address.Parse(account)   
//            });
//            
//            var userResultReturn1=  userResult1.JsonInfo["result"]["ReadableReturnValue"].ToString().Replace("\"","");
//            _logger.WriteInfo($"user balance: {userResultReturn1}");
//        }
//
//        [TestMethod]
//        [DataRow("")]
//        [DataRow("")]
////        [DataRow("")]
////        [DataRow("")]
//        public void RequestSideChain(string account)
//        {
//            ByteString code = ByteString.FromBase64("4d5a90000300");
//
//            var resourceBalance = new ResourceTypeBalancePair
//            {
//                Amount = 1,
//                Type = ResourceType.Ram
//            };
//
//            CrossChainService.SetAccount(account);
//            CrossChainService.ExecuteMethodWithResult(CrossChainContractMethod.RequestChainCreation,
//                new SideChainCreationRequest()
//                {
//                    ResourceBalances = {resourceBalance},
//                    ContractCode = code,
//                    IndexingPrice = 1,
//                    LockedTokenAmount = 200000
//                });
//        }
//
//        [TestMethod]
//        [DataRow(2750978,"")]
//        [DataRow(2816514,"")]
////        [DataRow(2882050,"")]
////        [DataRow(2750978,"")]
////        [DataRow(2816514,"")]
//        public void CreateSideChain(int value,string account)
//        {
//            CrossChainService.SetAccount(account);
//            CrossChainService.ExecuteMethodWithResult(CrossChainContractMethod.CreateSideChain, new SInt32Value
//            {
//                Value = value
//            });
//        }
//
//        [TestMethod]
////        [DataRow("")]
////        [DataRow("")]
////        [DataRow("")]
//        [DataRow("")]
//        public void Verify(string account)
//        {
//            TokenService.SetAccount("QL8AmZ41zYMrTSiJBbPmmEc933h6cHzAZBXcgARtXuLoAT");
//            var userResult = TokenService.ExecuteMethodWithResult(TokenMethod.GetBalance, new GetBalanceInput
//            {
//                Owner = Address.Parse(account),
//                Symbol = "ELF"
//            });
//            var userResultReturn=  userResult.JsonInfo["result"]["ReadableReturnValue"].ToString().Replace("\"","");
//            _logger.WriteInfo($"balance: {userResultReturn}");
//        }
//
//        [TestMethod]
//        public void ReCharge()
//        {
//            Verify("2ErQEpj6v63LRSBwijZkHQVkRt1JH6P5Tuz6a6Jzg5DDiDF");
//            
//            TokenService.SetAccount("3FZ5ppzjYZbJLCqbdDGpSTiFcFXK2y4Xic7hwQUpnLwsGHh");
//            TokenService.ExecuteMethodWithResult(TokenMethod.Approve, new ApproveInput
//            {
//                Symbol = _symbol,
//                Amount = 10000,
//                Spender = Address.Parse("2ErQEpj6v63LRSBwijZkHQVkRt1JH6P5Tuz6a6Jzg5DDiDF"),
//
//            });
//
//            crossChainContractService.SetAccount("3FZ5ppzjYZbJLCqbdDGpSTiFcFXK2y4Xic7hwQUpnLwsGHh");
//            crossChainContractService.ExecuteMethodWithResult(CrossChainContractMethod.Recharge, new RechargeInput
//            {
//                ChainId = 2750978,
//                Amount = 2000
//            });
//        }
//
//        [TestMethod]
//        [DataRow(2816514)]
//        public void GetSideChainStatus(int chainId)
//        {
//            var result = crossChainContractService.ExecuteMethodWithResult(CrossChainContractMethod.GetChainStatus, new SInt32Value
//            {
//                Value = chainId
//            });
//            var resultReturn = result.JsonInfo["result"]["ReadableReturnValue"].ToString().Replace("\"", "");
//            _logger.WriteInfo($"{resultReturn}");
//        }
//        
//        #region verify
//
//        [TestMethod]
//        public void TransferOnMainChain()
//        {
//            Random rd = new Random();
//            var amount = rd.Next(100,500);
//            var transferResult = TokenService.ExecuteMethodWithResult(TokenMethod.Transfer, new TransferInput
//            {
//                Symbol = _symbol,
//                To = Address.Parse("2jqpCqV6AXrYP5SLTbSzkEeANooUzM4pqhokPrbE4d9yzeU"),
//                Amount = amount,
//                Memo = "Transfer"
//            });
//            var txIdInString = transferResult.JsonInfo["result"]["TransactionId"].ToString();
//            var blockNumber = transferResult.JsonInfo["result"]["BlockNumber"].ToString();
//            
//            _logger.WriteInfo($"{txIdInString},{blockNumber}");
//        }
//
//        [TestMethod]
//        [DataRow("41d32ae6169f130dfa543b9491713f4732aca85ad08053da3cf20830a0238794","26")]
//        public void VerifyMainChainTransaction(string txIdInString,string blockNumber)
//        {
//            var ci = new CommandInfo("GetBlockInfo");
//            ci.Parameter = $"{blockNumber} {true}";
//            var blockInfoResult = CH.ExecuteCommand(ci);
//            blockInfoResult.GetJsonInfo();
//            var transactionIds = blockInfoResult.JsonInfo["result"]["Body"]["Transactions"].ToArray();
//            var transactionStatus = new List<string>();
//            
//            foreach (var transactionId in transactionIds)
//            {
//                var CI = new CommandInfo("GetTransactionResult");
//                CI.Parameter = $"{transactionId}";
//                var txResult = CH.ExecuteCommand(CI);
//                txResult.GetJsonInfo();
//                var resultStatus = txResult.JsonInfo["result"]["Status"].ToString();
//                transactionStatus.Add(resultStatus);
//            }
//
//            var txIdsWithStatus = new List<Hash>();
//            for(int num =0; num<transactionIds.Length;num++)
//            {
//                var txId = Hash.LoadHex(transactionIds[num].ToString());
//                string txRes = transactionStatus[num];
//                var rawBytes = txId.DumpByteArray().Concat(EncodingHelper.GetBytesFromUtf8String(txRes))
//                    .ToArray();
//                var txIdWithStatus = Hash.FromRawBytes(rawBytes);
//                txIdsWithStatus.Add(txIdWithStatus);
//            }
//            
//            var bmt = new BinaryMerkleTree();
//            bmt.AddNodes(txIdsWithStatus);
//            var root = bmt.ComputeRootHash();
//            int index = 1;
//            var merklePath = bmt.GenerateMerklePath(index);
//            
//            var verificationInput = new VerifyTransactionInput
//            {
//                ParentChainHeight = long.Parse(blockNumber),
//                TransactionId = Hash.LoadHex(txIdInString),
//                VerifiedChainId = 9992731
//            };
//            verificationInput.Path.AddRange(merklePath.Path);
//            
//            // change to side chain a to verify
//            changeToSideChainA();
//            
//            Thread.Sleep(4000);
//                        
//            var verifyResult =
//                sideCrossChainContractService.ExecuteMethodWithResult(CrossChainContractMethod.VerifyTransaction, verificationInput);
//            verifyResult.JsonInfo["result"]["ReadableReturnValue"].ToString().ShouldBe("true");
//        }
//
//        [TestMethod]
//        public void transferOnsideAChain()
//        {
//            changeToSideChainA();
//            
//            Random rd = new Random();
//            var amount = rd.Next(100,500);
//            sideTokenService.SetAccount("4P8WY7VXepP6fFK3S2k87gtZDetZjFMyLeEzMDCRsKJ62kc");
//            var transferResult = sideTokenService.ExecuteMethodWithResult(TokenMethod.Transfer, new TransferInput
//            {
//                Symbol = _symbol,
//                To = Address.Parse("4x2h4sXQyWpKFmBqbArjJ4FyKALm6dW6jbPBsMdVNoqdM75"),
//                Amount = amount,
//                Memo = "Transfer"
//            });
//            var txIdInString = transferResult.JsonInfo["result"]["TransactionId"].ToString();
//            var blockNumber = transferResult.JsonInfo["result"]["BlockNumber"].ToString();
//            
//            _logger.WriteInfo($"{txIdInString},{blockNumber}");
//        }
//
//        [TestMethod]
//        [DataRow("90279f487b911d48e8f39755f3fbb8d94dff58f4ae1d7996dc97e05a2182c039","201")]
//        public void VerifysideACHainTransaction(string txIdInString,string blockNumber )
//        {
//            changeToSideChainA();
//            
//            var ci = new CommandInfo("GetBlockInfo");
//            ci.Parameter = $"{blockNumber} {true}";
//            var blockInfoResult = sideACH.ExecuteCommand(ci);
//            blockInfoResult.GetJsonInfo();
//            var transactionIds = blockInfoResult.JsonInfo["result"]["Body"]["Transactions"].ToArray();
//            var transactionStatus = new List<string>();
//            
//            foreach (var transactionId in transactionIds)
//            {
//                var CI = new CommandInfo("GetTransactionResult");
//                CI.Parameter = $"{transactionId}";
//                var txResult = sideACH.ExecuteCommand(CI);
//                txResult.GetJsonInfo();
//                var resultStatus = txResult.JsonInfo["result"]["Status"].ToString();
//                transactionStatus.Add(resultStatus);
//            }
//
//            var txIdsWithStatus = new List<Hash>();
//            for(int num =0; num<transactionIds.Length;num++)
//            {
//                var txId = Hash.LoadHex(transactionIds[num].ToString());
//                string txRes = transactionStatus[num];
//                var rawBytes = txId.DumpByteArray().Concat(EncodingHelper.GetBytesFromUtf8String(txRes))
//                    .ToArray();
//                var txIdWithStatus = Hash.FromRawBytes(rawBytes);
//                txIdsWithStatus.Add(txIdWithStatus);
//            }
//            
//            var bmt = new BinaryMerkleTree();
//            bmt.AddNodes(txIdsWithStatus);
//            var root = bmt.ComputeRootHash();
//            int index = 1;
//            var merklePath = bmt.GenerateMerklePath(index);
//            var verificationInput = new VerifyTransactionInput
//            {
//                TransactionId = Hash.LoadHex(txIdInString),
//                VerifiedChainId = 2750978
//            };
//            verificationInput.Path.AddRange(merklePath.Path);
//            
//
//            // verify side chain transaction
//            var transferResult1 = sideCrossChainContractService.ExecuteContractMethodWithResult(CrossChainContractMethod.GetBoundParentChainHeightAndMerklePathByHeight.ToString(), new SInt64Value
//            {
//                Value = long.Parse(blockNumber)// side chain height
//            });
//            var outputInBase64 = transferResult1.JsonInfo["result"]["ReturnValue"].ToString();
//            var crossChainMerkleProofContext =
//                CrossChainMerkleProofContext.Parser.ParseFrom(ByteString.FromBase64(outputInBase64));
//            verificationInput.Path.AddRange(crossChainMerkleProofContext.MerklePathForParentChainRoot.Path);
//            verificationInput.ParentChainHeight = crossChainMerkleProofContext.BoundParentChainHeight;
//            
//            //verify in main chain            
//            var verifyResult1 =
//                crossChainContractService.ExecuteContractMethodWithResult(CrossChainContractMethod.VerifyTransaction.ToString(), verificationInput);
//            verifyResult1.JsonInfo["result"]["ReadableReturnValue"].ToString().ShouldBe("true");
//
//            //change to side chain B
//            changeToSideChainB();
//
//            sideBCrossChainContractService.SetAccount(SideBChainAccount);
//            var verifyResult2 =
//                sideBCrossChainContractService.ExecuteContractMethodWithResult(CrossChainContractMethod.VerifyTransaction.ToString(), verificationInput);
//            verifyResult2.JsonInfo["result"]["ReadableReturnValue"].ToString().ShouldBe("true");
//        }
//        
//        #endregion
//        
//        #region cross chain transfer
//
//        [TestMethod]
//        public void MainChainTransferSideChainA()
//        {
//            var accountA = "4P8WY7VXepP6fFK3S2k87gtZDetZjFMyLeEzMDCRsKJ62kc";
//            //get ELF token info
//            var getTokenInfoResult = TokenService.ExecuteMethodWithResult(TokenMethod.GetTokenInfo, new TokenInfo
//            {
//                Symbol = _symbol	
//            });
//            var tokenInfoReadableReturn = getTokenInfoResult.JsonInfo["result"]["ReadableReturnValue"].ToString();
//            var tokenInfoOutputInBase64 = getTokenInfoResult.JsonInfo["result"]["ReturnValue"].ToString();
//            var tokenInfo = TokenInfo.Parser.ParseFrom(ByteString.FromBase64(tokenInfoOutputInBase64));
//            
//            _logger.WriteInfo($"Token info: {tokenInfoReadableReturn}");
//            
//            //Transfer
//            TokenService.SetAccount(InitAccount);
//            var crossChainTransfer = TokenService.ExecuteMethodWithResult(TokenMethod.CrossChainTransfer,new CrossChainTransferInput
//            {
//                Amount = 100000,
//                Memo= "transfer to side chain a",
//                To = Address.Parse(accountA),
//                ToChainId = 2750978,
//                TokenInfo = tokenInfo
//            });
//            
//            var blockNumber = crossChainTransfer.JsonInfo["result"]["BlockNumber"].ToString();
//            _logger.WriteInfo($"Block Number: {blockNumber}");
//        }
//
//        [TestMethod]
//        [DataRow("4P8WY7VXepP6fFK3S2k87gtZDetZjFMyLeEzMDCRsKJ62kc","337","0a200a1e11cb05224528a90a08e24e726a7e07da293115fd40fd262b123d7b4ff2b612200a1eaaa58b6cf58d4ef337f6dc55b701fd57d622015a3548a91a4e40892aa35518d0022204a1963483321243726f7373436861696e5472616e736665723a89010a200a1e959560aced63faf661565711ade7b2ef3066b4d5afcf6d431bfc6cede83812420a03454c461209656c6620746f6b656e1880a8d6b9072080a8d6b907280432200a1edd8eea50c31966e06e4a2662bebef7ed81d09a47b2eb1eb3729f2f0cc781380118c09a0c22187472616e7366657220746f207369646520636861696e20612882f4a7014a4160bdd37df88d6ee2abd67553cda43d134d3ccca9fa7c1c270430fec4904e1d80446959369474b831a4f36b95884b7d8834bf294680f3ac73b8bf53b00048ebab00")]
//        public void SideChainAReceive(string accountA,string blockNumber,string rawTx)
//        {
//            var ci = new CommandInfo("GetBlockInfo");
//            ci.Parameter = $"{blockNumber} {true}";
//            var blockInfoResult = CH.ExecuteCommand(ci);
//            blockInfoResult.GetJsonInfo();
//            var transactionIds = blockInfoResult.JsonInfo["result"]["Body"]["Transactions"].ToArray();
//            var transactionStatus = new List<string>();
//            
//            foreach (var transactionId in transactionIds)
//            {
//                var CI = new CommandInfo("GetTransactionResult");
//                CI.Parameter = $"{transactionId}";
//                var txResult = CH.ExecuteCommand(CI);
//                txResult.GetJsonInfo();
//                var resultStatus = txResult.JsonInfo["result"]["Status"].ToString();
//                transactionStatus.Add(resultStatus);
//            }
//
//            var txIdsWithStatus = new List<Hash>();
//            for(int num =0; num<transactionIds.Length;num++)
//            {
//                var txId = Hash.LoadHex(transactionIds[num].ToString());
//                string txRes = transactionStatus[num];
//                var rawBytes = txId.DumpByteArray().Concat(EncodingHelper.GetBytesFromUtf8String(txRes))
//                    .ToArray();
//                var txIdWithStatus = Hash.FromRawBytes(rawBytes);
//                txIdsWithStatus.Add(txIdWithStatus);
//            }
//            
//            var bmt = new BinaryMerkleTree();
//            bmt.AddNodes(txIdsWithStatus);
//            var root = bmt.ComputeRootHash();
//            int index = 2;
//            var merklePath = bmt.GenerateMerklePath(index);
//            
//                        
//            var crossChainReceiveToken = new CrossChainReceiveTokenInput
//            {
//                FromChainId = 9992731,
//                ParentChainHeight = long.Parse(blockNumber)
//            };
//            crossChainReceiveToken.MerklePath.AddRange(merklePath.Path);
//            crossChainReceiveToken.TransferTransactionBytes = ByteString.CopyFrom(ByteArrayHelpers.FromHexString(rawTx));
//                
//            changeToSideChainA();
//            sideTokenService.SetAccount(accountA);
//            sideTokenService.ExecuteMethodWithResult(TokenMethod.CrossChainReceiveToken, crossChainReceiveToken);
//            
//            //verify
//            sideTokenService.SetAccount(accountA);
//            var userResult = sideTokenService.ExecuteMethodWithResult(TokenMethod.GetBalance, new GetBalanceInput
//            {
//                Owner = Address.Parse(accountA),
//                Symbol = _symbol	
//            });
//            var userResultReturn=  userResult.JsonInfo["result"]["ReadableReturnValue"].ToString().Replace("\"","");
//            _logger.WriteInfo($"balance: {userResultReturn}");
//        }
//        
//         [TestMethod]
//        [DataRow("4P8WY7VXepP6fFK3S2k87gtZDetZjFMyLeEzMDCRsKJ62kc","66JWaTFTxdChCmhgXRXUCdA7B1Ff5FW72FGs3JQRei8eoeN")]
//        public void SideChainATransferMainChain(string accountA, string accountM)
//        {
//            changeToSideChainA();
//            //get ELF token info
//            var getTokenInfoResult = sideTokenService.ExecuteMethodWithResult(TokenMethod.GetTokenInfo, new TokenInfo
//            {
//                Symbol = _symbol	
//            });
//            var tokenInfoOutputInBase64 = getTokenInfoResult.JsonInfo["result"]["ReturnValue"].ToString();
//            var tokenInfo = TokenInfo.Parser.ParseFrom(ByteString.FromBase64(tokenInfoOutputInBase64));
//
//            sideTokenService.SetAccount(accountA);
//            var crossChainTransferInput = new CrossChainTransferInput
//            {
//                Amount = 1000,
//                Memo= "transfer to side chain a",
//                To = Address.Parse(accountM),
//                ToChainId = 9992731,
//                TokenInfo = tokenInfo
//            };
//            //Transfer
//            var crossChainTransferResult =
//                sideTokenService.ExecuteMethodWithResult(TokenMethod.CrossChainTransfer,crossChainTransferInput);
//            
//            var blockNumber = crossChainTransferResult.JsonInfo["result"]["BlockNumber"].ToString();
//            _logger.WriteInfo($"Block Number: {blockNumber}");
//        }
//        
//        [TestMethod]
//        [DataRow("66JWaTFTxdChCmhgXRXUCdA7B1Ff5FW72FGs3JQRei8eoeN","251","0a200a1e959560aced63faf661565711ade7b2ef3066b4d5afcf6d431bfc6cede83812200a1e80ee395414cb759fc3a997f2b1a3db506aa9779bebbdef653aec7121fd6818fa0122043a0f0179321243726f7373436861696e5472616e736665723a88010a200a1ee13856880d015537da22c6cc63eba346de9348a3155fcc5c75575c7090b112420a03454c461209656c6620746f6b656e1880a8d6b9072080a8d6b907280432200a1edd8eea50c31966e06e4a2662bebef7ed81d09a47b2eb1eb3729f2f0cc781380118d00f22187472616e7366657220746f207369646520636861696e2061289bf4e1044a415284afb51842afccd1dbd4e1715b8a7fc9c54405be596163a6986a2c14294b3229e0bd354118d8167d6d7eef523a0a30f15862f13e5b5eea012465c725030d8b01")]
//        public void MainChainReceiveChainA(string accountM,string blockNumber,string rawTx)
//        {
//            changeToSideChainA();
//            
//            var ci = new CommandInfo("GetBlockInfo");
//            ci.Parameter = $"{blockNumber} {true}";
//            var blockInfoResult = sideACH.ExecuteCommand(ci);
//            blockInfoResult.GetJsonInfo();
//            var transactionIds = blockInfoResult.JsonInfo["result"]["Body"]["Transactions"].ToArray();
//            var transactionStatus = new List<string>();
//            
//            foreach (var transactionId in transactionIds)
//            {
//                var CI = new CommandInfo("GetTransactionResult");
//                CI.Parameter = $"{transactionId}";
//                var txResult = sideACH.ExecuteCommand(CI);
//                txResult.GetJsonInfo();
//                var resultStatus = txResult.JsonInfo["result"]["Status"].ToString();
//                transactionStatus.Add(resultStatus);
//            }
//
//            var txIdsWithStatus = new List<Hash>();
//            for(int num =0; num<transactionIds.Length;num++)
//            {
//                var txId = Hash.LoadHex(transactionIds[num].ToString());
//                string txRes = transactionStatus[num];
//                var rawBytes = txId.DumpByteArray().Concat(EncodingHelper.GetBytesFromUtf8String(txRes))
//                    .ToArray();
//                var txIdWithStatus = Hash.FromRawBytes(rawBytes);
//                txIdsWithStatus.Add(txIdWithStatus);
//            }
//            
//            var bmt = new BinaryMerkleTree();
//            bmt.AddNodes(txIdsWithStatus);
//            var root = bmt.ComputeRootHash();
//            int index = 2;
//            var merklePath = bmt.GenerateMerklePath(index);           
//                        
//            var crossChainReceiveToken = new CrossChainReceiveTokenInput
//            {
//                FromChainId = 2750978,
//            };
//            crossChainReceiveToken.MerklePath.AddRange(merklePath.Path);
//                              
//            // verify side chain transaction
//            var transferResult1 = sideCrossChainContractService.ExecuteContractMethodWithResult(CrossChainContractMethod.GetBoundParentChainHeightAndMerklePathByHeight.ToString(), new SInt64Value
//            {
//                Value = long.Parse(blockNumber)// side chain height
//            });
//            var outputInBase64 = transferResult1.JsonInfo["result"]["ReturnValue"].ToString();
//            var crossChainMerkleProofContext =
//                CrossChainMerkleProofContext.Parser.ParseFrom(ByteString.FromBase64(outputInBase64));
//            crossChainReceiveToken.MerklePath.AddRange(crossChainMerkleProofContext.MerklePathForParentChainRoot.Path);
//            crossChainReceiveToken.ParentChainHeight = crossChainMerkleProofContext.BoundParentChainHeight;
//            crossChainReceiveToken.TransferTransactionBytes = ByteString.CopyFrom(ByteArrayHelpers.FromHexString(rawTx));
//            
//            //receive in main chain
//
//            TokenService.SetAccount(accountM);
//            TokenService.ExecuteMethodWithResult(TokenMethod.CrossChainReceiveToken, crossChainReceiveToken);
//            
//            //verify
//            TokenService.SetAccount(accountM);
//            var userResult = TokenService.ExecuteMethodWithResult(TokenMethod.GetBalance, new GetBalanceInput
//            {
//                Owner = Address.Parse(accountM),
//                Symbol = _symbol	
//            });
//            var userResultReturn=  userResult.JsonInfo["result"]["ReadableReturnValue"].ToString().Replace("\"","");
//            _logger.WriteInfo($"balance: {userResultReturn}");
//        }
//
//        
//        
//
//        [TestMethod]
//        [DataRow("4P8WY7VXepP6fFK3S2k87gtZDetZjFMyLeEzMDCRsKJ62kc","PtaMy1NB13stxkbs1fvGHHnRD6kDXxX7kYR658RikHVYeM")]
//        public void SideChainATransferSideChainB(string accountA, string accountB)
//        {
//            changeToSideChainA();
//            //get ELF token info
//            var getTokenInfoResult = sideTokenService.ExecuteMethodWithResult(TokenMethod.GetTokenInfo, new TokenInfo
//            {
//                Symbol = _symbol	
//            });
//            var tokenInfoReadableReturn = getTokenInfoResult.JsonInfo["result"]["ReadableReturnValue"].ToString();
//            var tokenInfoOutputInBase64 = getTokenInfoResult.JsonInfo["result"]["ReturnValue"].ToString();
//            var tokenInfo = TokenInfo.Parser.ParseFrom(ByteString.FromBase64(tokenInfoOutputInBase64));
//            
//            _logger.WriteInfo($"Token info: {tokenInfoReadableReturn}");
//
//            sideTokenService.SetAccount(accountA);
//            var crossChainTransferInput = new CrossChainTransferInput
//            {
//                Amount = 5000,
//                Memo= "transfer to side chain a",
//                To = Address.Parse(accountB),
//                ToChainId = 2816514,
//                TokenInfo = tokenInfo
//            };
//            //Transfer
//            var crossChainTransferResult =
//                sideTokenService.ExecuteMethodWithResult(TokenMethod.CrossChainTransfer,crossChainTransferInput);
//            
//            var blockNumber = crossChainTransferResult.JsonInfo["result"]["BlockNumber"].ToString();
//            _logger.WriteInfo($"Block Number: {blockNumber}");
//        }
//        
//        [TestMethod]
//        [DataRow("PtaMy1NB13stxkbs1fvGHHnRD6kDXxX7kYR658RikHVYeM","49","0a200a1e959560aced63faf661565711ade7b2ef3066b4d5afcf6d431bfc6cede83812200a1e80ee395414cb759fc3a997f2b1a3db506aa9779bebbdef653aec7121fd6818302204d525f815321243726f7373436861696e5472616e736665723a88010a200a1e1175043895bc6d37501000ec385b1acd32e895049c29e52d15a94de2f21f12420a03454c461209656c6620746f6b656e1880a8d6b9072080a8d6b907280432200a1edd8eea50c31966e06e4a2662bebef7ed81d09a47b2eb1eb3729f2f0cc781380118904e22187472616e7366657220746f207369646520636861696e20612882f4ab014a410997f0fe29583aeb6ae36e3eb12ce0f4517c676b543985e86423705709689af001a62325aaf0d95a3dd9a905949003b7aab29626dda7ee5ddf4000926edb47e400")]
//        public void SideChainBReceive(string accountB,string blockNumber,string rawTx)
//        {
//            changeToSideChainA();
//            
//            var ci = new CommandInfo("GetBlockInfo");
//            ci.Parameter = $"{blockNumber} {true}";
//            var blockInfoResult = sideACH.ExecuteCommand(ci);
//            blockInfoResult.GetJsonInfo();
//            var transactionIds = blockInfoResult.JsonInfo["result"]["Body"]["Transactions"].ToArray();
//            var transactionStatus = new List<string>();
//            
//            foreach (var transactionId in transactionIds)
//            {
//                var CI = new CommandInfo("GetTransactionResult");
//                CI.Parameter = $"{transactionId}";
//                var txResult = sideACH.ExecuteCommand(CI);
//                txResult.GetJsonInfo();
//                var resultStatus = txResult.JsonInfo["result"]["Status"].ToString();
//                transactionStatus.Add(resultStatus);
//            }
//
//            var txIdsWithStatus = new List<Hash>();
//            for(int num =0; num<transactionIds.Length;num++)
//            {
//                var txId = Hash.LoadHex(transactionIds[num].ToString());
//                string txRes = transactionStatus[num];
//                var rawBytes = txId.DumpByteArray().Concat(EncodingHelper.GetBytesFromUtf8String(txRes))
//                    .ToArray();
//                var txIdWithStatus = Hash.FromRawBytes(rawBytes);
//                txIdsWithStatus.Add(txIdWithStatus);
//            }
//            
//            var bmt = new BinaryMerkleTree();
//            bmt.AddNodes(txIdsWithStatus);
//            var root = bmt.ComputeRootHash();
//            int index = 2;
//            var merklePath = bmt.GenerateMerklePath(index);           
//                        
//            var crossChainReceiveToken = new CrossChainReceiveTokenInput
//            {
//                FromChainId = 2750978,
//            };
//            crossChainReceiveToken.MerklePath.AddRange(merklePath.Path);
//                              
//            // verify side chain transaction
//            var transferResult1 = sideCrossChainContractService.ExecuteContractMethodWithResult(CrossChainContractMethod.GetBoundParentChainHeightAndMerklePathByHeight.ToString(), new SInt64Value
//            {
//                Value = long.Parse(blockNumber)// side chain height
//            });
//            var outputInBase64 = transferResult1.JsonInfo["result"]["ReturnValue"].ToString();
//            var crossChainMerkleProofContext =
//                CrossChainMerkleProofContext.Parser.ParseFrom(ByteString.FromBase64(outputInBase64));
//            crossChainReceiveToken.MerklePath.AddRange(crossChainMerkleProofContext.MerklePathForParentChainRoot.Path);
//            crossChainReceiveToken.ParentChainHeight = crossChainMerkleProofContext.BoundParentChainHeight;
//            crossChainReceiveToken.TransferTransactionBytes = ByteString.CopyFrom(ByteArrayHelpers.FromHexString(rawTx));
//            
//            //receive in side chain B
//
//            changeToSideChainB();
//            sideBTokenService.SetAccount(accountB);
//            sideBTokenService.ExecuteMethodWithResult(TokenMethod.CrossChainReceiveToken, crossChainReceiveToken);
//            
//            //verify
//            sideBTokenService.SetAccount(accountB);
//            var userResult = sideBTokenService.ExecuteMethodWithResult(TokenMethod.GetBalance, new GetBalanceInput
//            {
//                Owner = Address.Parse(accountB),
//                Symbol = _symbol	
//            });
//            var userResultReturn=  userResult.JsonInfo["result"]["ReadableReturnValue"].ToString().Replace("\"","");
//            _logger.WriteInfo($"balance: {userResultReturn}");
//        }
//
//        [TestMethod]
//        [DataRow("PtaMy1NB13stxkbs1fvGHHnRD6kDXxX7kYR658RikHVYeM","66JWaTFTxdChCmhgXRXUCdA7B1Ff5FW72FGs3JQRei8eoeN")]
//        public void SideChainBTransferMainChain(string accountB, string accountM)
//        {
//            changeToSideChainB();
//            //get ELF token info
//            var getTokenInfoResult = sideBTokenService.ExecuteMethodWithResult(TokenMethod.GetTokenInfo, new TokenInfo
//            {
//                Symbol = _symbol	
//            });
//            var tokenInfoOutputInBase64 = getTokenInfoResult.JsonInfo["result"]["ReturnValue"].ToString();
//            var tokenInfo = TokenInfo.Parser.ParseFrom(ByteString.FromBase64(tokenInfoOutputInBase64));
//
//            sideBTokenService.SetAccount(accountB);
//            var crossChainTransferInput = new CrossChainTransferInput
//            {
//                Amount = 1000,
//                Memo= "transfer to side chain a",
//                To = Address.Parse(accountM),
//                ToChainId = 9992731,
//                TokenInfo = tokenInfo
//            };
//            //Transfer
//            var crossChainTransferResult =
//                sideBTokenService.ExecuteMethodWithResult(TokenMethod.CrossChainTransfer,crossChainTransferInput);
//            
//            var blockNumber = crossChainTransferResult.JsonInfo["result"]["BlockNumber"].ToString();
//            _logger.WriteInfo($"Block Number: {blockNumber}");
//        }
//        
//        [TestMethod]
//        [DataRow("66JWaTFTxdChCmhgXRXUCdA7B1Ff5FW72FGs3JQRei8eoeN","169","0a200a1e1175043895bc6d37501000ec385b1acd32e895049c29e52d15a94de2f21f12200a1e778e3006a12cc609d78bad825f6bc18ff1e354ec7fdaaa02de71c0983abb18a80122047bc6a503321243726f7373436861696e5472616e736665723a88010a200a1ee13856880d015537da22c6cc63eba346de9348a3155fcc5c75575c7090b112420a03454c461209656c6620746f6b656e1880a8d6b9072080a8d6b907280432200a1edd8eea50c31966e06e4a2662bebef7ed81d09a47b2eb1eb3729f2f0cc781380118d00f22187472616e7366657220746f207369646520636861696e2061289bf4e1044a41dade00c1e310303ecab0e23fa62ff2f7ce7dc06901d090d90f6d51e0b9fe075d381eaabf06750e437ce4cf0ef891231eac4fe22733fa5091777e3d4cd977929300")]
//        public void MainChainReceive(string accountB,string blockNumber,string rawTx)
//        {
//            changeToSideChainB();
//            
//            var ci = new CommandInfo("GetBlockInfo");
//            ci.Parameter = $"{blockNumber} {true}";
//            var blockInfoResult = sideBCH.ExecuteCommand(ci);
//            blockInfoResult.GetJsonInfo();
//            var transactionIds = blockInfoResult.JsonInfo["result"]["Body"]["Transactions"].ToArray();
//            var transactionStatus = new List<string>();
//            
//            foreach (var transactionId in transactionIds)
//            {
//                var CI = new CommandInfo("GetTransactionResult");
//                CI.Parameter = $"{transactionId}";
//                var txResult = sideBCH.ExecuteCommand(CI);
//                txResult.GetJsonInfo();
//                var resultStatus = txResult.JsonInfo["result"]["Status"].ToString();
//                transactionStatus.Add(resultStatus);
//            }
//
//            var txIdsWithStatus = new List<Hash>();
//            for(int num =0; num<transactionIds.Length;num++)
//            {
//                var txId = Hash.LoadHex(transactionIds[num].ToString());
//                string txRes = transactionStatus[num];
//                var rawBytes = txId.DumpByteArray().Concat(EncodingHelper.GetBytesFromUtf8String(txRes))
//                    .ToArray();
//                var txIdWithStatus = Hash.FromRawBytes(rawBytes);
//                txIdsWithStatus.Add(txIdWithStatus);
//            }
//            
//            var bmt = new BinaryMerkleTree();
//            bmt.AddNodes(txIdsWithStatus);
//            var root = bmt.ComputeRootHash();
//            int index = 2;
//            var merklePath = bmt.GenerateMerklePath(index);           
//                        
//            var crossChainReceiveToken = new CrossChainReceiveTokenInput
//            {
//                FromChainId = 2816514,
//            };
//            crossChainReceiveToken.MerklePath.AddRange(merklePath.Path);
//                              
//            // verify side chain transaction
//            var transferResult1 = sideBCrossChainContractService.ExecuteContractMethodWithResult(CrossChainContractMethod.GetBoundParentChainHeightAndMerklePathByHeight.ToString(), new SInt64Value
//            {
//                Value = long.Parse(blockNumber)// side chain height
//            });
//            var outputInBase64 = transferResult1.JsonInfo["result"]["ReturnValue"].ToString();
//            var crossChainMerkleProofContext =
//                CrossChainMerkleProofContext.Parser.ParseFrom(ByteString.FromBase64(outputInBase64));
//            crossChainReceiveToken.MerklePath.AddRange(crossChainMerkleProofContext.MerklePathForParentChainRoot.Path);
//            crossChainReceiveToken.ParentChainHeight = crossChainMerkleProofContext.BoundParentChainHeight;
//            crossChainReceiveToken.TransferTransactionBytes = ByteString.CopyFrom(ByteArrayHelpers.FromHexString(rawTx));
//            
//            //receive in main chain
//
//            TokenService.SetAccount(accountB);
//            TokenService.ExecuteMethodWithResult(TokenMethod.CrossChainReceiveToken, crossChainReceiveToken);
//            
//            //verify
//            TokenService.SetAccount(accountB);
//            var userResult = TokenService.ExecuteMethodWithResult(TokenMethod.GetBalance, new GetBalanceInput
//            {
//                Owner = Address.Parse(accountB),
//                Symbol = _symbol	
//            });
//            var userResultReturn=  userResult.JsonInfo["result"]["ReadableReturnValue"].ToString().Replace("\"","");
//            _logger.WriteInfo($"balance: {userResultReturn}");
//        }
//
//
//
//        public void changeToSideChainA()
//        {
//            sideACH = new CliHelper(SideARpcUrl, AccountManager.GetDefaultDataDir());
//            var sideci = new CommandInfo("GetChainInformation");
//            sideACH.RpcGetChainInformation(sideci);
//            Assert.IsTrue(sideci.Result, "Connect chain got exception.");
//
//            sideTokenAbi = "3v3tzwWfS384bSvyDZU1PyVjgXJzPj5Xgj1N55jRrVGmzA9";
//            sideCrossChainAbi = "6NRUWAMzAv2hFNwEfSKqnihQ1XSw2KKJAyZfXG1cr3b9T6n";
//            
//            sideTokenService = new TokenContract(sideACH, SideAChainAccount, sideTokenAbi);
//            sideCrossChainContractService = new CrossChainContract(sideACH,SideAChainAccount,sideCrossChainAbi);
//        }
//
//        public void changeToSideChainB()
//        {
//            sideBCH = new CliHelper(SideBRpcUrl, AccountManager.GetDefaultDataDir());
//            var sideBci = new CommandInfo("GetChainInformation");
//            sideBCH.RpcGetChainInformation(sideBci);
//            Assert.IsTrue(sideBci.Result, "Connect chain got exception.");
//
//            sideBTokenAbi = "3hkwLTqfS1qvesDLLfFiBiuWaVMhjrvdT7zrQ1u75u9az33";
//            sideBCrossChainAbi = "4EkqvQBy99QHySsUVbvx5uJaMyPvMjizLxWF66zWTtDzJL1";
//            
//            sideBTokenService = new TokenContract(sideBCH, SideBChainAccount, sideBTokenAbi);
//            sideBCrossChainContractService = new CrossChainContract(sideBCH,SideBChainAccount,sideBCrossChainAbi);
//        }
//
//        [TestMethod]
//        //[DataRow("ELF")]
//        [DataRow("RAM")]
//        public void create(string symbol)
//        {
//            changeToSideChainA();
//            TokenService.ExecuteMethodWithResult(TokenMethod.Create, new CreateInput
//            {
//                Symbol = symbol,
//                Decimals = 2,
//                IsBurnable = true,
//                Issuer = Address.Parse(InitAccount),
//                TokenName = "RAM Token",
//                TotalSupply = 1000_0000
//            });
//
//            TokenService.SetAccount(InitAccount);
//            TokenService.ExecuteMethodWithResult(TokenMethod.Issue, new IssueInput
//            {
//                Symbol = symbol,
//                Amount = 1000_0000,
//                Memo = "Issue",
//                To = Address.Parse(InitAccount)
//            });
//        }
//        
//        #endregion
//
//        [TestMethod]
//        [DataRow(2882050)]
//        [DataRow(2947586)]
//        public void WithdrawRequest(int chainId)
//        {
//            crossChainContractService.SetAccount("3FZ5ppzjYZbJLCqbdDGpSTiFcFXK2y4Xic7hwQUpnLwsGHh");
//            crossChainContractService.ExecuteMethodWithResult(CrossChainContractMethod.WithdrawRequest, new SInt32Value
//            {
//                Value = chainId
//            });
//
//            GetSideChainStatus(chainId);
//        }
//
//        [TestMethod]
//        [DataRow(2750978)]
//        //[DataRow(2882050)]
//        //[DataRow(2947586)]
//        public void RequestChainDisposal(int chainId)
//        {
//            crossChainContractService.SetAccount("3FZ5ppzjYZbJLCqbdDGpSTiFcFXK2y4Xic7hwQUpnLwsGHh");
////            crossChainContractService.ExecuteMethodWithResult(CrossChainContractMethod.RequestChainDisposal, new SInt32Value
////            {
////                Value = chainId
////            });
////            
////            GetSideChainStatus(chainId);
//
//            crossChainContractService.ExecuteMethodWithResult(CrossChainContractMethod.DisposeSideChain, new SInt32Value
//            {
//                Value = chainId
//            });
//            
//            GetSideChainStatus(chainId);
//        }
//
//
//    }
//}