using System;
using System.Runtime.Serialization;
using ThinkNet.Infrastructure;

namespace ThinkNet.Messaging
{
    /// <summary>
    /// 实现 <see cref="IEvent"/> 的抽象类
    /// </summary>
    [DataContract]
    [Serializable]
    public abstract class Event : IEvent
    {
        /// <summary>
        /// Default Constructor.
        /// </summary>
        protected Event()
            : this(null)
        { }
        /// <summary>
        /// Parameterized constructor.
        /// </summary>
        protected Event(string id)
        {
            this.Id = id.DefaultIfEmpty(ObjectId.GenerateNewStringId);
        }

        /// <summary>
        /// 事件标识
        /// </summary>
        [DataMember]
        public string Id { get; private set; }

        /// <summary>
        /// 获取源标识的字符串形式
        /// </summary>
        public virtual string GetSourceStringId()
        {
            return string.Empty;
        }

        public override string ToString()
        {
            return string.Concat(this.GetType().FullName, "@", this.Id);
        }
    }    
}
