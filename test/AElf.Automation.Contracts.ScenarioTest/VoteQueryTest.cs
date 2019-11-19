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
    public class VoteQueryTest
    {
        [TestInitialize]
        public void Initialize()
        {
            //Init log
            Log4NetHelper.LogInit("VoteBP");
            CandidatePublicKeys = new List<string>();
            UserList = new List<string>();
            CH = new NodeManager(RpcUrl);

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

        public static readonly ILog Logger = Log4NetHelper.GetLogger();
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
        public INodeManager CH { get; set; }

        #endregion

//        #region Dividends Test
//
//        [TestMethod]
//        [DataRow(1)]
//        [DataRow(2)]
//        [DataRow(5)]
//        public void GetTermDividends(int termNo)
//        {
//            var dividends = dividendsService.CallReadOnlyMethod(DividendsMethod.GetTermDividends, termNo.ToString());
//            Logger.Info($"GetTermDividends Terms:{termNo}, Dividends: {dividendsService.ConvertViewResult(dividends, true)}");
//        }
//
//        [TestMethod]
//        [DataRow(18)]
//        [DataRow(19)]
//        [DataRow(20)]
//        public void GetTermTotalWeights(int termNo)
//        {
//            var dividends = dividendsService.CallReadOnlyMethod(DividendsMethod.GetTermTotalWeights, termNo.ToString());
//            Logger.Info($"GetTermTotalWeights Terms:{termNo}, Total weight: {dividendsService.ConvertViewResult(dividends, true)}");
//        }
//
//        [TestMethod]
//        [DataRow(1000, 90)]
//        [DataRow(100, 900)]
//        [DataRow(500, 180)]
//        public void CheckDividendsOfPreviousTerm(int ticketsAmount, int lockTime)
//        {
//            var dividends = dividendsService.CallReadOnlyMethod(DividendsMethod.CheckDividendsOfPreviousTerm, ticketsAmount.ToString(), lockTime.ToString());
//            Logger.Info($"Ticket: {ticketsAmount}, LockTime: {lockTime}, Dividens: {dividendsService.ConvertViewResult(dividends, true)}");
//        }
//
//        [TestMethod]
//        [DataRow(1000, 90, 18)]
//        [DataRow(100, 900, 18)]
//        [DataRow(500, 180, 18)]
//        public void CheckDividends(int ticketsAmount, int lockTime, int termNo)
//        {
//            var dividends = dividendsService.CallReadOnlyMethod(DividendsMethod.CheckDividends, ticketsAmount.ToString(), lockTime.ToString(), termNo.ToString());
//            Logger.Info(
//                $"Ticket: {ticketsAmount}, LockTime: {lockTime}, TermNo: {termNo}, Dividens: {dividendsService.ConvertViewResult(dividends, true)}");
//        }
//
//        [TestMethod]
//        public void CheckStandardDividendsOfPreviousTerm()
//        {
//            var dividends = dividendsService.CallReadOnlyMethod(DividendsMethod.CheckStandardDividendsOfPreviousTerm);
//            Logger.Info($"Ticket: 10000, LockTime: 90, Dividens: {dividendsService.ConvertViewResult(dividends, true)}");
//        }
//
//        [TestMethod]
//        [DataRow(10)]
//        [DataRow(20)]
//        [DataRow(50)]
//        public void CheckStandardDividends(int termNo)
//        {
//            var dividends = dividendsService.CallReadOnlyMethod(DividendsMethod.CheckStandardDividendsOfPreviousTerm, termNo.ToString());
//            Logger.Info($"Ticket: 10000, LockTime: 90, Term:{termNo}, Dividens: {dividendsService.ConvertViewResult(dividends, true)}");
//        }
//
//        #endregion
    }
}