using System;

namespace ThinkNet.Infrastructure
{
    /// <summary>
    /// 表示一个序列化器。用来序列化对象的字符串形式
    /// </summary>
    public interface ISerializer
    {
        /// <summary>
        /// 序列化一个对象
        /// </summary>
        string Serialize(object obj, bool indented = false, bool containType = false);

        /// <summary>
        /// 从 <param name="serialized" /> 反序列化一个对象。
        /// </summary>
        object Deserialize(string serialized);

        /// <summary>
        /// 根据 <param name="type" /> 从 <param name="serialized" /> 反序列化一个对象。
        /// </summary>
        object Deserialize(string serialized, Type type);        
    }
}
