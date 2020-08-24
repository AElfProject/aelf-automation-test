using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.Client.Service;
using AElf.Contracts.MultiToken;
using AElf.Contracts.TestContract.TransactionFees;
using AElf.Types;
using AElfChain.Common;
using AElfChain.Common.Contracts;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using log4net;
using Newtonsoft.Json;
using Shouldly;
using Volo.Abp.Threading;

namespace AElf.Automation.SideChainEconomicTest.EconomicTest
{
    public class SideChainManager
    {
        public static ILog Logger = Log4NetHelper.GetLogger();

        public SideChainManager()
        {
            SideChains = new Dictionary<int, ContractServices>();
            Contracts = new Dictionary<int, string>();
        }

        public Dictionary<int, ContractServices> SideChains { get; set; }
        public Dictionary<int, string> Contracts { get; set; }

        public void InitializeSideChain(string serviceUrl, string account, int id, string contract)
        {
            var contractServices = new ContractServices(serviceUrl, account, NodeOption.DefaultPassword);
            SideChains.Add(id, contractServices);
            Contracts.Add(id,contract);
        }

        public string DeployContractResources(ContractServices services, string account = "")
        {
            var name = "AElf.Contracts.TestContract.TransactionFees";
            if (account == "")
                account = services.CallAddress;
            var authority = new AuthorityManager(services.NodeManager, account);
            var deployContract = authority.DeployContractWithAuthority(account, name);
            var acs8Contract = deployContract.ToBase58();
            Logger.Info($"Acs8 contract address: {acs8Contract}");
            
            return acs8Contract;
        }

        public List<string> CheckCreatorRentResourceBalance(ContractServices services)
        {
            var symbols = services.RentResourceSymbols;
            var balanceList = new List<string>();
            foreach (var symbol in symbols)
            {
                var balance = services.TokenService.GetUserBalance(services.CallAddress, symbol);
                if(balance < 1000_00000000)
                    balanceList.Add(symbol);
                Logger.Info($"Contract {services.CallAddress} {symbol} Token balance is {balance}");
            }

            return balanceList;
        }
        
        public Dictionary<string,long> CheckContractResourceBalance(ContractServices services, string contract)
        {
            var symbols = services.FeeResourceSymbols;
            var balanceList = new Dictionary<string,long>();
            foreach (var symbol in symbols)
            {
                var balance = services.TokenService.GetUserBalance(contract, symbol);
                if(balance < 1000_00000000)
                    balanceList.Add(symbol, balance);
                Logger.Info($"Contract {contract} {symbol} Token balance is {balance}");
            }

            return balanceList;
        }
        
        public async Task QueryResourceUsage(ContractServices services)
        {
            var genesis = services.GenesisService;
            var token = genesis.GetTokenImplStub();
            var resourceUsage = await token.GetResourceUsage.CallAsync(new Empty());
            Logger.Info(JsonConvert.SerializeObject(resourceUsage));
        }

        public void  QueryOwningRental(ContractServices services)
        {
            var genesis = services.GenesisService;
            var token = genesis.GetTokenImplStub();
            var rental = AsyncHelper.RunSync(()=> token.GetOwningRental.CallAsync(new Empty()));
            foreach (var item in rental.ResourceAmount) Logger.Info($"{item.Key}, {item.Value}");
        }
        
        public void ProposalThroughController(ContractServices contractServices, IMessage input, string method)
        {
            var authoritySideManager = new AuthorityManager(contractServices.NodeManager);
            var token = contractServices.GenesisService.GetTokenContract();
            var association = contractServices.GenesisService.GetAssociationAuthContract();
            var stub = contractServices.GenesisService.GetTokenImplStub();
            var controller =
                AsyncHelper.RunSync(() => stub.GetSideChainRentalControllerCreateInfo.CallAsync(new Empty()));
            var organization = controller.OwnerAddress;
            var controllerInfo = association.GetOrganization(organization);
            var proposer = contractServices.CallAccount;
            controllerInfo.ProposerWhiteList.Proposers.Contains(proposer).ShouldBeTrue();

            //create proposal 
            var createProposal = association.CreateProposal(token.ContractAddress, method,
                input, organization, proposer.ToBase58());
            association.Approve(createProposal, proposer.ToBase58());

            //create parliament approve proposal
            var parliament = controllerInfo.ProposerWhiteList.Proposers.Where(p => !p.Equals(proposer)).ToList();
            var approveProposal = authoritySideManager.ExecuteTransactionWithAuthority(association.ContractAddress,
                nameof(AssociationMethod.Approve), createProposal, proposer.ToBase58(), parliament.First());
            approveProposal.Status.ShouldBe(TransactionResultStatus.Mined);

            //check proposal
            var proposalInfo = association.CheckProposal(createProposal);
            proposalInfo.ToBeReleased.ShouldBeTrue();

            //release
            var release = association.ReleaseProposal(createProposal, proposer.ToBase58());
            release.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
        }

        public void OnlyCpuCounterAction(TransactionFeesContract txFees, string account = "")
        {
            if (account == "")
                account = txFees.CallAddress;
            var beforeResource = txFees.QueryContractResource();
            var randNo = CommonHelper.GenerateRandomNumber(1, 10);
            txFees.SetAccount(account);
            var txResult = txFees.ExecuteMethodWithResult(TxFeesMethod.ReadCpuCountTest, new Int32Value
            {
                Value = randNo
            });
            if (txResult.Status.ConvertTransactionResultStatus() == TransactionResultStatus.Mined)
            {
                WaitOneBlock(txFees.ApiClient,txResult.BlockNumber);
                var txFee = txResult.GetResourceTokenFee();
                var afterResource = txFees.QueryContractResource();

                //assert result
                beforeResource["READ"].ShouldBe(afterResource["READ"]+ txFee["READ"]);
                beforeResource["WRITE"].ShouldBe(afterResource["WRITE"] + txFee["WRITE"]);
                beforeResource["TRAFFIC"].ShouldBe(afterResource["TRAFFIC"] + txFee["TRAFFIC"]);
                beforeResource["STORAGE"].ShouldBe(afterResource["STORAGE"] + txFee["STORAGE"]);
            }
        }

        public void OnlyRamCounterAction(TransactionFeesContract txFees,string account = "")
        {
            if (account == "")
                account = txFees.CallAddress;
            var beforeResource = txFees.QueryContractResource();
            var randNo = CommonHelper.GenerateRandomNumber(1, 10);
            txFees.SetAccount(account);
            var txResult = txFees.ExecuteMethodWithResult(TxFeesMethod.WriteRamCountTest, new Int32Value
            {
                Value = randNo
            });
            if (txResult.Status.ConvertTransactionResultStatus() == TransactionResultStatus.Mined)
            {
                WaitOneBlock(txFees.ApiClient,txResult.BlockNumber);
                var afterResource = txFees.QueryContractResource();

                var txFee = txResult.GetResourceTokenFee();
                //assert result
                beforeResource["READ"].ShouldBe(afterResource["READ"]+ txFee["READ"]);
                beforeResource["WRITE"].ShouldBe(afterResource["WRITE"] + txFee["WRITE"]);
                beforeResource["TRAFFIC"].ShouldBe(afterResource["TRAFFIC"] + txFee["TRAFFIC"]);
                beforeResource["STORAGE"].ShouldBe(afterResource["STORAGE"] + txFee["STORAGE"]);
            }
        }

        public void BothCpuAndRamCounterAction(TransactionFeesContract txFees,string  account = "")
        {
            if (account == "")
                account = txFees.CallAddress;
            var beforeResource = txFees.QueryContractResource();
            var randNo1 = CommonHelper.GenerateRandomNumber(1, 10);
            var randNo2 = CommonHelper.GenerateRandomNumber(1, 10);
            txFees.SetAccount(account);
            var txResult = txFees.ExecuteMethodWithResult(TxFeesMethod.ComplexCountTest, new ReadWriteInput
            {
                Read = randNo1,
                Write = randNo2
            });
            if (txResult.Status.ConvertTransactionResultStatus() == TransactionResultStatus.Mined)
            {
                WaitOneBlock(txFees.ApiClient,txResult.BlockNumber);
                var afterResource = txFees.QueryContractResource();

                var txFee = txResult.GetResourceTokenFee();
                //assert result
                beforeResource["READ"].ShouldBe(afterResource["READ"]+ txFee["READ"]);
                beforeResource["WRITE"].ShouldBe(afterResource["WRITE"] + txFee["WRITE"]);
                beforeResource["TRAFFIC"].ShouldBe(afterResource["TRAFFIC"] + txFee["TRAFFIC"]);
                beforeResource["STORAGE"].ShouldBe(afterResource["STORAGE"] + txFee["STORAGE"]);
            }
        }

        public void NoCpuAndRamCounterAction(TransactionFeesContract txFees,string  account = "")
        {
            if (account == "")
                account = txFees.CallAddress;
            var beforeResource = txFees.QueryContractResource();
            txFees.SetAccount(account);
            var txResult = txFees.ExecuteMethodWithResult(TxFeesMethod.NoReadWriteCountTest, new StringValue
            {
                Value = $"NoReadWriteCountTest-{Guid.NewGuid().ToString()}"
            });
            if (txResult.Status.ConvertTransactionResultStatus() == TransactionResultStatus.Mined)
            {
                WaitOneBlock(txFees.ApiClient,txResult.BlockNumber);
                var afterResource = txFees.QueryContractResource();

                var txFee = txResult.GetResourceTokenFee();
                //assert result
                beforeResource["READ"].ShouldBe(afterResource["READ"]+ txFee["READ"]);
                beforeResource["WRITE"].ShouldBe(afterResource["WRITE"] + txFee["WRITE"]);
                beforeResource["TRAFFIC"].ShouldBe(afterResource["TRAFFIC"] + txFee["TRAFFIC"]);
                beforeResource["STORAGE"].ShouldBe(afterResource["STORAGE"] + txFee["STORAGE"]);
            }
        }
        
        private void WaitOneBlock(AElfClient client,long blockHeight)
        {
            while (true)
            {
                var height = AsyncHelper.RunSync(client.GetBlockHeightAsync);
                if (height >= blockHeight + 1)
                    return;
                Thread.Sleep(500);
            }
        }
    }
}