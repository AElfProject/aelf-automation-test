using System;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace AElf.Automation.CliTesting.Http
{
    public class HttpRequestor
    {
        private HttpClient _client;
        private string _serverUrl;

        public HttpRequestor(string serverUrl)
        {
            _serverUrl = serverUrl;
            _client = new HttpClient();
            _client.BaseAddress = new Uri(_serverUrl);
            _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public string DoRequest(string content)
        {
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "/");
            request.Content = new StringContent(content, Encoding.UTF8, "application/json");

            string result = null;
            try
            {
                var response = _client.SendAsync(request).Result;
                result = response.Content.ReadAsStringAsync().Result;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            return result;
        }

        //Performance testing
        public string DoRequest(string content, out long mileSecond)
        {
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "/");
            request.Content = new StringContent(content, Encoding.UTF8, "application/json");

            string result = null;
            mileSecond = 0;
            Stopwatch exec = new Stopwatch();
            try
            {
                exec.Start();
                var response = _client.SendAsync(request).Result;
                result = response.Content.ReadAsStringAsync().Result;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            finally
            {
                exec.Stop();
                mileSecond = exec.ElapsedMilliseconds;
            }

            return result;
        }
    }
}
