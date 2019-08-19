using System;
using System.Collections.Generic;
using Acs0;
using AElf.Automation.Common.Contracts;
using AElf.Automation.Common.Helpers;
using AElf.Automation.Common.Utils;
using AElf.Automation.Common.WebApi.Dto;
using AElf.Contracts.MultiToken;
using AElf.Types;
using log4net;
using Shouldly;

namespace AElf.Automation.ScenariosExecution.Scenarios
{
    public class ExceptionScenario : BaseScenario
    {
        public TokenContract Token { get; }
        public GenesisContract Genesis { get; }
        public List<string> Testers { get; }
        
        public new static readonly ILog Logger = Log4NetHelper.GetLogger();

        public ExceptionScenario()
        {
            InitializeScenario();
            Token = Services.TokenService;
            Genesis = Services.GenesisService;

            Testers = AllTesters.GetRange(80, 20);
        }

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
                Symbol = "ELF",
                Owner = AddressUtils.Generate()
            });
            executeResult.Result.ShouldBe(false);
            var info = executeResult.InfoMsg as TransactionResultDto;
            info?.Error.ShouldNotBeNull();
            Logger.Info("Execute not existed contract method failed.");
        }

        private void TransferWithoutEnoughTokenAction()
        {
            var user = Testers[GenerateRandomNumber(0, 10)];
            var testToken = Token.GetNewTester(user);
            var executeResult = testToken.ExecuteMethodWithResult(TokenMethod.Transfer, new TransferInput
            {
                Symbol = "ELF",
                Amount = 100_000_00000000L,
                To = AddressUtils.Generate(),
                Memo = "Transfer without enough token test"
            });
            executeResult.Result.ShouldBe(false);
            var info = executeResult.InfoMsg as TransactionResultDto;
            info?.Error.ShouldNotBeNull();
            Logger.Info("Transfer without enough token execute failed.");
        }

        private void TransferWithoutEnoughAllowanceAction()
        {
            var user = Testers[GenerateRandomNumber(0, 10)];
            var testToken = Token.GetNewTester(user);
            var executeResult = testToken.ExecuteMethodWithResult(TokenMethod.TransferFrom, new TransferFromInput
            {
                Symbol = "ELF",
                Amount = 100_000_00000000L,
                From = AddressUtils.Generate(),
                To = AddressUtils.Generate(),
                Memo = "Transfer from test"
            });
            executeResult.Result.ShouldBe(false);
            var info = executeResult.InfoMsg as TransactionResultDto;
            info?.Error.ShouldNotBeNull();
            Logger.Info("Transfer without enough allowance execute failed.");
        }

        private void UpdateContractAuthorWithoutPermissionAction()
        {
            var testUser = Testers[GenerateRandomNumber(0, 10)];
            var tester = Genesis.GetNewTester(testUser);
            var executeResult = tester.ExecuteMethodWithResult(GenesisMethod.ChangeContractAuthor, new ChangeContractAuthorInput
            {
                NewAuthor = AddressHelper.Base58StringToAddress(testUser),
                ContractAddress = AddressHelper.Base58StringToAddress(Token.ContractAddress)
            });
            executeResult.Result.ShouldBeFalse();
            var info = executeResult.InfoMsg as TransactionResultDto;
            info?.Error.ShouldNotBeNull();
            Logger.Info("Update contract author information without permission execute failed.");
        }
    }
}