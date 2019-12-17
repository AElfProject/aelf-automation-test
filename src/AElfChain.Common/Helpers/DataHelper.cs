using System;
using System.Collections.Generic;
using log4net;
using Newtonsoft.Json.Linq;

namespace AElfChain.Common.Helpers
{
    public static class DataHelper
    {
        private static readonly ILog Logger = Log4NetHelper.GetLogger();

        public static bool TryGetValueFromJson(out string value, string jsonInfo, params string[] pathArray)
        {
            value = string.Empty;

            JToken info = JObject.Parse(jsonInfo);
            foreach (var path in pathArray)
                if (info[path] != null)
                {
                    info = info[path];
                }
                else
                {
                    Logger.Error($"Child path '{path}' not exist.");
                    return false;
                }

            value = info.ToString();

            return true;
        }

        public static bool TryGetValueFromJson(out string value, JObject jsonInfo, params string[] pathArray)
        {
            value = string.Empty;

            JToken info = jsonInfo;
            foreach (var path in pathArray)
                if (info[path] != null)
                {
                    info = info[path];
                }
                else
                {
                    Logger.Error($"Child path '{path}' not exist.");
                    return false;
                }

            value = info.ToString();

            return true;
        }

        public static bool TryGetArrayFromJson(out List<string> valueList, string jsonInfo, params string[] pathArray)
        {
            valueList = new List<string>();

            JToken info = JObject.Parse(jsonInfo);
            for (var i = 0; i < pathArray.Length; i++)
                if (info[pathArray[i]] != null)
                {
                    if (i != pathArray.Length - 1)
                    {
                        info = info[pathArray[i]];
                        continue;
                    }

                    var array = (JArray) info[pathArray[i]];
                    foreach (var item in array)
                    {
                        var value = item.ToString();
                        valueList.Add(value);
                    }

                    return true;
                }
                else
                {
                    Logger.Error($"Child path '{pathArray[i]}' not exist.");
                    return false;
                }

            return true;
        }

        public static bool TryGetArrayFromJson(out List<string> valueList, JObject jsonInfo, params string[] pathArray)
        {
            valueList = new List<string>();

            JToken info = jsonInfo;
            for (var i = 0; i < pathArray.Length; i++)
                if (info[pathArray[i]] != null)
                {
                    if (i != pathArray.Length - 1)
                    {
                        info = info[pathArray[i]];
                        continue;
                    }

                    var array = (JArray) info[pathArray[i]];
                    foreach (var item in array) valueList.Add(item.Value<string>());

                    return true;
                }
                else
                {
                    Logger.Error($"Child path '{pathArray[i]}' not exist.");
                    return false;
                }

            return true;
        }

        /// <summary>
        ///     convert hex value
        /// </summary>
        /// <param name="hexString"></param>
        /// <param name="isHex">whether hex type</param>
        /// <returns></returns>
        public static string ConvertHexInfo(string hexString, bool isHex = false)
        {
            return isHex ? ConvertHexToValue(hexString).ToString() : ConvertHexToString(hexString);
        }

        public static string ConvertHexToString(string hexString)
        {
            var strValue = "";
            try
            {
                while (hexString.Length > 0)
                {
                    strValue += Convert.ToChar(Convert.ToUInt32(hexString.Substring(0, 2), 16)).ToString();
                    hexString = hexString.Substring(2, hexString.Length - 2);
                }
            }
            catch (Exception)
            {
                Logger.Error($"Convert hex string got exception. Hex string value: {hexString}");
            }


            return strValue;
        }

        public static long ConvertHexToValue(string hexValue)
        {
            long value = 0;
            try
            {
                value = Convert.ToInt64(hexValue, 16);
            }
            catch (Exception)
            {
                Logger.Error($"Convert hex value got exception. Hex value: {hexValue}");
            }

            return value;
        }
    }
}