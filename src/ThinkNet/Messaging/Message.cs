using System;
using System.Runtime.Serialization;
using ThinkNet.Infrastructure;

namespace ThinkNet.Messaging
{
    /// <summary>
    /// 实现 <see cref="IMessage"/> 的抽象类
    /// </summary>
    [DataContract]
    [Serializable]
    public abstract class Message : IMessage
    {
        /// <summary>
        /// Default Constructor.
        /// </summary>
        protected Message()
            : this(ObjectId.GenerateNewStringId())
        { }
        /// <summary>
        /// Parameterized constructor.
        /// </summary>
        protected Message(string id)
            : this(id, DateTime.UtcNow)
        { }

        /// <summary>
        /// Parameterized constructor.
        /// </summary>
        protected Message(string id, DateTime time)
        {
            this.CreatedTime = time.ToUniversalTime();
            this.Id = id;
        }

        /// <summary>
        /// 事件标识
        /// </summary>
        [DataMember]
        public string Id { get; protected set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        [DataMember]
        public DateTime CreatedTime { get; protected set; }

        /// <summary>
        /// 输出该类型的字符串
        /// </summary>
        public override string ToString()
        {
            return string.Concat(this.GetType().FullName, "@", this.Id);
        }

    } 
}
