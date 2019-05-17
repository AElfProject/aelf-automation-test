using System;
using System.IO;
using AElf.Automation.Common.Extensions;
using AElf.Automation.Common.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AElf.Automation.Common.Contracts;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Contracts.MultiToken.Messages;
using AElf.Contracts.Vote;
using AElf.Kernel;
using Google.Protobuf.WellKnownTypes;
using VoteMinerInput = AElf.Contracts.Election.VoteMinerInput;

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
        public string TokenContract { get; set; }
        public string ConsensusContract { get; set; }
        public string DividendsContract { get; set; }
        
        public string ElectionContract { get; set; }
        
        public string VoteContract { get; set; }

        public List<string> UserList { get; set; }
        public List<string> FullNodeAccounts { get; set; }
        public List<string> BpNodeAccounts { get; set; }
        public List<string> NodesPublicKeys { get; set; }
        public List<string> CandidatePublicKeys { get; set; }
        public List<string> CurrentMinersKeys { get; set; }
        public List<CandidateInfo> CandidateInfos { get; set; }

        public string TokenSymbol { get; } = "ELF";
        public string InitAccount { get; } = "ELF_2GkD1q74HwBrFsHufmnCKHJvaGVBYkmYcdG3uebEsAWSspX";
        public string FeeAccount { get; } = "ELF_1dVay78LmRRzP7ymunFsBJFT8frYK4hLNjUCBi4VWa2KmZ";

        //Contract service List
        public static TokenContract tokenService { get; set; }
        public static ConsensusContract consensusService { get; set; }
        public static DividendsContract dividendsService { get; set; }
        public static ElectionContract electionService { get; set; }
        public static VoteContract voteService { get; set; }

        public string RpcUrl { get; } = "http://192.168.197.13:8100/chain";
        public WebApiHelper CH { get; set; }

        #endregion

        [TestInitialize]
        public void Initialize()
        {
            //Init log
            string logName = "VoteBP_" + DateTime.Now.ToString("MMddHHmmss") + ".log";
            string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", logName);
            Logger.InitLogHelper(dir);
            CandidatePublicKeys = new List<string>();
            UserList = new List<string>();
            CH = new WebApiHelper(RpcUrl, AccountManager.GetDefaultDataDir());
            
            //Connect Chain
            var ci = new CommandInfo(ApiMethods.GetChainInformation);
            CH.GetChainInformation(ci);
            Assert.IsTrue(ci.Result, "Connect chain got exception.");

            //Get FullNode Info
            FullNodeAccounts = new List<string>();
            FullNodeAccounts.Add("4KTaV1zZSCx4tKiEreqBEsB9DJoU2LqwRugcjowQpeX6YhM");
            FullNodeAccounts.Add("3dZLptQfoqUqFeSvJd8wZeKQ2bHZZXPyuJPFikFJ5Tvgt1F");
            FullNodeAccounts.Add("4GG2yVjPqYJd2zefrDJJkBnNeyvjPpTSKNANc39k7Q4JQpL");

            //Get BpNode Info
            BpNodeAccounts = new List<string>();
            BpNodeAccounts.Add("23my6hyVjXvHn4cS8i1CLgmCgtZDcNLAKKsgCXLuKrQvHzf");
            BpNodeAccounts.Add("4LCeZkfBMUKs5LeZDzPtGHwpK3kC4SyU4F5rU3Jhfe51JEg");
            BpNodeAccounts.Add("5KxGVgyXhSr3hYi6PFaJAmgknvFhrDcgG1fNe2EcUv3y3UY");

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
            tokenService = new TokenContract(CH, InitAccount, TokenContract);
            consensusService = new ConsensusContract(CH, InitAccount, ConsensusContract);
            dividendsService = new DividendsContract(CH, InitAccount, DividendsContract);
            voteService = new VoteContract(CH, InitAccount, VoteContract);
            electionService = new ElectionContract(CH, InitAccount, ElectionContract);
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
        private void QueryContractsBalance()
        {
            var consensusBalance = tokenService.CallViewMethod<GetBalanceOutput>(TokenMethod.GetBalance, new GetBalanceInput
            {
                Owner = Address.Parse(ConsensusContract),
                Symbol = TokenSymbol
            });
            Logger.WriteInfo($"Consensus account balance : {consensusBalance.Balance}");
            var dividendsResult = tokenService.CallViewMethod<GetBalanceOutput>(TokenMethod.GetBalance, new GetBalanceInput
            {
                Owner = Address.Parse(DividendsContract),
                Symbol = TokenSymbol
            });
            Logger.WriteInfo($"Dividends account balance : {dividendsResult.Balance}");
        }

        private void SetTokenFeeAddress()
        {
            tokenService.ExecuteMethodWithResult(TokenMethod.SetFeePoolAddress, Address.Parse(FeeAccount));
        }

        private void QueryTokenFeeBalance()
        {
            var feeBalance = tokenService.CallViewMethod<GetBalanceOutput>(TokenMethod.GetBalance, new GetBalanceInput
            {
                Owner = Address.Parse(FeeAccount),
                Symbol = TokenSymbol
            });
            Logger.WriteInfo($"Fee account balance : {feeBalance.Balance}");
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
                var callResult = tokenService.CallViewMethod<GetBalanceOutput>(TokenMethod.GetBalance, new GetBalanceInput
                {
                    Owner = Address.Parse(bpAcc),
                    Symbol = TokenSymbol
                });
                Console.WriteLine($"BpNode-[{bpAcc}] balance: " + callResult.Balance);
            }

            foreach (var fullAcc in FullNodeAccounts)
            {
                var callResult = tokenService.CallViewMethod<GetBalanceOutput>(TokenMethod.GetBalance,
                    new GetBalanceInput
                    {
                        Owner = Address.Parse(fullAcc),
                        Symbol = TokenSymbol
                    });
                Console.WriteLine($"FullNode-[{fullAcc}] balance: " + callResult.Balance);
            }
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
                var balanceResult = tokenService.CallViewMethod<GetBalanceOutput>(TokenMethod.GetBalance, new GetBalanceInput
                {
                    Owner = Address.Parse(fullAcc),
                    Symbol = TokenSymbol
                });
                if (balanceResult.Balance >= 100000)
                    continue;

                tokenService.ExecuteMethodWithResult(TokenMethod.Transfer, new TransferInput {
                    Memo = "transfer balance for announcement election.",
                    Amount = 100_000,
                    Symbol = TokenSymbol,
                    To = Address.Parse(fullAcc)
                });
            }
            //分配资金给BP
            Logger.WriteInfo("Allowance token to BpNode accounts");
            foreach (var bpAcc in BpNodeAccounts)
            {
                Logger.WriteInfo($"Account: {bpAcc}\nPubKey:{CH.GetPublicKeyFromAddress(bpAcc)}");
                var balanceResult = tokenService.CallViewMethod<GetBalanceOutput>(TokenMethod.GetBalance, new GetBalanceInput
                {
                    Owner = Address.Parse(bpAcc),
                    Symbol = TokenSymbol
                });
                if (balanceResult.Balance >= 100000)
                    continue;

                tokenService.ExecuteMethodWithResult(TokenMethod.Transfer, new TransferInput {
                    Memo = "transfer balance for announcement election.",
                    Amount = 100_000,
                    Symbol = TokenSymbol,
                    To = Address.Parse(bpAcc)
                });
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
                electionService.SetAccount(bpAcc);
                electionService.ExecuteMethodWithResult(ElectionMethod.AnnounceElection, new Empty());
            }

            foreach (var fullAcc in FullNodeAccounts)
            {
                electionService.SetAccount(fullAcc);
                electionService.ExecuteMethodWithResult(ElectionMethod.AnnounceElection, new Empty());
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
            //Get candidate event
            var voteEvent = voteService.CallViewMethod<VotingItem>(VoteMethod.GetVotingItem, new GetVotingItemInput
            {
                VotingItemId = Hash.Generate(), //need update item id
            });
            CandidatePublicKeys = voteEvent.Options.ToList();
           
            Assert.IsTrue(CandidatePublicKeys.Count == 0, "Candidate account is null.");
            var count = 1;
            foreach (var item in CandidatePublicKeys)
            {
                Logger.WriteInfo($"Candidate {count++}: {item}");
            }
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
                    UserList.Add(ci.InfoMsg?.ToString().Replace("Account address:", "").Trim());

                //unlock
                var uc = new CommandInfo("AccountUnlock", "account");
                uc.Parameter = $"{UserList[i]} 123 notimeout";
                CH.UnlockAccount(uc);
            }

            //分配资金给普通用户
            tokenService.SetAccount(BpNodeAccounts[0]);
            foreach (var acc in UserList)
            {
                tokenService.ExecuteMethodWithTxId(TokenMethod.Transfer, new TransferInput
                {
                    Amount = 100_000,
                    Memo = "",
                    Symbol = TokenSymbol,
                    To = Address.Parse(acc) 
                });
            }
            tokenService.CheckTransactionResultList();

            foreach (var userAcc in UserList)
            {
                var callResult = tokenService.CallViewMethod<GetBalanceOutput>(TokenMethod.GetBalance, new GetBalanceInput
                {
                    Symbol = TokenSymbol,
                    Owner = Address.Parse(userAcc)
                });
                Console.WriteLine($"User-{userAcc} balance: " + callResult.Balance);
            }

            Logger.WriteInfo("All accounts created and unlocked.");
        }


        [TestMethod]
        [DataRow(
            "04d5a0ab908b1e6a99be1d4b1d5e4ab7c3bd3b234f714d674a1aad7dc462436b0345cb6384b589a5be0aa6bc9c8a78ebb10e5d0a865deade3fc48b446075b26cb3")]
        public void UserVoteForCandidate(string pubKey)
        {
            PrepareUserAccountAndBalance(1);
           const int voteVolume = 100;

            voteService.SetAccount(UserList[0]);
            voteService.ExecuteMethodWithResult(VoteMethod.Vote, new VoteInput 
            {
                Amount = voteVolume,
                
            });

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

                    voteService.SetAccount(voteAcc);
                    voteService.ExecuteMethodWithTxId(VoteMethod.Vote, new VoteInput
                    {
                      //votePbk, voteVolumn, voteLock  
                    });
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

                var votePbk = NodesPublicKeys[candidates[i]];
                var voteVolume = 200;

                voteService.SetAccount(UserList[i]);
                voteService.ExecuteMethodWithTxId(VoteMethod.Vote, new VoteMinerInput
                {
                    CandidatePublicKey = votePbk,
                    Amount = voteVolume,
                    EndTimestamp = DateTime.UtcNow.Add(TimeSpan.FromDays(90)).ToTimestamp()
                });
                Logger.WriteInfo($"Vote action: User: {UserList[i]}, Tickets: {voteVolume}");
            }

            consensusService.CheckTransactionResultList();
            //检查投票结果
            GetPageableElectionInfo();
            Logger.WriteInfo("Vote completed.");
        }
        
        [TestMethod]
        public void GetCandidateHistoryInfo()
        {
            Logger.WriteInfo("GetCandidateHistoryInfo Test");

            GetCandidateList();
            foreach (var pubKey in CandidatePublicKeys)
            {
                var historyResult = consensusService.CallViewMethod<CandidateInHistory>(ConsensusMethod.GetCandidateHistoryInformation, new PublicKey
                {
                    Hex = pubKey
                });
                Logger.WriteInfo(historyResult.ToString());
            }
        }

        [TestMethod]
        public void GetGetCurrentMinersInfo()
        {
            Logger.WriteInfo("GetCurrentVictories Test");
            var minersResult = consensusService.CallViewMethod<MinerListWithRoundNumber>(ConsensusMethod.GetCurrentMiners, new Empty());
            CurrentMinersKeys = minersResult.MinerList.PublicKeys.Select(o=>o.ToByteArray().ToHex()).ToList();
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
                var ticketResult = consensusService.CallViewMethod<Tickets>(ConsensusMethod.GetTicketsInfo, new PublicKey
                {
                    Hex = candidate
                });
                Logger.WriteInfo(ticketResult.ToString());

                Logger.WriteInfo($"Candidate: {candidate}, Tickets: {ticketResult.VotedTickets}");
            }
        }

        [TestMethod]
        [DataRow("04d5a0ab908b1e6a99be1d4b1d5e4ab7c3bd3b234f714d674a1aad7dc462436b0345cb6384b589a5be0aa6bc9c8a78ebb10e5d0a865deade3fc48b446075b26cb3")]
        public void GetCandidateTicketsInfo(string candidatePublicKey)
        {
            Logger.WriteInfo("GetCandidateTicketsInfo Test");
            var ticketResult = consensusService.CallViewMethod<Tickets>(ConsensusMethod.GetTicketsInfo, new PublicKey
            {
                Hex = candidatePublicKey
            });
            Logger.WriteInfo(ticketResult.ToString());
        }

        [TestMethod]
        public void GetPageableElectionInfo()
        {
            Logger.WriteInfo("GetCurrentElectionInfo Test");
            var currentElectionResult = consensusService.CallViewMethod<TicketsDictionary>(ConsensusMethod.GetPageableElectionInfo, new PageableElectionInfoInput
            {
                Length = 10,
                OrderBy = 0,
                Start = 0
            });
            Logger.WriteInfo(currentElectionResult.Maps.ToString());
        }

        [TestMethod]
        public void GetVotesCount()
        {
            var voteCount = consensusService.CallViewMethod<SInt64Value>(ConsensusMethod.GetVotesCount, new Empty());
            Logger.WriteInfo($"Votes count: {voteCount.Value}");
        }

        [TestMethod]
        public void GetTicketsCount()
        {
            UserVoteAction(5, 1);
            var ticketsCount = consensusService.CallViewMethod<SInt64Value>(ConsensusMethod.GetTicketsCount, new Empty());
            Logger.WriteInfo($"Tickets count: {ticketsCount.Value}");
        }

        [TestMethod]
        public void GetCurrentVictories()
        {
            Logger.WriteInfo("GetCurrentVictories Test");
            var victoriesResult = consensusService.CallViewMethod<StringList>(ConsensusMethod.GetCurrentVictories, new Empty());
            Logger.WriteInfo(victoriesResult.Values.ToString());
        }

        [TestMethod]
        public void QueryDividends()
        {
            UserVoteAction(5, 1);
            Thread.Sleep(30000);
            foreach (var userAcc in UserList)
            {
                Logger.WriteInfo($"Account check: {userAcc}");
                var balanceBefore = tokenService.CallViewMethod<GetBalanceOutput>(TokenMethod.GetBalance, new GetBalanceInput
                {
                    Symbol = TokenSymbol,
                    Owner = Address.Parse(userAcc)
                });
                Logger.WriteInfo($"Init balance: {balanceBefore.Balance}");

                consensusService.SetAccount(userAcc);
                //consensusService.CallContractMethod(ConsensusMethod.ReceiveAllDividends);

                var balanceAfter1 = tokenService.CallViewMethod<GetBalanceOutput>(TokenMethod.GetBalance, new GetBalanceInput
                {
                    Symbol = TokenSymbol,
                    Owner = Address.Parse(userAcc)
                });
                Logger.WriteInfo($"Received dividends balance: {balanceAfter1.Balance}");

                //consensusService.CallContractMethod(ConsensusMethod.WithdrawAll, "true");

                var balanceAfter2 = tokenService.CallViewMethod<GetBalanceOutput>(TokenMethod.GetBalance, new GetBalanceInput
                {
                    Symbol = TokenSymbol,
                    Owner = Address.Parse(userAcc)
                });
                Logger.WriteInfo($"Revert back vote balance: {balanceAfter2.Balance}");
            }
        }

        [TestMethod]
        public void QueryMinedBlockCountInCurrentTerm()
        {
            GetGetCurrentMinersInfo();
            foreach (var miner in CurrentMinersKeys)
            {
                var blockResult = consensusService.CallViewMethod<SInt64Value>(ConsensusMethod.QueryMinedBlockCountInCurrentTerm, new PublicKey
                {
                    Hex = miner
                });
                Logger.WriteInfo($"Generate blocks count: {blockResult.Value}");
            }
        }

        //退出选举
        [TestMethod]
        public void QuitElection()
        {
            electionService.SetAccount(FullNodeAccounts[0]);
            electionService.ExecuteMethodWithTxId(ElectionMethod.QuitElection, new Empty());

            electionService.SetAccount(FullNodeAccounts[1]);
            electionService.ExecuteMethodWithTxId(ElectionMethod.QuitElection, new Empty());

            electionService.SetAccount(FullNodeAccounts[2]);
            electionService.ExecuteMethodWithTxId(ElectionMethod.QuitElection, new Empty());
            
            electionService.CheckTransactionResultList();
            GetCandidateList();

            //查询余额
            foreach (var fullAcc in FullNodeAccounts)
            {
                var callResult = tokenService.CallViewMethod<GetBalanceOutput>(TokenMethod.GetBalance, new GetBalanceInput
                {
                    Symbol = TokenSymbol,
                    Owner = Address.Parse(fullAcc)
                });
                Console.WriteLine($"FullNode token-{fullAcc}: " + callResult.Balance);
            }
        }


        [TestMethod]
        public void ReAnnounceElection()
        {
            electionService.SetAccount(FullNodeAccounts[0]);
            electionService.ExecuteMethodWithTxId(ElectionMethod.AnnounceElection, new Empty());

            electionService.SetAccount(FullNodeAccounts[1]);
            electionService.ExecuteMethodWithTxId(ElectionMethod.AnnounceElection, new Empty());

            electionService.SetAccount(FullNodeAccounts[2]);
            electionService.ExecuteMethodWithTxId(ElectionMethod.AnnounceElection, new Empty());
            
            electionService.CheckTransactionResultList();
            
            GetCandidateList();

            //查询余额
            foreach (var fullAcc in FullNodeAccounts)
            {
                var callResult = tokenService.CallViewMethod<GetBalanceOutput>(TokenMethod.GetBalance, new GetBalanceInput
                {
                    Owner = Address.Parse(fullAcc),
                    Symbol = TokenSymbol
                });
                Console.WriteLine($"FullNode token-{fullAcc}: " + callResult.Balance);
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
        */
    }
}
