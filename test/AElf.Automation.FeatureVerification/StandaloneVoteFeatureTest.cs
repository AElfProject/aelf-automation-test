using System.Collections.Generic;
using System.IO;
using AElfChain.Common.Contracts;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using log4net;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AElf.Automation.Contracts.ScenarioTest
{
    [TestClass]
    public class StandaloneVoteFeatureTest
    {
        [TestInitialize]
        public void Initlize()
        {
            //Init log
            Log4NetHelper.LogInit("VoteBP");
            CandidatePublicKeys = new List<string>();
            UserList = new List<string>();
            CH = new NodeManager(RpcUrl);

            //Get BpNode Info
            BpNodeAccounts = new List<string>();
            BpNodeAccounts.Add("ELF_36MNPfQt6KZJaRHKjjoMMxwuPffTSKjgve2RuBxpLHmCjVm");

            //Get candidate infos
            CandidateInfos = new List<CandidateInfo>();
            for (var i = 0; i < BpNodeAccounts.Count; i++)
            {
                var name = $"Bp-{i + 1}";
                var account = BpNodeAccounts[i];
                var pubKey = CH.GetAccountPublicKey(account);
                CandidateInfos.Add(new CandidateInfo {Name = name, Account = account, PublicKey = pubKey});
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
            Logger.Info("Delete all account files created.");
            foreach (var item in UserList)
            {
                var file = Path.Combine(CommonHelper.GetCurrentDataDir(), $"{item}.json");
                File.Delete(file);
            }
        }

        #region Priority

        public ILog Logger = Log4NetHelper.GetLogger();
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
        public INodeManager CH { get; set; }

        #endregion

        /*
        private void SetTokenFeeAddress()
        {
            tokenService.CallContractMethod(TokenMethod.SetFeePoolAddress, FeeAccount);
            var feeResult = tokenService.CallReadOnlyMethod(TokenMethod.FeePoolAddress);
            DataHelper.TryGetValueFromJson(out var feeAddress, feeResult, "result", "return");
            Logger.Info($"Fee account address : {feeAddress}");
        }

        private void QueryTokenFeeBalance()
        {
            var feeResult = tokenService.CallReadOnlyMethod(TokenMethod.GetBalance, FeeAccount);
            Logger.Info($"Fee account balance : {tokenService.ConvertViewResult(feeResult, true)}");
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

            Logger.Info("Allowance token to BpNode accounts");
            foreach (var bpAcc in BpNodeAccounts)
            {
                Logger.Info($"Account: {bpAcc}\nPubKey:{SideNode.GetAccountPublicKey(bpAcc)}");
                var balanceResult = tokenService.CallReadOnlyMethod(TokenMethod.GetBalance, bpAcc);
                var balance = long.Parse(tokenService.ConvertViewResult(balanceResult, true));
                if (balance >= 100000)
                    continue;

                consensusService.CallContractWithoutResult(ConsensusMethod.InitialBalance, bpAcc, "100000");
            }

            consensusService.CheckTransactionResultList();

            Logger.Info("All accounts asset prepared completed.");
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
                ci = SideNode.NewAccount(ci);
                if (ci.Result)
                    UserList.Add(ci.InfoMsg?[0]);

                //unlock
                var uc = new CommandInfo("AccountUnlock", "account");
                uc.Parameter = String.Format("{0} {1} {2}", UserList[i], "123", "notimeout");
                SideNode.UnlockAccount(uc);
            }

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

            Logger.Info("All accounts created and unlocked.");
        }

        [TestMethod]
        public void GetPageableElectionInfo()
        {
            Logger.Info("GetCurrentElectionInfo Test");
            var currentElectionResult = consensusService.CallReadOnlyMethod(ConsensusMethod.GetPageableElectionInfoToFriendlyString, "0", "0", "0");
            Logger.Info(consensusService.ConvertViewResult(currentElectionResult));
        }

        [TestMethod]
        public void GetTicketsInfo()
        {
            Logger.Info("GetTicketsInfo Test");
            GetCandidateList();
            foreach (var candidate in CandidatePublicKeys)
            {
                var ticketResult = consensusService.CallReadOnlyMethod(ConsensusMethod.GetTicketsInfoToFriendlyString, candidate);
                var ticketsJson = consensusService.ConvertViewResult(ticketResult);
                Logger.Info(ticketsJson);

                DataHelper.TryGetArrayFromJson(out var recordArray, ticketsJson, "VotingRecords");
                var sumCount = 0;
                foreach (var record in recordArray)
                {
                    DataHelper.TryGetValueFromJson(out var countStr, record, "Count");
                    sumCount += int.Parse(countStr);
                }
                Logger.Info($"Candidate: {candidate}, Tickets: {sumCount}");
            }
        }

        [TestMethod]
        public void JoinElection()
        {
            PrepareCandidateAsset();

            foreach (var bpAcc in BpNodeAccounts)
            {
                consensusService.SetAccount(bpAcc);
                consensusService.CallContractWithoutResult(ConsensusMethod.AnnounceElection, $"Bp-{bpAcc.Substring(5,4)}");
            }

            consensusService.CheckTransactionResultList();

            GetCandidateList();

            Logger.Info("All Full Node joined election completed.");
        }

        //attend election
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
            //check vote result
            GetPageableElectionInfo();
            GetTicketsInfo();
            Logger.Info("Vote completed.");
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
                Logger.Info($"Candidate {count++}: {item}");
            }
        }
        */
    }
}