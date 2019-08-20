using System.Collections.Generic;
using AElfChain.SDK;

namespace AElf.Automation.QueryTransaction
{
    public class TransactionPoolQuery
    {
        private string[] _urlCollection;
        private List<IApiService> _apiCollection;
        
        public TransactionPoolQuery(string[] urlCollection)
        {
            _urlCollection = urlCollection;
        }
    }
}