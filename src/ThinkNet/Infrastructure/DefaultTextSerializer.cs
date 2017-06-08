
namespace ThinkNet.Infrastructure
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;
    using System.Runtime.Serialization.Json;
    using System.Text;
    using System.Web.Script.Serialization;

    /// <summary>
    /// <see cref="ITextSerializer" /> 的默认实现。
    /// </summary>
    public class DefaultTextSerializer : ITextSerializer
    {
        #region Fields

        private readonly ITextSerializer serializer;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultTextSerializer"/> class.
        /// </summary>
        public DefaultTextSerializer()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string relativeSearchPath = AppDomain.CurrentDomain.RelativeSearchPath;
            string binPath = string.IsNullOrEmpty(relativeSearchPath)
                                 ? baseDir
                                 : Path.Combine(baseDir, relativeSearchPath);
            string jsonDllPath = string.IsNullOrEmpty(binPath)
                                     ? "Newtonsoft.Json.dll"
                                     : Path.Combine(binPath, "Newtonsoft.Json.dll");

            if (File.Exists(jsonDllPath)
                || AppDomain.CurrentDomain.GetAssemblies().Any(a => a.GetName().Name == "Newtonsoft.Json"))
            {
                this.serializer = new JsonSerializer();
            }
            else
            {
                this.serializer = new NetFrameworkSerializer();
            }
        }

        #endregion

        #region Methods and Operators

        /// <summary>
        /// 从字符串反序列化一个对象。
        /// </summary>
        public object Deserialize(string serialized)
        {
            return this.serializer.Deserialize(serialized);
        }

        /// <summary>
        /// 根据 <param name="type" /> 从 <param name="serialized" /> 反序列化一个对象。
        /// </summary>
        public object Deserialize(string serialized, Type type)
        {
            return this.serializer.Deserialize(serialized, type);
        }

        /// <summary>
        /// 序列化一个对象
        /// </summary>
        public string Serialize(object obj, bool containType)
        {
            return this.serializer.Serialize(obj, containType);
        }

        #endregion

        private class CustomTypeResolver : JavaScriptTypeResolver
        {
            #region Public Methods and Operators
  
            public override Type ResolveType(string id)
            {
                return Type.GetType(id, false, true);
            }

            public override string ResolveTypeId(Type type)
            {
                return string.Concat(
                    type.FullName, 
                    ", ", 
                    Path.GetFileNameWithoutExtension(type.Assembly.ManifestModule.FullyQualifiedName));
            }

            #endregion
        }

        private class DateTimeConverter : JavaScriptConverter
        {
            #region Public Properties

            public override IEnumerable<Type> SupportedTypes
            {
                get
                {
                    yield return typeof(DateTime);
                }
            }

            #endregion

            #region Methods and Operators

            public override object Deserialize(
                IDictionary<string, object> dictionary, 
                Type type, 
                JavaScriptSerializer serializer)
            {
                string value = dictionary["Value"].ToString();
                if (string.IsNullOrEmpty(value))
                {
                    return null;
                }

                return DateTime.Parse(value);
            }

            public override IDictionary<string, object> Serialize(object obj, JavaScriptSerializer serializer)
            {
                var result = new Dictionary<string, object>();
                result["Value"] = obj is DateTime
                                      ? ((DateTime)obj).ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss.FFFFFFFK")
                                      : null;

                return result;
            }

            #endregion
        }

        private class JsonSerializer : ITextSerializer
        {
            #region Fields

            private static readonly Func<object, TextReader, Type, object> DeserializeObjectDelegate;

            private static readonly Type JsonSerializerSettingsType;
            private static readonly Type JsonSerializerType;

            private static readonly Action<object, TextWriter, object> SerializeObjectDelegate;



            private readonly object jsonSerializerWithTypeName;
            private readonly object jsonSerializerWithoutTypeName;

            #endregion

            #region Constructors and Destructors

            static JsonSerializer()
            {
                JsonSerializerType = Type.GetType("Newtonsoft.Json.JsonSerializer, Newtonsoft.Json");

                // JsonConvertType = Type.GetType("Newtonsoft.Json.JsonConvert, Newtonsoft.Json");
                JsonSerializerSettingsType = Type.GetType("Newtonsoft.Json.JsonSerializerSettings, Newtonsoft.Json");

                SerializeObjectDelegate = GetMethodSerializeObject();
                DeserializeObjectDelegate = GetMethodDeserializeObject();
            }

            public JsonSerializer()
            {
                object[] jsonSerializerSettings = CreateJsonSerializerSettings();
                MethodInfo method = JsonSerializerType.GetMethod("Create", new[] { JsonSerializerSettingsType });
                this.jsonSerializerWithoutTypeName = method.Invoke(null, new[] { jsonSerializerSettings[0] });
                this.jsonSerializerWithTypeName = method.Invoke(null, new[] { jsonSerializerSettings[1] });
            }

            #endregion

            #region Methods and Operators

            public object Deserialize(string serialized, Type type)
            {
                using (var reader = new StringReader(serialized))
                {
                    return
                        DeserializeObjectDelegate(
                            type == null ? this.jsonSerializerWithTypeName : this.jsonSerializerWithoutTypeName, 
                            reader, 
                            type);
                }
            }

            public object Deserialize(string serialized)
            {
                return this.Deserialize(serialized, null);
            }

            public string Serialize(object obj, bool containType)
            {
                using (var writer = new StringWriter())
                {
                    SerializeObjectDelegate(
                        containType ? this.jsonSerializerWithTypeName : this.jsonSerializerWithoutTypeName, 
                        writer, 
                        obj);
                    return writer.ToString();
                }
            }


            private static object[] CreateJsonSerializerSettings()
            {
                // var jsonConvertType = Type.GetType("Newtonsoft.Json.JsonConvert, Newtonsoft.Json");
                Type jsonConverterType = Type.GetType("Newtonsoft.Json.JsonConverter, Newtonsoft.Json");
                Type converterListType = typeof(List<>).MakeGenericType(jsonConverterType);
                Type isoDateTimeConverterType =
                    Type.GetType("Newtonsoft.Json.Converters.IsoDateTimeConverter, Newtonsoft.Json");
                object converterList = Activator.CreateInstance(converterListType);
                converterListType.GetMethod("Add")
                    .Invoke(converterList, new[] { Activator.CreateInstance(isoDateTimeConverterType) });

                object withoutTypeNameSetting = Activator.CreateInstance(JsonSerializerSettingsType);
                JsonSerializerSettingsType.GetProperty("ConstructorHandling").SetValue(withoutTypeNameSetting, 1, null);
                JsonSerializerSettingsType.GetProperty("NullValueHandling").SetValue(withoutTypeNameSetting, 1, null);
                JsonSerializerSettingsType.GetProperty("Formatting").SetValue(withoutTypeNameSetting, 0, null);
                JsonSerializerSettingsType.GetProperty("Converters")
                    .SetValue(withoutTypeNameSetting, converterList, null);

                object withTypeNameSetting = Activator.CreateInstance(JsonSerializerSettingsType);
                JsonSerializerSettingsType.GetProperty("ConstructorHandling").SetValue(withTypeNameSetting, 1, null);
                JsonSerializerSettingsType.GetProperty("NullValueHandling").SetValue(withTypeNameSetting, 1, null);
                JsonSerializerSettingsType.GetProperty("Formatting").SetValue(withTypeNameSetting, 0, null);
                JsonSerializerSettingsType.GetProperty("Converters").SetValue(withTypeNameSetting, converterList, null);
                JsonSerializerSettingsType.GetProperty("TypeNameHandling").SetValue(withTypeNameSetting, 3, null);
                JsonSerializerSettingsType.GetProperty("TypeNameAssemblyFormat").SetValue(withTypeNameSetting, 0, null);

                return new[] { withoutTypeNameSetting, withTypeNameSetting };
            }

            private static Func<object, TextReader, Type, object> GetMethodDeserializeObject()
            {
                ParameterExpression jsonParam = Expression.Parameter(typeof(object), "json");
                ParameterExpression readerParam = Expression.Parameter(typeof(TextReader), "reader");
                ParameterExpression typeParam = Expression.Parameter(typeof(Type), "type");
                Expression convertedParam = Expression.Convert(jsonParam, JsonSerializerType);
                MethodInfo method = JsonSerializerType.GetMethod(
                    "Deserialize", 
                    new[] { typeof(TextReader), typeof(Type) });
                MethodCallExpression methodCall = Expression.Call(convertedParam, method, readerParam, typeParam);
                return
                    (Func<object, TextReader, Type, object>)
                    Expression.Lambda(methodCall, new[] { jsonParam, readerParam, typeParam }).Compile();
            }

            private static Action<object, TextWriter, object> GetMethodSerializeObject()
            {
                ParameterExpression jsonParam = Expression.Parameter(typeof(object), "json");
                ParameterExpression writerParam = Expression.Parameter(typeof(TextWriter), "writer");
                ParameterExpression valueParam = Expression.Parameter(typeof(object), "value");
                Expression convertedParam = Expression.Convert(jsonParam, JsonSerializerType);
                MethodInfo method = JsonSerializerType.GetMethod(
                    "Serialize", 
                    new[] { typeof(TextWriter), typeof(object) });
                MethodCallExpression methodCall = Expression.Call(convertedParam, method, writerParam, valueParam);
                return
                    (Action<object, TextWriter, object>)
                    Expression.Lambda(methodCall, new[] { jsonParam, writerParam, valueParam }).Compile();
            }

            #endregion
        }


        private class NetFrameworkSerializer : ITextSerializer
        {
            #region Fields

            private readonly JavaScriptSerializer serializer;

            #endregion

            #region Constructors and Destructors

            public NetFrameworkSerializer()
            {
                this.serializer = new JavaScriptSerializer(new CustomTypeResolver());
                this.serializer.RegisterConverters(new[] { new DateTimeConverter() });

                //this.settings = new DataContractJsonSerializerSettings() {
                //    IgnoreExtensionDataObject = false,
                //    UseSimpleDictionaryFormat = true,
                //    DateTimeFormat = new DateTimeFormat("yyyy'-'MM'-'dd'T'HH':'mm':'ss.FFFFFFFK") {
                //        DateTimeStyles = DateTimeStyles.RoundtripKind
                //    }
                //};
            }

            #endregion

            #region Methods and Operators

            public object Deserialize(string serialized)
            {
                return this.serializer.DeserializeObject(serialized);
            }

            public object Deserialize(string serialized, Type type)
            {
                var serializer = new DataContractJsonSerializer(type);
                byte[] buffer = Encoding.UTF8.GetBytes(serialized);
                using (var stream = new MemoryStream(buffer))
                {
                    return serializer.ReadObject(stream);
                }
            }

            public string Serialize(object obj, bool containType)
            {
                if (containType)
                {
                    return this.serializer.Serialize(obj);
                }

                Type type = obj.GetType();
                var serializer = new DataContractJsonSerializer(type);
                using (var stream = new MemoryStream())
                {
                    serializer.WriteObject(stream, obj);
                    return Encoding.UTF8.GetString(stream.ToArray());
                }
            }

            #endregion
        }
    }
}