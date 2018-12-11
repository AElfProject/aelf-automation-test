using AElf.Automation.Common.Helpers;

namespace AElf.Automation.ContractsTesting.Contracts
{
    public class ProposalContract: ContractBase
    {
        public ProposalContract(CliHelper ch, string contractAbi):
            base(ch, contractAbi)
        {
        }

        public void InitlizeProprosal()
        {
            LoadContractAbi();
        }


    }
}