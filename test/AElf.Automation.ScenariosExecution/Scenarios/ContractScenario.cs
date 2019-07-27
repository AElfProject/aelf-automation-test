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
        public string ContractAddress { get; set; }
        public static bool IsUpdateContract { get; set; }
        public static string ContractManager { get; set; }
        public static string ContractOwner { get; set; }
        public List<string> Testers { get; }

        public ContractScenario()
        {
            InitializeScenario();

            Genesis = Services.GenesisService;
            FunctionContract = Services.FunctionContractService;
            UpdateContract = Services.UpdateContractService;
            ContractAddress = FunctionContract == null
                ? UpdateContract.ContractAddress
                : FunctionContract.ContractAddress;
            Testers = AllTesters.GetRange(0, 5);
        }

        public void RunContractScenario()
        {
            InitializeTestContract();

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
                //InitializeTestContract,
                ExecuteTestContractMethod,
                ExecuteTestContractNewMethod,
                UpdateTestContractCode,
                UpdateTestContractOwner
            });
        }

        private void InitializeTestContract()
        {
            if (UpdateContract != null)
                return;

            Logger.Info("Test deploy customer contract.");
            FunctionContract.ExecuteMethodWithResult(FunctionMethod.InitialBasicFunctionContract,
                new InitialBasicContractInput
                {
                    ContractName = "Test Contract1",
                    MinValue = 10L,
                    MaxValue = 1000L,
                    MortgageValue = 1000_000_000L,
                    Manager = AddressHelper.Base58StringToAddress(Testers[0])
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
                var winMoney =
                    FunctionContract.CallViewMethod<MoneyOutput>(FunctionMethod.QueryUserWinMoney,
                        AddressHelper.Base58StringToAddress(account));
                FunctionContract.ExecuteMethodWithResult(FunctionMethod.UserPlayBet, new BetInput
                {
                    Int64Value = GenerateRandomNumber(60, 99) + winMoney.Int64Value
                });
                Thread.Sleep(3 * 1000);
            }

            Logger.Info("Test contract old methods executed successful.");
        }

        private void UpdateTestContractOwner()
        {
            var owner = Genesis.GetContractOwner(ContractAddress);
            var ownerCandidates = Testers.FindAll(o => o != owner.GetFormatted()).ToList();
            var id = GenerateRandomNumber(0, Testers.Count - 2);

            Genesis.SetAccount(owner.GetFormatted());
            var updateResult = Genesis.ExecuteMethodWithResult(GenesisMethod.ChangeContractOwner,
                new ChangeContractAuthorInput
                {
                    ContractAddress = AddressHelper.Base58StringToAddress(FunctionContract.ContractAddress),
                    NewAuthor = AddressHelper.Base58StringToAddress(ownerCandidates[id])
                });

            if (updateResult.InfoMsg is TransactionResultDto txDto)
            {
                if (txDto.Status.ConvertTransactionResultStatus() != TransactionResultStatus.Mined)
                    Logger.Error(txDto.Error);
            }

            var newOwner = Genesis.GetContractOwner(FunctionContract.ContractAddress);
            if (newOwner.GetFormatted() == ownerCandidates[id])
                Logger.Info($"TestContract owner updated from {owner} to {newOwner}");
        }

        private void UpdateTestContractCode()
        {
            var owner = Genesis.GetContractOwner(ContractAddress);

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
                Logger.Info("Update contract to UpdateContract successful.");
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
                Logger.Info("Update contract to BasicContract successful.");
            }
        }

        private void ExecuteTestContractNewMethod()
        {
            if (!IsUpdateContract)
                return;

            UpdateContract.SetAccount(Services.CallAddress); //set manager account

            //execute new method
            var txResult = UpdateContract.ExecuteMethodWithResult(UpdateMethod.UpdateMortgage, new BetInput
            {
                Int64Value = 10_000
            });
            if (!(txResult.InfoMsg is TransactionResultDto txDto)) return;
            if (txDto.Status.ConvertTransactionResultStatus() != TransactionResultStatus.Mined)
            {
                Logger.Error(txDto.Error);
                return;
            }

            Logger.Info("New contract action method executed successful.");

            //call New method
            var result = UpdateContract.CallViewMethod<BetStatus>(UpdateMethod.QueryBetStatus, new Empty());
            if (!result.BoolValue)
                Logger.Info("New contract view method called successful.");
        }
    }
}