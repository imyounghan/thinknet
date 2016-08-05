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
        /// 从字节数组反序列化一个对象。
        /// </summary>
        public static object Deserialize(this IBinarySerializer serializer, byte[] data, Type type)
        {
            using (var stream = new MemoryStream(data)) {
                return serializer.Deserialize(stream, type);
            }
        }
        /// <summary>
        /// 从字节数组反序列化一个对象。
        /// </summary>
        public static T Deserialize<T>(this IBinarySerializer serializer, byte[] data, bool resolveType = false)
        {
            using (var stream = new MemoryStream(data)) {
                if (resolveType)
                    return (T)serializer.Deserialize(stream, typeof(T));
                else
                    return (T)serializer.Deserialize(stream);
            }
        }
        /// <summary>
        /// 序列化一个对象到字节数组。
        /// </summary>
        public static byte[] Serialize(this IBinarySerializer serializer, object obj, bool formatType = false)
        {
            using (var stream = new MemoryStream()) {
                serializer.Serialize(stream, obj, formatType);
                return stream.ToArray();
            }
        }
        

        /// <summary>
        /// 从字符串反序列化一个对象。
        /// </summary>
        public static object Deserialize(this ITextSerializer serializer, string serialized, Type type)
        {
            using (var reader = new StringReader(serialized)) {
                return serializer.Deserialize(reader, type);
            }
        }
        /// <summary>
        /// 从字符串反序列化一个对象。
        /// </summary>
        public static T Deserialize<T>(this ITextSerializer serializer, string serialized, bool resolveType = false)
        {
            using (var reader = new StringReader(serialized)) {
                if (resolveType)
                    return (T)serializer.Deserialize(reader, typeof(T));
                else
                    return (T)serializer.Deserialize(reader);
            }
        }

        /// <summary>
        /// 序列化一个对象到字节串。
        /// </summary>
        public static string Serialize(this ITextSerializer serializer, object data, bool formatType = false)
        {
            using (var writer = new StringWriter()) {
                serializer.Serialize(writer, data, formatType);
                return writer.ToString();
            }
        }
    }
}
