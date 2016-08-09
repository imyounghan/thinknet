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
                TypeNameAssemblyFormat = FormatterAssemblyStyle.Simple,
                NullValueHandling = NullValueHandling.Ignore,
                Formatting = Formatting.None,
                TypeNameHandling = TypeNameHandling.All,
                Converters = new List<JsonConverter>(converters)
            };
            this.withoutTypeNameSettings = new JsonSerializerSettings {
                NullValueHandling = NullValueHandling.Ignore,
                Formatting = Formatting.None,
                TypeNameHandling = TypeNameHandling.None,
                Converters = new List<JsonConverter>(converters)
            };
        }        

        public string Serialize(object obj, bool containType = false)
        {
            try
            {
                return containType ?
                    JsonConvert.SerializeObject(obj, withTypeNameSettings) :
                    JsonConvert.SerializeObject(obj, withoutTypeNameSettings);
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
