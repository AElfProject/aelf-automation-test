using System;
using System.Collections.Generic;
using System.IO;
using AElf.Automation.Common.Contracts;
using AElf.Automation.Common.Extensions;
using AElf.Automation.Common.Helpers;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AElf.Automation.ContractsTesting
{
    class Program
    {
        #region Private Properties
        private static readonly ILogHelper Logger = LogHelper.GetLogHelper();
        private string TokenAbi { get; set; }
        private string ConsesusAbi { get; set; }
        private List<string> Users { get; set; }
        private string FeeAccount { get; } = "ELF_4PAjijP5gDrWebRdjLBgJT6nyjdb3F2sZPEmBZzsbeA7i4s";
        #endregion

        #region Parameter Option

        [Option("-ba|--bp.accoount", Description = "Bp account info")]
        public string BpAccount { get; set; } = "ELF_3SMq6XUt2ogboq3fTXwKF6bs3zt9f3EBqsMfDpVzvaX4U4K";

        [Option("-bp|--bp.password", Description = "Bp account password info")]
        public string BpPassword { get; set; } = "123";

        [Option("-e|--endpoint", Description = "Node service endpoint info")]
        public string Endpoint { get; set; } = "http://192.168.197.43:8000/chain";

        #endregion

        public static int Main(string[] args)
        {
            try
            {
                return CommandLineApplication.Execute<Program>(args);
            }
            catch (AssertFailedException ex)
            {
                Logger.WriteError($"Execute failed: {ex.Message}");
            }

            return 0;
        }

        private void OnExecute()
        {
            #region Basic Preparation
            //Init Logger
            string logName = "ContractTest_" + DateTime.Now.ToString("MMddHHmmss") + ".log";
            string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", logName);
            Logger.InitLogHelper(dir);

            var ch = new CliHelper(Endpoint, AccountManager.GetDefaultDataDir());

            //Connect Chain
            var ci = new CommandInfo("ConnectChain");
            ch.ExecuteCommand(ci);
            Assert.IsTrue(ci.Result, "Connect chain got exception.");

            //Get AElf.Contracts.Token ABI
            ci.GetJsonInfo();
            TokenAbi = ci.JsonInfo["AElf.Contracts.Token"].ToObject<string>();
            ConsesusAbi = ci.JsonInfo["AElf.Contracts.Consensus"].ToObject<string>();

            //Load default Contract Abi
            ci = new CommandInfo("LoadContractAbi");
            ch.RpcLoadContractAbi(ci);
            Assert.IsTrue(ci.Result, "Load contract abi got exception.");

            //Account preparation
            Users = new List<string>();

            for (int i = 0; i < 5; i++)
            {
                ci = new CommandInfo("account new", "account") {Parameter = "123"};
                ci = ch.NewAccount(ci);
                if(ci.Result)
                    Users.Add(ci.InfoMsg?[0].Replace("Account address:", "").Trim());

                //unlock
                var uc = new CommandInfo("account unlock", "account");
                uc.Parameter = string.Format("{0} {1} {2}", Users[i], "123", "notimeout");
                ch.UnlockAccount(uc);
            }
            #endregion

            #region AElf.Token operation
            //Deploy and Load ABI
            var tokenContract = new TokenContract(ch, BpAccount, TokenAbi);
            //Set token fee
            tokenContract.CallContractMethod(TokenMethod.SetFeePoolAddress, FeeAccount);

            var consesusContract = new ConsensusContract(ch, BpAccount, ConsesusAbi);
            consesusContract.CallContractMethod(ConsensusMethod.InitialBalance, BpAccount, "100000");

            //Approve Test
            tokenContract.SetAccount(Users[1]);
            tokenContract.CallContractMethod(TokenMethod.Approve, Users[1], "1000");

            //Transfer to Account A, B, C
            tokenContract.CallContractWithoutResult(TokenMethod.Transfer, Users[1], "5000");
            tokenContract.CallContractWithoutResult(TokenMethod.Transfer, Users[2], "10000");
            tokenContract.CallContractWithoutResult(TokenMethod.Transfer, Users[3], "15000");

            tokenContract.CheckTransactionResultList();

            //Get balance
            var txOwner = tokenContract.CallReadOnlyMethod(TokenMethod.BalanceOf, Users[0]);
            var txBa = tokenContract.CallReadOnlyMethod(TokenMethod.BalanceOf, Users[1]);
            var txBb = tokenContract.CallReadOnlyMethod(TokenMethod.BalanceOf, Users[2]);
            var txBc = tokenContract.CallReadOnlyMethod(TokenMethod.BalanceOf, Users[3]);

            //Convert to Value
            Logger.WriteInfo($"Owner current balance: {tokenContract.ConvertViewResult(txOwner, true)}");

            Logger.WriteInfo($"A current balance: {tokenContract.ConvertViewResult(txBa, true)}");

            Logger.WriteInfo($"B current balance: {tokenContract.ConvertViewResult(txBb, true)}");

            Logger.WriteInfo($"C current balance: {tokenContract.ConvertViewResult(txBc, true)}");

            #endregion

            #region AElf.Contract.Resource
            var resourceContract = new ResourceContract(ch, Users[0]);

            resourceContract.CallContractMethod(ResourceMethod.Initialize, tokenContract.ContractAbi, Users[0], Users[0]);

            resourceContract.CallContractMethod(ResourceMethod.IssueResource, "CPU", "1000000");
            resourceContract.CallContractMethod(ResourceMethod.IssueResource, "Ram", "1000000");
            resourceContract.CallContractMethod(ResourceMethod.IssueResource, "Net", "1000000");
            
            //Buy resource
            resourceContract.Account = Users[1];
            resourceContract.CallContractMethod(ResourceMethod.BuyResource, "Cpu", "1000");
            resourceContract.CallContractMethod(ResourceMethod.BuyResource, "Cpu", "6000");
            resourceContract.CallContractMethod(ResourceMethod.BuyResource, "Ram", "10000");

            //Account 4 have no money
            resourceContract.SetAccount(Users[4]);
            resourceContract.CallContractMethod(ResourceMethod.BuyResource, "Net", "1000");
            resourceContract.CallContractMethod(ResourceMethod.BuyResource, "NET", "10000");

            //Query user resource
            resourceContract.CallReadOnlyMethod(ResourceMethod.GetUserBalance, Users[1], "Cpu");
            resourceContract.CallReadOnlyMethod(ResourceMethod.GetUserBalance, Users[4], "Cpu");
            resourceContract.CallReadOnlyMethod(ResourceMethod.GetUserBalance, Users[4], "Net");

            //Query user token
            tokenContract.ExecuteContractMethod("BalanceOf", Users[0]);

            //Sell resource
            resourceContract.SetAccount(Users[1]);
            resourceContract.CallContractMethod(ResourceMethod.SellResource, "CPU", "100");
            resourceContract.CallContractMethod(ResourceMethod.SellResource, "cpu", "500");
            resourceContract.CallContractMethod(ResourceMethod.SellResource, "Cpu", "1000");

            resourceContract.SetAccount(Users[4]);
            resourceContract.CallContractMethod(ResourceMethod.SellResource, "Ram", "100");
            resourceContract.CallContractMethod(ResourceMethod.SellResource, "Ram", "500");
            resourceContract.CallContractMethod(ResourceMethod.GetUserBalance, Users[0], "Ram");
            resourceContract.CallContractMethod(ResourceMethod.SellResource, "Ram", "1000");

            #endregion
        }
    }
}
