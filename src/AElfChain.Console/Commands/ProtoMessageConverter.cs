using System;
using Google.Protobuf;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace AElfChain.Console.Commands
{
    public class ProtoMessageConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return typeof(IMessage)
                .IsAssignableFrom(objectType);
        }

        public override object ReadJson(JsonReader reader,
            Type objectType, object existingValue,
            JsonSerializer serializer)
        {
            // Read an entire object from the reader.
            var converter = new ExpandoObjectConverter();
            var o = converter.ReadJson(reader, objectType, existingValue,
                serializer);
            // Convert it back to json text.
            var text = JsonConvert.SerializeObject(o, Formatting.Indented);
            // And let protobuf's parser parse the text.
            var message = (IMessage) Activator
                .CreateInstance(objectType);
            return JsonParser.Default.Parse(text,
                message.Descriptor);
        }

        public override void WriteJson(JsonWriter writer, object value,
            JsonSerializer serializer)
        {
            writer.WriteRawValue(JsonFormatter.Default
                .Format((IMessage) value));
        }
    }

    public static class JsonSerializerHelper
    {
        private static JsonSerializerSettings _settings;
        public static JsonSerializerSettings SerializerSettings => GetSerializerSettings();

        private static JsonSerializerSettings GetSerializerSettings()
        {
            if (_settings != null)
                return _settings;

            _settings = new JsonSerializerSettings
            {
                DefaultValueHandling = DefaultValueHandling.Include,
                NullValueHandling = NullValueHandling.Ignore
            };

            _settings.Converters.Add(new ProtoMessageConverter());

            return _settings;
        }
    }
}