using System.Collections.Generic;
using AElf.Automation.Common.Contracts;

namespace AElf.Automation.ScenariosExecution.Scenarios
{
    public class ResourceScenario : BaseScenario
    {
        public TokenContract Token { get; set; }
        public TokenConverterContract TokenConverter { get; set; }
        public List<string> Testers { get; }
        public ResourceScenario()
        {
            InitializeScenario();

            Token = Services.TokenService;
            TokenConverter = new TokenConverterContract(Services.ApiHelper, Services.CallAddress);
            Testers = AllTesters.GetRange(5, 20);
        }

        public void RunResourceScenario()
        {
            
        }
        
        public void BuyResourceAction()
        {
            
        }

        public void SellResourceAction()
        {
            
        }

        public void InitializeTokenConverter()
        {
            
        }
    }
}