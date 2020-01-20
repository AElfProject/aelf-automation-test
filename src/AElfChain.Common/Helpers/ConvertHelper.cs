using Newtonsoft.Json;

namespace AElfChain.Common.Helpers
{
    public static class ConvertHelper
    {
        /// <summary>
        ///     convert object to json
        /// </summary>
        /// <param name="obj">object target</param>
        /// <returns>json string</returns>
        public static string ObjectToJson(object obj)
        {
            return obj == null ? string.Empty : JsonConvert.SerializeObject(obj);
        }

        /// <summary>
        ///     convert object to json
        /// </summary>
        /// <param name="obj">object target</param>
        /// <param name="serializerSettings">serializer settings</param>
        /// <returns>json string</returns>
        public static string ObjectToJson(object obj, JsonSerializerSettings serializerSettings)
        {
            return null == obj ? string.Empty : JsonConvert.SerializeObject(obj, serializerSettings);
        }

        /// <summary>
        ///     convert object to json
        /// </summary>
        /// <param name="obj">target obj</param>
        /// <param name="isConvertToSingleQuotes">is convert to single quotes</param>
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
        ///     convert object to json
        /// </summary>
        /// <param name="obj">object target</param>
        /// <param name="isConvertToSingleQuotes">is convert to single quotes</param>
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
        ///     convert json to object
        /// </summary>
        /// <typeparam name="T">object type</typeparam>
        /// <param name="jsonString">json string</param>
        /// <returns>object instance</returns>
        public static T JsonToObject<T>(string jsonString)
        {
            return string.IsNullOrWhiteSpace(jsonString) ? default : JsonConvert.DeserializeObject<T>(jsonString);
        }

        /// <summary>
        ///     convert json to T object
        /// </summary>
        /// <typeparam name="T">object type</typeparam>
        /// <param name="jsonString">json string</param>
        /// <param name="settings"></param>
        /// <returns>T object instance</returns>
        public static T JsonToObject<T>(string jsonString, JsonSerializerSettings settings)
        {
            return string.IsNullOrWhiteSpace(jsonString)
                ? default
                : JsonConvert.DeserializeObject<T>(jsonString, settings);
        }
    }
}