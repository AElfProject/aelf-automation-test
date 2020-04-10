using System;
using System.Collections.Generic;
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
//        public string ContractAddress = "2Nk2R2se5kVvb9QZwrQomc3iMEwc2TN9Z4TUmbgzMHsC7MkD7G";
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
            var tokenInfo = ParallelContract.GetTokenInfo("PARALLEL");
            if (tokenInfo.Equals(new TokenInfo()))
                ParallelContract.ExecuteMethodWithResult(TokenMethod.Create, new CreateInput
                {
                    Symbol = "PARALLEL",
                    TotalSupply = 100000000000000000,
                    Decimals = 8,
                    IsBurnable = true,
                    IsProfitable = true,
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
                accounts.Add(acc);
                token.TransferBalance(Tester, acc, 10000_00000000);
            }

            //issue
            foreach (var acc in accounts) ParallelContract.IssueBalance(Tester, acc, 1000_00000000, "PARALLEL");

            //make transaction conflict
            for (var i = 0; i < 20; i++)
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

                    token.SetAccount(acc);
                    var eTransactionId = token.ExecuteMethodWithTxId(TokenMethod.Transfer, new TransferInput
                    {
                        To = accounts[i].ConvertAddress(),
                        Amount = 100,
                        Symbol = "ELF"
                    });
                    Logger.Info($"TransactionId: {eTransactionId}");
                }
            }

            ParallelContract.CheckTransactionResultList();
            Console.ReadLine();

            // send transaction again, and transaction will be non-parallelizable transactions.
            for (var i = 0; i < 20; i++)
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