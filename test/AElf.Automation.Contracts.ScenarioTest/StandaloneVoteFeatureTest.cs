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
    public class StandaloneVoteFeatureTest
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
        public List<CandidateInfo> CandidateInfos { get; set; }

        public string InitAccount { get; } = "ELF_2GkD1q74HwBrFsHufmnCKHJvaGVBYkmYcdG3uebEsAWSspX";
        public string FeeAccount { get; } = "ELF_1dVay78LmRRzP7ymunFsBJFT8frYK4hLNjUCBi4VWa2KmZ";

        //Contract service List
        public static TokenContract tokenService { get; set; }
        public static ConsensusContract consensusService { get; set; }
        public static DividendsContract dividendsService { get; set; }

        public string RpcUrl { get; } = "http://192.168.197.35:8000/chain";
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
            var ci = new CommandInfo(ApiMethods.GetChainInformation);
            CH.RpcGetChainInformation(ci);
            Assert.IsTrue(ci.Result, "Connect chain got exception.");

            //Get BpNode Info
            BpNodeAccounts = new List<string>();
            BpNodeAccounts.Add("ELF_36MNPfQt6KZJaRHKjjoMMxwuPffTSKjgve2RuBxpLHmCjVm");

            //Get candidate infos
            CandidateInfos = new List<CandidateInfo>();
            for (int i = 0; i < BpNodeAccounts.Count; i++)
            {
                string name = $"Bp-{i+1}";
                string account = BpNodeAccounts[i];
                string pubKey = CH.GetPublicKeyFromAddress(account);
                CandidateInfos.Add(new CandidateInfo(){Name = name, Account = account, PublicKey = pubKey});
            }

            //Init service
            tokenService = new TokenContract(CH, InitAccount, TokenAbi);
            consensusService = new ConsensusContract(CH, InitAccount, ConsensusAbi);
            dividendsService = new DividendsContract(CH, InitAccount, DividendsAbi);

            //Set Token Fee
            //SetTokenFeeAccount();
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
        /*
        private void SetTokenFeeAddress()
        {
            tokenService.CallContractMethod(TokenMethod.SetFeePoolAddress, FeeAccount);
            var feeResult = tokenService.CallReadOnlyMethod(TokenMethod.FeePoolAddress);
            DataHelper.TryGetValueFromJson(out var feeAddress, feeResult, "result", "return");
            Logger.WriteInfo($"Fee account address : {feeAddress}");
        }

        private void QueryTokenFeeBalance()
        {
            var feeResult = tokenService.CallReadOnlyMethod(TokenMethod.GetBalance, FeeAccount);
            Logger.WriteInfo($"Fee account balance : {tokenService.ConvertViewResult(feeResult, true)}");
        }

        [TestMethod]
        public void SetTokenFeeAccount()
        {
            SetTokenFeeAddress();
            QueryTokenFeeBalance();
        }
        
  
        [TestMethod]
        public void PrepareCandidateAsset()
        {
            consensusService.SetAccount(BpNodeAccounts[0]);

            //分配资金给BP
            Logger.WriteInfo("Allowance token to BpNode accounts");
            foreach (var bpAcc in BpNodeAccounts)
            {
                Logger.WriteInfo($"Account: {bpAcc}\nPubKey:{CH.GetPublicKeyFromAddress(bpAcc)}");
                var balanceResult = tokenService.CallReadOnlyMethod(TokenMethod.GetBalance, bpAcc);
                var balance = long.Parse(tokenService.ConvertViewResult(balanceResult, true));
                if (balance >= 100000)
                    continue;

                consensusService.CallContractWithoutResult(ConsensusMethod.InitialBalance, bpAcc, "100000");
            }

            consensusService.CheckTransactionResultList();

            Logger.WriteInfo("All accounts asset prepared completed.");
        }

        [TestMethod]
        [DataRow(50)]
        public void PrepareUserAccountAndBalance(int userAccount)
        {
            //Account preparation
            UserList = new List<string>();
            var ci = new CommandInfo("AccountNew", "account");
            for (int i = 0; i < userAccount; i++)
            {
                ci.Parameter = "123";
                ci = CH.NewAccount(ci);
                if (ci.Result)
                    UserList.Add(ci.InfoMsg?[0].Replace("Account address:", "").Trim());

                //unlock
                var uc = new CommandInfo("AccountUnlock", "account");
                uc.Parameter = String.Format("{0} {1} {2}", UserList[i], "123", "notimeout");
                CH.UnlockAccount(uc);
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
                var callResult = tokenService.CallReadOnlyMethod(TokenMethod.GetBalance, userAcc);
                Console.WriteLine($"User-{userAcc} balance: " + tokenService.ConvertViewResult(callResult, true));
            }

            Logger.WriteInfo("All accounts created and unlocked.");
        }

        [TestMethod]
        public void GetPageableElectionInfo()
        {
            Logger.WriteInfo("GetCurrentElectionInfo Test");
            var currentElectionResult = consensusService.CallReadOnlyMethod(ConsensusMethod.GetPageableElectionInfoToFriendlyString, "0", "0", "0");
            Logger.WriteInfo(consensusService.ConvertViewResult(currentElectionResult));
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

                DataHelper.TryGetArrayFromJson(out var recordArray, ticketsJson, "VotingRecords");
                var sumCount = 0;
                foreach (var record in recordArray)
                {
                    DataHelper.TryGetValueFromJson(out var countStr, record, "Count");
                    sumCount += int.Parse(countStr);
                }
                Logger.WriteInfo($"Candidate: {candidate}, Tickets: {sumCount}");
            }
        }

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

            consensusService.CheckTransactionResultList();

            GetCandidateList();

            Logger.WriteInfo("All Full Node joined election completed.");
        }

        //参加选举
        [TestMethod]
        [DataRow(50)]
        public void UserVoteAction(int voteUserCount)
        {
            GetCandidateList();
            PrepareUserAccountAndBalance(voteUserCount);

            Random rd = new Random(DateTime.Now.Millisecond);
            foreach (var voteAcc in UserList)
            {
                string votePbk = CandidatePublicKeys[rd.Next(0, CandidatePublicKeys.Count-1)];
                string voteVolumn = rd.Next(100, 500).ToString();
                string voteLock = rd.Next(5, 10).ToString();

                consensusService.SetAccount(voteAcc);
                consensusService.CallContractWithoutResult(ConsensusMethod.Vote, votePbk, voteVolumn, voteLock);
            }

            consensusService.CheckTransactionResultList();
            //检查投票结果
            GetPageableElectionInfo();
            GetTicketsInfo();
            Logger.WriteInfo("Vote completed.");
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
        */
    }
}