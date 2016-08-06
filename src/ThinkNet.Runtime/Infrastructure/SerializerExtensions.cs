using System;
using System.Text;

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
        public static object DeserializeFromBinary(this ISerializer serializer, byte[] data)
        {
            var json = Encoding.UTF8.GetString(data);
            return serializer.Deserialize(json);
        }
        /// <summary>
        /// 从字节数组反序列化一个对象。
        /// </summary>
        public static object DeserializeFromBinary(this ISerializer serializer, byte[] data, Type type)
        {
            var json = Encoding.UTF8.GetString(data);
            return serializer.Deserialize(json, type);
        }
        /// <summary>
        /// 从字节数组反序列化一个对象。
        /// </summary>
        public static T DeserializeFromBinary<T>(this ISerializer serializer, byte[] data)
        {
            return (T)serializer.DeserializeFromBinary(data, typeof(T));
        }
        /// <summary>
        /// 序列化一个对象到字节数组。
        /// </summary>
        public static byte[] SerializeToBinary(this ISerializer serializer, object obj, bool containType = false)
        {
            var json = serializer.Serialize(obj, containType);
            return Encoding.UTF8.GetBytes(json);
        }


        /// <summary>
        /// 从字符串反序列化一个对象。
        /// </summary>
        public static T Deserialize<T>(this ISerializer serializer, string serialized)
            where T : class
        {
            //using (var reader = new StringReader(serialized)) {
            //    if (resolveType)
            //        return (T)serializer.Deserialize(reader, typeof(T));
            //    else
            //        return (T)serializer.Deserialize(reader);
            //}

            return (T)serializer.Deserialize(serialized, typeof(T));
        }        
    }
}
