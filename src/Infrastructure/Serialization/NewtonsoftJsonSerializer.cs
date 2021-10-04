using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;

namespace Infrastructure.Serialization
{
    public class NewtonsoftJsonSerializer : IJsonSerializer
    {
        private readonly JsonSerializerSettings settings;
        private readonly JsonSerializer serializer;
        private readonly ConcurrentDictionary<string, Type> resolvedTypes = new ConcurrentDictionary<string, Type>();

        public NewtonsoftJsonSerializer(bool useUtcInsteadOfLocalTime = false)
        {
            this.settings = new JsonSerializerSettings
            {
                // DEPRECATED
                // Allows deserializing to the actual runtime type
                // without caring for collection types.
                //TypeNameHandling = TypeNameHandling.Objects,

                // In a version resilient way
                TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
                Formatting = Formatting.None,
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                
                DateTimeZoneHandling = useUtcInsteadOfLocalTime ? DateTimeZoneHandling.Utc : DateTimeZoneHandling.Local, // If the app will be international, then change always to UTC.
                DateFormatHandling = DateFormatHandling.IsoDateFormat,
                DateParseHandling = DateParseHandling.DateTime // we need this to parse Datetimes from EventMetadata 
            };
            this.serializer = JsonSerializer.Create(this.settings);
        }

        public string Serialize(object value)
        {
            using (var writer = new StringWriter())
            {
                var jsonWriter = new JsonTextWriter(writer);
                this.serializer.Serialize(jsonWriter, value);
                writer.Flush();
                return writer.ToString();
            }
        }

        public string SerializeToPlainJSON(object value)
        {
            return JsonConvert.SerializeObject(value);
        }

        public T Deserialize<T>(string value) 
        {
            // Deprecated method down bellow
            //public T DeserializeExplicitly<T>(string value) => JsonConvert.DeserializeObject<T>(value, this.settings)!;

            using (var reader = new StringReader(value))
            {
                var jsonReader = new JsonTextReader(reader);

                var deserialized = this.serializer.Deserialize<T>(jsonReader);
                return deserialized;
            }
        }

        public string SerializeDictionary<TKey, TValue>(IDictionary<TKey, TValue> dictionary)
        {
            return JsonConvert.SerializeObject(dictionary, this.settings);
        }

        public IDictionary<TKey, TValue> DeserializeDictionary<TKey, TValue>(string value)
        {
            return JsonConvert.DeserializeObject<IDictionary<TKey, TValue>>(value, this.settings);
        }

        public object? Deserialize(string value, Type type)
        {
            using (var reader = new StringReader(value))
            {
                var jsonReader = new JsonTextReader(reader);

                var deserialized = this.serializer.Deserialize(jsonReader, type);
                return deserialized;
            }
        }

        public object? Deserialize(string value, string fullTypeName, string assembly) =>
            this.Deserialize(value, this.ResolveType(fullTypeName, assembly));

        private Type ResolveType(string fullTypeName, string assembly) => this.resolvedTypes.GetOrAdd($"{fullTypeName}, {assembly}", Type.GetType!);
    }
}
