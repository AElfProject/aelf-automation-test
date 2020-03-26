using System.Collections.Generic;
using System.IO;
using AElfChain.Common;
using AElfChain.Common.Contracts;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using log4net;
using Microsoft.VisualStudio.TestTools.UnitTesting;

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
            for (var i = 0; i < BpNodeAccounts.Count; i++)
            {
                var name = $"Bp-{i + 1}";
                var account = BpNodeAccounts[i];
                var pubKey = CH.GetAccountPublicKey(account);
                NodesPublicKeys.Add(pubKey);
                Logger.Info($"{account}: {pubKey}");
                CandidateInfos.Add(new CandidateInfo {Name = name, Account = account, PublicKey = pubKey});
            }

            for (var i = 0; i < FullNodeAccounts.Count; i++)
            {
                var name = $"Full-{i + 1}";
                var account = FullNodeAccounts[i];
                var pubKey = CH.GetAccountPublicKey(account);
                NodesPublicKeys.Add(pubKey);
                Logger.Info($"{account}: {pubKey}");
                CandidateInfos.Add(new CandidateInfo {Name = name, Account = account, PublicKey = pubKey});
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
            Logger.Info("Delete all account files created.");
            foreach (var item in UserList)
            {
                var file = Path.Combine(CommonHelper.GetCurrentDataDir(), $"{item}.json");
                File.Delete(file);
            }
        }

        #region Priority

        public ILog Logger = Log4NetHelper.GetLogger();
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

        public string TokenSymbol { get; } = NodeOption.NativeTokenSymbol;
        public string InitAccount { get; } = "ELF_2GkD1q74HwBrFsHufmnCKHJvaGVBYkmYcdG3uebEsAWSspX";
        public string FeeAccount { get; } = "ELF_1dVay78LmRRzP7ymunFsBJFT8frYK4hLNjUCBi4VWa2KmZ";

        //Contract service List
        public static TokenContract tokenService { get; set; }
        public static ConsensusContract consensusService { get; set; }
        public static DividendsContract dividendsService { get; set; }
        public static ElectionContract electionService { get; set; }
        public static VoteContract voteService { get; set; }

        public string RpcUrl { get; } = "http://192.168.197.13:8100/chain";
        public INodeManager CH { get; set; }

        #endregion

        /*
        private void QueryContractsBalance()
        {
            var consensusBalance = tokenService.CallViewMethod<GetBalanceOutput>(TokenMethod.GetBalance, new GetBalanceInput
            {
                Owner = AddressHelper.Base58StringToAddress(ConsensusContract),
                Symbol = NativeTokenSymbol
            });
            Logger.Info($"Consensus account balance : {consensusBalance.Balance}");
            var dividendsResult = tokenService.CallViewMethod<GetBalanceOutput>(TokenMethod.GetBalance, new GetBalanceInput
            {
                Owner = AddressHelper.Base58StringToAddress(DividendsContract),
                Symbol = NativeTokenSymbol
            });
            Logger.Info($"Dividends account balance : {dividendsResult.Balance}");
        }

        private void SetTokenFeeAddress()
        {
            tokenService.ExecuteMethodWithResult(TokenMethod.SetFeePoolAddress, AddressHelper.Base58StringToAddress(FeeAccount));
        }

        private void QueryTokenFeeBalance()
        {
            var feeBalance = tokenService.CallViewMethod<GetBalanceOutput>(TokenMethod.GetBalance, new GetBalanceInput
            {
                Owner = AddressHelper.Base58StringToAddress(FeeAccount),
                Symbol = NativeTokenSymbol
            });
            Logger.Info($"Fee account balance : {feeBalance.Balance}");
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
            //Query balance
            foreach (var bpAcc in BpNodeAccounts)
            {
                var callResult = tokenService.CallViewMethod<GetBalanceOutput>(TokenMethod.GetBalance, new GetBalanceInput
                {
                    Owner = AddressHelper.Base58StringToAddress(bpAcc),
                    Symbol = NativeTokenSymbol
                });
                Console.WriteLine($"BpNode-[{bpAcc}] balance: " + callResult.Balance);
            }

            foreach (var fullAcc in FullNodeAccounts)
            {
                var callResult = tokenService.CallViewMethod<GetBalanceOutput>(TokenMethod.GetBalance,
                    new GetBalanceInput
                    {
                        Owner = AddressHelper.Base58StringToAddress(fullAcc),
                        Symbol = NativeTokenSymbol
                    });
                Console.WriteLine($"FullNode-[{fullAcc}] balance: " + callResult.Balance);
            }
        }

        [TestMethod]
        [DataRow("ELF_6HC6tx7kPguUhCFWeoVQfEJiv5Tfw4itrEgMPNT5ujsV2Vz")]
        public void QueryPublicKey(string account, string password="")
        {
            var pubKey = CH.GetAccountPublicKey(account, password);
            Logger.Info($"PubKey: {pubKey}");
        }

        [TestMethod]
        public void PrepareCandidateAsset()
        {
            consensusService.SetAccount(BpNodeAccounts[0]);

            //allocation balance from bp to full node
            Logger.Info("Allowance token to FullNode accounts");
            foreach (var fullAcc in FullNodeAccounts)
            {
                Logger.Info($"Account: {fullAcc}\nPubKey:{CH.GetAccountPublicKey(fullAcc)}");
                var balanceResult = tokenService.CallViewMethod<GetBalanceOutput>(TokenMethod.GetBalance, new GetBalanceInput
                {
                    Owner = AddressHelper.Base58StringToAddress(fullAcc),
                    Symbol = NativeTokenSymbol
                });
                if (balanceResult.Balance >= 100000)
                    continue;

                tokenService.ExecuteMethodWithResult(TokenMethod.Transfer, new TransferInput {
                    Memo = "transfer balance for announcement election.",
                    Amount = 100_000,
                    Symbol = NativeTokenSymbol,
                    To = AddressHelper.Base58StringToAddress(fullAcc)
                });
            }
            //allocation balance to bp
            Logger.Info("Allowance token to BpNode accounts");
            foreach (var bpAcc in BpNodeAccounts)
            {
                Logger.Info($"Account: {bpAcc}\nPubKey:{CH.GetAccountPublicKey(bpAcc)}");
                var balanceResult = tokenService.CallViewMethod<GetBalanceOutput>(TokenMethod.GetBalance, new GetBalanceInput
                {
                    Owner = AddressHelper.Base58StringToAddress(bpAcc),
                    Symbol = NativeTokenSymbol
                });
                if (balanceResult.Balance >= 100000)
                    continue;

                tokenService.ExecuteMethodWithResult(TokenMethod.Transfer, new TransferInput {
                    Memo = "transfer balance for announcement election.",
                    Amount = 100_000,
                    Symbol = NativeTokenSymbol,
                    To = AddressHelper.Base58StringToAddress(bpAcc)
                });
            }

            consensusService.CheckTransactionResultList();

            QueryCandidatesBalance();

            Logger.Info("All accounts asset prepared completed.");
        }

        //attend election
        [TestMethod]
        public void JoinElection()
        {
            PrepareCandidateAsset();

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

            QueryCandidatesBalance();

            GetCandidateList();

            Logger.Info("All Full Node joined election completed.");
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
                Logger.Info($"Candidate {count++}: {item}");
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

            //allocation balance to common tester
            tokenService.SetAccount(BpNodeAccounts[0]);
            foreach (var acc in UserList)
            {
                tokenService.ExecuteMethodWithTxId(TokenMethod.Transfer, new TransferInput
                {
                    Amount = 100_000,
                    Memo = "",
                    Symbol = NativeTokenSymbol,
                    To = AddressHelper.Base58StringToAddress(acc) 
                });
            }
            tokenService.CheckTransactionResultList();

            foreach (var userAcc in UserList)
            {
                var callResult = tokenService.CallViewMethod<GetBalanceOutput>(TokenMethod.GetBalance, new GetBalanceInput
                {
                    Symbol = NativeTokenSymbol,
                    Owner = AddressHelper.Base58StringToAddress(userAcc)
                });
                Console.WriteLine($"User-{userAcc} balance: " + callResult.Balance);
            }

            Logger.Info("All accounts created and unlocked.");
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

            //check election result
            GetPageableElectionInfo();
            GetTicketsInfo();
            Logger.Info("Vote completed.");
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
                Logger.Info($"Vote action: User: {UserList[i]}, Tickets: {voteVolume}");
            }

            consensusService.CheckTransactionResultList();
            GetPageableElectionInfo();
            Logger.Info("Vote completed.");
        }
        
        [TestMethod]
        public void GetCandidateHistoryInfo()
        {
            Logger.Info("GetCandidateHistoryInfo Test");

            GetCandidateList();
            foreach (var pubKey in CandidatePublicKeys)
            {
                var historyResult = consensusService.CallViewMethod<CandidateInHistory>(ConsensusMethod.GetCandidateHistoryInformation, new PublicKey
                {
                    Hex = pubKey
                });
                Logger.Info(historyResult.ToString());
            }
        }

        [TestMethod]
        public void GetGetCurrentMinersInfo()
        {
            Logger.Info("GetCurrentVictories Test");
            var minersResult = consensusService.CallViewMethod<MinerListWithRoundNumber>(ConsensusMethod.GetCurrentMinersPubkey, new Empty());
            CurrentMinersKeys = minersResult.MinerList.PublicKeys.Select(o=>o.ToByteArray().ToHex()).ToList();
            var count = 1;
            foreach (var miner in CurrentMinersKeys)
            {
                Logger.Info($"Miner {count++}: {miner}");
            }
        }

        [TestMethod]
        public void GetTicketsInfo()
        {
            Logger.Info("GetTicketsInfo Test");
            GetCandidateList();
            foreach (var candidate in CandidatePublicKeys)
            {
                var ticketResult = consensusService.CallViewMethod<Tickets>(ConsensusMethod.GetTicketsInfo, new PublicKey
                {
                    Hex = candidate
                });
                Logger.Info(ticketResult.ToString());

                Logger.Info($"Candidate: {candidate}, Tickets: {ticketResult.VotedTickets}");
            }
        }

        [TestMethod]
        [DataRow("04d5a0ab908b1e6a99be1d4b1d5e4ab7c3bd3b234f714d674a1aad7dc462436b0345cb6384b589a5be0aa6bc9c8a78ebb10e5d0a865deade3fc48b446075b26cb3")]
        public void GetCandidateTicketsInfo(string candidatePublicKey)
        {
            Logger.Info("GetCandidateTicketsInfo Test");
            var ticketResult = consensusService.CallViewMethod<Tickets>(ConsensusMethod.GetTicketsInfo, new PublicKey
            {
                Hex = candidatePublicKey
            });
            Logger.Info(ticketResult.ToString());
        }

        [TestMethod]
        public void GetPageableElectionInfo()
        {
            Logger.Info("GetCurrentElectionInfo Test");
            var currentElectionResult = consensusService.CallViewMethod<TicketsDictionary>(ConsensusMethod.GetPageableElectionInfo, new PageableElectionInfoInput
            {
                Length = 10,
                OrderBy = 0,
                Start = 0
            });
            Logger.Info(currentElectionResult.Maps.ToString());
        }

        [TestMethod]
        public void GetVotesCount()
        {
            var voteCount = consensusService.CallViewMethod<Int64Value>(ConsensusMethod.GetVotesCount, new Empty());
            Logger.Info($"Votes count: {voteCount.Value}");
        }

        [TestMethod]
        public void GetTicketsCount()
        {
            UserVoteAction(5, 1);
            var ticketsCount = consensusService.CallViewMethod<Int64Value>(ConsensusMethod.GetTicketsCount, new Empty());
            Logger.Info($"Tickets count: {ticketsCount.Value}");
        }

        [TestMethod]
        public void GetCurrentVictories()
        {
            Logger.Info("GetCurrentVictories Test");
            var victoriesResult = consensusService.CallViewMethod<StringList>(ConsensusMethod.GetCurrentVictories, new Empty());
            Logger.Info(victoriesResult.Values.ToString());
        }

        [TestMethod]
        public void QueryDividends()
        {
            UserVoteAction(5, 1);
            Thread.Sleep(30000);
            foreach (var userAcc in UserList)
            {
                Logger.Info($"Account check: {userAcc}");
                var balanceBefore = tokenService.CallViewMethod<GetBalanceOutput>(TokenMethod.GetBalance, new GetBalanceInput
                {
                    Symbol = NativeTokenSymbol,
                    Owner = AddressHelper.Base58StringToAddress(userAcc)
                });
                Logger.Info($"Init balance: {balanceBefore.Balance}");

                consensusService.SetAccount(userAcc);
                //consensusService.CallContractMethod(ConsensusMethod.ReceiveAllDividends);

                var balanceAfter1 = tokenService.CallViewMethod<GetBalanceOutput>(TokenMethod.GetBalance, new GetBalanceInput
                {
                    Symbol = NativeTokenSymbol,
                    Owner = AddressHelper.Base58StringToAddress(userAcc)
                });
                Logger.Info($"Received dividends balance: {balanceAfter1.Balance}");

                //consensusService.CallContractMethod(ConsensusMethod.WithdrawAll, "true");

                var balanceAfter2 = tokenService.CallViewMethod<GetBalanceOutput>(TokenMethod.GetBalance, new GetBalanceInput
                {
                    Symbol = NativeTokenSymbol,
                    Owner = AddressHelper.Base58StringToAddress(userAcc)
                });
                Logger.Info($"Revert back vote balance: {balanceAfter2.Balance}");
            }
        }

        [TestMethod]
        public void QueryMinedBlockCountInCurrentTerm()
        {
            GetGetCurrentMinersInfo();
            foreach (var miner in CurrentMinersKeys)
            {
                var blockResult = consensusService.CallViewMethod<Int64Value>(ConsensusMethod.QueryMinedBlockCountInCurrentTerm, new PublicKey
                {
                    Hex = miner
                });
                Logger.Info($"Generate blocks count: {blockResult.Value}");
            }
        }

        //quit election
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

            foreach (var fullAcc in FullNodeAccounts)
            {
                var callResult = tokenService.CallViewMethod<GetBalanceOutput>(TokenMethod.GetBalance, new GetBalanceInput
                {
                    Symbol = NativeTokenSymbol,
                    Owner = AddressHelper.Base58StringToAddress(fullAcc)
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

            foreach (var fullAcc in FullNodeAccounts)
            {
                var callResult = tokenService.CallViewMethod<GetBalanceOutput>(TokenMethod.GetBalance, new GetBalanceInput
                {
                    Owner = AddressHelper.Base58StringToAddress(fullAcc),
                    Symbol = NativeTokenSymbol
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

            GetCandidateHistoryInfo();
            GetGetCurrentMinersInfo();
            GetTicketsInfo();
            GetCurrentVictories();
            QueryDividends();

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