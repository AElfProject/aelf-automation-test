using System.Linq;
using Acs7;
using AElfChain.Common;
using AElf.Contracts.CrossChain;
using Google.Protobuf;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AElf.Automation.SideChainTests
{
    [TestClass]
    public class SideChainCreate : SideChainTestBase
    {
        [TestInitialize]
        public void InitializeNodeTests()
        {
            Initialize();
        }


        [TestMethod]
        [DataRow("CnVL7BcRcaYGVovoiz4eiv4ZZFyJR7vjBmqgcZqgmicVXKKpx")]
        public void TransferSideChain(string account)
        {
            TransferToken(MainServices,InitAccount, account, 121000_00000000, NodeOption.NativeTokenSymbol);
            TransferToken(SideAServices,InitAccount, account, 121000_00000000, GetPrimaryTokenSymbol(SideAServices));
            TransferToken(SideBServices,InitAccount, account, 121000_00000000, GetPrimaryTokenSymbol(SideBServices));

            _logger.Info($"{GetBalance(MainServices,InitAccount, NodeOption.NativeTokenSymbol).Balance}"); 
            _logger.Info($"{GetBalance(SideAServices,InitAccount, GetPrimaryTokenSymbol(SideAServices)).Balance}");
            _logger.Info($"{GetBalance(SideBServices,InitAccount, GetPrimaryTokenSymbol(SideBServices)).Balance}");
           
            _logger.Info($"{GetBalance(MainServices,account, NodeOption.NativeTokenSymbol)}"); 
            _logger.Info($"{GetBalance(SideAServices,account, GetPrimaryTokenSymbol(SideAServices)).Balance}");
            _logger.Info($"{GetBalance(SideBServices,account, GetPrimaryTokenSymbol(SideBServices)).Balance}");
        }

        [TestMethod]
        [DataRow("4401e46059f2f829cfb3f69f97fe8b1f4ee3d58356d5a74717c13d4925a8b024")]
        [DataRow("2323f166cfaa67f611b428bbcd5cb0ba47c027b41e6e28a536d02873329dbc48")]
        public void ApproveProposal(string proposalId)
        {
            foreach (var bp in Miners)
            {
                var result = Approve(MainServices,bp, proposalId);
                _logger.Info($"Approve is {result.ReadableReturnValue}");
            }
        }

        [TestMethod]
        public void CreateProposal()
        {
           TokenApprove(MainServices,InitAccount, 400000);
            var tokenInfo = new SideChainTokenInfo
            {
                Symbol = "STA",
                TokenName = "Side chain token STA",
                Decimals = 8,
                IsBurnable = true,
                Issuer = AddressHelper.Base58StringToAddress(InitAccount),
                TotalSupply = 10_00000000_00000000
            };
            var result = RequestSideChainCreation(MainServices,InitAccount, "123", 1, 400000, true,tokenInfo);
            
            _logger.Info($"proposal message is {result}");
        }
        
        [TestMethod]
        [DataRow(
            "6ab3db4d09f48526f5a64c573b57b6288a70a37822dbb6cd00ef025f35add9ce")]
        public void ReleaseProposal(string proposalId)
        {
            var result = ReleaseSideChainCreation(MainServices,InitAccount, proposalId);
            var release = result.Logs.First(l => l.Name.Contains(nameof(SideChainCreatedEvent)))
                .NonIndexed;
            var byteString = ByteString.FromBase64(release);
            var sideChainCreatedEvent = SideChainCreatedEvent.Parser
                .ParseFrom(byteString);
            var chainId = sideChainCreatedEvent.ChainId;
            var creator = sideChainCreatedEvent.Creator;
           
            _logger.Info($"Side chain id is {chainId}, creator is {creator}");
        }


        [TestMethod]
        [DataRow("94caf8b5a32e8d74c42ceb4a18cb4a06bde743f3c85da57e3fafbc8796443fbe")]
        public void GetProposal(string proposalId)
        {
            var result = GetProposal(MainServices,proposalId);
            _logger.Info(
                $"proposal message is {result.ProposalId},{result.ExpiredTime},{result.ToAddress},{result.OrganizationAddress},{result.ContractMethodName}");
        }


        [TestMethod]
        [DataRow("tDVW")]
//        [DataRow(2816514)]
//        [DataRow(2882050)]
//        [DataRow(2947586)]
        public void CheckStatus(string chainId)
        {
            var intChainId = ChainHelper.ConvertBase58ToChainId(chainId);
            var status = GetChainStatus(MainServices,intChainId);
            _logger.Info($"side chain is {status}");
        }
        
        [TestMethod]
        [DataRow("tDVW")]
        public void Recharge(string chainId)
        {
            var intChainId = ChainHelper.ConvertBase58ToChainId(chainId);
            var status = Recharge(MainServices,InitAccount,intChainId,200000);
            _logger.Info($" Transaction is {status.Status}");
        }
        

        [TestMethod]
        [DataRow("W4xEKTZcvPKXRAmdu9xEpM69ArF7gUxDh9MDgtsKnu7JfePXo", 2816514)]
        public void RequestChainDisposal(string account, int chainId)
        {
            var result = RequestChainDisposal(MainServices,account, chainId);
            var proposalId = result.ReadableReturnValue;

            _logger.Info($"Disposal chain proposal id is {proposalId}");
        }


        [TestMethod]
        [DataRow("28Y8JA1i2cN6oHvdv7EraXJr9a1gY6D1PpJXw9QtRMRwKcBQMK")]
        public void CheckBalance(string account)
        {
            var balance = GetBalance(MainServices,MainServices.CrossChainService.ContractAddress,
                NodeOption.NativeTokenSymbol);
            _logger.Info($"side chain balance is {balance}");

            var userBalance = GetBalance(MainServices,account, NodeOption.NativeTokenSymbol);
            _logger.Info($"user balance is {userBalance}");
        }
    }
}