using System;
using System.Collections.Generic;
using System.Linq;
using AElf.Contracts.MultiToken;
using AElfChain.Common.Contracts;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using log4net;

namespace AElf.Automation.ContractsTesting
{
    public class CodeRemarkTest
    {
//        public string ContractAddress = "2LUmicHyH4RXrMjG4beDwuDsiWJESyLkgkwPdGTR8kahRzq5XS";
        public ILog Logger = Log4NetHelper.GetLogger();
        public string Tester = "28Y8JA1i2cN6oHvdv7EraXJr9a1gY6D1PpJXw9QtRMRwKcBQMK";

        public CodeRemarkTest(INodeManager nodeManager)
        {
            NodeManager = nodeManager;
            ParallelContract = new TokenContract(NodeManager, Tester);
        }

        public INodeManager NodeManager { get; set; }
        public TokenContract ParallelContract { get; set; }

        public void ExecuteContractMethodTest()
        {
            //create
            var elfTokenInfo = ParallelContract.GetTokenInfo("ELF");
            if (elfTokenInfo.Equals(new TokenInfo()))
                ParallelContract.ExecuteMethodWithResult(TokenMethod.Create, new CreateInput
                {
                    Symbol = "ELF",
                    TotalSupply = 100000000000000000,
                    Decimals = 8,
                    IsBurnable = true,
                    TokenName = "fake ELF Token",
                    Issuer = Tester.ConvertAddress()
                });
            
            var tokenInfo = ParallelContract.GetTokenInfo("PARALLEL");
            if (tokenInfo.Equals(new TokenInfo()))
                ParallelContract.ExecuteMethodWithResult(TokenMethod.Create, new CreateInput
                {
                    Symbol = "PARALLEL",
                    TotalSupply = 100000000000000000,
                    Decimals = 8,
                    IsBurnable = true,
                    TokenName = "Parallel Token",
                    Issuer = Tester.ConvertAddress()
                });

            //execute some other transactions
            //transfer ELF
            var accounts = new List<string>();
            var genesis = NodeManager.GetGenesisContract();
            var token = genesis.GetTokenContract();
            for (var i = 0; i < 20; i++)
            {
                var acc = NodeManager.GetRandomAccount();
                if(acc.Equals(Tester)) continue;
                accounts.Add(acc);
                token.TransferBalance(Tester, acc, 10000_00000000);
            }

            //issue
            foreach (var acc in accounts) ParallelContract.IssueBalance(Tester, acc, 1000_00000000, "PARALLEL");

            //make transaction conflict
            var a = 1;
            for (var i = 0; i < accounts.Count-1; i++)
            {
                a++;
                ParallelContract.SetAccount(accounts[i]);
                var b = 1;
                foreach (var acc in accounts)
                {
                    b++;
                    if (acc.Equals(accounts[i])) continue;
                    var pTransactionId = ParallelContract.ExecuteMethodWithTxId(TokenMethod.Transfer, new TransferInput
                    {
                        To = acc.ConvertAddress(),
                        Amount = 100,
                        Symbol = "PARALLEL"
                    });
                    Logger.Info($"{a}|{b} => TransactionId: {pTransactionId}");

                    token.SetAccount(acc);
                    var eTransactionId = token.ExecuteMethodWithTxId(TokenMethod.Transfer, new TransferInput
                    {
                        To = accounts[i].ConvertAddress(),
                        Amount = 100,
                        Symbol = "ELF"
                    });
                    Logger.Info($"{a}|{b} => TransactionId: {eTransactionId}");
                }
            }

            ParallelContract.CheckTransactionResultList();
            Console.ReadLine();

            // send transaction again, and transaction will be non-parallelizable transactions.
            for (var i = 0; i < accounts.Count-1; i++)
            {
                ParallelContract.SetAccount(accounts[i]);
                foreach (var acc in accounts)
                {
                    if (acc.Equals(accounts[i])) continue;
                    var pTransactionId = ParallelContract.ExecuteMethodWithTxId(TokenMethod.Transfer, new TransferInput
                    {
                        To = acc.ConvertAddress(),
                        Amount = 100,
                        Symbol = "PARALLEL"
                    });
                    Logger.Info($"TransactionId: {pTransactionId}");
                }
            }
        }
    }
}