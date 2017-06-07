

namespace ThinkNet.Infrastructure
{
    using System;
    using System.IO;

    /// <summary>
    /// 表示一个序列化器。用来序列化对象的字符串形式
    /// </summary>
    public interface ITextSerializer
    {
        /// <summary>
        /// 序列化一个对象
        /// </summary>
        string Serialize(object obj, bool containType = false);

        /// <summary>
        /// 从 <param name="serialized" /> 反序列化一个对象。
        /// </summary>
        object Deserialize(string serialized);

        /// <summary>
        /// 根据 <param name="type" /> 从 <param name="serialized" /> 反序列化一个对象。
        /// </summary>
        object Deserialize(string serialized, Type type);
    }


    /// <summary>
    /// 序列化器的扩展类
    /// </summary>
    public static class TextSerializerExtensions
    {
        /// <summary>
        /// 从字符串反序列化一个对象。
        /// </summary>
        public static T Deserialize<T>(this ITextSerializer serializer, string serialized, bool resolveType = false)
            where T : class
        {
            if (resolveType)
                return (T)serializer.Deserialize(serialized, typeof(T));
            else
                return (T)serializer.Deserialize(serialized);
        }
    }
}
