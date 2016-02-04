using System;
using System.Runtime.Serialization;
using ThinkNet.Messaging;

namespace ThinkNet.Kernel
{
    /// <summary>
    /// Represents an abstract domain event.
    /// </summary>
    [DataContract]
    [Serializable]
    public abstract class Event<TSourceId> : Event
    {
        /// <summary>
        /// Default Constructor.
        /// </summary>
        protected Event()
        { }
        /// <summary>
        /// Parameterized Constructor.
        /// </summary>
        protected Event(string eventId)
            : base(eventId)
        { }

        /// <summary>
        /// 事件来源的标识id
        /// </summary>
        [DataMember]
        public TSourceId SourceId { get; internal set; }

        /// <summary>
        /// 输出字符串信息
        /// </summary>
        public override string ToString()
        {
            return string.Concat(this.GetType().FullName, "|", this.SourceId);
        }

        /// <summary>
        /// 获取源标识的字符串形式
        /// </summary>
        protected override string GetSourceStringId()
        {
            return this.SourceId.ToString();
        }
    }
}
