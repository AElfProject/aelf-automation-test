using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace AElfChain.SDK
{
    public interface IHttpService
    {
        void SetFailRetryTimes(int times);
        Task<T> GetResponseAsync<T>(string url, string version = null,
            HttpStatusCode expectedStatusCode = HttpStatusCode.OK);

        Task<T> PostResponseAsync<T>(string url, Dictionary<string, string> parameters,
            string version = null, HttpStatusCode expectedStatusCode = HttpStatusCode.OK);

        Task<T> DeleteResponseAsObjectAsync<T>(string url, string version = null,
            HttpStatusCode expectedStatusCode = HttpStatusCode.OK);
    }
}