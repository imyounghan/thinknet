
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
        public static T Deserialize<T>(this IBinarySerializer serializer, byte[] data) where T : class
        {
            return serializer.Deserialize(data, typeof(T)) as T;
        }        
    }
}
