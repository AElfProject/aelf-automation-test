using System;
using System.Collections.Concurrent;
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
        public string RawTx { get; set; }
        public string FromAccount { get; set; }
        public string ReceiveAccount { get; set; }

        public TxInfo(long blockNumber,string txid,string rawTx,string fromAccount,string receiveAccount)
        {
            TxId = txid;
            BlockNumber = blockNumber;
            RawTx = rawTx;
            FromAccount = fromAccount;
            ReceiveAccount = receiveAccount;
        }
        
        public TxInfo(long blockNumber,string txid)
        {
            TxId = txid;
            BlockNumber = blockNumber;
        }
    }

    public class VerifyResult
    {
        public string Result { get; set; }
        public TxInfo TxInfo { get; set; }
        public string ChaindId { get; set; }

        public VerifyResult(string result,TxInfo txInfo,string chaindId)
        {
            Result = result;
            TxInfo = txInfo;
            ChaindId = chaindId;
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
        public List<List<string>> AccountLists { get; set; }
        public string KeyStorePath { get; set; }
        public long BlockHeight { get; set; }
        public List<string> TxIdList { get; set; }
        public List<TxInfo> TxInfos { get; set; }
        public List<TxInfo> RawTxInfos { get; set; }
        public List<VerifyResult> VerifyResultsList { get; set; }
        public int ThreadCount { get; }
        public int ExeTimes { get; }
        private readonly ILogHelper _logger = LogHelper.GetLogHelper();

        #endregion
        
        public Operation MainChain;
        public List<Operation> SideChains;

        public OperationSet(int threadCount,
            int exeTimes, string initAccount, List<string> sideUrls, string baseUrl = "http://127.0.0.1:8000",
            string keyStorePath = "")
            {
                if (keyStorePath == "")
                    keyStorePath = GetDefaultDataDir();

                
                SideUrls = sideUrls;
                AccountList = new List<AccountInfo>();
                AccountLists = new List<List<string>>();
                TxIdList = new List<string>();
                TxInfos = new List<TxInfo>();
                RawTxInfos = new List<TxInfo>();
                VerifyResultsList = new List<VerifyResult>();
                
                BlockHeight = 1;
                ExeTimes = exeTimes;
                ThreadCount = threadCount;
                KeyStorePath = keyStorePath;
                BaseUrl = baseUrl;
                InitAccount = initAccount;
                SideChains = new List<Operation>();
            }

        public void InitMainExecCommand()
        {
            _logger.WriteInfo("Rpc Url: {0}", BaseUrl);
            _logger.WriteInfo("Key Store Path: {0}", Path.Combine(KeyStorePath, "keys"));
            _logger.WriteInfo("Prepare new and unlock accounts.");

            MainChain = InitMain(InitAccount);
            SideChains = InitSideNodes(InitAccount);
            //New
            NewAccounts(MainChain, 5);

            //Unlock Account
            UnlockAllAccounts(MainChain, 5);
        }

        public void MainChainTransactionVerifyOnSideChains()
        {
            TxInfos = new List<TxInfo>();
            long blockHeight = 1;
            for (var r = 1; r > 0; r++) //continuous running
            {
                var ci = new CommandInfo(ApiMethods.GetBlockHeight);
                MainChain.ApiHelper.GetBlockHeight(ci);
                var currentHeight = (long) ci.InfoMsg;
                if (blockHeight+10 > currentHeight)
                {
                    blockHeight = currentHeight-10;
                }
  
                for (var i = blockHeight ; i < blockHeight+10; i++)
                {
                    var CI = new CommandInfo(ApiMethods.GetBlockByHeight){Parameter = $"{i} {true}"};
                    var result = MainChain.ApiHelper.ExecuteCommand(CI);
                    var blockResult = result.InfoMsg as BlockDto;
                    var txIds = blockResult.Body.Transactions;

                    for (int j = 0; j < txIds.Count; j++)
                    {
                        var txInfo = new TxInfo(i,txIds[j]);
                        TxInfos.Add(txInfo);
                        _logger.WriteInfo($"the transactions block is:{txInfo.BlockNumber},transaction id is: {txInfo.TxId}");
                    }
                }            

                _logger.WriteInfo("Waiting for the index");
                Thread.Sleep(20000);
                _logger.WriteInfo("Verify on the side chain");

                for (int j = 0; j < TxInfos.Count; j++)
                {
                    for (int k = 0; k < SideChains.Count; k++)
                    {
                        _logger.WriteInfo($"Verify block:{TxInfos[j].BlockNumber},transaction:{TxInfos[j].TxId}");
                        var result = VerifyMainChainTransaction(SideChains[k],TxInfos[j],InitAccount);

                        if (result.Equals("false"))
                        {
                            Thread.Sleep(4000);
                            var checkTime = 5;
                            _logger.WriteInfo($"the verify result is {result}, revalidate the results");
                            while (checkTime>0)
                            {
                                checkTime--;
                                result = VerifyMainChainTransaction(SideChains[k],TxInfos[j],InitAccount);
                                if(result.Equals("true"))
                                    return;
                                Thread.Sleep(4000);
                            }
                            _logger.WriteInfo($"result is {result},verify failed");
                        }
                        
                        var verifyResult = new VerifyResult(result,TxInfos[j],SideChains[k].chainId);
                        VerifyResultsList.Add(verifyResult);
                    }
                    Thread.Sleep(100);
                }

                foreach (VerifyResult item in VerifyResultsList)
                {
                    switch (item.Result)
                    {
                        case "true":
                            _logger.WriteInfo("On Side Chain {0}, transaction={1} Verify successfully.",item.ChaindId,item.TxInfo.TxId);
                            break;
                        case "false":
                            _logger.WriteInfo("On Side Chain {0}, transaction={1} Verify failed.",item.ChaindId,item.TxInfo.TxId);
                            break;
                        default:
                            continue;
                    }
                    Thread.Sleep(10);
                }

                blockHeight = blockHeight+10;
                CheckNodeStatus(MainChain);
            }
        }
        
        public void SideChainTransactionVerifyOnMainChain(int sideChainNum)
        {
            TxInfos = new List<TxInfo>();
            long blockHeight = 1;
            for (var r = 1; r > 0; r++) //continuous running
            {
                var ci = new CommandInfo(ApiMethods.GetBlockHeight);
                SideChains[sideChainNum].ApiHelper.GetBlockHeight(ci);
                var currentHeight = (long) ci.InfoMsg;
                if (blockHeight + 10 > currentHeight)
                {
                    blockHeight = currentHeight-10;
                }
  
                for (var i = blockHeight ; i < blockHeight+10; i++)
                {
                    var CI = new CommandInfo(ApiMethods.GetBlockByHeight){Parameter = $"{i} {true}"};
                    var result = SideChains[sideChainNum].ApiHelper.ExecuteCommand(CI);
                    var blockResult = result.InfoMsg as BlockDto;
                    var txIds = blockResult.Body.Transactions;

                    for (int j = 0; j < txIds.Count; j++)
                    {
                        var txInfo = new TxInfo(i,txIds[j]);
                        TxInfos.Add(txInfo);
                        _logger.WriteInfo($"the transactions block is:{txInfo.BlockNumber},transaction id is: {txInfo.TxId}");
                    }
                }            

                _logger.WriteInfo("Waiting for the index");
                Thread.Sleep(20000);
                _logger.WriteInfo("Verify on the other chain");

                for (int j = 0; j < TxInfos.Count; j++)
                {
                    //verify on other side chain
                    for (int k = 0; k < SideChains.Count; k++)
                    {
                        if(k == sideChainNum) continue;
                        _logger.WriteInfo($"Verify on the side chain {k}");
                        _logger.WriteInfo($"Verify block:{TxInfos[j].BlockNumber},transaction:{TxInfos[j].TxId}");
                        var result = VerifySideChainTransaction(SideChains[k],TxInfos[j],sideChainNum,InitAccount);
                        
                        if (result.Equals("false"))
                        {
                            Thread.Sleep(4000);
                            var checkTime = 5;
                            _logger.WriteInfo($"the verify result is {result}, revalidate the results");
                            while (checkTime>0)
                            {
                                checkTime--;
                                result = VerifySideChainTransaction(SideChains[k],TxInfos[j],sideChainNum,InitAccount);
                                if(result.Equals("true"))
                                    return;
                                Thread.Sleep(4000);
                            }
                            _logger.WriteInfo($"result is {result},verify failed");
                        }
                        
                        var verifyResult = new VerifyResult(result,TxInfos[j],SideChains[k].chainId);
                        VerifyResultsList.Add(verifyResult);
                    }
                    Thread.Sleep(100);
                    
                    //verify on main chain 
                    _logger.WriteInfo($"Verify on the Main chain");
                    _logger.WriteInfo($"Verify block:{TxInfos[j].BlockNumber},transaction:{TxInfos[j].TxId}");
                     var resultOnMain = VerifySideChainTransaction(MainChain,TxInfos[j],sideChainNum,InitAccount);
                    if (resultOnMain.Equals("false"))
                    {
                        Thread.Sleep(4000);
                        var checkTime = 5;
                        _logger.WriteInfo($"the verify result is {resultOnMain}, revalidate the results");
                        while (checkTime>0)
                        {
                            checkTime--;
                            resultOnMain = VerifySideChainTransaction(MainChain,TxInfos[j],sideChainNum,InitAccount);
                            if(resultOnMain.Equals("true"))
                                return;
                            Thread.Sleep(4000);
                        }
                        _logger.WriteInfo($"result is {resultOnMain},verify failed");
                    }
                    var verifyResultOnMain = new VerifyResult(resultOnMain,TxInfos[j],MainChain.chainId);
                    VerifyResultsList.Add(verifyResultOnMain);
                }

                foreach (VerifyResult item in VerifyResultsList)
                {
                    switch (item.Result)
                    {
                        case "true":
                            _logger.WriteInfo("On chain {0} , transaction={1} Verify successfully.",item.ChaindId,item.TxInfo.TxId);
                            break;
                        case "false":
                            _logger.WriteInfo("On chain {0}, transaction={1} Verify failed.",item.ChaindId,item.TxInfo.TxId);
                            break;
                        default:
                            continue;
                    }
                    Thread.Sleep(10);
                }

                blockHeight = blockHeight+10;
                CheckNodeStatus(SideChains[sideChainNum]);
            }
        }
        
        public void TransferOnMainChainAndVerifyOnSideChain()
        {
            _logger.WriteInfo("Transfer on the Main chain");
            TxInfos = new List<TxInfo>();
            VerifyResultsList = new List<VerifyResult>();
            for (int i = 0; i < AccountList.Count; i++)
            {
                var txInfo = Transfer(MainChain, InitAccount, AccountList[i].Account, 10000);
                TxInfos.Add(txInfo);
                Thread.Sleep(100);
                _logger.WriteInfo($"the transactions block is:{txInfo.BlockNumber},transaction id is{txInfo.TxId}");
            }
            
            CheckNodeStatus(MainChain);
            
            Thread.Sleep(60000);
            _logger.WriteInfo("Verify on the side chain");

            for (int j = 0; j < TxInfos.Count; j++)
            {
                for (int k = 0; k < SideChains.Count; k++)
                {
                    var result = VerifyMainChainTransaction(SideChains[k],TxInfos[j],InitAccount);
                    if (result.Equals("false"))
                    {
                        Thread.Sleep(4000);
                        var checkTime = 5;
                        _logger.WriteInfo($"the verify result is {result}, revalidate the results");
                        while (checkTime>0)
                        {
                            checkTime--;
                            result = VerifyMainChainTransaction(SideChains[k],TxInfos[j],InitAccount);
                            if(result.Equals("true"))
                                return;
                            Thread.Sleep(4000);
                        }
                        _logger.WriteInfo($"result is {result},verify failed");
                    }
                  
                    var verifyResult = new VerifyResult(result,TxInfos[j],SideChains[k].chainId);
                    VerifyResultsList.Add(verifyResult);
                }
                Thread.Sleep(100);
            }

            foreach (VerifyResult item in VerifyResultsList)
            {
                switch (item.Result)
                {
                    case "true":
                        _logger.WriteInfo("On Side Chain={0}, transaction={1} Verify successfully.",item.ChaindId,item.TxInfo.TxId);
                        break;
                    case "false":
                        _logger.WriteInfo("On Side Chain={0}, transaction={1} Verify failed.",item.ChaindId,item.TxInfo.TxId);
                        
                        break;
                    default:
                        continue;
                }
                Thread.Sleep(10);
            }
        }

        public void CrossChainTransferToInitAccount()
        {
            //Main Chain Transfer to SideChain
            //Get all side chain id;
            _logger.WriteInfo("Get all side chain ids");
            var chainIdList = new List<int>();
            for (int i = 0; i < SideUrls.Count; i++)
            {
                int chainId = ChainHelpers.ConvertBase58ToChainId(SideChains[i].chainId);
                chainIdList.Add(chainId);
            }
            
            _logger.WriteInfo("Main chan transfer to side chain InitAccount ");
            for (int i = 0; i < chainIdList.Count; i++)
            {
                var rawTxInfo = CrossChainTransfer(MainChain, InitAccount,InitAccount,chainIdList[i], 10000000);
                Thread.Sleep(100);
                RawTxInfos.Add(rawTxInfo);
                _logger.WriteInfo($"the transactions block is:{rawTxInfo.BlockNumber},transaction id is{rawTxInfo.TxId}");
            }
            
            Thread.Sleep(60000);
            for (int i = 0; i < chainIdList.Count; i++)
            {
                _logger.WriteInfo($"Side chain {chainIdList[i]} receive the token");
                var accountBalance = ReceiveFromMainChain(SideChains[i], RawTxInfos[i]);
                _logger.WriteInfo($"On side chain {chainIdList[i]}, InitAccount:{InitAccount} balance is {accountBalance.Balance}");
            }
        }

        public void MainChainTransferToSideChain(int sideChainNumber,long amount)
        {
            AccountList = new List<AccountInfo>();
            RawTxInfos = new List<TxInfo>();
            //create account on side chain
            NewAccounts(SideChains[sideChainNumber],5);

            //Unlock Account
            UnlockAllAccounts(SideChains[sideChainNumber],5);
            
            //Main Chain Transfer to SideChain
            int chainId = ChainHelpers.ConvertBase58ToChainId(SideChains[sideChainNumber].chainId);
            for (int i = 0; i < AccountList.Count; i++)
            {
                var rawTxInfo = CrossChainTransfer(MainChain, InitAccount,AccountList[i].Account,chainId, amount);
                Thread.Sleep(100);
                RawTxInfos.Add(rawTxInfo);
                _logger.WriteInfo($"the transactions block is:{rawTxInfo.BlockNumber},transaction id is{rawTxInfo.TxId}");
            }

            Thread.Sleep(60000);
            _logger.WriteInfo($"Side chain {sideChainNumber} receive the token");
            //Side Chain Receive 
            for (int i = 0; i < RawTxInfos.Count; i++)
            {
                var accountBalance = ReceiveFromMainChain(SideChains[sideChainNumber], RawTxInfos[i]);
                Assert.IsTrue(accountBalance.Balance == amount);
                _logger.WriteInfo($"Account:{RawTxInfos[i].ReceiveAccount} balance is {accountBalance.Balance}");
            }
        }

        public void MultiCrossChainTransferFromMainChain()
        {
            AccountLists = new List<List<string>>();
            _logger.WriteInfo("Create account on main chain:");
            var mainAccountList = NewAccount(MainChain,5);
            UnlockAccounts(MainChain,5,mainAccountList);
            AccountLists.Add(mainAccountList);
            
            _logger.WriteInfo("Create account on each side chain:");
            for (int i = 0; i < SideUrls.Count; i++)
            {
                _logger.WriteInfo($"On chain {SideChains[i].chainId}: ");
                var accountList = NewAccount(SideChains[i],5);
                UnlockAccounts(SideChains[i],5,accountList);
                AccountLists.Add(accountList);
            }
            
            _logger.WriteInfo("Transfer token to each account :");
            for (int i = 0; i < AccountLists[0].Count; i++)
            {
                Transfer(MainChain,InitAccount,AccountLists[0][i],10000);
            }

            for (int i = 1; i < AccountLists.Count; i++)
            {
                for (int j = 0; j < AccountLists[i].Count; j++)
                {
                    Transfer(SideChains[i-1], InitAccount, AccountLists[i][j], 10000);
                }
            }

            _logger.WriteInfo("Main chain transfer to each account");
            var sideRawTxInfos = new List<List<TxInfo>>();
            for (int i = 0; i < SideChains.Count; i++)
            {
                RawTxInfos = new List<TxInfo>();
               
                int chainId = ChainHelpers.ConvertBase58ToChainId(SideChains[i].chainId);
                for (int j = 0; j < AccountLists[0].Count; j++)
                {
                    for (int k = 0; k < AccountLists[i+1].Count; k++)
                    {
                        var rawTxInfo = CrossChainTransfer(MainChain, AccountLists[0][j],AccountLists[i+1][k],chainId,100);
                        Thread.Sleep(100);
                        RawTxInfos.Add(rawTxInfo);
                        _logger.WriteInfo($"the transactions block is:{rawTxInfo.BlockNumber},transaction id is: {rawTxInfo.TxId}");
                    } 
                }
                sideRawTxInfos.Add(RawTxInfos);
            }
            
            _logger.WriteInfo("Waiting for the index");
            Thread.Sleep(60000);
            _logger.WriteInfo("Side chain receive the token");
            //Side Chain Receive 
            for (int i = 0; i < SideChains.Count; i++)
            {
                for (int j = 0; j < sideRawTxInfos[i].Count; j++)
                {
                    var accountBalance = ReceiveFromMainChain(SideChains[i], sideRawTxInfos[i][j]);
                    _logger.WriteInfo($"Account:{sideRawTxInfos[i][j].ReceiveAccount} balance is {accountBalance.Balance}");
                }
            }
            
            _logger.WriteInfo("show the balance of all account");

            for (int i = 0; i < AccountLists[0].Count; i++)
            {
                var accountBalacnce = MainChain.GetBalance(AccountLists[0][i], "ELF");
                _logger.WriteInfo($"Account:{accountBalacnce.Owner}, balance is:{accountBalacnce.Balance}");
            }

            for (int i = 0; i < SideChains.Count; i++)
            {
                for (int j = 0; j < AccountLists[i+1].Count; j++)
                {
                    var accountBalance = SideChains[i].GetBalance(AccountLists[i+1][j], "ELF");
                    _logger.WriteInfo($"Account:{accountBalance.Owner}, balance is: {accountBalance.Balance}");
                }
            }

            // side chain cross transfer
            for (int i = 0; i < SideChains.Count; i++)
            {
                MultiCrossChainTransferFromSideChain(i);
            }
        }

        public void MultiCrossChainTransferFromSideChain(int fromSideChainNum)
        {            
            _logger.WriteInfo("Side chain transfer to each account");
            var sideRawTxInfos = new List<List<TxInfo>>();
            var mainRawTxInfos = new List<TxInfo>();
            for (int i = 0; i < AccountLists[fromSideChainNum+1].Count; i++) //from side chain account
            {
                for (int j = 0; j < SideChains.Count; j++)
                {
                    if (j == fromSideChainNum) continue;
                    int chainId = ChainHelpers.ConvertBase58ToChainId(SideChains[j].chainId);
                    RawTxInfos = new List<TxInfo>();
                    for (int k = 0; k < AccountLists[i + 1].Count; k++) // to side chain account
                    {
                        var rawTxInfo = CrossChainTransfer(SideChains[fromSideChainNum],
                            AccountLists[fromSideChainNum + 1][i], AccountLists[j + 1][k], chainId, 100);
                        Thread.Sleep(100);
                        RawTxInfos.Add(rawTxInfo);
                        _logger.WriteInfo(
                                $"the transactions block is:{rawTxInfo.BlockNumber},transaction id is: {rawTxInfo.TxId}");
                    }
                    sideRawTxInfos.Add(RawTxInfos);
                }

                int mainChainId = ChainHelpers.ConvertBase58ToChainId(MainChain.chainId);
                RawTxInfos = new List<TxInfo>();
                for (int k = 0; k < AccountLists[0].Count; k++)
                {
                    var rawTxInfo = CrossChainTransfer(SideChains[fromSideChainNum], AccountLists[fromSideChainNum+1][i],AccountLists[0][k],mainChainId,100);
                    Thread.Sleep(100);
                    mainRawTxInfos.Add(rawTxInfo);
                    _logger.WriteInfo($"the transactions block is:{rawTxInfo.BlockNumber},transaction id is: {rawTxInfo.TxId}");
                }
            }
            _logger.WriteInfo("Waiting for the index");
            Thread.Sleep(60000);
            _logger.WriteInfo("Other chain receive the token");
            //Side Chain Receive 
            int fromChainId = ChainHelpers.ConvertBase58ToChainId(SideChains[fromSideChainNum].chainId);
            for (int i = 0; i < sideRawTxInfos.Count(); i++)
            {   
                for (int j = 0; j < sideRawTxInfos[i].Count; j++)
                {
                    if (i == fromSideChainNum)
                    {
                        var accountBalance = ReceiveFromSideChain(SideChains[i+1], fromSideChainNum, sideRawTxInfos[i][j],fromChainId);
                        _logger.WriteInfo($"Account:{sideRawTxInfos[i][j].ReceiveAccount} balance is {accountBalance.Balance}");
                    }
                    else
                    {
                        var accountBalance = ReceiveFromSideChain(SideChains[i],fromSideChainNum, sideRawTxInfos[i][j],fromChainId);
                        _logger.WriteInfo(
                            $"Account:{sideRawTxInfos[i][j].ReceiveAccount} balance is {accountBalance.Balance}");
                    }
                }
            }
            
            //Main chain receive
            for (int i = 0; i < mainRawTxInfos.Count(); i++)
            {   
                var accountBalance = ReceiveFromSideChain(MainChain, fromSideChainNum, mainRawTxInfos[i],fromChainId);
                _logger.WriteInfo($"Account:{mainRawTxInfos[i].ReceiveAccount} balance is {accountBalance.Balance}");   
            }
            
            
            _logger.WriteInfo("show the balance of all account");

            for (int i = 0; i < AccountLists[0].Count; i++)
            {
                var accountBalacnce = MainChain.GetBalance(AccountLists[0][i], "ELF");
                _logger.WriteInfo($"Account:{accountBalacnce.Owner}, balance is:{accountBalacnce.Balance}");
            }

            for (int i = 0; i < SideChains.Count; i++)
            {
                for (int j = 0; j < AccountLists[i+1].Count; j++)
                {
                    var accountBalance = SideChains[i].GetBalance(AccountLists[i+1][j], "ELF");
                    _logger.WriteInfo($"Account:{accountBalance.Owner}, balance is: {accountBalance.Balance}");
                }
            }
        }




        public void DeleteAccounts()
        {
            foreach (var item in AccountList)
            {
                var file = Path.Combine(KeyStorePath, $"{item.Account}.ak");
                File.Delete(file);
            }
        }


        private Operation InitMain(string initAccount)
        {
            var mainService = new ContractServices(BaseUrl, initAccount, "Main");
            MainChain = new Operation(mainService,"Main");
            return MainChain;
        }

        private List<Operation> InitSideNodes(string initAccount)
        {
            for (int i = 0; i < SideUrls.Count; i++)
            {
                var sideService = new ContractServices(SideUrls[i],initAccount,"Side");
                var side =  new Operation(sideService,"Side");
                SideChains.Add(side);
            }

            return SideChains;
        }


        private TxInfo Transfer(Operation chain,string initAccount,string toAddress,long amount)
        {
            var result = chain.TransferToken(initAccount, toAddress, amount, "ELF");
            var transferResult = result.InfoMsg as TransactionResultDto;
            var txIdInString = transferResult.TransactionId;
            var blockNumber = transferResult.BlockNumber;
            var txInfo = new TxInfo(blockNumber,txIdInString);

            return txInfo;
        }
        

        private string VerifyMainChainTransaction(Operation chain,TxInfo txinfo,string sideChainAccount)
        {
            var merklePath = GetMerklePath(MainChain,txinfo.BlockNumber,txinfo.TxId );
            
            var verificationInput = new VerifyTransactionInput
            {
                ParentChainHeight = txinfo.BlockNumber,
                TransactionId = Hash.LoadHex(txinfo.TxId),
                VerifiedChainId = 9992731
            };
            verificationInput.Path.AddRange(merklePath.Path);
            
            // change to side chain a to verify            
            var result = chain.VerifyTransaction(verificationInput,sideChainAccount);
            var verifyResult = result.InfoMsg as TransactionResultDto;
            var returnResult = verifyResult.ReadableReturnValue;
            return returnResult;
        }

        private string VerifySideChainTransaction(Operation chain,TxInfo txinfo,int sideChainNumber,string InitAccount)
        {           
            var merklePath = GetMerklePath(SideChains[sideChainNumber],txinfo.BlockNumber,txinfo.TxId );
            int chainId = ChainHelpers.ConvertBase58ToChainId(SideChains[sideChainNumber].chainId);
            var verificationInput = new VerifyTransactionInput
            {
                TransactionId = Hash.LoadHex(txinfo.TxId),
                VerifiedChainId = chainId
            };
            verificationInput.Path.AddRange(merklePath.Path);   

            // verify side chain transaction
            var crossChainMerkleProofContext =
                SideChains[sideChainNumber].GetBoundParentChainHeightAndMerklePathByHeight(InitAccount, txinfo.BlockNumber);
            verificationInput.Path.AddRange(crossChainMerkleProofContext.MerklePathForParentChainRoot.Path);
            verificationInput.ParentChainHeight = crossChainMerkleProofContext.BoundParentChainHeight;
            
            //verify in other chain            
            var result =
                chain.VerifyTransaction(verificationInput, InitAccount);
            var verifyResult = result.InfoMsg as TransactionResultDto;
            var returnResult = verifyResult.ReadableReturnValue;

            return returnResult;
        }
        
        private TxInfo CrossChainTransfer(Operation chain,string fromAccount,string toAccount,int toChainId,long amount)
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
            var receiveAccount = toAccount;
            var rawTxInfo = new TxInfo(blockNumber,txId,rawTx,fromAccount,receiveAccount);
            return rawTxInfo;
        }

        private GetBalanceOutput ReceiveFromMainChain(Operation chain,TxInfo rawTxInfo)
        {
            var merklePath = GetMerklePath(MainChain,rawTxInfo.BlockNumber,rawTxInfo.TxId);
                      
            var crossChainReceiveToken = new CrossChainReceiveTokenInput
            {
                FromChainId = 9992731,
                ParentChainHeight = rawTxInfo.BlockNumber
            };
            crossChainReceiveToken.MerklePath.AddRange(merklePath.Path);
            crossChainReceiveToken.TransferTransactionBytes = ByteString.CopyFrom(ByteArrayHelpers.FromHexString(rawTxInfo.RawTx));
            chain.CrossChainReceive(rawTxInfo.ReceiveAccount, crossChainReceiveToken);
            
           //Get Balance
            var balance = chain.GetBalance(rawTxInfo.ReceiveAccount, "ELF");
            return balance;
        }

        private GetBalanceOutput ReceiveFromSideChain(Operation chain, int fromSideChainNum,TxInfo rawTxInfo,int fromChainId)
        {
            var merklePath = GetMerklePath(SideChains[fromSideChainNum],rawTxInfo.BlockNumber,rawTxInfo.TxId);                          
            var crossChainReceiveToken = new CrossChainReceiveTokenInput
            {
                FromChainId = fromChainId,
            };
            crossChainReceiveToken.MerklePath.AddRange(merklePath.Path);
                              
            // verify side chain transaction
            var crossChainMerkleProofContext =
                SideChains[fromSideChainNum].GetBoundParentChainHeightAndMerklePathByHeight(rawTxInfo.FromAccount, rawTxInfo.BlockNumber);
            crossChainReceiveToken.MerklePath.AddRange(crossChainMerkleProofContext.MerklePathForParentChainRoot.Path);
            crossChainReceiveToken.ParentChainHeight = crossChainMerkleProofContext.BoundParentChainHeight;
            crossChainReceiveToken.TransferTransactionBytes = ByteString.CopyFrom(ByteArrayHelpers.FromHexString(rawTxInfo.RawTx));
            
            chain.CrossChainReceive(rawTxInfo.ReceiveAccount, crossChainReceiveToken);
            //Get Balance
            var balance = chain.GetBalance(rawTxInfo.ReceiveAccount, "ELF");
            return balance;
        }
        
        private MerklePath GetMerklePath(Operation chain,long blockNumber,string TxId)
        {
            int index = 0;
            var ci = new CommandInfo(ApiMethods.GetBlockByHeight){Parameter = $"{blockNumber} {true}"};
            ci = chain.ApiHelper.ExecuteCommand(ci);            
            var blockInfoResult = ci.InfoMsg as BlockDto;
            var transactionIds = blockInfoResult.Body.Transactions;
            var transactionStatus = new List<string>();
            
            foreach (var transactionId in transactionIds)
            {
                var CI = new CommandInfo(ApiMethods.GetTransactionResult) {Parameter = transactionId};
                var result = chain.ApiHelper.ExecuteCommand(CI);
                var txResult = result.InfoMsg as TransactionResultDto;
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
        
        private void UnlockAllAccounts(Operation chain,int count)
        {
            for (var i = 0; i < count; i++)
            {
                var ci = new CommandInfo(ApiMethods.AccountUnlock)
                {
                    Parameter = $"{AccountList[i].Account} 123 notimeout"
                };
                ci = chain.ApiHelper.ExecuteCommand(ci);
                Assert.IsTrue(ci.Result);
            }
        }

        private void NewAccounts(Operation chain,int count)
        {
            for (var i = 0; i < count; i++)
            {
                var ci = new CommandInfo(ApiMethods.AccountNew) {Parameter = "123"};
                ci = chain.ApiHelper.ExecuteCommand(ci);
                Assert.IsTrue(ci.Result);
                AccountList.Add(new AccountInfo(ci.InfoMsg.ToString().Replace("Account address:", "").Trim()));
            }
        }
        
        private List<string> NewAccount(Operation chain,int count)
        {
            var accountList = new List<string>();
            for (var i = 0; i < count; i++)
            {
                var ci = new CommandInfo(ApiMethods.AccountNew) {Parameter = "123"};
                ci = chain.ApiHelper.ExecuteCommand(ci);
                Assert.IsTrue(ci.Result);
                accountList.Add(ci.InfoMsg.ToString().Replace("Account address:","").Trim());
            }

            return accountList;
        }
        
        private void UnlockAccounts(Operation chain,int count,List<string> accountList)
        {
            for (var i = 0; i < count; i++)
            {
                var ci = new CommandInfo(ApiMethods.AccountUnlock)
                {
                    Parameter = $"{accountList[i]} 123 notimeout"
                };
                ci = chain.ApiHelper.ExecuteCommand(ci);
                Assert.IsTrue(ci.Result);
            }
        }
        
        
        private void CheckNodeStatus(Operation chain)
        {
            for (var i = 0; i < 10; i++)
            {
                var ci = new CommandInfo(ApiMethods.GetBlockHeight);
                chain.ApiHelper.GetBlockHeight(ci);
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