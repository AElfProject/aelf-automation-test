using System;
using System.Linq;
using Acs1;
using AElfChain.Common;
using AElfChain.Common.Contracts;
using AElfChain.Common.ContractSerializer;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using AElf.Types;
using AElfChain.Common;
using AElfChain.Common.Managers;
using Google.Protobuf.WellKnownTypes;
using log4net;

namespace AElf.Automation.SetTransactionFees
{
    public class ContractsFee
    {
        private INodeManager NodeManager { get; set; }
        private ContractHandler ContractHandler { get; set; }
        
        private GenesisContract Genesis { get; set; }
        
        private string Caller { get; set; }

        private static ILog Logger = Log4NetHelper.GetLogger();

        public ContractsFee(INodeManager nodeManager)
        {
            NodeManager = nodeManager;
            ContractHandler = new ContractHandler();
            Caller = NodeOption.AllNodes.First().Account;
            Genesis = nodeManager.GetGenesisContract(Caller); 
        }

        public void SetAllContractsMethodFee(long amount)
        {
            var authority = new AuthorityManager(NodeManager);
            var genesisOwner = authority.GetGenesisOwnerAddress();
            var miners = authority.GetCurrentMiners();
            var systemContracts = Genesis.GetAllSystemContracts();
            
            foreach (var provider in GenesisContract.NameProviderInfos.Keys)
            {
                Logger.Info($"Begin set contract: {provider}");
                var contractInfo = ContractHandler.GetContractInfo(provider);
                var contractAddress = systemContracts[provider];
                if (contractAddress == new Address())
                {
                    Logger.Warn($"Contract {provider} not deployed.");
                    continue;
                }
                
                var contractFee = new ContractMethodFee(NodeManager, authority, contractInfo, contractAddress.GetFormatted());
                contractFee.SetContractFees(NodeOption.ChainToken, amount, genesisOwner, miners, Caller);
            }
        }

        public void QueryAllContractsMethodFee()
        {
            var systemContracts = Genesis.GetAllSystemContracts();
            foreach (var provider in GenesisContract.NameProviderInfos.Keys)
            {
                Logger.Info($"Query contract fees: {provider}");
                var contractInfo = ContractHandler.GetContractInfo(provider);
                var contractAddress = systemContracts[provider];
                if (contractAddress == new Address())
                {
                    Logger.Warn($"Contract {provider} not deployed.");
                    Console.WriteLine();
                    continue;
                }
                foreach (var method in contractInfo.ActionMethodNames)
                {
                    var feeResult = NodeManager.QueryView<MethodFees>(Caller, contractAddress.GetFormatted(),
                        "GetMethodFee", new StringValue
                        {
                            Value = method
                        });
                    if (feeResult.Fees.Count > 0)
                    {
                        var amountInfo = feeResult.Fees.First();
                        Logger.Info($"Method: {method.PadRight(48)} Symbol: {amountInfo.Symbol}   Amount: {amountInfo.BasicFee}");
                    }
                    else
                    {
                        Logger.Warn($"Method: {method.PadRight(48)} Symbol: {NodeOption.ChainToken}   Amount: 0");
                    }
                }
                Console.WriteLine();
            }
        }
    }
}