using System.Collections.Generic;
using System.Security.Cryptography;
using System.Threading.Tasks;
using AElfChain.Common;
using AElfChain.Common.Contracts;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using AElf.Contracts.MultiToken;
using AElf.Kernel;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using log4net;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace AElf.Automation.Contracts.ScenarioTest
{
    [TestClass]
    public class TokenContractTest
    {
        private ILog Logger { get; set; }
        public string TokenAbi { get; set; }
        public INodeManager NodeManager { get; set; }
        public AuthorityManager AuthorityManager { get; set; }
        public List<string> UserList { get; set; }

        public TokenContract TokenContract;
        public GenesisContract GenesisContract;
        public ParliamentAuthContract ParliamentAuthContract;
        public TokenContractContainer.TokenContractStub TokenSub;
        
        public string InitAccount { get; } = "2RCLmZQ2291xDwSbDEJR6nLhFJcMkyfrVTq1i1YxWC4SdY49a6";
        public string TestAccount { get; } = "28Y8JA1i2cN6oHvdv7EraXJr9a1gY6D1PpJXw9QtRMRwKcBQMK";
        private static string RpcUrl { get; } = "192.168.197.56:8001";

        [TestInitialize]
        public void Initialize()
        {
            Log4NetHelper.LogInit("ContractTest");
            Logger = Log4NetHelper.GetLogger();

            NodeManager = new NodeManager(RpcUrl);
            AuthorityManager = new AuthorityManager(NodeManager,InitAccount);
            GenesisContract = GenesisContract.GetGenesisContract(NodeManager, InitAccount);
            TokenContract = GenesisContract.GetTokenContract(InitAccount);
            ParliamentAuthContract = GenesisContract.GetParliamentAuthContract(InitAccount);
            var tester = new ContractTesterFactory(NodeManager);
            TokenSub = tester.Create<TokenContractContainer.TokenContractStub>(TokenContract.Contract, InitAccount);
        }

        [TestMethod]
        public async Task NewStubTest_Call()
        {
            var tokenContractAddress =
                AddressHelper.Base58StringToAddress("WnV9Gv3gioSh3Vgaw8SSB96nV8fWUNxuVozCf6Y14e7RXyGaM");
            var tester = new ContractTesterFactory(NodeManager);
            var tokenStub = tester.Create<TokenContractContainer.TokenContractStub>(tokenContractAddress, InitAccount);
            var tokenInfo = await tokenStub.GetTokenInfo.CallAsync(new GetTokenInfoInput
            {
                Symbol = NodeOption.NativeTokenSymbol
            });
            tokenInfo.ShouldNotBeNull();
        }

        [TestMethod]
        public async Task NewStubTest_Execution()
        {
            var tokenContractAddress =
                AddressHelper.Base58StringToAddress("WnV9Gv3gioSh3Vgaw8SSB96nV8fWUNxuVozCf6Y14e7RXyGaM");
            var tester = new ContractTesterFactory(NodeManager);
            var tokenStub = tester.Create<TokenContractContainer.TokenContractStub>(tokenContractAddress, InitAccount);
            var transactionResult = await tokenStub.Transfer.SendAsync(new TransferInput
            {
                Amount = 100,
                Symbol = NodeOption.NativeTokenSymbol,
                To = AddressHelper.Base58StringToAddress(TestAccount),
                Memo = "Test transfer with new sdk"
            });
            transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            //query balance
            var result = await tokenStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = AddressHelper.Base58StringToAddress(TestAccount),
                Symbol = NodeOption.NativeTokenSymbol
            });
            result.Balance.ShouldBeGreaterThanOrEqualTo(100);
        }

        [TestMethod]
        public async Task GetBalance()
        {
            var result = await TokenSub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = AddressHelper.Base58StringToAddress(TestAccount),
                Symbol = TokenContract.GetPrimaryTokenSymbol()
            });
            Logger.Info($"{result.Symbol},{result.Balance}");
        }

        [TestMethod]
        public void DeployContractWithAuthority_Test()
        {
            var authority = new AuthorityManager(NodeManager, TestAccount);
            var contractAddress = authority.DeployContractWithAuthority(TestAccount, "AElf.Contracts.MultiToken.dll");
            contractAddress.ShouldNotBeNull();
        }

        [TestMethod]
        public async Task SendTransaction()
        {
            var result = await TokenSub.Transfer.SendAsync(new TransferInput
            {
                To = AddressHelper.Base58StringToAddress(TestAccount),
                Amount = 1000_00000000,
                Memo = "Transfer to test account",
                Symbol = NodeManager.GetNativeTokenSymbol()
            });
            var size = result.Transaction.Size();
            Logger.Info($"transfer size is: {size}");
        }
        
        
        /*
                
        UpdateCoefficientFormContract,
        UpdateCoefficientFormSender,
        UpdateLinerAlgorithm,
        UpdatePowerAlgorithm,
        ChangeFeePieceKey,
        */

        [TestMethod]
        public void UpdateCoefficientFormContract()
        {
            var input = new CoefficientFromContract
            {
            };
            var organization = ParliamentAuthContract.GetGenesisOwnerAddress();
            var proposal = ParliamentAuthContract.CreateProposal(TokenContract.ContractAddress,
                nameof(TokenMethod.UpdateCoefficientFormContract), input, organization, InitAccount);
            var miners = AuthorityManager.GetCurrentMiners();
            ParliamentAuthContract.MinersApproveProposal(proposal,miners);
            var result = ParliamentAuthContract.ReleaseProposal(proposal);
            result.Status.ShouldBe(TransactionResultStatus.Mined);
        }
//
        [TestMethod]
        public void UpdateCoefficientFormSender()
        {
            var input = new CoefficientFromSender
            {
                PieceKey = 1000000,
                IsLiner = false,
                IsChangePieceKey = true,
                NewPieceKeyCoefficient = new NewPieceKeyCoefficient
                {
                    NewPieceKey = 100
                }
            };
            var organization = ParliamentAuthContract.GetGenesisOwnerAddress();
            var proposal = ParliamentAuthContract.CreateProposal(TokenContract.ContractAddress,
                nameof(TokenMethod.UpdateCoefficientFormSender), input, organization, InitAccount);
            var miners = AuthorityManager.GetCurrentMiners();
            ParliamentAuthContract.MinersApproveProposal(proposal,miners);
            var result = ParliamentAuthContract.ReleaseProposal(proposal,InitAccount);
            result.Status.ShouldBe(TransactionResultStatus.Mined);
        }

        [TestMethod]
        public async Task GetCalculateFeeCoefficientOfDeveloper()
        {
            /*
             *     [pbr::OriginalName("Cpu")] Cpu = 0,
            [pbr::OriginalName("Sto")] Sto = 1,
            [pbr::OriginalName("Ram")] Ram = 2,
            [pbr::OriginalName("Net")] Net = 3,
                [pbr::OriginalName("Tx")] Tx = 4,
             */
            var result = await TokenSub.GetCalculateFeeCoefficientOfContract.CallAsync(new SInt32Value{Value = 0});
            var cpu = result.Coefficients;
            Logger.Info($"{cpu}");
            
            var result1 = await TokenSub.GetCalculateFeeCoefficientOfContract.CallAsync(new SInt32Value{Value = 1});
            var sto = result1.Coefficients;
            Logger.Info($"{sto}");
            
            var result2 = await TokenSub.GetCalculateFeeCoefficientOfContract.CallAsync(new SInt32Value{Value = 2});
            var ram = result2.Coefficients;
            Logger.Info($"{ram}");
            
            var result3 = await TokenSub.GetCalculateFeeCoefficientOfContract.CallAsync(new SInt32Value{Value = 3});
            var net = result3.Coefficients;
            Logger.Info($"{net}");
            
            var result4 = await TokenSub.GetCalculateFeeCoefficientOfContract.CallAsync(new SInt32Value{Value = 4});
            var tx = result4.Coefficients;
            Logger.Info($"{tx}");
        }
        
        [TestMethod]
        public async Task GetCalculateFeeCoefficientOfUser()
        {
            var result = await TokenSub.GetCalculateFeeCoefficientOfSender.CallAsync(new Empty());
            Logger.Info($"{result.Coefficients}");
        }
    }
}