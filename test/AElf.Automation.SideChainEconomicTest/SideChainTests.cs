using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using AElf.Automation.SideChainEconomicTest.EconomicTest;
using AElf.Contracts.MultiToken;
using AElfChain.Common.Contracts;

namespace AElf.Automation.SideChainEconomicTest
{
    public class SideChainTests : TestBase
    {
        private TransactionFeesContract _acs8Contract;

        public void GetTokenInfo(ContractServices services)
        {
            Logger.Info("Query side chain token info");

            services.GetTokenBalances(services.CallAddress);
            services.GetPrimaryToken(services.CallAddress);

            var primaryToken = services.TokenService.GetPrimaryTokenSymbol();
            var miners = GetMiners();
            foreach (var miner in miners)
            {
                var balance = services.TokenService.GetUserBalance(miner,primaryToken);
                if (balance > 100_00000000)
                    continue;
                services.TransferPrimaryToken(services.CallAddress,miner,1000_00000000);
                services.GetPrimaryToken(miner);
            }
        }

        public TransactionFeesContract DeployAndTransfer(ContractServices services)
        {
            var contract = SideManager.DeployContractResources(services);
            _acs8Contract = new TransactionFeesContract(services.NodeManager,services.CallAddress, contract);
            CheckContractBalanceAndTransfer(services, _acs8Contract, out List<string> symbolList);
            _acs8Contract.InitializeTxFees(_acs8Contract.Contract);
            return _acs8Contract;
        }

        public void UpdateSideChainRentalTest(ContractServices services)
        {
            var input = new UpdateRentalInput
            {
                Rental =
                {
                    {"CPU", 100},
                    {"RAM", 50},
                    {"DISK", 4},
                    {"NET", 2}
                }
            };
            SideManager.ProposalThroughController(services, input, nameof(TokenMethod.UpdateRental));
        }

        public void Donate(ContractServices services)
        {
            var check = services.ConsensusService.GetSymbolList();
            var list = new List<string>();
            foreach (var symbol in services.FeeResourceSymbols)
            {
                var status = check.Value.Contains(symbol);
                if (status == false)
                    list.Add(symbol);
            }

            if (list.Count == 0) return;
            {
                foreach (var symbol in list)
                    services.DonateSideChainDividendsPool(symbol,1_00000000);
            }
            services.ConsensusService.GetSymbolList();
        }

        public void CheckDistributed(ContractServices services)
        {
            services.ConsensusService.GetUndistributedDividends(); 
            services.ConsensusService.GetDividends();
        }
        
        public void CheckConsensusBalance(ContractServices services)
        {
            var consensus = services.ConsensusService;
            Logger.Info($"{services.NodeManager.GetChainId()}: {consensus.ContractAddress}");
            foreach (var symbol in services.FeeResourceSymbols)
            {
                var balance = services.TokenService.GetUserBalance(consensus.ContractAddress,symbol);
                Logger.Info($" {consensus.ContractAddress} Token '{symbol}' balance = {balance}");
            }
        }

        public void ResourceFeeTest(TransactionFeesContract txContract)
        {
            SideManager.OnlyCpuCounterAction(txContract);
            SideManager.OnlyRamCounterAction(txContract);
            SideManager.BothCpuAndRamCounterAction(txContract);
            SideManager.NoCpuAndRamCounterAction(txContract);
        }

        public bool CheckContractBalanceAndTransfer(ContractServices services, TransactionFeesContract txContract,out List<string> symbolList)
        {
            var isNeedCrossTransfer = false;
            var symbols = SideManager.CheckContractResourceBalance(services, txContract.ContractAddress);
            var needTransferSymbols = symbols.Keys.Where(k => symbols[k] < 1000_00000000).ToList();
            var list = services.GetTokenBalances(services.CallAddress,2000_00000000, services.FeeResourceSymbols);
            if (needTransferSymbols.Count != 0 && list.Count == 0)
                services.TransferResources(services.CallAddress, txContract.ContractAddress, 2000_00000000,
                    needTransferSymbols);
            if (list.Count != 0)
            {
                isNeedCrossTransfer = true;
                Logger.Info("Need cross chain transfer first: ");
            }
            symbolList = list;
            return isNeedCrossTransfer;
        }

        public bool CheckBalanceAndTransfer(ContractServices services)
        {
            var isNeedCrossTransfer = false;
            var symbols = services.TokenService.GetPrimaryTokenSymbol().Equals("ELF")
                ? services.FeeResourceSymbols
                : services.Symbols;
            var list = services.GetTokenBalances(services.CallAddress,2000_00000000,symbols);
            if (list.Count != 0)
            {
                isNeedCrossTransfer = true;
                Logger.Info("Need cross chain transfer first: ");
            }
            
            return isNeedCrossTransfer;
        }
        public void CheckMinersRentResource()
        {
            var miners = GetMiners();
            foreach (var miner in miners)
            foreach (var symbol in SideB.RentResourceSymbols)
            {
                var balance = SideA.TokenService.GetUserBalance(miner, symbol);
                Logger.Info($"{miner}: {symbol}={balance}");
            }
        }

        public void TakeBakeResource(ContractServices services,TransactionFeesContract txContract)
        {
            var symbols = services.FeeResourceSymbols;
            var miners = GetMiners();
            foreach (var miner in miners)
            foreach (var symbol in symbols)
            {
                var balance = services.TokenService.GetUserBalance(miner,symbol);
                if (balance <= 100_00000000) continue;
                var transferAmount = balance - 100_00000000;
                services.TokenService.SetAccount(miner);
                services.TokenService.TransferBalance(miner, txContract.ContractAddress,
                    transferAmount,symbol);
            }
        }

        public void ResourceFeeTestJob(TransactionFeesContract txContract)
        {
            ExecuteStandaloneTask(new Action[]
            {
                () => ResourceFeeTest(txContract)
            });
        }

    }
}