using AElf.Automation.Common.Extensions;
using AElf.Automation.Common.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using AElf.Automation.Common.Contracts;

namespace AElf.Automation.Contracts.ScenarioTest
{
    [TestClass]
    public class VoteBpTest
    {
        public ILogHelper Logger = LogHelper.GetLogHelper();
        public string TokenAbi { get; set; }
        public List<string> AccList { get; set; }
        public List<string> FullNodeAccounts { get; set; }
        public List<string> BpNodeAccounts { get; set; }
        public List<string> CandidatePublicKeys { get; set; }
        public string InitAccount { get; } = "ELF_2GkD1q74HwBrFsHufmnCKHJvaGVBYkmYcdG3uebEsAWSspX";

        //Contract service List
        public static TokenContract tokenService { get; set; }
        public static ConsensusContract consensusService { get; set; }
        public static DividendsContract dividendsService { get; set; }

        public string RpcUrl { get; } = "http://192.168.197.20:8010/chain";
        public CliHelper CH { get; set; }

        [TestInitialize]
        public void Initlize()
        {
            //Init log
            string logName = "VoteBP_" + DateTime.Now.ToString("MMddHHmmss") + ".log";
            string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", logName);
            Logger.InitLogHelper(dir);

            CH = new CliHelper(RpcUrl, AccountManager.GetDefaultDataDir());
            
            //Connect Chain
            var ci = new CommandInfo("connect_chain");
            CH.RpcConnectChain(ci);
            Assert.IsTrue(ci.Result, "Connect chain got exception.");

            //Get AElf.Contracts.Token ABI
            ci.GetJsonInfo();
            TokenAbi = ci.JsonInfo["AElf.Contracts.Token"].ToObject<string>();

            //Load default Contract Abi
            ci = new CommandInfo("load_contract_abi");
            CH.RpcLoadContractAbi(ci);
            Assert.IsTrue(ci.Result, "Load contract abi got exception.");
        }

        public void PrepareAccount()
        {
            //Account preparation
            AccList = new List<string>();
            var ci = new CommandInfo("account new", "account");
            for (int i = 0; i < 10; i++)
            {
                ci.Parameter = "123";
                ci = CH.NewAccount(ci);
                if (ci.Result)
                    AccList.Add(ci.InfoMsg?[0].Replace("Account address:", "").Trim());

                //unlock
                var uc = new CommandInfo("account unlock", "account");
                uc.Parameter = String.Format("{0} {1} {2}", AccList[i], "123", "notimeout");
                uc = CH.UnlockAccount(uc);
            }

            //Unlock init account
            var initAcc = new CommandInfo("account unlock", "account");
            initAcc.Parameter = String.Format("{0} {1} {2}", InitAccount, "123", "notimeout");
            initAcc = CH.UnlockAccount(initAcc);

            //Get FullNode Info
            FullNodeAccounts = new List<string>();
            FullNodeAccounts.Add("ELF_26cUpeiNb6q4DdFFEXTiPgWcifxtwEMsqshKHWGeYxaJkT1");
            FullNodeAccounts.Add("ELF_2AQdMyH4bo6KKK7Wt5fN7LerrWdoPUYTcJHyZNKXqoD2a4V");
            FullNodeAccounts.Add("ELF_3FWgHoNdR92rCSEbYqzD6ojCCmVKEpoPt87tpmwWYAkYm6d");
            FullNodeAccounts.Add("ELF_4ZNjzrUrNQGyWrAmxEtX7s5i4bNhXHECYHd4XK1hR9rJNFC");
            FullNodeAccounts.Add("ELF_6Fp72su6EPmHkEiojK1FyP7DsMHm16MygkG93zyqSbnE84v");
            foreach (var fullAcc in FullNodeAccounts)
            {
                //unlock
                var uc = new CommandInfo("account unlock", "account");
                uc.Parameter = String.Format("{0} {1} {2}", fullAcc, "123", "notimeout");
                uc = CH.UnlockAccount(uc);
            }

            //Get BpNode Info
            BpNodeAccounts = new List<string>();
            BpNodeAccounts.Add("ELF_2jLARdoRyaQ2m8W5s1Fnw7EgyvEr9SX9SgNhejzcwfBKkvy");
            BpNodeAccounts.Add("ELF_2VsBNkgc9ZVkr6wQoNY7FjnPooMJscS9SLNQ5jDFtuSEKud");
            BpNodeAccounts.Add("ELF_3YcUM4EjAcUYyZxsNb7KHPfXdnYdwKmwr9g3p2eipBE6Wym");
            BpNodeAccounts.Add("ELF_59w62zTynBKyQg5Pi4uNTz29QF7M1SHazN71g6pG5N25wY1");
            BpNodeAccounts.Add("ELF_5tqoweoWNrCRKG8Z28LM63B4aiuBjhZwy6JYw57iqcDqgN6");
            foreach (var bpAcc in BpNodeAccounts)
            {
                //unlock
                var uc = new CommandInfo("account unlock", "account");
                uc.Parameter = String.Format("{0} {1} {2}", bpAcc, "123", "notimeout");
                uc = CH.UnlockAccount(uc);
            }
            Logger.WriteInfo("All accounts created and unlocked.");
        }

        public void PrepareAsset()
        {
            tokenService = new TokenContract(CH, InitAccount, TokenAbi);
            tokenService.CallContractMethod(TokenMethod.Initialize, "elfToken", "ELF", "800000000", "2");
            //查询剩余余额
            var initResult = tokenService.CallContractMethod(TokenMethod.BalanceOf, InitAccount);
            Console.WriteLine("InitAccount balance: " + tokenService.GetValueFromHex(initResult.JsonInfo));
            //分配资金给全节点用户
            foreach (var fullAcc in FullNodeAccounts)
            {
                tokenService.CallContractWithoutResult(TokenMethod.Transfer, fullAcc, "100000");
            }
            //查询余额
            foreach (var fullAcc in FullNodeAccounts)
            {
                var txResult = tokenService.CallContractMethod(TokenMethod.BalanceOf, fullAcc);
                Console.WriteLine("Balance value: " + tokenService.GetValueFromHex(txResult.JsonInfo));
            }

            //分配资金给普通用户
            foreach (var acc in AccList)
            {
                tokenService.CallContractWithoutResult(TokenMethod.Transfer, acc, "10000");
            }
            tokenService.CheckTransactionResultList();
            Logger.WriteInfo("All accounts asset prepared completed.");
        }

        public void JoinElection()
        {
            consensusService = new ConsensusContract(CH, InitAccount);
            //参加选举
            foreach (var fullAcc in FullNodeAccounts)
            {
                consensusService.Account = fullAcc;
                consensusService.CallContractWithoutResult(ConsensusMethod.AnnounceElection, "Empty");
            }
            consensusService.CheckTransactionResultList(); 

            //检查是否扣除Token
            foreach (var fullAcc in FullNodeAccounts)
            {
                var txResult = tokenService.CallContractMethod(TokenMethod.BalanceOf, fullAcc);
                Console.WriteLine("Balance value: " + tokenService.GetValueFromHex(txResult.JsonInfo));
            }

            Logger.WriteInfo("All Full Node joined election completed.");
        }

        public void GetCandidateInfo()
        {
            var blockchainAge = consensusService.CallContractMethod(ConsensusMethod.GetBlockchainAge, "Empty");
            var currentMiners = consensusService.CallContractMethod(ConsensusMethod.GetCurrentMinersToFriendlyString, "Empty");
            var candidateResult = consensusService.CallContractMethod(ConsensusMethod.GetCandidatesListToFriendlyString, "Empty");
            var candidateHistoryResult = consensusService.CallContractMethod(ConsensusMethod.GetCandidateHistoryInfoToFriendlyString, "Empty");
            var ticketsInfo = consensusService.CallContractMethod(ConsensusMethod.GetTicketsInfoToFriendlyString, "Empty");
        }

        public void VoteAction()
        {
            Random rd = new Random(DateTime.Now.Millisecond);
            foreach (var voteAcc in AccList)
            {
                string votePbk = CandidatePublicKeys[rd.Next(0, CandidatePublicKeys.Count-1)];
                string voteVolumn = rd.Next(1, 500).ToString();
                string voteLock = rd.Next(90, 1080).ToString();
                consensusService.CallContractMethod(ConsensusMethod.Vote, votePbk, voteVolumn, voteLock);
            }
            Logger.WriteInfo("Vote completed.");
        }

        [TestMethod]
        public void VoteBP()
        {
            PrepareAccount();
            PrepareAsset();
            JoinElection();
            GetCandidateInfo();
            VoteAction();
        }
        public void QueryDividences(int round)
        {

        }

        public void QueryCandidatePerformance(string pubKey)
        {

        }

        public void QueryVoteResult()
        {

        }

        public void QueryUserDividence()
        {

        }

        public void QueryCandidateVoteInformation()
        {

        }
    }
}
