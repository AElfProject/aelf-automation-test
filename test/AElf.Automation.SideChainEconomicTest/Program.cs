using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Acs7;
using AElfChain.Common;
using AElfChain.Common.Contracts;
using AElfChain.Common.Helpers;
using log4net;
using Virgil.Crypto;

namespace AElf.Automation.SideChainEconomicTest
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            Log4NetHelper.LogInit("SideChainEconomic");
            TaskCollection = new List<Task>();

            TransactionFeesContract acs8ContractA;
            TransactionFeesContract acs8ContractB;

            var sideTest = new SideChainTests();
            var mainTest = new MainChainTests();
            
            mainTest.MainManager.BuyResources(mainTest.Main.CallAddress, 200000);
            mainTest.GetTokenInfo();

            if (sideTest.CheckBalanceAndTransfer(sideTest.SideA))
                mainTest.PrepareSideChainToken(mainTest.MainToSideA, sideTest.SideA);
            if (sideTest.CheckBalanceAndTransfer(sideTest.SideB))
                mainTest.PrepareSideChainToken(mainTest.MainToSideB, sideTest.SideB);

            sideTest.GetTokenInfo(sideTest.SideA);
            sideTest.GetTokenInfo(sideTest.SideB);

            Logger.Info($"Deploy Contracts on side chain {sideTest.SideA.NodeManager.GetChainId()}: ");
            acs8ContractA = sideTest.ContractA == ""
                ? sideTest.DeployAndTransfer(sideTest.SideA)
                : new TransactionFeesContract(sideTest.SideA.NodeManager, sideTest.SideA.CallAddress,
                    sideTest.ContractA);
            Logger.Info($"Deploy Contracts on side chain {sideTest.SideB.NodeManager.GetChainId()}: ");
            acs8ContractB = sideTest.ContractB == ""
                ? sideTest.DeployAndTransfer(sideTest.SideB)
                : new TransactionFeesContract(sideTest.SideB.NodeManager, sideTest.SideB.CallAddress,
                    sideTest.ContractB);
            
            Logger.Info("Update Rent:");
            sideTest.UpdateSideChainRentalTest(sideTest.SideB);
            
            Logger.Info($"Donate {sideTest.SideA.NodeManager.GetChainId()}:");
            sideTest.Donate(sideTest.SideA);
            Logger.Info($"Donate {sideTest.SideB.NodeManager.GetChainId()}:");
            sideTest.Donate(sideTest.SideB);

            TaskCollection.Add(RunContinueJobWithInterval(() => sideTest.ResourceFeeTestJob(acs8ContractA), 20));
            TaskCollection.Add(RunContinueJobWithInterval(() => sideTest.ResourceFeeTestJob(acs8ContractB),20));
            TaskCollection.Add(RunContinueJobWithInterval(() =>
            {
                var status = sideTest.CheckContractBalanceAndTransfer(sideTest.SideA,acs8ContractA,out List<string> symbols);
                if (status)
                {
                    var list = mainTest.Main.GetTokenBalances(mainTest.Main.CallAddress,10000_00000000);
                    mainTest.MainManager.BuyResources(mainTest.Main.CallAddress, 100000,list);
                    mainTest.TransferSideChainToken(mainTest.MainToSideA,sideTest.SideA,symbols);
                }
                Thread.Sleep(30000);
            },10));
            
            TaskCollection.Add(RunContinueJobWithInterval(() =>
            {
                var status = sideTest.CheckContractBalanceAndTransfer(sideTest.SideB,acs8ContractB,out List<string> symbols);
                if (status)
                {
                    var list = mainTest.Main.GetTokenBalances(mainTest.Main.CallAddress,10000_00000000);
                    mainTest.MainManager.BuyResources(mainTest.Main.CallAddress, 100000,list);
                    mainTest.TransferSideChainToken(mainTest.MainToSideB,sideTest.SideB,symbols);
                }
                Thread.Sleep(30000);
            },10));
            
            TaskCollection.Add(RunContinueJobWithInterval(() =>
            {
                sideTest.SideManager.QueryOwningRental(sideTest.SideB);
                var list = sideTest.SideManager.CheckCreatorRentResourceBalance(sideTest.SideB);
                if (list.Count!=0)
                    mainTest.TransferSideChainToken(mainTest.MainToSideB,sideTest.SideB,list);
                Thread.Sleep(30000);
            },10));
            
            TaskCollection.Add(RunContinueJobWithInterval(() =>
            {
                sideTest.CheckDistributed(sideTest.SideB);
                sideTest.CheckDistributed(sideTest.SideB);
                Thread.Sleep(30000);
            },10));

            Task.WaitAll(TaskCollection.ToArray());
        }
        
        
        private static Task RunContinueJobWithInterval(Action action, int seconds)
        {
            void NewAction()
            {
                while (true)
                    try
                    {
                        action.Invoke();
                        Task.Delay(1000 * seconds);
                    }
                    catch (Exception e)
                    {
                        Logger.Error(e);
                        break;
                    }
            }

            return Task.Run(NewAction);
        }
        
        private static readonly ILog Logger = Log4NetHelper.GetLogger();
        private static List<Task> TaskCollection { get; set; }

    }
}