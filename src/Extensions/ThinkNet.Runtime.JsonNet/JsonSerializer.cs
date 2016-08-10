using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters;
using ThinkNet.Infrastructure;

namespace ThinkNet.Runtime
{
    [Register(typeof(ISerializer))]
    public class JsonSerializer : ISerializer
    {
        private readonly JsonSerializerSettings withoutTypeNameSettings;
        private readonly JsonSerializerSettings withTypeNameSettings;
        public JsonSerializer()
        {
            var converters = new JsonConverter[] {
                new IsoDateTimeConverter()
            };
            this.withTypeNameSettings = new JsonSerializerSettings {
                // In a version resilient way
                ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor,
                NullValueHandling = NullValueHandling.Ignore,
                TypeNameHandling = TypeNameHandling.All,
                TypeNameAssemblyFormat = FormatterAssemblyStyle.Simple,
                Converters = new List<JsonConverter>(converters)
            };
            this.withoutTypeNameSettings = new JsonSerializerSettings {
                ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor,
                NullValueHandling = NullValueHandling.Ignore,
                 CheckAdditionalContent = false,
                Converters = new List<JsonConverter>(converters)
            };
        }

        public string Serialize(object obj, bool indented, bool containType)
        {
            try
            {
                var formatting = indented ? Formatting.Indented : Formatting.None;

                return containType ?
                    JsonConvert.SerializeObject(obj, formatting, withTypeNameSettings) :
                    JsonConvert.SerializeObject(obj, formatting, withoutTypeNameSettings);
            }
            catch (JsonSerializationException ex)
            {
                // Wrap in a standard .NET exception.
                throw new SerializationException(ex.Message, ex);
            }
        }

        public object Deserialize(string serialized)
        {
            return this.Deserialize(serialized, null);
        }

        public object Deserialize(string serialized, Type type)
        {
            try
            {
                return type == null ?
                    JsonConvert.DeserializeObject(serialized, withoutTypeNameSettings) :
                    JsonConvert.DeserializeObject(serialized, type, withTypeNameSettings);
            }
            catch (JsonSerializationException ex)
            {
                // Wrap in a standard .NET exception.
                throw new SerializationException(ex.Message, ex);
            }
        }
    }    
}
