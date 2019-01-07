using AElf.Automation.Common.Extensions;
using AElf.Automation.Common.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using AElf.Automation.Common.Contracts;


namespace AElf.Automation.Contracts.ScenarioTest
{
    [TestClass]
    public class VoteFeatureTest
    {
        #region Priority
        public ILogHelper Logger = LogHelper.GetLogHelper();
        public string TokenAbi { get; set; }
        public string ConsensusAbi { get; set; }
        public string DividendsAbi { get; set; }

        public List<string> UserList { get; set; }
        public List<string> FullNodeAccounts { get; set; }
        public List<string> BpNodeAccounts { get; set; }
        public List<string> CandidatePublicKeys { get; set; }
        public List<string> CurrentMinersKeys { get; set; }
        public string InitAccount { get; } = "ELF_2GkD1q74HwBrFsHufmnCKHJvaGVBYkmYcdG3uebEsAWSspX";
        public string FeeAccount { get; } = "ELF_1dVay78LmRRzP7ymunFsBJFT8frYK4hLNjUCBi4VWa2KmZ";

        //Contract service List
        public static TokenContract tokenService { get; set; }
        public static ConsensusContract consensusService { get; set; }
        public static DividendsContract dividendsService { get; set; }

        public string RpcUrl { get; } = "http://192.168.197.20:8010/chain";
        public CliHelper CH { get; set; }

        #endregion

        [TestInitialize]
        public void Initlize()
        {
            //Init log
            string logName = "VoteBP_" + DateTime.Now.ToString("MMddHHmmss") + ".log";
            string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", logName);
            Logger.InitLogHelper(dir);
            CandidatePublicKeys = new List<string>();
            UserList = new List<string>();
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

            //Get FullNode Info
            FullNodeAccounts = new List<string>();
            FullNodeAccounts.Add("ELF_26cUpeiNb6q4DdFFEXTiPgWcifxtwEMsqshKHWGeYxaJkT1");
            FullNodeAccounts.Add("ELF_2AQdMyH4bo6KKK7Wt5fN7LerrWdoPUYTcJHyZNKXqoD2a4V");
            FullNodeAccounts.Add("ELF_3FWgHoNdR92rCSEbYqzD6ojCCmVKEpoPt87tpmwWYAkYm6d");
            FullNodeAccounts.Add("ELF_4ZNjzrUrNQGyWrAmxEtX7s5i4bNhXHECYHd4XK1hR9rJNFC");
            FullNodeAccounts.Add("ELF_6Fp72su6EPmHkEiojK1FyP7DsMHm16MygkG93zyqSbnE84v");

            //Get BpNode Info
            BpNodeAccounts = new List<string>();
            BpNodeAccounts.Add("ELF_2jLARdoRyaQ2m8W5s1Fnw7EgyvEr9SX9SgNhejzcwfBKkvy");
            BpNodeAccounts.Add("ELF_2VsBNkgc9ZVkr6wQoNY7FjnPooMJscS9SLNQ5jDFtuSEKud");
            BpNodeAccounts.Add("ELF_3YcUM4EjAcUYyZxsNb7KHPfXdnYdwKmwr9g3p2eipBE6Wym");
            BpNodeAccounts.Add("ELF_59w62zTynBKyQg5Pi4uNTz29QF7M1SHazN71g6pG5N25wY1");
            BpNodeAccounts.Add("ELF_5tqoweoWNrCRKG8Z28LM63B4aiuBjhZwy6JYw57iqcDqgN6");

            //Init service
            tokenService = new TokenContract(CH, InitAccount, TokenAbi);
            consensusService = new ConsensusContract(CH, InitAccount, ConsensusAbi);
            dividendsService = new DividendsContract(CH, InitAccount, DividendsAbi);

            QueryContractsBalance();
        }

        [TestCleanup]
        public void Cleanup()
        {
            if (UserList.Count == 0) return;
            Logger.WriteInfo("Delete all account files created.");
            foreach (var item in UserList)
            {
                string file = Path.Combine(AccountManager.GetDefaultDataDir(), $"{item}.ak");
                File.Delete(file);
            }
        }

        private void QueryContractsBalance()
        {
            var consensusResult = tokenService.CallReadOnlyMethod(TokenMethod.BalanceOf, ConsensusAbi);
            Logger.WriteInfo($"Consensus account balance : {tokenService.ConvertViewResult(consensusResult, true)}");
            var dividensResult = tokenService.CallReadOnlyMethod(TokenMethod.BalanceOf, DividendsAbi);
            Logger.WriteInfo($"Dividends account balance : {tokenService.ConvertViewResult(dividensResult, true)}");
        }

        private void SetTokenFeeAddress()
        {
            tokenService.CallContractMethod(TokenMethod.SetFeePoolAddress, FeeAccount);
            var feeResult = tokenService.CallReadOnlyMethod(TokenMethod.FeePoolAddress);
            Logger.WriteInfo($"Fee account address : {tokenService.ConvertViewResult(feeResult)}");
        }

        private void QueryTokenFeeBalance()
        {
            var feeResult = tokenService.CallReadOnlyMethod(TokenMethod.BalanceOf, FeeAccount);
            Logger.WriteInfo($"Fee account balance : {tokenService.ConvertViewResult(feeResult, true)}");
        }

        [TestMethod()]
        [Ignore("Will recover after testing contract fee part.")]
        public void SetTokenFeeAccount()
        {
            SetTokenFeeAddress();
            QueryTokenFeeBalance();
        }

        [TestMethod]
        public void QueryCandidatesBalance()
        {
            //查询余额
            foreach (var bpAcc in BpNodeAccounts)
            {
                var callResult = tokenService.CallReadOnlyMethod(TokenMethod.BalanceOf, bpAcc);
                Console.WriteLine($"BpNode-[{bpAcc}] balance: " + tokenService.ConvertViewResult(callResult, true));
            }

            foreach (var fullAcc in FullNodeAccounts)
            {
                var callResult = tokenService.CallReadOnlyMethod(TokenMethod.BalanceOf, fullAcc);
                Console.WriteLine($"FullNode-[{fullAcc}] balance: " + tokenService.ConvertViewResult(callResult, true));
            }
        }

        [TestMethod]
        public void PrepareCandidateAsset()
        {
            consensusService.SetAccount(BpNodeAccounts[0]);

            //分配资金给FullNode
            foreach (var fullAcc in FullNodeAccounts)
            {
                var balanceResult = tokenService.CallReadOnlyMethod(TokenMethod.BalanceOf, fullAcc);
                var balance = long.Parse(tokenService.ConvertViewResult(balanceResult, true));
                if (balance >= 100000)
                    continue;

                consensusService.CallContractWithoutResult(ConsensusMethod.InitialBalance, fullAcc, "100000");
            }
            //分配资金给BP
            foreach (var bpAcc in BpNodeAccounts)
            {
                var balanceResult = tokenService.CallReadOnlyMethod(TokenMethod.BalanceOf, bpAcc);
                var balance = long.Parse(tokenService.ConvertViewResult(balanceResult, true));
                if (balance >= 100000)
                    continue;

                consensusService.CallContractWithoutResult(ConsensusMethod.InitialBalance, bpAcc, "100000");
            }

            consensusService.CheckTransactionResultList();

            //查询余额
            QueryCandidatesBalance();

            Logger.WriteInfo("All accounts asset prepared completed.");
        }

        [TestMethod]
        public void JoinElection()
        {
            PrepareCandidateAsset();

            //参加选举
            foreach (var fullAcc in FullNodeAccounts)
            {
                consensusService.SetAccount(fullAcc);
                consensusService.CallContractWithoutResult(ConsensusMethod.AnnounceElection, $"Full-{fullAcc.Substring(5, 4)}");
            }

            foreach (var bpAcc in BpNodeAccounts)
            {
                consensusService.SetAccount(bpAcc);
                consensusService.CallContractWithoutResult(ConsensusMethod.AnnounceElection, $"BP-{bpAcc.Substring(5,4)}");
            }
            consensusService.CheckTransactionResultList(); 

            //检查余额
            QueryCandidatesBalance();

            GetCandidateList();

            Logger.WriteInfo("All Full Node joined election completed.");
        }

        [TestMethod]
        public void GetCandidateList()
        {
            var candidateResult = consensusService.CallReadOnlyMethod(ConsensusMethod.GetCandidatesListToFriendlyString);
            string candidatesJson = consensusService.ConvertViewResult(candidateResult);
            bool result = DataHelper.TryGetArrayFromJson(out var pubkeyList, candidatesJson, "Values");
            Assert.IsTrue(result, "Candidate account is null.");
            CandidatePublicKeys = pubkeyList;
            var count = 1;
            foreach (var item in CandidatePublicKeys)
            {
                Logger.WriteInfo($"Candidate {count++}: {item}");
            }
        }

        [TestMethod]
        public void IsCandidate()
        {
            GetCandidateList();
            //判断是否是Candidate
            Logger.WriteInfo("IsCandidate Test");
            var candidate1Result = consensusService.CallReadOnlyMethod(ConsensusMethod.IsCandidate, CandidatePublicKeys[4]);
            Logger.WriteInfo(consensusService.ConvertViewResult(candidate1Result,true));
            var candidate2Result = consensusService.CallReadOnlyMethod(ConsensusMethod.IsCandidate, "4ZNjzrUrNQGyWrAmxEtX7s5i4bNhXHECYHd4XK1hR9rJNFC");
            Logger.WriteInfo(consensusService.ConvertViewResult(candidate2Result, true));
        }

        [TestMethod]
        [DataRow(50)]
        public void PrepareUserAccountAndBalance(int userAccount)
        {
            //Account preparation
            UserList = new List<string>();
            var ci = new CommandInfo("account new", "account");
            for (int i = 0; i < userAccount; i++)
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
            tokenService.SetAccount(BpNodeAccounts[0]);
            foreach (var acc in UserList)
            {
                tokenService.CallContractWithoutResult(TokenMethod.Transfer, acc, "10000");
            }
            tokenService.CheckTransactionResultList();

            foreach (var userAcc in UserList)
            {
                var callResult = tokenService.CallReadOnlyMethod(TokenMethod.BalanceOf, userAcc);
                Console.WriteLine($"User-{userAcc} balance: " + tokenService.ConvertViewResult(callResult, true));
            }

            Logger.WriteInfo("All accounts created and unlocked.");
        }

        //参加选举
        [TestMethod]
        public void UserVoteAction()
        {
            GetCandidateList();
            PrepareUserAccountAndBalance(10);

            Random rd = new Random(DateTime.Now.Millisecond);
            foreach (var voteAcc in UserList)
            {
                string votePbk = CandidatePublicKeys[rd.Next(0, CandidatePublicKeys.Count-1)];
                string voteVolumn = rd.Next(100, 500).ToString();
                string voteLock = rd.Next(90, 1080).ToString();

                consensusService.SetAccount(voteAcc);
                consensusService.CallContractWithoutResult(ConsensusMethod.Vote, votePbk, voteVolumn, voteLock);
            }



            consensusService.CheckTransactionResultList();
            //检查投票结果
            GetCurrentElectionInfo();
            Logger.WriteInfo("Vote completed.");
        }


        [TestMethod]
        [DataRow(5)]
        public void UserVoteForFullNode(int voteUsers)
        {
            GetCandidateList();
            PrepareUserAccountAndBalance(voteUsers);

            Random rd = new Random(DateTime.Now.Millisecond);
            foreach (var voteAcc in UserList)
            {
                //Vote For someone
                int candidateUser = rd.Next(0, CandidatePublicKeys.Count - 1);
                string votePbk = CandidatePublicKeys[candidateUser];
                string voteVolumn = "100";
                string voteLock = "5";

                consensusService.SetAccount(voteAcc);
                consensusService.CallContractWithoutResult(ConsensusMethod.Vote, votePbk, voteVolumn, voteLock);
                Logger.WriteInfo($"Vote action: User: {candidateUser}, Tickets: {voteVolumn}");
            }

            consensusService.CheckTransactionResultList();
            //检查投票结果
            GetCurrentElectionInfo();
            Logger.WriteInfo("Vote completed.");
        }

        [TestMethod]
        public void GetBlockchainAge()
        {
            var ageInfo = consensusService.CallReadOnlyMethod(ConsensusMethod.GetBlockchainAge);
            Logger.WriteInfo($"Chain age: {consensusService.ConvertViewResult(ageInfo, true)}");
        }

        [TestMethod]
        [DataRow(24)]
        [DataRow(25)]
        public void GetTermSnapshot(int termNo)
        {
            var termInfo = consensusService.CallReadOnlyMethod(ConsensusMethod.GetTermSnapshotToFriendlyString, termNo.ToString());
            Logger.WriteInfo(consensusService.ConvertViewResult(termInfo));
        }

        [TestMethod]
        [DataRow(100)]
        [DataRow(260)]
        public void GetTermNumberByRoundNumber(int termNo)
        {
            Logger.WriteInfo("GetTermNumberByRoundNumber Test");
            var termNo1 = consensusService.CallReadOnlyMethod(ConsensusMethod.GetTermNumberByRoundNumber, termNo.ToString());
            Logger.WriteInfo(consensusService.ConvertViewResult(termNo1, true));
        }

        [TestMethod]
        public void GetCandidateHistoryInfo()
        {
            Logger.WriteInfo("GetCandidateHistoryInfo Test");

            GetCandidateList();
            foreach (var pubKey in CandidatePublicKeys)
            {
                var historyResult = consensusService.CallReadOnlyMethod(ConsensusMethod.GetCandidateHistoryInfoToFriendlyString, pubKey);
                Logger.WriteInfo(consensusService.ConvertViewResult(historyResult));
            }
        }

        [TestMethod]
        public void GetGetCurrentMinersInfo()
        {
            Logger.WriteInfo("GetCurrentVictories Test");
            var minersResult = consensusService.CallReadOnlyMethod(ConsensusMethod.GetCurrentMinersToFriendlyString);
            var jsonString = consensusService.ConvertViewResult(minersResult);
            DataHelper.TryGetArrayFromJson(out var pubkeys, jsonString, "PublicKeys");
            CurrentMinersKeys = pubkeys;
            var count = 1;
            foreach (var miner in CurrentMinersKeys)
            {
                Logger.WriteInfo($"Miner {count++}: {miner}");
            }
        }

        [TestMethod]
        public void GetTicketsInfo()
        {
            Logger.WriteInfo("GetTicketsInfo Test");
            GetCandidateList();
            foreach (var candidate in CandidatePublicKeys)
            {
                var ticketResult = consensusService.CallReadOnlyMethod(ConsensusMethod.GetTicketsInfoToFriendlyString, candidate);
                Logger.WriteInfo(consensusService.ConvertViewResult(ticketResult));
            }
        }

        [TestMethod]
        [DataRow("04bba3c7c54d9802f40010ae141e0f5c0b1bf9144e4f8e08e30f56a2e3922e4f3a924998a4f3c7d437f69da7af4398c281d02a753ccb297aa735294dc2e4ea6064")]
        public void GetCandidateTicketsInfo(string candidatePubkey)
        {
            Logger.WriteInfo("GetCandidateTicketsInfo Test");
            var ticketResult = consensusService.CallReadOnlyMethod(ConsensusMethod.GetTicketsInfoToFriendlyString, candidatePubkey);
            Logger.WriteInfo(consensusService.ConvertViewResult(ticketResult));
        }

        [TestMethod]
        public void GetCurrentElectionInfo()
        {
            Logger.WriteInfo("GetCurrentElectionInfo Test");
            var currentElectionResult = consensusService.CallReadOnlyMethod(ConsensusMethod.GetCurrentElectionInfoToFriendlyString, "0", "0", "0");
            Logger.WriteInfo(consensusService.ConvertViewResult(currentElectionResult));
        }

        [TestMethod]
        public void GetVotesCount()
        {
            var voteCount = consensusService.CallReadOnlyMethod(ConsensusMethod.GetVotesCount);
            Logger.WriteInfo($"Votes count: {consensusService.ConvertViewResult(voteCount, true)}");
        }

        [TestMethod]
        public void GetTicketsCount()
        {
            UserVoteAction();
            var ticketsCount = consensusService.CallReadOnlyMethod(ConsensusMethod.GetTicketsCount);
            Logger.WriteInfo($"Tickets count: {consensusService.ConvertViewResult(ticketsCount, true)}");
        }

        [TestMethod]
        public void GetCurrentVictories()
        {
            Logger.WriteInfo("GetCurrentVictories Test");
            var victoriesResult = consensusService.CallReadOnlyMethod(ConsensusMethod.GetCurrentVictoriesToFriendlyString);
            Logger.WriteInfo(consensusService.ConvertViewResult(victoriesResult));
        }

        [TestMethod]
        public void QueryCurrentDividends()
        {
            Logger.WriteInfo("QueryCurrentDividends Test");
            var victoriesResult = consensusService.CallReadOnlyMethod(ConsensusMethod.QueryCurrentDividends);
            Logger.WriteInfo(consensusService.ConvertViewResult(victoriesResult, true));
        }

        [TestMethod]
        public void QueryCurrentDividendsForVoters()
        {
            Logger.WriteInfo("QueryCurrentDividendsForVoters Test");
            var dividendsResult = consensusService.CallReadOnlyMethod(ConsensusMethod.QueryCurrentDividendsForVoters);
            Logger.WriteInfo(consensusService.ConvertViewResult(dividendsResult, true));
        }

        [TestMethod]
        public void QueryDividends()
        {
            UserVoteAction();
            Thread.Sleep(30000);
            foreach (var userAcc in UserList)
            {
                Logger.WriteInfo($"Account check: {userAcc}");
                consensusService.SetAccount(userAcc);
                var balanceBefore = tokenService.CallReadOnlyMethod(TokenMethod.BalanceOf, userAcc);
                Logger.WriteInfo($"Init balance: {tokenService.ConvertViewResult(balanceBefore, true)}");

                consensusService.SetAccount(userAcc);
                var allDividenceResult = consensusService.CallContractMethod(ConsensusMethod.ReceiveAllDividends);

                var balanceAfter1 = tokenService.CallReadOnlyMethod(TokenMethod.BalanceOf, userAcc);
                Logger.WriteInfo($"Received dividends balance: {tokenService.ConvertViewResult(balanceAfter1, true)}");

                var drawResult = consensusService.CallContractMethod(ConsensusMethod.WithdrawAll, "true");

                var balanceAfter2 = tokenService.CallReadOnlyMethod(TokenMethod.BalanceOf, userAcc);
                Logger.WriteInfo($"Revertback vote balance: {tokenService.ConvertViewResult(balanceAfter2, true)}");
            }
        }

        [TestMethod]
        public void QueryMinedBlockCountInCurrentTerm()
        {
            GetGetCurrentMinersInfo();
            foreach (var miner in CurrentMinersKeys)
            {
                var blockResult = consensusService.CallReadOnlyMethod(ConsensusMethod.QueryMinedBlockCountInCurrentTerm, miner);
                Logger.WriteInfo($"Generate blocks count: {consensusService.ConvertViewResult(blockResult, true)}");
            }
        }

        [TestMethod]
        public void QueryAliasesInUse()
        {
            var aliasResults = consensusService.CallReadOnlyMethod(ConsensusMethod.QueryAliasesInUseToFriendlyString);
            Logger.WriteInfo($"Alias in use: {consensusService.ConvertViewResult(aliasResults)}");
        }

        //退出选举
        [TestMethod]
        public void QuitElection()
        {
            consensusService.SetAccount(FullNodeAccounts[0]);
            consensusService.CallContractMethod(ConsensusMethod.QuitElection);

            consensusService.SetAccount(FullNodeAccounts[1]);
            consensusService.CallContractMethod(ConsensusMethod.QuitElection);

            consensusService.SetAccount(FullNodeAccounts[2]);
            consensusService.CallContractMethod(ConsensusMethod.QuitElection);
            GetCandidateList();

            //查询余额
            foreach (var fullAcc in FullNodeAccounts)
            {
                var callResult = tokenService.CallReadOnlyMethod(TokenMethod.BalanceOf, fullAcc);
                Console.WriteLine($"FullNode token-{fullAcc}: " + tokenService.ConvertViewResult(callResult, true));
            }
        }


        [TestMethod]
        public void ReAnnounceElection()
        {
            consensusService.SetAccount(FullNodeAccounts[0]);
            consensusService.CallContractMethod(ConsensusMethod.AnnounceElection, $"Full{FullNodeAccounts[0].Substring(8, 4)}");

            consensusService.SetAccount(FullNodeAccounts[1]);
            consensusService.CallContractMethod(ConsensusMethod.AnnounceElection, $"Full{FullNodeAccounts[1].Substring(8, 4)}");

            consensusService.SetAccount(FullNodeAccounts[2]);
            consensusService.CallContractMethod(ConsensusMethod.AnnounceElection, $"Full{FullNodeAccounts[2].Substring(8, 4)}");
            GetCandidateList();

            //查询余额
            foreach (var fullAcc in FullNodeAccounts)
            {
                var callResult = tokenService.CallReadOnlyMethod(TokenMethod.BalanceOf, fullAcc);
                Console.WriteLine($"FullNode token-{fullAcc}: " + tokenService.ConvertViewResult(callResult, true));
            }
        }

        [TestMethod]
        public void VoteBP()
        {
            PrepareCandidateAsset();
            JoinElection();
            GetCandidateList();

            PrepareUserAccountAndBalance(10);
            UserVoteAction();

            //查询信息
            GetCandidateHistoryInfo();
            GetGetCurrentMinersInfo();
            GetTicketsInfo();
            GetCurrentVictories();
            QueryDividends();
            QueryCurrentDividendsForVoters();

            //取消参选
            QuitElection();
        }

        [TestMethod]
        public void QuiteElectionTest()
        {
            QuitElection();
            GetCandidateList();
        }

        [TestMethod]
        public void VoteBpTest()
        {
            GetCandidateList();
            PrepareUserAccountAndBalance(10);
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
