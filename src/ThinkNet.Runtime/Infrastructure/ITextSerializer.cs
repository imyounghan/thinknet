using System;
using System.IO;

namespace ThinkNet.Infrastructure
{
    /// <summary>
    /// 表示一个序列化器。用来序列化对象的字符串形式
    /// </summary>
    public interface ITextSerializer
    {
        /// <summary>
        /// 序列化一个对象到 <see cref="TextWriter"/>
        /// </summary>
        void Serialize(TextWriter writer, object obj);

        /// <summary>
        /// 序列化一个对象到 <see cref="TextWriter"/>
        /// </summary>
        void Serialize(TextWriter writer, object obj, bool formatType);

        /// <summary>
        /// 从 <see cref="TextReader"/> 反序列化一个对象。
        /// </summary>
        object Deserialize(TextReader reader);

        /// <summary>
        /// 根据 <see cref="Type"/> 从 <see cref="TextReader"/> 反序列化一个对象。
        /// </summary>
        object Deserialize(TextReader reader, Type type);
    }    
}
