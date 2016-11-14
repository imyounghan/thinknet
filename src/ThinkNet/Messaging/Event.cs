﻿using System;
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

            this.Id = id;
            this.CreationTime = time.Kind == DateTimeKind.Utc ? time : time.ToUniversalTime();
        }

        [DataMember(Name = "id")]
        public string Id { get; private set; }
        [DataMember(Name = "creationTime")]
        public DateTime CreationTime { get; set; }

        /// <summary>
        /// 获取源标识的字符串形式
        /// </summary>
        protected virtual string GetSourceStringId()
        {
            return null;
        }

        #region IMessage 成员

        string IMessage.GetKey()
        {
            return this.GetSourceStringId();
        }

        #endregion
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
        /// 获取源标识的字符串形式
        /// </summary>
        protected override string GetSourceStringId()
        {
            return this.SourceId.ToString();
        }
    }
}
