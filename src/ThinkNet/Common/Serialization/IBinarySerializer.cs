using System;
using System.IO;

namespace ThinkNet.Common.Serialization
{
    /// <summary>
    /// 表示一个序列化器。用来序列化对象的字节数组
    /// </summary>
    public interface IBinarySerializer
    {
        /// <summary>
        /// 序列化一个对象到 <see cref="DataStream"/>。
        /// </summary>
        void Serialize(Stream stream, object obj, bool containType = false);
        /// <summary>
        /// 反序列化一个对象从一个 <see cref="DataStream"/>。
        /// </summary>
        object Deserialize(Stream stream);
        /// <summary>
        /// 反序列化一个对象从一个 <see cref="DataStream"/>。
        /// </summary>
        object Deserialize(Stream stream, Type type);
    }
}
