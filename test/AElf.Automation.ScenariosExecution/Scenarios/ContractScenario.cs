using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AElf.Contracts.TestContract.BasicFunction;
using AElf.Types;
using AElfChain.Common.Contracts;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using log4net;

namespace AElf.Automation.ScenariosExecution.Scenarios
{
    public class ContractScenario : BaseScenario
    {
        public new static readonly ILog Logger = Log4NetHelper.GetLogger();
        public AuthorityManager AuthorityManager;
        public string miner;

        public ContractScenario()
        {
            InitializeScenario();
            AuthorityManager = new AuthorityManager(Services.NodeManager, Services.CallAddress);
            miner = AuthorityManager.GetCurrentMiners().First();
            Genesis = Services.GenesisService;
            Testers = AllTesters.GetRange(0, 5);
            PrintTesters(nameof(ContractScenario), Testers);
        }

        public BasicFunctionContract FunctionContract { get; set; }
        public BasicUpdateContract UpdateContract { get; set; }
        public GenesisContract Genesis { get; }
        public List<string> Testers { get; }

        public void RunContractScenario()
        {
            ExecuteFunctionContractMethod();
            ExecuteUpdateContractMethod();
            UpdateBasicFunctionContract();
            UpdateBasicUpdateContract();
        }

        public void RunContractScenarioJob()
        {
            ExecuteStandaloneTask(new Action[]
            {
                ExecuteFunctionContractMethod,
                ExecuteUpdateContractMethod,
                UpdateBasicFunctionContract,
                UpdateBasicUpdateContract,
                () => PrepareTesterToken(Testers),
                UpdateEndpointAction
            });
        }

        private void ExecuteFunctionContractMethod()
        {
            FunctionContract =
                BasicFunctionContract.GetOrDeployBasicFunctionContract(Services.NodeManager, miner);

            foreach (var account in Testers.GetRange(1, Testers.Count - 1))
            {
                FunctionContract.SetAccount(account);
                FunctionContract.CallViewMethod<MoneyOutput>(FunctionMethod.QueryUserWinMoney,
                    account.ConvertAddress());
                var txResult = FunctionContract.ExecuteMethodWithResult(FunctionMethod.UserPlayBet, new BetInput
                {
                    Int64Value = GenerateRandomNumber(120, 8000)
                });
                if (txResult.Status.ConvertTransactionResultStatus() == TransactionResultStatus.Mined)
                    Logger.Info("Function contract 'UserPlayBet' execute successful.");

                Thread.Sleep(3 * 1000);
            }
        }

        private void ExecuteUpdateContractMethod()
        {
            UpdateContract = BasicUpdateContract.GetOrDeployBasicUpdateContract(Services.NodeManager, miner);

            foreach (var account in Testers.GetRange(1, Testers.Count - 1))
            {
                UpdateContract.SetAccount(account);
                UpdateContract.CallViewMethod<MoneyOutput>(UpdateMethod.QueryUserWinMoney,
                    account.ConvertAddress());
                var txResult = UpdateContract.ExecuteMethodWithResult(UpdateMethod.UserPlayBet, new BetInput
                {
                    Int64Value = GenerateRandomNumber(120, 8000)
                });
                if (txResult.Status.ConvertTransactionResultStatus() == TransactionResultStatus.Mined)
                    Logger.Info("Update contract 'UserPlayBet' execute successful.");

                Thread.Sleep(3 * 1000);
            }
        }

        private void UpdateBasicFunctionContract()
        {
            var result = UpdateTestContractCode(FunctionContract.Contract, BasicUpdateContract.ContractFileName);
            if (!result) return;
            var updateContract = new BasicUpdateContract(Services.NodeManager, miner, FunctionContract.ContractAddress);
            var contractName = updateContract.GetContractName();
            if (contractName == nameof(BasicUpdateContract))
                Logger.Info("Contract update from 'BasicFunctionContract' to 'BasicUpdateContract' successful.");
            else
                Logger.Error("Contract update from 'BasicFunctionContract' to 'BasicUpdateContract' failed.");
        }

        private void UpdateBasicUpdateContract()
        {
            var result = UpdateTestContractCode(UpdateContract.Contract, BasicFunctionContract.ContractFileName);
            if (!result) return;
            var functionContract =
                new BasicFunctionContract(Services.NodeManager, miner, UpdateContract.ContractAddress);
            var contractName = functionContract.GetContractName();
            if (contractName == nameof(BasicFunctionContract))
                Logger.Info("Contract update from 'BasicUpdateContract' to 'BasicFunctionContract' successful.");
            else
                Logger.Error("Contract update from 'BasicUpdateContract' to 'BasicFunctionContract' failed.");
        }

//        private string UpdateTestContractAuthor(Address contract)
//        {
//            var owner = Genesis.GetContractAuthor(contract);
//            var ownerCandidates = Testers.FindAll(o => o != owner.GetFormatted()).ToList();
//            var id = GenerateRandomNumber(0, Testers.Count - 2);
//
//            Genesis.SetAccount(owner.GetFormatted());
//            var updateResult = Genesis.ExecuteMethodWithResult(GenesisMethod.ChangeContractAuthor,
//                new ChangeContractAuthorInput
//                {
//                    ContractAddress = contract,
//                    NewAuthor = ownerCandidates[id].ConvertAddress()
//                });
//
//            if (updateResult.Status.ConvertTransactionResultStatus() != TransactionResultStatus.Mined)
//                return owner.GetFormatted();
//
//            var newOwner = Genesis.GetContractAuthor(contract);
//            if (newOwner.GetFormatted() == ownerCandidates[id])
//                Logger.Info($"Contract '{contract}' owner updated successful.");
//            else
//            {
//                Logger.Error($"Contract '{contract}' owner updated failed.");
//            }
//
//            return newOwner.GetFormatted();
//        }
        private bool UpdateTestContractCode(Address contract, string contractName)
        {
            Genesis.SetAccount(miner);

            //update to update contract
            try
            {
                AuthorityManager.UpdateContractWithAuthority(miner, contract.GetFormatted(), contractName);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}