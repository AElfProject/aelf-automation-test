using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using AElf.Automation.Common.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AElf.Automation.SideChainVerification
{
    public struct IndexItem
    {
        public string ChainId { get; set; }
        public int Height { get; set; }
    }

    public struct MerkleItem
    {
        public string TxId { get; set; }
        public string MPath { get; set; }
        public int PHeight { get; set; }
    }

    public class VerifyResult
    {
        public string NodeName { get; set; }
        public int Height { get; set; }
        public string Result { get; set; }
        public List<string> TxList { get; set; }
        public ConcurrentQueue<string> VerifyTxList { get; set; }
        public List<string> PassList { get; set; }
        public List<string> FailList { get; set; }

        public VerifyResult(string nodeName, int height)
        {
            NodeName = nodeName;
            Height = height;
            Result = "Not Verify";
            TxList = new List<string>();
            VerifyTxList = new ConcurrentQueue<string>();
            PassList = new List<string>();
            FailList = new List<string>();
        }
    }

    public class SideChain
    {
        private readonly WebApiHelper _ch;
        private readonly string _chainName;
        private string _account;
        private string _sideChainTxId;
        private string _chainId;

        public ConcurrentQueue<VerifyResult> VerifyResultList;
        public List<CancellationTokenSource> CancellationList;
        public CancellationTokenSource CtsLinkSource;
        public readonly ILogHelper Logger = LogHelper.GetLogHelper();

        public SideChain(string rpcUrl, string chainName)
        {
            var rpcUrl1 = rpcUrl.Contains("chain") ? rpcUrl : $"{rpcUrl}/chain";
            var keyStorePath = GetDefaultDataDir();
            _chainName = chainName;
            _ch = new WebApiHelper(rpcUrl1, keyStorePath);
            //connection chain
            var ci = new CommandInfo(ApiMethods.GetChainInformation);
            _ch.GetChainInformation(ci);
            VerifyResultList = new ConcurrentQueue<VerifyResult>();
            CancellationList = new List<CancellationTokenSource>();

            InitVerifyAccount();
            GetSideChainTxId();
        }

        public void GetSideChainTxId()
        {
            //Connect Chain
            var ci = new CommandInfo(ApiMethods.GetChainInformation);
            _ch.ExecuteCommand(ci);
            Assert.IsTrue(ci.Result, "Connect chain got exception.");
            ci.GetJsonInfo();
            _sideChainTxId = ci.JsonInfo["AElf.Contracts.CrossChain"].ToString();
            _chainId = ci.JsonInfo["chain_id"].ToString();
        }

        public int GetCurrentHeight()
        {
            var ci = new CommandInfo(ApiMethods.GetBlockHeight);
            _ch.ExecuteCommand(ci);
            Assert.IsTrue(ci.Result, "Query current height got exception.");
            ci.GetJsonInfo();
            return Int32.Parse(ci.JsonInfo["result"].ToString());
        }

        public List<IndexItem> GetIndexBlockInfo(int height)
        {
            List<IndexItem> reList = new List<IndexItem>();

            var ci = new CommandInfo(ApiMethods.GetBlockInfo);
            ci.Parameter = $"{height} {false}";
            _ch.ExecuteCommand(ci);
            Assert.IsTrue(ci.Result, "Query block information got exception.");
            ci.GetJsonInfo();
            var indexSideInfo = ci.JsonInfo["result"]["Body"]["IndexedSideChainBlcokInfo"];
            if (indexSideInfo.Children().Count() != 0)
            {
                //continue with sidechain verification
                var indexs = indexSideInfo.ToArray();
                foreach (var item in indexs)
                {
                    string chainId = item.Path.Replace("result.result.Body.IndexedSideChainBlcokInfo.", "");
                    int indexHeight = indexSideInfo[chainId].Value<Int32>("Height");
                    IndexItem index = new IndexItem() {ChainId = chainId, Height = indexHeight};
                    reList.Add(index);
                }
            }

            return reList;
        }

        public List<string> GetBlockTransactions(int height)
        {
            List<string> trList = new List<string>();

            var ci = new CommandInfo(ApiMethods.GetBlockInfo);
            ci.Parameter = $"{height} {true}";
            _ch.GetBlockByHeight(ci);
            Assert.IsTrue(ci.Result, "Query block information got exception.");
            ci.GetJsonInfo();
            var transactions = ci.JsonInfo["result"]["Body"]["Transactions"].ToArray();
            foreach (var item in transactions)
            {
                trList.Add(item.ToString());
            }

            return trList;
        }

        public MerkleItem GetMerkelPath(string txId)
        {
            MerkleItem merkle = new MerkleItem();
            merkle.TxId = txId;
            var ci = new CommandInfo(ApiMethods.GetMerklePath);
            ci.Parameter = txId;
            //_ch.GetMerklePath(ci);
            Assert.IsTrue(ci.Result, "Get merkel path got exception.");
            ci.GetJsonInfo();
            if (ci.JsonInfo["result"]["error"] != null)
                return new MerkleItem();

            merkle.MPath = ci.JsonInfo["result"]["merkle_path"].ToString();
            merkle.PHeight = ci.JsonInfo["result"].Value<Int32>("parent_height");

            return merkle;
        }

        public void PostVeriyTransaction(IndexItem item)
        {
            if (item.ChainId == _chainId.Substring(2))
                return;
            //Query merkle path collection
            var trans = GetBlockTransactions(item.Height);
            if (trans.Count == 0)
                return;
            List<MerkleItem> merkleList = new List<MerkleItem>();
            foreach (var tran in trans)
            {
                var merkle = GetMerkelPath(tran);
                if (merkle.MPath != null)
                    merkleList.Add(merkle);
            }

            //Gen transaction
            List<string> rawTxList = new List<string>();
            foreach (var merkle in merkleList)
            {
                rawTxList.Add(GenVerifyTransactionInfo(merkle));
            }

            //Post verification
            var ci = new CommandInfo(ApiMethods.SendTransactions);
            foreach (var rawTx in rawTxList)
            {
                ci.Parameter += "," + rawTx;
            }

            ci.Parameter = ci.Parameter.Substring(1);
            _ch.ExecuteCommand(ci);
            Assert.IsTrue(ci.Result, "Execute transactions got exception.");
            var result = ci.InfoMsg.ToString().Replace("[", "").Replace("]", "").Replace("\"", "").Replace("\n", "")
                .Split(",");
            ConcurrentQueue<string> txResList = new ConcurrentQueue<string>();
            foreach (var txHash in result)
            {
                txResList.Enqueue(txHash.Trim());
            }

            //Add verify Result
            VerifyResult vr = new VerifyResult(_chainName, item.Height);
            vr.TxList = trans.ToList();
            vr.VerifyTxList = txResList;
            VerifyResultList.Enqueue(vr);
        }

        public void StartCheckVerifyResultTasks(int thredCount)
        {
            for (int i = 0; i < thredCount; i++)
            {
                var cts = new CancellationTokenSource();
                var i1 = i;
                cts.Token.Register(() => Logger.WriteInfo("Cancle check transaction result task: {0}", i1));
                CancellationList.Add(cts);
                ThreadPool.QueueUserWorkItem(o => CheckVerifyTransactionResult(cts.Token, null));
            }

            CtsLinkSource =
                CancellationTokenSource.CreateLinkedTokenSource(CancellationList.Select(o => o.Token).ToArray());
        }

        public void StopCheckVerifyResultTasks()
        {
            Logger.WriteInfo("Close all transaction verify tasks.");
            try
            {
                CtsLinkSource.Cancel();
            }
            catch (ObjectDisposedException ode)
            {
                Logger.WriteError(ode.Message);
            }

            Logger.WriteInfo("Transaction result check summary:");
            foreach (var verifyItem in VerifyResultList)
            {
                if (verifyItem.Result == "Passed" || verifyItem.Result == "Not Verify")
                    continue;
                Logger.WriteError(
                    $"Chain:{verifyItem.NodeName}, Height:{verifyItem.Height}, TxCount:{verifyItem.TxList.Count}, Result: {verifyItem.Result}");
            }
        }

        public void CheckVerifyTransactionResult(CancellationToken token, object state)
        {
            while (true)
            {
                if (token.IsCancellationRequested)
                    break;

                if (!VerifyResultList.TryDequeue(out var vr))
                {
                    Thread.Sleep(5000);
                    continue;
                }

                int txLength = vr.VerifyTxList.Count;
                int checkTimes = 0;
                while (true)
                {
                    if (txLength == vr.VerifyTxList.Count && checkTimes == 5)
                    {
                        Logger.WriteWarn("Chain={0} at Height: {1} failed due to transaction always pending.",
                            vr.NodeName, vr.Height);
                        vr.Result = "Pending";
                        break;
                    }

                    if (txLength == vr.VerifyTxList.Count && checkTimes != 5)
                    {
                        checkTimes++;
                        Thread.Sleep(2000);
                    }
                    else
                    {
                        checkTimes = 0;
                    }

                    if (!vr.VerifyTxList.TryDequeue(out var txId))
                    {
                        Logger.WriteInfo("Chain={0}, Height={1} passed verification.", _chainName, vr.Height);
                        vr.Result = "Passed";
                        Console.WriteLine();
                        break;
                    }

                    var ci = new CommandInfo(ApiMethods.GetTransactionResult);
                    ci.Parameter = txId;
                    _ch.GetTransactionResult(ci);
                    if (ci.Result)
                    {
                        ci.GetJsonInfo();
                        string deployResult = ci.JsonInfo["result"]["Status"].ToString();

                        if (deployResult == "Pending")
                        {
                            Logger.WriteWarn("TxId: {0}, Status: Pendig", txId);
                            vr.VerifyTxList.Enqueue(txId);
                            Thread.Sleep(500);
                        }
                        else if (deployResult == "Mined")
                        {
                            string returnValue = ci.JsonInfo["result"]["return"].ToString();
                            if (returnValue != "01")
                            {
                                Logger.WriteInfo(ci.InfoMsg.ToString());
                                Assert.IsTrue(false,
                                    $"Verification failed with transaction with chain: {vr.NodeName} at height: {vr.Height}");
                            }
                        }
                    }
                    else
                    {
                        //Handle request failed scenario
                        vr.VerifyTxList.Enqueue(txId);
                        Thread.Sleep(500);
                    }
                }
            }
        }

        public string GenVerifyTransactionInfo(MerkleItem merkle)
        {
            string parameterinfo = "{\"from\":\"" + _account +
                                   "\",\"to\":\"" + _sideChainTxId +
                                   "\",\"method\":\"VerifyTransaction\",\"incr\":\"" +
                                   GetCurrentTimeStamp() + "\",\"params\":[\"" + merkle.TxId + "\",\"" + merkle.MPath +
                                   "\",\"" + merkle.PHeight + "\"]}";
            var ci = new CommandInfo(ApiMethods.SendTransaction);
            ci.Parameter = parameterinfo;
            string requestInfo = _ch.GenerateTransactionRawTx(ci);

            return requestInfo;
        }

        private void InitVerifyAccount()
        {
            //New
            var ci = new CommandInfo(ApiMethods.AccountNew);
            ci.Parameter = "123";
            ci = _ch.ExecuteCommand(ci);
            Assert.IsTrue(ci.Result, "Create account got exception.");
            _account = ci.InfoMsg?.ToString();

            //Unlock
            ci = new CommandInfo(ApiMethods.AccountUnlock);
            ci.Parameter = String.Format("{0} {1} {2}", _account, "123", "notimeout");
            ci = _ch.ExecuteCommand(ci);
            Assert.IsTrue(ci.Result, "Unlock account got exception.");
        }

        private string GetDefaultDataDir()
        {
            try
            {
                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "aelf");
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);

                string keyPath = Path.Combine(path, "keys");
                if (!Directory.Exists(keyPath))
                    Directory.CreateDirectory(keyPath);

                return path;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private string GetCurrentTimeStamp()
        {
            return DateTime.Now.ToString("MMddHHmmss") + DateTime.Now.Millisecond.ToString();
        }
    }
}