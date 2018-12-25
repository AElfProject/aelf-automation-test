using AElf.Automation.Common.Extensions;
using AElf.Automation.Common.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using AElf.Automation.Common.Contracts;

namespace AElf.Automation.Contracts.ScenarioTest
{
    [TestClass]
    public class VoteBpOnlyTest
    {
        public ILogHelper Logger = LogHelper.GetLogHelper();
        public string TokenAbi { get; set; }
        public string ConsensusAbi { get; set; }
        public string DividendsAbi { get; set; }

        public List<string> UserList { get; set; }
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
            ConsensusAbi = ci.JsonInfo["AElf.Contracts.Consensus"].ToObject<string>();
            DividendsAbi = ci.JsonInfo["AElf.Contracts.Dividends"].ToObject<string>();

            //Load default Contract Abi
            ci = new CommandInfo("load_contract_abi");
            CH.RpcLoadContractAbi(ci);
            Assert.IsTrue(ci.Result, "Load contract abi got exception.");

            //Account
            //Unlock init account
            var initAcc = new CommandInfo("account unlock", "account");
            initAcc.Parameter = String.Format("{0} {1} {2}", InitAccount, "123", "notimeout");
            initAcc = CH.UnlockAccount(initAcc);

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

            //Init service
            tokenService = new TokenContract(CH, InitAccount, TokenAbi);
            consensusService = new ConsensusContract(CH, InitAccount, ConsensusAbi);
            dividendsService = new DividendsContract(CH, InitAccount, DividendsAbi);
        }

        public void PrepareCandidateAsset()
        {
            tokenService.CallContractMethod(TokenMethod.Initialize, "elfToken", "ELF", "800000000", "2");
            //查询剩余余额
            var initResult = tokenService.CallReadOnlyMethod(TokenMethod.BalanceOf, InitAccount);
            Console.WriteLine("InitAccount balance: " + tokenService.ConvertViewResult(initResult, true));

            //分配资金给Dividends
            tokenService.CallContractWithoutResult(TokenMethod.Transfer, DividendsAbi, "1000000");

            //分配资金给BP
            foreach (var bpAcc in BpNodeAccounts)
            {
                tokenService.CallContractWithoutResult(TokenMethod.Transfer, bpAcc, "100000");
            }

            tokenService.CheckTransactionResultList();

            //查询余额
            foreach (var bpAcc in BpNodeAccounts)
            {
                var callResult = tokenService.CallReadOnlyMethod(TokenMethod.BalanceOf, bpAcc);
                Console.WriteLine($"BP token-{bpAcc}: " + tokenService.ConvertViewResult(callResult, true));
            }

            Logger.WriteInfo("All accounts asset prepared completed.");
        }

        public void PrepareUserAccount()
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

            //分配资金给普通用户
            foreach (var acc in UserList)
            {
                tokenService.CallContractWithoutResult(TokenMethod.Transfer, acc, "10000");
            }
            tokenService.CheckTransactionResultList();

            foreach (var userAcc in UserList)
            {
                var callResult = tokenService.CallReadOnlyMethod(TokenMethod.BalanceOf, userAcc);
                Console.WriteLine($"User token-{userAcc}: " + tokenService.ConvertViewResult(callResult, true));
            }

            Logger.WriteInfo("All accounts created and unlocked.");
        }

        public void JoinElection()
        {
            //参加选举
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
                Console.WriteLine($"BP token-{bpAcc}: " + tokenService.ConvertViewResult(callResult, true));
            }

            Logger.WriteInfo("All Full Node joined election completed.");
        }

        public void GetCandidateList()
        {
            var candidateResult = consensusService.CallReadOnlyMethod(ConsensusMethod.GetCandidatesListToFriendlyString, "Empty");
            string candidatesJson = consensusService.ConvertViewResult(candidateResult);
            bool result = DataHelper.TryGetArrayFromJson(out var pubkeyList, candidatesJson, "Values");
            CandidatePublicKeys = pubkeyList;
            foreach (var item in CandidatePublicKeys)
            {
                Logger.WriteInfo($"Candidate: {item}");
            }

            //判断是否是Candidate
            Logger.WriteInfo("IsCandidate Test");
            var candidate1Result = consensusService.CallReadOnlyMethod(ConsensusMethod.IsCandidate, CandidatePublicKeys[1]);
            Logger.WriteInfo(consensusService.ConvertViewResult(candidate1Result,true));
            var candidate2Result = consensusService.CallReadOnlyMethod(ConsensusMethod.IsCandidate, "4ZNjzrUrNQGyWrAmxEtX7s5i4bNhXHECYHd4XK1hR9rJNFC");
            Logger.WriteInfo(consensusService.ConvertViewResult(candidate2Result, true));
        }

        public void GetCandidateHistoryInfo()
        {
            Logger.WriteInfo("GetCandidateHistoryInfo Test");
            foreach (var pubKey in CandidatePublicKeys)
            {
                var historyResult = consensusService.CallReadOnlyMethod(ConsensusMethod.GetCandidateHistoryInfoToFriendlyString, pubKey);
                Logger.WriteInfo(consensusService.ConvertViewResult(historyResult));
            }
        }

        public void GetGetCurrentMinersInfo()
        {
            Logger.WriteInfo("GetCurrentVictories Test");
            var minersResult = consensusService.CallReadOnlyMethod(ConsensusMethod.GetCurrentMinersToFriendlyString, "empty");
            Logger.WriteInfo(consensusService.ConvertViewResult(minersResult));
        }

        public void GetTicketsInfo()
        {
            Logger.WriteInfo("GetTicketsInfo Test");
            foreach (var candidate in CandidatePublicKeys)
            {
                var ticketResult = consensusService.CallReadOnlyMethod(ConsensusMethod.GetTicketsInfoToFriendlyString, candidate);
                Logger.WriteInfo(consensusService.ConvertViewResult(ticketResult));
            }
        }

        public void GetCurrentElectionInfo()
        {
            Logger.WriteInfo("GetCurrentElectionInfo Test");
            var currentElectionResult = consensusService.CallReadOnlyMethod(ConsensusMethod.GetCurrentElectionInfoToFriendlyString, "0", "0", "0");
            Logger.WriteInfo(consensusService.ConvertViewResult(currentElectionResult));
        }

        public void GetCurrentVictories()
        {
            Logger.WriteInfo("GetCurrentVictories Test");
            var victoriesResult = consensusService.CallReadOnlyMethod(ConsensusMethod.GetCurrentVictoriesToFriendlyString, "empty");
            Logger.WriteInfo(consensusService.ConvertViewResult(victoriesResult));
        }

        public void QueryCurrentDividendsForVoters()
        {
            Logger.WriteInfo("QueryCurrentDividendsForVoters Test");
            var dividendsResult = consensusService.CallReadOnlyMethod(ConsensusMethod.QueryCurrentDividendsForVoters, "empty");
            Logger.WriteInfo(consensusService.ConvertViewResult(dividendsResult, true));
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
        public void UserVoteAction()
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
            //检查投票结果
            GetCurrentElectionInfo();
            Logger.WriteInfo("Vote completed.");
        }

        //退出选举
        public void QuitElection()
        {
            consensusService.Account = BpNodeAccounts[0];
            consensusService.CallContractMethod(ConsensusMethod.QuitElection, "Empty");
            consensusService.Account = BpNodeAccounts[1];
            consensusService.CallContractMethod(ConsensusMethod.QuitElection, "Empty");
            consensusService.Account = BpNodeAccounts[2];
            consensusService.CallContractMethod(ConsensusMethod.QuitElection, "Empty");
            GetCandidateList();

            //查询余额
            foreach (var fullAcc in BpNodeAccounts)
            {
                var callResult = tokenService.CallReadOnlyMethod(TokenMethod.BalanceOf, fullAcc);
                Console.WriteLine($"BpNode token-{fullAcc}: " + tokenService.ConvertViewResult(callResult, true));
            }
        }

        [TestMethod]
        public void VoteBP()
        {
            PrepareCandidateAsset();
            JoinElection();
            GetCandidateList();

            PrepareUserAccount();
            UserVoteAction();

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

        [TestMethod]
        public void JoinElectionTest()
        {
            PrepareCandidateAsset();
            JoinElection();
            GetCandidateList();
        }

        [TestMethod]
        public void QuiteElectionTest()
        {
            QuiteElectionTest();
            GetCandidateList();
        }

        [TestMethod]
        public void VoteBpTest()
        {
            GetCandidateList();
            PrepareUserAccount();
            UserVoteAction();
            GetTicketsInfo();
            GetCurrentVictories();
        }

        [TestMethod]
        public void QueryInformationTest()
        {
            GetCandidateList();

            GetCandidateHistoryInfo();

            GetCurrentElectionInfo();

            GetCurrentVictories();

            GetTicketsInfo();
        }
    }
}