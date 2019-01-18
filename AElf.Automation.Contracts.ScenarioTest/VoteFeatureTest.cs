using AElf.Automation.Common.Extensions;
using AElf.Automation.Common.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using AElf.Automation.Common.Contracts;


namespace AElf.Automation.Contracts.ScenarioTest
{
    public class CandidateInfo
    {
        public string Name { get; set; }
        public string Account { get; set; }
        public string PublicKey { get; set; }
    }

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
        public List<string> NodesPublicKeys { get; set; }
        public List<string> CandidatePublicKeys { get; set; }
        public List<string> CurrentMinersKeys { get; set; }
        public List<CandidateInfo> CandidateInfos { get; set; }
        public int TestNode { get; } = 3;
        public string InitAccount { get; } = "ELF_2GkD1q74HwBrFsHufmnCKHJvaGVBYkmYcdG3uebEsAWSspX";
        public string FeeAccount { get; } = "ELF_1dVay78LmRRzP7ymunFsBJFT8frYK4hLNjUCBi4VWa2KmZ";

        //Contract service List
        public static TokenContract tokenService { get; set; }
        public static ConsensusContract consensusService { get; set; }
        public static DividendsContract dividendsService { get; set; }

        public string RpcUrl { get; } = "http://192.168.197.15:8010/chain";
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
            if (TestNode == 5)
            {
                FullNodeAccounts.Add("ELF_4ZNjzrUrNQGyWrAmxEtX7s5i4bNhXHECYHd4XK1hR9rJNFC");
                FullNodeAccounts.Add("ELF_6Fp72su6EPmHkEiojK1FyP7DsMHm16MygkG93zyqSbnE84v");
            }

            //Get BpNode Info
            BpNodeAccounts = new List<string>();
            BpNodeAccounts.Add("ELF_2jLARdoRyaQ2m8W5s1Fnw7EgyvEr9SX9SgNhejzcwfBKkvy");
            BpNodeAccounts.Add("ELF_2VsBNkgc9ZVkr6wQoNY7FjnPooMJscS9SLNQ5jDFtuSEKud");
            BpNodeAccounts.Add("ELF_3YcUM4EjAcUYyZxsNb7KHPfXdnYdwKmwr9g3p2eipBE6Wym");
            if (TestNode == 5)
            {
                BpNodeAccounts.Add("ELF_59w62zTynBKyQg5Pi4uNTz29QF7M1SHazN71g6pG5N25wY1");
                BpNodeAccounts.Add("ELF_5tqoweoWNrCRKG8Z28LM63B4aiuBjhZwy6JYw57iqcDqgN6");
            }

            //Get candidate infos
            NodesPublicKeys = new List<string>();
            CandidateInfos = new List<CandidateInfo>();
            for (int i = 0; i < BpNodeAccounts.Count; i++)
            {
                string name = $"Bp-{i+1}";
                string account = BpNodeAccounts[i];
                string pubKey = CH.GetPublicKeyFromAddress(account);
                NodesPublicKeys.Add(pubKey);
                Logger.WriteInfo($"{account}: {pubKey}");
                CandidateInfos.Add(new CandidateInfo(){Name = name, Account = account, PublicKey = pubKey});
            }
            for (int i = 0; i < FullNodeAccounts.Count; i++)
            {
                string name = $"Full-{i+1}";
                string account = FullNodeAccounts[i];
                string pubKey = CH.GetPublicKeyFromAddress(account);
                NodesPublicKeys.Add(pubKey);
                Logger.WriteInfo($"{account}: {pubKey}");
                CandidateInfos.Add(new CandidateInfo(){Name = name, Account = account, PublicKey = pubKey});
            }

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
            DataHelper.TryGetValueFromJson(out var feeAddress, feeResult, "result", "return");
            Logger.WriteInfo($"Fee account address : {feeAddress}");
        }

        private void QueryTokenFeeBalance()
        {
            var feeResult = tokenService.CallReadOnlyMethod(TokenMethod.BalanceOf, FeeAccount);
            Logger.WriteInfo($"Fee account balance : {tokenService.ConvertViewResult(feeResult, true)}");
        }

        [TestMethod]
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
        [DataRow("ELF_6HC6tx7kPguUhCFWeoVQfEJiv5Tfw4itrEgMPNT5ujsV2Vz", 10000)]
        [DataRow("ELF_2N9soUD1FxhWS9JDkiee1uayZCnmhgwoSESThQYUqLX5AVG", 10000)]
        public void InitialUserBalance(string account, long balance)
        {
            consensusService.SetAccount(BpNodeAccounts[0]);
            consensusService.CallContractMethod(ConsensusMethod.InitialBalance, account, balance.ToString());
            var callResult = tokenService.CallReadOnlyMethod(TokenMethod.BalanceOf, account);
            Console.WriteLine($"[{account}] balance: " + tokenService.ConvertViewResult(callResult, true));
        }

        [TestMethod]
        [DataRow("ELF_6HC6tx7kPguUhCFWeoVQfEJiv5Tfw4itrEgMPNT5ujsV2Vz")]
        public void QueryPublicKey(string account, string password="123")
        {
            var pubKey = CH.GetPublicKeyFromAddress(account, password);
            Logger.WriteInfo($"PubKey: {pubKey}");
        }

        [TestMethod]
        public void PrepareCandidateAsset()
        {
            consensusService.SetAccount(BpNodeAccounts[0]);

            //分配资金给FullNode
            Logger.WriteInfo("Allowance token to FullNode accounts");
            foreach (var fullAcc in FullNodeAccounts)
            {
                Logger.WriteInfo($"Account: {fullAcc}\nPubKey:{CH.GetPublicKeyFromAddress(fullAcc)}");
                var balanceResult = tokenService.CallReadOnlyMethod(TokenMethod.BalanceOf, fullAcc);
                var balance = long.Parse(tokenService.ConvertViewResult(balanceResult, true));
                if (balance >= 100000)
                    continue;

                consensusService.CallContractWithoutResult(ConsensusMethod.InitialBalance, fullAcc, "100000");
            }
            //分配资金给BP
            Logger.WriteInfo("Allowance token to BpNode accounts");
            foreach (var bpAcc in BpNodeAccounts)
            {
                Logger.WriteInfo($"Account: {bpAcc}\nPubKey:{CH.GetPublicKeyFromAddress(bpAcc)}");
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

        //参加选举
        [TestMethod]
        public void JoinElection()
        {
            PrepareCandidateAsset();

            //参加选举
            foreach (var bpAcc in BpNodeAccounts)
            {
                consensusService.SetAccount(bpAcc);
                consensusService.CallContractWithoutResult(ConsensusMethod.AnnounceElection, $"Bp-{bpAcc.Substring(5,4)}");
            }

            foreach (var fullAcc in FullNodeAccounts)
            {
                consensusService.SetAccount(fullAcc);
                consensusService.CallContractWithoutResult(ConsensusMethod.AnnounceElection, $"Full-{fullAcc.Substring(5, 4)}");
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
                CH.UnlockAccount(uc);
            }

            //分配资金给普通用户
            tokenService.SetAccount(BpNodeAccounts[0]);
            foreach (var acc in UserList)
            {
                tokenService.CallContractWithoutResult(TokenMethod.Transfer, acc, "100000");
            }
            tokenService.CheckTransactionResultList();

            foreach (var userAcc in UserList)
            {
                var callResult = tokenService.CallReadOnlyMethod(TokenMethod.BalanceOf, userAcc);
                Console.WriteLine($"User-{userAcc} balance: " + tokenService.ConvertViewResult(callResult, true));
            }

            Logger.WriteInfo("All accounts created and unlocked.");
        }


        [TestMethod]
        [DataRow(
            "04d5a0ab908b1e6a99be1d4b1d5e4ab7c3bd3b234f714d674a1aad7dc462436b0345cb6384b589a5be0aa6bc9c8a78ebb10e5d0a865deade3fc48b446075b26cb3")]
        public void UserVoteForCandidate(string pubKey)
        {
            PrepareUserAccountAndBalance(1);
            string voteVolumn = "100";
            string voteLock = "8";

            consensusService.SetAccount(UserList[0]);
            consensusService.CallContractMethod(ConsensusMethod.Vote, pubKey, voteVolumn, voteLock);

            //Get candidate ticket
            GetCandidateTicketsInfo(pubKey);
        }


        [TestMethod]
        [DataRow(5, 20)]
        public void UserVoteAction(int voteUserCount, int voteTimes)
        {
            GetCandidateList();
            PrepareUserAccountAndBalance(voteUserCount);

            Random rd = new Random(DateTime.Now.Millisecond);
            for (int i = 0; i < voteTimes; i++)
            {
                foreach (var voteAcc in UserList)
                {
                    string votePbk = CandidatePublicKeys[rd.Next(0, CandidatePublicKeys.Count-1)];
                    string voteVolumn = rd.Next(10, 50).ToString();
                    string voteLock = rd.Next(5, 10).ToString();

                    consensusService.SetAccount(voteAcc);
                    consensusService.CallContractWithoutResult(ConsensusMethod.Vote, votePbk, voteVolumn, voteLock);
                }
            }
            consensusService.CheckTransactionResultList();

            //检查投票结果
            GetPageableElectionInfo();
            GetTicketsInfo();
            Logger.WriteInfo("Vote completed.");
        }

        [TestMethod]
        [DataRow(new[]{0, 1, 3})]
        public void UserVoteForNodes(int[] candidates)
        {
            GetCandidateList();
            PrepareUserAccountAndBalance(candidates.Length);

            for (int i = 0; i < candidates.Length; i++)
            {
                //Vote For someone

                string votePbk = NodesPublicKeys[candidates[i]];
                string voteVolumn = "200";
                string voteLock = "5";

                consensusService.SetAccount(UserList[i]);
                consensusService.CallContractWithoutResult(ConsensusMethod.Vote, votePbk, voteVolumn, voteLock);
                Logger.WriteInfo($"Vote action: User: {UserList[i]}, Tickets: {voteVolumn}");
            }

            consensusService.CheckTransactionResultList();
            //检查投票结果
            GetPageableElectionInfo();
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
                var ticketsJson = consensusService.ConvertViewResult(ticketResult);
                Logger.WriteInfo(ticketsJson);

                DataHelper.TryGetValueFromJson(out var recordTickets, ticketsJson, "ObtainedTickets");
                Logger.WriteInfo($"Candidate: {candidate}, Tickets: {recordTickets}");
            }
        }

        [TestMethod]
        [DataRow("04d5a0ab908b1e6a99be1d4b1d5e4ab7c3bd3b234f714d674a1aad7dc462436b0345cb6384b589a5be0aa6bc9c8a78ebb10e5d0a865deade3fc48b446075b26cb3")]
        public void GetCandidateTicketsInfo(string candidatePubkey)
        {
            Logger.WriteInfo("GetCandidateTicketsInfo Test");
            var ticketResult = consensusService.CallReadOnlyMethod(ConsensusMethod.GetTicketsInfoToFriendlyString, candidatePubkey);
            Logger.WriteInfo(consensusService.ConvertViewResult(ticketResult));
        }

        [TestMethod]
        public void GetPageableElectionInfo()
        {
            Logger.WriteInfo("GetCurrentElectionInfo Test");
            var currentElectionResult = consensusService.CallReadOnlyMethod(ConsensusMethod.GetPageableElectionInfoToFriendlyString, "0", "0", "0");
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
            UserVoteAction(5, 1);
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
            UserVoteAction(5, 1);
            Thread.Sleep(30000);
            foreach (var userAcc in UserList)
            {
                Logger.WriteInfo($"Account check: {userAcc}");
                consensusService.SetAccount(userAcc);
                var balanceBefore = tokenService.CallReadOnlyMethod(TokenMethod.BalanceOf, userAcc);
                Logger.WriteInfo($"Init balance: {tokenService.ConvertViewResult(balanceBefore, true)}");

                consensusService.SetAccount(userAcc);
                consensusService.CallContractMethod(ConsensusMethod.ReceiveAllDividends);

                var balanceAfter1 = tokenService.CallReadOnlyMethod(TokenMethod.BalanceOf, userAcc);
                Logger.WriteInfo($"Received dividends balance: {tokenService.ConvertViewResult(balanceAfter1, true)}");

                consensusService.CallContractMethod(ConsensusMethod.WithdrawAll, "true");

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
        public void VoteBp()
        {
            PrepareCandidateAsset();
            JoinElection();
            GetCandidateList();

            PrepareUserAccountAndBalance(10);
            UserVoteAction(5, 1);

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
            UserVoteAction(5, 1);
            GetTicketsInfo();
            GetCurrentVictories();
        }

        [TestMethod]
        public void QueryInformationTest()
        {
            GetCandidateList();

            GetCandidateHistoryInfo();

            GetPageableElectionInfo();

            GetCurrentVictories();

            GetTicketsInfo();
        }
    }
}
