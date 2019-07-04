using Newtonsoft.Json;

namespace AElf.Automation.Common.Helpers
{
    public static class ConvertHelper
    {
        /// <summary>
        /// 将object对象转换为Json数据
        /// </summary>
        /// <param name="obj">object对象</param>
        /// <returns>转换后的json字符串</returns>
        public static string ObjectToJson(object obj)
        {
            return obj == null ? string.Empty : JsonConvert.SerializeObject(obj);
        }

        /// <summary>
        /// 将object对象转换为Json数据
        /// </summary>
        /// <param name="obj">object对象</param>
        /// <param name="serializerSettings">序列化设置</param>
        /// <returns>转换后的json字符串</returns>
        public static string ObjectToJson(object obj, JsonSerializerSettings serializerSettings)
        {
            return null == obj ? string.Empty : JsonConvert.SerializeObject(obj, serializerSettings);
        }

        /// <summary>
        /// 将object对象转换为Json数据
        /// </summary>
        /// <param name="obj">目标对象</param>
        /// <param name="isConvertToSingleQuotes">是否将双引号转成单引号</param>
        public static string ObjectToJson(object obj, bool isConvertToSingleQuotes)
        {
            if (obj == null)
                return string.Empty;
            var result = JsonConvert.SerializeObject(obj);
            if (isConvertToSingleQuotes)
                result = result.Replace("\"", "'");
            return result;
        }

        /// <summary>
        /// 将object对象转换为Json数据
        /// </summary>
        /// <param name="obj">目标对象</param>
        /// <param name="isConvertToSingleQuotes">是否将双引号转成单引号</param>
        /// <param name="settings"></param>
        public static string ObjectToJson(object obj, bool isConvertToSingleQuotes, JsonSerializerSettings settings)
        {
            if (obj == null)
                return string.Empty;
            var result = JsonConvert.SerializeObject(obj, settings);
            if (isConvertToSingleQuotes)
                result = result.Replace("\"", "'");
            return result;
        }

        /// <summary>
        /// 将Json对象转换为T对象
        /// </summary>
        /// <typeparam name="T">对象的类型</typeparam>
        /// <param name="jsonString">json对象字符串</param>
        /// <returns>由字符串转换得到的T对象</returns>
        public static T JsonToObject<T>(string jsonString)
        {
            return string.IsNullOrWhiteSpace(jsonString) ? default(T) : JsonConvert.DeserializeObject<T>(jsonString);
        }

        /// <summary>
        /// 将Json对象转换为T对象
        /// </summary>
        /// <typeparam name="T">对象的类型</typeparam>
        /// <param name="jsonString">json对象字符串</param>
        /// <param name="settings"></param>
        /// <returns>由字符串转换得到的T对象</returns>
        public static T JsonToObject<T>(string jsonString, JsonSerializerSettings settings)
        {
            return string.IsNullOrWhiteSpace(jsonString) ? default(T) : JsonConvert.DeserializeObject<T>(jsonString, settings);
        }
    }
}