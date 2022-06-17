using System.Collections.Generic;
using System.Linq;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Contracts.Election;
using AElf.Contracts.Profit;
using AElf.Standards.ACS3;
using AElfChain.Common.Contracts;
using AElf.Types;
using AElfChain.Common.DtoExtension;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Shouldly;
using Volo.Abp.Threading;

namespace AElf.Automation.EconomicSystemTest
{
    [TestClass]
    public class NodeTests : ElectionTests
    {
        [TestInitialize]
        public void InitializeNodeTests()
        {
            Initialize();
        }

        [TestMethod]
        [DataRow(0)]
        [DataRow(1)]
        public void AnnouncementNode(int index)
        {
            var account = FullNodeAddress[index];
            Behaviors.TransferToken(InitAccount, account, 10_1000_00000000);
            var result = Behaviors.AnnouncementElection(account,account);
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
        }

        [TestMethod]
        public void NodeAnnounceElectionAction()
        {
            var term = Behaviors.ConsensusService.GetCurrentTermInformation();
            Logger.Info($"Term: {term.TermNumber}");
            foreach (var user in FullNodeAddress)
            {
                // Behaviors.TransferToken(InitAccount, user, 10_1000_00000000);
                var election = Behaviors.ElectionService.GetNewTester(user);
                var parliament = Behaviors.ParliamentService.GetGenesisOwnerAddress();
                var electionResult = election.ExecuteMethodWithResult(ElectionMethod.AnnounceElection, parliament);
                electionResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            }

            var candidateList = Behaviors.GetCandidates();
            Logger.Info($"{candidateList.Value.Count}");
            foreach (var publicKey in candidateList.Value)
                Logger.Info($"Candidate PublicKey: {publicKey.ToByteArray().ToHex()}");
        }

        [TestMethod]
        public void SetAdmin()
        {
            var newAdmin = Behaviors.ParliamentService.GetGenesisOwnerAddress();
            foreach (var full in BpNodeAddress)
            {
//            var full = BpNodeAddress[1];
                var pubkey = Behaviors.NodeManager.GetAccountPublicKey(full);
                var admin = Behaviors.ElectionService.GetCandidateAdmin(pubkey);

                if (admin.Equals(new Address()))
                {
                    admin = full.ConvertAddress();
                }

                Behaviors.ElectionService.SetAccount(admin.ToBase58());
                var result = Behaviors.ElectionService.ExecuteMethodWithResult(ElectionMethod.SetCandidateAdmin,
                    new SetCandidateAdminInput
                    {
                        Admin = admin,
                        Pubkey = pubkey
                    });
                result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
                var checkAdmin = Behaviors.ElectionService.GetCandidateAdmin(pubkey);
                checkAdmin.ShouldBe(admin);
            }
        }

        [TestMethod]
        public void SetAdmin_ThroughParliament()
        {
            var account = BpNodeAddress[0];
            var pubkey = Behaviors.NodeManager.GetAccountPublicKey(account);
            var admin = Behaviors.ElectionService.GetCandidateAdmin(pubkey);
            var organization = Behaviors.ParliamentService.GetGenesisOwnerAddress();
            var newAdmin = Behaviors.ParliamentService.GetGenesisOwnerAddress();
            var input = new SetCandidateAdminInput
            {
                Admin = newAdmin,
                Pubkey = pubkey
            };
            var result = AuthorityManager.ExecuteTransactionWithAuthority(Behaviors.ElectionService.ContractAddress,
                nameof(ElectionMethod.SetCandidateAdmin), input, InitAccount, organization);
            result.Status.ShouldBe(TransactionResultStatus.Mined);
            var checkAdmin = Behaviors.ElectionService.GetCandidateAdmin(pubkey);
            checkAdmin.ShouldBe(newAdmin);
        }

        [TestMethod]
        public void ReplacePubkey()
        {
            Behaviors.ElectionService.SetAccount(BpNodeAddress[1]);
            var oldPubkey = Behaviors.NodeManager.GetAccountPublicKey(BpNodeAddress[1]);
            var newPubkey = Behaviors.NodeManager.GetAccountPublicKey(ReplaceAddress[0]);
            Logger.Info($"{oldPubkey}");
            var result = Behaviors.ElectionService.ExecuteMethodWithResult(ElectionMethod.ReplaceCandidatePubkey,
                new ReplaceCandidatePubkeyInput
                {
                    OldPubkey = oldPubkey,
                    NewPubkey = newPubkey
                });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var checkKey = Behaviors.ElectionService.GetNewestPubkey(oldPubkey);
            checkKey.ShouldBe(newPubkey);
        }

        [TestMethod]
        public void ReplacePubkey_throughParliament()
        {
            var account = BpNodeAddress[4];
            var newAccount = ReplaceAddress[4];
            var oldPubkey = Behaviors.NodeManager.GetAccountPublicKey(account);
            var newPubkey = Behaviors.NodeManager.GetAccountPublicKey(newAccount);
            Logger.Info($"{oldPubkey}");
            Logger.Info($"{newPubkey}");


            var pubkey = Behaviors.NodeManager.GetAccountPublicKey(account);
            var admin = Behaviors.ElectionService.GetCandidateAdmin(pubkey);
            Logger.Info($"{admin}");
            var input = new ReplaceCandidatePubkeyInput
            {
                OldPubkey = oldPubkey,
                NewPubkey = newPubkey
            };
            var result = AuthorityManager.ExecuteTransactionWithAuthority(Behaviors.ElectionService.ContractAddress,
                nameof(ElectionMethod.ReplaceCandidatePubkey), input, InitAccount, admin);
            result.Status.ShouldBe(TransactionResultStatus.Mined);
            var checkKey = Behaviors.ElectionService.GetNewestPubkey(oldPubkey);
            checkKey.ShouldBe(newPubkey);

            var replacedKey = Behaviors.ElectionService.GetReplacedPubkey(newPubkey);
            replacedKey.ShouldBe(oldPubkey);
        }

        [TestMethod]
        public void Quit_throughParliament()
        {
            var quitAccount = FullNodeAddress[2];
            var balanceOfAccount = Behaviors.TokenService.GetUserBalance(quitAccount);
            var quitPubkey = Behaviors.NodeManager.GetAccountPublicKey(quitAccount);
            Logger.Info($"{quitPubkey}");
            var admin = Behaviors.ElectionService.GetCandidateAdmin(quitPubkey);
            Logger.Info($"{admin}");

            var input = new StringValue {Value = quitPubkey};
            var result = AuthorityManager.ExecuteTransactionWithAuthority(Behaviors.ElectionService.ContractAddress,
                nameof(ElectionMethod.QuitElection), input, InitAccount, admin);
            result.Status.ShouldBe(TransactionResultStatus.Mined);

            var afterBalance = Behaviors.TokenService.GetUserBalance(quitAccount);
            afterBalance.ShouldBe(balanceOfAccount + 10_0000_00000000);
        }

        [TestMethod]
        public void EnableElection()
        {
            var parliament = Behaviors.ParliamentService.GetGenesisOwnerAddress();
            var result = AuthorityManager.ExecuteTransactionWithAuthority(Behaviors.ElectionService.ContractAddress,
                nameof(ElectionMethod.EnableElection), new Empty(), InitAccount, parliament);
            result.Status.ShouldBe(TransactionResultStatus.Mined);
        }
        
        [TestMethod]
        public void Transfer()
        {
            for (var i = 0; i < ReplaceAddress.Count; i++)
            {
                var newBalance = Behaviors.TokenService.GetUserBalance(ReplaceAddress[i]);
                Logger.Info($"{ReplaceAddress[i]} : {newBalance}");
            }
        }

        [TestMethod]
        public void SetMaximumMinersCount()
        {
            var amount = 5;
            var maximumBlocksCount = Behaviors.ConsensusService.GetMaximumMinersCount().Value;
            Logger.Info($"{maximumBlocksCount}");
            var consensus = Behaviors.ConsensusService;
            var input = new Int32Value {Value = amount};
            var result = Behaviors.AuthorityManager.ExecuteTransactionWithAuthority(consensus.ContractAddress,
                nameof(ConsensusMethod.SetMaximumMinersCount), input, InitAccount);
            result.Status.ShouldBe(TransactionResultStatus.Mined);
            var newMaximumBlocksCount = Behaviors.ConsensusService.GetMaximumMinersCount().Value;
            newMaximumBlocksCount.ShouldBe(maximumBlocksCount < amount ? maximumBlocksCount : amount);
            Logger.Info($"{newMaximumBlocksCount}");
        }

        [TestMethod]
        public void SetMaximumMinersCountThroughAssociation()
        {
            var amount = 4;
            var maximumBlocksCount = Behaviors.ConsensusService.GetMaximumMinersCount().Value;
            Logger.Info($"{maximumBlocksCount}");
            var consensus = Behaviors.ConsensusService;
            var association = Behaviors.ContractManager.Association;
            var maximumMinersCountController = Behaviors.ConsensusService.GetMaximumMinersCountController();
            var associationOrganization = maximumMinersCountController.OwnerAddress;
            var input = new Int32Value {Value = amount};
            var proposer = association.GetOrganization(associationOrganization).ProposerWhiteList.Proposers.First();
            var proposalId = association.CreateProposal(consensus.ContractAddress,
                nameof(ConsensusMethod.SetMaximumMinersCount), input, associationOrganization,
                proposer.ToBase58());
            association.ApproveWithAssociation(proposalId, associationOrganization);
            var release = association.ReleaseProposal(proposalId, proposer.ToBase58());
            release.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            maximumBlocksCount = Behaviors.ConsensusService.GetMaximumMinersCount().Value;
            maximumBlocksCount.ShouldBe(amount);
        }

        [TestMethod]
        public void ChangeMaximumMinersCountController()
        {
            var parliament = Behaviors.ContractManager.Parliament;
            var association = Behaviors.ContractManager.Association;
            var consensus = Behaviors.ConsensusService;
            var genesisOwnerAddress = parliament.GetGenesisOwnerAddress();
            var maximumMinersCountController = Behaviors.ConsensusService.GetMaximumMinersCountController();
            var oldInput = new AuthorityInfo
            {
                ContractAddress = parliament.Contract,
                OwnerAddress = genesisOwnerAddress
            };
            var associationOrganization = Behaviors.AuthorityManager.CreateAssociationOrganization();
            var input = new AuthorityInfo
            {
                ContractAddress = association.Contract,
                OwnerAddress = associationOrganization
            };
            if (maximumMinersCountController.ContractAddress.Equals(parliament.Contract))
            {
                var result = Behaviors.AuthorityManager.ExecuteTransactionWithAuthority(consensus.ContractAddress,
                    nameof(ConsensusMethod.ChangeMaximumMinersCountController), input, InitAccount);
                result.Status.ShouldBe(TransactionResultStatus.Mined);
                maximumMinersCountController = Behaviors.ConsensusService.GetMaximumMinersCountController();
                maximumMinersCountController.ContractAddress.ShouldBe(association.Contract);
                maximumMinersCountController.OwnerAddress.ShouldBe(associationOrganization);
            }
            else if (maximumMinersCountController.ContractAddress.Equals(association.Contract))
            {
                var proposer = association.GetOrganization(associationOrganization).ProposerWhiteList.Proposers.First();
                var proposalId = association.CreateProposal(consensus.ContractAddress,
                    nameof(ConsensusMethod.ChangeMaximumMinersCountController), oldInput, associationOrganization,
                    proposer.ToBase58());
                association.ApproveWithAssociation(proposalId, associationOrganization);
                var release = association.ReleaseProposal(proposalId, proposer.ToBase58());
                release.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
                maximumMinersCountController = Behaviors.ConsensusService.GetMaximumMinersCountController();
                maximumMinersCountController.ContractAddress.ShouldBe(parliament.Contract);
                maximumMinersCountController.OwnerAddress.ShouldBe(genesisOwnerAddress);
            }
        }

        //bEAS9mHqZVzP5tsDKRqj1JC5FwFVg4b3TcfjtEtauWC9wbApY
        [TestMethod]
        public void CreateEmergencyResponseOrganization()
        {
            var result = AuthorityManager.ExecuteTransactionWithAuthority(Behaviors.ParliamentService.ContractAddress,
                "CreateEmergencyResponseOrganization", new Empty(), InitAccount);
            result.Status.ShouldBe(TransactionResultStatus.Mined);
            var logEvent = result.Logs.First(l => l.Name.Equals(nameof(OrganizationCreated))).NonIndexed;
            var organization = OrganizationCreated.Parser.ParseFrom(logEvent);
            Logger.Info(organization);

            var getOrganization = Behaviors.ParliamentService.GetEmergencyResponseOrganizationAddress();
            getOrganization.ShouldBe(organization.OrganizationAddress);
        }

        [TestMethod]
        public void RemoveNode()
        {
            GetMiners();

            var account = BpNodeAddress[3];
            var approveList = GetCurrentMiners();
            var result = AuthorityManager.ExecuteTransactionWithAuthority(Behaviors.ElectionService.ContractAddress,
                "RemoveEvilNode", new StringValue {Value = NodeManager.GetAccountPublicKey(account)},
                "bEAS9mHqZVzP5tsDKRqj1JC5FwFVg4b3TcfjtEtauWC9wbApY".ConvertAddress(), approveList, InitAccount);
            result.Status.ShouldBe(TransactionResultStatus.Mined);

            GetMiners();
        }


        [TestMethod]
        public void GetMaximumMinersCount()
        {
            var maximumMinersCount = Behaviors.ConsensusService.GetMaximumMinersCount().Value;
            Logger.Info($"{maximumMinersCount}");
        }

        [TestMethod]
        public void GetMaximumBlocksCount()
        {
            var blocksCount = Behaviors.ConsensusService.GetMaximumBlocksCount();
            Logger.Info(blocksCount);
        }

        [TestMethod]
        public void GetMiners()
        {
            var termNumber =
                Behaviors.ConsensusService.CallViewMethod<Int64Value>(ConsensusMethod.GetCurrentTermNumber,
                    new Empty()).Value;
            var candidateList = Behaviors.GetCandidates();
            var voteMessage =
                $"TermNumber={termNumber}, candidates count is {candidateList.Value.Count}";
            Logger.Info(voteMessage);

            GetCurrentMiners();
        }

        [TestMethod]
        public void GetVoteStatus()
        {
            var termNumber =
                Behaviors.ConsensusService.CallViewMethod<Int64Value>(ConsensusMethod.GetCurrentTermNumber,
                    new Empty()).Value;
            var candidateList = Behaviors.GetCandidates();
            var voteMessage =
                $"TermNumber={termNumber}, candidates count is {candidateList.Value.Count}, got vote keys info: \r\n";
            foreach (var fullNode in candidateList.Value)
            {
                var candidateVote = Behaviors.ElectionService.CallViewMethod<CandidateVote>(
                    ElectionMethod.GetCandidateVote,
                    new StringValue
                    {
                        Value = fullNode.ToHex()
                    });
                var address = Address.FromPublicKey(fullNode.ToByteArray());
                if (candidateVote.Equals(new CandidateVote()))
                    continue;
                voteMessage +=
                    $" {fullNode.ToHex()} = {address} All tickets: {candidateVote.AllObtainedVotedVotesAmount}, " +
                    $"Active tickets: {candidateVote.ObtainedActiveVotedVotesAmount}\r\n";
            }

            Logger.Info(voteMessage);
        }

        [TestMethod]
        public void GetTermSnapshot()
        {
            var termNumber =
                Behaviors.ConsensusService.CallViewMethod<Int64Value>(ConsensusMethod.GetCurrentTermNumber,
                    new Empty()).Value;
            if (termNumber.Equals(1)) return;

            var snapshot =
                Behaviors.ElectionService.CallViewMethod<TermSnapshot>(ElectionMethod.GetTermSnapshot,
                    new GetTermSnapshotInput {TermNumber = termNumber - 1});
            Logger.Info($"{snapshot.ElectionResult},{snapshot.MinedBlocks},{snapshot.EndRoundNumber}");
        }

        [TestMethod]
        public void GetVictories()
        {
            var victories = Behaviors.GetVictories();
            var publicKeys = victories.Value.Select(o => o.ToByteArray().ToHex()).ToList();
            foreach (var p in publicKeys)
            {
                var account = Address.FromPublicKey(ByteArrayHelper.HexStringToByteArray(p));
                Logger.Info($"{account}: {p}");
            }
        }

        [TestMethod]
        public void QuitElection()
        {
            var candidate = BpNodeAddress.First();
            var beforeBalance = Behaviors.GetBalance(candidate).Balance;
            var pubkey = Behaviors.NodeManager.GetAccountPublicKey(candidate);
            var admin = Behaviors.ElectionService.GetCandidateAdmin(pubkey);

            var result = Behaviors.QuitElection(admin.ToBase58(), candidate);
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var fee = result.GetDefaultTransactionFee();
            var afterBalance = Behaviors.GetBalance(candidate).Balance;
            beforeBalance.ShouldBe(afterBalance - 100000_00000000L + fee);
        }

        [TestMethod]
        public void GetCandidates()
        {
            var candidates = Behaviors.GetCandidates();
            Logger.Info($"Candidate count: {candidates.Value.Count}");
            foreach (var candidate in candidates.Value)
            {
                var account = Address.FromPublicKey(candidate.ToByteArray());
                Logger.Info($"Address: {account} \n " +
                            $"Candidate: {candidate.ToByteArray().ToHex()}");
            }
        }

        [TestMethod]
        public void CheckCandidatesTickets()
        {
            var voteRankList = Behaviors.GetDataCenterRankingList();
            var rankInfo = voteRankList.DataCenters.OrderByDescending(o => o.Value).ToList();
            Logger.Info(rankInfo.Count());
            var i = 1;
            foreach (var info in rankInfo)
            {
                var account = Address.FromPublicKey(ByteArrayHelper.HexStringToByteArray(info.Key));
                Logger.Info($"{i}: PublicKey={info.Key} \n" +
                            $"Account={account}  " +
                            $" Tickets={info.Value}");
                i++;
            }
        }

        [TestMethod]
        public void GetTreasuryInfo()
        {
            var treasury = Behaviors.Treasury;
            var profit = Behaviors.ProfitService;
            Behaviors.ProfitService.GetTreasurySchemes(treasury.ContractAddress);
            var treasuryAmount = treasury.GetCurrentTreasuryBalance();
            Logger.Info(JsonConvert.SerializeObject(treasuryAmount));
            Logger.Info($"treasury dotnet balance : {treasuryAmount.Value["ELF"]}");

            var height = AsyncHelper.RunSync(() => NodeManager.ApiClient.GetBlockHeightAsync());
            var dividends = treasury.GetDividends(height);
            Logger.Info(JsonConvert.SerializeObject(dividends));
            var treasuryBalance = Behaviors.TokenService.GetUserBalance(treasury.ContractAddress);
            Logger.Info($"treasury  balance : {treasuryBalance}");

            var treasuryProfit =
                profit.GetScheme(Schemes[SchemeType.Treasury].SchemeId);
            Logger.Info(treasuryProfit);

            var minerRewardProfit =
                profit.GetScheme(Schemes[SchemeType.MinerReward].SchemeId);
            Logger.Info(minerRewardProfit);



            var dividendPoolWeightProportion = treasury.GetDividendPoolWeightProportion();
            var minerRewardWeightProportion = treasury.GetMinerRewardWeightProportion();
        }

        [TestMethod]
        public void GetCurrentRoundMinedBlockBonus()
        {
            var round = Behaviors.ConsensusService.GetCurrentTermInformation();
            var roundNumber = round.RoundNumber;
            var term = round.TermNumber;
            var blocksBonus = Behaviors.ConsensusService.GetCurrentTermMiningReward().Value;
            var blockCount = blocksBonus / 12500000;
            Logger.Info($"{term} {roundNumber}: {blockCount} {blocksBonus}");
        }

        [TestMethod]
        public void GetRoundInformation()
        {
            var termInfo = Behaviors.ConsensusService.GetCurrentTermInformation();
            var roundNumber = termInfo.RoundNumber;
            // var roundNumber = 40;
            var round = Behaviors.ConsensusService.GetRoundInformation(roundNumber);
            var blocksCount = round.RealTimeMinersInformation
                .Values.Sum(minerInRound => minerInRound.ProducedBlocks);

            foreach (var value in round.RealTimeMinersInformation.Values)
            {
                Logger.Info(Address.FromPublicKey(ByteArrayHelper.HexStringToByteArray(value.Pubkey)));
                Logger.Info(value.ProducedBlocks);
            }

            var miningReward = Behaviors.ConsensusService.GetCurrentMiningRewardPerBlock().Value;
            Logger.Info(miningReward);
            var blocksBonus = blocksCount * 12500000;
            Logger.Info($"{blocksCount}: {blocksBonus}");
        }

        [TestMethod]
        public void GetReward()
        {
            GetPreviousReward();
        }

        [TestMethod]
        public void GetProfitDetails()
        {
            var minerBasicReward = Behaviors.Schemes[SchemeType.MinerBasicReward].SchemeId;
            var result = Behaviors.ProfitService.GetProfitDetails(FullNodeAddress[0], minerBasicReward);
            Logger.Info(result.Details);
        }

        [TestMethod]
        public void CheckProfitCandidates()
        {
            var symbol = "ELF";
            var profit = Behaviors.ProfitService;
            var minerBasicReward = Behaviors.Schemes[SchemeType.MinerBasicReward].SchemeId;
            var flexibleReward = Behaviors.Schemes[SchemeType.FlexibleReward].SchemeId;
            var welcomeReward = Behaviors.Schemes[SchemeType.WelcomeReward].SchemeId;
            var CitizenWelfare = Behaviors.Schemes[SchemeType.CitizenWelfare].SchemeId;
            var BackupSubsidy = Behaviors.Schemes[SchemeType.BackupSubsidy].SchemeId;

            long amount = 0;
            long backupAmount = 0;
            long sumBasicRewardAmount = 0;
            long sumWelcomeRewardAmount = 0;
            long sumFlexibleRewardAmount = 0;

            var miners = GetCurrentMiners();
            var term = Behaviors.ConsensusService.GetCurrentTermInformation();
            // miners.Add(BpNodeAddress[0]);
            // miners.Add(BpNodeAddress[1]);
            // miners.Add(BpNodeAddress[4]);
            // miners.Add(ReplaceAddress[0]);
            // miners.Add(FullNodeAddress[0]);
            foreach (var miner in miners)
            {
                var minerBasicRewardMap = profit.GetProfitsMap(miner, minerBasicReward);
                long profitAmount = 0;
                if (!minerBasicRewardMap.Equals(new ReceivedProfitsMap()))
                {
                    profitAmount = minerBasicRewardMap.Value[symbol];
                    Logger.Info($"MinerBasicReward amount: user {miner} profit {symbol} amount is {profitAmount}");
                }

                sumBasicRewardAmount += profitAmount;
                amount += profitAmount;

                long testVotesWeighRewardAmount = 0;
                long welcomeRewardAmount = 0;

                var welcomeRewardMap = profit.GetProfitsMap(miner, welcomeReward);
                if (!welcomeRewardMap.Equals(new ReceivedProfitsMap()))
                {
                    welcomeRewardAmount = welcomeRewardMap.Value[symbol];
                    Logger.Info(
                        $"WelcomeReward amount: user {miner} profit {symbol} amount is {welcomeRewardAmount}");
                }

                sumWelcomeRewardAmount += welcomeRewardAmount;
                amount += welcomeRewardAmount;

                long flexibleRewardAmount = 0;

                var flexibleRewardMap = profit.GetProfitsMap(miner, flexibleReward);
                if (!flexibleRewardMap.Equals(new ReceivedProfitsMap()))
                {
                    flexibleRewardAmount = flexibleRewardMap.Value[symbol];
                    Logger.Info(
                        $"FlexibleReward amount: user {miner} profit {symbol} amount is {flexibleRewardAmount}");
                }

                sumFlexibleRewardAmount += flexibleRewardAmount;
                amount += flexibleRewardAmount;
            }

            Logger.Info(
                $"{term.TermNumber} {amount} MinerBasicReward (10%):{sumBasicRewardAmount}; WelcomeReward(5%):{sumWelcomeRewardAmount}; FlexibleRewardAmount {sumFlexibleRewardAmount}");

            var candidates = Behaviors.GetCandidatesAddress();
            // candidates.Add(FullNodeAddress[3].ConvertAddress());
            foreach (var candidate in candidates)
            {
                var backupSubsidy = profit.GetProfitsMap(candidate.ToBase58(), BackupSubsidy);
                long backupSubsidyAmount = 0;
                if (!backupSubsidy.Equals(new ReceivedProfitsMap()))
                {
                    backupSubsidyAmount = backupSubsidy.Value[symbol];
//                    testBackupSubsidyAmount = backupSubsidy.Value["TEST"];
                    Logger.Info(
                        $"BackupSubsidy amount: user {candidate} profit {symbol} amount is {backupSubsidyAmount}");
                }

                backupAmount += backupSubsidyAmount;
            }

            Logger.Info($"{term.TermNumber} BackupSubsidy (5%):{backupAmount}");

            // var all = GetPreviousReward();
            // if (sumFlexibleRewardAmount == 0 && sumWelcomeRewardAmount == 0)
            // {
            //     sumBasicRewardAmount.ShouldBe(all.Div(5));
            //
            // }
            // if (sumFlexibleRewardAmount != 0 && sumWelcomeRewardAmount != 0)
            // {
            //     sumBasicRewardAmount.ShouldBe(all.Div(10));
            //     sumFlexibleRewardAmount.ShouldBe(all.Div(20));
            //     sumWelcomeRewardAmount.ShouldBe(all.Div(20));
            // }
            long minerBalanceSum = 0;
            long flexibleBalanceSum = 0;
            long welcomeBalanceSum = 0;
            for (var i = 1; i < term.TermNumber; i++)
            {
                var minerAddress = Behaviors.ProfitService.GetSchemeAddress(minerBasicReward, i);
                var minerBalance = Behaviors.TokenService.GetUserBalance(minerAddress.ToBase58());
                Logger.Info($"Period {i}, minerBasicReward {minerBalance}");
                minerBalanceSum += minerBalance;

                var flexibleAddress = Behaviors.ProfitService.GetSchemeAddress(flexibleReward, i);
                var flexibleBalance = Behaviors.TokenService.GetUserBalance(flexibleAddress.ToBase58());
                Logger.Info($"Period {i}, flexibleAddress {flexibleBalance}");
                flexibleBalanceSum += flexibleBalance;
                
                var welcomeAddress = Behaviors.ProfitService.GetSchemeAddress(welcomeReward, i);
                var welcomeBalance = Behaviors.TokenService.GetUserBalance(welcomeAddress.ToBase58());
                Logger.Info($"Period {i}, welcomeAddress {welcomeBalance}");
                welcomeBalanceSum += welcomeBalance;
            }
            
            Logger.Info($" minerBalanceSum {minerBalanceSum}; flexibleBalanceSum {flexibleBalanceSum}; welcomeBalanceSum {welcomeBalanceSum}");

            var info = Behaviors.TokenService.GetTokenInfo(symbol);
            Logger.Info(info);
        }

        [TestMethod]
        public void ClaimMinerBasicReward()
        {
            var profit = Behaviors.ProfitService;
            var MinerBasicReward = Behaviors.Schemes[SchemeType.MinerBasicReward].SchemeId;
            var FlexibleReward = Behaviors.Schemes[SchemeType.FlexibleReward].SchemeId;
            var WelcomeReward = Behaviors.Schemes[SchemeType.WelcomeReward].SchemeId;
            var CitizenWelfare = Behaviors.Schemes[SchemeType.CitizenWelfare].SchemeId;
            var miners = GetCurrentMiners();
            var term = Behaviors.ConsensusService.GetCurrentTermInformation();
            long feeAmount = 0;
            // miners.Add(BpNodeAddress[4]);
            // miners.Add(BpNodeAddress[1]);
            // miners.Add(BpNodeAddress[4]);
            //  Behaviors.TransferToken(InitAccount, ReplaceAddress[0], 1000_000000000);
            // foreach (var miner in miners)
            // {
                var miner = "2X9u7M3YWNUNXqbsTvCsbHkS2ncrTVsiCKsUtf8YRr3DZCQLb6";
                var profitMap = profit.GetProfitsMap(miner, MinerBasicReward);
                // if (profitMap.Equals(new ReceivedProfitsMap()))
                    // continue;
                var profitAmountFull = profitMap.Value["ELF"];
                Logger.Info($"Profit amount: user {miner} profit {profitMap} ELF amount is {profitAmountFull}");
                var beforeBalance = Behaviors.TokenService.GetUserBalance(miner);
                var newProfit = profit.GetNewTester(miner);
                var profitResult = newProfit.ExecuteMethodWithResult(ProfitMethod.ClaimProfits, new ClaimProfitsInput
                {
                    SchemeId = MinerBasicReward,
                    Beneficiary = miner.ConvertAddress()
                });
                profitResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
                var fee = profitResult.GetDefaultTransactionFee();
                var afterBalance = Behaviors.TokenService.GetUserBalance(miner);
                feeAmount += fee;
//                afterBalance.ShouldBe(beforeBalance + profitAmountFull - fee);
                var claimProfit = profitResult.Logs.Where(l => l.Name.Contains(nameof(ProfitsClaimed))).ToList();
                foreach (var cf in claimProfit)
                {
                    var info = ProfitsClaimed.Parser.ParseFrom(ByteString.FromBase64(cf.NonIndexed));
                    Logger.Info($"{info.Period}: {info.Amount}");
                }
            // }

            Logger.Info($"{term.TermNumber}: fee {feeAmount}");
        }

        [TestMethod]
        public void GetCurrentMiningRewardPerBlock()
        {
            var miningReward = Behaviors.ConsensusService.GetCurrentMiningRewardPerBlock();
            Logger.Info(miningReward.Value);
        }

        [TestMethod]
        public void CheckRewardBalance()
        {
            var MinerBasicReward = Behaviors.Schemes[SchemeType.MinerBasicReward].SchemeId;
            var FlexibleReward = Behaviors.Schemes[SchemeType.FlexibleReward].SchemeId;
            var WelcomeReward = Behaviors.Schemes[SchemeType.WelcomeReward].SchemeId;
            var CitizenWelfare = Behaviors.Schemes[SchemeType.CitizenWelfare].SchemeId;
            var BackupSubsidy = Behaviors.Schemes[SchemeType.BackupSubsidy].SchemeId;
            long sum = 0;
            var term = Behaviors.ConsensusService.GetCurrentTermInformation();
            for (var i = 1; i < term.TermNumber; i++)
            {
                var address = Behaviors.ProfitService.GetSchemeAddress(MinerBasicReward, i);
                var balance = Behaviors.TokenService.GetUserBalance(address.ToBase58());
                Logger.Info($"Period {i}, reward {balance}");
                sum += balance;
            }
            Logger.Info($"reward {sum}");
        }

        [TestMethod]
        public void GetCandidateHistory()
        {
            foreach (var candidate in FullNodeAddress)
            {
                var candidateResult = Behaviors.GetCandidateInformation(candidate);
                var getCandidateVoteWithRecords =
                    Behaviors.ElectionService.CallViewMethod<CandidateVote>(ElectionMethod.GetCandidateVoteWithRecords,
                        new StringValue {Value = NodeManager.AccountManager.GetPublicKey(candidate)});
                Logger.Info("Candidate: ");
                Logger.Info($"PublicKey: {candidateResult.Pubkey}");
                Logger.Info($"Terms: {candidateResult.Terms}");
                Logger.Info($"ContinualAppointmentCount: {candidateResult.ContinualAppointmentCount}");
                Logger.Info($"ProducedBlocks: {candidateResult.ProducedBlocks}");
                Logger.Info($"MissedTimeSlots: {candidateResult.MissedTimeSlots}");
                Logger.Info($"AnnouncementTransactionId: {candidateResult.AnnouncementTransactionId}");
                Logger.Info(getCandidateVoteWithRecords);
            }
        }

        private List<string> GetCurrentMiners()
        {
            var minerList = new List<string>();
            var miners =
                Behaviors.ConsensusService.CallViewMethod<MinerList>(ConsensusMethod.GetCurrentMinerList, new Empty());
            Logger.Info($"Miners count is : {miners.Pubkeys.Count}");
            foreach (var minersPubkey in miners.Pubkeys)
            {
                var miner = Address.FromPublicKey(minersPubkey.ToByteArray());
                minerList.Add(miner.ToBase58());
                Logger.Info($"Miner is : {miner} \n PublicKey: {minersPubkey.ToHex()}");
            }
            
            return minerList;
        }
        
        private long GetPreviousReward()
        {
            var term = Behaviors.ConsensusService.GetCurrentTermInformation();
            var blocks = Behaviors.ConsensusService.GetMinedBlocksOfPreviousTerm();
            Logger.Info(blocks.Value);
            var miningReward = Behaviors.ConsensusService.GetCurrentMiningRewardPerBlock().Value;
            Logger.Info(miningReward);
            var blocksBonus = blocks.Value * miningReward;
            Logger.Info($"Term: {term.TermNumber -1} => {blocks.Value }: {blocksBonus}");
            
            var treasury = Behaviors.Treasury;
            var treasuryAmount = treasury.GetCurrentTreasuryBalance();
            Logger.Info(JsonConvert.SerializeObject(treasuryAmount));
            Logger.Info($"treasury dotnet balance : {treasuryAmount.Value["ELF"]}");

            var all = treasuryAmount.Value["ELF"] + blocksBonus;
            Logger.Info($"All reward {all}");

            return all;
        }
    }
}