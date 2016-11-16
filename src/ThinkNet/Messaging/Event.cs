using System;
using System.Runtime.Serialization;
using ThinkNet.Common;

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
            : this(ObjectId.GenerateNewStringId())
        { }
        /// <summary>
        /// Parameterized constructor.
        /// </summary>
        protected Event(string id)
            : this(id, DateTime.UtcNow)
        { }
        /// <summary>
        /// Parameterized constructor.
        /// </summary>
        protected Event(DateTime time)
            : this(ObjectId.GenerateNewStringId(), time)
        { }
        /// <summary>
        /// Parameterized constructor.
        /// </summary>
        protected Event(string id, DateTime time)
        {
            id.NotNullOrWhiteSpace("id");

            this.UniqueId = id;
            this.CreationTime = time.Kind == DateTimeKind.Utc ? time : time.ToUniversalTime();
        }

        [DataMember(Name = "id")]
        public string UniqueId { get; private set; }
        [DataMember(Name = "creationTime")]
        public DateTime CreationTime { get; set; }

        /// <summary>
        /// 获取该事件的Key
        /// </summary>
        public virtual string GetKey()
        {
            return null;
        }

        /// <summary>
        /// 输出字符串信息
        /// </summary>
        public override string ToString()
        {
            return string.Format("{0}@{1}", this.GetType().FullName, this.UniqueId);
        }
    }

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
        [DataMember(Name = "sourceId")]
        public TSourceId SourceId { get; internal set; }

        /// <summary>
        /// 输出字符串信息
        /// </summary>
        public override string ToString()
        {
            return string.Format("{0}@{1}#{2}", this.GetType().FullName, this.UniqueId, this.SourceId);
        }

        /// <summary>
        /// 获取该事件的Key
        /// </summary>
        public override string GetKey()
        {
            return this.SourceId.ToString();
        }
    }
}
