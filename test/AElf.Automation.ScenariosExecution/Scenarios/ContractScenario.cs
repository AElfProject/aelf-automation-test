using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Acs0;
using AElf.Automation.Common.Contracts;
using AElf.Automation.Common.WebApi.Dto;
using AElf.Contracts.TestContract.BasicFunction;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Automation.ScenariosExecution.Scenarios
{
    public class ContractScenario : BaseScenario
    {
        public BasicFunctionContract FunctionContract { get; set; }
        public BasicUpdateContract UpdateContract { get; set; }
        public GenesisContract Genesis { get; }
        public bool IsUpdateContract { get; set; }
        public List<string> Testers { get; }

        public ContractScenario()
        {
            InitializeScenario();

            Genesis = Services.GenesisService;
            Testers = AllTesters.GetRange(0, 5);
        }

        public void RunContractScenario()
        {
            DeployTestContract();

            ExecuteTestContractMethod();

            UpdateTestContractCode();

            ExecuteTestContractNewMethod();

            UpdateTestContractOwner();

            ExecuteTestContractMethod();
        }

        public void RunContractScenarioJob()
        {
            ExecuteStandaloneTask(new Action[]
            {
                DeployTestContract,
                ExecuteTestContractMethod,
                UpdateTestContractCode,
                ExecuteTestContractNewMethod,
                UpdateTestContractOwner,
                ExecuteTestContractMethod,
            });
        }

        private void DeployTestContract()
        {
            if (FunctionContract != null)
                return;

            Logger.WriteInfo("Test deploy customer contract.");
            FunctionContract = new BasicFunctionContract(Services.ApiHelper, Services.CallAddress);
            FunctionContract.ExecuteMethodWithResult(FunctionMethod.InitialBasicFunctionContract,
                new InitialBasicContractInput
                {
                    ContractName = "Test Contract1",
                    MinValue = 10L,
                    MaxValue = 1000L,
                    MortgageValue = 1000_000_000L,
                    Manager = Address.Parse(Testers[0])
                });

            FunctionContract.SetAccount(Testers[0]);
            FunctionContract.ExecuteMethodWithResult(FunctionMethod.UpdateBetLimit, new BetLimitInput
            {
                MinValue = 50,
                MaxValue = 100
            });

            IsUpdateContract = false;
        }

        private void ExecuteTestContractMethod()
        {
            if (IsUpdateContract)
                return;

            foreach (var account in Testers.GetRange(1, Testers.Count - 1))
            {
                FunctionContract.SetAccount(account);
                FunctionContract.ExecuteMethodWithResult(FunctionMethod.UserPlayBet, new BetInput
                {
                    Int64Value = GenerateRandomNumber(50, 100)
                });
                Thread.Sleep(10 * 1000);
            }

            Logger.WriteInfo("Test contract old methods executed successful.");
        }

        private void UpdateTestContractOwner()
        {
            var owner = Genesis.GetContractOwner(FunctionContract.ContractAddress);
            var ownerCandidates = Testers.FindAll(o => o != owner.GetFormatted()).ToList();
            var id = GenerateRandomNumber(0, Testers.Count - 2);

            Genesis.SetAccount(owner.GetFormatted());
            var updateResult = Genesis.ExecuteMethodWithResult(GenesisMethod.ChangeContractOwner,
                new ChangeContractOwnerInput
                {
                    ContractAddress = Address.Parse(FunctionContract.ContractAddress),
                    NewOwner = Address.Parse(ownerCandidates[id])
                });

            if (updateResult.InfoMsg is TransactionResultDto txDto)
            {
                if (txDto.Status != "Mined")
                    Logger.WriteError(txDto.Error);
            }

            var newOwner = Genesis.GetContractOwner(FunctionContract.ContractAddress);
            if (newOwner.GetFormatted() == ownerCandidates[id])
                Logger.WriteInfo($"TestContract owner updated from {owner} to {newOwner}");
        }

        private void UpdateTestContractCode()
        {
            var owner = Genesis.GetContractOwner(FunctionContract.ContractAddress);

            Genesis.SetAccount(owner.GetFormatted());
            if (!IsUpdateContract)
            {
                //update to update contract
                var result = Genesis.UpdateContract(owner.GetFormatted(), FunctionContract.ContractAddress,
                    BasicUpdateContract.ContractFileName);
                if (!result) return;
                IsUpdateContract = true;
                UpdateContract = new BasicUpdateContract(Services.ApiHelper, owner.GetFormatted(),
                    FunctionContract.ContractAddress);
                Logger.WriteInfo("Update contract to UpdateContract successful.");
            }
            else
            {
                //update to basic contract
                var result = Genesis.UpdateContract(owner.GetFormatted(), UpdateContract.ContractAddress,
                    BasicFunctionContract.ContractFileName);
                if (!result) return;
                IsUpdateContract = false;
                FunctionContract = new BasicFunctionContract(Services.ApiHelper, owner.GetFormatted(),
                    UpdateContract.ContractAddress);
                Logger.WriteInfo("Update contract to BasicContract successful.");
            }
        }

        private void ExecuteTestContractNewMethod()
        {
            if (!IsUpdateContract)
                return;

            var manager = Testers[0];
            UpdateContract.SetAccount(manager);

            //execute new method
            var txResult = UpdateContract.ExecuteMethodWithResult(UpdateMethod.UpdateMortgage, new BetInput
            {
                Int64Value = 10_000
            });
            if (!(txResult.InfoMsg is TransactionResultDto txDto)) return;
            if (txDto.Status != "Mined")
            {
                Logger.WriteError(txDto.Error);
                return;
            }

            Logger.WriteInfo("New contract action method executed successful.");

            //call New method
            var result = UpdateContract.CallViewMethod<BetStatus>(UpdateMethod.QueryBetStatus, new Empty());
            if (!result.BoolValue)
                Logger.WriteInfo("New contract view method called successful.");
        }
    }
}