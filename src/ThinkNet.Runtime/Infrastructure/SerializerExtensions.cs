using System;
using System.IO;

namespace ThinkNet.Infrastructure
{
    /// <summary>
    /// 序列化器的扩展。
    /// </summary>
    public static class SerializerExtensions
    {
        /// <summary>
        /// 反序列化一个对象类型从一个字节数组。
        /// </summary>
        public static T Deserialize<T>(this IBinarySerializer serializer, byte[] data, Type type) where T : class
        {
            return serializer.Deserialize(data, type) as T;
        }

        /// <summary>
        /// Serializes the given data object as a string.
        /// </summary>
        public static string Serialize<T>(this ITextSerializer serializer, T data)
        {
            using (var writer = new StringWriter()) {
                serializer.Serialize(writer, data);
                return writer.ToString();
            }
        }

        /// <summary>
        /// Serializes the given data object as a string.
        /// </summary>
        public static string Serialize(this ITextSerializer serializer, object data)
        {
            using (var writer = new StringWriter()) {
                serializer.Serialize(writer, data);
                return writer.ToString();
            }
        }

        /// <summary>
        /// Deserializes the specified string into an object of type <typeparamref name="T"/>.
        /// </summary>
        public static T Deserialize<T>(this ITextSerializer serializer, string serialized)
        {
            using (var reader = new StringReader(serialized)) {
                return (T)serializer.Deserialize(reader, typeof(T));
            }
        }

        /// <summary>
        /// Deserializes the specified string into an object.
        /// </summary>
        public static object Deserialize(this ITextSerializer serializer, string serialized)
        {
            using (var reader = new StringReader(serialized)) {
                return serializer.Deserialize(reader);
            }
        }

        /// <summary>
        /// Deserializes the specified string into an object.
        /// </summary>
        public static object Deserialize(this ITextSerializer serializer, string serialized, Type type)
        {
            using (var reader = new StringReader(serialized)) {
                return serializer.Deserialize(reader, type);
            }
        }
    }
}
