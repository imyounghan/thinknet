using System;
using System.Runtime.Serialization;
using ThinkNet.Infrastructure;

namespace ThinkNet.Messaging
{
    /// <summary>
    /// 表示一个事件的抽象类
    /// </summary>
    [DataContract]
    public abstract class Event : IMessage, IUniquelyIdentifiable
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
            : this(id, DateTime.UtcNow)
        { }
        /// <summary>
        /// Parameterized constructor.
        /// </summary>
        protected Event(DateTime time)
            : this(null, time)
        { }
        /// <summary>
        /// Parameterized constructor.
        /// </summary>
        protected Event(string id, DateTime time)
        {
            this.Id = id.IfEmpty(UniqueId.GenerateNewStringId);
            this.CreationTime = time.Kind == DateTimeKind.Utc ? time : time.ToUniversalTime();
        }

        /// <summary>
        /// 事件标识
        /// </summary>
        [DataMember(Name = "id")]
        public string Id { get; private set; }
        /// <summary>
        /// 创建时间
        /// </summary>
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
            return string.Format("{0}@{1}", this.GetType().FullName, this.Id);
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
            return string.Format("{0}@{1}#{2}", this.GetType().FullName, this.Id, this.SourceId);
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
