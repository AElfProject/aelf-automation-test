using System;
using System.Linq;
using Acs0;
using AElf.Contracts.MultiToken;
using AElf.Types;
using AElfChain.Common.Contracts;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using AElfChain.Common.Utils;
using Google.Protobuf;
using log4net;
using Newtonsoft.Json;

namespace AElf.Automation.ContractsTesting
{
    public class ProposeContract
    {
        public INodeManager NodeManager { get; set; }
        public AuthorityManager Authority { get; set; }
        public string TestAccount = "28Y8JA1i2cN6oHvdv7EraXJr9a1gY6D1PpJXw9QtRMRwKcBQMK";
        public ILog Logger = Log4NetHelper.GetLogger();
        
        public ProposeContract(INodeManager nodeManager)
        {
            NodeManager = nodeManager;
            Authority = new AuthorityManager(nodeManager);
        }

        public Address DeployContract()
        {
            var contractName = "AElf.Contracts.MultiToken";
            var genesis = NodeManager.GetGenesisContract(TestAccount);
            var parliament = genesis.GetParliamentAuthContract();
            var proposalId = genesis.ProposeNewContract(TestAccount, contractName);
            parliament.CheckProposalCanBeReleased(proposalId);
            var releaseResult = genesis.ReleaseApprovedContract(TestAccount, proposalId);
            var byteString = ByteString.FromBase64(releaseResult.Logs.First(o=>o.Name == "ContractDeployed").NonIndexed);
            var address = ContractDeployed.Parser.ParseFrom(byteString).Address;
            Console.WriteLine($"Deployed contract: {address}");
            return address;
        }

        public void ExecuteContractMethod(string contractAddress)
        {
            var systemToken = NodeManager.GetTokenInfo("ELF");
            var token = new TokenContract(NodeManager, TestAccount, contractAddress);
            var symbol = CommonHelper.RandomString(6, false);
            token.ExecuteMethodWithResult(TokenMethod.Create, new CreateInput
            {
                TokenName = "test with new deployed contract",
                Symbol = symbol,
                TotalSupply = 4000_0000_00000000,
                Decimals = 8,
                Issuer = TestAccount.ConvertAddress(),
                IssueChainId = systemToken.IssueChainId
            });

            var tokenInfo = token.CallViewMethod<TokenInfo>(TokenMethod.GetTokenInfo, new GetTokenInfoInput
            {
                Symbol = symbol
            });
            Logger.Info(JsonConvert.SerializeObject(tokenInfo), Format.Json);
        }
    }
}