using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace AElf.Automation.Common.Helpers
{
    public static class DataHelper
    {
        private static readonly ILogHelper Logger = LogHelper.GetLogHelper();

        public static bool TryGetValueFromJson(out string value, string jsonInfo, params string[] pathArray)
        {
            value = string.Empty;

            JToken info = JObject.Parse(jsonInfo);
            foreach (var path in pathArray)
            {
                if (info[path] != null)
                    info = info[path];
                else
                {
                    Logger.WriteError($"Child path '{path}' not exist.");
                    return false;
                }
            }

            value = info.ToString();

            return true;
        }

        public static bool TryGetValueFromJson(out string value, JObject jsonInfo, params string[] pathArray)
        {
            value = string.Empty;

            JToken info = jsonInfo;
            foreach (var path in pathArray)
            {
                if (info[path] != null)
                    info = info[path];
                else
                {
                    Logger.WriteError($"Child path '{path}' not exist.");
                    return false;
                }
            }

            value = info.ToString();

            return true;
        }

        public static bool TryGetArrayFromJson(out List<string> valueList, string jsonInfo, params string[] pathArray)
        {
            valueList = new List<string>();

            JToken info = JObject.Parse(jsonInfo);
            for (int i=0; i<pathArray.Length; i++)
            {
                if (info[pathArray[i]] != null)
                {
                    if (i != pathArray.Length - 1)
                    {
                        info = info[pathArray[i]];
                        continue;
                    }
                    else
                    {
                        JArray array = (JArray) info[pathArray[i]];
                        foreach (var item in array)
                        {
                            var value = item.ToString();
                            //valueList.Add(item.Value<string>());
                            valueList.Add(value);

                        }
                    }

                    return true;
                }
                else
                {
                    Logger.WriteError($"Child path '{pathArray[i]}' not exist.");
                    return false;
                }
            }

            return true;
        }

        public static bool TryGetArrayFromJson(out List<string> valueList, JObject jsonInfo, params string[] pathArray)
        {
            valueList = new List<string>();

            JToken info = jsonInfo;
            for (int i=0; i<pathArray.Length; i++)
            {
                if (info[pathArray[i]] != null)
                {
                    if (i != pathArray.Length - 1)
                    {
                        info = info[pathArray[i]];
                        continue;
                    }
                    else
                    {
                        JArray array = (JArray) info[pathArray[i]];
                        foreach (var item in array)
                        {
                            valueList.Add(item.Value<string>());
                        }
                    }

                    return true;
                }
                else
                {
                    Logger.WriteError($"Child path '{pathArray[i]}' not exist.");
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 转换十六进制字符串
        /// </summary>
        /// <param name="hexString"></param>
        /// <param name="isHex">是否是十六进制数值</param>
        /// <returns></returns>
        public static string ConvertHexInfo(string hexString, bool isHex = false)
        {
            if (isHex)
                return ConvertHexToValue(hexString).ToString();

            return ConvertHexToString(hexString);
        }

        public static string ConvertHexToString(string hexString)
        {
            string strValue = "";
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
               Logger.WriteError($"Convert hex string got exception. Hex string value: {hexString}");
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
                Logger.WriteError($"Convert hex value got exception. Hex value: {hexValue}");
            }

            return value;
        }
    }
}