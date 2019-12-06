using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace AElfChain.SDK
{
    public class HttpService : IHttpService
    {
        private HttpClient Client { get; set; }
        public int FailRetryTimes { get; set; }
        private int TimeoutSeconds { get; }

        public HttpService(int timeout, int retryTimes)
        {
            TimeoutSeconds = timeout;
            FailRetryTimes = retryTimes;
        }

        public void SetFailRetryTimes(int times)
        {
            FailRetryTimes = times;
        }

        /// <summary>
        /// 异步Get请求
        /// </summary>
        /// <param name="url"></param>
        /// <param name="version"></param>
        /// <param name="expectedStatusCode"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public async Task<T> GetResponseAsync<T>(string url, string version = null,
            HttpStatusCode expectedStatusCode = HttpStatusCode.OK)
        {
            var strResponse = await GetResponseAsStringAsync(url, version, expectedStatusCode);
            return JsonConvert.DeserializeObject<T>(strResponse, new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            });
        }

        /// <summary>
        /// 异步Post请求
        /// </summary>
        /// <param name="url"></param>
        /// <param name="parameters"></param>
        /// <param name="version"></param>
        /// <param name="expectedStatusCode"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public async Task<T> PostResponseAsync<T>(string url, Dictionary<string, string> parameters,
            string version = null, HttpStatusCode expectedStatusCode = HttpStatusCode.OK)
        {
            var strResponse = await PostResponseAsStringAsync(url, parameters, version, expectedStatusCode);
            return JsonConvert.DeserializeObject<T>(strResponse, new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            });
        }

        /// <summary>
        /// 异步Delete请求
        /// </summary>
        /// <param name="url"></param>
        /// <param name="version"></param>
        /// <param name="expectedStatusCode"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public async Task<T> DeleteResponseAsObjectAsync<T>(string url, string version = null,
            HttpStatusCode expectedStatusCode = HttpStatusCode.OK)
        {
            var strResponse = await DeleteResponseAsStringAsync(url, version, expectedStatusCode);
            return JsonConvert.DeserializeObject<T>(strResponse, new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            });
        }

        private async Task<string> GetResponseAsStringAsync(string url, string version = null,
            HttpStatusCode expectedStatusCode = HttpStatusCode.OK)
        {
            var response = await GetResponseAsync(url, version, expectedStatusCode);
            return await response.Content.ReadAsStringAsync();
        }

        private async Task<HttpResponseMessage> GetResponseAsync(string url, string version = null,
            HttpStatusCode expectedStatusCode = HttpStatusCode.OK, int retryTimes = 0)
        {
            version = !string.IsNullOrWhiteSpace(version) ? $";v={version}" : string.Empty;

            var client = GetDefaultClient(version);
            try
            {
                var response = await client.GetAsync(url);
                if (response.StatusCode == expectedStatusCode)
                    return response;
                var message = await response.Content.ReadAsStringAsync();
                throw new AElfChainApiException(message);
            }
            catch (Exception ex)
            {
                retryTimes++;
                if (retryTimes > FailRetryTimes) throw new AElfChainApiException(ex.Message);
                Thread.Sleep(1000);
                return await GetResponseAsync(url, version, expectedStatusCode, retryTimes);
            }
        }

        private async Task<string> PostResponseAsStringAsync(string url, Dictionary<string, string> parameters,
            string version = null, HttpStatusCode expectedStatusCode = HttpStatusCode.OK)
        {
            var response = await PostResponseAsync(url, parameters, version, true, expectedStatusCode);
            return await response.Content.ReadAsStringAsync();
        }

        private async Task<HttpResponseMessage> PostResponseAsync(string url,
            Dictionary<string, string> parameters,
            string version = null, bool useApplicationJson = false,
            HttpStatusCode expectedStatusCode = HttpStatusCode.OK, int retryTimes = 0)
        {
            version = !string.IsNullOrWhiteSpace(version) ? $";v={version}" : string.Empty;
            var client = GetDefaultClient(version);

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
                Console.WriteLine($"StatusCode: {response.StatusCode}, Message:{message}");
                throw new HttpRequestException();
            }
            catch (Exception ex)
            {
                retryTimes++;
                if (retryTimes > FailRetryTimes) throw new AElfChainApiException(ex.Message);;

                Console.WriteLine($"Retry PostResponseAsync request: {url}, times: {retryTimes}");
                Thread.Sleep(5000);
                return await PostResponseAsync(url, parameters, version, useApplicationJson, expectedStatusCode,
                    retryTimes);
            }
        }

        private async Task<string> DeleteResponseAsStringAsync(string url, string version = null,
            HttpStatusCode expectedStatusCode = HttpStatusCode.OK)
        {
            var response = await DeleteResponseAsync(url, version, expectedStatusCode);
            return await response.Content.ReadAsStringAsync();
        }

        private async Task<HttpResponseMessage> DeleteResponseAsync(string url, string version = null,
            HttpStatusCode expectedStatusCode = HttpStatusCode.OK, int retryTimes = 0)
        {
            version = !string.IsNullOrWhiteSpace(version) ? $";v={version}" : string.Empty;
            var client = GetDefaultClient(version);
            try
            {
                var response = await client.DeleteAsync(url);
                if (response.StatusCode == expectedStatusCode)
                    return response;
                var message = await response.Content.ReadAsStringAsync();
                throw new AElfChainApiException(message);
            }
            catch (Exception ex)
            {
                retryTimes++;
                if (retryTimes > FailRetryTimes) throw new AElfChainApiException(ex.Message);
                Thread.Sleep(500);
                return await DeleteResponseAsync(url, version, expectedStatusCode);
            }
        }

        private HttpClient GetDefaultClient(string version = null)
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
    }
}