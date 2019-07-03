using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Acs7;
using AElf.Automation.Common.Contracts;
using AElf.Automation.Common.Helpers;
using AElf.Automation.Common.WebApi.Dto;
using AElf.Automation.SideChain.VerificationTest;
using AElf.Contracts.CrossChain;
using AElf.Contracts.MultiToken.Messages;
using AElf.CSharp.Core.Utils;
using AElf.Kernel;
using AElf.Types;
using Google.Protobuf;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AElf.Automation.SideChain.Verification.Test
{
    public class TxInfo
    {
        public string TxId { get; set; }
        public long BlockNumber { get; set; }
        public string RawTx { get; set; }
        public string FromAccount { get; set; }
        public string ReceiveAccount { get; set; }

        public TxInfo(long blockNumber, string txid, string rawTx, string fromAccount, string receiveAccount)
        {
            TxId = txid;
            BlockNumber = blockNumber;
            RawTx = rawTx;
            FromAccount = fromAccount;
            ReceiveAccount = receiveAccount;
        }

        public TxInfo(long blockNumber, string txid)
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

        public VerifyResult(string result, TxInfo txInfo, string chaindId)
        {
            Result = result;
            TxInfo = txInfo;
            ChaindId = chaindId;
        }
    }

    public class OperationSet
    {
        #region Public Property
        
        public string BaseUrl { get; set; }
        public List<string> SideUrls { get; set; }
        public string InitAccount { get; set; }
        public List<List<string>> AccountLists { get; set; }
        public List<string> MainChainAccountList { get; set; }
        public string KeyStorePath { get; set; }
        public long BlockHeight { get; set; }
        public int ThreadCount { get; }
        public int ExeTimes { get; }
        private readonly ILogHelper _logger = LogHelper.GetLogHelper();

        #endregion

        public Operation MainChain;
        public List<Operation> SideChains;

        public OperationSet(int threadCount,
            int exeTimes, string initAccount, List<string> sideUrls, string baseUrl,
            string keyStorePath = "")
        {
            if (keyStorePath == "")
                keyStorePath = CommonHelper.GetCurrentDataDir();
            
            SideUrls = sideUrls;
            AccountLists = new List<List<string>>();
            MainChainAccountList = new List<string>();

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
            MainChainAccountList = NewAccount(MainChain, 5);

            //Unlock Account
            UnlockAccounts(MainChain, 5, MainChainAccountList);
        }

        public void MainChainTransactionVerifyOnSideChains(string transactionIds)
        {
            var transactionId = transactionIds.Split(",");
            var transactionIdList = new List<string>(transactionId);
            var blockNumberList = new List<long>();
            //find the block number
            foreach (var txid in transactionIdList)
            {
                var ci = new CommandInfo(ApiMethods.GetTransactionResult) {Parameter = txid};
                var result = MainChain.ApiHelper.ExecuteCommand(ci);
                var txResult = result.InfoMsg as TransactionResultDto;
                var block = txResult.BlockNumber;
                blockNumberList.Add(block);
            }

            var blockHeight = blockNumberList.Max();
            _logger.WriteInfo($"Verify from block {blockHeight}");

            for (var r = 1; r > 0; r++) //continuous running
            {
                var txInfos = new List<TxInfo>();
                var verifyResultsList = new List<VerifyResult>();
                var ci = new CommandInfo(ApiMethods.GetBlockHeight);
                MainChain.ApiHelper.GetBlockHeight(ci);
                var currentHeight = (long) ci.InfoMsg;
                if (blockHeight + 50 > currentHeight)
                {
                    blockHeight = currentHeight - 50;
                }

                for (var i = blockHeight; i < blockHeight + 50; i++)
                {
                    var CI = new CommandInfo(ApiMethods.GetBlockByHeight) {Parameter = $"{i} {true}"};
                    var result = MainChain.ApiHelper.ExecuteCommand(CI);
                    var blockResult = result.InfoMsg as BlockDto;
                    var txIds = blockResult.Body.Transactions;

                    foreach (var txId in txIds)
                    {
                        var txInfo = new TxInfo(i, txId);
                        txInfos.Add(txInfo);
                        _logger.WriteInfo(
                            $"the transactions block is:{txInfo.BlockNumber},transaction id is: {txInfo.TxId}");
                    }
                }
                _logger.WriteInfo("Waiting for the index");
                Thread.Sleep(20000);
                
                _logger.WriteInfo("Verify on the side chain");

                foreach (var sideChain in SideChains)
                {
                    foreach (var txInfo in txInfos)
                    {
                        _logger.WriteInfo($"Verify block:{txInfo.BlockNumber},transaction:{txInfo.TxId}");
                        var result = VerifyMainChainTransaction(sideChain, txInfo, InitAccount);
                        switch (result)
                        {
                            case null:
                                continue;
                            case "false":
                            {
                                Thread.Sleep(4000);
                                var checkTime = 5;
                                _logger.WriteInfo($"the verify result is {result}, revalidate the results");
                                while (checkTime > 0)
                                {
                                    checkTime--;
                                    result = VerifyMainChainTransaction(sideChain, txInfo, InitAccount);
                                    if (result.Equals("true"))
                                        goto case "true";
                                    Thread.Sleep(4000);
                                }

                                _logger.WriteInfo($"result is {result},verify failed");
                                break;
                            }
                            case "true":
                            {
                                var verifyResult = new VerifyResult(result, txInfo, sideChain.chainId);
                                verifyResultsList.Add(verifyResult);
                                break;
                            }
                            default:
                                continue;
                        }
                    }

                    Thread.Sleep(100);
                }

                foreach (var item in verifyResultsList)
                {
                    switch (item.Result)
                    {
                        case "true":
                            _logger.WriteInfo("On Side Chain {0}, transaction={1} Verify successfully.", item.ChaindId,
                                item.TxInfo.TxId);
                            break;
                        case "false":
                            _logger.WriteInfo("On Side Chain {0}, transaction={1} Verify failed.", item.ChaindId,
                                item.TxInfo.TxId);
                            break;
                        default:
                            continue;
                    }

                    Thread.Sleep(10);
                }

                blockHeight += 50;
                CheckNodeStatus(MainChain);
            }
        }

        public void SideChainTransactionVerifyOnMainChain(int sideChainNum,long blockHeight)
        {
            for (var r = 1; r > 0; r++) //continuous running
            {
                var sideTxInfos = new List<TxInfo>();
                var sideVerifyResultsList = new List<VerifyResult>();
                var ci = new CommandInfo(ApiMethods.GetBlockHeight);
                SideChains[sideChainNum].ApiHelper.GetBlockHeight(ci);
                var currentHeight = (long) ci.InfoMsg;
                if (blockHeight + 50 > currentHeight)
                {
                    blockHeight = currentHeight - 50;
                }

                for (var i = blockHeight; i < blockHeight + 50; i++)
                {
                    var CI = new CommandInfo(ApiMethods.GetBlockByHeight) {Parameter = $"{i} true"};
                    var result = SideChains[sideChainNum].ApiHelper.ExecuteCommand(CI);
                    var blockResult = result.InfoMsg as BlockDto;
                    var txIds = blockResult.Body.Transactions;

                    foreach (var t in txIds)
                    {
                        var txInfo = new TxInfo(i, t);
                        sideTxInfos.Add(txInfo);
                        _logger.WriteInfo(
                            $"the transactions block is:{txInfo.BlockNumber},transaction id is: {txInfo.TxId}");
                    }
                }

                _logger.WriteInfo("Waiting for the index");
                Thread.Sleep(10000);

                _logger.WriteInfo("Verify on the other chain");
                foreach (var txInfo in sideTxInfos)
                {
                    //verify on other side chain
                    for (int k = 0; k < SideChains.Count; k++)
                    {
                        if (k == sideChainNum) continue;
                        _logger.WriteInfo($"Verify on the side chain {k}");
                        _logger.WriteInfo($"Verify block:{txInfo.BlockNumber},transaction:{txInfo.TxId}");
                        var result = VerifySideChainTransaction(SideChains[k], txInfo, sideChainNum);
                        switch (result)
                        {
                            case null:
                                continue;
                            case "false":
                            {
                                Thread.Sleep(4000);
                                var checkTime = 5;
                                _logger.WriteInfo($"the verify result is {result}, revalidate the results");
                                while (checkTime > 0)
                                {
                                    checkTime--;
                                    result = VerifySideChainTransaction(SideChains[k], txInfo, sideChainNum);
                                    if (result.Equals("true"))
                                        return;
                                    Thread.Sleep(4000);
                                }

                                _logger.WriteInfo($"result is {result},verify failed");
                                break;
                            }
                            case "true":
                            {
                                var verifyResult = new VerifyResult(result, txInfo, SideChains[k].chainId);
                                sideVerifyResultsList.Add(verifyResult);
                                break;
                            }
                            default:
                                continue;
                        }
                    }

                    Thread.Sleep(100);

                    //verify on main chain 
                    _logger.WriteInfo($"Verify on the Main chain");
                    _logger.WriteInfo($"Verify block:{txInfo.BlockNumber},transaction:{txInfo.TxId}");
                    var resultOnMain = VerifySideChainTransaction(MainChain, txInfo, sideChainNum);
                    switch (resultOnMain)
                    {
                        case null:
                            continue;
                        case "false":
                        {
                            Thread.Sleep(4000);
                            var checkTime = 5;
                            _logger.WriteInfo($"the verify result is {resultOnMain}, revalidate the results");
                            while (checkTime > 0)
                            {
                                checkTime--;
                                resultOnMain = VerifySideChainTransaction(MainChain, txInfo, sideChainNum);
                                if (resultOnMain.Equals("true"))
                                    return;
                                Thread.Sleep(4000);
                            }
                            _logger.WriteInfo($"result is {resultOnMain},verify failed");
                            break;
                        }
                        case "true":
                        {
                            var verifyResultOnMain = new VerifyResult(resultOnMain, txInfo, MainChain.chainId);
                            sideVerifyResultsList.Add(verifyResultOnMain);
                            break;
                        }
                        default:
                            continue;
                    }
                }

                foreach (var item in sideVerifyResultsList)
                {
                    switch (item.Result)
                    {
                        case "true":
                            _logger.WriteInfo("On chain {0} , transaction={1} Verify successfully.", item.ChaindId,
                                item.TxInfo.TxId);
                            break;
                        case "false":
                            _logger.WriteInfo("On chain {0}, transaction={1} Verify failed.", item.ChaindId,
                                item.TxInfo.TxId);
                            break;
                        default:
                            continue;
                    }

                    Thread.Sleep(10);
                }

                blockHeight += 50;
                CheckNodeStatus(SideChains[sideChainNum]);
            }
        }

        public void CrossChainTransferToInitAccount()
        {
            //Main Chain Transfer to SideChain
            //Get all side chain id;
            _logger.WriteInfo("Get all side chain ids");
            var chainIdList = new List<int>();
            for (var i = 0; i < SideUrls.Count; i++)
            {
                var chainId = ChainHelpers.ConvertBase58ToChainId(SideChains[i].chainId);
                chainIdList.Add(chainId);
            }

            _logger.WriteInfo("Main chan transfer to side chain InitAccount ");
            var initRawTxInfos = new List<TxInfo>();
            foreach (var chainId in chainIdList)
            {
                TxInfo rawTxInfo = null;
                var transferTimes = 5;
                while (rawTxInfo == null && transferTimes >0)
                {
                    transferTimes--;
                    rawTxInfo = CrossChainTransfer(MainChain, InitAccount, InitAccount, chainId, 100000);
                }
                Assert.IsTrue(transferTimes>0||rawTxInfo!=null, "The first cross chain transfer failed, please start over.");
                
                initRawTxInfos.Add(rawTxInfo);
                _logger.WriteInfo(
                    $"the transactions block is:{rawTxInfo.BlockNumber},transaction id is: {rawTxInfo.TxId}");
            }
            _logger.WriteInfo("Waiting for the index");
            Thread.Sleep(120000);
            
            for (var i = 0; i < chainIdList.Count; i++)
            {
                _logger.WriteInfo($"side chain {chainIdList[i]} received token");
                _logger.WriteInfo($"Receive CrossTransfer Transaction id is : {initRawTxInfos[i].TxId}");
                CommandInfo result = null;
                var transferTimes = 5;
                while (result ==null && transferTimes >0)
                {
                    transferTimes--;
                    result = ReceiveFromMainChain(SideChains[i], initRawTxInfos[i]);
                }
                if (result == null)
                {
                    _logger.WriteInfo($"{chainIdList[i]} receive transaction is failed.");
                    Assert.IsTrue(false, "The first receive transfer failed, please start over.");
                }
                
                var resultReturn = result.InfoMsg as TransactionResultDto;
                var status =
                    (TransactionResultStatus) Enum.Parse(typeof(TransactionResultStatus),
                        resultReturn.Status, true);
                if (status == TransactionResultStatus.NotExisted || status == TransactionResultStatus.Failed)
                {
                    Thread.Sleep(1000);
                    var checkTime = 5;
                    while (checkTime > 0)
                    {
                        _logger.WriteInfo($"Receive {6 - checkTime} time");
                        checkTime--;
                        var reResult = ReceiveFromMainChain(SideChains[i], initRawTxInfos[i]);
                        if(reResult == null) continue;
                        var reResultReturn = reResult.InfoMsg as TransactionResultDto;
                        var reStatus =
                            (TransactionResultStatus) Enum.Parse(typeof(TransactionResultStatus),
                                reResultReturn.Status, true);
                        if (reStatus == TransactionResultStatus.Mined)
                            goto GetBalance;
                        if (reResultReturn.Error.Contains("Token already claimed"))
                            goto GetBalance;
                        Thread.Sleep(2000);
                    }
                    
                    _logger.WriteInfo($"{chainIdList[i]} receive transaction is failed.");
                    Assert.IsTrue(false, "The first receive transfer failed, please start over.");
                }
                
                GetBalance:
                _logger.WriteInfo($"check the balance on the side chain {chainIdList[i]}");
                var accountBalance = SideChains[i].GetBalance(InitAccount, "ELF").Balance;

                _logger.WriteInfo(
                    $"On side chain {chainIdList[i]}, InitAccount:{InitAccount} balance is {accountBalance}");
            }
        }

        public void MultiCrossChainTransfer()
        {
            AccountLists = new List<List<string>>();

            _logger.WriteInfo("Create account on each side chain:");
            for (var i = 0; i < SideUrls.Count; i++)
            {
                _logger.WriteInfo($"On chain {SideChains[i].chainId}: ");
                var accountList = NewAccount(SideChains[i], 5);
                UnlockAccounts(SideChains[i], 5, accountList);
                AccountLists.Add(accountList);
                UnlockAccounts(SideChains[i],5,MainChainAccountList);
                //Unlock side chain account on other side chain
                for (int j = 0; j < SideUrls.Count; j++)
                {
                    if (j == i) continue;
                    UnlockAccounts(SideChains[j],5,accountList);
                }
            }

            // Unlock side chain account on main chain 
            foreach (var accountList in AccountLists)
            {
                MainChain.ApiHelper.ListAccounts();
                UnlockAccounts(MainChain,accountList.Count,accountList);
            }

            _logger.WriteInfo("Transfer token to each account :");
            foreach (var mainChainAccount in MainChainAccountList)
            {
                Transfer(MainChain, InitAccount, mainChainAccount, 10000);
            }

            for (var i = 0; i < AccountLists.Count; i++)
            {
                for (var j = 0; j < AccountLists[i].Count; j++)
                {
                    Transfer(SideChains[i], InitAccount, AccountLists[i][j], 10000);
                }
            }

            _logger.WriteInfo("show the main chain account balance: ");

            foreach (var mainAccount in MainChainAccountList)
            {
                var accountBalance = MainChain.GetBalance(mainAccount, "ELF");
                _logger.WriteInfo($"Account:{accountBalance.Owner}, balance is:{accountBalance.Balance}");
            }

            _logger.WriteInfo("show the side chain account balance: ");
            for (var i = 0; i < SideChains.Count; i++)
            {
                for (var j = 0; j < AccountLists[i].Count; j++)
                {
                    var accountBalance = SideChains[i].GetBalance(AccountLists[i][j], "ELF");
                    _logger.WriteInfo($"Account:{accountBalance.Owner}, balance is: {accountBalance.Balance}");
                }
            }

            for (var r = 1; r > 0; r++) //continuous running
            {
                _logger.WriteInfo($"{r} Round");
                _logger.WriteInfo("Main chain transfer to each account");
                var sideRawTxInfos = new List<List<TxInfo>>();
                for (var i = 0; i < SideChains.Count; i++)
                {
                    var rawTxInfos = new List<TxInfo>();

                    var chainId = ChainHelpers.ConvertBase58ToChainId(SideChains[i].chainId);
                    for (var j = 0; j < MainChainAccountList.Count; j++)
                    {
                        for (var k = 0; k < AccountLists[i].Count; k++)
                        {
                            var rawTxInfo = CrossChainTransfer(MainChain, MainChainAccountList[j], AccountLists[i][k],
                                chainId, 100);
                            if(rawTxInfo == null) continue;
                            
                            Thread.Sleep(100);
                            rawTxInfos.Add(rawTxInfo);
                            _logger.WriteInfo(
                                $"the transactions block is:{rawTxInfo.BlockNumber},transaction id is: {rawTxInfo.TxId}");
                        }
                    }
                    sideRawTxInfos.Add(rawTxInfos);
                }

                _logger.WriteInfo("Waiting for the index");
                Thread.Sleep(120000);
                _logger.WriteInfo("Side chain receive the token");
                //Side Chain Receive 
                for (int i = 0; i < SideChains.Count; i++)
                {
                    for (int j = 0; j < sideRawTxInfos[i].Count; j++)
                    {
                        _logger.WriteInfo($"Receive CrossTransfer Transaction id is : {sideRawTxInfos[i][j].TxId}");
                        
                        var result = ReceiveFromMainChain(SideChains[i],sideRawTxInfos[i][j]);
                        if (result == null) continue;
                        var resultReturn = result.InfoMsg as TransactionResultDto;
                        var status =
                            (TransactionResultStatus) Enum.Parse(typeof(TransactionResultStatus),
                                resultReturn.Status, true);

                        if (status == TransactionResultStatus.Failed || status == TransactionResultStatus.NotExisted)
                        {
                            Thread.Sleep(1000);
                            var checkTime = 5;
                           
                            while (checkTime > 0)
                            {
                                _logger.WriteInfo($"Receive {6 - checkTime} time");
                                checkTime--;
                                var reResult = ReceiveFromMainChain(SideChains[i],sideRawTxInfos[i][j]);
                                var reResultReturn = reResult.InfoMsg as TransactionResultDto;
                                var reStatus =
                                    (TransactionResultStatus) Enum.Parse(typeof(TransactionResultStatus),
                                        reResultReturn.Status, true);
                                if (reStatus == TransactionResultStatus.Mined)
                                    goto GetBalance;
                                Thread.Sleep(2000);
                            }
                            
                            _logger.WriteInfo($"the receive transaction {resultReturn.TransactionId} is failed.");
                        }
                        GetBalance:
                        _logger.WriteInfo($"check the balance on the side chain");
                        var accountBalance = SideChains[i].GetBalance(sideRawTxInfos[i][j].ReceiveAccount, "ELF").Balance;
                
                        _logger.WriteInfo($"On side chain {i+1}, Account:{sideRawTxInfos[i][j].ReceiveAccount} balance is {accountBalance}");
                        
                        Thread.Sleep(1000);
                    }
                }

                _logger.WriteInfo("show the main chain account balance: ");

                for (int i = 0; i < MainChainAccountList.Count; i++)
                {
                    var accountBalance = MainChain.GetBalance(MainChainAccountList[i], "ELF");
                    _logger.WriteInfo($"Account:{accountBalance.Owner}, balance is:{accountBalance.Balance}");
                }

                _logger.WriteInfo("show the side chain account balance: ");
                for (var i = 0; i < SideChains.Count; i++)
                {
                    for (var j = 0; j < AccountLists[i].Count; j++)
                    {
                        var accountBalance = SideChains[i].GetBalance(AccountLists[i][j], "ELF");
                        _logger.WriteInfo($"Account:{accountBalance.Owner}, balance is: {accountBalance.Balance}");
                    }
                }

                // side chain cross transfer
                for (var i = 0; i < SideChains.Count; i++)
                {
                    MultiCrossChainTransferFromSideChain(i);
                }
            }
        }

        private void MultiCrossChainTransferFromSideChain(int fromSideChainNum)
        {
            _logger.WriteInfo($"Side chain {fromSideChainNum + 1} transfer to each account");
            var sideRawTxInfos = new List<List<TxInfo>>();
            var mainRawTxInfos = new List<TxInfo>();
            for (var i = 0; i < SideChains.Count; i++) //to side chain
            {
                if (i == fromSideChainNum) continue;
                var rawTxInfos = new List<TxInfo>();
                for (var j = 0; j < AccountLists[fromSideChainNum].Count; j++) // from side chain
                {
                    var chainId = ChainHelpers.ConvertBase58ToChainId(SideChains[i].chainId);
                    for (var k = 0; k < AccountLists[i].Count; k++) // to side chain account
                    {
                        var rawTxInfo = CrossChainTransfer(SideChains[fromSideChainNum],
                            AccountLists[fromSideChainNum][j], AccountLists[i][k], chainId, 100);
                        if (rawTxInfo == null) continue;
                        
                        Thread.Sleep(100);
                        rawTxInfos.Add(rawTxInfo);
                        _logger.WriteInfo(
                            $"the transactions block is:{rawTxInfo.BlockNumber},transaction id is: {rawTxInfo.TxId}");
                    }
                }
                sideRawTxInfos.Add(rawTxInfos);
            }
            
            for (var j = 0; j < AccountLists[fromSideChainNum].Count; j++) // from side chain
            {
                var mainChainId = ChainHelpers.ConvertBase58ToChainId(MainChain.chainId);
                for (var k = 0; k < MainChainAccountList.Count; k++)
                {
                    var rawTxInfo = CrossChainTransfer(SideChains[fromSideChainNum], AccountLists[fromSideChainNum][j],MainChainAccountList[k],mainChainId,100);
                    if (rawTxInfo == null) continue;
                    
                    Thread.Sleep(100);
                    mainRawTxInfos.Add(rawTxInfo);
                    _logger.WriteInfo(
                        $"the transactions block is:{rawTxInfo.BlockNumber},transaction id is: {rawTxInfo.TxId}");
                }
            }

            _logger.WriteInfo("Waiting for the index");
            Thread.Sleep(150000);
                  
            //Side Chain Receive 
            var fromChainId = ChainHelpers.ConvertBase58ToChainId(SideChains[fromSideChainNum].chainId);
            for (var i = 0; i < SideChains.Count; i++)
            {
                if (i == fromSideChainNum) continue;
                _logger.WriteInfo("side chain receive the token");
                for (var j = 0; j < sideRawTxInfos.Count; j++)
                {
                    for (var k = 0; k < sideRawTxInfos[j].Count; k++)
                    {
                        _logger.WriteInfo($"Receive CrossTransfer Transaction id is :{sideRawTxInfos[j][k].TxId}");
                        
                        var crossChainReceiveToken = GetCrossChainReceiveTokenInput(fromSideChainNum,sideRawTxInfos[j][k],fromChainId);
                        if (crossChainReceiveToken == null) continue;
                        
                        var result = SideChains[i].CrossChainReceive(sideRawTxInfos[j][k].FromAccount, crossChainReceiveToken);
                        if (result == null) continue;
                        
                        var resultReturn = result.InfoMsg as TransactionResultDto;
                        var status =
                            (TransactionResultStatus) Enum.Parse(typeof(TransactionResultStatus),
                                resultReturn.Status, true);

                        if (status == TransactionResultStatus.Failed || status == TransactionResultStatus.NotExisted)
                        {
                            Thread.Sleep(1000);
                            var checkTime = 5;
                            _logger.WriteInfo($"Receive {6 - checkTime} time");
                            while (checkTime > 0)
                            {
                                checkTime--;
                                var reResult = SideChains[i].CrossChainReceive(sideRawTxInfos[j][k].FromAccount, crossChainReceiveToken);
                                var reResultReturn = reResult.InfoMsg as TransactionResultDto;
                                var reStatus =
                                    (TransactionResultStatus) Enum.Parse(typeof(TransactionResultStatus),
                                        reResultReturn.Status, true);
                                if (reStatus == TransactionResultStatus.Mined)
                                    goto GetBalance;
                                Thread.Sleep(2000);
                            }
                            _logger.WriteInfo($"the receive transaction {resultReturn.TransactionId} is failed.");
                        }
                
                        GetBalance:
                        _logger.WriteInfo($"check the balance on the side chain");
                        var accountBalance = SideChains[i].GetBalance(sideRawTxInfos[j][k].ReceiveAccount, "ELF").Balance;
                
                        _logger.WriteInfo($"On side chain {i+1}, Account:{sideRawTxInfos[j][k].ReceiveAccount} balance is {accountBalance}");
                        Thread.Sleep(1000);
                    }
                }
            }

            //Main chain receive
            _logger.WriteInfo("Main chain receive the token");
            for (int i = 0; i < mainRawTxInfos.Count(); i++)
            {   
                _logger.WriteInfo($"Receive CrossTransfer Transaction id is :{mainRawTxInfos[i].TxId}");
                
                var crossChainReceiveToken = GetCrossChainReceiveTokenInput(fromSideChainNum,mainRawTxInfos[i],fromChainId);
                if(crossChainReceiveToken == null) continue;
                var result = MainChain.CrossChainReceive(mainRawTxInfos[i].FromAccount, crossChainReceiveToken);
                if (result == null) continue;
                
                var resultReturn = result.InfoMsg as TransactionResultDto;
                var status =
                    (TransactionResultStatus) Enum.Parse(typeof(TransactionResultStatus),
                        resultReturn.Status, true);

                if (status == TransactionResultStatus.Failed || status == TransactionResultStatus.NotExisted)
                {
                    Thread.Sleep(1000);
                    var checkTime = 5;
                    _logger.WriteInfo($"Receive {6 - checkTime} time");
                    while (checkTime > 0)
                    {
                        checkTime--;
                        var reResult = MainChain.CrossChainReceive(mainRawTxInfos[i].FromAccount, crossChainReceiveToken);
                        var reResultReturn = reResult.InfoMsg as TransactionResultDto;
                        var reStatus =
                            (TransactionResultStatus) Enum.Parse(typeof(TransactionResultStatus),
                                reResultReturn.Status, true);
                        if (reStatus == TransactionResultStatus.Mined)
                            goto GetBalance;
                        Thread.Sleep(2000);
                    }
                    _logger.WriteInfo($"the receive transaction {resultReturn.TransactionId} is failed.");
                }

                GetBalance:
                _logger.WriteInfo($"check the balance on the main chain");
                var accountBalance = MainChain.GetBalance(mainRawTxInfos[i].ReceiveAccount, "ELF").Balance;
                
                _logger.WriteInfo($"On main chain , Account:{mainRawTxInfos[i].ReceiveAccount} balance is {accountBalance}");
                Thread.Sleep(1000);
            }
            
            
            _logger.WriteInfo("show the main chain account balance: ");

            for (int i = 0; i < MainChainAccountList.Count; i++)
            {
                var accountBalacnce = MainChain.GetBalance(MainChainAccountList[i], "ELF");
                _logger.WriteInfo($"Account:{accountBalacnce.Owner}, balance is:{accountBalacnce.Balance}");
            }
            
            _logger.WriteInfo("show the side chain account balance: ");
            for (var i = 0; i < SideChains.Count; i++)
            {
                for (var j = 0; j < AccountLists[i].Count; j++)
                {
                    var accountBalance = SideChains[i].GetBalance(AccountLists[i][j], "ELF");
                    _logger.WriteInfo($"Account:{accountBalance.Owner}, balance is: {accountBalance.Balance}");
                }
            }
        }

        public void DeleteAccounts()
        {
            foreach (var item in MainChainAccountList)
            {
                var file = Path.Combine(KeyStorePath, $"{item}.ak");
                File.Delete(file);
            }

            foreach (var items in AccountLists)
            {
                foreach (var item in items)
                {
                    var file = Path.Combine(KeyStorePath, $"{item}.ak");
                    File.Delete(file);
                }
            }
        }

        #region private method

        private Operation InitMain(string initAccount)
        {
            var mainService = new ContractServices(BaseUrl, initAccount, "Main", KeyStorePath);
            MainChain = new Operation(mainService, "Main");
            return MainChain;
        }

        private List<Operation> InitSideNodes(string initAccount)
        {
            foreach (var t in SideUrls)
            {
                var sideService = new ContractServices(t, initAccount, "Side", KeyStorePath);
                var side = new Operation(sideService, "Side");
                SideChains.Add(side);
            }

            return SideChains;
        }

        private void Transfer(Operation chain, string initAccount, string toAddress, long amount)
        {
            var transferTimes = 5;
            CommandInfo result = null; 
            while (result == null && transferTimes >0)
            {
                transferTimes--;
                result = chain.TransferToken(initAccount, toAddress, amount, "ELF");
            }

            if (result == null)
            {
                _logger.WriteInfo("Transfer transaction is failed.");
                Assert.IsTrue(false, "Initial fund transfer failed.");
            }
            var resultReturn = result.InfoMsg as TransactionResultDto;
            var status =
                (TransactionResultStatus) Enum.Parse(typeof(TransactionResultStatus),
                    resultReturn.Status, true);
            if (status == TransactionResultStatus.Mined) return;
            Thread.Sleep(1000);
            var checkTime = 5;
            while (checkTime > 0)
            {
                _logger.WriteInfo($"Transfer {6 - checkTime} time");
                checkTime--;
                var reResult = chain.TransferToken(initAccount, toAddress, amount, "ELF");
                if(reResult == null) continue;
                var reResultReturn = reResult.InfoMsg as TransactionResultDto;
                var resSatus =
                    (TransactionResultStatus) Enum.Parse(typeof(TransactionResultStatus),
                        reResultReturn.Status, true);
                if (resSatus == TransactionResultStatus.Mined)
                    return;
                Thread.Sleep(2000);
            }
        }

        private string VerifyMainChainTransaction(Operation chain, TxInfo txinfo, string sideChainAccount)
        {
            var merklePath = GetMerklePath(MainChain, txinfo.BlockNumber, txinfo.TxId);
            if (merklePath == null) return null;
            
            var verificationInput = new VerifyTransactionInput
            {
                ParentChainHeight = txinfo.BlockNumber,
                TransactionId = Hash.LoadHex(txinfo.TxId),
                VerifiedChainId = 9992731
            };
            verificationInput.Path.AddRange(merklePath.Path);

            // change to side chain a to verify   
            var verifyTimes = 5;
            CommandInfo result = null; 
            while (result == null && verifyTimes >0)
            {
                verifyTimes--;
                result = chain.VerifyTransaction(verificationInput, sideChainAccount);
            }
            if (result == null)
                return null;
            
            var verifyResult = result.InfoMsg as TransactionResultDto;
            var status =
                (TransactionResultStatus) Enum.Parse(typeof(TransactionResultStatus),
                    verifyResult.Status, true);
            if (status == TransactionResultStatus.Mined) 
                return verifyResult.ReadableReturnValue;
            Thread.Sleep(1000);
            var checkTime = 3;
            while (checkTime > 0)
            {
                _logger.WriteInfo($"Send verify transaction  {6 - checkTime} time");
                checkTime--; 
                result = chain.VerifyTransaction(verificationInput, sideChainAccount);
                if (result == null) continue;
                verifyResult = result.InfoMsg as TransactionResultDto;
                status =
                    (TransactionResultStatus) Enum.Parse(typeof(TransactionResultStatus),
                        verifyResult.Status, true);
                if (status == TransactionResultStatus.Mined)
                    break;
                Thread.Sleep(2000);
            }
            return verifyResult.ReadableReturnValue;
        }

        private string VerifySideChainTransaction(Operation chain, TxInfo txinfo, int sideChainNumber)
        {
            var merklePath = GetMerklePath(SideChains[sideChainNumber], txinfo.BlockNumber, txinfo.TxId);
            if (merklePath == null) return null;
            int chainId = ChainHelpers.ConvertBase58ToChainId(SideChains[sideChainNumber].chainId);
            var verificationInput = new VerifyTransactionInput
            {
                TransactionId = Hash.LoadHex(txinfo.TxId),
                VerifiedChainId = chainId
            };
            verificationInput.Path.AddRange(merklePath.Path);

            // verify side chain transaction
            var crossChainMerkleProofContext =
                SideChains[sideChainNumber]
                    .GetBoundParentChainHeightAndMerklePathByHeight(InitAccount, txinfo.BlockNumber);
            verificationInput.Path.AddRange(crossChainMerkleProofContext.MerklePathForParentChainRoot.Path);
            verificationInput.ParentChainHeight = crossChainMerkleProofContext.BoundParentChainHeight;

            //verify in other chain            
            var result =
                chain.VerifyTransaction(verificationInput, InitAccount);
            var verifyResult = result.InfoMsg as TransactionResultDto;
            var returnResult = verifyResult.ReadableReturnValue;

            return returnResult;
        }

        private TxInfo CrossChainTransfer(Operation chain, string fromAccount, string toAccount, int toChainId,
            long amount)
        {
            //get token info
            var tokenInfo = chain.GetTokenInfo("ELF");
            //Transfer
            var crossChainTransferInput = new CrossChainTransferInput()
            {
                Amount = amount,
                Memo = "cross chain transfer",
                To = Address.Parse(toAccount),
                ToChainId = toChainId,
                TokenInfo = tokenInfo
            };
            // var result = chain.CrossChainTransfer(fromAccount,crossChainTransferInput);
            // execute cross chain transfer
            var rawTx = chain.ApiHelper.GenerateTransactionRawTx(fromAccount,
                chain.TokenService.ContractAddress, TokenMethod.CrossChainTransfer.ToString(), crossChainTransferInput);
            _logger.WriteInfo($"Transaction rawTx is: {rawTx}");
            var txId = ExecuteMethodWithTxId(chain, rawTx);
            var result = CheckTransactionResult(chain, txId);
            if (result == null)
                return null;
            // get transaction info            
            var txResult = result.InfoMsg as TransactionResultDto;
            var status =
                (TransactionResultStatus) Enum.Parse(typeof(TransactionResultStatus),
                    txResult.Status, true);
            if (status == TransactionResultStatus.NotExisted||status == TransactionResultStatus.Failed)
            {
                Thread.Sleep(2000);
                _logger.WriteInfo("Check the transaction again.");
                result = CheckTransactionResult(chain, txId);
                txResult = result.InfoMsg as TransactionResultDto;
                status =
                    (TransactionResultStatus) Enum.Parse(typeof(TransactionResultStatus),
                        txResult.Status, true);
                if (status == TransactionResultStatus.NotExisted||status == TransactionResultStatus.Failed)
                    goto Nullable;
            }
            var blockNumber = txResult.BlockNumber;
            var receiveAccount = toAccount;
            var rawTxInfo = new TxInfo(blockNumber, txId, rawTx, fromAccount, receiveAccount);
            return rawTxInfo;
            Nullable:
            return null;
        }

        private CommandInfo ReceiveFromMainChain(Operation chain, TxInfo rawTxInfo)
        {
            var merklePath = GetMerklePath(MainChain, rawTxInfo.BlockNumber, rawTxInfo.TxId);
            if (merklePath == null)
                return null;
            var crossChainReceiveToken = new CrossChainReceiveTokenInput
            {
                FromChainId = 9992731,
                ParentChainHeight = rawTxInfo.BlockNumber
            };
            crossChainReceiveToken.MerklePath.AddRange(merklePath.Path);
            crossChainReceiveToken.TransferTransactionBytes =
                ByteString.CopyFrom(ByteArrayHelpers.FromHexString(rawTxInfo.RawTx));
            var result = chain.CrossChainReceive(rawTxInfo.FromAccount, crossChainReceiveToken);
            return result;
        }

        private CrossChainReceiveTokenInput GetCrossChainReceiveTokenInput(int fromSideChainNum, TxInfo rawTxInfo,
            int fromChainId)
        {
            var merklePath = GetMerklePath(SideChains[fromSideChainNum], rawTxInfo.BlockNumber, rawTxInfo.TxId);
            if (merklePath == null) return null;
            
            var crossChainReceiveToken = new CrossChainReceiveTokenInput
            {
                FromChainId = fromChainId,
            };
            crossChainReceiveToken.MerklePath.AddRange(merklePath.Path);

            // verify side chain transaction
            var crossChainMerkleProofContext =
                SideChains[fromSideChainNum]
                    .GetBoundParentChainHeightAndMerklePathByHeight(rawTxInfo.FromAccount, rawTxInfo.BlockNumber);
            crossChainReceiveToken.MerklePath.AddRange(crossChainMerkleProofContext.MerklePathForParentChainRoot.Path);
            crossChainReceiveToken.ParentChainHeight = crossChainMerkleProofContext.BoundParentChainHeight;
            crossChainReceiveToken.TransferTransactionBytes =
                ByteString.CopyFrom(ByteArrayHelpers.FromHexString(rawTxInfo.RawTx));

            return crossChainReceiveToken;
        }

        private MerklePath GetMerklePath(Operation chain, long blockNumber, string TxId)
        {
            var index = 0;
            var ci = new CommandInfo(ApiMethods.GetBlockByHeight) {Parameter = $"{blockNumber} true"};
            ci = chain.ApiHelper.ExecuteCommand(ci);
            var blockInfoResult = ci.InfoMsg as BlockDto;
            var transactionIds = blockInfoResult.Body.Transactions;
            var transactionStatus = new List<string>();

            foreach (var transactionId in transactionIds)
            {
                var CI = new CommandInfo(ApiMethods.GetTransactionResult) {Parameter = transactionId};
                var result = chain.ApiHelper.ExecuteCommand(CI);
                var txResult = result.InfoMsg as TransactionResultDto;
                
                var resultStatus =
                    (TransactionResultStatus) Enum.Parse(typeof(TransactionResultStatus),
                        txResult.Status, true);
                transactionStatus.Add(resultStatus.ToString());
            }

            var txIdsWithStatus = new List<Hash>();
            for (var num = 0; num < transactionIds.Count; num++)
            {
                var txId = Hash.LoadHex(transactionIds[num]);
                var txRes = transactionStatus[num];
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
            var merklePath = new MerklePath();
            merklePath.Path.AddRange(bmt.GenerateMerklePath(index));
            return merklePath;
        }

        private List<string> NewAccount(Operation chain, int count)
        {
            var accountList = new List<string>();
            for (var i = 0; i < count; i++)
            {
                var ci = new CommandInfo(ApiMethods.AccountNew) {Parameter = "123"};
                ci = chain.ApiHelper.ExecuteCommand(ci);
                Assert.IsTrue(ci.Result);
                accountList.Add(ci.InfoMsg.ToString());
            }

            return accountList;
        }

        private void UnlockAccounts(Operation chain, int count, List<string> accountList)
        {
            chain.ApiHelper.ListAccounts();
            
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
                var currentHeight = (long) ci.InfoMsg;

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

        private string ExecuteMethodWithTxId(Operation chain, string rawTx)
        {
            var ci = new CommandInfo(ApiMethods.SendTransaction)
            {
                Parameter = rawTx
            };
            chain.ApiHelper.BroadcastWithRawTx(ci);
            if (ci.Result)
            {
                var transactionOutput = ci.InfoMsg as SendTransactionOutput;

                return transactionOutput?.TransactionId;
            }

            Assert.IsTrue(ci.Result, $"Execute contract failed. Reason: {ci.GetErrorMessage()}");

            return string.Empty;
        }

        private CommandInfo CheckTransactionResult(Operation chain, string txId, int maxTimes = 60)
        {
            CommandInfo ci = null;
            var checkTimes = 1;
            while (checkTimes <= maxTimes)
            {
                ci = new CommandInfo(ApiMethods.GetTransactionResult) {Parameter = txId};
                chain.ApiHelper.GetTransactionResult(ci);
                if (ci.Result)
                {
                    if (ci.InfoMsg is TransactionResultDto transactionResult)
                    {
                        var status =
                            (TransactionResultStatus) Enum.Parse(typeof(TransactionResultStatus),
                                transactionResult.Status, true);
                        switch (status)
                        {
                            case TransactionResultStatus.Mined:
                                _logger.WriteInfo($"Transaction {txId} status: {transactionResult.Status}");
                                return ci;
                            case TransactionResultStatus.NotExisted:
                                _logger.WriteError($"Transaction {txId} status: {transactionResult.Status}");
                                return ci;
                            case TransactionResultStatus.Failed:
                            {
                                var message = $"Transaction {txId} status: {transactionResult.Status}";
                                message +=
                                    $"\r\nMethodName: {transactionResult.Transaction.MethodName}, Parameter: {transactionResult.Transaction.Params}";
                                message += $"\r\nError Message: {transactionResult.Error}";
                                _logger.WriteError(message);
                                return ci;
                            }
                        }
                    }
                }

                checkTimes++;
                Thread.Sleep(100);
            }

            var result = ci.InfoMsg as TransactionResultDto;
            _logger.WriteError(result?.Error);
            _logger.WriteError("Transaction execute status cannot be 'Mined' after one minutes.");
//            Assert.IsTrue(false, "Transaction execute status cannot be 'Mined' after one minutes.");
            return null;
        }
        
        #endregion
    }
}