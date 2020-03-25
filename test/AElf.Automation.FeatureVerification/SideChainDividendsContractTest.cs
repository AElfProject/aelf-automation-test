using System;
using System.Linq;
using System.Threading.Tasks;
using AElf.Client.Dto;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Contracts.Election;
using AElf.Contracts.MultiToken;
using AElf.Contracts.TokenHolder;
using AElf.Types;
using AElfChain.Common;
using AElfChain.Common.Contracts;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using log4net.Repository.Hierarchy;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using Volo.Abp.Threading;
using PubkeyList = AElf.Contracts.Consensus.AEDPoS.PubkeyList;

namespace AElf.Automation.Contracts.ScenarioTest
{
    [TestClass]
    public class SideChainDividendsContractTest
    {
        public ILogHelper Logger = LogHelper.GetLogger();
        public INodeManager MainNode { get; set; }
        public INodeManager SideNode { get; set; }

        public ContractManager MainManager { get; set; }
        public ContractManager SideManager { get; set; }
        public GenesisContract Genesis { get; set; }
        private string TestAccount { get; } = "2oSMWm1tjRqVdfmrdL8dgrRvhWu1FP8wcZidjS6wPbuoVtxhEz";
        private string InitAccount { get; } = "28Y8JA1i2cN6oHvdv7EraXJr9a1gY6D1PpJXw9QtRMRwKcBQMK";
        private string CandidateAccount { get; } = "2RCLmZQ2291xDwSbDEJR6nLhFJcMkyfrVTq1i1YxWC4SdY49a6";
        private string Symbol = "TEST";
        
        public SideChainDividendsContractTest()
        {
            Log4NetHelper.LogInit();
            Logger.InitLogHelper();
            MainNode = new NodeManager("192.168.197.14:8000");

            NodeInfoHelper.SetConfig("nodes-env1-side1");
            var bpNode = NodeInfoHelper.Config.Nodes.First();
            SideNode = new NodeManager(bpNode.Endpoint);
            Genesis = SideNode.GetGenesisContract(InitAccount);

            MainManager = new ContractManager(MainNode, InitAccount);
            SideManager = new ContractManager(SideNode, InitAccount);

//            MainManager.Token.TransferBalance(InitAccount, TestAccount, 10000_00000000);
//            MainManager.Token.TransferBalance(InitAccount, CandidateAccount, 200000_00000000);

//            SideManager.Token.TransferBalance(bpNode.Account, TestAccount, 20_00000000);
            AsyncHelper.RunSync(ContributeToSideChainDividendsPool);
        }

        [TestMethod]
        public void VoteToCandidates()
        {
            var election = MainManager.Election;
            election.SetAccount(CandidateAccount);
            var electionResult = election.ExecuteMethodWithResult(ElectionMethod.AnnounceElection, new Empty());
            electionResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            
            var voteResult = UserVote(TestAccount, CandidateAccount, 90, 1000);
            voteResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
        }

        [TestMethod]
        public async Task CheckToken()
        {
            var sideToken = SideManager.Token;
            var consensusContract = SideManager.Consensus;
            var tokenHolder = Genesis.GetTokenHolderStub(TestAccount);
            var sideTokenInfo = sideToken.GetTokenInfo("SHARE");
            var token = MainManager.Token;
            var mainTokenInfo = token.GetTokenInfo("SHARE");
//            sideTokenInfo.Equals(mainTokenInfo).ShouldBeTrue();
            var balance = sideToken.GetUserBalance(TestAccount, Symbol);
            var consensus = sideToken.GetUserBalance(consensusContract.ContractAddress, Symbol);
            var scheme = await tokenHolder.GetScheme.CallAsync(consensusContract.Contract);
            var voteBalance = sideToken.GetUserBalance(TestAccount, "VOTE");
            var testBalance = sideToken.GetUserBalance(TestAccount, Symbol);
        }

        [TestMethod]
        public async Task RegisterForProfits()
        {
            var sideToken = SideManager.Token;
            var holder = SideManager.TokenHolderStub; 
            var testHolder = Genesis.GetTokenHolderStub(TestAccount);
            var shareBeforeBalance = sideToken.GetUserBalance(TestAccount, "SHARE");
            var approveResult = await SideManager.TokenImplStub.Approve.SendAsync(new ApproveInput
            {
                Spender = SideManager.TokenHolder.Contract,
                Amount = 50,
                Symbol = "SHARE"
            });
            approveResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var registerResult =
                await SideManager.TokenHolderStub.RegisterForProfits.SendAsync(new RegisterForProfitsInput
                {
                    SchemeManager = SideManager.Consensus.Contract,
                    Amount = 50
                });
            
            registerResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var shareBalance = sideToken.GetUserBalance(TestAccount, "SHARE");
            shareBeforeBalance.ShouldBe(shareBalance+50);
        }

        [TestMethod]
        public async Task ClaimProfits()
        {
            var tokenHolder = Genesis.GetTokenHolderStub(TestAccount);
            var consensusContract = SideManager.Consensus;
            var sideToken = SideManager.Token;
            var before = sideToken.GetUserBalance(TestAccount, Symbol);
            var scheme = await tokenHolder.GetScheme.CallAsync(consensusContract.Contract);
            var claim = await tokenHolder.ClaimProfits.SendAsync(new ClaimProfitsInput
            {
                Beneficiary = TestAccount.ConvertAddress(),
                SchemeManager = consensusContract.Contract,
            });
            claim.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var after = sideToken.GetUserBalance(TestAccount, Symbol);
        }

        [TestMethod]
        public async Task Withdraw()
        {
            var tokenHolder = Genesis.GetTokenHolderStub(TestAccount);
            var consensusContract = SideManager.Consensus;

            var result = await tokenHolder.Withdraw.SendAsync(consensusContract.Contract);
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        }

        private TransactionResultDto UserVote(string account, string candidate, int lockTime, long amount)
        {
            var token = MainManager.Token;
            var election = MainManager.Election;
            //check balance
            var beforeBalance = token.GetUserBalance(account);

            election.SetAccount(account);
            var vote = election.ExecuteMethodWithResult(ElectionMethod.Vote, new VoteMinerInput
            {
                CandidatePubkey = MainNode.GetAccountPublicKey(candidate),
                Amount = amount,
                EndTimestamp = DateTime.UtcNow.Add(TimeSpan.FromDays(lockTime)).ToTimestamp()
            });
            vote.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var voteFee = TransactionFeeCharged.Parser
                .ParseFrom(ByteString.FromBase64(vote.Logs.First(l => l.Name.Equals(nameof(TransactionFeeCharged)))
                    .NonIndexed)).Amount;
            var afterBalance = token.GetUserBalance(account);
            var voteBalance = token.GetUserBalance(account, "VOTE");
            var shareBalance = token.GetUserBalance(account, "SHARE");
            Logger.Info($"SHARE balance is {shareBalance}");
//            afterBalance.ShouldBe(beforeBalance+amount+voteFee);
            voteBalance.ShouldBe(shareBalance);
            return vote;
        }

        private async Task ContributeToSideChainDividendsPool()
        {
            var tokenStub = SideManager.TokenStub;
            var token = SideManager.Token;
            if (token.GetTokenInfo(Symbol).Equals(new TokenInfo()))
            {
                await tokenStub.Create.SendAsync(new CreateInput
                {
                    Issuer = AddressHelper.Base58StringToAddress(InitAccount),
                    Symbol = Symbol,
                    Decimals = 8,
                    IsBurnable = true,
                    TokenName = "TEST symbol",
                    TotalSupply = 100000000_00000000,
                    IsProfitable = true
                });
            }
            var testToken = Genesis.GetTokenStub(InitAccount);
            if (token.GetUserBalance(InitAccount,Symbol)<100000000000)
            {
                await tokenStub.Issue.SendAsync(new IssueInput
                {
                    Amount = 1000000_00000000,
                    Symbol = Symbol,
                    To = AddressHelper.Base58StringToAddress(InitAccount)
                });
            }

            var consensusContract = SideManager.Consensus.ContractAddress;
            var allowance = token.GetAllowance(InitAccount, consensusContract, Symbol);
            if (allowance<100000000000)
            {
                await tokenStub.Approve.SendAsync(new ApproveInput
                {
                    Amount = 1000000_00000000,
                    Symbol = Symbol,
                    Spender = consensusContract.ConvertAddress()
                });
            }

//            if (token.GetUserBalance(consensusContract,Symbol) <= 0)
//            {
//                var consensus = SideManager.ConsensusStub;
//                var result =
//                    await consensus.ContributeToSideChainDividendsPool.SendAsync(
//                        new ContributeToSideChainDividendsPoolInput
//                        {
//                            Amount = 100000000000,
//                            Symbol = Symbol
//                        });
//                result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
//            }
        }
    }
}