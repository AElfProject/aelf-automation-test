﻿using AElf.Automation.Common.Extensions;
using AElf.Automation.Common.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using AElf.Automation.Common.Contracts;
using Newtonsoft.Json.Linq;

namespace AElf.Automation.Contracts.ScenarioTest
{
    [TestClass]
    public class VoteBpTest
    {
        public ILogHelper Logger = LogHelper.GetLogHelper();
        public string TokenAbi { get; set; }
        public List<string> UserList { get; set; }
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
            CandidatePublicKeys = new List<string>();
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
            UserList = new List<string>();
            var ci = new CommandInfo("account new", "account");
            for (int i = 0; i < 10; i++)
            {
                ci.Parameter = "123";
                ci = CH.NewAccount(ci);
                if (ci.Result)
                    UserList.Add(ci.InfoMsg?[0].Replace("Account address:", "").Trim());

                //unlock
                var uc = new CommandInfo("account unlock", "account");
                uc.Parameter = String.Format("{0} {1} {2}", UserList[i], "123", "notimeout");
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
            var initResult = tokenService.CallReadOnlyMethod(TokenMethod.BalanceOf, InitAccount);
            Console.WriteLine("InitAccount balance: " + tokenService.ConvertQueryResult(initResult, true));

            //分配资金给FullNode
            foreach (var fullAcc in FullNodeAccounts)
            {
                tokenService.CallContractWithoutResult(TokenMethod.Transfer, fullAcc, "100000");
            }
            //分配资金给BP
            foreach (var bpAcc in BpNodeAccounts)
            {
                tokenService.CallContractWithoutResult(TokenMethod.Transfer, bpAcc, "100000");
            }
            //分配资金给普通用户
            foreach (var acc in UserList)
            {
                tokenService.CallContractWithoutResult(TokenMethod.Transfer, acc, "10000");
            }
            tokenService.CheckTransactionResultList();

            //查询余额
            foreach (var bpAcc in BpNodeAccounts)
            {
                var callResult = tokenService.CallReadOnlyMethod(TokenMethod.BalanceOf, bpAcc);
                Console.WriteLine($"BP token-{bpAcc}: " + tokenService.ConvertQueryResult(callResult, true));
            }
            foreach (var fullAcc in FullNodeAccounts)
            {
                var callResult = tokenService.CallReadOnlyMethod(TokenMethod.BalanceOf, fullAcc);
                Console.WriteLine($"FullNode token-{fullAcc}: " + tokenService.ConvertQueryResult(callResult, true));
            }

            foreach (var userAcc in UserList)
            {
                var callResult = tokenService.CallReadOnlyMethod(TokenMethod.BalanceOf, userAcc);
                Console.WriteLine($"User token-{userAcc}: " + tokenService.ConvertQueryResult(callResult, true));
            }

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

            foreach (var bpAcc in BpNodeAccounts)
            {
                consensusService.Account = bpAcc;
                consensusService.CallContractWithoutResult(ConsensusMethod.AnnounceElection, "Empty");
            }
            consensusService.CheckTransactionResultList(); 

            //检查余额
            foreach (var bpAcc in BpNodeAccounts)
            {
                var callResult = tokenService.CallReadOnlyMethod(TokenMethod.BalanceOf, bpAcc);
                Console.WriteLine($"BP token-{bpAcc}: " + tokenService.ConvertQueryResult(callResult, true));
            }
            foreach (var fullAcc in FullNodeAccounts)
            {
                var callResult = tokenService.CallReadOnlyMethod(TokenMethod.BalanceOf, fullAcc);
                Console.WriteLine($"FullNode token-{fullAcc}: " + tokenService.ConvertQueryResult(callResult, true));
            }

            Logger.WriteInfo("All Full Node joined election completed.");
        }

        public void GetCandidateList()
        {
            var candidateResult = consensusService.CallContractMethod(ConsensusMethod.GetCandidatesListToFriendlyString, "Empty");
            string candidatesHex = candidateResult.JsonInfo["result"]["result"]["return"].ToString();
            string candidateStr = BaseContract.ConvertHexToString(candidatesHex);
            JObject parsed = JObject.Parse(candidateStr);
            JArray array = (JArray) parsed["Values"];
            foreach (var item in array)
            {
                CandidatePublicKeys.Add(item.Value<string>());
                Logger.WriteInfo($"Candidate: {item.Value<string>()}");
            }

            //判断是否是Candidate
            Logger.WriteInfo("IsCandidate Test");
            var candidate1Result = consensusService.CallReadOnlyMethod(ConsensusMethod.IsCandidate, CandidatePublicKeys[4]);
            Logger.WriteInfo(consensusService.ConvertQueryResult(candidate1Result,true));
            var candidate2Result = consensusService.CallReadOnlyMethod(ConsensusMethod.IsCandidate, "4ZNjzrUrNQGyWrAmxEtX7s5i4bNhXHECYHd4XK1hR9rJNFC");
            Logger.WriteInfo(consensusService.ConvertQueryResult(candidate2Result, true));
        }

        public void GetCandidateHistoryInfo()
        {
            Logger.WriteInfo("GetCandidateHistoryInfo Test");
            foreach (var pubKey in CandidatePublicKeys)
            {
                var historyResult = consensusService.CallReadOnlyMethod(ConsensusMethod.GetCandidateHistoryInfoToFriendlyString, pubKey);
                Logger.WriteInfo(consensusService.ConvertQueryResult(historyResult));
            }
        }

        public void GetGetCurrentMinersInfo()
        {
            Logger.WriteInfo("GetCurrentVictories Test");
            var minersResult = consensusService.CallReadOnlyMethod(ConsensusMethod.GetCurrentMinersToFriendlyString, "empty");
            Logger.WriteInfo(consensusService.ConvertQueryResult(minersResult));
        }

        public void GetTicketsInfo()
        {
            Logger.WriteInfo("GetTicketsInfo Test");
            foreach (var candidate in CandidatePublicKeys)
            {
                var ticketResult = consensusService.CallReadOnlyMethod(ConsensusMethod.GetTicketsInfoToFriendlyString, candidate);
                Logger.WriteInfo(consensusService.ConvertQueryResult(ticketResult));
            }
        }

        public void GetCurrentVictories()
        {
            Logger.WriteInfo("GetCurrentVictories Test");
            var victoriesResult = consensusService.CallReadOnlyMethod(ConsensusMethod.GetCurrentVictoriesToFriendlyString, "empty");
            Logger.WriteInfo(consensusService.ConvertQueryResult(victoriesResult));
        }

        public void QueryCurrentDividendsForVoters()
        {
            Logger.WriteInfo("QueryCurrentDividendsForVoters Test");
            var dividendsResult = consensusService.CallReadOnlyMethod(ConsensusMethod.QueryCurrentDividendsForVoters, "empty");
            Logger.WriteInfo(consensusService.ConvertQueryResult(dividendsResult, true));
        }

        public void QueryDividences()
        {
            foreach (var userAcc in UserList)
            {
                consensusService.Account = userAcc;
                var allDividenceResult = consensusService.CallReadOnlyMethod(ConsensusMethod.GetAllDividends, "Empty");
                consensusService.CallContractMethod(ConsensusMethod.WithdrawAll, "Empty");
            }
        }

        //参加选举
        public void VoteAction()
        {
            Random rd = new Random(DateTime.Now.Millisecond);
            foreach (var voteAcc in UserList)
            {
                string votePbk = CandidatePublicKeys[rd.Next(0, CandidatePublicKeys.Count-1)];
                string voteVolumn = rd.Next(1, 500).ToString();
                string voteLock = rd.Next(90, 1080).ToString();

                consensusService.Account = voteAcc;
                consensusService.CallContractWithoutResult(ConsensusMethod.Vote, votePbk, voteVolumn, voteLock);
            }

            consensusService.CheckTransactionResultList();
            Logger.WriteInfo("Vote completed.");
        }

        [TestMethod]
        public void VoteBP()
        {
            PrepareAccount();
            PrepareAsset();
            JoinElection();
            GetCandidateList();
            VoteAction();

            //查询信息
            GetCandidateHistoryInfo();
            GetGetCurrentMinersInfo();
            GetTicketsInfo();
            GetCurrentVictories();
            QueryDividences();
            QueryCurrentDividendsForVoters();

            //取消参选
            QuitElection();
        }

        public void QuitElection()
        {
            consensusService.Account = FullNodeAccounts[0];
            consensusService.CallContractMethod(ConsensusMethod.QuitElection, "Empty");
            consensusService.Account = FullNodeAccounts[1];
            consensusService.CallContractMethod(ConsensusMethod.QuitElection, "Empty");
            GetCandidateList();

            //查询余额
            foreach (var fullAcc in FullNodeAccounts)
            {
                var callResult = tokenService.CallReadOnlyMethod(TokenMethod.BalanceOf, fullAcc);
                Console.WriteLine($"FullNode token-{fullAcc}: " + tokenService.ConvertQueryResult(callResult, true));
            }
        }

        [TestMethod]
        public void TestNullSignature()
        {
            //Account preparation
            UserList = new List<string>();
            var ci = new CommandInfo("account new", "account");
            for (int i = 0; i < 1; i++)
            {
                ci.Parameter = "123";
                ci = CH.NewAccount(ci);
                if (ci.Result)
                    UserList.Add(ci.InfoMsg?[0].Replace("Account address:", "").Trim());

                //unlock
                var uc = new CommandInfo("account unlock", "account");
                uc.Parameter = String.Format("{0} {1} {2}", UserList[i], "123", "notimeout");
                uc = CH.UnlockAccount(uc);
            }
            consensusService = new ConsensusContract(CH, UserList[0], "ELF_hQZE5kPUVH8BtVMvKfLVMYeNRYE1xB2RzQVn1E5j5zwb9t");
            consensusService.CallContractMethod(ConsensusMethod.GetCurrentTermNumber);
        }
    }
}
