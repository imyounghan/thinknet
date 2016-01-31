using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Web.Script.Serialization;
using ThinkLib.Common;

namespace ThinkNet.Infrastructure
{
    /// <summary>
    /// 表示一个序列化器。用来序列化对象的字符串形式
    /// </summary>
    [RequiredComponent(typeof(DefaultTextSerializer))]
    public interface ITextSerializer
    {
        /// <summary>
        /// 序列化一个对象到 <see cref="TextWriter"/>
        /// </summary>
        void Serialize(TextWriter writer, object obj);

        /// <summary>
        /// 从 <see cref="TextReader"/> 反序列化一个对象。
        /// </summary>
        object Deserialize(TextReader reader);

        /// <summary>
        /// 根据 <see cref="Type"/> 从 <see cref="TextReader"/> 反序列化一个对象。
        /// </summary>
        object Deserialize(TextReader reader, Type type);
    }


    internal class DefaultTextSerializer : ITextSerializer, IInitializer
    {
        public static readonly ITextSerializer Instance = new DefaultTextSerializer();

        private readonly JavaScriptSerializer _jsonSerializer;

        public DefaultTextSerializer()
        {
            this._jsonSerializer = new JavaScriptSerializer(new CustomTypeResolver());
        }

        public void Serialize(TextWriter writer, object obj)
        {
            writer.Write(_jsonSerializer.Serialize(obj));
            writer.Flush();
        }

        public object Deserialize(TextReader reader)
        {
            return _jsonSerializer.DeserializeObject(reader.ReadToEnd());
        }

        public object Deserialize(TextReader reader, Type type)
        {
            return _jsonSerializer.Deserialize(reader.ReadToEnd(), type);
        }


        public void Initialize(IEnumerable<Type> types)
        {
            //var types = assemblies.SelectMany(assembly => assembly.GetTypes());
            var converterTypes = types.Where(IsConverter);

            if (!converterTypes.Any()) {
                _jsonSerializer.RegisterConverters(new JavaScriptConverter[] { new DefaultJavaScriptConverter(types) });
                return;
            }

            var converters = converterTypes.Select(type => (JavaScriptConverter)(IsInjection(type) ?
                Activator.CreateInstance(type, types) : Activator.CreateInstance(type)));
            _jsonSerializer.RegisterConverters(converters);
        }

        private static bool IsConverter(Type type)
        {
            return type.IsClass && !type.IsAbstract && type.IsSubclassOf(typeof(JavaScriptConverter));
        }

        private static bool IsInjection(Type type)
        {
            return type.GetConstructors().Any(constructor => {
                var parameters = constructor.GetParameters();
                return parameters.Count() == 1 && parameters.First().ParameterType == typeof(IEnumerable<Type>);
            });
        }

        /// <summary>
        /// 自定义类型转换器
        /// </summary>
        class DefaultJavaScriptConverter : JavaScriptConverter
        {
            private readonly IEnumerable<Type> _supportedTypes;
            /// <summary>
            /// Parameterized Constructor.
            /// </summary>
            public DefaultJavaScriptConverter(IEnumerable<Type> types)
            {
                _supportedTypes = types.Where(IsSupportedType).ToArray();
            }

            private static bool IsSupportedType(Type type)
            {
                return type.IsClass && !type.IsAbstract && type.IsDefined<SerializableAttribute>(false);
            }

            /// <summary>
            /// 返回支持的类型
            /// </summary>
            public override IEnumerable<Type> SupportedTypes
            {
                get { return _supportedTypes; }
            }

            /// <summary>
            /// 将对象序列化成名称/值对的字典
            /// </summary>
            public override IDictionary<string, object> Serialize(object obj, JavaScriptSerializer serializer)
            {
                return obj.GetType().GetProperties()
                    .Where(prop => !prop.IsDefined<ScriptIgnoreAttribute>(false) || prop.CanWrite)
                    .ToDictionary(prop => prop.Name, prop => prop.GetValue(obj, null));
            }
            /// <summary>
            /// 将所提供的字典转换为指定类型的对象
            /// </summary>
            public override object Deserialize(IDictionary<string, object> dictionary, Type type, JavaScriptSerializer serializer)
            {
                var obj = FormatterServices.GetUninitializedObject(type);
                obj.GetType().GetProperties()
                    .ForEach(prop => {
                        if (!prop.CanWrite)
                            return;

                        object value;
                        if (dictionary.TryGetValue(prop.Name, out value) && value != null) {
                            if (prop.PropertyType != value.GetType()) {
                                value = TypeConverter.To(value, prop.PropertyType);
                            }
                            prop.SetValue(obj, value, null);
                        }
                    });

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
