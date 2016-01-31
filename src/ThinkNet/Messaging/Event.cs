using System;
using System.Runtime.Serialization;
using ThinkLib.Utilities;

namespace ThinkNet.Messaging
{
    /// <summary>
    /// 实现 <see cref="IEvent"/> 的抽象类
    /// </summary>
    [DataContract]
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
            this.Id = id.Safe(GuidUtil.NewSequentialId().ToString());
        }

        /// <summary>
        /// 事件标识
        /// </summary>
        [DataMember]
        public string Id { get; private set; }

        /// <summary>
        /// 获取源标识的字符串形式
        /// </summary>
        protected virtual string GetSourceStringId()
        {
            return string.Empty;
        }

        public override string ToString()
        {
            return string.Concat(this.GetType().FullName, "@", this.Id);
            //return this.GetSourceStringId().Safe(this.Id);
        }

        [IgnoreDataMember]
        string IEvent.SourceId
        {
            get { return this.GetSourceStringId(); }
        }     
    }    
}
