using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Serialization;
using Shouldly;

namespace AElf.Automation.Common.Helpers
{
    public static class HttpHelper
    {
        private static readonly HttpClient Client = new HttpClient();
        
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

            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            httpClient.DefaultRequestHeaders.Add("Connection", "close");
            try
            {
                HttpResponseMessage response = httpClient.PostAsync(url, httpContent).Result;

                statusCode = response.StatusCode.ToString();
                if (response.IsSuccessStatusCode)
                {
                    string result = response.Content.ReadAsStringAsync().Result;
                    return result;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            finally
            {
                httpClient.Dispose();
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

            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            httpClient.DefaultRequestHeaders.Add("Connection", "close");
            Stopwatch exec = new Stopwatch();
            try
            {
                exec.Start();
                HttpResponseMessage response = httpClient.PostAsync(url, httpContent).Result;
                statusCode = response.StatusCode.ToString();
                if (response.IsSuccessStatusCode)
                {
                    string result = response.Content.ReadAsStringAsync().Result;
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
                httpClient.Dispose();
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
        /// <param name="paramters"></param>
        /// <param name="version"></param>
        /// <param name="expectedStatusCode"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static async Task<T> PostResponseAsync<T>(string url, Dictionary<string, string> paramters,
            string version = null, HttpStatusCode expectedStatusCode = HttpStatusCode.OK)
        {
            var strResponse = await PostResponseAsStringAsync(url, paramters, version, expectedStatusCode);
            return JsonConvert.DeserializeObject<T>(strResponse, new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            });
        }

        private static async Task<string> GetResponseAsStringAsync(string url,string version = null,
            HttpStatusCode expectedStatusCode = HttpStatusCode.OK)
        {
            var response = await GetResponseAsync(url, version, expectedStatusCode);
            return await response.Content.ReadAsStringAsync();
        }

        private static async Task<HttpResponseMessage> GetResponseAsync(string url,string version = null,
            HttpStatusCode expectedStatusCode = HttpStatusCode.OK)
        {
            version = !string.IsNullOrWhiteSpace(version) ? $";v={version}" : string.Empty;
            Client.DefaultRequestHeaders.Accept.Clear();
            Client.DefaultRequestHeaders.Accept.Add(MediaTypeWithQualityHeaderValue.Parse($"application/json{version}"));
            
            var response = await Client.GetAsync(url);
            response.StatusCode.ShouldBe(expectedStatusCode);
            return response;
        }

        private static async Task<string> PostResponseAsStringAsync(string url, Dictionary<string, string> paramters,
            string version = null, HttpStatusCode expectedStatusCode = HttpStatusCode.OK)
        {
            var response = await PostResponseAsync(url, paramters, version, true, expectedStatusCode);
            return await response.Content.ReadAsStringAsync();
        }

        private static async Task<HttpResponseMessage> PostResponseAsync(string url, Dictionary<string, string> paramters,
            string version = null, bool useApplicationJson = false,
            HttpStatusCode expectedStatusCode = HttpStatusCode.OK)
        {
            version = !string.IsNullOrWhiteSpace(version) ? $";v={version}" : string.Empty;
            HttpContent content;
            if (useApplicationJson)
            {
                Client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                var paramsStr = JsonConvert.SerializeObject(paramters);
                content = new StringContent(paramsStr,Encoding.UTF8, "application/json");
                content.Headers.ContentType = MediaTypeHeaderValue.Parse($"application/json{version}");
            }
            else
            {
                content = new FormUrlEncodedContent(paramters);
                content.Headers.ContentType = MediaTypeHeaderValue.Parse($"application/x-www-form-urlencoded{version}");
            }
            
            var response = await Client.PostAsync(url, content);
            response.StatusCode.ShouldBe(expectedStatusCode);
            return response;
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

        public static async Task<string> DeleteResponseAsStringAsync(string url, string version = null,
            HttpStatusCode expectedStatusCode = HttpStatusCode.OK)
        {
            var response = await DeleteResponseAsync(url, version, expectedStatusCode);
            return await response.Content.ReadAsStringAsync();
        }

        public static async Task<HttpResponseMessage> DeleteResponseAsync(string url, string version = null,
            HttpStatusCode expectedStatusCode = HttpStatusCode.OK)
        {
            version = !string.IsNullOrWhiteSpace(version) ? $";v={version}" : string.Empty;
            Client.DefaultRequestHeaders.Accept.Clear();
            Client.DefaultRequestHeaders.Accept.Add(MediaTypeWithQualityHeaderValue.Parse($"application/json{version}"));
            
            var response = await Client.DeleteAsync(url);
            response.StatusCode.ShouldBe(expectedStatusCode);
            return response;
        }
    }
}
