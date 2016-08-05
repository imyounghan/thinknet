using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Web.Script.Serialization;

namespace ThinkNet.Infrastructure
{
    /// <summary>
    /// <see cref="ITextSerializer"/> 的默认实现。
    /// </summary>
    public class DefaultTextSerializer : ITextSerializer
    {
        ///// <summary>
        ///// <see cref="ITextSerializer"/> 的一个实例。
        ///// </summary>
        //public static readonly ITextSerializer Instance = new DefaultTextSerializer();

        private readonly JavaScriptSerializer _jsonSerializer;

        public DefaultTextSerializer()
        {
            this._jsonSerializer = new JavaScriptSerializer(new CustomTypeResolver());
            //this._jsonSerializer.RegisterConverters(new JavaScriptConverter[] { new CustomJavaScriptConverter() });
        }

        #region ITextSerializer 成员
        /// <summary>
        /// 序列化一个对象到 <see cref="TextWriter"/>
        /// </summary>
        public void Serialize(TextWriter writer, object obj)
        {
            this.Serialize(writer, obj, false);
        }

        /// <summary>
        /// 序列化一个对象到 <see cref="TextWriter"/>
        /// </summary>
        public void Serialize(TextWriter writer, object obj, bool formatType)
        {
            string json;
            if (formatType) {
                json = _jsonSerializer.Serialize(obj);
            }
            else {
                var serializer = new DataContractJsonSerializer(obj.GetType());
                using (var stream = new MemoryStream()) {
                    serializer.WriteObject(stream, obj);
                    json = Encoding.UTF8.GetString(stream.ToArray());
                }
            }

            writer.Write(json);
            writer.Flush();
        }

        /// <summary>
        /// 从 <see cref="TextReader"/> 反序列化一个对象。
        /// </summary>
        public object Deserialize(TextReader reader)
        {
            return _jsonSerializer.DeserializeObject(reader.ReadToEnd());
        }
        /// <summary>
        /// 从 <see cref="TextReader"/> 反序列化一个对象。
        /// </summary>
        public object Deserialize(TextReader reader, Type type)
        {
            var buffer = Encoding.UTF8.GetBytes(reader.ReadToEnd());
            var serializer = new DataContractJsonSerializer(type);
            using (var stream = new MemoryStream(buffer)) {
                return serializer.ReadObject(stream);
            }
        }

        #endregion


        class CustomJavaScriptConverter : JavaScriptConverter
        {
            /// <summary>
            /// 返回支持的类型
            /// </summary>
            public override IEnumerable<Type> SupportedTypes
            {
                get { return Type.EmptyTypes; }
            }

            private static bool Filtered(PropertyInfo property)
            {
                if (!property.CanWrite)
                    return false;

                if (property.IsDefined(typeof(ScriptIgnoreAttribute), false))
                    return false;

                if (property.IsDefined(typeof(IgnoreDataMemberAttribute), false))
                    return false;

                return true;
            }

            private static string ConvertKey(PropertyInfo property)
            {
                var attribute = property.GetAttribute<DataMemberAttribute>(false);
                if (attribute != null && !string.IsNullOrEmpty(attribute.Name)) {
                    return attribute.Name;
                }
                return property.Name;
            }

            private static object ConvertValue(PropertyInfo property, object obj)
            {
                var value = property.GetValue(obj, null);
                var attribute = property.GetAttribute<DataMemberAttribute>(false);
                if (attribute != null && value == null && attribute.EmitDefaultValue) {
                    return property.PropertyType.GetDefault();
                }

                return value;
            }

            /// <summary>
            /// 将对象序列化成名称/值对的字典
            /// </summary>
            public override IDictionary<string, object> Serialize(object obj, JavaScriptSerializer serializer)
            {
                return obj.GetType().GetProperties()
                    .Where(Filtered)
                    .ToDictionary(ConvertKey, prop => ConvertValue(prop, obj));
            }
            /// <summary>
            /// 将所提供的字典转换为指定类型的对象
            /// </summary>
            public override object Deserialize(IDictionary<string, object> dictionary, Type type, JavaScriptSerializer serializer)
            {
                var obj = FormatterServices.GetUninitializedObject(type);

                foreach (var prop in type.GetProperties()) {
                    if (!prop.CanWrite)
                        continue;

                    object value;
                    if (dictionary.TryGetValue(prop.Name, out value) && value != null) {
                        if (prop.PropertyType != value.GetType()) {
                            value = Convert.ChangeType(value, prop.PropertyType);
                        }
                        prop.SetValue(obj, value, null);
                    }
                }
                

                return obj;
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
