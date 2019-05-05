using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using AElf.Automation.Common.Helpers;
using AElf.Automation.Common.WebApi;
using AElf.Automation.Common.WebApi.Dto;

namespace AElf.Automation.RpcPerformance
{
    public class WebOperation
    {
        public WebApiService Service { get; set; }
        public string HostUrl { get; set; }
        public List<AccountInfo> AccountList { get; set; }
        public string KeyStorePath { get; set; }
        public string TokenAbi { get; set; }
        public int BlockHeight { get; set; }
        public List<Contract> ContractList { get; set; }
        public List<string> TxIdList { get; set; }
        public int ThreadCount { get; set; }
        public int ExeTimes { get; set; }
        public ConcurrentQueue<string> ContractRpcList { get; set; }
        
        public readonly ILogHelper Logger = LogHelper.GetLogHelper();
        
        public WebOperation(int threadCount, int exeTimes, string hostUrl, string keyStorePath)
        {
            if (keyStorePath == "")
                keyStorePath = GetDefaultDataDir();
            
            AccountList = new List<AccountInfo>();
            ContractList = new List<Contract>();
            ContractRpcList = new ConcurrentQueue<string>();
            TxIdList = new List<string>();
            ThreadCount = threadCount;
            BlockHeight = 0;
            ExeTimes = exeTimes;
            KeyStorePath = keyStorePath;
            HostUrl = hostUrl;
        }
        
        private string GetDefaultDataDir()
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

        public void InitExecRpcCommand()
        {
            Logger.WriteInfo("Rpc Url: {0}", HostUrl);
            Logger.WriteInfo("Key Store Path: {0}", Path.Combine(KeyStorePath, "keys"));
            Logger.WriteInfo("Prepare new and unlock accounts.");
            
            Service = new WebApiService(HostUrl);

            //Connect Chain
            var chainStatus = Service.GetChainStatus().Result;

            //New
            NewAccounts(100);

            //Unlock Account
            UnlockAllAccounts(ThreadCount);
        }

        private void UnlockAllAccounts(int threadCount)
        {
            throw new NotImplementedException();
        }

        private void NewAccounts(int i)
        {
            throw new NotImplementedException();
        }
    }
}