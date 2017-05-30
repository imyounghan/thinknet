

namespace ThinkNet.Infrastructure
{
    using System;
    using System.IO;

    /// <summary>
    /// 表示一个序列化的接口
    /// </summary>
    public interface ITextSerializer
    {
        /// <summary>
        /// Serializes an object graph to a text reader.
        /// </summary>
        void Serialize(TextWriter writer, object graph, bool containType = false);

        /// <summary>
        /// Deserializes an object graph from the specified text reader.
        /// </summary>
        object Deserialize(TextReader reader);

        /// <summary>
        /// Deserializes an object graph from the specified text reader.
        /// </summary>
        object Deserialize(TextReader reader, Type type);
    }


    /// <summary>
    /// 序列化器的扩展类
    /// </summary>
    public static class TextSerializerExtensions
    {
        /// <summary>
        /// 从字符串反序列化一个对象。
        /// </summary>
        public static object Deserialize(this ITextSerializer serializer, string serialized, Type type)
        {
            using(var reader = new StringReader(serialized)) {
                return serializer.Deserialize(reader, type);
            }
        }
        /// <summary>
        /// 从字符串反序列化一个对象。
        /// </summary>
        public static object Deserialize(this ITextSerializer serializer, string serialized)
        {
            using(var reader = new StringReader(serialized)) {
                return serializer.Deserialize(reader);
            }
        }

        /// <summary>
        /// 序列化一个对象到字符串。
        /// </summary>
        public static string Serialize(this ITextSerializer serializer, object graph)
        {
            return serializer.Serialize(graph, false);
        }

        /// <summary>
        /// 序列化一个对象到字符串。
        /// </summary>
        public static string Serialize(this ITextSerializer serializer, object graph, bool containType)
        {
            using(var writer = new StringWriter()) {
                serializer.Serialize(writer, graph, containType);
                return writer.ToString();
            }
        }

        /// <summary>
        /// 从字符串反序列化一个对象。
        /// </summary>
        public static T Deserialize<T>(this ITextSerializer serializer, string serialized, bool resolveType = false)
            where T : class
        {
            using(var reader = new StringReader(serialized)) {
                if(resolveType)
                    return (T)serializer.Deserialize(reader, typeof(T));
                else
                    return (T)serializer.Deserialize(reader);
            }
        }
    }
}
