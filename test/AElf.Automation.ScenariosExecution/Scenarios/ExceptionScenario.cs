using System;
using System.Collections.Generic;
using Acs0;
using AElfChain.Common;
using AElfChain.Common.Contracts;
using AElfChain.Common.Helpers;
using AElfChain.Common.Utils;
using AElf.Contracts.MultiToken;
using log4net;
using Shouldly;

namespace AElf.Automation.ScenariosExecution.Scenarios
{
    public class ExceptionScenario : BaseScenario
    {
        public ExceptionScenario()
        {
            InitializeScenario();
            Token = Services.TokenService;
            Genesis = Services.GenesisService;

            Testers = AllTesters.GetRange(10, 5);
            PrintTesters(nameof(ExceptionScenario), Testers);
        }

        public TokenContract Token { get; }
        public GenesisContract Genesis { get; }
        public List<string> Testers { get; }

        public void RunExceptionScenarioJob()
        {
            ExecuteStandaloneTask(new Action[]
            {
                NotExistedMethodAction,
                TransferWithoutEnoughTokenAction,
                TransferWithoutEnoughAllowanceAction,
                UpdateContractAuthorWithoutPermissionAction
            });
        }

        private void NotExistedMethodAction()
        {
            var methodName = $"Test-{Guid.NewGuid()}";
            var executeResult = Token.ExecuteMethodWithResult(methodName, new GetBalanceInput
            {
                Symbol = NodeOption.NativeTokenSymbol,
                Owner = AddressUtils.Generate()
            });
            executeResult.Error.ShouldNotBeNull();
            Logger.Info("Execute not existed contract method failed.");
        }

        private void TransferWithoutEnoughTokenAction()
        {
            var user = Testers[GenerateRandomNumber(0, 10)];
            var testToken = Token.GetNewTester(user);
            var executeResult = testToken.ExecuteMethodWithResult(TokenMethod.Transfer, new TransferInput
            {
                Symbol = NodeOption.NativeTokenSymbol,
                Amount = 100_000_00000000L,
                To = AddressUtils.Generate(),
                Memo = "Transfer without enough token test"
            });
            executeResult.Error.ShouldNotBeNull();
            Logger.Info("Transfer without enough token execute failed.");
        }

        private void TransferWithoutEnoughAllowanceAction()
        {
            var user = Testers[GenerateRandomNumber(0, 10)];
            var testToken = Token.GetNewTester(user);
            var executeResult = testToken.ExecuteMethodWithResult(TokenMethod.TransferFrom, new TransferFromInput
            {
                Symbol = NodeOption.NativeTokenSymbol,
                Amount = 100_000_00000000L,
                From = AddressUtils.Generate(),
                To = AddressUtils.Generate(),
                Memo = "Transfer from test"
            });
            executeResult.Error.ShouldNotBeNull();
            Logger.Info("Transfer without enough allowance execute failed.");
        }

        private void UpdateContractAuthorWithoutPermissionAction()
        {
            var testUser = Testers[GenerateRandomNumber(0, 10)];
            var tester = Genesis.GetNewTester(testUser);
            var executeResult = tester.ExecuteMethodWithResult(GenesisMethod.ChangeContractAuthor,
                new ChangeContractAuthorInput
                {
                    NewAuthor = AddressHelper.Base58StringToAddress(testUser),
                    ContractAddress = AddressHelper.Base58StringToAddress(Token.ContractAddress)
                });
            executeResult.Error.ShouldNotBeNull();
            Logger.Info("Update contract author information without permission execute failed.");
        }
    }
}