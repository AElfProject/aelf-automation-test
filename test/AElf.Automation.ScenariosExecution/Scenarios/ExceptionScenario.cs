using System;
using System.Collections.Generic;
using AElf.Contracts.MultiToken;
using AElfChain.Common;
using AElfChain.Common.Contracts;
using AElfChain.Common.DtoExtension;
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
                TransferWithoutEnoughAllowanceAction
            });
        }

        private void NotExistedMethodAction()
        {
            var methodName = $"Test-{Guid.NewGuid()}";
            var executeResult = Token.ExecuteMethodWithResult(methodName, new GetBalanceInput
            {
                Symbol = NodeOption.NativeTokenSymbol,
                Owner = AddressExtension.Generate()
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
                To = AddressExtension.Generate(),
                Memo = "T without enough token"
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
                From = AddressExtension.Generate(),
                To = AddressExtension.Generate(),
                Memo = "Transfer from test"
            });
            executeResult.Error.ShouldNotBeNull();
            Logger.Info("Transfer without enough allowance execute failed.");
        }
    }
}