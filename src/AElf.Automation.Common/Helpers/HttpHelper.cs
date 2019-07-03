using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Serialization;
using Volo.Abp.Threading;

namespace AElf.Automation.Common.Helpers
{
    public static class HttpHelper
    {
        /// <summary>
        /// post请求
        /// </summary>
        /// <param name="url"></param>
        /// <param name="postData">post数据</param>
        /// <param name="statusCode"></param>
        /// <returns></returns>
        public static string PostResponse(string url, string postData, out string statusCode)
        {
            statusCode = string.Empty;
            if (url.StartsWith("https"))
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls;

            HttpContent httpContent = new StringContent(postData);
            httpContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            var httpClient = GetDefaultClient();
            try
            {
                var response = AsyncHelper.RunSync(() => httpClient.PostAsync(url, httpContent));

                statusCode = response.StatusCode.ToString();
                if (response.IsSuccessStatusCode)
                {
                    var result = AsyncHelper.RunSync(response.Content.ReadAsStringAsync);
                    return result;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            return string.Empty;
        }

        /// <summary>
        /// post请求
        /// </summary>
        /// <param name="url"></param>
        /// <param name="postData"></param>
        /// <param name="statusCode"></param>
        /// <param name="timeSpan"></param>
        /// <returns></returns>
        public static string PostResponse(string url, string postData, out string statusCode, out long timeSpan)
        {
            timeSpan = 0;
            statusCode = string.Empty;

            if (url.StartsWith("https"))
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls;

            HttpContent httpContent = new StringContent(postData);
            httpContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            var httpClient = GetDefaultClient();
            var exec = new Stopwatch();
            try
            {
                exec.Start();
                var response = AsyncHelper.RunSync(() => httpClient.PostAsync(url, httpContent));
                statusCode = response.StatusCode.ToString();
                if (response.IsSuccessStatusCode)
                {
                    var result = AsyncHelper.RunSync(response.Content.ReadAsStringAsync);
                    return result;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            finally
            {
                exec.Stop();
                timeSpan = exec.ElapsedMilliseconds;
            }

            return string.Empty;
        }

        /// <summary>
        /// 异步Get请求
        /// </summary>
        /// <param name="url"></param>
        /// <param name="version"></param>
        /// <param name="expectedStatusCode"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static async Task<T> GetResponseAsync<T>(string url, string version = null,
            HttpStatusCode expectedStatusCode = HttpStatusCode.OK)
        {
            //$"Get request to: {url}".WriteSuccessLine();
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
        public static async Task<T> PostResponseAsync<T>(string url, Dictionary<string, string> parameters,
            string version = null, HttpStatusCode expectedStatusCode = HttpStatusCode.OK)
        {
            //$"Post request to: {url}".WriteSuccessLine();
            var strResponse = await PostResponseAsStringAsync(url, parameters, version, expectedStatusCode);
            return JsonConvert.DeserializeObject<T>(strResponse, new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            });
        }

        private static async Task<string> GetResponseAsStringAsync(string url, string version = null,
            HttpStatusCode expectedStatusCode = HttpStatusCode.OK)
        {
            var response = await GetResponseAsync(url, version, expectedStatusCode);
            return await response.Content.ReadAsStringAsync();
        }

        private static async Task<HttpResponseMessage> GetResponseAsync(string url, string version = null,
            HttpStatusCode expectedStatusCode = HttpStatusCode.OK, int retryTimes = 0)
        {
            version = !string.IsNullOrWhiteSpace(version) ? $";v={version}" : string.Empty;
            var client = GetDefaultClient(version);

            try
            {
                var response = await client.GetAsync(url);
                if (response.StatusCode == expectedStatusCode) return response;
                var message = await response.Content.ReadAsStringAsync();
                Logger.WriteError($"StatusCode: {response.StatusCode}, Message:{message}");
                throw new HttpRequestException();
            }
            catch (Exception)
            {
                retryTimes++;
                if (retryTimes > MaxRetryTimes) throw new HttpRequestException();

                Logger.WriteWarn($"Retry GetResponseAsync request: {url}, times: {retryTimes}");
                Thread.Sleep(5000);
                return await GetResponseAsync(url, version, expectedStatusCode, retryTimes);
            }
        }

        private static async Task<string> PostResponseAsStringAsync(string url, Dictionary<string, string> parameters,
            string version = null, HttpStatusCode expectedStatusCode = HttpStatusCode.OK)
        {
            var response = await PostResponseAsync(url, parameters, version, true, expectedStatusCode);
            return await response.Content.ReadAsStringAsync();
        }

        private static async Task<HttpResponseMessage> PostResponseAsync(string url,
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
                if (response.StatusCode == expectedStatusCode) return response;
                var message = await response.Content.ReadAsStringAsync();
                Logger.WriteError($"StatusCode: {response.StatusCode}, Message:{message}");
                throw new HttpRequestException();
            }
            catch (Exception)
            {
                retryTimes++;
                if (retryTimes > MaxRetryTimes) throw new HttpRequestException();

                Logger.WriteWarn($"Retry PostResponseAsync request: {url}, times: {retryTimes}");
                Thread.Sleep(5000);
                return await PostResponseAsync(url, parameters, version, useApplicationJson, expectedStatusCode);
            }
        }

        public static async Task<T> DeleteResponseAsObjectAsync<T>(string url, string version = null,
            HttpStatusCode expectedStatusCode = HttpStatusCode.OK)
        {
            var strResponse = await DeleteResponseAsStringAsync(url, version, expectedStatusCode);
            return JsonConvert.DeserializeObject<T>(strResponse, new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            });
        }

        private static async Task<string> DeleteResponseAsStringAsync(string url, string version = null,
            HttpStatusCode expectedStatusCode = HttpStatusCode.OK)
        {
            var response = await DeleteResponseAsync(url, version, expectedStatusCode);
            return await response.Content.ReadAsStringAsync();
        }

        private static async Task<HttpResponseMessage> DeleteResponseAsync(string url, string version = null,
            HttpStatusCode expectedStatusCode = HttpStatusCode.OK, int retryTimes = 0)
        {
            version = !string.IsNullOrWhiteSpace(version) ? $";v={version}" : string.Empty;
            var client = GetDefaultClient(version);

            try
            {
                var response = await client.DeleteAsync(url);
                if (response.StatusCode == expectedStatusCode) return response;
                var message = await response.Content.ReadAsStringAsync();
                Logger.WriteError($"StatusCode: {response.StatusCode}, Message:{message}");
                throw new HttpRequestException();
            }
            catch (Exception e)
            {
                retryTimes++;
                if (retryTimes > MaxRetryTimes) throw new HttpRequestException();

                Logger.WriteWarn($"Retry DeleteResponseAsync request: {url}, times: {retryTimes}");
                Thread.Sleep(5000);
                return await DeleteResponseAsync(url, version, expectedStatusCode);
            }
        }

        private static HttpClient GetDefaultClient(string version = null)
        {
            if (Client != null) return Client;

            Client = new HttpClient
            {
                Timeout = TimeSpan.FromMinutes(3)
            };
            Client.DefaultRequestHeaders.Accept.Clear();
            Client.DefaultRequestHeaders.Accept.Add(
                MediaTypeWithQualityHeaderValue.Parse($"application/json{version}"));
            Client.DefaultRequestHeaders.Add("Connection", "close");

            return Client;
        }

        private static int MaxRetryTimes { get; set; } = 5;
        private static HttpClient Client { get; set; }
        private static readonly ILogHelper Logger = LogHelper.GetLogHelper();
    }
}