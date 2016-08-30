using System;
using System.Runtime.Serialization;

namespace ThinkNet.Messaging
{
    /// <summary>
    /// 实现 <see cref="IEvent"/> 的抽象类
    /// </summary>
    [DataContract]
    [Serializable]
    public abstract class Event : Message, IEvent
    {
        /// <summary>
        /// Default Constructor.
        /// </summary>
        protected Event()
            : this(string.Empty)
        { }
        /// <summary>
        /// Parameterized constructor.
        /// </summary>
        protected Event(string id)
            : base(id)
        { }
        
        /// <summary>
        /// 事件来源的标识id
        /// </summary>
        //[DataMember(Name = "sourceId")]
        protected string SourceId { get; set; }

        #region IEvent 成员
        [IgnoreDataMember]
        string IEvent.SourceId
        {
            get
            {
                return this.SourceId;
            }
            set
            {
                this.SourceId = value;
            }
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
        new public TSourceId SourceId
        {
            get { return (TSourceId)base.SourceId.Change(typeof(TSourceId)); }
            internal set { base.SourceId = value.ToString(); }
        }

        /// <summary>
        /// 输出字符串信息
        /// </summary>
        public override string ToString()
        {
            return string.Concat(this.GetType().FullName, "&", this.SourceId, "@", this.Id);
        }

        ///// <summary>
        ///// 获取源标识的字符串形式
        ///// </summary>
        //public override string GetSourceStringId()
        //{
        //    return this.SourceId.ToString();
        //}
    }
}
