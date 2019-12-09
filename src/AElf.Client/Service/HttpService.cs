using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace AElf.Client.Service
{
    public interface IHttpService
    {
        Task<T> GetResponseAsync<T>(string url, string version = null,
            HttpStatusCode expectedStatusCode = HttpStatusCode.OK);

        Task<T> PostResponseAsync<T>(string url, Dictionary<string, string> parameters,
            string version = null, HttpStatusCode expectedStatusCode = HttpStatusCode.OK);

        Task<T> DeleteResponseAsObjectAsync<T>(string url, string version = null,
            HttpStatusCode expectedStatusCode = HttpStatusCode.OK);
    }

    public class HttpService : IHttpService
    {
        private HttpClient Client { get; set; }
        private int TimeoutSeconds { get; }

        public HttpService(int timeoutSeconds)
        {
            TimeoutSeconds = timeoutSeconds;
        }

        /// <summary>
        /// Get request.
        /// </summary>
        /// <param name="url"></param>
        /// <param name="version"></param>
        /// <param name="expectedStatusCode"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public async Task<T> GetResponseAsync<T>(string url, string version = null,
            HttpStatusCode expectedStatusCode = HttpStatusCode.OK)
        {
            var response = await GetResponseAsStringAsync(url, version, expectedStatusCode);
            return JsonConvert.DeserializeObject<T>(response, new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            });
        }

        /// <summary>
        /// Post request.
        /// </summary>
        /// <param name="url"></param>
        /// <param name="parameters"></param>
        /// <param name="version"></param>
        /// <param name="expectedStatusCode"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public async Task<T> PostResponseAsync<T>(string url, Dictionary<string, string> parameters,
            string version = null,
            HttpStatusCode expectedStatusCode = HttpStatusCode.OK)
        {
            var response = await PostResponseAsStringAsync(url, parameters, version, expectedStatusCode);
            return JsonConvert.DeserializeObject<T>(response, new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            });
        }

        /// <summary>
        /// Delete request.
        /// </summary>
        /// <param name="url"></param>
        /// <param name="version"></param>
        /// <param name="expectedStatusCode"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public async Task<T> DeleteResponseAsObjectAsync<T>(string url, string version = null,
            HttpStatusCode expectedStatusCode = HttpStatusCode.OK)
        {
            var response = await DeleteResponseAsStringAsync(url, version, expectedStatusCode);
            return JsonConvert.DeserializeObject<T>(response, new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            });
        }

        #region GetResponse

        private async Task<string> GetResponseAsStringAsync(string url, string version = null,
            HttpStatusCode expectedStatusCode = HttpStatusCode.OK)
        {
            var response = await GetResponseAsync(url, version, expectedStatusCode);
            return await response.Content.ReadAsStringAsync();
        }

        private async Task<HttpResponseMessage> GetResponseAsync(string url, string version = null,
            HttpStatusCode expectedStatusCode = HttpStatusCode.OK)
        {
            version = !string.IsNullOrWhiteSpace(version) ? $";v={version}" : string.Empty;

            var client = GetHttpClient(version);
            try
            {
                var response = await client.GetAsync(url);
                if (response.StatusCode == expectedStatusCode)
                    return response;
                var message = await response.Content.ReadAsStringAsync();
                throw new AElfClientException(message);
            }
            catch (Exception ex)
            {
                throw new AElfClientException(ex.Message);
            }
        }

        #endregion

        #region PostResponse

        private async Task<string> PostResponseAsStringAsync(string url, Dictionary<string, string> parameters,
            string version = null, HttpStatusCode expectedStatusCode = HttpStatusCode.OK)
        {
            var response = await PostResponseAsync(url, parameters, version, true, expectedStatusCode);
            return await response.Content.ReadAsStringAsync();
        }

        private async Task<HttpResponseMessage> PostResponseAsync(string url,
            Dictionary<string, string> parameters,
            string version = null, bool useApplicationJson = false,
            HttpStatusCode expectedStatusCode = HttpStatusCode.OK)
        {
            version = !string.IsNullOrWhiteSpace(version) ? $";v={version}" : string.Empty;
            var client = GetHttpClient(version);

            HttpContent content;
            if (useApplicationJson)
            {
                var paramsStr = JsonConvert.SerializeObject(parameters);
                content = new StringContent(paramsStr, Encoding.UTF8, "application/json");
                content.Headers.ContentType = MediaTypeHeaderValue.Parse($"application/json{version}");
            }

            else
            {
                content = new FormUrlEncodedContent(parameters);
                content.Headers.ContentType =
                    MediaTypeHeaderValue.Parse($"application/x-www-form-urlencoded{version}");
            }

            try
            {
                var response = await client.PostAsync(url, content);
                if (response.StatusCode == expectedStatusCode)
                    return response;
                var message = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException(message);
            }
            catch (Exception ex)
            {
                throw new AElfClientException(ex.Message);
            }
        }

        #endregion

        #region DeleteResponse

        private async Task<string> DeleteResponseAsStringAsync(string url, string version = null,
            HttpStatusCode expectedStatusCode = HttpStatusCode.OK)
        {
            var response = await DeleteResponseAsync(url, version, expectedStatusCode);
            return await response.Content.ReadAsStringAsync();
        }

        private async Task<HttpResponseMessage> DeleteResponseAsync(string url, string version = null,
            HttpStatusCode expectedStatusCode = HttpStatusCode.OK)
        {
            version = !string.IsNullOrWhiteSpace(version) ? $";v={version}" : string.Empty;
            var client = GetHttpClient(version);
            try
            {
                var response = await client.DeleteAsync(url);
                if (response.StatusCode == expectedStatusCode)
                    return response;
                var message = await response.Content.ReadAsStringAsync();
                throw new AElfClientException(message);
            }
            catch (Exception ex)
            {
                throw new AElfClientException(ex.Message);
            }
        }

        #endregion

        #region private methods

        private HttpClient GetHttpClient(string version = null)
        {
            if (Client != null) return Client;
            Client = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(TimeoutSeconds)
            };
            Client.DefaultRequestHeaders.Accept.Clear();
            Client.DefaultRequestHeaders.Accept.Add(
                MediaTypeWithQualityHeaderValue.Parse($"application/json{version}"));
            Client.DefaultRequestHeaders.Add("Connection", "close");
            return Client;
        }

        #endregion
    }
}