using System.Text;
using Acs3;
using AElf.Automation.Common.Contracts;
using AElf.Automation.Common.WebApi.Dto;
using AElf.Contracts.CrossChain;
using AElf.Types;
using Google.Protobuf;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Org.BouncyCastle.Utilities.Encoders;

namespace AElf.Automation.SideChainTests
{
    [TestClass]
    public class SideChainCreate: SideChainTestBase
    {
        [TestInitialize]
        public void InitializeNodeTests()
        {
            base.Initialize();
        }
               

        [TestMethod]
        [DataRow("W4xEKTZcvPKXRAmdu9xEpM69ArF7gUxDh9MDgtsKnu7JfePXo")]
        public void RequestSideChain(string account)
        {
            Tester.TransferToken(InitAccount, account, 400000, "ELF");
            Tester.TokenApprove(account, 400000);
            
            var result = Tester.RequestSideChain(account,300000);
            var transactionResult = result.InfoMsg as TransactionResultDto;
            var message = transactionResult.ReadableReturnValue;
            _logger.WriteInfo($"proposal message is {message}");
        }

        //708d7c62cb33df097c68686796fa4cba9b418ef3b73cd83ab85086037b5a0a9f 2882050
        //a2cc529ec1574adaf61f433c0acd5846449fac0896a9e118912b2687e743337b 2947586
        [TestMethod]
        [DataRow("344a0667f4d36362dc400e48610ed50b0ad9d3086044df8b99a7b338ffd61bc8")]
//        [DataRow("4739211d42fa6c77b51a72c18f2ddaa21dbe0369cf40e3d4c4f69f6f7dd059b3")]
//        [DataRow("b8ed3964a6567a2aafd62a82e4cfe4515757cb0acacea675f7bdd9664737f5c1")]
//        [DataRow("921d7e83dc9f4fc2a7e643c11ca6d272684539b5cdb3ef5b1a5d7c902b7f64db")] //disposal
        public void ApproveProposal(string proposalId)
        {
            foreach (var bp in BpNodeAddress)
            {
                var result = Tester.Approve(bp, proposalId);
                var resultDto = result.InfoMsg as TransactionResultDto;
                _logger.WriteInfo($"Approve is {resultDto.ReadableReturnValue}");
            }
        }

//        [TestMethod]
//        [DataRow("W4xEKTZcvPKXRAmdu9xEpM69ArF7gUxDh9MDgtsKnu7JfePXo","408ae6d137e6cec3928def3d1f38cbb73b8bb8c2c1ccc56265fec722501612df")]
//        public void ReleaseProposal(string account, string proposalId)
//        {
//            var result = Tester.Release(account,proposalId);
//            var transactionResult = result.InfoMsg as TransactionResultDto;
//            var creationRequested = transactionResult.Logs[0].NonIndexed;
//            var byteString = ByteString.FromBase64(creationRequested);
//            var chainId = CreationRequested.Parser.ParseFrom(byteString).ChainId;
//            var creator = CreationRequested.Parser.ParseFrom(byteString).Creator;
//            _logger.WriteInfo($"Side chain id is {chainId}, creator is {creator}");
//        }
//
//        [TestMethod]
//        [DataRow("W4xEKTZcvPKXRAmdu9xEpM69ArF7gUxDh9MDgtsKnu7JfePXo")]
//        public void CreateProposal(string account)
//        {
//            Tester.TransferToken(InitAccount, account, 400000, "ELF");
//            Tester.TokenApprove(account, 400000);
//            var address = Tester.GetOrganizationAddress(account);
//            var result = Tester.CreateSideChainProposal(address, account, 1, 1000);
//            
//            var transactionResult = result.InfoMsg as TransactionResultDto;
//            var message = transactionResult.ReadableReturnValue;
//            _logger.WriteInfo($"proposal message is {message}");
//        }
//        

        [TestMethod]
        [DataRow("94caf8b5a32e8d74c42ceb4a18cb4a06bde743f3c85da57e3fafbc8796443fbe")]
        public void GetProposal(string proposalId)
        {
            var result = Tester.GetProposal(proposalId);
            _logger.WriteInfo($"proposal message is {result.ProposalId},{result.ExpiredTime},{result.ToAddress},{result.OrganizationAddress},{result.ContractMethodName}");
            
        }



        [TestMethod]
        [DataRow(2750978)]
//        [DataRow(2816514)]
//        [DataRow(2882050)]
//        [DataRow(2947586)]
        public void CheckStatus(int chainId)
        {
            var status =Tester.GetChainStatus(chainId);
            _logger.WriteInfo($"side chain is {status}");
        }

        [TestMethod]
//        [DataRow("W4xEKTZcvPKXRAmdu9xEpM69ArF7gUxDh9MDgtsKnu7JfePXo",2750978,1000000)]
        [DataRow("W4xEKTZcvPKXRAmdu9xEpM69ArF7gUxDh9MDgtsKnu7JfePXo",2816514,1000000)]
        public void Recharge(string account,int chainId,long amount)
        {
            CheckBalance(account);
            
            if (Tester.GetBalance(account,"ELF").Balance < amount)
            {
                Tester.TransferToken(InitAccount, account, amount, "ELF");
            }
            Tester.TokenApprove(account, amount);
            var reCharge = Tester.Recharge(account,chainId, amount);
            var balance = Tester.GetBalance(Tester.CrossChainService.ContractAddress, "ELF");
            _logger.WriteInfo($"side chain lock balance is {balance}");
        }

        [TestMethod]
        [DataRow("W4xEKTZcvPKXRAmdu9xEpM69ArF7gUxDh9MDgtsKnu7JfePXo",2816514)]
        public void RequestChainDisposal(string account,int chainId)
        {
            var result = Tester.RequestChainDisposal(account, chainId);
            var transactionReturn = result.InfoMsg as TransactionResultDto;
            var proposalId = transactionReturn.ReadableReturnValue;
            
            _logger.WriteInfo($"Disposal chain proposal id is {proposalId}");
        }


        [TestMethod]
        [DataRow("W4xEKTZcvPKXRAmdu9xEpM69ArF7gUxDh9MDgtsKnu7JfePXo")]
        public void CheckBalance(string account)
        {
            var balance = Tester.GetBalance(Tester.CrossChainService.ContractAddress, "ELF");
            _logger.WriteInfo($"side chain balance is {balance}");

            var userBalance = Tester.GetBalance(account, "ELF");
            _logger.WriteInfo($"user balance is {userBalance}");
        }
    }
}