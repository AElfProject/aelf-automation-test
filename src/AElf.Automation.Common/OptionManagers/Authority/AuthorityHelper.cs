using AElf.Automation.Common.Helpers;
using AElf.Automation.Common.WebApi;

namespace AElf.Automation.Common.OptionManagers.Authority
{
    public class AuthorityHelper
    {
        private readonly ILogHelper _logger = LogHelper.GetLogHelper();
        private IApiService _apiService;
        
        public AuthorityHelper(string serviceUrl)
        {
            _apiService = new WebApiService(serviceUrl);
        }
        
        
    }
}