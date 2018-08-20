using System;
using AElf.Automation.CliTesting.Command;
using AElf.Automation.CliTesting.Command.Account;
using AElf.Automation.CliTesting.Parsing;
using AElf.Automation.CliTesting.Screen;
using AElf.Automation.CliTesting.Wallet;
using AElf.Common.Application;
using AElf.Cryptography;

namespace AElf.Automation.CliTesting.AutoTest
{
    public class InitCli
    {
        public static AElfCliProgram CliInstance {get;set;}

        public static void InitCliCommand(string rpcUrl)
        {
            ScreenManager screenManager = new ScreenManager();
            CommandParser parser = new CommandParser();

            AElfKeyStore kstore = new AElfKeyStore(ApplicationHelpers.GetDefaultDataDir());
            AccountManager manager = new AccountManager(kstore, screenManager);

            CliInstance = new AElfCliProgram(screenManager, parser, manager, rpcUrl);
            // Register local commands
            RegisterAccountCommands(CliInstance);
            RegisterNetworkCommands(CliInstance);

            CliInstance.RegisterCommand(new GetIncrementCmd());
            CliInstance.RegisterCommand(new SendTransactionCmd());
            CliInstance.RegisterCommand(new LoadContractAbiCmd());
            CliInstance.RegisterCommand(new DeployContractCommand());
            CliInstance.RegisterCommand(new GetTxResultCmd());
            CliInstance.RegisterCommand(new GetGenesisContractAddressCmd());
            CliInstance.RegisterCommand(new GetDeserializedResultCmd());
            CliInstance.RegisterCommand(new GetBlockHeightCmd());
        }

        public static void CleanCliCommand()
        {
            CliInstance = null;
        }

        private static void RegisterNetworkCommands(AElfCliProgram program)
        {
            program.RegisterCommand(new GetPeersCmd());
            program.RegisterCommand(new GetCommandsCmd());
        }

        private static void RegisterAccountCommands(AElfCliProgram program)
        {
            program.RegisterCommand(new AccountCmd());
        }
    }
}
