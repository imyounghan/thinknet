using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Web.Script.Serialization;

namespace ThinkNet.Common.Serialization
{
    /// <summary>
    /// <see cref="ITextSerializer"/> 的默认实现。
    /// </summary>
    public class DefaultTextSerializer : ITextSerializer
    {
        private readonly ITextSerializer serializer;

        public DefaultTextSerializer()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string relativeSearchPath = AppDomain.CurrentDomain.RelativeSearchPath;
            string binPath = string.IsNullOrEmpty(relativeSearchPath) ? baseDir : Path.Combine(baseDir, relativeSearchPath);
            string jsonDllPath = string.IsNullOrEmpty(binPath) ? "Newtonsoft.Json.dll" : Path.Combine(binPath, "Newtonsoft.Json.dll");

            if (File.Exists(jsonDllPath) || AppDomain.CurrentDomain.GetAssemblies().Any(a => a.GetName().Name == "Newtonsoft.Json")) {
                this.serializer = new JsonSerializer();
            }
            else {
                this.serializer = new OwnSerializer();
            }
        }

        #region ISerializer 成员

        public string Serialize(object obj, bool containType)
        {
            return serializer.Serialize(obj, containType);
        }

        public object Deserialize(string serialized)
        {
            return serializer.Deserialize(serialized);
        }

        public object Deserialize(string serialized, Type type)
        {
            return serializer.Deserialize(serialized, type);
        }

        #endregion

        public class DateTimeConverter : JavaScriptConverter
        {
            public override object Deserialize(IDictionary<string, object> dictionary, Type type, JavaScriptSerializer serializer)
            {
                var value = dictionary["Value"].ToString();
                if (string.IsNullOrEmpty(value))
                    return (DateTime?)null;

                return DateTime.Parse(value);
            }
            public override IDictionary<string, object> Serialize(object obj, JavaScriptSerializer serializer)
            {
                var result = new Dictionary<string, object>();
                result["Value"] = obj is DateTime ?
                    ((DateTime)obj).ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss.FFFFFFFK") :
                    null;

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

        class OwnSerializer : ITextSerializer
        {
            private readonly JavaScriptSerializer serializer;
            private readonly DataContractJsonSerializerSettings settings;

            public OwnSerializer()
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

            public string Serialize(object obj, bool containType)
            {
                if (containType)
                    return this.serializer.Serialize(obj);

                var type = obj.GetType();
                var serializer = new DataContractJsonSerializer(type);
                using (var stream = new MemoryStream()) {
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
                var serializer = new DataContractJsonSerializer(type);
                var buffer = Encoding.UTF8.GetBytes(serialized);
                using (var stream = new MemoryStream(buffer)) {
                    return serializer.ReadObject(stream);
                }
            }
        }

        class JsonSerializer : ITextSerializer
        {
            private static readonly Type JsonSerializerType;
            private static readonly Type JsonSerializerSettingsType;
            private static readonly Action<object, TextWriter, object> SerializeObjectDelegate;
            private static readonly Func<object, TextReader, Type, object> DeserializeObjectDelegate;


            static JsonSerializer()
            {
                JsonSerializerType = Type.GetType("Newtonsoft.Json.JsonSerializer, Newtonsoft.Json");
                //JsonConvertType = Type.GetType("Newtonsoft.Json.JsonConvert, Newtonsoft.Json");
                JsonSerializerSettingsType = Type.GetType("Newtonsoft.Json.JsonSerializerSettings, Newtonsoft.Json");

                SerializeObjectDelegate = GetMethodSerializeObject();
                DeserializeObjectDelegate = GetMethodDeserializeObject();
            }


            static object[] CreateJsonSerializerSettings()
            {
                var jsonConvertType = Type.GetType("Newtonsoft.Json.JsonConvert, Newtonsoft.Json");
                var jsonConverterType = Type.GetType("Newtonsoft.Json.JsonConverter, Newtonsoft.Json");
                var converterListType = typeof(List<>).MakeGenericType(jsonConverterType);
                var isoDateTimeConverterType =Type.GetType("Newtonsoft.Json.Converters.IsoDateTimeConverter, Newtonsoft.Json");
                var converterList = Activator.CreateInstance(converterListType);
                converterListType.GetMethod("Add").Invoke(converterList, new[] { Activator.CreateInstance(isoDateTimeConverterType) });

                var withoutTypeNameSetting = Activator.CreateInstance(JsonSerializerSettingsType);
                JsonSerializerSettingsType.GetProperty("ConstructorHandling").SetValue(withoutTypeNameSetting, 1, null);
                JsonSerializerSettingsType.GetProperty("NullValueHandling").SetValue(withoutTypeNameSetting, 1, null);
                JsonSerializerSettingsType.GetProperty("Formatting").SetValue(withoutTypeNameSetting, 0, null);
                JsonSerializerSettingsType.GetProperty("Converters").SetValue(withoutTypeNameSetting, converterList, null);

                var withTypeNameSetting = Activator.CreateInstance(JsonSerializerSettingsType);
                JsonSerializerSettingsType.GetProperty("ConstructorHandling").SetValue(withTypeNameSetting, 1, null);
                JsonSerializerSettingsType.GetProperty("NullValueHandling").SetValue(withTypeNameSetting, 1, null);
                JsonSerializerSettingsType.GetProperty("Formatting").SetValue(withTypeNameSetting, 0, null);
                JsonSerializerSettingsType.GetProperty("Converters").SetValue(withTypeNameSetting, converterList, null);
                JsonSerializerSettingsType.GetProperty("TypeNameHandling").SetValue(withTypeNameSetting, 3, null);
                JsonSerializerSettingsType.GetProperty("TypeNameAssemblyFormat").SetValue(withTypeNameSetting, 0, null);

                return new[] { withoutTypeNameSetting, withTypeNameSetting };
            }

            private readonly object jsonSerializerWithoutTypeName;
            private readonly object jsonSerializerWithTypeName;
            public JsonSerializer()
            {
                var jsonSerializerSettings = CreateJsonSerializerSettings();
                var method = JsonSerializerType.GetMethod("Create", new[] { JsonSerializerSettingsType });
                this.jsonSerializerWithoutTypeName = method.Invoke(null, new[] { jsonSerializerSettings[0] });
                this.jsonSerializerWithTypeName = method.Invoke(null, new[] { jsonSerializerSettings[1] });
            }

            private static Action<object, TextWriter, object> GetMethodSerializeObject()
            {
                ParameterExpression jsonParam = Expression.Parameter(typeof(object), "json");
                ParameterExpression writerParam = Expression.Parameter(typeof(TextWriter), "writer");
                ParameterExpression valueParam = Expression.Parameter(typeof(object), "value");
                Expression convertedParam = Expression.Convert(jsonParam, JsonSerializerType);
                var method =  JsonSerializerType.GetMethod("Serialize", new[] { typeof(TextWriter), typeof(object) });
                MethodCallExpression methodCall = Expression.Call(convertedParam, method, writerParam, valueParam);
                return (Action<object, TextWriter, object>)Expression.Lambda(methodCall, new[] { jsonParam, writerParam, valueParam }).Compile();
            }

            private static Func<object, TextReader, Type, object> GetMethodDeserializeObject()
            {
                ParameterExpression jsonParam = Expression.Parameter(typeof(object), "json");
                ParameterExpression readerParam = Expression.Parameter(typeof(TextReader), "reader");
                ParameterExpression typeParam = Expression.Parameter(typeof(Type), "type");
                Expression convertedParam = Expression.Convert(jsonParam, JsonSerializerType);
                var method =  JsonSerializerType.GetMethod("Deserialize", new[] { typeof(TextReader), typeof(Type) });
                MethodCallExpression methodCall = Expression.Call(convertedParam, method, readerParam, typeParam);
                return (Func<object, TextReader, Type, object>)Expression.Lambda(methodCall, new[] { jsonParam, readerParam, typeParam }).Compile();
            }

            public string Serialize(object obj, bool containType)
            {
                using (var writer = new StringWriter()) {
                    SerializeObjectDelegate(containType ? jsonSerializerWithTypeName : jsonSerializerWithoutTypeName, writer, obj);
                    return writer.ToString();
                }
            }

            public object Deserialize(string serialized, Type type)
            {
                using (var reader = new StringReader(serialized)) {
                    return DeserializeObjectDelegate(type == null ? jsonSerializerWithTypeName : jsonSerializerWithoutTypeName, reader, type);
                }
            }

            public object Deserialize(string serialized)
            {
                return this.Deserialize(serialized, null);
            }
        }
    }
}
