using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using AElf.Automation.Common.Contracts;
using AElf.Automation.Common.Helpers;
using AElf.Automation.Common.WebApi;
using AElf.Automation.Common.WebApi.Dto;
using AElf.Automation.SideChain.VerificationTest;
using AElf.Contracts.CrossChain;
using AElf.Contracts.MultiToken.Messages;
using AElf.CSharp.Core.Utils;
using AElf.Kernel;
using Google.Protobuf;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AElf.Automation.SideChain.Verification.Test
{
    public class AccountInfo
    {
        public string Account { get; }
        public int Increment { get; set; }

        public AccountInfo(string account)
        {
            Account = account;
            Increment = 0;
        }
    }

    public class Contract
    {
        public string ContractPath { get; }
        public string Symbol { get; set; }
        public int AccountId { get; }

        public Contract(int accId, string contractPath)
        {
            AccountId = accId;
            ContractPath = contractPath;
        }
    }

    public class TxInfo
    {
        public string TxId { get; set; }
        public long BlockNumber { get; set; }
        public string rawTx { get; set; }

        public TxInfo(long blockNumber,string txid,string rawTx)
        {
            TxId = txid;
            BlockNumber = blockNumber;
            this.rawTx = rawTx;
        }
        
        public TxInfo(long blockNumber,string txid)
        {
            TxId = txid;
            BlockNumber = blockNumber;
        }
    }
    
    public class OperationSet
    {
        #region Public Property

        public IApiHelper ApiHelper;
        public string BaseUrl { get; set; }
        public List<string> SideUrls { get; set; }
        public string InitAccount { get; set; }
        public List<AccountInfo> AccountList { get; set; }
        public string KeyStorePath { get; set; }
        public long BlockHeight { get; set; }
        public List<Contract> ContractList { get; set; }
        public List<string> TxIdList { get; set; }
        public List<TxInfo> TxInfos { get; set; }
        public List<TxInfo> RawTxInfos { get; set; }
        public int ThreadCount { get; }
        public int ExeTimes { get; }
        private readonly ILogHelper _logger = LogHelper.GetLogHelper();

        #endregion
        
        public Operation MainChain;
        public List<Operation> SideChains;

        public OperationSet(int threadCount,
            int exeTimes, string initAccount,
            string baseUrl = "http://127.0.0.1:8000",
            string keyStorePath = "")
            {
                if (keyStorePath == "")
                    keyStorePath = GetDefaultDataDir();

                SideUrls = new List<string>();
                AccountList = new List<AccountInfo>();
                ContractList = new List<Contract>();
                TxIdList = new List<string>();
                TxInfos = new List<TxInfo>();
                RawTxInfos = new List<TxInfo>();
                BlockHeight = 1;
                ExeTimes = exeTimes;
                ThreadCount = threadCount;
                KeyStorePath = keyStorePath;
                BaseUrl = baseUrl;
                InitAccount = initAccount;
            }

        public Operation InitMain(string initAccount)
        {
            var mainService = new ContractServices(BaseUrl, initAccount, "Main");
            MainChain = new Operation(mainService);
            return MainChain;
        }

        public List<Operation> InitSideNodes(string initAccount)
        {
            for (int i = 0; i < SideUrls.Count; i++)
            {
                var sideService = new ContractServices(SideUrls[i],initAccount,"Side");
                var side =  new Operation(sideService);
                SideChains.Add(side);
            }

            return SideChains;
        }
        
        public void InitMainExecCommand()
        {
            _logger.WriteInfo("Rpc Url: {0}", BaseUrl);
            _logger.WriteInfo("Key Store Path: {0}", Path.Combine(KeyStorePath, "keys"));
            _logger.WriteInfo("Prepare new and unlock accounts.");
            ApiHelper = new WebApiHelper(BaseUrl, KeyStorePath);

            //Connect Chain
            var ci = new CommandInfo(ApiMethods.GetChainInformation);
            ApiHelper.ExecuteCommand(ci);
            Assert.IsTrue(ci.Result, "Connect chain got exception.");

            //New
            NewAccounts(5);

            //Unlock Account
            UnlockAllAccounts(5);
        }
        
        public void CrossChainTransfer()
        {
            _logger.WriteInfo("Transfer on the Main chain");
            for (int i = 0; i < AccountList.Count; i++)
            {
                var txInfo = Transfer(MainChain, InitAccount, AccountList[i].Account, 10000);
                TxInfos.Add(txInfo);
            }
            
            Thread.Sleep(4000); 
            
            _logger.WriteInfo("Show the ");
            CheckNodeStatus();
            
        }

        public void ExecuteCrossChainVerifyTask(OperationSet operationSet,int sideChainNum )
        {

            
        }
        

        public TxInfo Transfer(Operation chain,string initAccount,string toAddress,long amount)
        {
            var result = chain.TransferToken(initAccount, toAddress, amount, "ELF");
            var transferResult = result.InfoMsg as TransactionResultDto;
            
            var txIdInString = transferResult.TransactionId;
            var blockNumber = transferResult.BlockNumber;
            var txInfo = new TxInfo(blockNumber,txIdInString);

            return txInfo;
        }
        

        public string VerifyMainChainTransaction(TxInfo txinfo,string url,int sideChainNumber,string sideChainAccount)
        {
            var merklePath = GetMerklePath(txinfo.BlockNumber,url,txinfo.TxId );
            
            var verificationInput = new VerifyTransactionInput
            {
                ParentChainHeight = txinfo.BlockNumber,
                TransactionId = Hash.LoadHex(txinfo.TxId),
                VerifiedChainId = 9992731
            };
            verificationInput.Path.AddRange(merklePath.Path);
            
            // change to side chain a to verify            
            Thread.Sleep(4000);

            var result = SideChains[sideChainNumber].VerifyTransaction(verificationInput,sideChainAccount);
            var verifyResult = result.InfoMsg as TransactionResultDto;
            var returnResult = verifyResult.ReadableReturnValue;
            return returnResult;
        }

        public string VerifySideChainTransaction(Operation chain,TxInfo txinfo,string url,int sideChainNumber,int chainId,string sideChainAccount,string InitAccount)
        {           
            var merklePath = GetMerklePath(txinfo.BlockNumber,url,txinfo.TxId );
            var verificationInput = new VerifyTransactionInput
            {
                TransactionId = Hash.LoadHex(txinfo.TxId),
                VerifiedChainId = chainId
            };
            verificationInput.Path.AddRange(merklePath.Path);   

            // verify side chain transaction
            var crossChainMerkleProofContext =
                SideChains[sideChainNumber].GetBoundParentChainHeightAndMerklePathByHeight(sideChainAccount, txinfo.BlockNumber);
            verificationInput.Path.AddRange(crossChainMerkleProofContext.MerklePathForParentChainRoot.Path);
            verificationInput.ParentChainHeight = crossChainMerkleProofContext.BoundParentChainHeight;
            
            //verify in other chain            
            var result =
                chain.VerifyTransaction(verificationInput, InitAccount);
            var verifyResult = result.InfoMsg as TransactionResultDto;
            var returnResult = verifyResult.ReadableReturnValue;

            return returnResult;
        }
        
        public TxInfo CrossChainTransfer(Operation chain,string fromAccount,string toAccount,int toChainId,long amount)
        {
            //get token info
            var tokenInfo = chain.GetTokenInfo("ELF");
            //Transfer
            var crossChainTransferInput = new CrossChainTransferInput()
            {
                Amount = amount,
                Memo= "cross chain transfer",
                To = Address.Parse(toAccount),
                ToChainId = toChainId,
                TokenInfo = tokenInfo
            };
            var result = chain.CrossChainTransfer(fromAccount,crossChainTransferInput);
            var rawTx = chain.ApiHelper.GenerateTransactionRawTx(MainChain.TokenService.CallAddress,
                chain.TokenService.ContractAddress, TokenMethod.CrossChainTransfer.ToString(), crossChainTransferInput);
            var resultReturn = result.InfoMsg as TransactionResultDto;
            var blockNumber = resultReturn.BlockNumber;
            var txId = resultReturn.TransactionId;
            var rawTxInfo = new TxInfo(blockNumber,txId,rawTx);
            return rawTxInfo;
        }

        public GetBalanceOutput ReceiveFromMainChain(TxInfo rawTxInfo,string url,int sideChainNumber,string receiveAccount,long amount)
        {
            var merklePath = GetMerklePath(rawTxInfo.BlockNumber,url,rawTxInfo.TxId);
                      
            var crossChainReceiveToken = new CrossChainReceiveTokenInput
            {
                FromChainId = 9992731,
                ParentChainHeight = rawTxInfo.BlockNumber
            };
            crossChainReceiveToken.MerklePath.AddRange(merklePath.Path);
            crossChainReceiveToken.TransferTransactionBytes = ByteString.CopyFrom(ByteArrayHelpers.FromHexString(rawTxInfo.rawTx));
            SideChains[sideChainNumber].CrossChainReceive(receiveAccount, crossChainReceiveToken);
            
           //Get Balance
            var balance = SideChains[sideChainNumber].GetBalance(receiveAccount, "ELF");
            return balance;
        }

        public GetBalanceOutput ReceiveFromSideChain(TxInfo rawTxInfo,string url,Operation chain,int fromChainId,string fromAccount,string receiveAccount,long amount)
        {
            var merklePath = GetMerklePath(rawTxInfo.BlockNumber,url,rawTxInfo.TxId);
                                      
            var crossChainReceiveToken = new CrossChainReceiveTokenInput
            {
                FromChainId = fromChainId,
            };
            crossChainReceiveToken.MerklePath.AddRange(merklePath.Path);
                              
            // verify side chain transaction
            var crossChainMerkleProofContext =
                chain.GetBoundParentChainHeightAndMerklePathByHeight(fromAccount, rawTxInfo.BlockNumber);
            crossChainReceiveToken.MerklePath.AddRange(crossChainMerkleProofContext.MerklePathForParentChainRoot.Path);
            crossChainReceiveToken.ParentChainHeight = crossChainMerkleProofContext.BoundParentChainHeight;
            crossChainReceiveToken.TransferTransactionBytes = ByteString.CopyFrom(ByteArrayHelpers.FromHexString(rawTxInfo.rawTx));
            
            chain.CrossChainReceive(receiveAccount, crossChainReceiveToken);
            //Get Balance
            var balance = chain.GetBalance(receiveAccount, "ELF");
            return balance;
        }
        
        public void DeleteAccounts()
        {
            foreach (var item in AccountList)
            {
                var file = Path.Combine(KeyStorePath, $"{item.Account}.ak");
                File.Delete(file);
            }
        }
                

        private MerklePath GetMerklePath(long blockNumber, string url,string TxId)
        {
            int index = 0;
            var apiService = new WebApiService(url);
            var blockInfoResult = apiService.GetBlockByHeight(blockNumber,true).Result;
            var transactionIds = blockInfoResult.Body.Transactions;
            var transactionStatus = new List<string>();
            
            foreach (var transactionId in transactionIds)
            {
                var txResult = apiService.GetTransactionResult(transactionId).Result;
                var resultStatus = txResult.Status;
                transactionStatus.Add(resultStatus);
            }

            var txIdsWithStatus = new List<Hash>();
            for(int num =0; num<transactionIds.Count;num++)
            {
                var txId = Hash.LoadHex(transactionIds[num]);
                string txRes = transactionStatus[num];
                var rawBytes = txId.DumpByteArray().Concat(EncodingHelper.GetBytesFromUtf8String(txRes))
                    .ToArray();
                var txIdWithStatus = Hash.FromRawBytes(rawBytes);
                txIdsWithStatus.Add(txIdWithStatus);
                if (transactionIds[num] == TxId)
                {
                    index = num;
                }
            }

            var bmt = new BinaryMerkleTree();
            bmt.AddNodes(txIdsWithStatus);
            var root = bmt.ComputeRootHash();
            var merklePath = bmt.GenerateMerklePath(index);

            return merklePath;
        }
        
        private static string GetDefaultDataDir()
        {
            try
            {
                var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "aelf");
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);

                var keyPath = Path.Combine(path, "keys");
                if (!Directory.Exists(keyPath))
                    Directory.CreateDirectory(keyPath);

                return path;
            }
            catch (Exception)
            {
                return null;
            }
        }
        
        private void UnlockAllAccounts(int count)
        {
            for (var i = 0; i < count; i++)
            {
                var ci = new CommandInfo(ApiMethods.AccountUnlock)
                {
                    Parameter = $"{AccountList[i].Account} 123 notimeout"
                };
                ci = ApiHelper.ExecuteCommand(ci);
                Assert.IsTrue(ci.Result);
            }
        }

        private void NewAccounts(int count)
        {
            for (var i = 0; i < count; i++)
            {
                var ci = new CommandInfo(ApiMethods.AccountNew) {Parameter = "123"};
                ci = ApiHelper.ExecuteCommand(ci);
                Assert.IsTrue(ci.Result);
                AccountList.Add(new AccountInfo(ci.InfoMsg.ToString().Replace("Account address:", "").Trim()));
            }
        }
        
        
        private void CheckNodeStatus()
        {
            for (var i = 0; i < 10; i++)
            {
                var ci = new CommandInfo(ApiMethods.GetBlockHeight);
                ApiHelper.GetBlockHeight(ci);
                var currentHeight = (long)ci.InfoMsg;

                _logger.WriteInfo("Current block height: {0}", currentHeight);
                if (BlockHeight != currentHeight)
                {
                    BlockHeight = currentHeight;
                    return;
                }

                Thread.Sleep(4000);
                _logger.WriteWarn("Block height not changed round: {0}", i + 1);
            }
            Assert.IsTrue(false, "Node block exception, block height not increased anymore.");
        }


    }
}