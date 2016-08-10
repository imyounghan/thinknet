using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Web.Script.Serialization;

namespace ThinkNet.Infrastructure
{
    /// <summary>
    /// <see cref="ISerializer"/> 的默认实现。
    /// </summary>
    public class DefaultSerializer : ISerializer
    {
        private readonly JavaScriptSerializer serializer;
        private readonly DataContractJsonSerializerSettings settings;

        public DefaultSerializer()
        {
            this.serializer = new JavaScriptSerializer(new CustomTypeResolver());
            this.serializer.RegisterConverters(new[] { new DateTimeConverter() });
            this.settings = new DataContractJsonSerializerSettings() {
                //IgnoreExtensionDataObject = false,
                UseSimpleDictionaryFormat = true,
                DateTimeFormat = new DateTimeFormat("yyyy'-'MM'-'dd'T'HH':'mm':'ss.FFFFFFFK") {
                     DateTimeStyles = DateTimeStyles.RoundtripKind
                }
            };
        }

        #region ISerializer 成员

        public string Serialize(object obj, bool indented, bool containType)
        {
            if(containType)
                return this.serializer.Serialize(obj);

            var type = obj.GetType();
            var serializer = new DataContractJsonSerializer(type, settings);
            using (var stream = new MemoryStream())
            {
                serializer.WriteObject(stream, obj);
                return Encoding.UTF8.GetString(stream.ToArray());
            }
        }

        public object Deserialize(string serialized)
        {
            return serializer.DeserializeObject(serialized);
        }

        public object Deserialize(string serialized, Type type)
        {
            var serializer = new DataContractJsonSerializer(type, settings);
            var buffer = Encoding.UTF8.GetBytes(serialized);
            using (var stream = new MemoryStream(buffer))
            {
                return serializer.ReadObject(stream);
            }
        }

        #endregion

        public class DateTimeConverter : JavaScriptConverter
        {
            public override object Deserialize(IDictionary<string, object> dictionary, Type type, JavaScriptSerializer serializer)
            {
                var value = dictionary["Value"].ToString();
                if(string.IsNullOrEmpty(value))
                    return (DateTime?)null;

                return DateTime.Parse(value);
            }
            public override IDictionary<string, object> Serialize(object obj, JavaScriptSerializer serializer)
            {
                var result = new Dictionary<string, object>();
                result["Value"] = obj is DateTime ?
                    ((DateTime)obj).ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss.FFFFFFFK") :
                    string.Empty;

                return result;
            }
            public override IEnumerable<Type> SupportedTypes
            {
                get { yield return typeof(DateTime); }
            }
        }


        class CustomTypeResolver : JavaScriptTypeResolver
        {
            public override Type ResolveType(string id)
            {
                return Type.GetType(id, false, true);
            }

            public override string ResolveTypeId(Type type)
            {
                return string.Concat(type.FullName, ", ", Path.GetFileNameWithoutExtension(type.Assembly.ManifestModule.FullyQualifiedName));
            }
        }
    }
}
