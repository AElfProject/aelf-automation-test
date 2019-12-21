using System.Linq;
using Acs7;
using AElfChain.Common;
using AElf.Contracts.CrossChain;
using AElf.Types;
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
        
        //708d7c62cb33df097c68686796fa4cba9b418ef3b73cd83ab85086037b5a0a9f 2882050
        //a2cc529ec1574adaf61f433c0acd5846449fac0896a9e118912b2687e743337b 2947586
        [TestMethod]
        [DataRow("67bc05f0b34415177d5c95dd2c1d4e02408e387aebeabb3fe7e4d360e9113f41")]
//        [DataRow("2323f166cfaa67f611b428bbcd5cb0ba47c027b41e6e28a536d02873329dbc48")]
//        [DataRow("b8ed3964a6567a2aafd62a82e4cfe4515757cb0acacea675f7bdd9664737f5c1")]
//        [DataRow("921d7e83dc9f4fc2a7e643c11ca6d272684539b5cdb3ef5b1a5d7c902b7f64db")] //disposal
        public void ApproveProposal(string proposalId)
        {
            foreach (var bp in Miners)
            {
                var result = MainContracts.Approve(bp, proposalId);
                _logger.Info($"Approve is {result.ReadableReturnValue}");
            }
        }
        

        [TestMethod]
        public void CreateProposal()
        {
            MainContracts.TokenApprove(InitAccount, 400000);
            var tokenInfo = new SideChainTokenInfo
            {
                Symbol = "STA",
                TokenName = "Side chain token STA",
                Decimals = 8,
                IsBurnable = true,
                Issuer = AddressHelper.Base58StringToAddress(InitAccount),
                TotalSupply = 10_00000000_00000000
            };
            var result = MainContracts.RequestSideChainCreation(InitAccount, "123", 1, 400000, true,tokenInfo);
            
            _logger.Info($"proposal message is {result}");
        }
        
        [TestMethod]
        [DataRow(
            "6ab3db4d09f48526f5a64c573b57b6288a70a37822dbb6cd00ef025f35add9ce")]
        public void ReleaseProposal(string proposalId)
        {
            var result = MainContracts.ReleaseSideChainCreation(InitAccount, proposalId);
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
            var result = MainContracts.GetProposal(proposalId);
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
            var status = MainContracts.GetChainStatus(intChainId);
            _logger.Info($"side chain is {status}");
        }
        
        [TestMethod]
        [DataRow("tDVW")]
        public void Recharge(string chainId)
        {
            var intChainId = ChainHelper.ConvertBase58ToChainId(chainId);
            var status = MainContracts.Recharge(InitAccount,intChainId,200000);
            _logger.Info($" Transaction is {status.Status}");
        }
        

        [TestMethod]
        [DataRow("W4xEKTZcvPKXRAmdu9xEpM69ArF7gUxDh9MDgtsKnu7JfePXo", 2816514)]
        public void RequestChainDisposal(string account, int chainId)
        {
            var result = MainContracts.RequestChainDisposal(account, chainId);
            var proposalId = result.ReadableReturnValue;

            _logger.Info($"Disposal chain proposal id is {proposalId}");
        }


        [TestMethod]
        [DataRow("28Y8JA1i2cN6oHvdv7EraXJr9a1gY6D1PpJXw9QtRMRwKcBQMK")]
        public void CheckBalance(string account)
        {
            var balance = MainContracts.GetBalance(MainContracts.CrossChainService.ContractAddress,
                NodeOption.NativeTokenSymbol);
            _logger.Info($"side chain balance is {balance}");

            var userBalance = MainContracts.GetBalance(account, NodeOption.NativeTokenSymbol);
            _logger.Info($"user balance is {userBalance}");
        }
    }
}