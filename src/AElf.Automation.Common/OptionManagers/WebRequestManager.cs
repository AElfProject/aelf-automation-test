using System;
using AElf.Automation.Common.Helpers;
using System.Collections.Generic;
using System.Diagnostics;

namespace AElf.Automation.Common.OptionManagers
{
    public class WebRequestManager
    {
        private string BaseUrl { get; set; }
        private readonly ILogHelper _log = LogHelper.GetLogHelper();

        public WebRequestManager(string baseUrl)
        {
            BaseUrl = baseUrl.Replace("/chain", "");
        }

        public T GetResponse<T>(string url, out string returnCode, out long timeSpan)
        {
            timeSpan = 0L;
            Stopwatch watch = new Stopwatch();
            try
            {
                watch.Start();
                returnCode = "OK";

                return HttpHelper.GetResponseAsync<T>($"{BaseUrl}{url}").Result;
            }
            catch (Exception)
            {
                returnCode = "Others";
                throw;
            }
            finally
            {
                watch.Stop();
                timeSpan = watch.ElapsedMilliseconds;
            }
        }

        public T PostResponse<T>(string url, Dictionary<string, string> parameters, out string returnCode,
            out long timeSpan)
        {
            timeSpan = 0L;
            Stopwatch watch = new Stopwatch();
            try
            {
                watch.Start();
                returnCode = "OK";

                return HttpHelper.PostResponseAsync<T>($"{BaseUrl}{url}", parameters).Result;
            }
            catch (Exception)
            {
                returnCode = "Others";
                throw;
            }
            finally
            {
                watch.Stop();
                timeSpan = watch.ElapsedMilliseconds;
            }
        }

        public T DeleteResponse<T>(string url, out string returnCode, out long timeSpan)
        {
            timeSpan = 0L;
            Stopwatch watch = new Stopwatch();
            try
            {
                watch.Start();
                returnCode = "OK";

                return HttpHelper.DeleteResponseAsObjectAsync<T>($"{BaseUrl}{url}").Result;
            }
            catch (Exception)
            {
                returnCode = "Others";
                throw;
            }
            finally
            {
                watch.Stop();
                timeSpan = watch.ElapsedMilliseconds;
            }
        }
    }
}